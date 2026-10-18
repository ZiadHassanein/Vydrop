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
    },
    ar: {
      invalid: "اختر رابط فيديو متاحًا للعامة.",
      missing: "تعذّر العثور على رابط هذا الفيديو. افتح الفيديو نفسه أو استخدم مشاركة ثم نسخ الرابط.",
      ambiguous: "يظهر أكثر من فيديو. افتح الفيديو المطلوب ثم حاول مجددًا.",
      inspect: "تعذّر فحص هذه الصفحة. أعد تحميل الإضافة والصفحة ثم حاول مجددًا.",
      busy: `${name} مشغول. أنهِ التنزيل الحالي أو ألغِه، ثم حاول مجددًا.`,
      unavailable: `افتح ${name} وفعّل الربط بالمتصفح، ثم حاول مجددًا.`,
      failed: `تعذّر إرسال الفيديو إلى ${name}. حاول مجددًا أو انسخ رابط الفيديو إلى التطبيق.`
    }
  };
  // Never display raw host/page errors: older hosts may omit a code entirely.
  function errorMessage(error, arabic) {
    const table = arabic ? messages.ar : messages.en;
    return Object.hasOwn(table, error?.code) ? table[error.code] : table.failed;
  }
  async function sendLink(value) {
    if (typeof value !== "string" || value.length > 8192 || /[\u0000-\u001f\u007f]/u.test(value)) throw fail(messages.en.invalid, "invalid");
    let url;
    try { url = new URL(value); } catch { throw fail(messages.en.invalid, "invalid"); }
    if (!["https:", "http:"].includes(url.protocol) || url.username || url.password || url.port) throw fail(messages.en.invalid, "invalid");
    let response;
    try { response = await api.runtime.sendNativeMessage(host, { version: 1, type: "open-link", url: url.href }); }
    catch { throw fail(`Open ${name} and enable Browser integration, then try again.`, "unavailable"); }
    if (!response?.ok) throw fail("The native host rejected this link.", Object.hasOwn(messages.en, response?.code) ? response.code : "failed");
    return response;
  }
  return { api, name, sendLink, errorMessage };
})();
