const { api, name, sendLink, errorMessage } = globalThis.ViFreeConnection;
// The popup follows the browser's interface language; the in-page overlay follows the page language.
const arabic = /^ar\b/iu.test(api.i18n?.getUILanguage?.() ?? globalThis.navigator?.language ?? "");
const text = arabic ? {
  intro: `افتح الفيديو الذي تشاهده في ${name}، ثم اختر ما تريد تنزيله.`, send: `فتح في ${name}`,
  finding: "جارٍ تحديد الفيديو…", opening: "جارٍ الفتح…", opened: `تم الفتح في ${name}. راجع الرابط قبل التنزيل.`
} : {
  intro: `Open the video you’re watching in ${name}, then choose what to download.`, send: `Open in ${name}`,
  finding: "Finding video…", opening: "Opening…", opened: `Opened in ${name}. Review the link before downloading.`
};
const button = document.querySelector("#send");
const status = document.querySelector("#status");
if (arabic) {
  document.documentElement.lang = "ar";
  document.documentElement.dir = "rtl";
  const intro = document.querySelector("#intro");
  if (intro) intro.textContent = text.intro;
  button.textContent = text.send;
}
button.addEventListener("click", async () => {
  button.disabled = true;
  status.textContent = text.finding;
  try {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const url = await globalThis.ViFreeVideoDetection.resolve(tab);
    status.textContent = text.opening;
    const response = await sendLink(url, { sourcePage: tab?.url });
    status.textContent = response.code === "queued" ? (arabic ? "أُضيف إلى قائمة التنزيل. راجعه في التطبيق قبل البدء." : "Added to the queue. Review it in the app before starting.") : text.opened;
  } catch (error) {
    status.textContent = errorMessage(error, arabic);
  } finally { button.disabled = false; }
});
