// Opt-in rendered popup checks. Browser APIs and native replies are fixture-only;
// no user browser profile, native registration or external network is used.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const capture = process.env.VYDROP_POPUP_CAPTURES || path.join(root, 'build/popup-captures');

(async () => {
  fs.mkdirSync(capture, { recursive: true });
  const browser = await chromium.launch({ channel: process.env.VYDROP_BROWSER_CHANNEL || 'msedge', headless: true });
  try {
    for (const language of ['en', 'ar']) for (const colorScheme of ['light', 'dark']) {
      const context = await browser.newContext({ viewport: { width: 340, height: 640 }, colorScheme });
      try {
        await context.addInitScript(({ language }) => {
          const fixture = globalThis.fixture = { enabled: false, requests: [], running: false, protected: false, permissions: 0 };
          const tab = { id: 7, url: 'https://www.youtube.com/watch?v=jNQXAC9IVRw' };
          globalThis.chrome = {
            i18n: { getUILanguage: () => language },
            tabs: { query: async () => [tab] },
            scripting: { executeScript: async () => [{ result: fixture.protected }] },
            permissions: {
              request: async () => { fixture.permissions++; fixture.enabled = true; return true; },
              remove: async () => { fixture.enabled = false; return true; }
            },
            runtime: {
              getManifest: () => ({ name: 'Vydrop Preview' }),
              sendMessage: async () => ({ enabled: fixture.enabled, candidates: fixture.enabled ? [{ kind: 'media', name: 'A selected video.mp4', frameId: 0, url: 'https://cdn.example.com/video.mp4' }] : [] }),
              connectNative: host => {
                let receive; fixture.requests.push({ host });
                return { onMessage: { addListener: fn => { receive = fn; } }, onDisconnect: { addListener: () => {} }, disconnect() {}, postMessage(message) {
                  fixture.requests.push(message);
                  queueMicrotask(() => receive({ ok: true, protocol: 2, requestId: message.requestId, code: message.type === 'ping' ? 'ready' : 'queued', appRunning: fixture.running }));
                } };
              },
              sendNativeMessage: async () => { throw new Error('Unexpected legacy delivery'); }
            }
          };
        }, { language });
        await context.route('**/*', route => {
          const url = new URL(route.request().url());
          const file = path.basename(url.pathname);
          if (url.hostname !== 'popup.vydrop.test' || !/^(?:popup\.html|popup\.css|connection\.js|video-detection\.js|popup\.js|popup-discovery\.js)$/u.test(file)) return route.abort();
          return route.fulfill({ path: path.join(root, 'browser/common', file), contentType: file.endsWith('.html') ? 'text/html' : file.endsWith('.css') ? 'text/css' : 'application/javascript' });
        });
        const page = await context.newPage();
        const errors = []; page.on('pageerror', error => errors.push(error.message));
        await page.goto('https://popup.vydrop.test/popup.html');
        assert.equal(await page.locator('html').getAttribute('lang'), language);
        assert.equal(await page.evaluate(() => fixture.requests.length), 0, 'opening the popup does not launch the native host');
        await page.locator('#test').click();
        await page.waitForFunction(() => document.querySelector('#status').textContent.includes('ready') || document.querySelector('#status').textContent.includes('متاح'));
        assert.equal(await page.evaluate(() => fixture.requests.filter(r => r.type && r.type !== 'ping').length), 0, 'a health test never sends a media URL');
        await page.evaluate(() => { fixture.running = true; });
        await page.locator('#test').click();
        await page.waitForFunction(() => /verified|التحقق/u.test(document.querySelector('#status').textContent));
        await page.locator('#enqueue').click();
        await page.waitForFunction(() => /Added|أُضيف/u.test(document.querySelector('#status').textContent));
        assert.equal(await page.evaluate(() => fixture.requests.at(-1).type), 'enqueue');
        await page.locator('summary').click();
        await page.locator('#discovery').click();
        await page.locator('#streams button').waitFor();
        assert.equal(await page.evaluate(() => fixture.permissions), 1, 'discovery permission requires the explicit toggle');
        await page.locator('#streams button').click();
        await page.waitForFunction(() => fixture.requests.at(-1).url === 'https://cdn.example.com/video.mp4');
        assert.equal(await page.evaluate(() => fixture.requests.at(-1).sourcePage), 'https://www.youtube.com/watch?v=jNQXAC9IVRw');
        await page.evaluate(() => { fixture.protected = true; });
        const beforeProtected = await page.evaluate(() => fixture.requests.length);
        await page.locator('#streams button').click();
        await page.waitForFunction(() => /protected|محمي/u.test(document.querySelector('#status').textContent));
        assert.equal(await page.evaluate(() => fixture.requests.length), beforeProtected, 'a protected player never sends a native request');
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'localized controls fit without horizontal overflow');
        await page.screenshot({ path: path.join(capture, `popup-${language}-${colorScheme}.png`), fullPage: true });
        await page.locator('#discovery').click();
        await page.waitForFunction(() => document.querySelector('#streams').children.length === 0);
        assert.deepEqual(errors, []);
        console.log(`PASS: rendered ${language}/${colorScheme} popup: health, queue, opt-in discovery, protection and layout`);
      } finally { await context.close(); }
    }
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
