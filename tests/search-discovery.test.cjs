const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {buildReference, absoluteLink} = require('../scripts/build-ai-reference.cjs');
const {settings, sitemapUrls} = require('../scripts/notify-search.cjs');
const root = path.resolve(__dirname, '..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replaceAll('\r\n', '\n');

test('AI reference contains the actual bilingual guides and repository limitations without templates', () => {
  const text = buildReference();
  assert.equal(read('docs/llms-full.txt'), text);
  assert.ok(Buffer.byteLength(text) < 200 * 1024);
  assert.equal((text.match(/^Source: https:/gmu) || []).length, 24);
  for (const source of ['README.md', 'SECURITY.md']) assert.ok(text.includes(`Source: https://github.com/ZiadHassanein/Vydrop/blob/main/${source}`));
  for (const locale of ['', 'ar/']) for (const page of ['supported-sites', 'browser-extension-setup', 'youtube-subtitles-srt']) {
    assert.ok(text.includes(`Source: https://ziadhassanein.github.io/Vydrop/${locale}${page}.html`));
  }
  assert.ok(text.includes(read('docs/_includes/facts.md').trim()));
  assert.ok(text.includes(read('docs/_includes/facts-ar.md').trim()));
  assert.ok(text.includes('not Authenticode-signed'));
  assert.ok(text.includes('Private, sign-in-only and DRM-protected media are unsupported.'));
  assert.doesNotMatch(text, /\{%|\{\{|<svg\b|github\.com\/ZiadHassanein\/ViFree(?:\/|\b)|github\.io\/ViFree\//u);
});

test('Plain-text links retain canonical guide routes, images and source anchors', () => {
  assert.equal(absoluteLink('supported-sites.md', 'docs/index.md'), 'https://ziadhassanein.github.io/Vydrop/supported-sites.html');
  assert.equal(absoluteLink('index.md', 'docs/ar/index.md'), 'https://ziadhassanein.github.io/Vydrop/ar/');
  assert.equal(absoluteLink('../index.md', 'docs/ar/supported-sites.md'), 'https://ziadhassanein.github.io/Vydrop/');
  assert.equal(absoluteLink('#chrome', 'docs/browser-extension-setup.md'), 'https://ziadhassanein.github.io/Vydrop/browser-extension-setup.html#chrome');
  assert.equal(absoluteLink('ARCHITECTURE.md', 'README.md'), 'https://github.com/ZiadHassanein/Vydrop/blob/main/ARCHITECTURE.md');
});

test('IndexNow only accepts URLs within the published key directory, without queries or credentials', () => {
  const {base, key, keyLocation} = settings();
  assert.equal(read(`docs/${key}.txt`).trim(), key);
  assert.equal(keyLocation, new URL(`${key}.txt`, base).href);
  const xml = url => `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"><url><loc>${url}</loc></url></urlset>`;
  assert.deepEqual(sitemapUrls(xml(base.href), base), [base.href]);
  assert.deepEqual(sitemapUrls(xml(new URL('ar/', base).href), base), [new URL('ar/', base).href]);
  for (const invalid of ['https://example.com/', 'https://ziadhassanein.github.io/Other/', 'https://ziadhassanein.github.io/Vydrop-sibling/', new URL('../', base).href, `${base.href}?q=1`, `${base.href}#fragment`, 'http://ziadhassanein.github.io/Vydrop/', 'https://user:pass@ziadhassanein.github.io/Vydrop/']) {
    assert.throws(() => sitemapUrls(xml(invalid), base), /outside the verified site scope/u);
  }
  assert.throws(() => sitemapUrls('<urlset></urlset>', base), /URL count/u);
  assert.throws(() => sitemapUrls('<sitemapindex></sitemapindex>', base), /Expected a URL sitemap/u);
});
