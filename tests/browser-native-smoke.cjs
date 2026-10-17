// Opt-in native-host protocol smoke test. This does not certify the installed extension.
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const crypto = require('node:crypto');
const path = require('node:path');
const [exe, value] = process.argv.slice(2);
if (!exe || !value) throw new Error('Usage: node browser-native-smoke.cjs <Vydrop.exe> <public-video-url>');
const url = new URL(value);
if (!['https:', 'http:'].includes(url.protocol) || url.username || url.password || url.port) throw new Error('A public video URL is required.');
const key = fs.readFileSync(path.join(__dirname, '../browser/public-key.txt'), 'utf8').trim();
const id = [...crypto.createHash('sha256').update(Buffer.from(key, 'base64')).digest().subarray(0, 16)]
  .map(b => String.fromCharCode(97 + (b >> 4), 97 + (b & 15))).join('');
const payload = Buffer.from(JSON.stringify({ version: 1, type: 'open-link', url: url.href }));
const size = Buffer.alloc(4); size.writeUInt32LE(payload.length);
const child = spawn(exe, [`chrome-extension://${id}/`], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
let output = Buffer.alloc(0);
let finished = false;
const timeout = setTimeout(() => { child.kill(); finish(new Error('Native-host smoke test timed out.')); }, 17000);
function finish(error) {
  if (finished) return;
  finished = true;
  clearTimeout(timeout);
  try {
    if (error) throw error;
    if (output.length < 4 || output.readUInt32LE(0) !== output.length - 4) throw new Error('Invalid native-host response.');
    const response = JSON.parse(output.subarray(4).toString('utf8'));
    if (!response.ok) throw new Error(response.message);
    console.log(JSON.stringify({ check: 'native-host CLI smoke; installed extension not exercised', url: url.href, response }));
  } catch (error) { console.error(error.message); process.exitCode = 1; }
  // A launched desktop app can inherit these handles. The framed response, not
  // closure of every inherited pipe, is the native-messaging completion signal.
  child.stdout.destroy(); child.stderr.destroy(); child.unref();
}
child.on('error', finish);
child.stderr.resume();
child.stdout.on('data', chunk => {
  output = Buffer.concat([output, chunk]);
  if (output.length >= 4) {
    const size = output.readUInt32LE(0);
    if (!size || size > 65536 || output.length > size + 4) { child.kill(); finish(new Error('Invalid native-host response.')); }
    else if (output.length === size + 4) finish();
  }
});
child.on('close', code => { if (!finished) finish(new Error(code ? 'Native host exited unsuccessfully.' : 'Incomplete native-host response.')); });
child.stdin.end(Buffer.concat([size, payload]));
