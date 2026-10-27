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
      const page=await browser.newPage({viewport:{width,height:640},colorScheme:scheme});
      await page.setContent(`<html lang="${lang}"><button id="anchor" style="position:absolute;left:20px;top:20px">Download by Vydrop</button><div style="margin:110px 20px;background:#eee;width:60%;height:400px">Local video fixture</div></html>`);
      await page.route('https://fixture.example/brand-logo-dark.svg',r=>r.fulfill({contentType:'image/svg+xml',body:fs.readFileSync(path.join(root,'assets/brand-logo-dark.svg')),headers:{'access-control-allow-origin':'*'}}));
      await page.route('https://fixture.example/poster.jpg',r=>r.fulfill({contentType:'image/jpeg',body:fs.readFileSync(path.join(root,'docs/images/extension/video/poster.jpg')),headers:{'access-control-allow-origin':'*'}}));
      await page.evaluate(()=>{window.browser={runtime:{getURL:file=>'https://fixture.example/'+file,getManifest:()=>({name:'Vydrop'})}};});
      await page.addScriptTag({path:path.join(root,'browser/common/connection.js')});
      await page.addScriptTag({path:path.join(root,'browser/common/quality-picker.js')});
      await page.evaluate(lang=>{
        window.sent=[];window.fixtureFailure=false;
        document.querySelector('#anchor').onclick=()=>VydropQualityPicker.open({anchor:document.querySelector('#anchor'),arabic:lang==='ar',inspect:async()=>{if(window.fixtureFailure)throw Object.assign(new Error('old host'),{code:'unsupported'});return {title:lang==='ar'?'إعداد إضافة Vydrop · فيديو اختبار محلي':'Vydrop extension setup · Local test video',source:'ziadhassanein.github.io',thumbnail:'https://fixture.example/poster.jpg',videos:[{format:'mp4',heights:[720,480,360,240,144]},{format:'mkv',heights:[720,480,360,240,144]},{format:'webm',heights:[720,480]}],audio:['m4a','mp3']};},send:async output=>{window.sent.push(output);return {ok:true,code:'opened'};}});
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
      const box=await dialog.boundingBox();assert.ok(box.x>=0&&box.y>=0&&box.x+box.width<=width&&box.y+box.height<=640);
      assert.equal(await dialog.locator('.wordmark').evaluate(e=>e.complete&&e.naturalWidth>0),true);
      await dialog.locator('.identity img').evaluate(e=>e.decode());
      await dialog.getByRole('radio',{name:'MP4',exact:true}).click();
      assert.equal(await dialog.getByRole('radio').count(),8);
      assert.equal(await dialog.evaluate(e=>e.scrollHeight<=e.clientHeight+1),true,'full five-quality layout fits');
      await dialog.screenshot({path:path.join(output,`quality-${lang}-${scheme}-${width}.png`)});
      await page.screenshot({path:path.join(output,`host-${lang}-${scheme}-${width}.png`)});
      await dialog.getByRole('radio',{name:'WEBM',exact:true}).click();await dialog.getByRole('radio',{name:'480p',exact:true}).click();
      assert.equal(await dialog.evaluate(e=>e.scrollHeight<=e.clientHeight+1),true,'three formats and five qualities fit without scrolling');
      await dialog.getByRole('button',{name:lang==='ar'?'تنزيل':'Download',exact:true}).evaluate(e=>e.click());
      assert.equal(await page.evaluate(()=>sent.length),0,'synthetic clicks cannot start downloads');
      await dialog.getByRole('button',{name:lang==='ar'?'تنزيل':'Download',exact:true}).click();
      assert.deepEqual(await page.evaluate(()=>sent[0]),{kind:'video',format:'webm',height:480}); count++;
      assert.equal(await dialog.count(),0),'successful delivery closes the chooser';assert.equal(await page.locator('#anchor').evaluate(e=>e===document.activeElement),true);count++;
      await page.evaluate(()=>{fixtureFailure=true;});await page.locator('#anchor').click();
      await page.waitForFunction(()=>!document.querySelector('#vydrop-quality-picker'));
      assert.equal(await page.evaluate(()=>sent.length),1,'failed format lookup never sends a fallback download');
      assert.equal(await page.locator('#anchor').evaluate(e=>e===document.activeElement),true);count++;
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
    await picker.getByRole('button',{name:'Download',exact:true}).click();
    const forwarded=await page.evaluate(()=>window.forwarded);
    assert.equal(forwarded.length,2);assert.equal(forwarded[0].type,'vydrop-inspect-video');
    assert.equal(forwarded[1].type,'vifree-open-video');assert.deepEqual(forwarded[1].output,{kind:'video',format:'mp4',height:480});
    assert.equal(forwarded[1].url,'https://cdn.example.com/video.mp4');count++;
    assert.equal(await picker.count(),0);
    const overlayButton=page.locator('#vifree-video-action').getByRole('button');
    for (const failure of [{ok:false,code:'inspect'},{ok:true,media:{videos:[],audio:[]}},{ok:true,media:{videos:'invalid',audio:[]}}]) {
      await page.evaluate(failure=>{browser.runtime.sendMessage=async message=>{forwarded.push(message);return failure;};},failure);
      const before=await page.evaluate(()=>forwarded.length);
      await overlayButton.click();await page.waitForFunction(()=>!document.querySelector('#vydrop-quality-picker'));
      assert.equal(await overlayButton.textContent(),'Download by Vydrop');
      assert.equal(await overlayButton.isEnabled(),true);
      assert.equal(await page.evaluate(()=>forwarded.length),before+1,'format failures cannot send an open or download command');count++;
    }
    for (const language of ['en','ar']) {
      // The overlay follows the page language at load, so use its own isolated page for each locale.
      const setup=await browser.newPage({viewport:{width:390,height:640}});
      await setup.route('https://example.com/fresh',r=>r.fulfill({contentType:'text/html',body:`<html lang="${language}"><video style="display:block;margin:100px 4px;width:350px;height:220px"></video></html>`}));
      await setup.goto('https://example.com/fresh');
      await setup.evaluate(()=>{window.forwarded=[];Object.defineProperties(document.querySelector('video'),{currentSrc:{get:()=> 'https://cdn.example.com/fresh.mp4'}});window.browser={runtime:{getManifest:()=>({name:'Vydrop'}),sendMessage:async m=>{forwarded.push(m);return {ok:false,code:window.failureCode};}}};});
      for (const file of ['connection.js','video-detection.js','quality-picker.js','video-overlay.js']) await setup.addScriptTag({path:path.join(root,'browser/common',file)});
      const action=setup.locator('#vifree-video-action').getByRole('button');
      for (const code of ['unavailable','preparing','unsupported','busy','storage']) {
        await setup.evaluate(code=>window.failureCode=code,code);
        await action.click();await setup.waitForFunction(()=>!document.querySelector('#vydrop-quality-picker'));
        const status=setup.locator('#vifree-video-action').locator('.status');
        await status.waitFor({state:'visible'});
        const message=await status.innerText();
        assert.equal(await action.isEnabled(),true);
        assert.equal(await setup.getByRole('dialog').count(),0,'setup errors use one short status, without a fallback panel');
        if(language==='ar')assert.match(message,/[\u0600-\u06ff]/u);else assert.match(message,/Vydrop/u);
        assert.equal(await status.evaluate(e=>e.getBoundingClientRect().right<=innerWidth),true);
        assert.equal(await setup.evaluate(()=>forwarded.some(m=>m.type==='vifree-open-video')),false);
        if(code==='preparing')await setup.screenshot({path:path.join(output,`fresh-pc-${language}.png`)});
        count++;
      }
      // A later successful lookup works without reloading the page.
      await setup.evaluate(()=>{browser.runtime.sendMessage=async m=>{forwarded.push(m);return {ok:true,media:{title:'Ready after setup',videos:[{format:'mp4',heights:[720]}],audio:[]}};};});
      await action.click();await setup.getByRole('dialog').getByRole('radio',{name:'720p',exact:true}).waitFor();
      assert.equal(await setup.locator('#vifree-video-action').locator('.status').innerText(),'');
      await setup.keyboard.press('Escape');await setup.close();count++;
    }
    await page.evaluate(()=>{browser.runtime.sendMessage=message=>new Promise(resolve=>{window.releaseFormats=()=>resolve({ok:true,media:{title:'Late metadata',source:'example.com',videos:[{format:'mp4',heights:[720]}],audio:[]}});});});
    await overlayButton.click();await page.getByRole('dialog').waitFor();
    await page.keyboard.press('Escape');
    assert.equal(await overlayButton.textContent(),'Download by Vydrop');
    await page.evaluate(()=>releaseFormats());await page.waitForTimeout(100);
    assert.equal(await page.getByRole('dialog').count(),0,'late metadata cannot resurrect a closed chooser');count++;
    await page.close();
    const compact=await browser.newPage({viewport:{width:390,height:640},colorScheme:'dark',forcedColors:'active'});
    await compact.setContent('<button id="anchor">Choose audio</button>');await compact.addScriptTag({path:path.join(root,'browser/common/quality-picker.js')});
    await compact.evaluate(()=>{document.querySelector('#anchor').onclick=()=>VydropQualityPicker.open({anchor:document.querySelector('#anchor'),inspect:async()=>({title:'Long audio-only source title · Local test media without artwork',source:'example.com',videos:[],audio:['mp3','m4a','opus','wav','flac']}),send:async output=>{window.audioChoice=output;return {ok:true};}});});
    await compact.locator('#anchor').click();const audioDialog=compact.getByRole('dialog');await audioDialog.getByRole('radio',{name:'FLAC',exact:true}).click();
    assert.equal(await audioDialog.getByRole('button',{name:'Video',exact:true}).isDisabled(),true);
    assert.equal(await audioDialog.evaluate(e=>e.scrollWidth<=e.clientWidth && e.scrollHeight<=e.clientHeight+1),true);count++;
    await compact.screenshot({path:path.join(output,'host-audio-forced-colors-390.png')});
    await audioDialog.getByRole('button',{name:'Download',exact:true}).click();assert.deepEqual(await compact.evaluate(()=>audioChoice),{kind:'audio',format:'flac',height:0});await compact.close();
    console.log(`PASS: ${count} rendered quality menu checks: themes, Arabic, compact bounds, exact output delivery, Escape/focus restoration and silent in-video inspection failures.`);
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
