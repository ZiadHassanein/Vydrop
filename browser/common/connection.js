globalThis.ViFreeConnection = (() => {
  const api = globalThis.browser ?? chrome;
  const preview = (api.runtime.getManifest?.().name ?? "Vydrop").endsWith(" Preview");
  const name = preview ? "Vydrop Preview" : "Vydrop";
  const host = preview ? "com.vifree.preview" : "com.vifree";
  // Errors carry a stable code so each surface can show its own language; the message stays English for logs.
  function fail(message, code) { const error = new Error(message); if (code) error.code = code; return error; }
  const messages = {
    en: {
      invalid: "Choose a public video link.",
      missing: "Could not find this video's link. Open the video itself or use Share → Copy link.",
      ambiguous: "More than one video is visible. Open the video you want, then try again.",
      inspect: "Could not inspect this page. Reload the extension and page, then try again.",
      busy: `${name} is busy. Finish or cancel the current download, then try again.`,
      unavailable: `Open ${name} and enable Browser integration, then try again.`,
      failed: `Could not send this video to ${name}. Try again, or copy the video link into the app.`
      ,unsupported: "Update Vydrop to use this action.", protected: "This player uses protected media, which Vydrop cannot download.", storage: "Vydrop could not save the queue. Check disk space and folder access."
    },
    ar: {
      invalid: "اختر رابط فيديو متاحًا للعامة.",
      missing: "تعذّر العثور على رابط هذا الفيديو. افتح الفيديو نفسه أو استخدم مشاركة ثم نسخ الرابط.",
      ambiguous: "يظهر أكثر من فيديو. افتح الفيديو المطلوب ثم حاول مجددًا.",
      inspect: "تعذّر فحص هذه الصفحة. أعد تحميل الإضافة والصفحة ثم حاول مجددًا.",
      busy: `${name} مشغول. أنهِ التنزيل الحالي أو ألغِه، ثم حاول مجددًا.`,
      unavailable: `افتح ${name} وفعّل الربط بالمتصفح، ثم حاول مجددًا.`,
      failed: `تعذّر إرسال الفيديو إلى ${name}. حاول مجددًا أو انسخ رابط الفيديو إلى التطبيق.`
      ,unsupported: "حدّث Vydrop لاستخدام هذا الإجراء.", protected: "يستخدم هذا المشغّل محتوى محميًا لا يستطيع Vydrop تنزيله.", storage: "تعذّر على Vydrop حفظ قائمة التنزيل. تحقق من المساحة المتاحة وأذونات المجلد."
    }
  };
  // Never display raw host/page errors: older hosts may omit a code entirely.
  function errorMessage(error, arabic) {
    const table = arabic ? messages.ar : messages.en;
    return Object.hasOwn(table, error?.code) ? table[error.code] : table.failed;
  }
  let port, idle, sequence = 0;
  const pending = new Map();
  function closePort() {
    clearTimeout(idle); const previous = port; port = null;
    for (const job of pending.values()) { clearTimeout(job.timeout); job.reject(fail(messages.en.unavailable, "unavailable")); }
    pending.clear(); previous?.disconnect();
  }
  function request2(type, fields = {}) {
    return new Promise((resolve, reject) => {
      if (!api.runtime.connectNative) { reject(fail(messages.en.unsupported, "unsupported")); return; }
      if (!port) {
        try { port = api.runtime.connectNative(host); }
        catch { reject(fail(messages.en.unavailable, "unavailable")); return; }
        const connectedPort = port;
        port.onMessage.addListener(response => {
          const id = response?.requestId ?? (pending.size === 1 ? pending.keys().next().value : "");
          const job = pending.get(id); if (!job) return;
          pending.delete(id); clearTimeout(job.timeout); job.resolve(response);
        });
        port.onDisconnect.addListener(() => {
          void api.runtime.lastError; if (port !== connectedPort) return; port = null;
          for (const job of pending.values()) { clearTimeout(job.timeout); job.reject(fail(messages.en.unavailable, "unavailable")); }
          pending.clear();
        });
      }
      const requestId = `${Date.now()}_${++sequence}`;
      const timeout = setTimeout(() => { pending.delete(requestId); reject(fail(messages.en.unavailable, "unavailable")); closePort(); }, type === "inspect" ? 80000 : 18000);
      pending.set(requestId, { resolve, reject, timeout });
      try { port.postMessage({ version: 2, type, requestId, ...fields }); }
      catch { pending.delete(requestId); clearTimeout(timeout); reject(fail(messages.en.unavailable, "unavailable")); closePort(); }
      clearTimeout(idle); idle = setTimeout(closePort, type === "inspect" ? 90000 : 30000);
    });
  }
  function validUrl(value) {
    if (typeof value !== "string" || value.length > 8192 || /[\u0000-\u001f\u007f]/u.test(value)) throw fail(messages.en.invalid, "invalid");
    let url;
    try { url = new URL(value); } catch { throw fail(messages.en.invalid, "invalid"); }
    if (!["https:", "http:"].includes(url.protocol) || url.username || url.password || url.port) throw fail(messages.en.invalid, "invalid");
    return url;
  }
  async function sendLink(value, context = {}) {
    const url = validUrl(value);
    let response, choiceSupported = false;
    if (api.runtime.connectNative) {
      let health;
      try { health = await request2("ping"); } catch { /* The legacy host may close after rejecting v2. */ }
      if (health?.ok && health.protocol === 2) {
        const fields = { url: url.href };
        if (context.sourcePage) fields.sourcePage = validUrl(context.sourcePage).href;
        const asset = context.mediaId || url.pathname.match(/^\/playlist\/vid\/v2\/([A-Za-z0-9_-]+)\/mp4-/u)?.[1];
        if (/^[A-Za-z0-9_-]{1,128}$/u.test(asset ?? "")) fields.mediaId = asset;
        if (context.output && health.capabilities?.includes("output-selection")) { fields.output = context.output; choiceSupported = true; }
        response = await request2(context.enqueue ? "enqueue" : "open-link", fields);
      }
    }
    try { response ??= await api.runtime.sendNativeMessage(host, { version: 1, type: "open-link", url: url.href }); }
    catch { throw fail(`Open ${name} and enable Browser integration, then try again.`, "unavailable"); }
    if (!response?.ok) throw fail("The native host rejected this link.", Object.hasOwn(messages.en, response?.code) ? response.code : "failed");
    return context.output && !choiceSupported ? { ...response, code: "opened-legacy" } : response;
  }
  async function inspectLink(value) {
    const url = validUrl(value);
    const health = await request2("ping");
    if (!health?.ok || !health.capabilities?.includes("formats")) throw fail(messages.en.unsupported, "unsupported");
    const result = await request2("inspect", { url: url.href });
    if (!result?.ok || !result.media) throw fail(messages.en.failed, result?.code || "inspect");
    return result.media;
  }
  return { api, name, sendLink, inspectLink, errorMessage, testConnection: () => request2("ping") };
})();
