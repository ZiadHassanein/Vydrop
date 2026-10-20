// Optional response-metadata observation. No request headers, cookies or response bodies are read.
globalThis.VydropMediaDiscovery = (() => {
  const api = globalThis.browser ?? chrome;
  const cache = new Map();
  const ttl = 90000, maximumTabs = 32, maximumCandidates = 48;
  let enabled = false, attached = false;
  function publicUrl(value) {
    try {
      if (typeof value !== "string" || value.length > 8192 || /[\u0000-\u0020\u007f]/u.test(value)) return "";
      const u = new URL(value);
      return /^https?:$/u.test(u.protocol) && !u.username && !u.password && !u.port && u.hostname.includes(".") &&
        !/^(?:\d{1,3}\.){3}\d{1,3}$/u.test(u.hostname) && !/(?:^|\.)(?:localhost|local)$/iu.test(u.hostname) && !u.hostname.includes(":") ? u.href : "";
    } catch { return ""; }
  }
  function observe(details) {
    if (!enabled || details.tabId < 0) return;
    if (details.type === "main_frame") { cache.delete(details.tabId); return; }
    if (details.type === "sub_frame") {
      const items = cache.get(details.tabId); if (items) cache.set(details.tabId, items.filter(item => item.frameId !== details.frameId)); return;
    }
    const url = publicUrl(details.url), origin = publicUrl(details.initiator || details.originUrl || "");
    if (!url || !origin || details.statusCode < 200 || details.statusCode >= 300) return;
    const mime = details.responseHeaders?.find(h => h.name.toLowerCase() === "content-type")?.value?.split(";")[0].trim().toLowerCase() ?? "";
    const kind = /^(?:application\/(?:vnd\.apple\.mpegurl|x-mpegurl)|audio\/mpegurl)$/u.test(mime) ? "HLS" : mime === "application/dash+xml" ? "DASH" : /^(?:video\/(?:mp4|webm)|audio\/(?:mpeg|mp4|ogg|webm))$/u.test(mime) ? "media" : "";
    if (!kind) return;
    if (/\.(?:ts|m4s|cmfv|cmfa)(?:$|\?)/iu.test(new URL(url).pathname) || /(?:^|\/)(?:segment|fragment|chunk)[-_]?\d/iu.test(new URL(url).pathname)) return;
    const now = Date.now();
    const items = (cache.get(details.tabId) ?? []).filter(item => now - item.at < ttl && item.url !== url);
    let name; try { name = decodeURIComponent(new URL(url).pathname.split("/").pop() || kind); } catch { name = kind; }
    items.unshift({ url, kind, mime, name: name.slice(0, 100), frameId: details.frameId, documentId: details.documentId ?? "", origin: new URL(origin).origin, at: now });
    cache.delete(details.tabId); cache.set(details.tabId, items.slice(0, maximumCandidates));
    while (cache.size > maximumTabs) cache.delete(cache.keys().next().value);
  }
  function attach() {
    if (attached || !api.webRequest?.onHeadersReceived) return;
    try { api.webRequest.onHeadersReceived.addListener(observe, { urls: ["https://*/*", "http://*/*"], types: ["main_frame", "sub_frame", "media", "xmlhttprequest", "other"] }, ["responseHeaders"]); attached = true; } catch { /* Optional permission has not been granted. */ }
  }
  function list(tabId) {
    if (!enabled) return [];
    return (cache.get(tabId) ?? []).filter(item => Date.now() - item.at < ttl).map(({ at, ...item }) => item);
  }
  async function refresh() { enabled = await api.permissions?.contains({ permissions: ["webRequest"], origins: ["https://*/*", "http://*/*"] }) ?? false; if (enabled) attach(); else cache.clear(); return enabled; }
  attach();
  api.permissions?.onAdded?.addListener(() => { refresh().catch(() => {}); });
  api.permissions?.onRemoved?.addListener(() => { refresh().catch(() => {}); });
  api.tabs?.onRemoved?.addListener(id => cache.delete(id));
  api.runtime.onMessage.addListener((message, sender, respond) => {
    if (!["vydrop-discovery-list", "vydrop-discovery-state"].includes(message?.type)) return false;
    // Only extension pages may read observed URLs; content scripts and sites cannot enumerate them.
    if (sender.id !== api.runtime.id || !sender.url?.startsWith(api.runtime.getURL(""))) { respond({ ok: false }); return false; }
    refresh().then(active => respond({ ok: true, enabled: active, candidates: Number.isInteger(message.tabId) ? list(message.tabId) : [] })).catch(() => respond({ ok: false, enabled: false, candidates: [] }));
    return true;
  });
  refresh().catch(() => {});
  return { observe, list, refresh };
})();
