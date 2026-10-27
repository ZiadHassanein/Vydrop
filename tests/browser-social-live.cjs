// Isolated rendered fixture: no user profile, login, cookies, ads or media downloads.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { chromium } = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
const root = path.resolve(__dirname, '..');
const asset = 'D4E05AQGLOHPl6PbZoQ';
const media = `https://dms.licdn.com/playlist/vid/v2/${asset}/mp4-720p/video`;
const post = 'https://www.linkedin.com/embed/feed/update/urn:li:ugcPost:7519664336286650368/';
const poster = `https://media.licdn.com/dms/image/v2/${asset}/videocover-high/poster`;
const source = file => fs.readFileSync(path.join(root, 'browser/common', file), 'utf8');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const context = await browser.newContext({ viewport: { width: 1100, height: 850 } });
    const feed = `<html><style>video{width:620px;height:380px;background:#333} .vjs-poster{display:none}</style>
      <nav role="menu">Saved items</nav><div role="listitem" id="selected"><div><button id="post-menu" aria-expanded="false"><svg id="ellipses-horizontal-fill-small"></svg></button><span data-menu-portal-anchor></span></div>
      <div><video id="main"></video><div class="vjs-poster"><img src="${poster}"></div></div><button aria-expanded="false"><svg id="ellipses-horizontal-fill-small"></svg>Comment menu</button></div>
      <div role="listitem"><a href="https://www.linkedin.com/feed/update/urn:li:activity:99999999999999/">Neighbor</a></div>
      <script>let opened=0;document.querySelector('#post-menu').onclick=()=>{const button=document.querySelector('#post-menu');const open=button.getAttribute('aria-expanded')==='true';button.setAttribute('aria-expanded',String(!open));if(open){document.querySelector('#own-menu')?.remove();document.querySelector('[data-menu-portal-anchor]').innerHTML='';return;}opened++;setTimeout(()=>{document.querySelector('[data-menu-portal-anchor]').innerHTML='<span aria-owns="own-menu"></span>';const menu=document.createElement('div');menu.id='own-menu';menu.setAttribute('role','menu');menu.innerHTML='<a href="https://www.linkedin.com/preload/embed-modal/?targetUrn=urn%3Ali%3AugcPost%3A7519664336286650368&hasLinkedInVideo=true">Embed this post</a>';document.body.append(menu);},50);};</script></html>`;
    await context.route('**/*', route => {
      if (route.request().url().startsWith(post)) return route.fulfill({ contentType:'text/html',body:`<video data-sources='${JSON.stringify([{src:media,type:'video/mp4','data-bitrate':584362}])}'></video>` });
      if (route.request().url() === 'https://www.linkedin.com/feed/') return route.fulfill({contentType:'text/html',body:feed});
      return route.fulfill({ status:404,body:'' });
    });
    const temporary = new Map(), inspected=[], sent=[], opened=[]; let next=100, handler;
    const api = { runtime:{id:'fixture',onMessage:{addListener:fn=>{handler=fn;}},onInstalled:{addListener(){}}}, contextMenus:{onClicked:{addListener(){}}},tabs:{
      create:async options=>{opened.push(options);const page=await context.newPage();await page.goto(options.url);await page.addScriptTag({content:source('video-detection.js')});const id=next++;temporary.set(id,page);return{id};},
      sendMessage:async(id,message)=>({ ...await temporary.get(id).evaluate(asset=>ViFreeVideoDetection.inspectVideoPage('',false,null,asset),message.asset),pageUrl:temporary.get(id).url()}),
      get:async id=>({url:temporary.get(id).url()}),remove:async id=>{await temporary.get(id).close();temporary.delete(id);}
    }};
    const background=vm.createContext({ URL,setTimeout,ViFreeConnection:{api,name:'Vydrop',holdReload:()=>()=>{},inspectLink:async url=>{inspected.push(url);return{title:'Selected feed video',source:'linkedin.com',videos:[{format:'mp4',heights:[720]}],audio:['m4a']};},sendLink:async(url,details)=>{sent.push({url,details});return{code:'started'};}} });
    for(const file of ['video-detection.js','background.js'])vm.runInContext(source(file),background);
    await context.exposeFunction('sendFixtureMessage', message=>new Promise(resolve=>handler(message,{id:'fixture',frameId:0,url:'https://www.linkedin.com/feed/',tab:{id:7,url:'https://www.linkedin.com/feed/'}},resolve)));
    const page=await context.newPage();await page.goto('https://www.linkedin.com/feed/');
    await page.evaluate(()=>{
      Object.defineProperties(document.querySelector('video'),{readyState:{get:()=>4},paused:{get:()=>false},ended:{get:()=>false},currentSrc:{get:()=>'blob:https://www.linkedin.com/selected'}});
      window.browser={runtime:{getManifest:()=>({name:'Vydrop'}),sendMessage:value=>window.sendFixtureMessage(value)}};
    });
    for(const file of ['connection.js','video-detection.js','quality-picker.js','video-overlay.js'])await page.addScriptTag({content:source(file)});
    await page.locator('#vifree-video-action').getByRole('button').click();
    const picker=page.getByRole('dialog');await picker.getByRole('radio',{name:'720p',exact:true}).waitFor();
    assert.deepEqual(inspected,[media]);assert.equal(opened.length,1);assert.equal(opened[0].url,post);assert.equal(opened[0].active,false);assert.equal(temporary.size,0);
    assert.equal(await page.locator('#post-menu').getAttribute('aria-expanded'),'false');
    await picker.getByRole('button',{name:'Download',exact:true}).click();assert.equal(sent.length,1);assert.equal(sent[0].url,media);assert.equal(sent[0].details.sourcePage,post);assert.equal(sent[0].details.output.height,720);
    // A menu already opened by the user must remain untouched.
    await page.locator('#post-menu').click(); await page.locator('#own-menu').waitFor();
    const blocked=await page.evaluate(()=>ViFreeVideoDetection.inspectVideoPage('',true,document.querySelector('video')));
    assert.equal(blocked.url,'');assert.equal(await page.locator('#post-menu').getAttribute('aria-expanded'),'true');await page.locator('#post-menu').click();
    // Reject an embed for another domain or a playlist that cannot identify one post.
    await page.evaluate(()=>{document.querySelector('#post-menu').onclick=()=>{document.querySelector('#post-menu').setAttribute('aria-expanded','true');document.querySelector('[data-menu-portal-anchor]').innerHTML='<span aria-owns="wrong"></span>';const menu=document.createElement('div');menu.id='wrong';menu.setAttribute('role','menu');menu.innerHTML='<a href="https://www.linkedin.com.evil.example/preload/embed-modal/?targetUrn=urn%3Ali%3AugcPost%3A7519664336286650368&hasLinkedInVideo=true">Embed</a>';document.body.append(menu);};});
    const rejected=await page.evaluate(()=>ViFreeVideoDetection.inspectVideoPage('',true,document.querySelector('video')));assert.equal(rejected.url,'');assert.equal(sent.length,1);
    await context.close();
    console.log('PASS: LinkedIn feed click resolves its own public embed, validates the exact poster asset, closes its own temporary tab/menu, and delivers the selected quality; neighboring posts and foreign menus are not used.');
  } finally {await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
