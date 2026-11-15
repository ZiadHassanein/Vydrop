(() => {
  // Browser XML, feeds and plain-text viewers do not expose HTML style elements.
  if (document.contentType && document.contentType !== "text/html") return;
  const api = globalThis.browser ?? globalThis.chrome;
  if (!api?.runtime?.sendMessage || globalThis.ViFreeOverlayActive) return;
  globalThis.ViFreeOverlayActive = true;

  let arabic = document.documentElement.lang.toLowerCase().startsWith("ar");
  const overlayText = () => arabic ? {
    open: "تنزيل بواسطة Vydrop", action: "نسخ رابط الفيديو وفتحه في Vydrop", audioAction: "فتح المقطع الصوتي في Vydrop", finding: "جارٍ تحديد المقطع…",
    opening: "جارٍ الفتح…", opened: "تم الفتح في Vydrop", copied: "تم نسخ الرابط وفتح الفيديو في Vydrop.",
    clipboard: "تم الفتح في Vydrop، لكن تعذّر نسخ الرابط.",
    retry: "حاول مجددًا", move: "اسحب الزر لتغيير مكانه. يمكنك أيضًا استخدام Shift مع الأسهم للتحريك، وShift مع Home لإعادة مكانه."
  } : {
    open: "Download by Vydrop", action: "Copy video link and open Vydrop", audioAction: "Open audio in Vydrop", finding: "Finding media…",
    opening: "Opening…", opened: "Opened in Vydrop", copied: "Link copied and video opened in Vydrop.",
    clipboard: "Opened in Vydrop. The clipboard was unavailable.",
    retry: "Try again", move: "Drag to move. Shift + Arrow keys also move the button; Shift + Home resets its position."
  };
  let text = overlayText();
  const actionHint = () => (host.dataset.mediaKind === "audio" ? text.audioAction : text.action) + ". " + text.move;
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
      cursor:grab;touch-action:none;user-select:none;pointer-events:auto;white-space:nowrap;transition:background-color 140ms ease-out,border-color 140ms ease-out}
    .action[data-dragging=true]{cursor:grabbing;border-color:#ff8393}
    .action:hover{background:#3e2933;border-color:#ff8393}
    :host([data-placement=side]) .action{margin-left:0}
    .action:focus-visible{outline:2px solid #ff8393;outline-offset:3px}
    .action:disabled{cursor:progress;opacity:.85}
    .action[data-state=success]{background:#3e2933;border-color:#ff8393}
    .action svg{width:17px;height:18px;flex:none;fill:#e63d4d;direction:ltr}
    .action span{overflow:hidden;text-overflow:ellipsis}
    :host([data-compact=true]) .action{width:36px;min-width:36px;justify-content:center;gap:0;padding:6px 8px}
    :host([data-compact=true]) .action span{display:none}
    .status{display:none;position:absolute;right:0;top:calc(100% + 6px);width:min(250px,calc(100vw - 16px));padding:8px 10px;border:1px solid #94828d;border-radius:8px;
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
  button.setAttribute("aria-description", actionHint());
  button.setAttribute("aria-keyshortcuts", "Shift+ArrowLeft Shift+ArrowRight Shift+ArrowUp Shift+ArrowDown Shift+Home");
  button.title = actionHint();
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

  let currentVideo = null, pendingVideo = null, busy = false, scheduled = false, feedbackTimer, chooser;
  let placementArea, manualPosition, drag, suppressClick = false;
  function applyLanguage(language) {
    if (arabic === (language === "ar")) return;
    const previous = text, previousArabic = arabic; arabic = language === "ar"; text = overlayText();
    button.dir = status.dir = arabic ? "rtl" : "ltr"; button.lang = status.lang = language;
    button.setAttribute("aria-label", text.open);
    button.setAttribute("aria-description", actionHint());
    for (const node of [label, status]) {
      const key = Object.keys(previous).find(key => previous[key] === node.textContent);
      if (key) node.textContent = text[key];
      else if (node === label && node.textContent === (previousArabic ? "في قائمة Vydrop" : "Queued in Vydrop")) node.textContent = arabic ? "في قائمة Vydrop" : "Queued in Vydrop";
      else for (const code of ["invalid", "missing", "ambiguous", "advertisement", "inspect", "busy", "unavailable", "preparing", "failed", "unsupported", "protected", "storage"])
        if (node.textContent === globalThis.ViFreeConnection.errorMessage({ code }, previousArabic)) { node.textContent = globalThis.ViFreeConnection.errorMessage({ code }, arabic); break; }
    }
    button.title = status.textContent || actionHint();
  }
  globalThis.ViFreeConnection?.watchLanguage?.(applyLanguage, () => Boolean(currentVideo) && host.style.display !== "none");
  const playing = video => video?.isConnected && !video.paused && !video.ended && video.readyState >= 2;
  const parent = node => node.parentElement || node.getRootNode?.()?.host;
  const mediaEvents = ["play", "playing", "loadstart", "loadeddata", "canplay", "pause", "ended", "loadedmetadata", "durationchange", "emptied"];
  const watchedRoots = new Map();
  function videosInPage() {
    const roots = new Set(Array.from(document.querySelectorAll("shreddit-player")).filter(player => player.getAttribute("comment-id") === null).slice(0, 100).map(player => player.shadowRoot).filter(Boolean));
    for (const [root, observer] of watchedRoots) if (!roots.has(root)) {
      observer.disconnect();
      for (const event of mediaEvents) root.removeEventListener(event, schedule, true);
      watchedRoots.delete(root);
    }
    for (const root of roots) if (!watchedRoots.has(root)) {
      const observer = new MutationObserver(schedule);
      observer.observe(root, { childList: true, subtree: true });
      for (const event of mediaEvents) root.addEventListener(event, schedule, true);
      watchedRoots.set(root, observer);
    }
    const videos = [document, ...roots].flatMap(root => Array.from(root.querySelectorAll("video,audio")).slice(0, 100)).slice(0, 100);
    if (/^https:\/\/w\.anaplayer\.online\/albaplayer\/[a-z0-9-]{1,160}\//u.test(location.href)) {
      const frame = Array.from(document.querySelectorAll("iframe")).find(node => node.id === "iframe");
      if (frame && globalThis.ViFreeVideoDetection.inspectVideoPage("", false, frame)?.reason === "mirror") videos.push(frame);
    }
    return videos;
  }
  function bounds(video, includeAdvertisement = false) {
    if (globalThis.ViFreeVideoDetection.isRealtimeVideo?.(video)) return null;
    if (!includeAdvertisement && video.closest('.ad-container,.dm-sdk-ima-slot')) return null;
    const audio = video.matches("audio");
    let surface = video;
    if (audio && (!video.controls || video.getBoundingClientRect().height < 24)) {
      const custom = video.closest('[data-audio-player],.audio-player,.audioPlayer,.audioplayer,.music-player,.player,.jp-audio,.mejs__container,.plyr--audio');
      if (custom?.querySelectorAll("audio,video").length === 1 && custom.querySelectorAll('button,[role="slider"],input[type="range"]').length)
        surface = custom;
    }
    const r = surface.getBoundingClientRect();
    if (r.width < 80 || r.height < (audio ? 24 : 60) || r.bottom <= 0 || r.top >= innerHeight || r.right <= 0 || r.left >= innerWidth) return null;
    const paint = getComputedStyle(surface);
    if (paint.display === "none" || paint.visibility === "hidden") return null;
    let visual = r;
    if (Number(paint.opacity) === 0) {
      const canvas = !audio && Array.from(video.parentElement?.querySelectorAll("canvas") ?? []).find(c => {
        const r = c.getBoundingClientRect(), s = getComputedStyle(c);
        return r.width >= 80 && r.height >= 60 && s.display !== "none" && s.visibility !== "hidden" && Number(s.opacity) > 0;
      });
      if (!canvas) return null;
      visual = canvas.getBoundingClientRect();
    }
    for (let p = parent(surface), depth = 0; p && p !== document.body && depth < 40; p = parent(p), depth++) {
      const s = getComputedStyle(p);
      if (s.display === "none" || s.visibility === "hidden" || Number(s.opacity) === 0) return null;
    }
    return visual;
  }
  function refresh() {
    scheduled = false;
    if (currentVideo && globalThis.ViFreeVideoDetection.isRealtimeVideo?.(currentVideo)) chooser?.close();
    if (document.hidden || document.pictureInPictureElement) { hide(); return; }
    const videos = videosInPage();
    // A paused content player can remain painted underneath an active ad.
    // Keep that ad from revealing the content action until playback returns.
    const advertisements = videos.filter(video => playing(video) && video.closest('.ad-container,.dm-sdk-ima-slot'))
      .map(video => bounds(video, true)).filter(Boolean);
    const candidates = videos.map(video => ({ video, rect: bounds(video) })).filter(item => item.rect &&
      !advertisements.some(ad => Math.min(ad.right, item.rect.right) > Math.max(ad.left, item.rect.left) &&
        Math.min(ad.bottom, item.rect.bottom) > Math.max(ad.top, item.rect.top)))
      .sort((a, b) => Number(playing(b.video)) - Number(playing(a.video)) || visibleArea(b.rect) - visibleArea(a.rect));
    // Visibility depends on the player being on screen, including its poster,
    // paused and ended states. Do not wait for media bytes or playback to start.
    const selected = (busy || drag) && candidates.find(item => item.video === (pendingVideo || currentVideo)) || candidates[0];
    const video = selected?.video, rect = selected?.rect;
    const fullscreen = document.fullscreenElement;
    if (!video || !rect || !video.isConnected || fullscreen?.tagName === "VIDEO" || fullscreen && !fullscreen.contains(video)) {
      hide(); return;
    }
    const dialog = Array.from(document.querySelectorAll('dialog:modal,[role="dialog"][aria-modal="true"]')).find(d => {
      const r = d.getBoundingClientRect();
      return r.width > 0 && r.height > 0 && !d.closest('[inert],[aria-hidden="true"]') &&
        d.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true });
    });
    if (dialog && !dialog.contains(video)) { hide(); return; }
    const audio = video.matches("audio");
    host.dataset.mediaKind = audio ? "audio" : "video";
    if (currentVideo !== video && !busy) { label.textContent = text.open; status.textContent = ""; button.title = actionHint(); button.setAttribute("aria-description", actionHint()); delete button.dataset.state; }
    currentVideo = video;
    const parent = fullscreen || document.documentElement;
    if (host.parentElement !== parent) parent.append(host);
    const inset = 10, width = Math.min(190, Math.min(rect.right, innerWidth) - Math.max(rect.left, 0) - inset * 2, innerWidth - 24);
    const playerArea = { left: Math.max(rect.left, 0) + inset, right: Math.min(rect.right, innerWidth) - inset - width,
      top: Math.max(rect.top, 0) + inset, bottom: Math.min(rect.bottom, innerHeight) - inset - (button.offsetHeight || 34) };
    if (width < 36 || playerArea.right < playerArea.left || !audio && playerArea.bottom < playerArea.top) { hide(); return; }
    placementArea = { left: 8, right: innerWidth - 8 - width, top: 8, bottom: innerHeight - 8 - (button.offsetHeight || 34) };
    const left = manualPosition ? placementArea.left + manualPosition.x * (placementArea.right - placementArea.left) : playerArea.right;
    const audioTop = rect.top - (button.offsetHeight || 34) - 8;
    const top = manualPosition ? placementArea.top + manualPosition.y * (placementArea.bottom - placementArea.top) :
      audio ? Math.max(placementArea.top, Math.min(placementArea.bottom, audioTop >= 8 ? audioTop : rect.bottom + 8)) : playerArea.top;
    host.dataset.placement = "inside"; host.dataset.compact = width < 160 ? "true" : "false";
    host.style.setProperty("left", left + "px", "important"); host.style.setProperty("top", top + "px", "important");
    host.style.setProperty("max-width", width + "px", "important"); host.style.setProperty("width", width + "px", "important");
    host.style.setProperty("display", "block", "important");
    host.style.setProperty("visibility", "visible", "important");
  }
  function hide() { finishDrag(null, true); host.style.setProperty("display", "none", "important"); }
  function moveTo(left, top, area = placementArea) {
    if (!area) return;
    const fraction = (value, min, max) => max > min ? Math.min(1, Math.max(0, (value - min) / (max - min))) : 0;
    manualPosition = { x: fraction(left, area.left, area.right), y: fraction(top, area.top, area.bottom) };
    schedule();
  }
  function finishDrag(event, cancel = false) {
    if (!drag || event && event.pointerId !== drag.id) return;
    const previous = drag; drag = null;
    if (cancel) manualPosition = previous.before;
    suppressClick = previous.moved && !cancel;
    delete button.dataset.dragging;
    if (button.hasPointerCapture?.(previous.id)) button.releasePointerCapture(previous.id);
    schedule();
  }
  button.addEventListener("pointerdown", event => {
    if (!event.isTrusted || event.button !== 0 || busy || drag || !placementArea || !bounds(currentVideo)) return;
    suppressClick = false;
    const rect = host.getBoundingClientRect();
    drag = { id: event.pointerId, x: event.clientX, y: event.clientY, left: rect.left, top: rect.top,
      area: { ...placementArea }, before: manualPosition, moved: false };
    button.setPointerCapture(event.pointerId);
    event.stopPropagation();
  });
  button.addEventListener("pointermove", event => {
    if (!drag || event.pointerId !== drag.id) return;
    const x = event.clientX - drag.x, y = event.clientY - drag.y;
    if (!drag.moved && Math.hypot(x, y) < 5) return;
    drag.moved = true; button.dataset.dragging = "true";
    moveTo(drag.left + x, drag.top + y, drag.area);
    event.preventDefault(); event.stopPropagation();
  });
  button.addEventListener("pointerup", event => finishDrag(event));
  button.addEventListener("pointercancel", event => finishDrag(event, true));
  button.addEventListener("lostpointercapture", event => finishDrag(event, true));
  addEventListener("blur", () => finishDrag(null, true));
  button.addEventListener("keydown", event => {
    if (!event.isTrusted || !event.shiftKey || event.altKey || event.ctrlKey || event.metaKey || busy || !placementArea) return;
    const steps = { ArrowLeft: [-16, 0], ArrowRight: [16, 0], ArrowUp: [0, -16], ArrowDown: [0, 16] };
    if (event.key === "Home") { manualPosition = undefined; schedule(); }
    else if (steps[event.key]) { const rect = host.getBoundingClientRect(); moveTo(rect.left + steps[event.key][0], rect.top + steps[event.key][1]); }
    else return;
    event.preventDefault(); event.stopPropagation();
  });
  function visibleArea(r) {
    return Math.max(0, Math.min(r.right, innerWidth) - Math.max(r.left, 0)) * Math.max(0, Math.min(r.bottom, innerHeight) - Math.max(r.top, 0));
  }
  function schedule() {
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(refresh);
  }
  button.addEventListener("click", async event => {
    if (suppressClick && event.detail > 0) { suppressClick = false; event.preventDefault(); event.stopPropagation(); return; }
    if (!event.isTrusted || busy || !currentVideo) return;
    if (globalThis.ViFreeVideoDetection.isRealtimeVideo?.(currentVideo)) { refresh(); return; }
    event.preventDefault(); event.stopPropagation();
    busy = true; pendingVideo = currentVideo;
    button.disabled = true; button.setAttribute("aria-busy", "true");
    clearTimeout(feedbackTimer); status.textContent = ""; label.textContent = text.finding;
    delete button.dataset.state; button.title = actionHint();
    try {
      const source = pendingVideo.currentSrc || pendingVideo.src;
      let found;
      try { found = await globalThis.ViFreeVideoDetection.inspectVideoPage(source, true, pendingVideo); }
      catch { throw Object.assign(new Error("Page inspection failed."), { code: "inspect" }); }
      if (!found?.url) throw Object.assign(new Error("No unambiguous video link."), { code: ["ambiguous", "advertisement"].includes(found?.reason) ? found.reason : "missing" });
      if (globalThis.VydropQualityPicker) {
        label.textContent = text.open;

        chooser = globalThis.VydropQualityPicker.open({ anchor: button, arabic, audioOnly: pendingVideo.matches("audio"),
          playlistAvailable: Boolean(globalThis.ViFreeVideoDetection.playlistFor?.(found.url, location.href)),
          onClose: () => { chooser = null; if (!button.dataset.state) label.textContent = text.open; schedule(); },
          onInspectionFailure: error => {
            if (!["unavailable", "preparing", "unsupported", "busy", "storage"].includes(error?.code)) return;
            label.textContent = text.open;
            status.textContent = globalThis.ViFreeConnection.errorMessage(error, arabic);
            button.title = status.textContent;
            button.dataset.state = "error";
            schedule();
          },
          onResult: result => {
            label.textContent = result?.code === "queued" ? (arabic ? "في قائمة Vydrop" : "Queued in Vydrop") : text.opened;
            button.dataset.state = "success";
            clearTimeout(feedbackTimer);
            feedbackTimer = setTimeout(() => { label.textContent = text.open; delete button.dataset.state; schedule(); }, 4500);
          },
          preview: { title: document.title, thumbnail: pendingVideo?.poster, source: location.hostname },
          inspect: async () => {
            const response = await api.runtime.sendMessage({ version: 1, type: "vydrop-inspect-video", url: found.url, linkedinAsset: found.linkedinAsset, sourcePage: found.sourcePage, mirrorPage: found.mirrorPage, mediaId: found.mediaId });
            if (!response?.ok) throw Object.assign(new Error("inspect"), { code: response?.code || "inspect" });
            return response.media;
          },
          send: async (output, scope) => {
            const response = await api.runtime.sendMessage({ version: 1, type: "vifree-open-video", url: found.url, linkedinAsset: found.linkedinAsset, sourcePage: found.sourcePage, mirrorPage: found.mirrorPage, mediaId: found.mediaId, output, playlist: scope === "playlist" });
            if (!response?.ok) throw Object.assign(new Error("open"), { code: response?.code || "failed" });
            return response;
          }
        });
        return;
      }
      let copied = false;
      try { await navigator.clipboard.writeText(found.url); copied = true; } catch { /* Opening the app remains useful when clipboard access is unavailable. */ }
      label.textContent = text.opening;
      const response = await api.runtime.sendMessage({ version: 1, type: "vifree-open-video", url: found.url, linkedinAsset: found.linkedinAsset, sourcePage: found.sourcePage, mirrorPage: found.mirrorPage, mediaId: found.mediaId });
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
        feedbackTimer = setTimeout(() => { status.textContent = ""; label.textContent = text.open; button.title = actionHint(); delete button.dataset.state; schedule(); }, 4500);
      schedule();
    }
  });
  const observer = new MutationObserver(records => {
    if (records.some(record => record.target !== host && !host.contains(record.target) &&
      (record.type !== "attributes" || record.attributeName !== "class" || currentVideo &&
        (record.target === currentVideo || record.target.contains(currentVideo) ||
          currentVideo.getRootNode?.()?.host && record.target.contains(currentVideo.getRootNode().host))))) schedule();
  });
  observer.observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ["class", "data-live", "data-is-live"] });
  for (const event of [...mediaEvents, "fullscreenchange", "visibilitychange"])
    document.addEventListener(event, schedule, true);
  addEventListener("scroll", schedule, { passive: true, capture: true });
  addEventListener("resize", schedule, { passive: true });
  button.addEventListener("blur", schedule);
  if (globalThis.customElements?.whenDefined) void globalThis.customElements.whenDefined("shreddit-player").then(schedule);
  schedule();
})();
