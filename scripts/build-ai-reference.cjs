// Publish the existing documentation in one readable file; never invent AI-only product claims.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replaceAll('\r\n', '\n');
const config = read('docs/_config.yml');
const base = new URL(config.match(/^baseurl: (.+)$/mu)[1] + '/', config.match(/^url: (.+)$/mu)[1]);
const repository = JSON.parse(read('docs/_data/product.yml').match(/^repository: (.+)$/mu)[1]);
const decode = text => text.replaceAll('&amp;', '&').replaceAll('&quot;', '"').replaceAll('&#39;', "'").replaceAll('&lt;', '<').replaceAll('&gt;', '>').replaceAll('&nbsp;', ' ');

function absoluteLink(ref, source) {
  if (/^[a-z][a-z\d+.-]*:/iu.test(ref)) return ref;
  const [file, anchor = ''] = ref.split('#');
  const relative = path.relative(root, path.resolve(root, path.dirname(source), file || path.basename(source))).replaceAll('\\', '/');
  let url;
  if (relative.startsWith('docs/')) {
    url = new URL(relative.slice(5).replace(/(^|\/)index\.md$/u, '$1').replace(/\.md$/u, '.html'), base).href;
  } else {
    const kind = fs.existsSync(path.join(root, relative)) && fs.statSync(path.join(root, relative)).isDirectory() ? 'tree' : 'blob';
    url = `${repository}/${kind}/main/${relative}`;
  }
  return url + (anchor ? `#${anchor}` : '');
}

function asText(source) {
  let text = read(source).replace(/^---\n[\s\S]*?\n---\n/u, '');
  text = text.replace(/\{% include (facts(?:-ar)?\.md) %\}/gu, (_, file) => read(`docs/_includes/${file}`).trim());
  text = text.replace(/\{% include setup-shot\.html id="([^"]+)"(?: lang="([^"]+)")? %\}/gu, (_, id, language) => {
    const shot=JSON.parse(read('docs/_data/extension_shots.json'))[id];
    const lang=language || (source.startsWith('docs/ar/') ? 'ar' : 'en');
    const image=shot.localized_paths?.[lang] || shot.path;
    return `![${shot.alt[lang]}](${new URL(image.slice(1),base).href})\n\n${shot.caption[lang]}`;
  });
  if (/\{%|\{\{/u.test(text)) throw new Error(`${source}: unresolved template in plain-text reference`);
  text = text.replace(/<!--[\s\S]*?-->/gu, '').replace(/<svg\b[^>]*>[\s\S]*?<\/svg>/gu, '');
  text = text.replace(/<a\b[^>]*href="([^"]+)"[^>]*>([\s\S]*?)<\/a>/gu, (_, ref, label) => {
    const caption = label.replace(/<img\b[^>]*alt="([^"]*)"[^>]*>/gu, '$1').replace(/<[^>]*>/gu, ' ').replace(/\s+/gu, ' ').trim();
    return caption ? `[${decode(caption)}](${absoluteLink(decode(ref), source)})` : '';
  });
  text = text.replace(/<img\b[^>]*alt="([^"]*)"[^>]*>/gu, '$1');
  text = text.replace(/<h([1-6])\b[^>]*>([\s\S]*?)<\/h\1>/gu, (_, level, heading) => '\n' + '#'.repeat(Number(level)) + ' ' + heading + '\n');
  text = text.replace(/<\/?(?:p|div|span|figure|figcaption|picture|source|video|track|details|summary|em|bdi|a|br)\b[^>]*>/gu, '\n');
  text = text.replace(/(!?\[[^\]\n]*\])\(([^)\s]+)\)/gu, (_, label, ref) => `${label}(${absoluteLink(ref, source)})`);
  return decode(text).replace(/[ \t]+\n/gu, '\n').replace(/\n{3,}/gu, '\n\n').trim();
}

function buildReference() {
  function pages(folder) {
    return fs.readdirSync(path.join(root, folder), {withFileTypes: true}).flatMap(entry => {
      const file = `${folder}/${entry.name}`;
      if (entry.isDirectory()) return entry.name.startsWith('_') ? [] : pages(file);
      return entry.name.endsWith('.md') && /^alternate: true$/mu.test(read(file)) ? [file] : [];
    });
  }
  const sources = ['README.md', 'SECURITY.md', ...pages('docs').sort()];
  return read('docs/llms.txt').trim() + '\n\n---\n\n' +
    'This reference is generated from the official published guides and repository documents. Each section links to its original. The desktop interface supports English and Arabic; translated website guides do not add application interface languages. Product limitations and release/source distinctions apply throughout.\n\n' +
    sources.map(source => `Source: ${absoluteLink(path.basename(source), source)}\n\n${asText(source)}`).join('\n\n---\n\n') + '\n';
}

if (require.main === module) {
  const output = buildReference();
  const target = 'docs/llms-full.txt';
  if (process.argv.includes('--check')) {
    if (!fs.existsSync(path.join(root, target)) || read(target) !== output) {
      console.error(`Stale ${target}; run node scripts/build-ai-reference.cjs.`);
      process.exitCode = 1;
    } else console.log('PASS: plain-text reference matches the current English/Arabic guides and repository documents.');
  } else {
    fs.writeFileSync(path.join(root, target), output);
    console.log(`Generated ${target} (${Buffer.byteLength(output)} bytes).`);
  }
}

module.exports = {buildReference, absoluteLink};
