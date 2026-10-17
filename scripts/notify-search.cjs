// One explicit notification after publication. No account, paid API or recurring job.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const endpoint = 'https://api.indexnow.org/indexnow';

function settings() {
  const config = fs.readFileSync(path.join(root, 'docs/_config.yml'), 'utf8');
  const base = new URL(config.match(/^baseurl: (.+)$/mu)[1].trim() + '/', config.match(/^url: (.+)$/mu)[1].trim());
  const key = JSON.parse(fs.readFileSync(path.join(root, 'docs/_data/search.json'), 'utf8')).indexNowKey;
  if (!/^[a-z\d-]{8,128}$/iu.test(key)) throw new Error('Invalid IndexNow ownership key.');
  if (base.protocol !== 'https:') throw new Error('Only the public HTTPS site may be submitted.');
  return {base, key, keyLocation: new URL(`${key}.txt`, base).href};
}

function sitemapUrls(xml, base) {
  if (!xml.includes('<urlset')) throw new Error('Expected a URL sitemap, not a sitemap index.');
  const urls = [...xml.matchAll(/<loc>\s*([^<]+?)\s*<\/loc>/gu)].map(match => match[1].replaceAll('&amp;', '&'));
  if (!urls.length || urls.length > 10000) throw new Error('Sitemap URL count must be between 1 and 10000.');
  for (const href of urls) {
    const url = new URL(href);
    if (!url.href.startsWith(base.href) || url.origin !== base.origin || url.hash || url.search || url.username || url.password)
      throw new Error(`Refusing a URL outside the verified site scope: ${href}`);
  }
  return [...new Set(urls)];
}

async function run(submit) {
  const {base, key, keyLocation} = settings();
  async function get(url) {
    const response = await fetch(url, {redirect: 'error', signal: AbortSignal.timeout(45000)});
    if (response.status !== 200) throw new Error(`${url}: HTTP ${response.status}`);
    return response.text();
  }
  const publishedKey = await get(keyLocation);
  if (publishedKey.trim() !== key) throw new Error('The published ownership file does not match.');
  const urls = sitemapUrls(await get(new URL('sitemap.xml', base)), base);
  if (!submit) {
    console.log(`Ready: ${urls.length} sitemap URLs under ${base.href}. Use --submit to notify IndexNow.`);
    return;
  }
  const response = await fetch(endpoint, {
    method: 'POST', redirect: 'error', signal: AbortSignal.timeout(45000),
    headers: {'Content-Type': 'application/json; charset=utf-8'},
    body: JSON.stringify({host: base.host, key, keyLocation, urlList: urls})
  });
  if (![200, 202].includes(response.status)) throw new Error(`IndexNow rejected the notification: HTTP ${response.status}; ${(await response.text()).slice(0, 500)}`);
  const receipt = {
    submittedAt: new Date().toISOString(), endpoint, status: response.status, keyLocation, urls,
    outcome: response.status === 200 ? 'URLs received; indexing is not guaranteed.' : 'URLs received; ownership-key validation is pending. Indexing is not guaranteed.'
  };
  fs.mkdirSync(path.join(root, 'build'), {recursive: true});
  fs.writeFileSync(path.join(root, 'build/indexnow-submission.json'), JSON.stringify(receipt, null, 2) + '\n');
  console.log(`IndexNow HTTP ${response.status}: ${urls.length} URLs. ${receipt.outcome}`);
}

if (require.main === module) run(process.argv.includes('--submit')).catch(error => { console.error(error.message); process.exitCode = 1; });
module.exports = {settings, sitemapUrls};
