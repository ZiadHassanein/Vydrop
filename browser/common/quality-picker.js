(() => {
  let active;
  globalThis.VydropQualityPicker = {
    open({ anchor, arabic = false, inspect, send, embedded = false, onClose, onResult, onInspectionFailure, preview, playlistAvailable = false }) {
      active?.();
      const releaseReload = globalThis.ViFreeConnection?.holdReload?.();
      function translations(arabic) {
      const t = arabic ? {
        heading: "تنزيل بواسطة Vydrop", loading: "جارٍ فحص الصيغ المتاحة…", video: "فيديو", audio: "صوت", format: "الصيغة", quality: "الجودة", download: "تنزيل", queued: "أُضيف إلى قائمة تنزيل Vydrop.", open: "فتح في Vydrop", close: "إغلاق", automatic: "تلقائي · بجودة المصدر", size: "الحجم غير متاح قبل التنزيل", ready: "الصيغ جاهزة", hint: "يستخدم مجلد التنزيل المحفوظ في Vydrop.", legacy: "تم الفتح في إصدار Vydrop الأقدم. راجع التنزيل وابدأه هناك.", success: "أُرسل التنزيل إلى Vydrop.", fallback: "يمكنك اختيار الجودة داخل التطبيق.", unavailable: "تعذّر جلب الصيغ. افتح الفيديو داخل Vydrop أو حاول مجددًا.", retry: "حاول مجددًا"
      } : {
        heading: "Download by Vydrop", loading: "Checking available formats…", video: "Video", audio: "Audio", format: "Format", quality: "Quality", download: "Download", queued: "Added to the Vydrop queue.", open: "Open in Vydrop", close: "Close", automatic: "Automatic · source quality", size: "Size unavailable before downloading", ready: "Formats ready", hint: "Uses your saved Vydrop download folder.", legacy: "Opened in older Vydrop. Review and start it there.", success: "Download sent to Vydrop.", fallback: "You can choose the quality inside the app.", unavailable: "Could not load formats. Open this video in Vydrop or try again.", retry: "Try again"
      };
      Object.assign(t, arabic ? { scope: "نطاق التنزيل", single: "هذا الفيديو", playlist: "قائمة التشغيل كاملة", playlistDownload: "تنزيل قائمة التشغيل", playlistHint: "ينزّل الفيديوهات المتاحة بهذه الإعدادات.", upTo: "حتى " } :
        { scope: "Download scope", single: "This video", playlist: "Whole playlist", playlistDownload: "Download playlist", playlistHint: "Downloads available videos using these settings.", upTo: "Up to " });
      return t;
      }
      let t = translations(arabic);
      const previousHeight = document.body.style.minHeight;
      if (embedded) document.body.style.minHeight = "520px";
      const host = document.createElement("div"); host.id = "vydrop-quality-picker";
      host.style.cssText = "all:initial!important;position:fixed!important;inset:0!important;z-index:2147483647!important;pointer-events:none!important";
      const shadow = host.attachShadow({ mode: "open" });
      const style = document.createElement("style");
      style.textContent = `
:host{font-family:system-ui,sans-serif;color-scheme:light dark}
*{box-sizing:border-box}::selection{background:#753149;color:#fff4fa}
.panel{--page:#fcfafc;--ink:#2a2029;--muted:#715f6d;--field:#fff;--border:#c9b2c2;--divider:#e5d9e1;--accent:#a91f3b;--selected:#f8e8ee;pointer-events:auto;position:absolute;width:min(416px,calc(100vw - 24px));max-height:calc(100vh - 24px);overflow:auto;scrollbar-width:thin;scrollbar-color:var(--border) var(--page);background:var(--page);color:var(--ink);border-radius:10px;box-shadow:0 18px 48px #170d2040;font:400 14px/1.5 system-ui,sans-serif;font-variant-numeric:tabular-nums}
.top{display:flex;align-items:center;gap:12px;background:#3b1b2c;color:#fff4fa;padding:13px 20px;min-height:64px}.wordmark{width:128px;height:34px;object-fit:contain;flex:none}.top-status{flex:1;text-align:end;font-size:12px;color:#ddc2d2}.top-status[data-ready=true]{color:#fff4fa}.close{display:grid;place-items:center;background:none;color:inherit;border:0;padding:5px;min-width:32px;min-height:32px}.body{padding:22px}
.identity{display:grid;grid-template-columns:116px minmax(0,1fr);gap:16px;align-items:center;margin-bottom:22px}.identity img{width:116px;aspect-ratio:16/10;object-fit:cover;border-radius:5px}.identity.no-image{grid-template-columns:1fr}.source{color:var(--muted);font-size:12px;overflow-wrap:anywhere;margin-top:7px}.title{font-size:18px;font-weight:650;line-height:1.4;margin:0;overflow-wrap:anywhere;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}
button{font:inherit;color:inherit;min-height:42px;border:1px solid var(--border);border-radius:5px;background:transparent;padding:9px 12px;cursor:pointer}button:hover{border-color:var(--accent)}button:focus-visible{outline:2px solid var(--accent);outline-offset:3px}button:disabled{opacity:.55;cursor:default}
.tabs{display:flex;border:1px solid var(--border);border-radius:6px;padding:3px;margin-bottom:22px;gap:3px}.tabs button{flex:1;border:0;border-radius:3px;background:none;display:flex;align-items:center;justify-content:center;gap:8px;min-height:39px;padding:7px}.tabs button[aria-pressed=true],.tabs button[aria-checked=true]{background:var(--selected);color:var(--accent);font-weight:650}
label{display:block;font-weight:650;margin:0 0 9px}.formats,.choices{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:9px;margin-bottom:18px}.formats button,.choices button{background:var(--field);padding:12px;min-height:49px;display:flex;align-items:center;gap:10px;font-weight:600;text-align:start;min-width:0}.formats button::before,.choices button::before{content:"";flex:none;width:17px;height:17px;border:1.5px solid var(--border);border-radius:50%;background:var(--field)}.formats button[aria-checked=true],.choices button[aria-checked=true]{background:var(--selected);border-color:var(--accent);color:var(--accent)}.formats button[aria-checked=true]::before,.choices button[aria-checked=true]::before{border:5px solid var(--accent);box-shadow:inset 0 0 0 2px var(--field)}.choices button{min-height:53px}.choices small{font-size:10px;font-weight:500;color:var(--muted);margin-inline-start:auto}
.summary{border-top:1px solid var(--divider);padding-top:15px;margin-top:2px}.selection{font-size:14px;font-weight:650}.muted{color:var(--muted);font-size:12px;margin:4px 0 0}.launch{background:#c72d49;color:#fff;border-color:#c72d49;width:100%;font-weight:650;clip-path:polygon(12px 0,100% 0,100% 100%,0 100%,0 12px);min-height:48px;display:flex;align-items:center;justify-content:center;gap:10px;margin-top:18px}.launch:focus-visible{clip-path:none}.hint{text-align:center;margin-top:11px}.status{margin:0 0 14px;overflow-wrap:anywhere;font-size:13px}.status:empty{display:none}.status[data-error=true]{color:var(--accent)}.retry{margin-bottom:14px}.hidden{display:none!important}svg{flex:none}
@media(max-width:420px){.body{padding:18px}.top{padding:12px 18px}.identity{grid-template-columns:100px minmax(0,1fr);gap:12px}.identity img{width:100px}.title{font-size:17px}.formats button,.choices button{padding:11px}.top-status{font-size:11px}}
@media(prefers-color-scheme:dark){.panel{--page:#261e2a;--ink:#faf3f7;--muted:#ccbac7;--field:#2b242f;--border:#795c70;--divider:#443448;--accent:#f17b96;--selected:#442638}}
.top{padding:9px 16px;min-height:50px}.wordmark{width:112px;height:30px}.body{padding:14px 16px}.identity{grid-template-columns:88px minmax(0,1fr);gap:12px;margin-bottom:12px}.identity img{width:88px;height:54px;aspect-ratio:auto}.title{font-size:16px;line-height:1.35}.source{font-size:11px;margin-top:4px}.tabs{margin-bottom:13px;padding:2px}.tabs button{min-height:33px;padding:5px;font-size:13px}.formats,.choices{gap:6px;margin-bottom:13px}.formats{grid-template-columns:repeat(3,minmax(0,1fr))}.formats[data-count="1"]{grid-template-columns:1fr}.formats[data-count="2"]{grid-template-columns:repeat(2,minmax(0,1fr))}.choices{grid-template-columns:repeat(auto-fit,minmax(60px,1fr))}.choices.audio{grid-template-columns:1fr}.formats button,.choices button{min-height:35px;padding:6px;gap:6px;font-size:13px;justify-content:center}.formats button::before,.choices button::before{width:14px;height:14px}.formats button[aria-checked=true]::before,.choices button[aria-checked=true]::before{border-width:4px}.choices small{display:none}label{font-size:12px;margin-bottom:6px}.summary{padding-top:10px}.selection{font-size:13px}.muted{font-size:11px}.launch{min-height:42px;margin-top:12px}.hint{margin-top:7px}.status{font-size:12px;margin-bottom:10px}.top-status{font-size:11px}
@media(forced-colors:active){.panel{--page:Canvas;--ink:CanvasText;--muted:CanvasText;--field:Canvas;--border:ButtonText;--divider:ButtonText;--accent:Highlight;--selected:Canvas;border:1px solid ButtonText}.top{background:Canvas;color:CanvasText}.top-status{color:CanvasText}.launch{forced-color-adjust:none;background:Highlight;color:HighlightText;clip-path:none}.formats button[aria-checked=true],.choices button[aria-checked=true]{outline:2px solid Highlight}.wordmark{background:#3b1b2c}}
      `;
      shadow.append(style);
      function el(tag, className, value) { const node = document.createElement(tag); if (className) node.className = className; if (value) node.textContent = value; return node; }
      function button(label, className, action) { const node = el("button", className, label); node.type = "button"; node.addEventListener("click", action); return node; }
      function icon(data) { const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg"), path = document.createElementNS(svg.namespaceURI, "path"); svg.setAttribute("viewBox", "0 0 24 24"); svg.setAttribute("width", "18"); svg.setAttribute("height", "18"); svg.setAttribute("aria-hidden", "true"); path.setAttribute("d", data); path.setAttribute("fill", "none"); path.setAttribute("stroke", "currentColor"); path.setAttribute("stroke-width", "1.7"); path.setAttribute("stroke-linecap", "round"); path.setAttribute("stroke-linejoin", "round"); svg.append(path); return svg; }
      function radioGroup(node, label) { node.setAttribute("role", "radiogroup"); node.setAttribute("aria-label", label); node.addEventListener("keydown", event => { if (!["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End"].includes(event.key)) return; const items = [...node.children], index = items.indexOf(shadow.activeElement); if (index < 0) return; const next = event.key === "Home" ? 0 : event.key === "End" ? items.length - 1 : (index + (event.key === "ArrowDown" || event.key === (arabic ? "ArrowLeft" : "ArrowRight") ? 1 : -1) + items.length) % items.length; items[next].click(); items[next].focus(); event.preventDefault(); }); }
      function selectRadio(group, selected) { for (const node of group.children) { node.setAttribute("aria-checked", String(node === selected)); node.tabIndex = node === selected ? 0 : -1; } }
      const panel = el("section", "panel"); panel.dir = arabic ? "rtl" : "ltr"; panel.setAttribute("role", "dialog"); panel.setAttribute("aria-label", t.heading);
      const logo = document.createElementNS("http://www.w3.org/2000/svg", "svg"); logo.setAttribute("viewBox", "0 0 64 64"); logo.setAttribute("width", "24"); logo.setAttribute("height", "24"); logo.setAttribute("aria-hidden", "true");
      const logoPath = document.createElementNS(logo.namespaceURI, "path"); logoPath.setAttribute("d", "M4,8.016L21.625,8.016 27.873,18.056 27.873,27.426 21.179,27.426 32,39.474 42.821,27.426 36.127,27.426 36.127,18.056 42.375,8.016 60,8.016 60,11.586 32,55.984 4,11.586Z"); logoPath.setAttribute("fill", "#ff7797");
      const logoArrow = document.createElementNS(logo.namespaceURI, "path"); logoArrow.setAttribute("d", "M27.873,18.056 L36.127,18.056 36.127,27.426 42.821,27.426 32,39.474 21.179,27.426 27.873,27.426Z"); logoArrow.setAttribute("fill", "white"); logo.append(logoPath, logoArrow);
      const wordmarkUrl = globalThis.ViFreeConnection?.api?.runtime?.getURL?.("brand-logo-dark.svg");
      const wordmark = wordmarkUrl ? el("img", "wordmark") : logo;
      if (wordmarkUrl) { wordmark.src = wordmarkUrl; wordmark.alt = "Vydrop"; wordmark.addEventListener("error", () => wordmark.replaceWith(logo), { once: true }); }
      const topStatus = el("span", "top-status"), closeButton = button("", "close", close); closeButton.append(icon("M6 6L18 18M18 6L6 18")); closeButton.setAttribute("aria-label", t.close); const top = el("div", "top"); top.append(wordmark, topStatus, closeButton);
      const body = el("div", "body"), title = el("h2", "title"), status = el("p", "status", t.loading); status.setAttribute("role", "status");
      const scopePicker = el("div", playlistAvailable ? "tabs" : "hidden"); radioGroup(scopePicker, t.scope);
      const singleScope = button(t.single, "", () => chooseScope("video")), playlistScope = button(t.playlist, "", () => chooseScope("playlist"));
      for (const option of [singleScope, playlistScope]) { option.setAttribute("role", "radio"); scopePicker.append(option); }
      selectRadio(scopePicker, singleScope);
      const playlistHint = el("p", "muted hidden", t.playlistHint);
      const options = el("div", "hidden"), tabs = el("div", "tabs"), formatLabel = el("label", "", t.format), format = el("div", "formats"), qualityLabel = el("label", "", t.quality), choices = el("div", "choices");
      format.id = "vydrop-format"; radioGroup(format, t.format); radioGroup(choices, t.quality);
      const video = button(t.video, "", () => chooseKind("video")), audio = button(t.audio, "", () => chooseKind("audio")); video.prepend(icon("M3 5H16V19H3ZM16 9L22 6V18L16 15")); audio.prepend(icon("M3 10V14M7 6V18M12 3V21M17 7V17M21 10V14")); tabs.append(video, audio);
      const summary = el("div", "summary"), selection = el("div", "selection"), sizeHint = el("p", "muted", t.size); summary.append(selection, sizeHint); options.append(tabs, formatLabel, format, qualityLabel, choices, summary);
      const retry = button(t.retry, "retry hidden", load), launch = button(t.open, "launch", event => { if (event.isTrusted) void openApp(); }); launch.prepend(icon("M12 3V15M7 10L12 15L17 10M3 15V21H21V15")); launch.disabled = true;
      const identity = el("div", "identity no-image"), thumbnail = el("img"), mediaText = el("div"), source = el("div", "source");
      thumbnail.alt = ""; thumbnail.referrerPolicy = "no-referrer"; thumbnail.crossOrigin = "anonymous"; thumbnail.hidden = true;
      thumbnail.addEventListener("error", () => { thumbnail.hidden = true; identity.classList.add("no-image"); });
      mediaText.append(title, source); identity.append(thumbnail, mediaText);
      const folderHint = el("p", "muted hint", t.hint);
      body.append(identity, scopePicker, playlistHint, status, options, retry, launch, folderHint); panel.append(top, body); shadow.append(panel);
      const parent = document.fullscreenElement || document.documentElement; parent.append(host);
      let media, kind = "video", scope = "video", selectedHeight = 0, disposed = false, loading = false, stopLanguage;
      function applyLanguage(language) {
        if (disposed || arabic === (language === "ar")) return;
        const previous = t, previousArabic = arabic; arabic = language === "ar"; t = translations(arabic);
        panel.dir = arabic ? "rtl" : "ltr"; panel.lang = language; panel.setAttribute("aria-label", t.heading); closeButton.setAttribute("aria-label", t.close);
        for (const node of [status, topStatus]) {
          const key = Object.keys(previous).find(key => previous[key] === node.textContent);
          if (key) node.textContent = t[key];
          else for (const code of ["invalid", "missing", "ambiguous", "advertisement", "inspect", "busy", "unavailable", "preparing", "failed", "unsupported", "protected", "storage"])
            if (node.textContent === globalThis.ViFreeConnection?.errorMessage({ code }, previousArabic)) { node.textContent = globalThis.ViFreeConnection.errorMessage({ code }, arabic); break; }
        }
        singleScope.textContent = t.single; playlistScope.textContent = t.playlist; playlistHint.textContent = t.playlistHint;
        scopePicker.setAttribute("aria-label", t.scope); format.setAttribute("aria-label", t.format); choices.setAttribute("aria-label", t.quality);
        video.lastChild.textContent = t.video; audio.lastChild.textContent = t.audio;
        formatLabel.textContent = t.format; qualityLabel.textContent = t.quality; sizeHint.textContent = t.size; folderHint.textContent = t.hint; retry.textContent = t.retry;
        launch.lastChild.textContent = media ? scope === "playlist" ? t.playlistDownload : t.download : t.open;
        if (media) {
          if (kind === "audio") for (const choice of choices.children) { choice.textContent = t.automatic; choice.setAttribute("aria-label", t.automatic); }
          updateSummary();
        }
        position();
      }
      function position() { const rect = anchor?.getBoundingClientRect(); const x = rect ? Math.min(innerWidth - panel.offsetWidth - 12, Math.max(12, rect.right - panel.offsetWidth)) : 12; panel.style.left = `${x}px`; panel.style.top = `${rect ? Math.min(Math.max(12, rect.bottom + 8), Math.max(12, innerHeight - panel.offsetHeight - 12)) : 12}px`; }
      function close() { stopLanguage?.(); releaseReload?.(); if (embedded) document.body.style.minHeight = previousHeight; disposed = true; host.remove(); document.removeEventListener("pointerdown", outside, true); document.removeEventListener("keydown", keys, true); removeEventListener("resize", position); removeEventListener("scroll", position, true); anchor?.focus(); if (active === close) active = null; onClose?.(); }
      function outside(event) { if (!event.composedPath().includes(host) && !event.composedPath().includes(anchor)) close(); }
      function keys(event) { if (event.key === "Escape") { close(); event.stopPropagation(); event.preventDefault(); } if (event.key === "Tab") { const all = [...shadow.querySelectorAll('button:not(:disabled):not([tabindex="-1"])')].filter(n => n.getClientRects().length); const current = shadow.activeElement; if (event.shiftKey && current === all[0]) { all.at(-1).focus(); event.preventDefault(); } else if (!event.shiftKey && current === all.at(-1)) { all[0].focus(); event.preventDefault(); } } }
      document.addEventListener("pointerdown", outside, true); document.addEventListener("keydown", keys, true); addEventListener("resize", position); addEventListener("scroll", position, true); active = close;
      function chooseKind(value) { kind = value; renderFormats(); }
      function chooseScope(value) {
        scope = value === "playlist" && playlistAvailable ? "playlist" : "video";
        selectRadio(scopePicker, scope === "playlist" ? playlistScope : singleScope);
        playlistHint.classList.toggle("hidden", scope !== "playlist");
        launch.lastChild.textContent = media ? scope === "playlist" ? t.playlistDownload : t.download : t.open;
        if (media) updateSummary(); position();
      }
      function updateSummary() { selection.textContent = format.value.toUpperCase() + " · " + (kind === "video" ? (scope === "playlist" ? t.upTo : "") + selectedHeight + "p" : t.automatic); }
      function renderFormats() {
        video.disabled = !media.videos.length; audio.disabled = !media.audio.length;
        video.setAttribute("aria-pressed", String(kind === "video")); audio.setAttribute("aria-pressed", String(kind === "audio"));
        const available = kind === "video" ? media.videos.map(v => v.format) : media.audio; format.value = available[0] || ""; format.dataset.count = String(available.length); choices.classList.toggle("audio", kind === "audio");
        format.replaceChildren(); for (const value of available) { const option = button(value.toUpperCase(), "", () => { format.value = value; selectRadio(format, option); renderQualities(); }); option.setAttribute("role", "radio"); format.append(option); } selectRadio(format, format.firstChild);
        renderQualities();
      }
      function renderQualities() {
        choices.replaceChildren(); const heights = kind === "video" ? media.videos.find(v => v.format === format.value)?.heights || [] : [0];
        selectedHeight = heights.find(h => h <= 1080) ?? heights[0] ?? 0;
        for (const height of heights) { const label = height ? `${height}p` : t.automatic; const choice = button(label, "", () => { selectedHeight = height; selectRadio(choices, choice); updateSummary(); }); choice.setAttribute("role", "radio"); choice.setAttribute("aria-label", label); choice.setAttribute("aria-checked", String(height === selectedHeight)); choice.tabIndex = height === selectedHeight ? 0 : -1; if (height >= 720) choice.append(el("small", "", height >= 4320 ? "8K" : height >= 2160 ? "4K" : "HD")); choices.append(choice); }
        launch.disabled = !format.value || !heights.length; launch.lastChild.textContent = scope === "playlist" ? t.playlistDownload : t.download; updateSummary(); position();
      }
      async function load() {
        if (loading) return; loading = true; launch.disabled = true; options.classList.add("hidden"); retry.classList.add("hidden"); status.textContent = t.loading; status.dataset.error = "false"; topStatus.textContent = ""; media = null;
        try {
          const data = await inspect(); if (disposed) return;
          if (!data || !Array.isArray(data.videos) || !Array.isArray(data.audio)) throw new Error("formats");
          media = { title: typeof data.title === "string" ? data.title.slice(0, 600) : "Video", videos: data.videos.filter(v => ["mp4", "mkv", "webm"].includes(v.format) && Array.isArray(v.heights)).map(v => ({ format: v.format, heights: [...new Set(v.heights.filter(h => Number.isInteger(h) && h > 0 && h <= 8640))].slice(0, 24) })).filter(v => v.heights.length), audio: data.audio.filter(f => ["mp3", "m4a", "opus", "wav", "flac"].includes(f)) };
          if (!media.videos.length && !media.audio.length) throw new Error("formats");
          title.textContent = media.title;
          source.textContent = typeof data.source === "string" ? data.source.slice(0, 200) : "";
          try { const url = new URL(data.thumbnail); if (url.protocol === "https:" && !url.username && !url.password && !url.port && !/^(localhost|127\.|0\.|10\.|192\.168\.|169\.254\.|172\.(1[6-9]|2\d|3[01])\.|\[)/iu.test(url.hostname)) { thumbnail.hidden = false; thumbnail.src = url.href; identity.classList.remove("no-image"); } } catch { /* A text-only source is an honest fallback. */ }
          kind = media.videos.length ? "video" : "audio"; options.classList.remove("hidden"); status.textContent = ""; topStatus.textContent = t.ready; topStatus.dataset.ready = "true"; renderFormats();
        } catch (error) {
          if (!disposed) {
            // A failed in-video lookup returns to the original action without a dead fallback panel.
            if (!embedded) { close(); onInspectionFailure?.(error); return; }
            if (error?.code === "preparing") {
              status.textContent = globalThis.ViFreeConnection.errorMessage(error, arabic);
              status.dataset.error = "false"; return;
            }
            media = null; status.textContent = error?.code === "unsupported" ? t.fallback :
              error?.code === "unavailable" ? globalThis.ViFreeConnection.errorMessage(error, arabic) : t.unavailable;
            status.dataset.error = "true"; retry.classList.remove("hidden"); launch.disabled = false;
          }
        }
        finally { loading = false; if (!disposed) position(); }
      }
      async function openApp() {
        launch.disabled = true;
        try { const result = await send(media ? { kind, format: format.value, height: kind === "video" ? selectedHeight : 0 } : undefined, scope); if (disposed) return; status.dataset.error = "false"; status.textContent = result?.code === "opened-legacy" ? t.legacy : result?.code === "queued" ? t.queued : t.success; onResult?.(result); if (media && result?.code !== "opened-legacy") close(); }
        catch (error) { if (!disposed) { status.dataset.error = "true"; status.textContent = globalThis.ViFreeConnection.errorMessage(error, arabic); } }
        finally { if (!disposed) { launch.disabled = false; position(); } }
      }
      if (preview) {
        title.textContent = typeof preview.title === "string" ? preview.title.slice(0, 600) : "";
        source.textContent = typeof preview.source === "string" ? preview.source.slice(0, 200) : "";
        try { const image = new URL(preview.thumbnail); if (image.protocol === "https:" && !image.username && !image.password && !image.port && !/^(localhost|127\.|0\.|10\.|192\.168\.|169\.254\.|172\.(1[6-9]|2\d|3[01])\.|\[)/iu.test(image.hostname)) { thumbnail.src = image.href; thumbnail.hidden = false; identity.classList.remove("no-image"); } } catch { /* Page artwork is optional. */ }
      }
      stopLanguage = globalThis.ViFreeConnection?.watchLanguage?.(applyLanguage);
      position(); top.lastChild.focus(); void load(); return { close };
    }
  };
})();
