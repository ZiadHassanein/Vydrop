// Isolated, offline headless Chrome. Pointer events affect this test page only.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const output = path.resolve(process.env.VYDROP_OVERLAY_CONTROLS_CAPTURE || path.join(root, 'build/overlay-controls'));

(async () => {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ headless: true, ...(process.env.VYDROP_BROWSER_EXECUTABLE
    ? { executablePath: process.env.VYDROP_BROWSER_EXECUTABLE } : { channel: process.env.VYDROP_BROWSER_CHANNEL || 'msedge' }) });
  try {
    async function fixture(url, language = 'en', width = 1100) {
      const page = await browser.newPage({ viewport: { width, height: 800 } });
      const errors = []; page.on('pageerror', error => errors.push(error.message));
      await page.route('**/*', route => route.fulfill({ contentType: 'text/html', body: `<!doctype html><html lang="${language}"><style>
        body{margin:0;background:#f7f8fa;min-height:1400px}main{margin:80px 24px;width:min(800px,calc(100vw - 48px))}
        video{display:block;width:100%;height:400px;background:#20242c}</style><main><video></video></main></html>` }));
      await page.goto(url);
      await page.evaluate(() => {
        window.sent = []; window.playerDuration = NaN;
        Object.defineProperties(document.querySelector('video'), {
          duration: { get: () => playerDuration }, readyState: { get: () => 0 }
        });
        window.chrome = { runtime: { id: 'fixture', getManifest: () => ({ name: 'Vydrop' }), sendMessage: async message => {
          if (message.type === 'vydrop-language') return { ok: true, language: document.documentElement.lang };
          sent.push(message); return { ok: true, code: 'opened' };
        } } };
        // Clipboard writes are intercepted in the fixture and never touch Windows.
        Object.defineProperty(navigator, 'clipboard', { value: { writeText: async () => {} } });
      });
      for (const file of ['connection.js', 'video-detection.js', 'video-overlay.js'])
        await page.addScriptTag({ path: path.join(root, 'browser/common', file) });
      return { page, action: page.locator('#vifree-video-action').getByRole('button'), errors };
    }
    for (const url of ['https://meet.google.com/abc-defg-hij', 'https://teams.microsoft.com/v2/',
      'https://us02web.zoom.us/wc/123/join', 'https://company.webex.com/meet/person']) {
      const { page, action, errors } = await fixture(url);
      await page.waitForTimeout(100);
      assert.equal(await action.isVisible(), false, url);
      assert.equal(await page.evaluate(() => sent.length), 0);
      assert.deepEqual(errors, []); await page.close();
    }
    console.log('PASS: Meet, Teams, Zoom and Webex show no download overlay and send no media requests.');

    const live = await fixture('https://example.com/watch');
    await live.action.waitFor();
    await live.page.evaluate(() => { document.querySelector('video').srcObject = new MediaStream(); });
    await live.action.waitFor({ state: 'hidden' });
    await live.page.evaluate(() => {
      document.querySelector('video').srcObject = null; playerDuration = Infinity;
      document.querySelector('video').dispatchEvent(new Event('durationchange'));
    });
    assert.equal(await live.action.isVisible(), false);
    await live.page.evaluate(() => {
      playerDuration = 120; document.querySelector('video').dispatchEvent(new Event('durationchange'));
    });
    await live.action.waitFor();
    await live.page.evaluate(() => {
      document.querySelector('main').className = 'ytp-live';
    });
    await live.action.waitFor({ state: 'hidden' });
    assert.equal(await live.page.evaluate(() => sent.length), 0);
    assert.deepEqual(live.errors, []); await live.page.close();
    console.log('PASS: MediaStream, unbounded live playback and active live flags hide the action; finite recorded playback restores it.');

    for (const [language, width] of [['en', 1100], ['ar', 390]]) {
      const { page, action, errors } = await fixture('https://example.com/watch', language, width);
      await action.waitFor();
      const start = await action.boundingBox();
      await page.mouse.move(start.x + start.width / 2, start.y + start.height / 2);
      await page.mouse.down();
      await page.mouse.move(start.x + start.width / 2 - 90, start.y + start.height / 2 + 140, { steps: 8 });
      await page.mouse.up();
      await page.waitForFunction(() => !document.querySelector('#vifree-video-action').shadowRoot.querySelector('button').dataset.dragging);
      const moved = await action.boundingBox();
      assert.ok(moved.y > start.y + 100 && moved.x < start.x - 20, 'Dragging actually moves the action');
      assert.equal(await page.evaluate(() => sent.length), 0, 'Dropping a moved button must not activate download');
      await page.screenshot({ path: path.join(output, `dragged-${language}.png`) });
      await page.evaluate(() => { document.querySelector('video').dispatchEvent(new Event('pause')); });
      await page.waitForTimeout(40);
      const paused = await action.boundingBox();
      assert.ok(Math.abs(paused.x - moved.x) < 2 && Math.abs(paused.y - moved.y) < 2, 'Playback events preserve the chosen position');
      await page.evaluate(() => scrollTo(0, 30)); await page.waitForTimeout(40);
      assert.ok(Math.abs((await action.boundingBox()).y - moved.y) < 2, 'A manually chosen browser position stays put while the player scrolls');
      await page.evaluate(() => scrollTo(0, 0)); await page.waitForTimeout(40);
      await action.focus(); await action.press('Shift+ArrowDown');
      await page.waitForTimeout(40);
      assert.ok((await action.boundingBox()).y >= moved.y + 14, 'Keyboard movement also works');
      await action.press('Shift+Home'); await page.waitForTimeout(40);
      assert.ok(Math.abs((await action.boundingBox()).y - start.y) < 2, 'Reset restores the default corner');
      const reset = await action.boundingBox();
      await page.mouse.move(reset.x + reset.width / 2, reset.y + reset.height / 2); await page.mouse.down();
      await page.mouse.move(2, 795, { steps: 8 }); await page.mouse.up(); await page.waitForTimeout(40);
      const edge = await action.boundingBox(), player = await page.locator('video').boundingBox();
      assert.ok(edge.x >= 0 && edge.x + edge.width <= width && edge.y > player.y + player.height && edge.y + edge.height <= 800,
        'The button can leave the player while staying inside the browser viewport');
      assert.equal(await page.evaluate(() => sent.length), 0);
      await page.setViewportSize({ width: width === 390 ? 340 : 900, height: 620 });
      await page.waitForTimeout(60);
      const resized = await action.boundingBox();
      assert.ok(resized.x >= 0 && resized.x + resized.width <= (width === 390 ? 340 : 900) && resized.y + resized.height <= 620);
      await action.click(); await page.waitForFunction(() => sent.length === 1);
      assert.equal(await page.evaluate(() => sent[0].url), 'https://example.com/watch');
      assert.deepEqual(errors, []); await page.close();
      console.log(`PASS: ${language}/${width}: free drag, no accidental download, playback/scroll persistence, keyboard move/reset, viewport bounds, resize and ordinary click.`);
    }
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
