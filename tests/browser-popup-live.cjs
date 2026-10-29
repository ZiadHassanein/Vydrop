// Rendered popup fixtures use an isolated profile and simulated native replies.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const capture = process.env.VYDROP_POPUP_CAPTURES || path.join(root, 'build/popup-captures');
(async () => {
  fs.mkdirSync(capture, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    for (const language of ['en', 'ar']) for (const colorScheme of ['light', 'dark']) {
      const context = await browser.newContext({ viewport: { width: 340, height: 640 }, colorScheme });
      try {
        await context.addInitScript(({ language }) => {
          const fixture = globalThis.fixture = { requests: [], permissions: 0 };
          const tab = { id: 7, url: 'https://www.youtube.com/watch?v=jNQXAC9IVRw' };
          globalThis.chrome = {
            i18n: { getUILanguage: () => language },
            tabs: { query: async () => [tab] },
            scripting: { executeScript: async () => [{ frameId: 0, result: {url:tab.url, reason:'permalink'} }] },
            permissions: { request: async () => { fixture.permissions++; return true; } },
            runtime: {
              getManifest: () => ({ name: 'Vydrop' }),
              connectNative: host => {
                let receive;
                return { onMessage: { addListener: fn => { receive = fn; } }, onDisconnect: { addListener: () => {} }, disconnect() {}, postMessage(message) {
                  fixture.requests.push(message);
                  const result = message.type === 'ping' ? {code:'ready',capabilities:['formats','output-selection','start-download']} : message.type === 'inspect' ? {code:'formats',media:{title:'Selected video',source:'youtube.com',videos:[{format:'mp4',heights:[720,480]}],audio:['mp3']}} : {code:message.type === 'enqueue'?'queued':'started'};
                  queueMicrotask(() => receive({ ok: true, protocol: 2, requestId: message.requestId, ...result }));
                } };
              },
              sendNativeMessage: async () => { throw new Error('Unexpected legacy delivery'); }
            }
          };
        }, { language });
        await context.route('**/*', route => {
          const url = new URL(route.request().url()), file = path.basename(url.pathname);
          if (url.hostname !== 'popup.vydrop.test' || !/^(?:popup\.html|popup\.css|connection\.js|video-detection\.js|quality-picker\.js|popup\.js)$/u.test(file)) return route.abort();
          return route.fulfill({ path: path.join(root, 'browser/common', file), contentType: file.endsWith('.html') ? 'text/html' : file.endsWith('.css') ? 'text/css' : 'application/javascript' });
        });
        const page = await context.newPage(), errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto('https://popup.vydrop.test/popup.html');
        assert.equal(await page.locator('html').getAttribute('lang'), language);
        assert.equal(await page.evaluate(() => fixture.requests.filter(request => request.type !== 'ping').length), 0,
          'opening the popup may read the app language but never inspects, opens, queues or downloads media');
        assert.equal(await page.evaluate(() => fixture.permissions), 0);
        assert.equal(await page.locator('button').count(),2);
        assert.equal(await page.locator('#test,details,#discovery,#streams').count(),0);
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        await page.screenshot({ path: path.join(capture, `popup-${language}-${colorScheme}.png`), fullPage: true });
        await page.locator('#enqueue').click();
        await page.waitForFunction(() => /Added|أُضيف/u.test(document.querySelector('#status').textContent));
        const queued = await page.evaluate(() => fixture.requests.at(-1));
        assert.equal(queued.type,'enqueue');assert.equal(queued.url,'https://www.youtube.com/watch?v=jNQXAC9IVRw');
        assert.equal(queued.sourcePage,queued.url);assert.equal(await page.locator('#enqueue').isEnabled(),true);
        await page.locator('#send').click();
        const dialog=page.getByRole('dialog');await dialog.getByRole('radio',{name:'480p',exact:true}).click();
        assert.equal(await page.evaluate(()=>fixture.requests.filter(r=>r.type==='download').length),0,'inspection never starts a download');
        await dialog.getByRole('button',{name:language==='ar'?'تنزيل':'Download',exact:true}).click();
        await page.waitForFunction(()=>fixture.requests.some(r=>r.type==='download'));
        const sent=await page.evaluate(()=>fixture.requests.at(-1));
        assert.equal(sent.type,'download');assert.deepEqual(sent.output,{kind:'video',format:'mp4',height:480});
        assert.equal(sent.url,queued.url);assert.equal(await page.locator('#send').isEnabled(),true);
        assert.deepEqual(errors, []);
        console.log(`PASS: ${language}/${colorScheme} popup: two actions, no permission request, exact queue and confirmed quality delivery`);
      } finally { await context.close(); }
    }
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
