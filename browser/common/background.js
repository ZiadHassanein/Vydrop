if (!globalThis.ViFreeConnection) importScripts("connection.js", "video-detection.js");
if (typeof importScripts === "function" && !globalThis.VydropMediaDiscovery) importScripts("media-discovery.js");
const { api, name, sendLink, errorMessage } = globalThis.ViFreeConnection;
const arabic = /^ar\b/iu.test(api.i18n?.getUILanguage?.() ?? "");
const text = arabic
  ? { link: `فتح الرابط في ${name}`, video: `فتح الفيديو في ${name}` }
  : { link: `Open link in ${name}`, video: `Open video in ${name}` };
const resolvedSources = new Map();
async function resolveOnce(message, sender) {
  const key = `${sender.tab.id}:${sender.frameId}:${message.url}:${message.linkedinAsset || ""}:${message.sourcePage || ""}`;
  const cached = resolvedSources.get(key);
  if (cached && Date.now() - cached.time < 30000) return cached.promise;
  if (resolvedSources.size >= 8) resolvedSources.delete(resolvedSources.keys().next().value);
  const entry = { time: Date.now(), promise: globalThis.ViFreeVideoDetection.resolveSource({ url: message.url, linkedinAsset: message.linkedinAsset, sourcePage: message.sourcePage }) };
  resolvedSources.set(key, entry);
  try { return await entry.promise; } catch (error) { resolvedSources.delete(key); throw error; }
}

api.runtime.onMessage.addListener((message, sender, respond) => {
  if (!["vifree-open-video", "vydrop-inspect-video"].includes(message?.type) || message.version !== 1) return false;
  let allowed = false;
  try {
    const page = new URL(sender.url);
    allowed = sender.id === api.runtime.id && Number.isInteger(sender.tab?.id) &&
      Number.isInteger(sender.frameId) && sender.frameId >= 0 && /^https?:$/u.test(page.protocol) && !page.username && !page.password;
  } catch { /* Only this extension's web content scripts can forward an explicit video click. */ }
  if (!allowed) { respond({ ok: false, code: "invalid", message: "This video page cannot open Vydrop." }); return false; }
  resolveOnce(message, sender)
    .then(async url => {
      if (message.type === "vydrop-inspect-video") {
        const media = await globalThis.ViFreeConnection.inspectLink(url);
        respond({ ok: true, media, url }); return;
      }
      const result = await sendLink(url, { sourcePage: message.sourcePage || (url === message.url ? sender.tab.url || sender.url : message.url), output: message.output, startDownload: Boolean(message.output) });
      respond({ ok: true, url, code: result.code });
    }, error => respond({ ok: false, code: error.code, message: error.message }))
    .catch(error => respond({ ok: false, code: error.code, message: error.message }));
  return true;
});

api.runtime.onInstalled.addListener(async () => {
  await api.contextMenus.removeAll();
  api.contextMenus.create({ id: "vifree-send-link", title: text.link, contexts: ["link"], targetUrlPatterns: ["https://*/*", "http://*/*"] });
  api.contextMenus.create({ id: "vifree-send-page", title: text.video, contexts: ["page", "video"] });
});
api.contextMenus.onClicked.addListener(async (info, tab) => {
  try {
    await sendLink(await globalThis.ViFreeVideoDetection.resolve(tab, info));
    await api.action.setBadgeText({ text: "OK", tabId: tab?.id });
    await api.action.setBadgeBackgroundColor({ color: "#CB2D42", tabId: tab?.id });
    await api.action.setTitle({ title: text.video, tabId: tab?.id });
  } catch (error) {
    await api.action.setBadgeText({ text: "!", tabId: tab?.id });
    await api.action.setBadgeBackgroundColor({ color: "#AF2527", tabId: tab?.id });
    await api.action.setTitle({ title: errorMessage(error, arabic), tabId: tab?.id });
  }
});
