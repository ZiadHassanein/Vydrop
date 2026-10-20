// Real MV3 webRequest integration in a disposable profile. This fixture promotes
// optional permissions only in its copied manifest, avoiding a real user grant.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');

(async () => {
  const source = process.env.VYDROP_EXTENSION;
  assert.ok(source, 'Set VYDROP_EXTENSION to the packaged Chromium companion');
  const fixture = fs.mkdtempSync(path.join(root, 'build/discovery-mv3-'));
  const extension = path.join(fixture, 'extension'); fs.cpSync(source, extension, { recursive: true });
  const manifestPath = path.join(extension, 'manifest.json');
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  assert.deepEqual(manifest.optional_permissions, ['webRequest']);
  manifest.permissions.push(...manifest.optional_permissions);
  manifest.host_permissions = manifest.optional_host_permissions;
  delete manifest.optional_permissions; delete manifest.optional_host_permissions;
  fs.writeFileSync(manifestPath, JSON.stringify(manifest));
  const context = await chromium.launchPersistentContext(path.join(fixture, 'profile'), {
    channel: process.env.VYDROP_BROWSER_CHANNEL || 'msedge', headless: true,
    args: ['--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  try {
    const worker = context.serviceWorkers()[0] || await context.waitForEvent('serviceworker');
    await worker.evaluate(() => {
      // A detection test may never invoke the actual installed native host.
      chrome.runtime.connectNative = () => { throw new Error('Native delivery disabled by this fixture'); };
      chrome.runtime.sendNativeMessage = async () => { throw new Error('Native delivery disabled by this fixture'); };
    });
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.hostname === 'media.example.test') return route.fulfill({ contentType: 'video/mp4', body: 'synthetic media response', headers: { 'Access-Control-Allow-Origin': '*', 'Set-Cookie': 'fixture-only=value; SameSite=None; Secure' } });
      if (url.hostname === 'page.example.test') return route.fulfill({ contentType: 'text/html', body: '<!doctype html><title>Owned discovery fixture</title><p>Video response fixture</p>' });
      return route.abort();
    });
    const page = await context.newPage(); await page.goto('https://page.example.test/post');
    const tabId = await worker.evaluate(async () => (await chrome.tabs.query({ url: 'https://page.example.test/post' }))[0].id);
    await page.evaluate(() => fetch('https://media.example.test/movie.mp4').then(response => response.text()));
    for (let attempt = 0; attempt < 30 && !await worker.evaluate(id => VydropMediaDiscovery.list(id).length, tabId); attempt++) await new Promise(resolve => setTimeout(resolve, 100));
    const popup = await context.newPage(); await popup.goto(new URL('popup.html', worker.url()).href);
    const reply = await popup.evaluate(id => chrome.runtime.sendMessage({ type: 'vydrop-discovery-list', tabId: id }), tabId);
    assert.equal(reply.enabled, true);
    assert.equal(reply.candidates.length, 1);
    assert.equal(reply.candidates[0].url, 'https://media.example.test/movie.mp4');
    assert.equal(reply.candidates[0].frameId, 0);
    assert.doesNotMatch(JSON.stringify(reply), /fixture-only|Set-Cookie/u);
    await page.goto('https://page.example.test/another-post');
    const after = await popup.evaluate(id => chrome.runtime.sendMessage({ type: 'vydrop-discovery-list', tabId: id }), tabId);
    assert.equal(after.candidates.length, 0, 'real top-level navigation clears the prior video candidates');
    console.log('PASS: real MV3 response observation, extension-page delivery, metadata-only storage and navigation cleanup (fixture permission grant)');
  } finally { await context.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
