// Validate Jekyll's real output, including canonical URLs and both language variants.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const site = path.resolve(process.argv[2] || 'build/site');
const origin = 'https://ziadhassanein.github.io';
const base = '/Vydrop';
function walk(folder) { return fs.readdirSync(folder, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(folder, e.name)) : path.join(folder, e.name)); }
const htmlFiles = walk(site).filter(file => file.endsWith('.html'));
const locales = JSON.parse(fs.readFileSync(path.join(__dirname,'../docs/_data/locales.json'),'utf8'));
const entities = text => text.replaceAll('&amp;', '&').replaceAll('&quot;', '"').replaceAll('&#39;', "'");
const canonicals = new Set();
for (const file of htmlFiles) {
  const text = fs.readFileSync(file, 'utf8');
  const route = '/' + path.relative(site, file).replaceAll('\\', '/').replace(/index\.html$/u, '');
  const canonical = `${origin}${base}${route}`;
  canonicals.add(canonical);
  const title = text.match(/<title>([^<]+)<\/title>/u)?.[1];
  assert.ok(title && entities(title).length <= 60, `${route}: title ${title}`);
  const desc = text.match(/<meta name="description" content="([^"]*)"/u)?.[1];
  assert.ok(desc && entities(desc).length <= 155, `${route}: description`);
  assert.ok(text.includes(`rel="canonical" href="${canonical}"`), `${route}: canonical`);
  assert.equal((text.match(/rel="canonical"/gu) || []).length, 1, `${route}: exactly one canonical`);
  assert.ok(text.includes('name="robots" content="index, follow, max-image-preview:large"'), `${route}: indexing and image previews`);
  assert.ok(text.includes('hreflang="en"') && text.includes('hreflang="ar"'), `${route}: alternates`);
  const alternates=[...text.matchAll(/<link rel="alternate" hreflang="([^"]+)" href="([^"]+)"/gu)];
  assert.equal(new Set(alternates.map(item=>item[1])).size,alternates.length,`${route}: unique alternate languages`);
  for(const [,language,href] of alternates) {
    const alternate=new URL(href);
    assert.equal(alternate.origin,origin,`${route}: alternate origin`);
    assert.ok(alternate.pathname.startsWith(base+'/'),`${route}: alternate path`);
    const targetFile=path.join(site,alternate.pathname.slice(base.length),alternate.pathname.endsWith('/')?'index.html':'');
    const target=fs.readFileSync(targetFile,'utf8');
    assert.ok(target.includes(`href="${canonical}"`),`${route}: reciprocal alternate ${language}`);
  }
  const localeHome=locales.homes.find(locale=>locale.url===route);
  if(localeHome) {
    for(const locale of locales.homes) assert.ok(text.includes(`hreflang="${locale.code}" href="${origin}${base}${locale.url}"`),`${route}: home alternate ${locale.code}`);
    assert.ok(text.includes(`<html lang="${localeHome.code}"`) || localeHome.code===localeHome.lang && text.includes(`<html lang="${localeHome.lang}"`),`${route}: localized document language`);
  }
  assert.ok(!text.includes('{{') && !text.includes('{%'), `${route}: unrendered Liquid`);
  assert.equal((text.match(/<h1\b/gu) || []).length, 1, `${route}: one H1`);
  if (route === '/' || route === '/ar/') assert.ok(text.includes('<table>') && text.includes('<thead>'), `${route}: semantic comparison table`);
  if (route === '/browser-extension-setup.html') {
    assert.ok(text.includes('dir="rtl" lang="ar"'), `${route}: localized Arabic block`);
    assert.ok(!text.includes('**فك الضغط:**'), `${route}: Arabic steps render as Markdown`);
  }
  assert.doesNotMatch(text, /<script[^>]+src=/u);
  assert.doesNotMatch(text, /github\.com\/ZiadHassanein\/ViFree(?:\/|["#])|github\.io\/ViFree\/|<meta[^>]+content="[^"]*noindex/u, `${route}: stale identity or indexing block`);
  assert.ok(text.includes(`type="text/plain" title="Vydrop documentation as text" href="${origin}${base}/llms-full.txt"`), `${route}: readable text reference`);
  const googleVerification = [...text.matchAll(/<meta name="google-site-verification" content="([^"]*)"/gu)];
  assert.ok(googleVerification.length <= 1 && googleVerification.every(tag => tag[1].trim()), `${route}: ownership tags must be unique and nonempty`);
  if (route.startsWith('/ar/')) assert.ok(text.includes('<html lang="ar" dir="rtl">'), `${route}: Arabic direction`);
  for (const match of text.matchAll(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/gu)) {
    const data = JSON.parse(match[1]);
    if (data['@graph']) {
      const faq = data['@graph'].find(item => item['@type'] === 'FAQPage');
      if (faq) for (const item of faq.mainEntity) {
        assert.ok(text.includes(entities(item.name)), `${route}: visible FAQ question`);
        const visible = entities(text.replace(/<[^>]+>/gu, ' ')).replace(/\s+/gu, ' ');
        assert.ok(visible.includes(item.acceptedAnswer.text.replace(/\s+/gu, ' ')), `${route}: visible FAQ answer`);
      }
      const app = data['@graph'].find(item => item['@type'] === 'SoftwareApplication');
      const code = data['@graph'].find(item => item['@type'] === 'SoftwareSourceCode');
      if (app) {
        assert.equal(code.codeRepository, 'https://github.com/ZiadHassanein/Vydrop');
        assert.equal(code.targetProduct['@id'], app['@id']);
        assert.equal(app.sameAs, code.codeRepository);
      }
      const page = data['@graph'].find(item => item['@type'] === 'WebPage');
      if (page) {
        assert.equal(page.url, canonical, `${route}: page entity has the canonical URL`);
        assert.equal(page.about['@id'], `${origin}${base}/#application`, `${route}: stable product identity`);
        assert.equal(page.isPartOf['@id'], `${origin}${base}/#website`);
        assert.ok(data['@graph'].some(item => item['@type'] === 'Person' && item['@id'] === page.author['@id']));
        const breadcrumb = data['@graph'].find(item => item['@type'] === 'BreadcrumbList');
        if (!localeHome) {
          assert.ok(breadcrumb && text.includes('class="breadcrumbs"'), `${route}: visible breadcrumbs match structured data`);
          assert.equal(breadcrumb.itemListElement.at(-1).item, canonical);
        }
        if (page.dateModified) assert.ok(text.includes(`<time datetime="${page.dateModified}">`), `${route}: review date is visible`);
      }
    }
  }
  for (const match of text.matchAll(/(?:href|src|srcset)="([^"]+)"/gu)) {
    const href = entities(match[1]);
    if (/^(mailto:|#|chrome:|edge:|about:)/u.test(href)) continue;
    const url = new URL(href, canonical);
    if (url.origin !== origin) continue;
    assert.ok(url.pathname.startsWith(base + '/'), `${route}: base URL ${url.pathname}`);
    let local = path.join(site, decodeURIComponent(url.pathname.slice(base.length)));
    if (fs.existsSync(local) && fs.statSync(local).isDirectory()) local = path.join(local, 'index.html');
    assert.ok(fs.existsSync(local), `${route}: missing ${href}`);
    if (url.hash && local.endsWith('.html')) {
      const target = fs.readFileSync(local, 'utf8');
      const anchor = decodeURIComponent(url.hash.slice(1));
      assert.ok(target.includes(`id="${anchor}"`), `${route}: missing anchor ${href}`);
    }
  }
}
assert.ok(fs.existsSync(path.join(site, 'sitemap.xml')));
const {sitemapUrls} = require('./notify-search.cjs');
const sitemap = sitemapUrls(fs.readFileSync(path.join(site, 'sitemap.xml'), 'utf8'), new URL(`${origin}${base}/`));
assert.deepEqual([...sitemap].sort(), [...canonicals].sort(), 'Sitemap lists exactly the canonical HTML pages, including every localized guide');
assert.ok(fs.readFileSync(path.join(site, 'robots.txt'), 'utf8').includes(`${origin}${base}/sitemap.xml`));
assert.ok(fs.readFileSync(path.join(site, 'llms-full.txt'), 'utf8').includes('Source: https://github.com/ZiadHassanein/Vydrop/blob/main/README.md'));
console.log(`PASS: ${htmlFiles.length} rendered pages: metadata, links, alternates, JSON-LD, RTL, assets and sitemap.`);
