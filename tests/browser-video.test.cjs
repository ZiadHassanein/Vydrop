const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const root = path.join(__dirname, '..');
const scripts = name => fs.readFileSync(path.join(root, 'Browser/common', name), 'utf8');

// Synthetic DOM fixtures implement only standard traversal/layout operations, not detector logic.
class Element {
  constructor(tag, attrs = {}, children = []) {
    this.tag = tag; this.attrs = attrs; this.id = attrs.id ?? ''; this.children = children;
    this.paused = attrs.paused ?? false; this.ended = false; this.readyState = 4;
    this.src = attrs.src ?? 'blob:synthetic'; this.currentSrc = this.src;
    this.poster = attrs.poster ?? ''; this.mediaKeys = attrs.mediaKeys ?? null; this.textContent = attrs.textContent ?? '';
    this.style = { display: 'block', visibility: 'visible', opacity: '1', ...attrs.style };
    this.box = { left: 100, top: 100, width: 600, height: 400, ...attrs.box };
    for (const child of children) child.parentElement = this;
  }
  get href() { return this.attrs.href; }
  getAttribute(name) { return this.attrs[name] ?? null; }
  getRootNode() { let node = this; while (node.parentElement) node = node.parentElement; return node.rootNode ?? node; }
  dispatchEvent(event) { this.attrs.onEvent?.(event, this); return true; }
  getBoundingClientRect() { return { ...this.box, right: this.box.left + this.box.width, bottom: this.box.top + this.box.height }; }
  contains(node) { return this === node || this.children.some(child => child.contains(node)); }
  matches(selector) {
    return selector.split(',').some(value => value === this.tag || value.startsWith('.') && (this.attrs.class ?? '').split(/\s+/u).includes(value.slice(1)) || value === 'a[href]' && this.tag === 'a' && !!this.href ||
      /^\[([^=]+)="([^"]+)"\]$/u.test(value) && this.getAttribute(RegExp.$1) === RegExp.$2 ||
      /^\[([^=]+)\^="([^"]+)"\]$/u.test(value) && this.getAttribute(RegExp.$1)?.startsWith(RegExp.$2));
  }
  closest(selector) { for (let node = this; node; node = node.parentElement) if (node.matches(selector)) return node; return null; }
  querySelectorAll(selector) { return this.children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]); }
}
const el = (tag, attrs, ...children) => new Element(tag, attrs, children);
const link = href => el('a', { href });
function fixture(url, ...children) {
  const body = el('body', {}, ...children);
  const context = vm.createContext({ URL, setTimeout, MouseEvent: class { constructor(type) { this.type = type; } }, location: { href: url, hostname: new URL(url).hostname }, document: { body, querySelectorAll: selector => body.querySelectorAll(selector) }, getComputedStyle: node => node.style, innerWidth: 1200, innerHeight: 800 });
  vm.runInContext(scripts('video-detection.js'), context);
  return { context, body, detect: (hint, warm) => context.ViFreeVideoDetection.inspectVideoPage(hint, warm) };
}
const tiktok = (id, author, attrs = {}) => el('article', { 'data-e2e': 'recommend-list-item-container' },
  el('section', { 'data-e2e': 'feed-video' }, el('div', { id: `xgwrapper-0-${id}` }, el('video', attrs)), link(`https://www.tiktok.com/@${author}`)));
const facebook = (id, attrs = {}) => el('article', { role: 'article' }, el('div', {}, el('video', attrs)), link(`https://www.facebook.com/watch/?v=${id}&tracking=discard`));
function reddit(id, attrs = {}) {
  const player = el('shreddit-player', { 'post-id': `t3_${id}`, ...attrs });
  const child = el('div', {}, el('video'));
  const shadow = el('shadow-root', {}, child);
  shadow.host = player; child.parentElement = null; child.rootNode = shadow;
  player.shadowRoot = shadow;
  return player;
}

test('Reddit selects the post behind its open shadow player rather than a video-only CDN URL', () => {
  assert.equal(fixture('https://www.reddit.com/r/videos/', reddit('abc123')).detect().url, 'https://www.reddit.com/comments/abc123/');
});

test('Reddit follows the explicitly selected player without borrowing another feed post', () => {
  const one = reddit('abc123'), two = reddit('def456');
  const { context } = fixture('https://www.reddit.com/', one, two);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, two.shadowRoot.querySelectorAll('video')[0]).url, 'https://www.reddit.com/comments/def456/');
});

test('Reddit shadow players respect hidden host ancestors and avoid ads', () => {
  const hidden = reddit('abc123'); hidden.style.visibility = 'hidden';
  assert.equal(fixture('https://www.reddit.com/', hidden).detect().url, '');
  const ad = reddit('def456', { 'post-promoted': '' });
  assert.equal(fixture('https://www.reddit.com/comments/abc123/', ad).detect().reason, 'advertisement');
  const comment = reddit('abc123', { 'comment-id': 't1_example' });
  const { context } = fixture('https://www.reddit.com/comments/abc123/', comment);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, comment.shadowRoot.querySelectorAll('video')[0]).url, '', 'a comment GIF cannot resolve to the main post video');
});

test('Dailymotion format lookup follows navigation even when its frame URL stays the same', async () => {
  let handler;
  const inspected = [];
  const api = { runtime: { id: 'fixture', getManifest: () => ({ name: 'Vydrop' }), onMessage: { addListener: fn => { handler = fn; } }, onInstalled: { addListener: () => {} } }, contextMenus: { onClicked: { addListener: () => {} } } };
  const context = vm.createContext({ browser: api, URL });
  for (const file of ['connection.js', 'video-detection.js']) vm.runInContext(scripts(file), context);
  context.ViFreeConnection.inspectLink = async url => { inspected.push(url); return { title: url, videos: [], audio: [] }; };
  vm.runInContext(scripts('background.js'), context);
  const frameUrl = 'https://geo.dailymotion.com/player/xtv3w.html?';
  for (const id of ['x9wfwwa', 'x9vnf46']) {
    const sender = { id: 'fixture', frameId: 2, url: frameUrl, tab: { id: 42, url: `https://www.dailymotion.com/video/${id}` } };
    const response = await new Promise(resolve => handler({ version: 1, type: 'vydrop-inspect-video', url: frameUrl, tabUrl: 'https://evil.example/' }, sender, resolve));
    assert.equal(response.url, `https://www.dailymotion.com/video/${id}`);
    assert.equal(response.ok, true);
  }
  assert.deepEqual(inspected, ['https://www.dailymotion.com/video/x9wfwwa', 'https://www.dailymotion.com/video/x9vnf46']);
});

test('Reddit canonical links drop tracking and never mistake a homepage for a post', () => {
  assert.equal(fixture('https://www.reddit.com/r/videos/comments/abc123/example/?tracking=discard').detect().url, 'https://www.reddit.com/comments/abc123/');
  assert.equal(fixture('https://redd.it/abc123').detect().url, 'https://www.reddit.com/comments/abc123/');
  assert.equal(fixture('https://www.reddit.com/r/videos/').detect().url, '');
});

test('Twitch keeps the public clip permalink instead of the currently playing rendition', () => {
  assert.equal(fixture('https://www.twitch.tv/nasa/clip/ExampleClip-123?tracking=discard', el('video', { src: 'https://cdn.cloudfront.net/landscape/hevc/480/index.mp4' })).detect().url, 'https://clips.twitch.tv/ExampleClip-123');
  assert.equal(fixture('https://clips.twitch.tv/embed?clip=ExampleClip-123').detect().url, 'https://clips.twitch.tv/ExampleClip-123');
  assert.equal(fixture('https://www.twitch.tv/nasa', el('video')).detect().url, '');
});

test('Dailymotion normalizes watch and embedded video links', () => {
  assert.equal(fixture('https://www.dailymotion.com/video/x9wfwwa_title?tracking=discard').detect().url, 'https://www.dailymotion.com/video/x9wfwwa');
  assert.equal(fixture('https://geo.dailymotion.com/player/xtv3w.html?video=x9wfwwa', el('video')).detect().url, 'https://www.dailymotion.com/video/x9wfwwa');
});

test('Dailymotion player-only frames use the browser-supplied containing video page', async () => {
  const frameUrl = 'https://geo.dailymotion.com/player/xtv3w.html?';
  const { context, detect } = fixture(frameUrl, el('video'));
  assert.equal(detect().reason, 'frame');
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: frameUrl, frameUrl, tabUrl: 'https://www.dailymotion.com/video/x9wfwwa?tracking=discard' }), 'https://www.dailymotion.com/video/x9wfwwa');
  for (const tabUrl of ['https://evil.example/video/x9wfwwa', 'http://www.dailymotion.com/video/x9wfwwa', 'https://user:secret@www.dailymotion.com/video/x9wfwwa', 'https://www.dailymotion.com:8443/video/x9wfwwa', 'https://www.dailymotion.com/'])
    assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: frameUrl, frameUrl, tabUrl }), frameUrl);
});

test('Dailymotion resolves the explicitly clicked player alongside hidden ad videos', () => {
  const video = el('video', { src: 'blob:https://geo.dailymotion.com/fixture-player' });
  const { context } = fixture('https://geo.dailymotion.com/player/xtv3w.html?',
    el('main', {}, el('div', { class: 'player video_player' }, el('div', { class: 'video_view' }, video)),
      el('div', { class: 'ad-container' }, el('video', { paused: true, style: { display: 'none' } }),
        el('div', { style: { display: 'none' } }, el('video', { paused: true }), el('video', { paused: true })))));
  const found = context.ViFreeVideoDetection.inspectVideoPage(video.currentSrc, true, video);
  assert.equal(found.url, 'https://geo.dailymotion.com/player/xtv3w.html?');
  assert.equal(found.reason, 'frame');
});

test('Dailymotion keeps its watch-page handoff when ads or navigation replace the selected video node', () => {
  const previous = el('video', { src: 'blob:https://geo.dailymotion.com/previous' });
  const { context } = fixture('https://geo.dailymotion.com/player/xtv3w.html?', el('video', { src: 'blob:https://geo.dailymotion.com/current' }));
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage(previous.currentSrc, true, previous).reason, 'frame');
  assert.equal(fixture('https://geo.dailymotion.com/player/xtv3w.html?').detect().reason, 'frame');
  assert.equal(fixture('https://geo.dailymotion.com/player/xtv3w.html?video=x73dk0q').detect().url, 'https://www.dailymotion.com/video/x73dk0q', 'an explicit embedded video ID still takes precedence');
  for (const url of ['https://geo.dailymotion.com.evil.example/player/xtv3w.html?', 'https://geo.dailymotion.com/other/xtv3w.html?'])
    assert.notEqual(fixture(url).detect().reason, 'frame', 'the frame fallback is restricted to the known Dailymotion player');
});

test('Dailymotion rejects selected ad players and never forwards an ad stream as the content video', async () => {
  const ad = el('video', { src: 'https://ads.example/advertisement.mp4' });
  const { context } = fixture('https://geo.dailymotion.com/player/xtv3w.html?', el('div', { class: 'ad-container' }, ad));
  const found = context.ViFreeVideoDetection.inspectVideoPage(ad.currentSrc, true, ad);
  assert.equal(found.url, ''); assert.equal(found.reason, 'advertisement');
  const frameUrl = 'https://geo.dailymotion.com/player/xtv3w.html?';
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: ad.currentSrc, frameUrl, tabUrl: 'https://www.dailymotion.com/video/x73dk0q' }), 'https://www.dailymotion.com/video/x73dk0q', 'even an older content script cannot send its ad stream instead of the watch page');
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: ad.currentSrc, frameUrl: frameUrl + 'video=x73dk0q', tabUrl: 'https://publisher.example/article' }), 'https://www.dailymotion.com/video/x73dk0q', 'embedded content uses its explicit video ID');
  for (const tabUrl of ['https://publisher.example/article', undefined])
    assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: ad.currentSrc, frameUrl, tabUrl }), frameUrl, 'an unknown content ID never falls back to the ad stream');
});

test('The selected JW Player uses its main source configuration instead of an ad or unsupported embed URL', () => {
  const media = 'https://cdn.example/content/master.m3u8?token=fixture';
  const video = el('video', { src: 'https://ads.example/commercial.mp4' });
  const { detect } = fixture('https://arabhd.onl/embed-example123.html',
    el('div', { class: 'jwplayer', id: 'vplayer' }, video),
    el('script', { textContent: 'jwplayer("unrelated").setup({sources:[{file:"https://ads.example/other.mp4"}]});' }),
    el('script', { textContent: `jwplayer("vplayer").setup({sources:[{file:"${media}"}],advertising:{tag:"https://ads.example/vast.mp4"}});` }));
  const found = detect(video.currentSrc);
  assert.equal(found.url, media); assert.equal(found.mediaId, 'example123');
  assert.equal(found.sourcePage, 'https://arabhd.onl/embed-example123.html');
});

test('AnaFast embedded playback resolves through its public player page instead of a temporary CDN stream', async () => {
  const page = 'https://anafast.space/embed-j14ddsm8ejnx.html';
  const stream = 'https://cdn.example/main/master.m3u8?token=expired';
  const video = el('video');
  const { context, detect } = fixture(page,
    el('div', { class: 'jwplayer', id: 'vplayer' }, video),
    el('script', { textContent: `jwplayer("vplayer").setup({sources:[{file:"${stream}"}]});` }));
  const found = detect();
  assert.equal(found.url, stream, 'the content script still identifies the selected main source');
  const resolve = context.ViFreeVideoDetection.resolveSource;
  for (const url of [found.url, 'https://ads.example/commercial.mp4', page])
    assert.equal(await resolve({ url, frameUrl: page, tabUrl: 'https://v2.kirmizi.cfd/episode/example/' }), page,
      'native inspection and download obtain a fresh main stream through the exact browser-supplied embed');
  assert.equal(await resolve({ url: stream, frameUrl: page + '?tracking=fixture' }), page, 'irrelevant frame query data is omitted');
  for (const frameUrl of ['http://anafast.space/embed-j14ddsm8ejnx.html', 'https://anafast.space.evil.example/embed-j14ddsm8ejnx.html',
    'https://user@anafast.space/embed-j14ddsm8ejnx.html', 'https://anafast.space:444/embed-j14ddsm8ejnx.html',
    'https://anafast.space/embed-invalid.html', 'https://arabhd.onl/embed-j14ddsm8ejnx.html'])
    assert.equal(await resolve({ url: stream, frameUrl }), stream, 'unverified hosts and player shapes retain their original routing');
});

test('Packed player source data is bounded and decoded without executing page scripts', () => {
  const media = 'https://cdn.example/main/master.m3u8';
  const payload = '0("1").2({3:[{4:"5"}]});globalThis.pageCodeRan=true;';
  const packed = `eval(function(p,a,c,k,e,d){throw new Error("must not execute");}('${payload}',36,6,'jwplayer|vplayer|setup|sources|file|${media}'.split('|')))`;
  const { context, detect } = fixture('https://arabhd.onl/embed-example123.html', el('div', { class: 'jwplayer', id: 'vplayer' }, el('video')), el('script', { textContent: packed }));
  assert.equal(detect().url, media); assert.equal(context.pageCodeRan, undefined);
  for (const invalid of [packed.replace(',36,6,', ',99,6,'), packed.replace(',36,6,', ',36,999999,'), ' '.repeat(65537) + packed]) {
    const result = fixture('https://arabhd.onl/embed-example123.html', el('div', { class: 'jwplayer', id: 'vplayer' }, el('video')), el('script', { textContent: invalid })).detect();
    assert.notEqual(result.url, media);
  }
  const protectedVideo = fixture('https://arabhd.onl/embed-example123.html', el('div', { class: 'jwplayer', id: 'vplayer' }, el('video', { mediaKeys: {} })), el('script', { textContent: packed })).detect();
  assert.notEqual(protectedVideo.url, media);
});

test('TikTok current feed wrapper identifies the playing video behind a blob URL', () => {
  const { detect } = fixture('https://www.tiktok.com/en/', tiktok('7000000000000000001', 'playing'), tiktok('7000000000000000002', 'paused', { paused: true }));
  assert.equal(detect().url, 'https://www.tiktok.com/@playing/video/7000000000000000001');
});
test('TikTok re-evaluates the current player after feed scrolling', () => {
  const first = tiktok('7000000000000000001', 'first'), second = tiktok('7000000000000000002', 'second', { paused: true });
  const { detect } = fixture('https://www.tiktok.com/en/', first, second);
  assert.match(detect().url, /@first/u);
  first.querySelectorAll('video')[0].paused = true; second.querySelectorAll('video')[0].paused = false;
  assert.match(detect().url, /@second/u);
});
test('TikTok detects its real canvas-backed player while the video element is transparent', () => {
  const item = tiktok('7000000000000000001', 'canvas', { style: { opacity: '0' } });
  const player = item.querySelectorAll('video')[0].parentElement, canvas = el('canvas');
  canvas.parentElement = player; player.children.push(canvas);
  assert.equal(fixture('https://www.tiktok.com/en/', item).detect().url, 'https://www.tiktok.com/@canvas/video/7000000000000000001');
});
test('Facebook selects the playing article and removes tracking parameters', () => {
  assert.equal(fixture('https://www.facebook.com/', facebook('11111111111', { paused: true }), facebook('22222222222')).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
});
test('a wrapping video permalink takes precedence over stale detail-page navigation', () => {
  assert.equal(fixture('https://www.facebook.com/watch/?v=11111111111', el('a', { href: 'https://www.facebook.com/reel/22222222222/' }, el('video'))).detect().url, 'https://www.facebook.com/reel/22222222222/');
});
test('repeated links for the same Facebook video do not create ambiguity', () => {
  const article = facebook('11111111111'); article.children.push(link('https://www.facebook.com/watch/?v=11111111111&fbclid=discard'));
  assert.equal(fixture('https://www.facebook.com/watch/', article).detect().url, 'https://www.facebook.com/watch/?v=11111111111');
});
test('Facebook player data identifies a video in its fullscreen dialog', () => {
  assert.equal(fixture('https://www.facebook.com/', el('div', { role: 'dialog', 'data-video-id': '11111111111' }, el('video'))).detect().url, 'https://www.facebook.com/watch/?v=11111111111');
});
test('a Facebook feed timestamp can identify its video post without confusing post and video IDs', () => {
  const url = 'https://www.facebook.com/author/posts/pfbidSyntheticPost';
  assert.equal(fixture('https://www.facebook.com/', el('article', {}, el('video'), link(url + '?tracking=discard'))).detect().url, url);
});
test('explicit Facebook player IDs take precedence over another linked post', () => {
  const scope = el('article', {}, el('div', { 'data-video-id': '11111111111' }, el('video'), link('https://www.facebook.com/reel/22222222222/')));
  assert.equal(fixture('https://www.facebook.com/', scope).detect().url, 'https://www.facebook.com/watch/?v=11111111111');
});
for (const [name, attrs] of [ ['offscreen', { box: { top: 1000 } }], ['hidden', { style: { visibility: 'hidden' } }], ['transparent', { style: { opacity: '0' } }], ['not displayed', { style: { display: 'none' } }] ]) {
  test(`a ${name} preloaded player cannot win over the visible video`, () => {
    assert.equal(fixture('https://www.facebook.com/', facebook('11111111111', attrs), facebook('22222222222', { paused: true })).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
  });
}
test('equally visible playing videos are reported as ambiguous', () => {
  const found = fixture('https://www.facebook.com/', facebook('11111111111'), facebook('22222222222')).detect();
  assert.equal(found.url, ''); assert.equal(found.reason, 'ambiguous');
});
test('a transparent feed container cannot hide a competing playing video from visibility checks', () => {
  const hidden = facebook('11111111111'); hidden.style.opacity = '0';
  assert.equal(fixture('https://www.facebook.com/', hidden, facebook('22222222222', { paused: true })).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
});
test('right-clicking a video identifies that player without sending its blob URL', () => {
  const found = fixture('https://www.facebook.com/', facebook('11111111111', { src: 'blob:first' }), facebook('22222222222', { src: 'blob:second' })).detect('blob:second');
  assert.equal(found.url, 'https://www.facebook.com/watch/?v=22222222222');
});
test('a neighboring article or sidebar cannot supply an unrelated video link', () => {
  const found = fixture('https://www.facebook.com/', el('article', { role: 'article' }, el('video')), el('aside', {}, link('https://www.facebook.com/reel/22222222222/'))).detect();
  assert.equal(found.url, ''); assert.equal(found.reason, 'missing-link');
});
test('multiple distinct links within one player fail rather than choosing an arbitrary video', () => {
  const found = fixture('https://www.facebook.com/', el('article', {}, el('video'), link('https://www.facebook.com/reel/11111111111/'), link('https://www.facebook.com/reel/22222222222/'))).detect();
  assert.equal(found.url, '');
});
for (const url of ['https://www.tiktok.com/en/', 'https://www.facebook.com/', 'https://www.facebook.com/login/']) test(`a non-video page is never passed as a detection result: ${url}`, () => {
  const found = fixture(url).detect(); assert.equal(found.url, ''); assert.equal(found.reason, 'no-video');
});
for (const url of ['https://www.tiktok.com/@author/video/7000000000000000001', 'https://www.facebook.com/reel/11111111111/', 'https://www.facebook.com/watch/?v=11111111111', 'https://fb.watch/Example/']) test(`direct media links remain usable without a loaded player: ${url}`, () => {
  assert.equal(fixture(url).detect().url, url);
});

test('scripting runs only on explicit social-feed requests and returns the main-frame result', async () => {
  const { context } = fixture('https://www.tiktok.com/en/');
  let count = 0;
  context.ViFreeConnection = { api: { scripting: { executeScript: async request => { count++; assert.equal(request.target.tabId, 42); assert.equal(request.func.name, 'inspectVideoPage'); return [{ frameId: 7, result: { url: 'https://example.com/wrong' } }, { frameId: 0, result: { url: 'https://www.tiktok.com/@author/video/7000000000000000001' } }]; } } } };
  assert.equal(count, 0);
  assert.equal(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.youtube.com/watch?v=jNQXAC9IVRw' }), 'https://www.youtube.com/watch?v=jNQXAC9IVRw');
  assert.equal(count, 0);
  assert.match(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.tiktok.com/en/' }), /\/video\//u);
  assert.equal(count, 1);
  assert.equal(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.tiktok.com/en/' }, { linkUrl: 'https://vimeo.com/76979871' }), 'https://vimeo.com/76979871');
  assert.equal(count, 1);
});
test('detection errors distinguish missing video links, ambiguous players and injection failures', async () => {
  const { context } = fixture('https://www.facebook.com/');
  for (const [reason, expected] of [['missing-link', /Copy link/u], ['ambiguous', /More than one/u]]) {
    context.ViFreeConnection = { api: { scripting: { executeScript: async () => [{ frameId: 0, result: { reason } }] } } };
    await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.facebook.com/' }), expected);
  }
  context.ViFreeConnection.api.scripting.executeScript = async () => { throw new Error('denied'); };
  await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.facebook.com/' }), /Reload the extension/u);
});
test('a cross-site result cannot be sent in place of the selected Facebook video', async () => {
  const { context } = fixture('https://www.facebook.com/');
  context.ViFreeConnection = { api: { scripting: { executeScript: async () => [{ frameId: 0, result: { url: 'https://www.tiktok.com/@unrelated/video/7000000000000000001' } }] } } };
  await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.facebook.com/' }), /Copy link/u);
});

const nest = (node, count) => { while (count--) node = el('div', {}, node); return node; };
test('Facebook permalink survives the deeply nested player used by the live feed', () => {
  const post = el('div', {}, el('h3'), nest(el('video'), 25), link('https://www.facebook.com/reel/1038496899213671/'));
  assert.equal(fixture('https://www.facebook.com/', post).detect().url, 'https://www.facebook.com/reel/1038496899213671/');
});
test('a post heading bounds traversal before a neighboring video permalink', () => {
  const own = el('div', {}, el('h3'), nest(el('video'), 15));
  assert.equal(fixture('https://www.facebook.com/', own, link('https://www.facebook.com/reel/22222222222/')).detect().url, '');
});
test('Facebook public timestamps materialize after pointer entry without clicking or navigating', async () => {
  const events = [];
  const timestamp = el('a', { href: 'https://www.facebook.com/?__tn__=%2CO%2CP-R', onEvent: (event, node) => {
    events.push(event.type);
    if (event.type === 'mouseover') node.attrs.href = 'https://www.facebook.com/reel/1038496899213671/?tracking=discard';
  } });
  const { detect } = fixture('https://www.facebook.com/', el('div', {}, el('h3'), nest(el('video'), 15), timestamp));
  assert.equal(detect().reason, 'missing-link');
  assert.equal((await detect('', true)).url, 'https://www.facebook.com/reel/1038496899213671/');
  assert.deepEqual(events, ['mouseover', 'mouseenter']);
});
test('a lazy-link retry never enters a neighboring post', async () => {
  let touched = false;
  const other = el('a', { href: 'https://www.facebook.com/?__tn__=%2CO%2CP-R', onEvent: () => { touched = true; } });
  const found = await fixture('https://www.facebook.com/', el('article', {}, nest(el('video'), 15)), other).detect('', true);
  assert.equal(found.url, ''); assert.equal(touched, false);
});
test('a stale source hint cannot select another visible video', () => {
  assert.equal(fixture('https://www.facebook.com/', facebook('11111111111')).detect('blob:gone').reason, 'no-video');
});
test('the overlay identifies its exact player even when media is attached without a source URL', () => {
  const first = facebook('11111111111', { src: '' }), second = facebook('22222222222', { src: '' });
  const { context } = fixture('https://www.facebook.com/', first, second);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, second.querySelectorAll('video')[0]).url, 'https://www.facebook.com/watch/?v=22222222222');
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, el('video')).reason, 'no-video');
});
test('deeply hidden containers are excluded', () => {
  const hidden = nest(facebook('11111111111'), 20); hidden.style.display = 'none';
  assert.equal(fixture('https://www.facebook.com/', hidden, facebook('22222222222', { paused: true })).detect().url, 'https://www.facebook.com/watch/?v=22222222222');
});
for (const url of ['https://www.youtube.com/watch?v=jNQXAC9IVRw&list=ignored', 'https://www.youtube.com/shorts/jNQXAC9IVRw', 'https://www.youtube-nocookie.com/embed/jNQXAC9IVRw', 'https://youtu.be/jNQXAC9IVRw']) {
  test(`YouTube player has a canonical video link: ${url}`, () => assert.equal(fixture(url, el('video')).detect().url, 'https://www.youtube.com/watch?v=jNQXAC9IVRw'));
}
test('a YouTube feed player uses its own card instead of the homepage', () => {
  assert.equal(fixture('https://www.youtube.com/', el('ytd-rich-item-renderer', {}, el('video'), link('/watch?v=jNQXAC9IVRw'))).detect().url, 'https://www.youtube.com/watch?v=jNQXAC9IVRw');
});
test('a generic HTML5 player sends its direct HTTPS media URL', () => {
  assert.equal(fixture('https://example.com/', el('video', { src: 'https://cdn.example.com/movie.mp4?token=sample' })).detect().url, 'https://cdn.example.com/movie.mp4?token=sample');
});
test('a single blob-backed player sends its own page and a wrapped player prefers its permalink', () => {
  assert.equal(fixture('https://example.com/film', el('video')).detect().url, 'https://example.com/film');
  assert.equal(fixture('https://example.com/feed', el('a', { href: 'https://example.com/film/42' }, el('video')), el('video', { paused: true })).detect().url, 'https://example.com/film/42');
});
test('multiple blob-backed generic players cannot cause homepage forwarding', () => {
  assert.equal(fixture('https://example.com/', el('video'), el('video', { paused: true })).detect('blob:synthetic').url, '');
});
test('non-web pages and credential-bearing sources are never returned', () => {
  assert.equal(fixture('file:///C:/private.html', el('video')).detect().url, '');
  assert.equal(fixture('https://example.com/film', el('video', { src: 'https://user:secret@example.com/private.mp4' })).detect().url, 'https://example.com/film');
});

const linkedInActivity = 'https://www.linkedin.com/feed/update/urn:li:activity:7517227738701901824/';
const linkedInGroup = 'https://www.linkedin.com/feed/update/urn:li:groupPost:2066905-7517226468083638272/?highlightedUpdateUrn=urn%3Ali%3Aactivity%3A7517227738701901824&origin=SOCIAL_SHARE';
const linkedInGroupFeed = 'https://www.linkedin.com/groups/2066905/?q=highlightedFeedForGroups&highlightedUpdateUrn=urn%3Ali%3Aactivity%3A7517227738701901824';
const linkedInPost = 'https://www.linkedin.com/posts/the-mathworks_2_what-is-mathworks-cloud-center-activity-7151241570371948544-4Gu7';
test('LinkedIn resolves the selected feed post from its public activity attribute', () => {
  const first = el('div', { 'data-urn': 'urn:li:activity:7151241570371948544' }, el('video', { paused: true }));
  const active = el('div', { 'data-urn': 'urn:li:activity:7517227738701901824' }, el('video'));
  assert.equal(fixture('https://www.linkedin.com/feed/', first, active).detect().url, linkedInActivity);
});
for (const url of [linkedInActivity, linkedInGroup, linkedInGroupFeed, linkedInPost]) test(`LinkedIn public permalink is recognized: ${url}`, () => {
  const expected = url === linkedInGroup || url === linkedInGroupFeed ? linkedInActivity : url;
  assert.equal(fixture(url).detect().url, expected);
  assert.equal(fixture('https://www.linkedin.com/feed/', el('article', {}, el('video'), link(url + (url.includes('?') ? '&' : '?') + 'tracking=discard'))).detect().url, expected);
});
test('LinkedIn never substitutes a feed, private message or invented group activity for a video', () => {
  for (const url of ['https://www.linkedin.com/feed/', 'https://www.linkedin.com/messaging/', linkedInGroup.split('?')[0], linkedInGroupFeed.split('?')[0], linkedInGroupFeed + '&highlightedUpdateUrn=urn%3Ali%3Aactivity%3A7151241570371948544', linkedInGroupFeed.replace('urn%3Ali%3Aactivity%3A7517227738701901824', 'urn%3Ali%3AugcPost%3A7517227738701901824'), linkedInGroup.replace('urn%3Ali%3Aactivity%3A7517227738701901824', 'https%3A%2F%2Fevil.example%2Fvideo')])
    assert.equal(fixture(url, el('video')).detect().url, '');
});
test('LinkedIn post boundaries prevent borrowing a neighboring activity link', () => {
  for (const attrs of [{ 'data-urn': 'urn:li:ugcPost:7517226468083638272' }, { class: 'feed-shared-update-v2' }, { role: 'listitem' }]) {
    const own = el('div', attrs, el('video'));
    assert.equal(fixture('https://www.linkedin.com/feed/', el('section', {}, own, link(linkedInActivity))).detect().url, '');
  }
});
test('LinkedIn SDUI group feed uses the highlighted activity from its own card', () => {
  const own = el('div', { role: 'listitem' }, el('div', {}, nest(el('video'), 4), link(linkedInGroupFeed), link(linkedInGroupFeed)));
  const next = el('div', { role: 'listitem' }, link(linkedInPost));
  assert.equal(fixture('https://www.linkedin.com/feed/', el('div', { role: 'list' }, own, next)).detect().url, linkedInActivity);
});
test('LinkedIn activity data-id is read even when data-urn describes a different entity', () => {
  const own = el('div', { 'data-urn': 'urn:li:ugcPost:7517226468083638272', 'data-id': 'urn:li:activity:7517227738701901824' }, el('video'));
  assert.equal(fixture('https://www.linkedin.com/feed/', own).detect().url, linkedInActivity);
});
test('LinkedIn does not use a TikTok author to reinterpret a numeric video ID', () => {
  assert.equal(fixture('https://www.linkedin.com/feed/', el('article', { 'data-video-id': '7517227738701901824' }, el('video'), link('https://www.tiktok.com/@wrong'))).detect().url, '');
});
test('LinkedIn popup inspects the current player rather than forwarding the feed', async () => {
  const { context } = fixture('https://www.linkedin.com/feed/');
  let count = 0;
  context.ViFreeConnection = { api: { scripting: { executeScript: async request => {
    count++; assert.equal(request.target.tabId, 42);
    return [{ frameId: 0, result: { url: linkedInActivity, reason: 'player' } }];
  } } } };
  assert.equal(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.linkedin.com/feed/' }), linkedInActivity);
  assert.equal(count, 1);
  context.ViFreeConnection.api.scripting.executeScript = async () => [{ frameId: 0, result: { url: '', reason: 'missing-link' } }];
  await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.linkedin.com/feed/' }), /Copy link/u);
});

const linkedInAsset = 'D4D05AQHSyntheticVideo';
const linkedInMedia = `https://dms.licdn.com/playlist/vid/v2/${linkedInAsset}/mp4-720p/sample/0/video?e=9999999999&v=beta&t=synthetic`;
const linkedInPoster = `https://media.licdn.com/dms/image/v2/${linkedInAsset}/videocover-high/sample/0/poster`;
function linkedInMetadata(asset = linkedInAsset, url = linkedInMedia, extra = {}) {
  return el('code', { textContent: JSON.stringify({ included: [{ $type: 'com.linkedin.videocontent.VideoPlayMetadata', entityUrn: `urn:li:digitalmediaAsset:${asset}`, progressiveStreams: [{ mediaType: 'video/mp4', width: 478, height: 850, bitRate: 716048, streamingLocations: [{ url }] }], ...extra }] }) });
}
test('LinkedIn blob player uses only progressive media matched to its exact poster asset', () => {
  const own = el('div', { role: 'listitem' }, el('video', { poster: linkedInPoster }), link(linkedInGroupFeed));
  const found = fixture('https://www.linkedin.com/feed/', own, linkedInMetadata()).detect();
  assert.equal(found.url, linkedInMedia); assert.equal(found.reason, 'source');
});
test('LinkedIn chooses the highest reported progressive source without using a path quality label', () => {
  const low = linkedInMedia.replace('mp4-720p', 'mp4-1080p');
  const data = JSON.parse(linkedInMetadata().textContent);
  data.included[0].progressiveStreams.push({ mediaType: 'video/mp4', width: 360, height: 640, bitRate: 300000, streamingLocations: [{ url: low }] });
  assert.equal(fixture('https://www.linkedin.com/feed/', el('video', { poster: linkedInPoster }), el('code', { textContent: JSON.stringify(data) })).detect().url, linkedInMedia);
});
test('LinkedIn direct player sources remain usable without JSON hydration data', () => {
  assert.equal(fixture('https://www.linkedin.com/feed/', el('video', { src: linkedInMedia })).detect().url, linkedInMedia);
});
test('LinkedIn cannot borrow progressive media from another poster or a protected player', () => {
  for (const attrs of [{ poster: linkedInPoster.replace(linkedInAsset, 'OtherAsset') }, { poster: linkedInPoster, mediaKeys: {} }, {}])
    assert.equal(fixture('https://www.linkedin.com/feed/', el('article', {}, el('video', attrs), link(linkedInActivity)), linkedInMetadata()).detect().url, linkedInActivity);
});
test('LinkedIn media rejects untrusted URLs, adaptive-only and encrypted metadata', () => {
  for (const url of [linkedInMedia.replace('dms.licdn.com', 'dms.licdn.com.evil.example'), linkedInMedia.replace('https:', 'http:'), linkedInMedia.replace('dms.licdn.com', 'user:secret@dms.licdn.com'), linkedInMedia.replace('dms.licdn.com', 'dms.licdn.com:8443'), linkedInMedia.replace(linkedInAsset, 'WrongAsset'), linkedInMedia.replace('mp4-720p', 'dash')])
    assert.equal(fixture('https://www.linkedin.com/feed/', el('video', { poster: linkedInPoster }), linkedInMetadata(linkedInAsset, url)).detect().url, '');
  for (const extra of [{ hasDrm: true }, { isEncrypted: true }, { progressiveStreams: [] }])
    assert.equal(fixture('https://www.linkedin.com/feed/', el('video', { poster: linkedInPoster }), linkedInMetadata(linkedInAsset, linkedInMedia, extra)).detect().url, '');
});
test('LinkedIn malformed or oversized hydration JSON does not break permalink detection', () => {
  for (const textContent of [`${linkedInAsset} progressiveStreams { invalid`, linkedInAsset + 'progressiveStreams' + ' '.repeat(2 * 1024 * 1024)])
    assert.equal(fixture('https://www.linkedin.com/feed/', el('article', {}, el('video', { poster: linkedInPoster }), link(linkedInActivity)), el('code', { textContent })).detect().url, linkedInActivity);
});
test('LinkedIn popup accepts matched CDN sources and rejects substituted websites', async () => {
  const { context } = fixture('https://www.linkedin.com/feed/');
  for (const url of [linkedInMedia, 'https://evil.example/video.mp4']) {
    context.ViFreeConnection = { api: { scripting: { executeScript: async () => [{ frameId: 0, result: { url, reason: 'source' } }] } } };
    if (url === linkedInMedia) assert.equal(await context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.linkedin.com/feed/' }), linkedInMedia);
    else await assert.rejects(context.ViFreeVideoDetection.resolve({ id: 42, url: 'https://www.linkedin.com/feed/' }), /Copy link/u);
  }
});
test('LinkedIn SDUI poster images retain the selected asset and its own group page', () => {
  const player = el('div', {}, el('video'), el('div', { class: 'vjs-poster' }, el('img', { src: linkedInPoster })));
  const own = el('div', { role: 'listitem' }, player, link('http://['), link(linkedInGroupFeed));
  const found = fixture('https://www.linkedin.com/feed/', own).detect();
  assert.equal(found.linkedinAsset, linkedInAsset);
  assert.equal(found.sourcePage, linkedInGroupFeed);
  assert.equal(found.url, linkedInActivity);
});
test('LinkedIn background lookup reads only the requested asset without requiring playback', () => {
  const { context } = fixture(linkedInGroupFeed, linkedInMetadata());
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, null, linkedInAsset).url, linkedInMedia);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, null, 'OtherAsset').url, '');
});
test('protected LinkedIn players cannot request a background media lookup', () => {
  const found = fixture('https://www.linkedin.com/feed/', el('article', {}, el('video', { poster: linkedInPoster, mediaKeys: {} }), link(linkedInGroupFeed))).detect();
  assert.equal(found.linkedinAsset, undefined);
});
test('LinkedIn source lookup opens and closes only its own temporary post tab', async () => {
  const { context } = fixture('https://www.linkedin.com/feed/');
  const calls = [];
  context.ViFreeConnection = { api: { tabs: {
    create: async options => { calls.push(['create', options.url, options.active]); return { id: 99 }; },
    sendMessage: async (id, message, target) => { calls.push(['inspect', id, message.asset, target.frameId]); return { url: linkedInMedia, pageUrl: linkedInGroupFeed }; },
    remove: async id => { calls.push(['close', id]); }
  } } };
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: linkedInActivity, sourcePage: linkedInGroupFeed, linkedinAsset: linkedInAsset }), linkedInMedia);
  assert.deepEqual(calls, [['create', linkedInGroupFeed, false], ['inspect', 99, linkedInAsset, 0], ['inspect', 99, linkedInAsset, 0], ['close', 99]]);
  calls.length = 0;
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: linkedInActivity, sourcePage: 'https://evil.example/', linkedinAsset: linkedInAsset }), linkedInActivity);
  assert.equal(calls.length, 0);
});
test('LinkedIn temporary lookup rejects mismatched media and preserves the public post fallback', async () => {
  const { context } = fixture('https://www.linkedin.com/feed/');
  let removed = false, now = 0;
  context.Date = { now: () => now };
  context.ViFreeConnection = { api: { tabs: {
    create: async () => ({ id: 99 }),
    sendMessage: async () => { now += 20000; return { url: linkedInMedia.replace(linkedInAsset, 'OtherAsset'), pageUrl: linkedInGroupFeed }; },
    remove: async id => { assert.equal(id, 99); removed = true; }
  } } };
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: linkedInActivity, sourcePage: linkedInGroupFeed, linkedinAsset: linkedInAsset }), linkedInActivity);
  assert.equal(removed, true);
});
test('LinkedIn optional source hints outside its allowlist never open a privileged lookup', async () => {
  const { context } = fixture('https://www.linkedin.com/feed/');
  let lookups = 0;
  context.ViFreeConnection = { api: { tabs: {
    create: async () => { lookups++; throw new Error('Unexpected lookup'); },
    sendMessage: async () => { throw new Error('Unexpected message'); }, remove: async () => {}
  } } };
  const original = { url: linkedInActivity, sourcePage: linkedInGroupFeed, linkedinAsset: linkedInAsset };
  for (const change of [
    { sourcePage: 'https://evil.example/post' }, { sourcePage: linkedInGroupFeed.replace('https:', 'http:') },
    { sourcePage: linkedInGroupFeed.replace('www.linkedin.com', 'user:secret@www.linkedin.com') },
    { sourcePage: linkedInGroupFeed.replace('www.linkedin.com', 'www.linkedin.com:8443') },
    { sourcePage: 'https://127.0.0.1/post' }, { sourcePage: 'https://192.168.1.2/post' },
    { sourcePage: 'https://[::1]/post' }, { sourcePage: 'https://www.linkedin.com.evil.example/post' },
    { linkedinAsset: '../untrusted' }, { linkedinAsset: 'a'.repeat(101) },
    { url: 'https://www.linkedin.com/feed/update/urn:li:activity:invalid/' }
  ]) {
    const hint = { ...original, ...change };
    assert.equal(await context.ViFreeVideoDetection.resolveSource(hint), hint.url, 'retain ordinary post handling when optional hints do not qualify');
    assert.equal(lookups, 0);
  }
});
test('LinkedIn cleanup leaves a temporary tab alone if the user navigated it elsewhere', async () => {
  const { context } = fixture('https://www.linkedin.com/feed/');
  let removed = false;
  context.ViFreeConnection = { api: { tabs: {
    create: async () => ({ id: 99 }), sendMessage: async () => ({ url: linkedInMedia }),
    get: async () => ({ url: 'https://www.linkedin.com/jobs/' }), remove: async () => { removed = true; }
  } } };
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: linkedInActivity, sourcePage: linkedInGroupFeed, linkedinAsset: linkedInAsset }), linkedInMedia);
  assert.equal(removed, false);
});


const redditMaster = 'https://v.redd.it/selected123/HLSPlaylist.m3u8?f=sd';
test('Reddit uses the selected main master playlist with source identity and separate audio intact', () => {
  const one = reddit('abc123', { src: redditMaster.replace('selected123', 'neighbor123') });
  const own = reddit('def456', { src: redditMaster });
  const { context } = fixture('https://www.reddit.com/r/videos/', one, own);
  const found = context.ViFreeVideoDetection.inspectVideoPage('', true, own.shadowRoot.querySelectorAll('video')[0]);
  assert.equal(found.url, redditMaster);
  assert.equal(found.sourcePage, 'https://www.reddit.com/comments/def456/');
  assert.equal(found.mediaId, 'selected123');
});
test('Reddit never substitutes a rendition, untrusted host, comment or protected player for the main manifest', () => {
  for (const src of [redditMaster.replace('HLSPlaylist', 'CMAF_720'), redditMaster.replace('v.redd.it', 'v.redd.it.evil.example'), redditMaster.replace('https:', 'http:'), redditMaster.replace('v.redd.it', 'user:secret@v.redd.it'), redditMaster.replace('v.redd.it', 'v.redd.it:8443')])
    assert.equal(fixture('https://www.reddit.com/', reddit('abc123', { src })).detect().url, 'https://www.reddit.com/comments/abc123/');
  const own = reddit('abc123', { src: redditMaster }); own.shadowRoot.querySelectorAll('video')[0].mediaKeys = {};
  assert.equal(fixture('https://www.reddit.com/', own).detect().reason, 'player');
  const comment = reddit('abc123', { src: redditMaster, 'comment-id': 't1_other' });
  assert.equal(fixture('https://www.reddit.com/', comment).detect().url, '');
  assert.equal(fixture('https://www.reddit.com/', reddit('abc123', { src: redditMaster, 'post-promoted': '' })).detect().reason, 'advertisement');
});
test('Reddit toolbar and context handoffs preserve the selected asset and canonical post', async () => {
  const { context } = fixture('https://www.reddit.com/');
  const found = { url: redditMaster, reason: 'source', sourcePage: 'https://www.reddit.com/comments/abc123/', mediaId: 'selected123' };
  context.ViFreeConnection = { api: { scripting: { executeScript: async () => [{ frameId: 0, result: found }] } } };
  assert.equal(await context.ViFreeVideoDetection.resolve({ id: 7, url: 'https://www.reddit.com/' }), redditMaster);
  const details = await context.ViFreeVideoDetection.resolveDetails({ id: 7, url: 'https://www.reddit.com/' });
  assert.equal(details.sourcePage, found.sourcePage); assert.equal(details.mediaId, found.mediaId);
});
test('LinkedIn public embed data matches only the requested asset and selects the reported highest bitrate', () => {
  const low = linkedInMedia.replace('mp4-720p', 'mp4-640p');
  const sources = JSON.stringify([{ src: low, type: 'video/mp4', 'data-bitrate': 100 }, { src: linkedInMedia, type: 'video/mp4', 'data-bitrate': 200 }]);
  const video = el('video', { 'data-sources': sources, poster: linkedInPoster });
  // The synthetic selector is extended here only to model attribute existence.
  const original = video.matches.bind(video); video.matches = selector => selector === 'video[data-sources]' || original(selector);
  const { context } = fixture('https://www.linkedin.com/embed/feed/update/urn:li:ugcPost:7519664336286650368/', video);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, null, linkedInAsset).url, linkedInMedia);
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, null, 'OtherAsset').url, '');
  video.mediaKeys = {};
  assert.equal(context.ViFreeVideoDetection.inspectVideoPage('', false, null, linkedInAsset).url, '');
});
test('LinkedIn selected embed lookup accepts ugcPost without inventing an activity ID', async () => {
  const page = 'https://www.linkedin.com/embed/feed/update/urn:li:ugcPost:7519664336286650368/';
  const { context } = fixture('https://www.linkedin.com/feed/'); let opened;
  context.ViFreeConnection = { api: { tabs: {
    create: async options => { opened = options.url; return { id: 99 }; },
    sendMessage: async () => ({ url: linkedInMedia, pageUrl: page }), remove: async () => {}
  } } };
  assert.equal(await context.ViFreeVideoDetection.resolveSource({ url: page, sourcePage: page, linkedinAsset: linkedInAsset }), linkedInMedia);
  assert.equal(opened, page);
});
