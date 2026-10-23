// Opt-in MV3 integration check. Uses a fresh profile and synthetic LinkedIn DOM.
// VYDROP_EXTENSION and VYDROP_FIXTURE_MEDIA select a packaged companion and a local MP4.
// Native delivery is simulated; no signed-in profile, cookies or external requests are used.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const extension = path.resolve(process.env.VYDROP_EXTENSION || path.join(root, 'dist/Vydrop-browser/chromium'));
const media = process.env.VYDROP_FIXTURE_MEDIA;
const expectedVersion = process.env.VYDROP_EXPECTED_COMPANION || '1.5.1';
const asset = 'D4D05AQTestSelectedVideo';
const activity = 'https://www.linkedin.com/feed/update/urn:li:activity:7517135049939525632/';
const group = 'https://www.linkedin.com/groups/2066905/?q=highlightedFeedForGroups&highlightedUpdateUrn=urn%3Ali%3Aactivity%3A7517135049939525632';
const source = `https://dms.licdn.com/playlist/vid/v2/${asset}/mp4-720p-30fp-crf28/fixture/0/video?e=9999999999&v=beta&t=synthetic`;
const metadata = JSON.stringify({ included: [{ $type: 'com.linkedin.videocontent.VideoPlayMetadata', progressiveStreams: [{ mediaType: 'video/mp4', width: 478, height: 850, streamingLocations: [{ url: source }] }] }] });

(async () => {
  assert.ok(media && fs.statSync(media).isFile(), 'Set VYDROP_FIXTURE_MEDIA to a local MP4');
  const profile = fs.mkdtempSync(path.join(root, 'build/companion-source-profile-'));
  const context = await chromium.launchPersistentContext(profile, {
    channel: process.env.VYDROP_BROWSER_CHANNEL || 'msedge', headless: true,
    viewport: { width: 1440, height: 900 },
    args: ['--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  const results = [];
  try {
    const worker = context.serviceWorkers()[0] || await context.waitForEvent('serviceworker', { timeout: 10000 });
    assert.equal(await worker.evaluate(() => chrome.runtime.getManifest().version), expectedVersion);
    const expectedHost = (await worker.evaluate(() => chrome.runtime.getManifest().name)).endsWith(' Preview') ? 'com.vifree.preview' : 'com.vifree';
    await worker.evaluate(() => {
      globalThis.testNative = [];
      globalThis.testLookup = [];
      // Chromium can start an extension-created tab's first navigation before
      // Playwright attaches its fixture router. Attach on about:blank first;
      // the normal tabs API, content scripts and lookup messages remain real.
      const create = chrome.tabs.create.bind(chrome.tabs);
      chrome.tabs.create = async options => {
        const tab = await create({ ...options, url: 'about:blank' });
        await new Promise(resolve => setTimeout(resolve, 250));
        await chrome.tabs.update(tab.id, { url: options.url });
        return tab;
      };
      chrome.runtime.onMessage.addListener(message => { if (message.type === 'vifree-open-video') testLookup.push({ click: message }); });
      const send = chrome.tabs.sendMessage.bind(chrome.tabs);
      chrome.tabs.sendMessage = async (...args) => {
        try { const response = await send(...args); testLookup.push({ response }); return response; }
        catch (error) { testLookup.push({ error: error.message }); throw error; }
      };
      chrome.runtime.connectNative = undefined; // This detection-only fixture must never reach a registered local native host.
      chrome.runtime.sendNativeMessage = async (host, message) => { testNative.push({ host, message }); return { ok: true }; };
    });
    let lookupRequests = 0;
    await context.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (url.pathname === '/vydrop-fixture.mp4') return route.fulfill({ path: media, contentType: 'video/mp4' });
      if (url.origin === 'https://www.linkedin.com' && url.pathname === '/groups/2066905/' && url.searchParams.get('highlightedUpdateUrn') === 'urn:li:activity:7517135049939525632') {
        lookupRequests++;
        return route.fulfill({ contentType: 'text/html', body: `<!doctype html><html><body><code>${metadata}</code></body></html>` });
      }
      if (url.origin === 'https://www.linkedin.com' && url.pathname === '/feed/') {
        const direct = url.searchParams.get('fixture') === 'hydrated-post';
        return route.fulfill({ contentType: 'text/html', body: `<!doctype html><html lang="en"><style>
          body{margin:0;font:14px system-ui}main{margin:100px 40px;width:640px}video{display:block;width:640px;height:400px;background:#222}.vjs-poster{display:none}
          </style><main role="list"><div role="listitem"><div><video muted playsinline src="/vydrop-fixture.mp4"></video><div class="vjs-poster"><img src="https://media.licdn.com/dms/image/v2/${asset}/videocover-high/fixture"></div></div><a href="${group.replaceAll('&', '&amp;')}">This video post</a></div></main>${direct ? `<code>${metadata}</code>` : ''}</html>` });
      }
      return route.abort();
    });
    for (const scenario of ['hydrated-post', 'modern-feed']) {
      const page = await context.newPage();
      const created = [];
      const onPage = helper => { if (helper !== page) created.push(helper); };
      context.on('page', onPage);
      const beforeLookup = lookupRequests;
      try {
        await page.goto(`https://www.linkedin.com/feed/?fixture=${scenario}`);
        await page.locator('video').evaluate(v => v.play());
        const button = page.locator('#vifree-video-action button');
        await button.waitFor({ state: 'visible', timeout: 10000 });
        const player = await page.locator('video').boundingBox(), box = await button.boundingBox();
        assert.ok(box.y + box.height <= player.y || box.x >= player.x + player.width, 'the action stays outside the video');
        await button.click();
        await page.waitForFunction(() => ['success', 'error'].includes(document.querySelector('#vifree-video-action').shadowRoot.querySelector('button').dataset.state), null, { timeout: 22000 });
        assert.equal(await button.getAttribute('data-state'), 'success', await button.getAttribute('title'));
        const sent = await worker.evaluate(() => testNative.at(-1));
        if (sent.message.url !== source) console.log(JSON.stringify({ scenario, lookupRequests, created: created.map(p => ({ url: p.url(), closed: p.isClosed() })), lookup: await worker.evaluate(() => testLookup) }));
        assert.equal(sent.host, expectedHost);
        assert.equal(sent.message.url, source, 'native delivery receives only the exact selected progressive media');
        assert.equal(sent.message.type, 'open-link');
        assert.equal(page.isClosed(), false, 'the user video tab remains open');
        assert.equal(lookupRequests - beforeLookup, scenario === 'modern-feed' ? 1 : 0);
        assert.equal(created.length, scenario === 'modern-feed' ? 1 : 0);
        assert.ok(created.every(helper => helper.isClosed()), 'the extension removes its temporary lookup tab');
        results.push({ scenario, exactMedia: true, helperTabsClosed: created.length, nativeDelivery: 'simulated' });
        console.log(`PASS: packaged MV3 ${scenario}: exact media, outside button and temporary-tab lifecycle`);
      } finally { context.off('page', onPage); await page.close(); }
    }
    fs.writeFileSync(path.join(root, 'build/linkedin-source-mv3-results.json'), JSON.stringify({ version: expectedVersion, results }, null, 2));
  } finally { await context.close(); console.log('Disposable test profile: ' + profile); }
})().catch(error => { console.error(error); process.exitCode = 1; });
