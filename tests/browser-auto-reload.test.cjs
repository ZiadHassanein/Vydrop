const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const crypto = require('node:crypto');
const source = fs.readFileSync(path.join(__dirname, '../browser/common/auto-reload.js'), 'utf8');
const names = ['manifest.json','auto-reload.js','background.js','connection.js','video-detection.js','quality-picker.js','video-overlay.js','popup.js','popup.html','popup.css','brand-logo-dark.svg','icon.png','icon16.png','icon48.png'];
const hash = content => crypto.createHash('sha256').update(content).digest('hex');
function bundle(version = '1.5.8', name = 'Vydrop') {
  const files = Object.fromEntries(names.map(file => [file, Buffer.from(file === 'manifest.json' ? JSON.stringify({version,name}) : `updated ${file}`)]));
  files['companion-update.json'] = Buffer.from(JSON.stringify({schema:1,version,name,files:Object.fromEntries(Object.entries(files).map(([file,content])=>[file,hash(content)]))}));
  return files;
}
async function fixture(files = bundle(), options = {}) {
  const listeners = {}, calls = [], alarms = [];
  let reloads = 0, active = false;
  const runtime = {id:'own-extension',getManifest:()=>({name:'Vydrop',version:'1.5.7'}),getURL:name=>`chrome-extension://own-extension/${name}`,reload:()=>{reloads++;if(options.reloadThrows)throw Error('browser refused');},onStartup:{addListener:fn=>listeners.start=fn},onConnect:{addListener:fn=>listeners.connect=fn}};
  const api = {runtime,alarms:{get:async()=>options.existingAlarm ? {} : null,create:async(...args)=>alarms.push(args),onAlarm:{addListener:fn=>listeners.alarm=fn}}};
  const context = vm.createContext({chrome:api,crypto:crypto.webcrypto,TextDecoder,Uint8Array,ViFreeConnection:{hasPendingRequests:()=>active},fetch:async(url,init)=>{
    assert.ok(url.startsWith('chrome-extension://own-extension/')); assert.equal(init.cache,'no-store');assert.equal(init.redirect,'error');
    const name=url.split('/').at(-1);calls.push(name);const content=files[name];
    return {ok:Boolean(content),arrayBuffer:async()=>content.buffer.slice(content.byteOffset,content.byteOffset+content.byteLength)};
  }});
  vm.runInContext(source,context); await context.VydropAutoReload.ready;
  return {context,files,calls,alarms,listeners,runtime,get reloads(){return reloads;},active:value=>{active=value;},check:()=>context.VydropAutoReload.check()};
}
test('matching installed version only checks one local descriptor and restores its alarm',async()=>{
  const f=await fixture(bundle('1.5.7'));assert.equal(f.reloads,0);assert.deepEqual(f.calls,['companion-update.json']);
  assert.equal(f.alarms.length,1);assert.equal(f.alarms[0][0],'vydrop-companion-update');assert.equal(f.alarms[0][1].periodInMinutes,1);
  await f.listeners.alarm({name:'unrelated'});assert.equal(f.calls.length,1);
});
test('a complete updated local bundle reloads once and never loops in the same worker',async()=>{
  const f=await fixture();assert.equal(f.reloads,1);assert.equal(f.calls.length,names.length+2);await f.check();assert.equal(f.reloads,1);
});
test('partly copied files defer reload until their content matches the package',async()=>{
  const files=bundle(), valid=files['connection.js'];files['connection.js']=Buffer.from('previous file');const f=await fixture(files);
  assert.equal(f.reloads,0);files['connection.js']=valid;await f.check();assert.equal(f.reloads,1);
});
test('an update rollback can restore and reload an older coherent companion',async()=>{
  const f=await fixture(bundle('1.5.6'));assert.equal(f.reloads,1);
});
test('a missing legacy descriptor or different stable/preview channel is ignored',async()=>{
  for(const files of [{},bundle('1.5.8','Vydrop Preview')]) {const f=await fixture(files);assert.equal(f.reloads,0);}
});
test('malformed descriptors, unsafe paths, incomplete inventories and mismatched manifests never reload',async()=>{
  for(const mutate of [v=>v.schema=2,v=>v.version='garbage',v=>delete v.files['popup.js'],v=>v.files['../outside.js']='a'.repeat(64),v=>v.files['https://remote.example/script']='a'.repeat(64),v=>v.files['companion-update.json']='a'.repeat(64),v=>v.files['popup.js']='BAD',v=>v.version='1.5.9']) {
    const files=bundle(), value=JSON.parse(files['companion-update.json']);mutate(value);files['companion-update.json']=Buffer.from(JSON.stringify(value));const f=await fixture(files);assert.equal(f.reloads,0);
  }
});
test('oversized local files never trigger reload',async()=>{
  const files=bundle();files['companion-update.json']=Buffer.alloc(4*1024*1024+1);const f=await fixture(files);assert.equal(f.reloads,0);
});
test('native actions and same-extension chooser ports defer reload until released',async()=>{
  const f=await fixture(bundle('1.5.7'));Object.assign(f.files,bundle());f.active(true);const before=f.calls.length;await f.check();assert.equal(f.calls.length,before);f.active(false);
  let disconnect;f.listeners.connect({name:'vydrop-update-active',sender:{id:'own-extension'},onDisconnect:{addListener:fn=>disconnect=fn}});
  await f.check();assert.equal(f.reloads,0);disconnect();await f.check();assert.equal(f.reloads,1);
});
test('foreign extension activity cannot block a local reload and existing alarms are retained',async()=>{
  const f=await fixture(bundle('1.5.7'),{existingAlarm:true});Object.assign(f.files,bundle());
  f.listeners.connect({name:'vydrop-update-active',sender:{id:'other-extension'},onDisconnect:{addListener:()=>assert.fail()}});await f.check();assert.equal(f.reloads,1);assert.equal(f.alarms.length,0);
});
test('concurrent checks cannot duplicate reload, and a browser refusal can be retried',async()=>{
  const f=await fixture(bundle('1.5.7'));Object.assign(f.files,bundle());await Promise.all([f.check(),f.check(),f.check()]);assert.equal(f.reloads,1);
  const rejected=await fixture(bundle(),{reloadThrows:true});const before=rejected.reloads;await rejected.check();assert.equal(rejected.reloads,before+1);
});
test('native request leases include queued work and release after rejection',async()=>{
  let held=0;
  const api={runtime:{getManifest:()=>({name:'Vydrop'}),connect:()=>{held++;return{disconnect:()=>held--};}}};
  const context=vm.createContext({chrome:api,URL,setTimeout,clearTimeout,document:{}});vm.runInContext(fs.readFileSync(path.join(__dirname,'../browser/common/connection.js'),'utf8'),context);
  const connection=context.ViFreeConnection;
  const first=connection.testConnection(),second=connection.testConnection();assert.equal(connection.hasPendingRequests(),true);assert.equal(held,2);
  await Promise.allSettled([first,second]);assert.equal(connection.hasPendingRequests(),false);assert.equal(held,0);
  const release=connection.holdReload();release();release();assert.equal(held,0);assert.equal(connection.hasPendingRequests(),false);
});
test('a long-lived chooser sends heartbeats and cleans its timer on close or disconnect',()=>{
  let beat,disconnect,closed=0,messages=0,cleared=0;
  const api={runtime:{getManifest:()=>({name:'Vydrop'}),connect:()=>({onDisconnect:{addListener:fn=>disconnect=fn},postMessage:()=>messages++,disconnect:()=>closed++})}};
  const context=vm.createContext({chrome:api,URL,document:{},setInterval:(fn,ms)=>{assert.equal(ms,20000);beat=fn;return 7;},clearInterval:id=>{if(id===7)cleared++;}});
  vm.runInContext(fs.readFileSync(path.join(__dirname,'../browser/common/connection.js'),'utf8'),context);
  const release=context.ViFreeConnection.holdReload();beat();assert.equal(messages,1);disconnect();assert.equal(cleared,1);release();release();assert.equal(closed,1);assert.equal(context.ViFreeConnection.hasPendingRequests(),false);
});
test('a descriptor that changes during verification cannot activate a mixed update',async()=>{
  const files=bundle(),original=files['companion-update.json'];let reads=0;
  const descriptorKey='companion-update.json';Object.defineProperty(files,descriptorKey,{get(){reads++;return reads>1?Buffer.from(original.toString().replace('1.5.8','1.5.9')):original;}});
  const f=await fixture(files);assert.equal(f.reloads,0);
});
if(process.env.VIFREE_BROWSER_PACKAGE)test('both packaged reload descriptors cover the exact on-disk files',()=>{
  for(const browser of ['chromium','firefox']) {
    const folder=path.join(process.env.VIFREE_BROWSER_PACKAGE,browser),descriptor=JSON.parse(fs.readFileSync(path.join(folder,'companion-update.json'))),manifest=JSON.parse(fs.readFileSync(path.join(folder,'manifest.json')));
    assert.equal(descriptor.version,manifest.version);assert.equal(descriptor.name,manifest.name);
    for(const name of names)assert.ok(descriptor.files[name],name);
    assert.deepEqual(Object.keys(descriptor.files).sort(),fs.readdirSync(folder).filter(name=>name!=='companion-update.json').sort());
    for(const [name,expected]of Object.entries(descriptor.files))assert.equal(hash(fs.readFileSync(path.join(folder,name))),expected,`${browser}/${name}`);
  }
});
