// Headless fixture: real DOM, keyboard and pointer events; isolated from personal browser profiles.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..'), output = path.join(root, 'build/manager-browser-captures');
(async()=>{
  fs.mkdirSync(output,{recursive:true}); const browser=await chromium.launch({channel:'msedge',headless:true}); let count=0;
  try {
    for(const [lang,scheme,width] of [['en','light',1200],['en','dark',1200],['ar','light',1200],['ar','dark',390]]) {
      const page=await browser.newPage({viewport:{width,height:800},colorScheme:scheme});
      await page.setContent(`<html lang="${lang}"><button id="anchor" style="position:absolute;left:20px;top:20px">Download by Vydrop</button><div style="margin:110px 20px;background:#eee;width:60%;height:400px">Local video fixture</div></html>`);
      await page.route('https://fixture.example/brand-logo-dark.svg',r=>r.fulfill({contentType:'image/svg+xml',body:fs.readFileSync(path.join(root,'assets/brand-logo-dark.svg')),headers:{'access-control-allow-origin':'*'}}));
      await page.route('https://fixture.example/poster.jpg',r=>r.fulfill({contentType:'image/jpeg',body:fs.readFileSync(path.join(root,'docs/images/extension/video/poster.jpg')),headers:{'access-control-allow-origin':'*'}}));
      await page.evaluate(()=>{window.browser={runtime:{getURL:file=>'https://fixture.example/'+file,getManifest:()=>({name:'Vydrop'})}};});
      await page.addScriptTag({path:path.join(root,'browser/common/connection.js')});
      await page.addScriptTag({path:path.join(root,'browser/common/quality-picker.js')});
      await page.evaluate(lang=>{
        window.sent=[];window.fixtureFailure=false;
        document.querySelector('#anchor').onclick=()=>VydropQualityPicker.open({anchor:document.querySelector('#anchor'),arabic:lang==='ar',inspect:async()=>{if(window.fixtureFailure)throw Object.assign(new Error('old host'),{code:'unsupported'});return {title:lang==='ar'?'إعداد إضافة Vydrop · فيديو اختبار محلي':'Vydrop extension setup · Local test video',source:'ziadhassanein.github.io',thumbnail:'https://fixture.example/poster.jpg',videos:[{format:'mp4',heights:[1080,720,480,360]},{format:'webm',heights:[720,480]}],audio:['m4a','mp3']};},send:async output=>{window.sent.push(output);return {ok:true,code:'opened'};}});
      },lang);
      await page.locator('#anchor').click(); const dialog=page.getByRole('dialog'); await dialog.getByRole('radio',{name:'720p',exact:true}).waitFor();
      await dialog.getByRole('button',{name:lang==='ar'?'صوت':'Audio',exact:true}).click();
      await dialog.getByRole('radio',{name:'MP3',exact:true}).click();
      assert.match(await dialog.locator('.selection').textContent(),/^MP3 · /u);
      await dialog.getByRole('button',{name:lang==='ar'?'فيديو':'Video',exact:true}).click();
      await dialog.getByRole('radio',{name:'480p',exact:true}).click();
      await dialog.getByRole('radio',{name:'WEBM',exact:true}).click(); await dialog.getByRole('radio',{name:'480p',exact:true}).click();
      assert.equal(await dialog.locator('.selection').textContent(),'WEBM · 480p');
      await dialog.getByRole('radio',{name:'480p',exact:true}).press('Home');
      assert.equal(await dialog.getByRole('radio',{name:'720p',exact:true}).getAttribute('aria-checked'),'true');
      await dialog.getByRole('radio',{name:'720p',exact:true}).press('End');
      assert.equal(await dialog.getByRole('radio',{name:'480p',exact:true}).getAttribute('aria-checked'),'true'); count++;
      const box=await dialog.boundingBox();assert.ok(box.x>=0&&box.y>=0&&box.x+box.width<=width&&box.y+box.height<=800);
      assert.equal(await dialog.locator('.wordmark').evaluate(e=>e.complete&&e.naturalWidth>0),true);
      await dialog.locator('.identity img').evaluate(e=>e.decode());
      await dialog.screenshot({path:path.join(output,`quality-${lang}-${scheme}-${width}.png`)});
      await page.screenshot({path:path.join(output,`host-${lang}-${scheme}-${width}.png`)});
      await dialog.getByRole('button',{name:lang==='ar'?'فتح في Vydrop':'Open in Vydrop',exact:true}).click();
      assert.deepEqual(await page.evaluate(()=>sent[0]),{kind:'video',format:'webm',height:480}); count++;
      await page.keyboard.press('Escape'); assert.equal(await dialog.count(),0);assert.equal(await page.locator('#anchor').evaluate(e=>e===document.activeElement),true);count++;
      await page.evaluate(()=>{fixtureFailure=true;});await page.locator('#anchor').click();await dialog.getByText(lang==='ar'?'يمكنك اختيار الجودة داخل التطبيق.':'You can choose the quality inside the app.').waitFor();
      await dialog.getByRole('button',{name:lang==='ar'?'فتح في Vydrop':'Open in Vydrop',exact:true}).click();assert.equal(await page.evaluate(()=>sent.length),2);count++;
      await page.close();
    }
    const page=await browser.newPage({viewport:{width:1200,height:800}});
    await page.route('https://example.com/watch',r=>r.fulfill({contentType:'text/html',body:'<html lang="en"><video style="display:block;margin:130px 24px;width:600px;height:340px"></video></html>'}));
    await page.goto('https://example.com/watch');
    await page.evaluate(()=>{
      window.forwarded=[];
      Object.defineProperties(document.querySelector('video'),{readyState:{get:()=>4},paused:{get:()=>false},ended:{get:()=>false},currentSrc:{get:()=> 'https://cdn.example.com/video.mp4'}});
      window.browser={runtime:{getManifest:()=>({name:'Vydrop'}),sendMessage:async message=>{forwarded.push(message);return message.type==='vydrop-inspect-video'?{ok:true,media:{title:'Real overlay integration fixture',source:'example.com',videos:[{format:'mp4',heights:[720,480]}],audio:['m4a']}}:{ok:true,code:'opened'};}}};
    });
    for(const file of ['connection.js','video-detection.js','quality-picker.js','video-overlay.js'])await page.addScriptTag({path:path.join(root,'browser/common',file)});
    await page.locator('#vifree-video-action').getByRole('button').click();
    const picker=page.getByRole('dialog');await picker.getByRole('radio',{name:'480p',exact:true}).click();
    await picker.getByRole('button',{name:'Open in Vydrop',exact:true}).click();
    const forwarded=await page.evaluate(()=>window.forwarded);
    assert.equal(forwarded.length,2);assert.equal(forwarded[0].type,'vydrop-inspect-video');
    assert.equal(forwarded[1].type,'vifree-open-video');assert.deepEqual(forwarded[1].output,{kind:'video',format:'mp4',height:480});
    assert.equal(forwarded[1].url,'https://cdn.example.com/video.mp4');count++;
    await page.close();
    const compact=await browser.newPage({viewport:{width:390,height:640},colorScheme:'dark',forcedColors:'active'});
    await compact.setContent('<button id="anchor">Choose audio</button>');await compact.addScriptTag({path:path.join(root,'browser/common/quality-picker.js')});
    await compact.evaluate(()=>{document.querySelector('#anchor').onclick=()=>VydropQualityPicker.open({anchor:document.querySelector('#anchor'),inspect:async()=>({title:'Long audio-only source title · Local test media without artwork',source:'example.com',videos:[],audio:['mp3','m4a','opus','wav','flac']}),send:async output=>{window.audioChoice=output;return {ok:true};}});});
    await compact.locator('#anchor').click();const audioDialog=compact.getByRole('dialog');await audioDialog.getByRole('radio',{name:'FLAC',exact:true}).click();
    assert.equal(await audioDialog.getByRole('button',{name:'Video',exact:true}).isDisabled(),true);
    await audioDialog.getByRole('button',{name:'Open in Vydrop',exact:true}).click();assert.deepEqual(await compact.evaluate(()=>audioChoice),{kind:'audio',format:'flac',height:0});
    assert.equal(await audioDialog.evaluate(e=>e.scrollWidth<=e.clientWidth),true);count++;
    await compact.screenshot({path:path.join(output,'host-audio-forced-colors-390.png')});await compact.close();
    console.log(`PASS: ${count} rendered quality menu checks: themes, Arabic, compact bounds, exact output delivery, Escape/focus restoration and legacy fallback.`);
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
