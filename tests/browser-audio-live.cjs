// Offline, isolated headless browser. No personal profile, clipboard or native app is used.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const output = path.resolve(process.env.VYDROP_AUDIO_CAPTURE || path.join(root, 'build/audio-overlay'));

(async () => {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ headless: true, ...(process.env.VYDROP_BROWSER_EXECUTABLE
    ? { executablePath: process.env.VYDROP_BROWSER_EXECUTABLE } : { channel: process.env.VYDROP_BROWSER_CHANNEL || 'msedge' }) });
  try {
    async function fixture({ custom = false, language = 'en', theme = 'light', width = 1100, url = 'https://music.example.com/track/one', markup } = {}) {
      const page = await browser.newPage({ viewport: { width, height: 800 }, colorScheme: theme });
      const errors = []; page.on('pageerror', error => errors.push(error.message));
      const audio = '<audio controls preload="none"><source src="https://cdn.example.com/track-one.mp3" type="audio/mpeg"></audio>';
      const player = custom ? '<div class="audio-player"><audio preload="none" src="https://cdn.example.com/track-one.mp3" style="display:none"></audio><button>Play audio</button><input type="range" aria-label="Position"></div>' : audio;
      await page.route('**/*', route => route.fulfill({ contentType: 'text/html', body: `<!doctype html><html lang="${language}"><style>
        body{margin:0;background:${theme === 'dark' ? '#17191e' : '#f7f8fa'};color:${theme === 'dark' ? '#f1f3f5' : '#20242c'};font:16px system-ui}
        main{margin:150px 24px;width:min(520px,calc(100vw - 48px))}h1{font-size:24px;margin-bottom:75px}
        audio{display:block;width:min(320px,100%);height:54px;margin-top:55px}.audio-player{padding:20px;border:1px solid #84909f;border-radius:10px}
        .audio-player button{padding:12px}.audio-player input{width:60%;margin:16px 8px}</style><main><h1>Audio track</h1>${markup ?? player}</main></html>` }));
      await page.goto(url);
      await page.evaluate(() => {
        window.requests = [];
        window.chrome = { runtime: { getManifest: () => ({ name: 'Vydrop' }), sendMessage: async message => {
          if (message.type === 'vydrop-language') return { ok: true, language: document.documentElement.lang };
          requests.push(message);
          return message.type === 'vydrop-inspect-video' ? { ok: true, media: {
            title: 'Selected audio track', source: 'music.example.com', videos: [{ format: 'mp4', heights: [720] }], audio: ['mp3', 'm4a']
          } } : { ok: true, code: 'queued' };
        } } };
      });
      for (const file of ['connection.js', 'video-detection.js', 'quality-picker.js', 'video-overlay.js'])
        await page.addScriptTag({ path: path.join(root, 'browser/common', file) });
      return { page, action: page.locator('#vifree-video-action').getByRole('button'), errors };
    }
    for (const [custom, language, theme, width] of [[false, 'en', 'light', 1100], [false, 'ar', 'dark', 390],
      [true, 'en', 'dark', 1100], [true, 'ar', 'light', 390]]) {
      const { page, action, errors } = await fixture({ custom, language, theme, width });
      await action.waitFor();
      assert.equal(await page.evaluate(() => document.querySelector('audio').paused), true);
      assert.equal(await page.evaluate(() => requests.length), 0, 'Seeing an audio player never inspects or downloads it automatically');
      const bounds = await action.boundingBox(), player = await page.locator(custom ? '.audio-player' : 'audio').boundingBox();
      assert.ok(bounds.y + bounds.height < player.y || bounds.y > player.y + player.height, 'The default action does not cover playback controls');
      assert.match(await action.getAttribute('aria-description'), language === 'ar' ? /المقطع الصوتي/u : /Open audio/u);
      await page.screenshot({ path: path.join(output, `${custom ? 'custom' : 'native'}-${language}-${theme}-button.png`) });
      await action.click();
      const dialog = page.getByRole('dialog');
      const download = dialog.getByRole('button', { name: language === 'ar' ? 'تنزيل' : 'Download', exact: true });
      await download.waitFor();
      assert.equal(await dialog.getByRole('button', { name: language === 'ar' ? 'فيديو' : 'Video', exact: true }).count(), 0);
      assert.equal(await dialog.getByRole('button', { name: language === 'ar' ? 'صوت' : 'Audio', exact: true }).getAttribute('aria-pressed'), 'true');
      await dialog.getByRole('radio', { name: 'M4A', exact: true }).click();
      const inspection = await page.evaluate(() => requests[0]);
      assert.equal(inspection.type, 'vydrop-inspect-video');
      assert.equal(inspection.url, 'https://cdn.example.com/track-one.mp3');
      await page.screenshot({ path: path.join(output, `${custom ? 'custom' : 'native'}-${language}-${theme}-formats.png`) });
      await download.click();
      await page.waitForFunction(() => requests.some(message => message.type === 'vifree-open-video'));
      const delivery = await page.evaluate(() => requests.find(message => message.type === 'vifree-open-video'));
      assert.equal(delivery.url, inspection.url); assert.deepEqual(delivery.output, { kind: 'audio', format: 'm4a', height: 0 });
      assert.equal(await page.evaluate(() => requests.filter(message => message.type === 'vydrop-inspect-video').length), 1);
      assert.deepEqual(errors, []); await page.close();
      console.log(`PASS: ${custom ? 'custom' : 'native'}/${language}/${theme}/${width}: unplayed audio action, unobstructed controls, audio-only formats and exact selected-source delivery.`);
    }

    const mixed = await fixture({ markup: '<audio controls preload="none" src="https://cdn.example.com/one.mp3"></audio><audio controls preload="none" src="https://cdn.example.com/two.mp3"></audio>' });
    await mixed.action.waitFor();
    await mixed.page.evaluate(() => {
      const current = document.querySelectorAll('audio')[1];
      Object.defineProperties(current, { paused: { get: () => false }, readyState: { get: () => 2 } });
      current.dispatchEvent(new Event('play'));
    });
    await mixed.page.waitForTimeout(50);
    await mixed.action.click(); await mixed.page.getByRole('dialog').getByRole('button', { name: 'Download', exact: true }).waitFor();
    assert.equal(await mixed.page.evaluate(() => requests[0].url), 'https://cdn.example.com/two.mp3');
    await mixed.page.keyboard.press('Escape');
    assert.deepEqual(mixed.errors, []); await mixed.page.close();
    console.log('PASS: An active audio track wins over another paused track without borrowing its source.');

    for (const url of ['https://meet.google.com/abc-defg-hij', 'https://music.example.com/radio']) {
      const { page, action, errors } = await fixture({ url });
      if (url.includes('/radio')) {
        await action.waitFor();
        await page.evaluate(() => {
          Object.defineProperty(document.querySelector('audio'), 'duration', { get: () => Infinity });
          document.querySelector('audio').dispatchEvent(new Event('durationchange'));
        });
      }
      await action.waitFor({ state: 'hidden' });
      assert.equal(await page.evaluate(() => requests.length), 0);
      assert.deepEqual(errors, []); await page.close();
    }
    console.log('PASS: Meeting audio and live radio do not show a download action or send media requests.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
