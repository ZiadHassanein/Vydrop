// Check only this loaded companion's local files. Never launch the native host or refresh a website.
(() => {
  const api = globalThis.browser ?? globalThis.chrome;
  if (!api?.alarms || !api.runtime?.reload || !api.runtime.getURL) return;
  const alarmName = "vydrop-companion-update";
  const running = api.runtime.getManifest();
  const holds = new Set();
  const required = ["manifest.json", "auto-reload.js", "background.js", "connection.js", "video-detection.js", "quality-picker.js", "video-overlay.js", "popup.js", "popup.html", "popup.css", "brand-logo-dark.svg", "icon.png", "icon16.png", "icon48.png"];
  let checking = false, reloading = false;
  const busy = () => holds.size > 0 || globalThis.ViFreeConnection?.hasPendingRequests?.();
  api.runtime.onConnect?.addListener(port => {
    if (port.name !== "vydrop-update-active" || port.sender?.id !== api.runtime.id) return;
    holds.add(port);
    port.onMessage?.addListener(() => {}); // UI heartbeats keep this worker's activity lease alive.
    port.onDisconnect.addListener(() => holds.delete(port));
  });
  async function bytes(name) {
    const response = await fetch(api.runtime.getURL(name), { cache: "no-store", redirect: "error" });
    if (!response.ok) throw new Error("Companion file unavailable");
    const value = await response.arrayBuffer();
    if (value.byteLength > 4 * 1024 * 1024) throw new Error("Invalid companion file");
    return value;
  }
  async function check() {
    if (checking || reloading || busy()) return;
    checking = true;
    try {
      const descriptor = await bytes("companion-update.json");
      const update = JSON.parse(new TextDecoder().decode(descriptor));
      if (update.schema !== 1 || update.name !== running.name || !/^\d+\.\d+\.\d+(?:\.\d+)?$/u.test(update.version ?? "") || update.version === running.version) return;
      const entries = Object.entries(update.files ?? {});
      if (entries.length < required.length || entries.length > 40 || required.some(name => !Object.hasOwn(update.files, name)) ||
          entries.some(([name, hash]) => !/^[A-Za-z0-9_-][A-Za-z0-9._-]{0,100}$/u.test(name) || name === "companion-update.json" || !/^[a-f0-9]{64}$/u.test(hash))) return;
      let total = 0;
      // A partly copied or rolled-back update must never cause a reload into mixed files.
      for (const [name, expected] of entries) {
        const content = await bytes(name);
        if ((total += content.byteLength) > 12 * 1024 * 1024) return;
        const hash = [...new Uint8Array(await crypto.subtle.digest("SHA-256", content))].map(value => value.toString(16).padStart(2, "0")).join("");
        if (hash !== expected) return;
        if (name === "manifest.json") {
          const manifest = JSON.parse(new TextDecoder().decode(content));
          if (manifest.version !== update.version || manifest.name !== running.name) return;
        }
      }
      const after = new Uint8Array(await bytes("companion-update.json")), before = new Uint8Array(descriptor);
      if (busy() || before.length !== after.length || before.some((value, index) => value !== after[index])) return;
      reloading = true;
      try { api.runtime.reload(); } catch { reloading = false; }
    } catch { /* Missing, interrupted or older bundles are retried by the next local alarm. */ }
    finally { checking = false; }
  }
  async function start() {
    try {
      if (!await api.alarms.get(alarmName)) await api.alarms.create(alarmName, { delayInMinutes: 1, periodInMinutes: 1 });
      await check();
    } catch { /* Recreate a missing alarm on the next worker/browser startup. */ }
  }
  api.alarms.onAlarm.addListener(alarm => { if (alarm.name === alarmName) return check(); });
  api.runtime.onStartup?.addListener(start);
  globalThis.VydropAutoReload = { check, ready: start() };
})();
