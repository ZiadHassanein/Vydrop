// Offline rendered LinkedIn menu regression; no account, cookies or media requests.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const post = 'https://www.linkedin.com/embed/feed/update/urn:li:ugcPost:7520516702044999680/';
const detect = page => page.evaluate(() => ViFreeVideoDetection.inspectVideoPage('', true, document.querySelector('video')));
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  let count = 0;
  try {
    for (const mode of ['immediate', 'delayed', 'nested', 'delayed-nested', 'no-embed']) {
      const page = await browser.newPage();
      await page.route('**/*', route => route.fulfill({ contentType: 'text/html', body: `<!doctype html><html><style>video{width:620px;height:380px}</style>
        <nav role="menu">Unrelated sidebar</nav><div role="listitem"><div><button id="toggle" aria-expanded="false"><svg id="ellipses-horizontal-fill-small"></svg></button><span data-menu-portal-anchor></span></div>
        <video></video><div class="vjs-poster"><img src="https://media.licdn.com/dms/image/v2/D4E05AQG-bBKIRvxluQ/videocover-high/fixture"></div></div>
        <script>let clicks=0;const mode=${JSON.stringify(mode)};const toggle=document.querySelector('#toggle');
        toggle.onclick=()=>{clicks++;if(toggle.getAttribute('aria-expanded')==='true'){setTimeout(()=>{toggle.setAttribute('aria-expanded','false');document.querySelector('#owned')?.remove();document.querySelector('[data-menu-portal-anchor]').innerHTML='';},30);return;}
        const open=()=>{toggle.setAttribute('aria-expanded','true');document.querySelector('[data-menu-portal-anchor]').innerHTML='<span aria-owns="owned"></span>';const container=document.createElement('div');container.id='owned';
        const anchor=mode==='no-embed'?'<span>Copy link to post</span>':'<a href="https://www.linkedin.com/preload/embed-modal/?targetUrn=urn%3Ali%3AugcPost%3A7520516702044999680&hasLinkedInVideo=true">Embed this post</a>';
        if(mode.includes('nested'))container.innerHTML='<div role="menu">'+anchor+'</div>';else{container.setAttribute('role','menu');container.innerHTML=anchor;}document.body.append(container);};if(mode.startsWith('delayed'))setTimeout(open,120);else open();};</script></html>` }));
      await page.goto('https://www.linkedin.com/feed/');
      await page.evaluate(() => Object.defineProperties(document.querySelector('video'), { readyState: { get: () => 4 }, paused: { get: () => false }, currentSrc: { get: () => 'blob:https://www.linkedin.com/fixture' } }));
      await page.addScriptTag({ path: path.join(root, 'browser/common/video-detection.js') });
      const found = await detect(page);
      assert.equal(found.url, mode === 'no-embed' ? '' : post, mode + ': selected post must be identified');
      assert.equal(await page.locator('#toggle').getAttribute('aria-expanded'), 'false', mode + ': extension-owned menu must close before returning');
      assert.equal(await page.evaluate(() => clicks), 2, mode + ': open once and close once');
      // An already-open user menu must remain open and must not be clicked.
      await page.locator('#toggle').click(); await page.locator('#owned').waitFor();
      const before = await page.evaluate(() => clicks); await detect(page);
      assert.equal(await page.evaluate(() => clicks), before, mode + ': existing user menu stays untouched');
      assert.equal(await page.locator('#toggle').getAttribute('aria-expanded'), 'true');
      await page.close(); count++;
    }
    console.log(`PASS: ${count} LinkedIn menu layouts/timings, original post identity, asynchronous closure and untouched user-owned menus.`);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
