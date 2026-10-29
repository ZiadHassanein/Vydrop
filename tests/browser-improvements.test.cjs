const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const read = file => fs.readFileSync(path.join(__dirname, '../browser/common', file), 'utf8');
const timers = {setTimeout: (fn,ms) => {const t=setTimeout(fn,ms); t.unref(); return t;},clearTimeout};

function connection(host, clock = Date) {
  const sent=[], legacy=[];
  const api = {runtime:{getManifest:()=>({name:'Vydrop'}),connectNative:()=>{
    let listener;
    return {onMessage:{addListener:fn=>{listener=fn;}},onDisconnect:{addListener:()=>{}},disconnect:()=>{},postMessage:message=>{sent.push(message);Promise.resolve().then(()=>host(message)).then(response=>listener(response));}};
  },sendNativeMessage:async(name,message)=>{legacy.push({name,message});return {ok:true,code:'opened'};}}};
  const context=vm.createContext({browser:api,URL,Date:clock,...timers}); vm.runInContext(read('connection.js'),context);
  return {client:context.ViFreeConnection,sent,legacy,context};
}
test('v2 health and queued delivery use one port and preserve explicit source context',async()=>{
  const fixture=connection(m=>({ok:true,code:m.type==='ping'?'ready':'queued',protocol:2,requestId:m.requestId,appRunning:true}));
  assert.equal(fixture.sent.length,0,'Loading the companion never starts a native host');
  assert.equal((await fixture.client.testConnection()).appRunning,true);
  const result=await fixture.client.sendLink('https://media.example.com/movie.mp4',{enqueue:true,sourcePage:'https://example.com/post'});
  assert.equal(result.code,'queued');assert.equal(fixture.legacy.length,0);
  const request=fixture.sent.at(-1);assert.equal(request.version,2);assert.equal(request.type,'enqueue');assert.equal(request.sourcePage,'https://example.com/post');
  assert.equal(new Set(fixture.sent.map(m=>m.requestId)).size,fixture.sent.length);
});

test('app-language reads share a bounded passive health request and accept only supported languages', async () => {
  let now = 5000, language = 'en', ok = true;
  const fixture = connection(m => ({ ok, protocol: 2, requestId: m.requestId, language }), { now: () => now });
  assert.deepEqual(await Promise.all([fixture.client.getLanguage(), fixture.client.getLanguage()]), ['en', 'en']);
  assert.equal(fixture.sent.length, 1);
  assert.equal(fixture.sent[0].type, 'ping'); assert.equal(fixture.sent[0].url, undefined);
  language = 'ar'; now += 2100;
  assert.equal(await fixture.client.getLanguage(), 'ar');
  language = '<unsafe>'; now += 2100;
  assert.equal(await fixture.client.getLanguage(), 'ar', 'An invalid/older host preference keeps the last app language');
  ok = false; language = 'en'; now += 2100;
  assert.equal(await fixture.client.getLanguage(), 'ar');
  assert.equal(fixture.legacy.length, 0, 'Passive preference checks never start a legacy open-link fallback');
});

test('background shares app language only with its own web content scripts', async () => {
  const fixture = connection(m => ({ ok: true, requestId: m.requestId, language: 'ar' }));
  const titles = []; let handler;
  const api = fixture.client.api;
  Object.assign(api.runtime, { id: 'fixture', onMessage: { addListener: fn => { handler = fn; } }, onInstalled: { addListener: () => {} } });
  api.contextMenus = { update: async (id, value) => titles.push(value.title), onClicked: { addListener: () => {} } };
  fixture.context.ViFreeVideoDetection = {};
  vm.runInContext(read('background.js'), fixture.context);
  const message = { version: 1, type: 'vydrop-language' };
  const sender = { id: 'fixture', tab: { id: 2 }, frameId: 0, url: 'https://example.com/video' };
  let rejection;
  assert.equal(handler(message, { ...sender, id: 'other-extension' }, value => { rejection = value; }), false);
  assert.equal(rejection.code, 'invalid'); assert.equal(fixture.sent.length, 0);
  const response = await new Promise(resolve => assert.equal(handler(message, sender, resolve), true));
  assert.equal(response.language, 'ar'); assert.equal(fixture.sent.length, 1); assert.equal(fixture.sent[0].type, 'ping');
  assert.equal(titles.length, 2); assert.ok(titles.every(title => /[\u0600-\u06ff]/u.test(title)));
});
test('new companion falls back to exact v1 envelope with an older host',async()=>{
  const fixture=connection(()=>({ok:false,code:'invalid'}));
  assert.equal((await fixture.client.sendLink('https://example.com/video')).ok,true);
  assert.equal(fixture.legacy.length,1);assert.deepEqual(JSON.parse(JSON.stringify(fixture.legacy[0].message)),{version:1,type:'open-link',url:'https://example.com/video'});
});
test('parallel explicit actions correlate v2 replies without crossing URLs',async()=>{
  const fixture=connection(m=>({ok:true,protocol:2,requestId:m.requestId,code:m.url?.includes('second')?'queued':'opened'}));
  const [first,second]=await Promise.all([fixture.client.sendLink('https://example.com/first'),fixture.client.sendLink('https://example.com/second')]);
  assert.equal(first.code,'opened');assert.equal(second.code,'queued');
});
test('v2 still rejects credentials and controls before native delivery',async()=>{
  const fixture=connection(m=>({ok:true,protocol:2,requestId:m.requestId}));
  for(const value of ['file:///C:/private.mp4','https://user:secret@example.com/video','https://example.com/a\n'])await assert.rejects(fixture.client.sendLink(value));
  assert.equal(fixture.sent.length,0);
});

test('the popup keeps download and queue actions without connection-test or discovery controls', () => {
  const popup = read('popup.html');
  assert.match(popup, /id="send"/u); assert.match(popup, /id="enqueue"/u);
  assert.doesNotMatch(popup, /id="(?:test|discovery|streams|refresh-streams)"|<details|popup-discovery/u);
  assert.doesNotMatch(read('background.js'), /media-discovery|VydropMediaDiscovery/u);
});

test('format inspection and selected outputs preserve an explicit user choice', async()=>{
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['formats','output-selection'],media:{title:'Fixture',videos:[{format:'webm',heights:[720]}],audio:[]},code:'opened'}));
  assert.equal((await f.client.inspectLink('https://example.com/video')).title,'Fixture');
  const output={kind:'video',format:'webm',height:720};
  await f.client.sendLink('https://example.com/video',{output});
  assert.deepEqual(JSON.parse(JSON.stringify(f.sent.at(-1).output)),output);
  assert.equal(f.sent.filter(m=>m.type==='inspect').length,1);
});
test('an older host never receives unsupported format fields', async()=>{
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['ping','enqueue'],code:'opened'}));
  await assert.rejects(f.client.inspectLink('https://example.com/video'),e=>e.code==='unsupported');
  await f.client.sendLink('https://example.com/video',{output:{kind:'video',format:'mp4',height:720}});
  assert.equal(f.sent.at(-1).output,undefined);
});
const choice={kind:'video',format:'webm',height:720};
test('a confirmed choice requests immediate download only from a capable host',async()=>{
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['formats','output-selection','start-download'],code:m.type==='download'?'started':'ready'}));
  assert.equal((await f.client.sendLink('https://example.com/confirmed',{output:choice,startDownload:true})).code,'started');
  assert.equal(f.sent.at(-1).type,'download');assert.deepEqual(JSON.parse(JSON.stringify(f.sent.at(-1).output)),choice);
});
test('an older selection-capable host opens for manual start and keeps the exact choice',async()=>{
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['output-selection'],code:'opened'}));
  assert.equal((await f.client.sendLink('https://example.com/confirmed',{output:choice,startDownload:true})).code,'opened-legacy');
  assert.equal(f.sent.at(-1).type,'open-link');assert.deepEqual(JSON.parse(JSON.stringify(f.sent.at(-1).output)),choice);
});
test('concurrent and repeated explicit inspections share one real metadata request',async()=>{
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['formats'],media:{title:m.url,videos:[],audio:[]}}));
  const results=await Promise.all([f.client.inspectLink('https://example.com/a'),f.client.inspectLink('https://example.com/a')]);
  assert.equal(results[0],results[1]);await f.client.inspectLink('https://example.com/a');
  assert.equal(f.sent.filter(m=>m.type==='inspect').length,1);
  await f.client.inspectLink('https://example.com/b');assert.equal(f.sent.filter(m=>m.type==='inspect').length,2);
});
test('failed inspections are evicted and can retry immediately',async()=>{
  let fail=true;const f=connection(m=>({ok:m.type!=='inspect'||!fail,protocol:2,requestId:m.requestId,capabilities:['formats'],media:fail?null:{title:'Recovered',videos:[],audio:[]},code:'unavailable'}));
  await assert.rejects(f.client.inspectLink('https://example.com/a'));fail=false;
  assert.equal((await f.client.inspectLink('https://example.com/a')).title,'Recovered');
  assert.equal(f.sent.filter(m=>m.type==='inspect').length,2);
});
test('inspection cache evicts older videos at its eight-source limit',async()=>{
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['formats'],media:{title:'Fixture',videos:[],audio:[]}}));
  for(let i=0;i<9;i++) await f.client.inspectLink('https://example.com/'+i);
  await f.client.inspectLink('https://example.com/0');assert.equal(f.sent.filter(m=>m.type==='inspect').length,10);
});

test('a different-video request waits behind inspection without starting a premature deadline',async()=>{
  let release;
  const f=connection(m=>m.type==='inspect'&&m.url.endsWith('/a')?new Promise(resolve=>{release=()=>resolve({ok:true,protocol:2,requestId:m.requestId,media:{title:'A',videos:[],audio:[]}});}):({ok:true,protocol:2,requestId:m.requestId,capabilities:['formats'],media:{title:'B',videos:[],audio:[]}}));
  const first=f.client.inspectLink('https://example.com/a');
  await new Promise(resolve=>setImmediate(resolve));assert.equal(typeof release,'function');
  const second=f.client.inspectLink('https://example.com/b');
  await new Promise(resolve=>setImmediate(resolve));
  assert.equal(f.sent.length,2,'only the first ping and inspection are posted while the host is occupied');
  release();assert.equal((await first).title,'A');assert.equal((await second).title,'B');
});
test('cached inspection expires after sixty seconds and signed source changes do not share entries',async()=>{
  let now=1000;class Clock extends Date {static now(){return now;}}
  const f=connection(m=>({ok:true,protocol:2,requestId:m.requestId,capabilities:['formats'],media:{title:'Fixture',videos:[],audio:[]}}),Clock);
  await f.client.inspectLink('https://example.com/a?token=1');now+=59000;
  await f.client.inspectLink('https://example.com/a?token=1');assert.equal(f.sent.filter(m=>m.type==='inspect').length,1);
  now+=1001;await f.client.inspectLink('https://example.com/a?token=1');
  await f.client.inspectLink('https://example.com/a?token=2');assert.equal(f.sent.filter(m=>m.type==='inspect').length,3);
});

test('first-use desktop preparation remains actionable and cannot trigger an automatic download', async()=>{
  let preparing=true;
  const f=connection(m=>({ok:m.type==='ping'||!preparing,protocol:2,requestId:m.requestId,capabilities:['formats'],code:preparing?'preparing':'formats',media:preparing?null:{title:'Ready after setup',videos:[],audio:[]}}));
  await assert.rejects(f.client.inspectLink('https://example.com/fresh',{sourcePage:'https://example.com/post',mediaId:'fresh_asset'}),e=>e.code==='preparing');
  assert.equal(f.sent.some(m=>['open-link','download','enqueue'].includes(m.type)),false);
  assert.equal(f.sent.at(-1).sourcePage,'https://example.com/post');assert.equal(f.sent.at(-1).mediaId,'fresh_asset');
  preparing=false;assert.equal((await f.client.inspectLink('https://example.com/fresh')).title,'Ready after setup');
  assert.equal(f.sent.filter(m=>m.type==='inspect').length,2,'setup failure is not cached');
});

for (const browser of ['Chrome','Edge','Firefox']) test(`loaded companion reports its actual version for ${browser}`,async()=>{
  const sent=[];let listener;
  const api={runtime:{getManifest:()=>({name:'Vydrop',version:'1.5.8'}),connectNative:()=>({onMessage:{addListener:fn=>listener=fn},onDisconnect:{addListener(){}},disconnect(){},postMessage:message=>{sent.push(message);queueMicrotask(()=>listener({ok:true,protocol:2,requestId:message.requestId}));}})}};
  if(browser==='Firefox')api.runtime.getBrowserInfo=async()=>({name:'Firefox'});
  const context=vm.createContext({browser:api,URL,navigator:{userAgent:browser==='Edge'?'Chrome/130 Edg/130':'Chrome/130'},queueMicrotask,...timers});vm.runInContext(read('connection.js'),context);
  await context.ViFreeConnection.testConnection();assert.equal(sent.length,1);assert.equal(sent[0].companionVersion,'1.5.8');assert.equal(sent[0].companionBrowser,browser);
});
test('a loaded current companion retries the original health envelope for an older native host',async()=>{
  const sent=[];let listener;
  const api={runtime:{getManifest:()=>({name:'Vydrop',version:'1.5.8'}),connectNative:()=>({onMessage:{addListener:fn=>listener=fn},onDisconnect:{addListener(){}},disconnect(){},postMessage:message=>{sent.push(message);queueMicrotask(()=>listener({ok:!message.companionVersion,code:message.companionVersion?'invalid':'ready',protocol:2,requestId:message.requestId}));}})}};
  const context=vm.createContext({browser:api,URL,queueMicrotask,...timers});vm.runInContext(read('connection.js'),context);
  assert.equal((await context.ViFreeConnection.testConnection()).ok,true);assert.equal(sent.length,2);assert.equal(sent[1].companionVersion,undefined);assert.equal(sent[1].companionBrowser,undefined);
});


test('an explicit download gets a fresh native host after pending inspection completes', async () => {
  const ports = [], sent = [];
  let finishInspection;
  const api = { runtime: { getManifest: () => ({ name: 'Vydrop' }), connectNative: () => {
    let reply; const port = { id: ports.length, disconnected: false, onMessage: { addListener: fn => { reply = fn; } },
      onDisconnect: { addListener() {} }, disconnect() { this.disconnected = true; },
      postMessage(message) {
        sent.push({ port: this.id, type: message.type });
        const respond = () => reply({ ok: true, protocol: 2, requestId: message.requestId, code: 'started',
          capabilities: ['formats','output-selection','start-download'], media: { title: 'Fixture' } });
        if (message.type === 'inspect') finishInspection = respond; else queueMicrotask(respond);
      } };
    ports.push(port); return port;
  } } };
  const context = vm.createContext({ browser: api, URL, ...timers }); vm.runInContext(read('connection.js'), context);
  const client = context.ViFreeConnection;
  const inspect = client.inspectLink('https://example.com/video');
  while (!finishInspection) await new Promise(setImmediate);
  const download = client.sendLink('https://example.com/video', { output: { kind: 'audio', format: 'mp3', height: 0 }, startDownload: true });
  await new Promise(setImmediate);
  assert.equal(ports[0].disconnected, false, 'an active format lookup is not interrupted');
  finishInspection(); await inspect; await download;
  assert.equal(ports[0].disconnected, true);
  assert.notEqual(sent.find(m => m.type === 'download').port, sent.find(m => m.type === 'inspect').port);
});
