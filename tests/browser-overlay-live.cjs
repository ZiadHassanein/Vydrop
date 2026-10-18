// Opt-in headless regression: VYDROP_PLAYWRIGHT may point to an installed Playwright module.
// Playback and the native bridge are simulated; no personal browser profile is opened.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const activity = 'https://www.linkedin.com/feed/update/urn:li:activity:7517227738701901824/';

(async () => {
  const browser = await chromium.launch({ channel: process.env.VYDROP_BROWSER_CHANNEL || 'msedge', headless: true });
  let passed = 0;
  try {
    for (const [site, lang, width, theater, crowdedHeader] of [['youtube', 'en', 1440], ['youtube', 'en', 1440, true], ['youtube', 'en', 1440, false, true], ['youtube', 'ar', 1440, false, true], ['linkedin', 'en', 1440], ['linkedin', 'ar', 390]]) {
      const context = await browser.newContext({ viewport: { width, height: 900 } });
      const url = site === 'youtube' ? 'https://www.youtube.com/watch?v=jNQXAC9IVRw' : 'https://www.linkedin.com/feed/';
      const html = `<!doctype html><html lang="${lang}"><style>
        body{margin:0;background:#fff;color:#222;font:14px system-ui}header{height:56px;position:fixed;inset:0 0 auto;background:#fafafa}
        input{position:absolute;left:150px;top:8px;width:850px;height:36px}header button{position:absolute;right:16px;top:8px;width:110px;height:36px}main{margin:${theater ? '56px 0 0' : '68px 0 0 16px'};width:${theater ? '100vw' : 'min(996px,calc(100vw - 32px))'}}
        video{display:block;width:100%;height:560px;background:#222}aside{position:absolute;left:1028px;top:68px;width:250px;height:500px;background:#eee}
        @media(max-width:600px){header,aside{display:none}main{margin-top:100px}video{height:400px}}
        ${crowdedHeader ? 'header input{left:355px;width:655px}header button{display:none}.header-mic,.header-create,.header-notifications{position:absolute;top:8px;height:40px}.header-mic{left:1028px;width:44px}.header-create{left:1180px;width:100px}.header-notifications{left:1300px;width:40px}' : ''}
        ${site === 'linkedin' && width > 600 ? 'header input{left:16px;width:calc(100vw - 32px)}aside{left:1020px}aside button{height:110px;width:250px}' : ''}
        </style><header><input aria-label="Search"><button>Sign in</button></header><main><div role="listitem"><div><video></video></div><a href="https://www.linkedin.com/groups/2066905/?q=highlightedFeedForGroups&highlightedUpdateUrn=urn%3Ali%3Aactivity%3A7517227738701901824">Video post</a></div></main><aside><button>Recommended video</button></aside>
        ${crowdedHeader ? '<nav><button class="header-mic" aria-label="Voice search"></button><button class="header-create">Create</button><button class="header-notifications" aria-label="Notifications"></button></nav>' : ''}
        <div role="dialog" style="position:fixed;right:0;bottom:0;width:200px;height:60px">Nonmodal app suggestion</div>
        <div role="dialog" aria-modal="true" aria-hidden="true" style="width:200px;height:200px">Inactive sign-in modal</div></html>`;
      await context.route('**/*', route => route.fulfill({ status: 200, contentType: 'text/html', body: html }));
      const page = await context.newPage();
      await page.goto(url);
      await page.evaluate(() => {
        globalThis.testReady = 1; globalThis.testPaused = false; globalThis.testSent = [];
        const video = document.querySelector('video');
        Object.defineProperties(video, { readyState: { get: () => testReady }, paused: { get: () => testPaused }, currentSrc: { get: () => 'blob:synthetic-player' } });
        globalThis.testResponse = { ok: true };
        globalThis.browser = { runtime: { sendMessage: async value => { testSent.push(value); return testResponse; } } };
        Object.defineProperty(navigator, 'clipboard', { value: { writeText: async value => { globalThis.testCopied = value; } }, configurable: true });
      });
      for (const file of ['connection.js', 'video-detection.js', 'video-overlay.js']) await page.addScriptTag({ path: path.join(root, 'browser/common', file) });
      const button = page.locator('#vifree-video-action').getByRole('button');
      assert.equal(await button.isVisible(), false);
      // No mutation, scrolling or resizing: only the post-buffering media event can reveal it.
      await page.evaluate(() => { testReady = 4; document.querySelector('video').dispatchEvent(new Event('playing', { bubbles: false })); });
      await button.waitFor({ state: 'visible' });
      const box = await button.boundingBox(), videoBox = await page.locator('video').boundingBox();
      assert.ok(box.y + box.height <= videoBox.y || box.x >= videoBox.x + videoBox.width, 'button stays outside the picture');
      if (site === 'linkedin' && width > 600) {
        const sidebarControl = await page.locator('aside button').boundingBox();
        assert.ok(box.y >= sidebarControl.y + sidebarControl.height, 'the action finds a gap below the sidebar control');
        assert.ok(box.y <= videoBox.y + 120, 'the fallback stays near the upper-right corner');
      }
      const search = await page.locator('input').boundingBox();
      if (width > 600) assert.ok(box.x >= search.x + search.width || box.x + box.width <= search.x || box.y >= search.y + search.height || box.y + box.height <= search.y, 'button avoids the search control');
      if (crowdedHeader) {
        assert.equal(box.width, 36, 'a compact V fits the gap when the full label cannot');
        assert.equal(await button.getAttribute('aria-label'), lang === 'ar' ? 'تنزيل بواسطة Vydrop' : 'Download by Vydrop');
        for (const controls of ['.header-mic', '.header-create', '.header-notifications']) {
          const control = await page.locator(controls).boundingBox();
          assert.ok(box.x + box.width <= control.x || box.x >= control.x + control.width || box.y + box.height <= control.y || box.y >= control.y + control.height, 'the V never covers a host control');
        }
        await button.focus();
        assert.equal(await button.evaluate(b => b.matches(':focus-visible')), true);
        if (process.env.VYDROP_OVERLAY_CAPTURES) {
          fs.mkdirSync(process.env.VYDROP_OVERLAY_CAPTURES, { recursive: true });
          await page.screenshot({ path: path.join(process.env.VYDROP_OVERLAY_CAPTURES, `youtube-compact-${lang}.png`) });
        }
      }
      assert.equal(await page.evaluate(() => testSent.length), 0);
      await button.click();
      await page.waitForFunction(() => testSent.length === 1);
      const sent = await page.evaluate(() => ({ url: testSent[0].url, copied: testCopied }));
      const expected = site === 'youtube' ? url : activity;
      assert.equal(sent.url, expected); assert.equal(sent.copied, expected);
      assert.match(await page.locator('#vifree-video-action').locator('[role="status"]').textContent(), /Link copied and video opened|تم نسخ الرابط وفتح الفيديو/u);
      if (site === 'linkedin' && lang === 'en') {
        await page.evaluate(() => { testResponse = { ok: false, code: 'busy' }; });
        await button.click();
        await page.waitForFunction(() => document.querySelector('#vifree-video-action').shadowRoot.querySelector('button').dataset.state === 'error');
        assert.match(await button.getAttribute('title'), /Vydrop is busy/u);
        await page.waitForTimeout(4700);
        assert.equal(await button.getAttribute('data-state'), 'error', 'failed retries never retain the previous success state');
        assert.match(await button.getAttribute('title'), /Vydrop is busy/u, 'the error remains readable after the old feedback timeout');
        await page.evaluate(() => { testResponse = { ok: true }; });
        await button.click();
        await page.waitForFunction(() => document.querySelector('#vifree-video-action').shadowRoot.querySelector('button').dataset.state === 'success');
        assert.equal(await button.getAttribute('title'), 'Copy video link and open Vydrop');
      }
      if (site === 'linkedin' || site === 'youtube' && !theater && !crowdedHeader) {
        for (const code of ['missing', 'ambiguous', 'inspect', 'busy', 'unavailable', 'invalid', 'unknown', 'legacy']) {
          const before = await page.evaluate(() => testSent.length);
          await page.evaluate(({ code, expected }) => {
            globalThis.originalInspector ??= ViFreeVideoDetection.inspectVideoPage;
            ViFreeVideoDetection.inspectVideoPage = () => {
              if (code === 'inspect') throw new Error('RAW PRIVATE ERROR');
              if (code === 'missing' || code === 'ambiguous') return { url: '', reason: code };
              return { url: expected };
            };
            testResponse = { ok: false, code: code === 'legacy' ? undefined : code, message: 'RAW PRIVATE ERROR' };
          }, { code, expected });
          await button.click();
          await page.waitForFunction(() => document.querySelector('#vifree-video-action').shadowRoot.querySelector('button').dataset.state === 'error');
          const message = await button.getAttribute('title');
          assert.doesNotMatch(message, /RAW PRIVATE/u);
          assert.match(message, lang === 'ar' ? /[\u0600-\u06ff]/u : /video|page|Vydrop/u);
          assert.equal(await page.evaluate(() => testSent.length), before + (['missing', 'ambiguous', 'inspect'].includes(code) ? 0 : 1));
          if (['missing', 'ambiguous', 'inspect'].includes(code)) assert.doesNotMatch(message, /Browser integration|الربط بالمتصفح/u);
        }
        await page.evaluate(() => { ViFreeVideoDetection.inspectVideoPage = originalInspector; testResponse = { ok: true }; });
        await button.click();
        await page.waitForFunction(() => document.querySelector('#vifree-video-action').shadowRoot.querySelector('button').dataset.state === 'success');
      }
      await page.evaluate(() => {
        const modal = document.createElement('div'); modal.role = 'dialog'; modal.setAttribute('aria-modal', 'true'); modal.id = 'test-modal';
        modal.style.cssText = 'position:fixed;inset:100px;width:200px;height:200px;background:white'; document.body.append(modal);
      });
      await button.waitFor({ state: 'hidden' });
      await page.locator('#test-modal').evaluate(e => e.remove());
      await button.waitFor({ state: 'visible' });
      if (crowdedHeader) {
        await page.locator('nav').evaluate(e => e.remove());
        await page.locator('input').evaluate(e => { e.style.left = '20px'; e.style.width = '100px'; });
        await page.evaluate(() => dispatchEvent(new Event('resize')));
        await page.waitForFunction(() => document.querySelector('#vifree-video-action').dataset.compact === 'false');
        assert.ok((await button.boundingBox()).width > 100, 'the full label returns when space is available');
      }
      await page.evaluate(() => { testPaused = true; document.querySelector('video').dispatchEvent(new Event('pause')); });
      await button.waitFor({ state: 'hidden' });
      if (site === 'youtube' && lang === 'en' && !theater && !crowdedHeader) {
        const samples = await page.evaluate(async () => {
          const fixture = document.createElement('section');
          for (let index = 0; index < 99; index++) {
            const item = document.createElement('video');
            item.style.cssText = 'width:320px;height:180px;display:block';
            Object.defineProperties(item, { readyState: { get: () => 4 }, paused: { get: () => false }, ended: { get: () => false } });
            fixture.append(item);
          }
          document.body.append(fixture);
          testPaused = false;
          const original = requestAnimationFrame, timings = [];
          globalThis.requestAnimationFrame = callback => original(time => {
            const start = performance.now(); callback(time); timings.push(performance.now() - start);
          });
          try {
            for (let index = 0; index < 30; index++) {
              dispatchEvent(new Event('resize'));
              await new Promise(resolve => original(() => original(resolve)));
            }
          } finally { globalThis.requestAnimationFrame = original; fixture.remove(); }
          return timings;
        });
        samples.sort((a, b) => a - b);
        console.log(`MEASURE: 100-player overlay, ${samples.length} callbacks: p95=${samples[Math.floor(samples.length * .95)].toFixed(2)}ms, max=${samples.at(-1).toFixed(2)}ms`);
      }
      console.log(`PASS: ${site}/${lang}/${width}${theater ? '/theater' : ''}${crowdedHeader ? '/crowded-header' : ''}: delayed playback, outside placement, exact-player click and pause`); passed++;
      await context.close();
    }
    console.log(`${passed} headless overlay scenarios passed; native delivery was simulated.`);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
