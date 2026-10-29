const { api, name, sendLink, errorMessage } = globalThis.ViFreeConnection;
// The browser language is a fallback until the connected app supplies its preference.
let arabic = /^ar\b/iu.test(api.i18n?.getUILanguage?.() ?? globalThis.navigator?.language ?? "");
const popupText = () => arabic ? {
  intro: `افتح الفيديو الذي تشاهده في ${name}، ثم اختر ما تريد تنزيله.`, send: `فتح في ${name}`,
  finding: "جارٍ تحديد الفيديو…", opening: "جارٍ الفتح…", opened: `تم الفتح في ${name}. راجع الرابط قبل التنزيل.`,
  queued: "أُضيف إلى قائمة التنزيل. راجعه في التطبيق قبل البدء.", queuedStart: "أُضيف إلى قائمة التنزيل. ابدأه من التطبيق."
} : {
  intro: `Open the video you’re watching in ${name}, then choose what to download.`, send: `Open in ${name}`,
  finding: "Finding video…", opening: "Opening…", opened: `Opened in ${name}. Review the link before downloading.`,
  queued: "Added to the queue. Review it in the app before starting.", queuedStart: "Added to the queue. Start it in the app."
};
let text = popupText();
const button = document.querySelector("#send");
const status = document.querySelector("#status");
function renderLanguage() {
  document.documentElement.lang = arabic ? "ar" : "en";
  document.documentElement.dir = arabic ? "rtl" : "ltr";
  const intro = document.querySelector("#intro");
  if (intro) intro.textContent = text.intro;
  button.textContent = text.send;
  if (queueButton) queueButton.textContent = arabic ? "إضافة إلى قائمة التنزيل" : "Add to download queue";
}
button.addEventListener("click", async () => {
  button.disabled = true;
  const releaseReload = globalThis.ViFreeConnection.holdReload();
  status.textContent = text.finding;
  try {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const found = globalThis.ViFreeVideoDetection.resolveDetails ? await globalThis.ViFreeVideoDetection.resolveDetails(tab) : { url: await globalThis.ViFreeVideoDetection.resolve(tab) };
    const url = found.url;
    const source = { sourcePage: found.sourcePage || tab?.url, mediaId: found.mediaId };
    const playlist = globalThis.ViFreeVideoDetection.playlistFor?.(url, tab?.url) || "";
    if (globalThis.VydropQualityPicker) {
      globalThis.VydropQualityPicker.open({ anchor: button, arabic, embedded: true,
        playlistAvailable: Boolean(playlist),
        inspect: () => globalThis.ViFreeConnection.inspectLink(url, source),
        send: (output, scope) => sendLink(scope === "playlist" ? playlist : url, { ...source, output, startDownload: Boolean(output), playlist: scope === "playlist" })
      });
      status.textContent = ""; return;
    }
    status.textContent = text.opening;
    const response = await sendLink(url, source);
    status.textContent = response.code === "queued" ? text.queued : text.opened;
  } catch (error) {
    status.textContent = errorMessage(error, arabic);
  } finally { releaseReload(); button.disabled = false; }
});

const queueButton = document.querySelector("#enqueue");
if (queueButton?.addEventListener) {
  queueButton.textContent = arabic ? "إضافة إلى قائمة التنزيل" : "Add to download queue";
  queueButton.addEventListener("click", async () => {
    queueButton.disabled = true;
    const releaseReload = globalThis.ViFreeConnection.holdReload();
    status.textContent = text.finding;
    try {
      const [tab] = await api.tabs.query({ active: true, currentWindow: true });
      const found = globalThis.ViFreeVideoDetection.resolveDetails ? await globalThis.ViFreeVideoDetection.resolveDetails(tab) : { url: await globalThis.ViFreeVideoDetection.resolve(tab) };
      const result = await sendLink(found.url, { enqueue: true, sourcePage: found.sourcePage || tab?.url, mediaId: found.mediaId });
      status.textContent = result.code === "queued" ? text.queuedStart : text.opened;
    } catch (error) { status.textContent = errorMessage(error, arabic); }
    finally { releaseReload(); queueButton.disabled = false; }
  });
}
renderLanguage();
globalThis.ViFreeConnection.watchLanguage?.(language => {
  if (arabic === (language === "ar")) return;
  const previous = text, previousArabic = arabic; arabic = language === "ar"; text = popupText();
  const key = Object.keys(previous).find(key => previous[key] === status.textContent);
  if (key) status.textContent = text[key];
  else for (const code of ["invalid", "missing", "ambiguous", "advertisement", "inspect", "busy", "unavailable", "preparing", "failed", "unsupported", "protected", "storage"])
    if (status.textContent === errorMessage({ code }, previousArabic)) { status.textContent = errorMessage({ code }, arabic); break; }
  renderLanguage();
});
