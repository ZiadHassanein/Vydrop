// Offline player/advertisement fixture. No personal profile, media or native download is used.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const frameUrl = 'https://geo.dailymotion.com/player/xtv3w.html?';
    const watch = 'https://www.dailymotion.com/video/x73dk0q';
    const inspected = [], delivered = [];
    let handler;
    const api = { runtime: { id: 'fixture', onMessage: { addListener: fn => { handler = fn; } }, onInstalled: { addListener() {} } }, contextMenus: { onClicked: { addListener() {} } } };
    const background = vm.createContext({ URL, ViFreeConnection: { api, name: 'Vydrop',
      inspectLink: async url => { inspected.push(url); return { title: 'Main video only', source: 'dailymotion.com', videos: [{ format: 'mp4', heights: [720] }], audio: ['m4a'] }; },
      sendLink: async (url, output) => { delivered.push({ url, output }); return { code: 'started' }; } } });
    for (const file of ['video-detection.js', 'background.js']) vm.runInContext(fs.readFileSync(path.join(root, 'browser/common', file), 'utf8'), background);
    const context = await browser.newContext({ viewport: { width: 1100, height: 800 } });
    await context.exposeFunction('sendFixtureMessage', message => new Promise(resolve => handler(message, { id: 'fixture', frameId: 1, url: frameUrl, tab: { id: 7, url: watch } }, resolve)));
    await context.route('**/*', route => route.fulfill({ contentType: 'text/html', body: route.request().url() === watch
      ? `<iframe title="Dailymotion video player" src="${frameUrl}" style="width:1000px;height:700px;border:0"></iframe>`
      : '<html lang="en"><style>body{margin:0;background:#111}video{position:absolute;inset:40px;width:800px;height:450px;background:#333}</style><main><div class="video_view"><video id="content"></video></div><div class="ad-container"><video id="advertisement"></video></div></main></html>' }));
    const page = await context.newPage(); await page.goto(watch);
    const frame = page.frames().find(item => item.url() === frameUrl); assert.ok(frame);
    await frame.evaluate(() => {
      // Chrome content-script frames can expose a null custom-element registry.
      Object.defineProperty(window, 'customElements', { value: null, configurable: true });
      window.adPlaying = true;
      for (const video of document.querySelectorAll('video')) Object.defineProperties(video, {
        readyState: { get: () => 4 }, ended: { get: () => false },
        paused: { get: () => video.id === 'advertisement' ? !adPlaying : adPlaying },
        currentSrc: { get: () => video.id === 'advertisement' ? 'https://ads.example/commercial.mp4' : 'blob:https://geo.dailymotion.com/content' }
      });
      window.browser = { runtime: { getManifest: () => ({ name: 'Vydrop' }), sendMessage: value => window.sendFixtureMessage(value) } };
    });
    for (const file of ['connection.js', 'video-detection.js', 'quality-picker.js', 'video-overlay.js']) await frame.addScriptTag({ path: path.join(root, 'browser/common', file) });
    await frame.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    const button = frame.locator('#vifree-video-action').getByRole('button');
    assert.equal(await button.isVisible(), false, 'the button stays hidden while only the ad is playing');
    assert.equal(inspected.length, 0); assert.equal(delivered.length, 0);
    await frame.evaluate(() => { adPlaying = false; document.querySelector('#content').dispatchEvent(new Event('playing')); });
    await button.click(); const picker = frame.getByRole('dialog');
    await picker.getByRole('radio', { name: '720p', exact: true }).waitFor();
    assert.deepEqual(inspected, [watch], 'format inspection receives the content page, never the ad stream');
    await picker.getByRole('button', { name: 'Download', exact: true }).click();
    assert.equal(delivered.length, 1); assert.equal(delivered[0].url, watch);
    assert.equal(delivered[0].output.output.height, 720);
    await frame.evaluate(() => { adPlaying = true; document.querySelector('#content').dispatchEvent(new Event('pause')); document.querySelector('#advertisement').dispatchEvent(new Event('playing')); });
    await button.waitFor({ state: 'hidden' });
    assert.equal(delivered.length, 1, 'returning ads cannot create another request');
    await context.close();
    console.log('PASS: Dailymotion ad playback hides the button; only the main watch page reaches inspection/download, including ad transitions. Native delivery was simulated.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
