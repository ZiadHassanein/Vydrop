(() => {
  let active;
  globalThis.VydropQualityPicker = {
    open({ anchor, arabic = false, inspect, send, embedded = false }) {
      active?.();
      const t = arabic ? {
        heading: "تنزيل بواسطة Vydrop", loading: "جارٍ فحص الصيغ المتاحة…", video: "فيديو", audio: "صوت", format: "الصيغة", quality: "الجودة", open: "فتح في Vydrop", close: "إغلاق", automatic: "تلقائي · بجودة المصدر", size: "الحجم غير متاح قبل التنزيل", ready: "اختر الصيغة ثم راجع التنزيل داخل التطبيق.", legacy: "تم الفتح. اختر الجودة داخل إصدار Vydrop الأقدم.", success: "تم الفتح في Vydrop. راجع التنزيل داخل التطبيق.", fallback: "يمكنك اختيار الجودة داخل التطبيق.", unavailable: "تعذّر جلب الصيغ. افتح الفيديو داخل Vydrop أو حاول مجددًا.", retry: "حاول مجددًا"
      } : {
        heading: "Download by Vydrop", loading: "Checking available formats…", video: "Video", audio: "Audio", format: "Format", quality: "Quality", open: "Open in Vydrop", close: "Close", automatic: "Automatic · source quality", size: "Size unavailable before downloading", ready: "Choose an output, then review it in the app.", legacy: "Opened. Choose the quality in your older Vydrop app.", success: "Opened in Vydrop. Review the download in the app.", fallback: "You can choose the quality inside the app.", unavailable: "Could not load formats. Open this video in Vydrop or try again.", retry: "Try again"
      };
      const previousHeight = document.body.style.minHeight;
      if (embedded) document.body.style.minHeight = "570px";
      const host = document.createElement("div"); host.id = "vydrop-quality-picker";
      host.style.cssText = "all:initial!important;position:fixed!important;inset:0!important;z-index:2147483647!important;pointer-events:none!important";
      const shadow = host.attachShadow({ mode: "open" });
      const style = document.createElement("style");
      style.textContent = `:host{font-family:system-ui,sans-serif;color-scheme:light dark}*{box-sizing:border-box}.panel{pointer-events:auto;position:absolute;width:min(348px,calc(100vw - 24px));max-height:calc(100vh - 24px);overflow:auto;background:#fcfafc;color:#2a2029;border:1px solid #c9b2c2;border-radius:10px;box-shadow:0 16px 48px #170d2040;font:400 13px/1.5 system-ui,sans-serif}.top{display:flex;align-items:center;gap:12px;background:#3b1b2c;color:#fff4fa;padding:14px 16px}.mark{color:#ff7797;font-size:23px;font-weight:900}.top strong{flex:1}.close{background:none;color:inherit;border:0;font-size:22px;padding:2px 7px;min-width:32px}.body{padding:18px}.identity{display:grid;grid-template-columns:64px 1fr;gap:12px;align-items:start;margin-bottom:16px}.identity img{width:64px;height:52px;object-fit:cover;border-radius:4px}.identity.no-image{grid-template-columns:1fr}.wordmark{width:104px;height:34px;object-fit:contain;flex:none}.source{color:#715f6d;font-size:12px;overflow-wrap:anywhere}.title{font-size:17px;font-weight:650;line-height:1.4;margin:0 0 16px;overflow-wrap:anywhere;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}.tabs{display:flex;border-bottom:1px solid #e5d9e1;margin-bottom:18px;gap:8px}.tabs button{flex:1;border:0;border-bottom:2px solid transparent;border-radius:0;background:none}.tabs button[aria-pressed=true]{border-color:#c72d49;color:#a91f3b;font-weight:700}button,select{font:inherit;color:inherit;min-height:38px;border:1px solid #c9b2c2;border-radius:5px;background:transparent;padding:8px 12px;cursor:pointer}button:hover{border-color:#a91f3b}button:focus-visible,select:focus-visible{outline:2px solid #a91f3b;outline-offset:3px}button:disabled{opacity:.55;cursor:default}label{display:block;font-weight:650;margin-bottom:7px}select{width:100%;background:#fff}.choices{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin:8px 0 16px}.choices button[aria-pressed=true]{background:#f8e8ee;border-color:#a91f3b;color:#a91f3b;font-weight:700}.muted{color:#715f6d;font-size:12px;margin:12px 0}.launch{background:#c72d49;color:white;border-color:#c72d49;width:100%;font-weight:650;clip-path:polygon(12px 0,100% 0,100% 100%,0 100%,0 12px);min-height:44px}.launch:focus-visible{clip-path:none}.status{margin:0 0 14px;overflow-wrap:anywhere}.status[data-error=true]{color:#a91f3b}.retry{margin-bottom:14px}.hidden{display:none!important}@media(prefers-color-scheme:dark){.panel{background:#261e2a;color:#faf3f7;border-color:#624759}.tabs{border-color:#443448}.tabs button[aria-pressed=true],.status[data-error=true]{color:#ff9eb4}.choices button[aria-pressed=true]{background:#442638;color:#ff9eb4;border-color:#f17b96}select{background:#2b242f}.muted,.source{color:#ccbac7}button:focus-visible,select:focus-visible{outline-color:#f17b96}}@media(forced-colors:active){.panel,.top,select{background:Canvas;color:CanvasText;border-color:ButtonText}.launch,.choices button[aria-pressed=true]{background:Highlight;color:HighlightText;clip-path:none}.muted,.status[data-error=true]{color:CanvasText}}`;
      shadow.append(style);
      function el(tag, className, value) { const node = document.createElement(tag); if (className) node.className = className; if (value) node.textContent = value; return node; }
      function button(label, className, action) { const node = el("button", className, label); node.type = "button"; node.addEventListener("click", action); return node; }
      const panel = el("section", "panel"); panel.dir = arabic ? "rtl" : "ltr"; panel.setAttribute("role", "dialog"); panel.setAttribute("aria-label", t.heading);
      const logo = document.createElementNS("http://www.w3.org/2000/svg", "svg"); logo.setAttribute("viewBox", "0 0 64 64"); logo.setAttribute("width", "24"); logo.setAttribute("height", "24"); logo.setAttribute("aria-hidden", "true");
      const logoPath = document.createElementNS(logo.namespaceURI, "path"); logoPath.setAttribute("d", "M4,8.016L21.625,8.016 27.873,18.056 27.873,27.426 21.179,27.426 32,39.474 42.821,27.426 36.127,27.426 36.127,18.056 42.375,8.016 60,8.016 60,11.586 32,55.984 4,11.586Z"); logoPath.setAttribute("fill", "#ff7797");
      const logoArrow = document.createElementNS(logo.namespaceURI, "path"); logoArrow.setAttribute("d", "M27.873,18.056 L36.127,18.056 36.127,27.426 42.821,27.426 32,39.474 21.179,27.426 27.873,27.426Z"); logoArrow.setAttribute("fill", "white"); logo.append(logoPath, logoArrow);
      const wordmarkUrl = globalThis.ViFreeConnection?.api?.runtime?.getURL?.("brand-logo-dark.svg");
      const wordmark = wordmarkUrl ? el("img", "wordmark") : logo;
      if (wordmarkUrl) { wordmark.src = wordmarkUrl; wordmark.alt = "Vydrop"; wordmark.addEventListener("error", () => wordmark.replaceWith(logo), { once: true }); }
      const top = el("div", "top"); top.append(wordmark, el("strong", "", t.heading), button("×", "close", close)); top.lastChild.setAttribute("aria-label", t.close);
      const body = el("div", "body"), title = el("h2", "title"), status = el("p", "status", t.loading); status.setAttribute("role", "status");
      const options = el("div", "hidden"), tabs = el("div", "tabs"), formatLabel = el("label", "", t.format), format = el("select"), qualityLabel = el("label", "", t.quality), choices = el("div", "choices");
      format.id = "vydrop-format"; formatLabel.htmlFor = format.id;
      const video = button(t.video, "", () => chooseKind("video")), audio = button(t.audio, "", () => chooseKind("audio")); tabs.append(video, audio);
      options.append(tabs, formatLabel, format, qualityLabel, choices, el("p", "muted", t.size));
      const retry = button(t.retry, "retry hidden", load), launch = button(t.open, "launch", openApp); launch.disabled = true;
      const identity = el("div", "identity no-image"), thumbnail = el("img"), mediaText = el("div"), source = el("div", "source");
      thumbnail.alt = ""; thumbnail.referrerPolicy = "no-referrer"; thumbnail.crossOrigin = "anonymous"; thumbnail.hidden = true;
      thumbnail.addEventListener("error", () => { thumbnail.hidden = true; identity.classList.add("no-image"); });
      mediaText.append(title, source); identity.append(thumbnail, mediaText);
      body.append(identity, status, options, retry, launch); panel.append(top, body); shadow.append(panel);
      const parent = document.fullscreenElement || document.documentElement; parent.append(host);
      let media, kind = "video", selectedHeight = 0, disposed = false, loading = false;
      function position() { const rect = anchor?.getBoundingClientRect(); const x = rect ? Math.min(innerWidth - panel.offsetWidth - 12, Math.max(12, rect.right - panel.offsetWidth)) : 12; panel.style.left = `${x}px`; panel.style.top = `${rect ? Math.min(Math.max(12, rect.bottom + 8), Math.max(12, innerHeight - panel.offsetHeight - 12)) : 12}px`; }
      function close() { if (embedded) document.body.style.minHeight = previousHeight; disposed = true; host.remove(); document.removeEventListener("pointerdown", outside, true); document.removeEventListener("keydown", keys, true); removeEventListener("resize", position); removeEventListener("scroll", position, true); anchor?.focus(); if (active === close) active = null; }
      function outside(event) { if (!event.composedPath().includes(host) && !event.composedPath().includes(anchor)) close(); }
      function keys(event) { if (event.key === "Escape") { close(); event.stopPropagation(); event.preventDefault(); } if (event.key === "Tab") { const all = [...shadow.querySelectorAll("button:not(:disabled),select")].filter(n => n.getClientRects().length); const current = shadow.activeElement; if (event.shiftKey && current === all[0]) { all.at(-1).focus(); event.preventDefault(); } else if (!event.shiftKey && current === all.at(-1)) { all[0].focus(); event.preventDefault(); } } }
      document.addEventListener("pointerdown", outside, true); document.addEventListener("keydown", keys, true); addEventListener("resize", position); addEventListener("scroll", position, true); active = close;
      function chooseKind(value) { kind = value; renderFormats(); }
      function renderFormats() {
        video.disabled = !media.videos.length; audio.disabled = !media.audio.length;
        video.setAttribute("aria-pressed", String(kind === "video")); audio.setAttribute("aria-pressed", String(kind === "audio"));
        format.replaceChildren(); for (const value of kind === "video" ? media.videos.map(v => v.format) : media.audio) { const option = el("option", "", value.toUpperCase()); option.value = value; format.append(option); }
        renderQualities();
      }
      function renderQualities() {
        choices.replaceChildren(); const heights = kind === "video" ? media.videos.find(v => v.format === format.value)?.heights || [] : [0];
        selectedHeight = heights.find(h => h <= 1080) ?? heights[0] ?? 0;
        for (const height of heights) { const choice = button(height ? `${height}p` : t.automatic, "", () => { selectedHeight = height; for (const node of choices.children) node.setAttribute("aria-pressed", String(node === choice)); }); choice.setAttribute("aria-pressed", String(height === selectedHeight)); choices.append(choice); }
        launch.disabled = !format.value || !heights.length; position();
      }
      format.addEventListener("change", renderQualities);
      async function load() {
        if (loading) return; loading = true; launch.disabled = true; options.classList.add("hidden"); retry.classList.add("hidden"); status.textContent = t.loading; status.dataset.error = "false"; media = null;
        try {
          const data = await inspect(); if (disposed) return;
          if (!data || !Array.isArray(data.videos) || !Array.isArray(data.audio)) throw new Error("formats");
          media = { title: typeof data.title === "string" ? data.title.slice(0, 600) : "Video", videos: data.videos.filter(v => ["mp4", "mkv", "webm"].includes(v.format) && Array.isArray(v.heights)).map(v => ({ format: v.format, heights: [...new Set(v.heights.filter(h => Number.isInteger(h) && h > 0 && h <= 8640))].slice(0, 24) })).filter(v => v.heights.length), audio: data.audio.filter(f => ["mp3", "m4a", "opus", "wav", "flac"].includes(f)) };
          if (!media.videos.length && !media.audio.length) throw new Error("formats");
          title.textContent = media.title;
          source.textContent = typeof data.source === "string" ? data.source.slice(0, 200) : "";
          try { const url = new URL(data.thumbnail); if (url.protocol === "https:" && !url.username && !url.password && !url.port && !/^(localhost|127\.|0\.|10\.|192\.168\.|169\.254\.|172\.(1[6-9]|2\d|3[01])\.|\[)/iu.test(url.hostname)) { thumbnail.hidden = false; thumbnail.src = url.href; identity.classList.remove("no-image"); } } catch { /* A text-only source is an honest fallback. */ }
          kind = media.videos.length ? "video" : "audio"; options.classList.remove("hidden"); status.textContent = t.ready; renderFormats();
        } catch (error) { if (!disposed) { media = null; status.textContent = error?.code === "unsupported" ? t.fallback : t.unavailable; status.dataset.error = "true"; retry.classList.remove("hidden"); launch.disabled = false; } }
        finally { loading = false; if (!disposed) position(); }
      }
      async function openApp() {
        launch.disabled = true;
        try { const result = await send(media ? { kind, format: format.value, height: kind === "video" ? selectedHeight : 0 } : undefined); if (disposed) return; status.dataset.error = "false"; status.textContent = result?.code === "opened-legacy" ? t.legacy : t.success; }
        catch (error) { if (!disposed) { status.dataset.error = "true"; status.textContent = globalThis.ViFreeConnection.errorMessage(error, arabic); } }
        finally { if (!disposed) { launch.disabled = false; position(); } }
      }
      position(); top.lastChild.focus(); void load(); return { close };
    }
  };
})();
