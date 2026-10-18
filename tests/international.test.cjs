const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const read = file => fs.readFileSync(path.join(root,file),'utf8').replaceAll('\r\n','\n');
const version = read('Directory.Build.props').match(/<Version>([^<]+)<\/Version>/u)[1];
const locales = JSON.parse(read('docs/_data/locales.json'));
const regions = JSON.parse(read('docs/_data/arabic-regions.json'));
const faq = JSON.parse(read('docs/_data/faq.json'));

test('The six home variants resolve to complete content with accurate language and metadata', () => {
  assert.deepEqual(locales.homes.map(locale=>locale.code),['en','ar','en-US','fr-FR','de-DE','es-ES']);
  for(const locale of locales.homes) {
    const file = `docs${locale.url}index.md`;
    const text = read(file);
    const front = text.split('---\n')[1];
    for(const [field,max] of [['title',60],['description',155]]) {
      const value=JSON.parse(front.match(new RegExp(`^${field}: (.+)$`,'mu'))[1]);
      assert.ok(value.length<=max,`${file}: ${field} is ${value.length} characters`);
    }
    assert.ok(front.includes(`lang: ${locale.lang}\n`),file);
    assert.match(front,/^home: true$/mu);
    assert.equal((text.match(/^# /gmu)||[]).length,1,file);
    assert.ok(locales.ui[locale.lang].skip && locales.ui[locale.lang].privacy,file);
    for(const item of faq[locale.lang]) {
      assert.ok(text.includes(item.question)&&text.includes(item.answer),`${file}: FAQ is visible and matches its structured data`);
    }
    if(!['en','ar'].includes(locale.code)) {
      assert.match(text,/id="formats"/u);
      assert.match(text,/id="extension"/u);
      assert.match(text,/Browser\/chromium/u);
      assert.match(text,/Browser\/firefox\/manifest\.json/u);
      assert.ok(text.includes(`Vydrop-v${version}-win-x64.zip`), `${file}: downloads the current release`);
      assert.ok(text.split(/\s+/u).length>350,`${file}: a usable guide, not a thin country doorway`);
    }
  }
});

test('Arabic wording covers exactly the 22 requested countries in visible explanatory guides', () => {
  const codes=['AE','BH','DJ','DZ','EG','IQ','JO','KM','KW','LB','LY','MA','MR','OM','PS','QA','SA','SD','SO','SY','TN','YE'];
  assert.deepEqual(regions.map(region=>region.code).sort(),codes);
  for(const file of ['docs/arabic-regional-guide.md','docs/ar/arabic-regional-guide.md']) {
    const text=read(file);
    for(const region of regions) {
      assert.ok(text.includes(`id="${region.code.toLowerCase()}"`) && text.includes(region.question),`${file}: ${region.code}`);
    }
  }
  assert.ok(regions.filter(region=>['DJ','KM','MR','SO'].includes(region.code)).every(region=>region.register==='العربية المبسطة'));
});

test('Website translations preserve the actual app language scope and source identity', () => {
  const schema=read('docs/_includes/jsonld.html');
  assert.match(schema,/"inLanguage": \["en", "ar"\]/u);
  assert.ok(schema.includes('page.language_code | default: page.lang'));
  assert.ok(read('docs/_includes/head.html').includes('site.data.locales.homes'));
  const text=read('docs/llms-full.txt');
  for(const locale of locales.homes) assert.ok(text.includes(`Source: https://ziadhassanein.github.io/Vydrop${locale.url}`),locale.code);
  assert.match(text,/translated website guides do not add application interface languages/u);
});
