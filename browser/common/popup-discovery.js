(() => {
  const { api, sendLink, testConnection, errorMessage } = globalThis.ViFreeConnection;
  const toggle = document.querySelector("#discovery");
  if (!toggle?.addEventListener) return;
  const ar = /^ar\b/iu.test(api.i18n?.getUILanguage?.() ?? "");
  const strings = ar ? {
    test: "اختبار الاتصال", enqueue: "إضافة إلى قائمة التنزيل", enable: "تفعيل اكتشاف مسارات الفيديو", disable: "إيقاف اكتشاف المسارات", refresh: "تحديث المسارات", empty: "شغّل الفيديو ثم حدّث هذه القائمة. لا تظهر المسارات المحمية أو كل أنواع المشغّلات.",
    hint: "اختياري: يقرأ نوع الوسائط وعنوانها من استجابات المواقع المسموح بها. لا يقرأ ملفات الارتباط أو بيانات الدخول. تُحفظ النتائج مؤقتًا في الذاكرة فقط.",
    verified: "تم التحقق من الاتصال بالمتصفح والتطبيق.", ready: "ربط المتصفح متاح. افتح Vydrop ثم اختبر الاتصال مجددًا.", denied: "لم تُمنح الأذونات. يظل إرسال الروابط المعتاد متاحًا.", enabled: "فعّل الفيديو أو حدّث صفحته ثم حدّث المسارات هنا.", opened: "أُرسل إلى Vydrop للمراجعة.", queued: "أُضيف إلى قائمة التنزيل. ابدأه من التطبيق.", working: "جارٍ التنفيذ…"
  } : {
    test: "Test connection", enqueue: "Add to download queue", enable: "Enable media-stream discovery", disable: "Disable stream discovery", refresh: "Refresh streams", empty: "Play the video, then refresh this list. Protected media and some players are unsupported.",
    hint: "Optional: reads media types and URLs from permitted website responses. No cookies or login data are read. Results stay briefly in memory only.",
    verified: "Browser-to-app connection verified.", ready: "Native host is ready. Open Vydrop, then test again.", denied: "Permission was not granted. Normal link sending still works.", enabled: "Play the video or refresh its page, then refresh streams here.", opened: "Sent to Vydrop for review.", queued: "Added to the queue. Start it in the app.", working: "Working…"
  };
  const status = document.querySelector("#status"), list = document.querySelector("#streams");
  const test = document.querySelector("#test"), enqueue = document.querySelector("#enqueue"), refresh = document.querySelector("#refresh-streams");
  test.textContent = strings.test; enqueue.textContent = strings.enqueue; refresh.textContent = strings.refresh;
  document.querySelector("#discovery-hint").textContent = strings.hint;
  document.querySelector("#discovery-heading").textContent = ar ? "اكتشاف مسارات الفيديو" : "Media-stream discovery";
  async function action(button, operation) {
    button.disabled = true; status.textContent = strings.working;
    try { await operation(); } catch (error) { status.textContent = errorMessage(error, ar); }
    finally { button.disabled = false; }
  }
  test.addEventListener("click", () => action(test, async () => {
    const result = await testConnection();
    if (!result?.ok || result.protocol !== 2) throw { code: "unsupported" };
    status.textContent = result.appRunning ? strings.verified : strings.ready;
  }));
  enqueue.addEventListener("click", () => action(enqueue, async () => {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const url = await globalThis.ViFreeVideoDetection.resolve(tab);
    const result = await sendLink(url, { enqueue: true, sourcePage: tab.url });
    status.textContent = result.code === "queued" ? strings.queued : strings.opened;
  }));
  let enabled = false;
  async function show() {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const result = await api.runtime.sendMessage({ type: "vydrop-discovery-list", tabId: tab?.id });
    enabled = result?.enabled === true;
    toggle.textContent = enabled ? strings.disable : strings.enable;
    refresh.hidden = !enabled; list.replaceChildren();
    if (!enabled) return;
    if (!result.candidates?.length) { const empty = document.createElement("p"); empty.textContent = strings.empty; list.append(empty); return; }
    for (const candidate of result.candidates) {
      const button = document.createElement("button"); button.className = "stream secondary";
      button.textContent = `${candidate.kind} · ${candidate.name}`;
      button.addEventListener("click", () => action(button, async () => {
        const inspected = await api.scripting.executeScript({ target: { tabId: tab.id, frameIds: [candidate.frameId] }, func: () => [...document.querySelectorAll("video")].some(v => !v.paused && v.mediaKeys) });
        if (inspected.some(item => item.result === true)) throw { code: "protected" };
        const result = await sendLink(candidate.url, { sourcePage: tab.url });
        status.textContent = result.code === "queued" ? strings.queued : strings.opened;
      }));
      list.append(button);
    }
  }
  toggle.addEventListener("click", async () => {
    // permissions.request must remain directly within this user gesture.
    const grant = enabled ? api.permissions.remove({ permissions: ["webRequest"], origins: ["https://*/*", "http://*/*"] }) : api.permissions.request({ permissions: ["webRequest"], origins: ["https://*/*", "http://*/*"] });
    await action(toggle, async () => { const granted = await grant; status.textContent = granted ? strings.enabled : strings.denied; await show(); });
  });
  refresh.addEventListener("click", () => action(refresh, async () => { await show(); status.textContent = ""; }));
  show().catch(() => { toggle.textContent = strings.enable; });
})();
