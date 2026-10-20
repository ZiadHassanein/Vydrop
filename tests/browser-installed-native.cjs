// Explicit opt-in integration test against the installed PREVIEW native host.
// Uses a disposable Edge profile, no signed-in tabs, real native messaging, and a pending-only fixture.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const [installed, url, report] = process.argv.slice(2);
if (!installed || !url || !report || !url.startsWith('https://example.com/vydrop-installer-fixture-')) throw new Error('Use a test-owned installer fixture.');
const extension = path.resolve(installed, 'Browser/chromium');
const manifest = JSON.parse(fs.readFileSync(path.join(extension, 'manifest.json')));
assert.equal(manifest.name, 'Vydrop Preview');
(async () => {
  const profile = fs.mkdtempSync(path.join(path.resolve(__dirname, '../build'), 'installed-browser-test-'));
  const context = await chromium.launchPersistentContext(profile, {
    channel: 'msedge', headless: true,
    args: ['--disable-extensions-except=' + extension, '--load-extension=' + extension]
  });
  let proof;
  try {
    const worker = context.serviceWorkers()[0] || await context.waitForEvent('serviceworker', { timeout: 15000 });
    assert.equal(await worker.evaluate(() => chrome.runtime.id), 'cpoiefoeogidkjcgbacikpcdjkjeekcf');
    const health = await worker.evaluate(() => globalThis.ViFreeConnection.testConnection());
    assert.equal(health.ok, true); assert.equal(health.protocol, 2); assert.equal(health.appRunning, false, 'the desktop must be closed before this test');
    const receipt = await worker.evaluate(value => globalThis.ViFreeConnection.sendLink(value, { enqueue: true, sourcePage: 'https://example.com/owned-test-page' }), url);
    assert.equal(receipt.ok, true); assert.equal(receipt.code, 'queued');
    const running = await worker.evaluate(() => globalThis.ViFreeConnection.testConnection());
    assert.equal(running.appRunning, true);
    proof = { result: 'PASS', browser: 'isolated headless Microsoft Edge', companion: manifest.version, identity: 'retained unpacked identity', nativeMessaging: 'real registered installed host', appBefore: 'closed', appAfter: 'running', receipt: receipt.code, automaticDownload: false, storeApprovalTested: false };
  } finally {
    const start = Date.now();
    await context.close();
    assert.ok(Date.now() - start < 15000, 'The browser must close while its launched desktop app remains open.');
    if (proof) proof.browserShutdownMs = Date.now() - start;
  }
  fs.writeFileSync(report, JSON.stringify(proof, null, 2));
  console.log('PASS: real Edge companion cold-starts the installed app, queues the fixture, and the browser exits independently.');
})().catch(error => { console.error(error); process.exitCode = 1; });
