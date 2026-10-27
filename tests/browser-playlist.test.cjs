const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const script = name => fs.readFileSync(path.join(__dirname, '../browser/common', name), 'utf8');
const video = 'https://www.youtube.com/watch?v=jNQXAC9IVRw';
const list = 'PLDoPjvoNmBAy532K9M_fjiAmrJ0gkCyLJ';
function context(extra = {}) { const c = vm.createContext({ URL, ...extra }); vm.runInContext(script('video-detection.js'), c); return c; }
test('playlist scope is derived from the matching public video page', () => {
  const detect = context().ViFreeVideoDetection;
  assert.equal(detect.playlistFor(video, `${video}&list=${list}&index=4`), `https://www.youtube.com/playlist?list=${list}`);
  assert.equal(detect.playlistFor(video, `https://youtu.be/jNQXAC9IVRw?list=${list}`), `https://www.youtube.com/playlist?list=${list}`);
  assert.equal(detect.playlistFor(video, `${video}&list=RDjNQXAC9IVRw`), `${video}&list=RDjNQXAC9IVRw&start_radio=1`);
  for (const page of [video, `${video}&list=short`, `${video}&list=${list}&list=${list}`, `https://www.youtube.com/watch?v=abcdefghijk&list=${list}`, `https://youtube.com.evil.example/watch?v=jNQXAC9IVRw&list=${list}`, `https://user@www.youtube.com/watch?v=jNQXAC9IVRw&list=${list}`, `http://www.youtube.com/watch?v=jNQXAC9IVRw&list=${list}`]) assert.equal(detect.playlistFor(video, page), '');
});
test('background routes explicit playlist choices while retaining single-video and legacy delivery', async () => {
  let handler; const deliveries = [];
  const api = { runtime: { id: 'fixture', onMessage: { addListener: f => { handler = f; } }, onInstalled: { addListener() {} } }, contextMenus: { onClicked: { addListener() {} } } };
  const c = context({ ViFreeConnection: { api, name: 'Vydrop', holdReload: () => () => {}, sendLink: async (...args) => { deliveries.push(args); return { code: 'opened-legacy' }; } } });
  vm.runInContext(script('background.js'), c);
  const sender = { id: 'fixture', tab: { id: 7, url: `${video}&list=${list}` }, frameId: 0, url: `${video}&list=${list}` };
  const message = { version: 1, type: 'vifree-open-video', url: video, playlist: true, output: { kind: 'video', format: 'mp4', height: 720 } };
  const run = (m, s = sender) => new Promise(resolve => handler(m, s, resolve));
  assert.equal((await run(message)).code, 'opened-legacy');
  assert.equal(deliveries[0][0], `https://www.youtube.com/playlist?list=${list}`);
  assert.equal(deliveries[0][1].startDownload, true);
  assert.equal(deliveries[0][1].playlist, true);
  await run({ ...message, playlist: false }); assert.equal(deliveries[1][0], video);
  assert.equal((await run(message, { ...sender, tab: { id: 8, url: video } })).ok, false);
  assert.equal(deliveries.length, 2);
});
test('playlist scope is negotiated and never downgraded to a single-video download', async () => {
  for (const supported of [false, true]) {
    const sent = []; let listener;
    const api = { runtime: { getManifest: () => ({ name: 'Vydrop' }), connectNative: () => ({
      onMessage: { addListener: fn => { listener = fn; } }, onDisconnect: { addListener() {} }, disconnect() {},
      postMessage(message) { sent.push(message); queueMicrotask(() => listener({ ok: true, protocol: 2, requestId: message.requestId, capabilities: ['output-selection', 'start-download', ...(supported ? ['playlist-download'] : [])], code: message.type === 'ping' ? 'ready' : 'started' })); }
    }), sendNativeMessage: () => { throw new Error('Playlist must not use v1'); } } };
    const c = vm.createContext({ browser: api, URL, setTimeout: (...args) => { const timer = setTimeout(...args); timer.unref(); return timer; }, clearTimeout });
    vm.runInContext(script('connection.js'), c);
    const send = () => c.ViFreeConnection.sendLink(`https://www.youtube.com/playlist?list=${list}`, { playlist: true, output: { kind: 'video', format: 'mp4', height: 720 }, startDownload: true });
    if (supported) { await send(); assert.equal(sent.at(-1).type, 'download'); assert.equal(sent.at(-1).scope, 'playlist'); }
    else { await assert.rejects(send, error => error.code === 'unsupported'); assert.equal(sent.length, 1); }
  }
});
