(() => {
  // Browser XML, feeds and plain-text viewers do not expose HTML style elements.
  if (document.contentType && document.contentType !== "text/html") return;
  const api = globalThis.browser ?? globalThis.chrome;
  if (!api?.runtime?.sendMessage || globalThis.ViFreeOverlayActive) return;
  globalThis.ViFreeOverlayActive = true;

  const arabic = document.documentElement.lang.toLowerCase().startsWith("ar");
  const text = arabic ? {
    open: "تنزيل بواسطة Vydrop", action: "نسخ رابط الفيديو وفتحه في Vydrop", finding: "جارٍ تحديد الفيديو…",
    opening: "جارٍ الفتح…", opened: "تم الفتح في Vydrop", copied: "تم نسخ الرابط وفتح الفيديو في Vydrop.",
    clipboard: "تم الفتح في Vydrop، لكن تعذّر نسخ الرابط.",
    retry: "حاول مجددًا"
  } : {
    open: "Download by Vydrop", action: "Copy video link and open Vydrop", finding: "Finding video…",
    opening: "Opening…", opened: "Opened in Vydrop", copied: "Link copied and video opened in Vydrop.",
    clipboard: "Opened in Vydrop. The clipboard was unavailable.",
    retry: "Try again"
  };
  const host = document.createElement("div");
  host.id = "vifree-video-action";
  host.style.cssText = "all:initial!important;position:fixed!important;z-index:2147483646!important;display:none!important;pointer-events:none!important;";
  const shadow = host.attachShadow({ mode: "open" });
  const style = document.createElement("style");
  style.textContent = `
    :host{color-scheme:dark;font-family:system-ui,sans-serif}
    *{box-sizing:border-box}
    .action{display:flex;align-items:center;gap:7px;min-height:34px;max-width:100%;margin-left:auto;padding:6px 10px;
      border:1px solid #94828d;border-radius:8px;background:#282b32;color:#f6f2f4;font:600 12px/20px system-ui,sans-serif;
      cursor:pointer;pointer-events:auto;white-space:nowrap;transition:background-color 140ms ease-out,border-color 140ms ease-out}
    .action:hover{background:#3e2933;border-color:#ff8393}
    :host([data-placement=side]) .action{margin-left:0}
    .action:focus-visible{outline:2px solid #ff8393;outline-offset:3px}
    .action:disabled{cursor:progress;opacity:.85}
    .action[data-state=success]{background:#3e2933;border-color:#ff8393}
    .action svg{width:17px;height:18px;flex:none;fill:#e63d4d;direction:ltr}
    .action span{overflow:hidden;text-overflow:ellipsis}
    :host([data-compact=true]) .action{width:36px;min-width:36px;justify-content:center;gap:0;padding:6px 8px}
    :host([data-compact=true]) .action span{display:none}
    .status{display:none;position:absolute;right:0;bottom:calc(100% + 6px);width:min(250px,calc(100vw - 16px));padding:8px 10px;border:1px solid #94828d;border-radius:8px;
      background:#282b32;color:#f6f2f4;font:400 12px/18px system-ui,sans-serif;overflow-wrap:anywhere}
    .status:not(:empty){display:block}
    .status[data-compact=true]{width:1px;height:1px;padding:0;border:0;overflow:hidden;clip-path:inset(50%)}
    ::selection{background:#3e2933;color:#f6f2f4}
    @media(prefers-reduced-motion:reduce){.action{transition:none}}
    @media(forced-colors:active){.action,.status{background:Canvas;color:CanvasText;border-color:ButtonText}.action svg{fill:ButtonText}}
  `;
  const button = document.createElement("button");
  button.type = "button";
  button.className = "action";
  button.dir = arabic ? "rtl" : "ltr";
  button.setAttribute("aria-label", text.open);
  button.setAttribute("aria-description", text.action);
  button.title = text.action;
  const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  icon.setAttribute("viewBox", "0 0 64 64");
  icon.setAttribute("aria-hidden", "true");
  const mark = document.createElementNS("http://www.w3.org/2000/svg", "path");
  mark.setAttribute("fill-rule", "evenodd");
  mark.setAttribute("d", "M4,8.016L21.625,8.016 27.873,18.056 27.873,27.426 21.179,27.426 32,39.474 42.821,27.426 36.127,27.426 36.127,18.056 42.375,8.016 60,8.016 60,11.586 32,55.984 4,11.586Z");
  const arrow = document.createElementNS("http://www.w3.org/2000/svg", "path");
  arrow.setAttribute("fill", "#FFFFFF");
  arrow.setAttribute("d", "M27.873,18.056L27.873,27.426 21.179,27.426 32,39.474 42.821,27.426 36.127,27.426 36.127,18.056Z");
  icon.append(mark, arrow);
  const label = document.createElement("span");
  label.textContent = text.open;
  button.append(icon, label);
  const status = document.createElement("div");
  status.className = "status";
  status.dir = button.dir;
  status.setAttribute("role", "status");
  status.setAttribute("aria-live", "polite");
  shadow.append(style, button, status);

  let currentVideo = null, pendingVideo = null, busy = false, scheduled = false, feedbackTimer;
  const playing = video => video?.isConnected && !video.paused && !video.ended && video.readyState >= 2;
  const intersects = (a, b) => b.width > 0 && b.height > 0 && a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
  function occupied(rect) {
    const covers = new Set();
    for (const x of [rect.left + 2, (rect.left + rect.right) / 2, rect.right - 2])
      for (const y of [rect.top + 2, (rect.top + rect.bottom) / 2, rect.bottom - 2]) {
        const cover = document.elementFromPoint(x, y);
        if (cover && cover !== host) covers.add(cover);
      }
    for (const cover of covers) {
      if (cover.closest('a,button,input,textarea,select,[role="button"],[role="link"],video,canvas,iframe')) return true;
      const walker = document.createTreeWalker(cover, NodeFilter.SHOW_TEXT);
      for (let node, count = 0; (node = walker.nextNode()) && count++ < 80;) {
        if (!node.textContent.trim() || node.parentElement.closest('script,style,noscript')) continue;
        if (getComputedStyle(node.parentElement).visibility === "hidden") continue;
        const range = document.createRange(); range.selectNodeContents(node);
        if (Array.from(range.getClientRects()).some(box => intersects(rect, box))) return true;
      }
    }
    return false;
  }
  function bounds(video) {
    const r = video.getBoundingClientRect();
    if (r.width < 80 || r.height < 60 || r.bottom <= 0 || r.top >= innerHeight || r.right <= 0 || r.left >= innerWidth) return null;
    const paint = getComputedStyle(video);
    if (paint.display === "none" || paint.visibility === "hidden") return null;
    let visual = r;
    if (Number(paint.opacity) === 0) {
      const canvas = Array.from(video.parentElement?.querySelectorAll("canvas") ?? []).find(c => {
        const r = c.getBoundingClientRect(), s = getComputedStyle(c);
        return r.width >= 80 && r.height >= 60 && s.display !== "none" && s.visibility !== "hidden" && Number(s.opacity) > 0;
      });
      if (!canvas) return null;
      visual = canvas.getBoundingClientRect();
    }
    for (let p = video.parentElement, depth = 0; p && p !== document.body && depth < 40; p = p.parentElement, depth++) {
      const s = getComputedStyle(p);
      if (s.display === "none" || s.visibility === "hidden" || Number(s.opacity) === 0) return null;
    }
    return visual;
  }
  function refresh() {
    scheduled = false;
    if (document.hidden || document.pictureInPictureElement) { host.style.setProperty("display", "none", "important"); return; }
    let video = busy && playing(pendingVideo) ? pendingVideo : null;
    if (!video) {
      const candidates = Array.from(document.querySelectorAll("video")).slice(0, 100).filter(v => !v.paused && !v.ended && v.readyState >= 2)
        .map(video => ({ video, rect: bounds(video) })).filter(v => v.rect)
        .sort((a, b) => visibleArea(b.rect) - visibleArea(a.rect));
      video = candidates[0]?.video;
    }
    const rect = video && bounds(video);
    const fullscreen = document.fullscreenElement;
    if (!video || !rect || !video.isConnected || fullscreen?.tagName === "VIDEO" || fullscreen && !fullscreen.contains(video)) {
      host.style.setProperty("display", "none", "important"); return;
    }
    const dialog = Array.from(document.querySelectorAll('dialog:modal,[role="dialog"][aria-modal="true"]')).find(d => {
      const r = d.getBoundingClientRect();
      return r.width > 0 && r.height > 0 && !d.closest('[inert],[aria-hidden="true"]') &&
        d.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true });
    });
    if (dialog && !dialog.contains(video)) { host.style.setProperty("display", "none", "important"); return; }
    if (currentVideo !== video && !busy) { label.textContent = text.open; status.textContent = ""; button.title = text.action; delete button.dataset.state; }
    currentVideo = video;
    const parent = fullscreen || document.documentElement;
    if (host.parentElement !== parent) parent.append(host);
    const width = Math.min(190, innerWidth - 16);
    const locations = [{ placement: "above", left: Math.max(8, Math.min(rect.right, innerWidth - 8) - width), top: rect.top - 40, width }];
    for (const offset of [width / 2, width * 3 / 4]) {
      const left = locations[0].left - offset;
      if (left >= Math.max(8, rect.left)) locations.push({ placement: "above", left, top: rect.top - 40, width });
    }
    // A wide search box can occupy the first position even though there is clear
    // header space just beyond the player's right edge. Keep these fallbacks
    // above the picture, without moving the website or covering its controls.
    for (const left of [rect.right + 8, rect.right + width / 2 + 8])
      if (left + width <= innerWidth - 8) locations.push({ placement: "above", left, top: rect.top - 40, width });
    const sideWidth = Math.min(190, innerWidth - rect.right - 16);
    if (sideWidth >= 160) locations.push({ placement: "side", left: rect.right + 8, top: Math.max(8, rect.top), width: sideWidth });
    // Logged-in headers can leave only a small gap between search and account
    // controls. Try the same outside area with the brand icon before hiding it.
    const compactWidth = 36, compactLeft = Math.min(rect.right, innerWidth - 8) - compactWidth;
    for (const offset of [0, 44, 88, 132, -44, -88, -132]) {
      const left = compactLeft + offset;
      if (left >= Math.max(8, rect.left) && left + compactWidth <= innerWidth - 8)
        locations.push({ placement: "above", compact: true, left, top: rect.top - 40, width: compactWidth });
    }
    if (sideWidth >= compactWidth) locations.push({ placement: "side", compact: true, left: rect.right + 8, top: Math.max(8, rect.top), width: compactWidth });
    // Collapsed captions and sidebar headings can fill the first row. Keep the
    // existing corner choices first, then search nearby space outside the frame.
    for (const offset of [40, 80, 120]) {
      if (sideWidth >= 160) locations.push({ placement: "side", left: rect.right + 8, top: Math.max(8, rect.top) + offset, width: sideWidth });
      if (sideWidth >= compactWidth) locations.push({ placement: "side", compact: true, left: rect.right + 8, top: Math.max(8, rect.top) + offset, width: compactWidth });
    }
    let placed = false;
    // Prefer above the top-right corner, then the clear space immediately to its
    // right. Both positions keep the whole tab outside the picture.
    for (const location of locations) {
      if (location.top < 8 || location.top + 34 > innerHeight - 4) continue;
      host.dataset.placement = location.placement;
      host.dataset.compact = location.compact ? "true" : "false";
      host.style.setProperty("left", location.left + "px", "important");
      host.style.setProperty("top", location.top + "px", "important");
      host.style.setProperty("max-width", location.width + "px", "important");
      host.style.setProperty("width", location.width + "px", "important");
      host.style.setProperty("visibility", "hidden", "important");
      host.style.setProperty("display", "block", "important");
      const buttonRect = button.getBoundingClientRect();
      // Empty header background is usable; actual text and interactive controls
      // still win the collision check, regardless of their container's role.
      if (occupied(buttonRect)) continue;
      placed = true; break;
    }
    if (!placed) { host.style.setProperty("display", "none", "important"); return; }
    delete status.dataset.compact;
    if (status.textContent) {
      const feedback = status.getBoundingClientRect();
      if (feedback.top < 8 || occupied(feedback)) status.dataset.compact = "true";
    }
    host.style.setProperty("visibility", "visible", "important");
  }
  function visibleArea(r) {
    return Math.max(0, Math.min(r.right, innerWidth) - Math.max(r.left, 0)) * Math.max(0, Math.min(r.bottom, innerHeight) - Math.max(r.top, 0));
  }
  function schedule() {
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(refresh);
  }
  button.addEventListener("click", async event => {
    if (!event.isTrusted || busy || !currentVideo) return;
    event.preventDefault(); event.stopPropagation();
    busy = true; pendingVideo = currentVideo;
    button.disabled = true; button.setAttribute("aria-busy", "true");
    clearTimeout(feedbackTimer); status.textContent = ""; label.textContent = text.finding;
    delete button.dataset.state; button.title = text.action;
    try {
      const source = pendingVideo.currentSrc || pendingVideo.src;
      let found;
      try { found = await globalThis.ViFreeVideoDetection.inspectVideoPage(source, true, pendingVideo); }
      catch { throw Object.assign(new Error("Page inspection failed."), { code: "inspect" }); }
      if (!found?.url) throw Object.assign(new Error("No unambiguous video link."), { code: found?.reason === "ambiguous" ? "ambiguous" : "missing" });
      if (globalThis.VydropQualityPicker) {
        globalThis.VydropQualityPicker.open({ anchor: button, arabic,
          inspect: async () => {
            const response = await api.runtime.sendMessage({ version: 1, type: "vydrop-inspect-video", url: found.url, linkedinAsset: found.linkedinAsset, sourcePage: found.sourcePage });
            if (!response?.ok) throw Object.assign(new Error("inspect"), { code: response?.code || "inspect" });
            return response.media;
          },
          send: async output => {
            const response = await api.runtime.sendMessage({ version: 1, type: "vifree-open-video", url: found.url, linkedinAsset: found.linkedinAsset, sourcePage: found.sourcePage, output });
            if (!response?.ok) throw Object.assign(new Error("open"), { code: response?.code || "failed" });
            return response;
          }
        });
        return;
      }
      let copied = false;
      try { await navigator.clipboard.writeText(found.url); copied = true; } catch { /* Opening the app remains useful when clipboard access is unavailable. */ }
      label.textContent = text.opening;
      const response = await api.runtime.sendMessage({ version: 1, type: "vifree-open-video", url: found.url, linkedinAsset: found.linkedinAsset, sourcePage: found.sourcePage });
      if (!response?.ok) throw Object.assign(new Error("Video delivery failed."), { code: response?.code });
      if (response.url && response.url !== found.url) {
        try { await navigator.clipboard.writeText(response.url); copied = true; } catch { copied = false; }
      }
      label.textContent = text.opened; button.dataset.state = "success";
      status.textContent = copied ? text.copied : text.clipboard;
    } catch (error) {
      label.textContent = text.retry;
      button.dataset.state = "error";
      status.textContent = globalThis.ViFreeConnection.errorMessage(error, arabic);
      button.title = status.textContent;
    } finally {
      busy = false; pendingVideo = null; button.disabled = false; button.removeAttribute("aria-busy");
      if (button.dataset.state === "success")
        feedbackTimer = setTimeout(() => { status.textContent = ""; label.textContent = text.open; button.title = text.action; delete button.dataset.state; schedule(); }, 4500);
      schedule();
    }
  });
  const observer = new MutationObserver(records => {
    if (records.some(record => record.target !== host && !host.contains(record.target))) schedule();
  });
  observer.observe(document.documentElement, { childList: true, subtree: true });
  for (const event of ["play", "playing", "loadeddata", "canplay", "pause", "ended", "loadedmetadata", "emptied", "fullscreenchange", "visibilitychange"])
    document.addEventListener(event, schedule, true);
  addEventListener("scroll", schedule, { passive: true, capture: true });
  addEventListener("resize", schedule, { passive: true });
  button.addEventListener("blur", schedule);
  schedule();
})();
