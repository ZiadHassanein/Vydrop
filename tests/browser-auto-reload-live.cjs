// Isolated Chromium extension lifecycle test. Does not use a personal browser/profile or native app.
const fs=require('node:fs/promises'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const {chromium}=require(process.env.VYDROP_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..');
const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
async function ready(worker){for(let attempt=0;attempt<100;attempt++){if(await worker.evaluate(()=>Boolean(globalThis.VydropAutoReload))){await worker.evaluate(()=>VydropAutoReload.ready);return;}await wait(50);}throw Error("Reload worker failed to start");}
async function refreshed(context,id,expected){
  await wait(1500);
  const probe=await context.newPage();await probe.goto(`chrome-extension://${id}/popup.html`);
  assert.equal(await probe.evaluate(()=>chrome.runtime.getManifest().version),expected,'the browser must activate the modified manifest');
  // Reloaded MV3 workers may sleep until a new event; an ordinary extension message wakes one.
  await probe.evaluate(()=>chrome.runtime.sendMessage({type:'isolated-reload-probe'}).catch(()=>{}));
  for(let attempt=0;attempt<100;attempt++){
    for(const candidate of context.serviceWorkers())try{if(await candidate.evaluate(()=>chrome.runtime.getManifest().version)===expected){await ready(candidate);await probe.close();return candidate;}}catch{}
    await wait(50);
  }
  throw Error('The updated background did not start');
}
async function descriptor(folder){const manifest=JSON.parse(await fs.readFile(path.join(folder,'manifest.json'))),files={};for(const name of(await fs.readdir(folder)).filter(name=>name!=='companion-update.json'))files[name]=crypto.createHash('sha256').update(await fs.readFile(path.join(folder,name))).digest('hex');await fs.writeFile(path.join(folder,'companion-update.json'),JSON.stringify({schema:1,name:manifest.name,version:manifest.version,files}));}
(async()=>{
  const input=process.argv[2];if(!input)throw Error('Pass the packaged Chromium folder');
  const target=path.join(root,'build','auto-reload-live-'+crypto.randomUUID()),extension=path.join(target,'extension');await fs.mkdir(target,{recursive:true});await fs.cp(path.resolve(input),extension,{recursive:true});
  const context=await chromium.launchPersistentContext(path.join(target,'profile'),{headless:true,executablePath:process.env.VYDROP_CHROMIUM,args:[`--disable-extensions-except=${extension}`,`--load-extension=${extension}`]});
  try{
    const setup=await context.newPage();await setup.goto("chrome://extensions");const devMode=setup.locator("extensions-toolbar #devMode");if(!await devMode.evaluate(node=>node.checked))await devMode.click();await setup.close();
    let worker=context.serviceWorkers()[0]||await context.waitForEvent('serviceworker');await ready(worker);
    const original=await worker.evaluate(()=>chrome.runtime.getManifest().version);assert.match(original,/^\d+\.\d+\.\d+$/u);const nextVersion=original.split('.').map((part,index)=>index===2?String(Number(part)+1):part).join('.');
    const id=new URL(worker.url()).host;
    const page=await context.newPage();await page.goto(`chrome-extension://${id}/popup.html`);
    await page.evaluate(()=>{globalThis.updateLease=chrome.runtime.connect({name:'vydrop-update-active'});});
    const oldConnection=await fs.readFile(path.join(extension,'connection.js'));
    const manifest=JSON.parse(await fs.readFile(path.join(extension,'manifest.json')));manifest.version=nextVersion;await fs.writeFile(path.join(extension,'manifest.json'),JSON.stringify(manifest));
    await fs.appendFile(path.join(extension,'connection.js'),'\n// Isolated updated bundle fixture.\n');await descriptor(extension);
    const newConnection=await fs.readFile(path.join(extension,'connection.js'));await fs.writeFile(path.join(extension,'connection.js'),oldConnection);
    await page.evaluate(()=>{updateLease.disconnect();});await wait(100);await worker.evaluate(()=>VydropAutoReload.check());
    assert.equal(await worker.evaluate(()=>chrome.runtime.getManifest().version),original,'partial copy must not reload');
    console.log('PASS: actual unpacked resource fetch sees changed files and rejects partial updates');
    await page.evaluate(()=>{globalThis.updateLease=chrome.runtime.connect({name:'vydrop-update-active'});});await fs.writeFile(path.join(extension,'connection.js'),newConnection);await wait(100);
    await worker.evaluate(()=>VydropAutoReload.check());assert.equal(await worker.evaluate(()=>chrome.runtime.getManifest().version),original,'open chooser must defer reload');
    console.log('PASS: same-extension activity defers an otherwise ready update');
    await page.evaluate(()=>{updateLease.disconnect();});await page.close();await wait(100);
    // Fire the real registered browser alarm; the production repeating alarm runs every minute.
    await worker.evaluate(()=>chrome.alarms.create('vydrop-companion-update',{when:Date.now()+500}));
    worker=await refreshed(context,id,nextVersion);
    assert.equal(await worker.evaluate(()=>chrome.runtime.getManifest().version),nextVersion);
    console.log('PASS: browser alarm automatically reloads the complete unpacked extension to its complete updated version');
    await worker.evaluate(()=>VydropAutoReload.check());await wait(750);assert.equal(await worker.evaluate(()=>chrome.runtime.getManifest().version),nextVersion);
    console.log('PASS: reloaded background is healthy without a reload loop');
    manifest.version=original;await fs.writeFile(path.join(extension,'manifest.json'),JSON.stringify(manifest));await fs.writeFile(path.join(extension,'connection.js'),oldConnection);await descriptor(extension);
    await worker.evaluate(()=>chrome.alarms.create('vydrop-companion-update',{when:Date.now()+500}));worker=await refreshed(context,id,original);
    assert.equal(await worker.evaluate(()=>chrome.runtime.getManifest().version),original);console.log('PASS: a complete rollback reloads its previous version automatically');
    console.log(JSON.stringify({result:'PASS',browser:'isolated Chromium',checks:5,profile:target}));
  }finally{await context.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
