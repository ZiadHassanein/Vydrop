// Offline regression fixture, isolated from the user's browser profiles.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
(async () => {
  const output = path.resolve(__dirname, '../build/playlist-captures'); fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    for (const [arabic, scheme, width] of [[false, 'light', 1200], [false, 'dark', 390], [true, 'light', 390], [true, 'dark', 1200]]) {
      const page = await browser.newPage({ viewport: { width, height: 720 }, colorScheme: scheme });
      await page.setContent('<button id="anchor">Download</button>');
      await page.addScriptTag({ path: path.resolve(__dirname, '../browser/common/quality-picker.js') });
      await page.evaluate(ar => { document.querySelector('#anchor').onclick = () => VydropQualityPicker.open({ anchor: document.querySelector('#anchor'), arabic: ar, playlistAvailable: true,
        inspect: async () => ({ title: 'Learn TypeScript In Arabic 2022', source: 'youtube.com', videos: [{ format: 'mp4', heights: [1080, 720, 480] }], audio: ['mp3'] }),
        send: async (output, scope) => { window.sent = { output, scope }; return { ok: true, code: 'started' }; } }); }, arabic);
      await page.locator('#anchor').click(); const dialog = page.getByRole('dialog');
      await dialog.getByRole('radio', { name: '720p', exact: true }).waitFor();
      await dialog.getByRole('radio', { name: arabic ? 'قائمة التشغيل كاملة' : 'Whole playlist', exact: true }).click();
      await dialog.getByRole('radio', { name: '720p', exact: true }).click();
      assert.equal(await dialog.evaluate(e => e.scrollWidth <= e.clientWidth), true);
      await page.screenshot({ path: path.join(output, `${arabic ? 'ar' : 'en'}-${scheme}-${width}.png`) });
      await dialog.getByRole('button', { name: arabic ? 'تنزيل قائمة التشغيل' : 'Download playlist', exact: true }).click();
      assert.deepEqual(await page.evaluate(() => sent), { output: { kind: 'video', format: 'mp4', height: 720 }, scope: 'playlist' });
      await page.close();
    }
    console.log('PASS: Playlist scope, quality delivery, compact bounds, English/Arabic and both themes.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
