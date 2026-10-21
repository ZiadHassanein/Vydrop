const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const read = file => fs.readFileSync(path.join(__dirname, '../browser/common', file), 'utf8');
const timers = {setTimeout: (fn,ms) => {const t=setTimeout(fn,ms); t.unref(); return t;},clearTimeout};

function connection(host) {
  const sent=[], legacy=[];
  const api = {runtime:{getManifest:()=>({name:'Vydrop'}),connectNative:()=>{
    let listener;
    return {onMessage:{addListener:fn=>{listener=fn;}},onDisconnect:{addListener:()=>{}},disconnect:()=>{},postMessage:message=>{sent.push(message);queueMicrotask(()=>listener(host(message)));}};
  },sendNativeMessage:async(name,message)=>{legacy.push({name,message});return {ok:true,code:'opened'};}}};
  const context=vm.createContext({browser:api,URL,Date,...timers}); vm.runInContext(read('connection.js'),context);
  return {client:context.ViFreeConnection,sent,legacy};
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

async function discovery(granted=true) {
  let now=100000, receive;const permissions={onAdded:{addListener:()=>{}},onRemoved:{addListener:()=>{}},contains:async()=>granted};
  const api={permissions,webRequest:{onHeadersReceived:{addListener:()=>{}}},tabs:{onRemoved:{addListener:()=>{}}},runtime:{id:'fixture',getURL:()=> 'chrome-extension://fixture/',onMessage:{addListener:fn=>{receive=fn;}}}};
  const context=vm.createContext({browser:api,URL,Date:{now:()=>now}});vm.runInContext(read('media-discovery.js'),context);await context.VydropMediaDiscovery.refresh();
  return {engine:context.VydropMediaDiscovery,tick:()=>{now+=91000;},receive};
}
const request=(url='https://cdn.example.com/master.m3u8',mime='application/vnd.apple.mpegurl')=>({url,tabId:1,frameId:0,documentId:'document-1',initiator:'https://example.com',type:'xmlhttprequest',statusCode:200,responseHeaders:[{name:'Content-Type',value:mime},{name:'Set-Cookie',value:'PRIVATE_SESSION'}]});
test('optional discovery stores bounded media metadata, never cookies or request bodies',async()=>{
  const f=await discovery();f.engine.observe(request());
  assert.equal(f.engine.list(1).length,1);assert.equal(f.engine.list(1)[0].kind,'HLS');
  assert.doesNotMatch(JSON.stringify(f.engine.list(1)),/PRIVATE_SESSION|Set-Cookie|responseHeaders/u);
  for(let i=0;i<100;i++)f.engine.observe(request(`https://cdn.example.com/${i}.mp4`,'video/mp4'));
  assert.equal(f.engine.list(1).length,48);
  f.tick();assert.equal(f.engine.list(1).length,0);
});
test('discovery remains inactive when optional permission is denied',async()=>{
  const f=await discovery(false);f.engine.observe(request());assert.equal(f.engine.list(1).length,0);
});
test('navigation clears candidates and tab/frame identity remains attached',async()=>{
  const f=await discovery();f.engine.observe({...request(),frameId:3});assert.equal(f.engine.list(1)[0].frameId,3);
  f.engine.observe({...request(),frameId:3,type:'sub_frame'});assert.equal(f.engine.list(1).length,0);
  f.engine.observe(request());f.engine.observe({...request(),type:'main_frame'});assert.equal(f.engine.list(1).length,0);
});
test('discovery rejects local, credential-bearing, non-media and failed responses',async()=>{
  const f=await discovery();
  for(const item of [request('http://127.0.0.1/x.mp4','video/mp4'),request('https://user:secret@example.com/x.mp4','video/mp4'),request('https://example.com/x','text/html'),{...request(),statusCode:403},request('https://example.com/a.m4s','video/mp4'),request('https://example.com/segment.ts','video/mp2t')])f.engine.observe(item);
  assert.equal(f.engine.list(1).length,0);
});
test('sites and content scripts cannot enumerate observed URLs',async()=>{
  const f=await discovery();f.engine.observe(request());
  for(const sender of [{id:'another',url:'chrome-extension://another/popup.html'},{id:'fixture',url:'https://example.com',tab:{id:1}}]){
    let reply;assert.equal(f.receive({type:'vydrop-discovery-list',tabId:1},sender,v=>{reply=v;}),false);assert.equal(reply.ok,false);
  }
  const reply=await new Promise(resolve=>f.receive({type:'vydrop-discovery-list',tabId:1},{id:'fixture',url:'chrome-extension://fixture/popup.html'},resolve));assert.equal(reply.candidates.length,1);
  const tabReply=await new Promise(resolve=>f.receive({type:'vydrop-discovery-list',tabId:1},{id:'fixture',url:'chrome-extension://fixture/popup.html',tab:{id:2}},resolve));assert.equal(tabReply.candidates.length,1);
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
