// Isolated MV3 regression. Public URLs are served by local fixtures; native delivery is simulated.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const expectedVersion = fs.readFileSync(path.join(root, 'src/Downloader.Infrastructure/Browser/BrowserIdentity.cs'), 'utf8').match(/CompanionVersion = "([0-9.]+)"/u)[1];
const episode = 'https://v2.kirmizi.cfd/episode/muhtemel-ask-19/';
const wrapper = 'https://w.anaplayer.online/albaplayer/fixture/';
const player = 'https://anafast.space/embed-j14ddsm8ejnx.html';
const stream = 'https://cdn.example/main/master.m3u8?token=expired';
(async () => {
  const profile = fs.mkdtempSync(path.join(root, 'build/anafast-profile-'));
  const extension = path.join(root, 'dist/Vydrop-browser/chromium');
  const context = await chromium.launchPersistentContext(profile, {
    ...(process.env.VYDROP_CHROMIUM_EXECUTABLE ? { executablePath: process.env.VYDROP_CHROMIUM_EXECUTABLE } : { channel: 'msedge' }),
    headless: true, viewport: { width: 1440, height: 1000 },
    args: ['--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  try {
    const worker = context.serviceWorkers()[0] || await context.waitForEvent('serviceworker');
    assert.equal(await worker.evaluate(() => chrome.runtime.getManifest().version), expectedVersion);
    await worker.evaluate(() => {
      globalThis.nativeRequests = [];
      const reply = request => {
        nativeRequests.push(request);
        const response = { ok: true, protocol: 2, requestId: request.requestId, code: 'started', capabilities: ['formats', 'output-selection', 'start-download'] };
        if (request.type === 'ping') response.code = 'ready';
        if (request.type === 'inspect') {
          response.code = 'formats';
          response.media = { title: 'AnaFast main episode fixture', source: 'anafast.space', videos: [{ format: 'mp4', heights: [480, 360] }], audio: ['m4a'] };
        }
        return response;
      };
      chrome.runtime.connectNative = () => {
        const listeners = [];
        return { onMessage: { addListener: fn => listeners.push(fn) }, onDisconnect: { addListener() {} },
          postMessage: request => queueMicrotask(() => listeners.forEach(fn => fn(reply(request)))), disconnect() {} };
      };
    });
    await context.route('**/*', route => {
      const url = route.request().url();
      if (url.endsWith('/fixture.mp4')) return route.fulfill({ path: path.join(root, 'build/anafast-fixture.mp4'), contentType: 'video/mp4' });
      if (url === episode) return route.fulfill({ contentType: 'text/html', body: `<iframe src="${wrapper}" style="width:1300px;height:900px;border:0"></iframe>` });
      if (url === wrapper) return route.fulfill({ contentType: 'text/html', body: `<iframe src="${player}" style="width:1200px;height:850px;border:0"></iframe>` });
      if (url === player) return route.fulfill({ contentType: 'text/html', body: `<html lang="en"><title>AnaFast test</title><style>body{margin:0;background:#111}.jwplayer{margin:30px;width:1100px;height:700px}video{width:100%;height:100%}</style><div id="vplayer" class="jwplayer"><video src="/fixture.mp4" preload="auto"></video></div><script>function jwplayer(){return{setup(){}}}jwplayer("vplayer").setup({sources:[{file:"${stream}"}]});</script></html>` });
      return route.abort();
    });
    const page = await context.newPage(); await page.goto(episode);
    const frame = page.frames().find(f => f.url() === player); assert.ok(frame);
    const button = frame.locator('#vifree-video-action button'); await button.waitFor({ state: 'visible' });
    await button.click();
    const picker = frame.getByRole('dialog'); await picker.getByRole('radio', { name: '480p', exact: true }).waitFor();
    let requests = await worker.evaluate(() => nativeRequests);
    assert.equal(requests.find(r => r.type === 'inspect').url, player, 'native inspection receives the original player page');
    assert.equal(requests.some(r => r.url === stream), false, 'temporary CDN link never reaches the native host');
    await picker.getByRole('button', { name: 'Download', exact: true }).click();
    await page.waitForTimeout(300);
    requests = await worker.evaluate(() => nativeRequests);
    const download = requests.find(r => r.type === 'download');
    assert.equal(download.url, player); assert.equal(download.output.height, 480);
    assert.equal(requests.filter(r => r.type === 'download').length, 1);
    console.log('PASS: packaged MV3, two nested frames, visible paused-video action, original player for inspection and selected 480p download; native delivery simulated.');
  } finally { await context.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
