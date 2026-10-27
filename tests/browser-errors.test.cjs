const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '..');
const load = (context, file) => vm.runInContext(fs.readFileSync(path.join(root, 'browser/common', file), 'utf8'), context);
const video = 'https://example.com/video';
const codes = ['invalid', 'missing', 'ambiguous', 'inspect', 'busy', 'unavailable', 'preparing', 'failed', 'unknown', undefined];

for (const language of ['en', 'ar']) for (const surface of ['popup', 'context-menu']) for (const code of codes) {
  test(`${surface} localizes ${code ?? 'legacy code-less'} failure in ${language}`, async () => {
    const listeners = {}, status = { textContent: '' };
    let calls = 0, title = '', click;
    const button = { disabled: false, addEventListener: (_, fn) => { click = fn; } };
    const api = {
      i18n: { getUILanguage: () => language },
      runtime: { getManifest: () => ({ name: 'Vydrop' }),
        onMessage: { addListener: fn => { listeners.message = fn; } }, onInstalled: { addListener: () => {} },
        sendNativeMessage: async () => { calls++; return { ok: false, code, message: 'RAW PRIVATE ERROR https://private.example/session' }; } },
      tabs: { query: async () => [{ id: 12, url: video }] },
      action: { setBadgeText: async () => {}, setBadgeBackgroundColor: async () => {}, setTitle: async value => { title = value.title; } },
      contextMenus: { onClicked: { addListener: fn => { listeners.menu = fn; } } }
    };
    const context = vm.createContext({ browser: api, URL, document: { documentElement: {}, querySelector: selector => selector === '#send' ? button : status } });
    load(context, 'connection.js');
    context.ViFreeVideoDetection = { resolve: async () => {
      if (['missing', 'ambiguous', 'inspect'].includes(code)) throw Object.assign(new Error('RAW PRIVATE ERROR'), { code });
      return code === 'invalid' ? 'javascript:bad' : video;
    } };
    if (surface === 'popup') { load(context, 'popup.js'); await click(); }
    else { load(context, 'background.js'); await listeners.menu({}, { id: 12, url: video }); }
    const message = surface === 'popup' ? status.textContent : title;
    assert.ok(message);
    assert.doesNotMatch(message, /RAW PRIVATE|private\.example|session/u);
    if (language === 'ar') assert.match(message, /[\u0600-\u06ff]/u);
    else assert.match(message, /video|Vydrop|page/u);
    if (['missing', 'ambiguous', 'inspect', 'invalid'].includes(code)) assert.equal(calls, 0);
    else assert.equal(calls, 1);
    if (['missing', 'ambiguous', 'inspect'].includes(code)) assert.doesNotMatch(message, /Browser integration|الربط بالمتصفح/u);
    assert.equal(button.disabled, false);
  });
}

test('a legacy extension can still read version-1 success and busy responses', async () => {
  // Exercise the previously shipped connection implementation against additive host responses.
  const legacy = fs.readFileSync(path.join(__dirname, 'fixtures/legacy-native-client.js'), 'utf8');
  let response = { ok: true, code: 'opened', message: 'Link opened in Vydrop.', future: 'ignored' };
  const context = vm.createContext({ URL, browser: { runtime: { getManifest: () => ({ name: 'Vydrop' }), sendNativeMessage: async (_, message) => {
    assert.equal(message.version, 1); return response;
  } } } });
  vm.runInContext(legacy, context);
  assert.equal((await context.legacySendLink(video)).ok, true);
  response = { ok: false, code: 'busy', message: 'Vydrop is busy.' };
  await assert.rejects(context.legacySendLink(video), error => error.code === 'busy');
  response = { ok: false, code: 'invalid', message: 'Could not send this link.' };
  await assert.rejects(context.legacySendLink(video));
});
