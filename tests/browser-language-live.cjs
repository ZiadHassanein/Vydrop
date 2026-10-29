// Isolated headless browser: no personal profile, desktop focus or physical input.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const output = path.join(root, 'build/audio-focus-20261029/language');
(async () => {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ ...(process.env.VYDROP_BROWSER_EXECUTABLE ? { executablePath: process.env.VYDROP_BROWSER_EXECUTABLE } : process.env.VYDROP_BROWSER_CHANNEL ? { channel: process.env.VYDROP_BROWSER_CHANNEL } : {}), headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1100, height: 850 }, colorScheme: 'dark' });
    const errors = []; page.on('pageerror', error => errors.push(error.message));
    await page.route('https://www.youtube.com/**', route => route.fulfill({ contentType: 'text/html', body: '<html lang="ar"><title>Language fixture</title><body><video style="display:block;margin:60px;width:800px;height:450px"></video></body></html>' }));
    await page.goto('https://www.youtube.com/watch?v=abcdefghijk&list=PLLanguageFixture');
    await page.evaluate(() => {
      window.appLanguage = 'en'; window.calls = []; window.sent = [];
      window.chrome = { runtime: { id: 'fixture', getManifest: () => ({ name: 'Vydrop' }),
        sendMessage: async message => {
          calls.push(message.type);
          if (message.type === 'vydrop-language') return { ok: true, language: appLanguage };
          if (message.type === 'vydrop-inspect-video') return { ok: true, media: { title: 'Unchanged media title', source: 'youtube.com', videos: [{ format: 'mp4', heights: [1080, 480] }, { format: 'webm', heights: [720, 480] }], audio: ['mp3', 'm4a'] } };
          sent.push(message); return { ok: true, code: 'queued' };
        } } };
    });
    for (const file of ['connection.js', 'video-detection.js', 'quality-picker.js', 'video-overlay.js']) await page.addScriptTag({ path: path.join(root, 'browser/common', file) });
    const overlay = page.locator('#vifree-video-action');
    await overlay.getByRole('button', { name: 'Download by Vydrop', exact: true }).waitFor();
    await overlay.getByRole('button', { name: 'Download by Vydrop', exact: true }).click();
    let dialog = page.getByRole('dialog');
    await dialog.getByRole('button', { name: 'Download', exact: true }).waitFor();
    await dialog.getByRole('radio', { name: 'WEBM', exact: true }).click();
    await dialog.getByRole('radio', { name: '480p', exact: true }).click();
    await dialog.getByRole('radio', { name: 'Whole playlist', exact: true }).click();
    await page.evaluate(() => { window.appLanguage = 'ar'; });
    await dialog.getByRole('button', { name: 'تنزيل قائمة التشغيل', exact: true }).waitFor();
    assert.equal(await dialog.getAttribute('dir'), 'rtl');
    assert.equal(await dialog.getByRole('radio', { name: 'WEBM', exact: true }).getAttribute('aria-checked'), 'true');
    assert.equal(await dialog.getByRole('radio', { name: '480p', exact: true }).getAttribute('aria-checked'), 'true');
    assert.equal(await dialog.getByRole('radio', { name: 'قائمة التشغيل كاملة', exact: true }).getAttribute('aria-checked'), 'true');
    assert.equal(await dialog.locator('.title').textContent(), 'Unchanged media title');
    await page.screenshot({ path: path.join(output, 'open-picker-ar.png') });
    await page.evaluate(() => { window.appLanguage = 'en'; });
    await dialog.getByRole('button', { name: 'Download playlist', exact: true }).waitFor();
    assert.equal(await dialog.getAttribute('dir'), 'ltr');
    await dialog.getByRole('button', { name: 'Download playlist', exact: true }).click();
    assert.deepEqual(await page.evaluate(() => sent.at(-1).output), { kind: 'video', format: 'webm', height: 480 });
    assert.equal(await page.evaluate(() => sent.at(-1).playlist), true);
    assert.equal(await page.evaluate(() => calls.filter(type => type === 'vydrop-inspect-video').length), 1);
    await page.screenshot({ path: path.join(output, 'overlay-en.png') });
    assert.deepEqual(errors, []);
    console.log('PASS: Arabic page follows app English, live AR/EN changes preserve WEBM/480p/playlist selection and send exactly that output without another inspection.');
    await page.close();
    const popup = await browser.newPage({ viewport: { width: 420, height: 700 } });
    popup.on('pageerror', error => errors.push(error.message));
    await popup.route('https://extension.example/**', route => { const name = new URL(route.request().url()).pathname.slice(1); return route.fulfill({ contentType: name.endsWith('.js') ? 'text/javascript' : name.endsWith('.css') ? 'text/css' : 'text/html', body: fs.readFileSync(path.join(root, 'browser/common', name || 'popup.html')) }); });
    await popup.addInitScript(() => {
      window.appLanguage = 'en';
      window.chrome = { i18n: { getUILanguage: () => 'ar' }, runtime: { getManifest: () => ({ name: 'Vydrop' }), connectNative: () => {
        let receive;
        return { onMessage: { addListener: fn => { receive = fn; } }, onDisconnect: { addListener: () => {} }, disconnect: () => {},
          postMessage: message => queueMicrotask(() => receive({ ok: true, requestId: message.requestId, language: appLanguage })) };
      } } };
    });
    await popup.goto('https://extension.example/popup.html');
    await popup.getByRole('button', { name: 'Open in Vydrop', exact: true }).waitFor();
    assert.equal(await popup.locator('html').getAttribute('dir'), 'ltr');
    await popup.evaluate(() => { window.appLanguage = 'ar'; });
    await popup.getByRole('button', { name: 'فتح في Vydrop', exact: true }).waitFor();
    assert.equal(await popup.locator('html').getAttribute('dir'), 'rtl');
    assert.equal(await popup.locator('#enqueue').textContent(), 'إضافة إلى قائمة التنزيل');
    await popup.evaluate(() => { window.appLanguage = 'en'; });
    await popup.getByRole('button', { name: 'Open in Vydrop', exact: true }).waitFor();
    assert.equal(await popup.locator('html').getAttribute('dir'), 'ltr');
    await popup.screenshot({ path: path.join(output, 'popup-en.png') });
    assert.deepEqual(errors, []);
    console.log('PASS: Arabic browser popup follows the app language and changes both actions and direction live in both directions.');
    await popup.close();
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
