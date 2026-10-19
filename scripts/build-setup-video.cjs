// Render a narrated tutorial without opening a desktop browser or sending OS input.
const fs = require('node:fs');
const path = require('node:path');
const {pathToFileURL} = require('node:url');
const {spawnSync} = require('node:child_process');
const root = path.resolve(__dirname, '..');
const work = path.resolve(process.argv[2] || path.join(root, 'build/setup-video'));
const data = JSON.parse(fs.readFileSync(path.join(__dirname, 'media/setup-video.json'), 'utf8'));
const media = path.join(root, 'docs/media');
const frames = path.join(work, 'frames');
for (const folder of [work, frames, media]) fs.mkdirSync(folder, {recursive:true});
const esc = value => String(value).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
function image(file) {
  const type = file.endsWith('.svg') ? 'image/svg+xml' : 'image/png';
  return `data:${type};base64,${fs.readFileSync(path.join(root,file)).toString('base64')}`;
}
const logo = image('assets/brand-logo-dark.svg');
const mark = image('assets/brand-mark.svg');
const capture = image('docs/images/extension/video/guided-setup.png');
function run(command,args,log) {
  const result = spawnSync(command,args,{encoding:'utf8',windowsHide:true,maxBuffer:8*1024*1024});
  if (log) fs.writeFileSync(path.join(work,log),`${result.stdout || ''}${result.stderr || ''}`);
  if (result.status !== 0) throw new Error(`${command} failed (${result.status}): ${(result.stderr || result.error || '').toString().slice(-1800)}`);
  return result.stdout;
}
function duration(file) { return Number(run('ffprobe',['-v','error','-show_entries','format=duration','-of','default=noprint_wrappers=1:nokey=1',file])); }
function browserFrame(name,address,body) {
  return `<div class="browser"><div class="window-top"><i></i><i></i><i></i><span>${esc(name)}</span></div><div class="address">${esc(address)}</div><div class="browser-body">${body}</div></div>`;
}
const button = text => `<div class="ui-button">${esc(text)}</div>`;
const toggle = '<span class="toggle"><b></b></span>';
const file = (name,detail,highlight=false) => `<div class="file ${highlight?'selected':''}"><span class="file-icon">${name.includes('.')?'≡':'▰'}</span><div><strong>${esc(name)}</strong>${detail?`<small>${esc(detail)}</small>`:''}</div>${highlight?'<span class="file-check">✓</span>':''}</div>`;
function panel(kind) {
  switch(kind) {
    case 'intro': return `<div class="intro-mark"><img src="${mark}" alt=""><div class="intro-steps"><div><b>01</b><span>Connect the app</span></div><div><b>02</b><span>Load the extension</span></div><div><b>03</b><span>Send a video</span></div></div><div class="browsers"><span>Chrome</span><span>Edge</span><span>Firefox</span></div></div>`;
    case 'folder': return `<div class="folder-window"><div class="window-top"><i></i><i></i><i></i><span>Extracted app folder · example</span></div><div class="address">Documents &nbsp;›&nbsp; Vydrop</div><div class="files">${file('Browser','Keep this folder with the app',true)}${file('Vydrop.exe','Open this Windows app',true)}${file('package.json','')}${file('README.txt','')}${file('THIRD-PARTY-NOTICES.txt','')}</div></div>`;
    case 'native': return `<div class="native-full"><img src="${capture}" alt="Native browser integration window"></div>`;
    case 'action': return `<div class="action-panel"><div class="capture-crop"><img src="${capture}" alt="Set up Chrome in Vydrop"><div class="capture-target" data-box="30,230,201,65"></div></div><div class="action-results"><div><b>✓</b> Connect this app</div><div><b>✓</b> Copy the correct path</div><div><b>✓</b> Open browser setup</div></div></div>`;
    case 'chrome': return browserFrame('Chrome · Extensions','chrome://extensions',`<div class="extensions-top"><h2>Extensions</h2><div class="dev focus">Developer mode ${toggle}</div></div><div class="focus button-wrap">${button('Load unpacked')}</div><div class="quiet-card"><img src="${mark}" alt=""><div><strong>Your extension appears here</strong><p>After selecting its extracted folder</p></div></div><div class="instruction-strip"><b>1</b> Developer mode &nbsp;&nbsp; <b>2</b> Load unpacked</div>`);
    case 'picker': return `<div class="folder-window"><div class="window-top"><i></i><i></i><i></i><span>Select the extension directory · example location</span></div><div class="address active-path">Documents&#92;Vydrop&#92;<strong>Browser&#92;chromium</strong></div><div class="files">${file('manifest.json','This file belongs inside the selected folder')}${file('connection.js','')}${file('background.js','')}${file('video-overlay.js','')}</div><div class="picker-footer"><span>Folder: <strong>chromium</strong></span><div class="focus button-wrap">${button('Select Folder')}</div></div></div>`;
    case 'enabled': return browserFrame('Chrome · Extensions','chrome://extensions',`<div class="extension-card"><img src="${mark}" alt=""><div class="extension-info"><h2>Vydrop <span>${esc(data.companionVersion)}</span></h2><p>Copy a video link and open it in Vydrop.</p><div class="extension-actions"><div class="focus button-wrap">${button('Details')}</div><span class="on-state">Enabled ${toggle}</span></div></div></div><div class="permission"><strong>Site access</strong><span>Allow the video websites you use</span></div><div class="instruction-strip">Refresh video pages after loading the extension.</div>`);
    case 'edge': return browserFrame('Microsoft Edge · Extensions','edge://extensions',`<div class="extensions-top"><h2>Extensions</h2><div class="dev">Developer mode ${toggle}</div></div><div class="focus button-wrap">${button('Load unpacked')}</div><div class="folder-callout"><small>Select this folder</small><code>Browser&#92;chromium</code></div><div class="instruction-strip">Use Set up Edge in Vydrop to copy the path.</div>`);
    case 'firefox': return browserFrame('Firefox · This Firefox','about:debugging#/runtime/this-firefox',`<div class="extensions-top"><h2>This Firefox</h2></div><div class="focus button-wrap">${button('Load Temporary Add-on…')}</div><div class="folder-callout"><small>Select this file</small><code>Browser&#92;firefox&#92;manifest.json</code></div><div class="temporary"><strong>Temporary installation</strong><span>Load it again after restarting Firefox.</span></div>`);
    case 'player': return browserFrame('Your video website','A public video page',`<div class="send-row"><span>Playing video</span><div class="vydrop-send"><img src="${mark}" alt="">Download by Vydrop</div></div><div class="video-picture"><div class="play-triangle"></div><span>Your selected video</span><div class="player-progress"><i></i></div></div><div class="instruction-strip">You can also use the Vydrop toolbar button.</div>`);
    case 'review': return `<div class="review-app"><div class="review-header"><img src="${logo}" alt="Vydrop"><span>New download</span></div><div class="ready"><b>✓</b><div><strong>Video checked</strong><span>Your selected public video</span></div></div><div class="review-options"><div><small>Save as</small><strong>Video · MP4 <span>⌄</span></strong></div><div><small>Quality</small><strong>Choose available quality <span>⌄</span></strong></div><div><small>Save to</small><strong>Downloads <span>Change</span></strong></div></div><div class="download-button">↓ &nbsp; Download</div></div>`;
    case 'finish': return `<div class="finish-panel"><img src="${logo}" alt="Vydrop"><div class="finish-list"><div><b>✓</b>App connected</div><div><b>✓</b>Extension loaded</div><div><b>✓</b>Video page refreshed</div></div><p>Keep the extracted app folder.</p><div class="guide-address">ziadhassanein.github.io/Vydrop</div></div>`;
    default: throw new Error(`Unknown scene kind: ${kind}`);
  }
}
const css = fs.readFileSync(path.join(__dirname, 'media/setup-video.css'), 'utf8');
async function main() {
  if (process.argv.includes('--frames-only') === false) for (const scene of data.scenes) {
    if (!fs.existsSync(path.join(work,'voice',`${scene.id}.wav`))) throw new Error('Narration is missing. Run narrate-setup-video.ps1 first.');
  }
  const playwright = require(process.env.VYDROP_PLAYWRIGHT || 'playwright');
  const sharp = require(process.env.VYDROP_SHARP || 'sharp');
  const browser = await playwright.chromium.launch({headless:true, ...(process.env.VYDROP_VIDEO_BROWSER ? {executablePath:process.env.VYDROP_VIDEO_BROWSER} : {}), args:['--disable-background-networking']});
  const context = await browser.newContext({viewport:{width:1920,height:1080},deviceScaleFactor:1});
  const page = await context.newPage();
  try {
    for (const [index,scene] of data.scenes.entries()) {
      const source = ['native','action'].includes(scene.kind) ? 'Native Vydrop interface capture' : ['intro','finish'].includes(scene.kind) ? 'Vydrop · Windows 10/11 x64' : 'Interface illustration · browser controls may vary';
      const html = `<!doctype html><html lang="en"><meta charset="utf-8"><title>${esc(scene.chapter)}</title><style>${css}</style><body><header><img src="${logo}" alt="Vydrop"><span>Browser setup · Vydrop ${esc(data.version)}</span></header><div class="main"><section class="copy"><div class="kicker">${String(index+1).padStart(2,'0')} / ${esc(scene.chapter)}</div><h1>${esc(scene.heading)}</h1><p>${esc(scene.body)}</p><div class="note">${esc(scene.note)}</div></section><section class="media">${panel(scene.kind)}<div class="source-label">${source}</div></section></div><footer><span>Connect → Load → Try a video</span><div class="rail">${data.scenes.map((_,i)=>`<span class="${i===index?'current':i<index?'done':''}"></span>`).join('')}</div><span>${String(index+1).padStart(2,'0')} / ${data.scenes.length}</span></footer></body></html>`;
      const filePath = path.join(frames,scene.id+'.html');
      fs.writeFileSync(filePath,html);
      await page.goto(pathToFileURL(filePath).href);
      await page.evaluate(async()=>{
        await document.fonts.ready;
        await Promise.all([...document.images].map(img=>img.decode()));
        for (const box of document.querySelectorAll('[data-box]')) {
          const img=box.parentElement.querySelector('img'); const scale=img.clientWidth/img.naturalWidth;
          const [x,y,w,h]=box.dataset.box.split(',').map(Number);
          Object.assign(box.style,{left:x*scale+'px',top:y*scale+'px',width:w*scale+'px',height:h*scale+'px'});
        }
      });
      const problems = await page.evaluate(()=>[...document.querySelectorAll('.copy,.media,.source-label,footer')].filter(e=>{const r=e.getBoundingClientRect();return r.right>1920||r.bottom>1040||r.left<0||r.top<0||e.scrollWidth>e.clientWidth+2;}).map(e=>e.className||e.tagName));
      if (problems.length) throw new Error(`${scene.id}: layout overflow: ${problems.join(', ')}`);
      await page.screenshot({path:path.join(frames,scene.id+'.png')});
      console.log(`Rendered ${scene.id}`);
    }
  } finally { await browser.close(); }
  await sharp(path.join(frames,'intro.png')).resize(1280,720).jpeg({quality:88,mozjpeg:true}).toFile(path.join(root,'docs/images/extension/video/poster.jpg'));
  if (process.argv.includes('--frames-only')) return;
  let elapsed=0;
  const chapters=[];
  const cues={en:[],ar:[]};
  for (const scene of data.scenes) {
    const wav=path.join(work,'voice',scene.id+'.wav');
    const spoken=duration(wav);
    const seconds=Math.ceil(Math.max(scene.minSeconds,spoken+1.8)*30)/30;
    const part=path.join(work,scene.id+'.mov');
    chapters.push({id:scene.id,title:scene.chapter,start:elapsed,duration:seconds,spokenSeconds:spoken});
    const marks=JSON.parse(fs.readFileSync(path.join(work,'voice',scene.id+'.timings.json'),'utf8'));
    if (marks.length!==scene.captions.length) throw new Error(`Missing caption timing marks: ${scene.id}`);
    for (const [index,caption] of scene.captions.entries()) {
      const start=elapsed+.7+marks[index].Seconds;
      const end=elapsed+.7+(marks[index+1]?.Seconds ?? spoken);
      for (const lang of ['en','ar']) cues[lang].push({start,end,text:caption[lang]});
    }
    const video=`fade=t=in:st=0:d=0.25,fade=t=out:st=${(seconds-.25).toFixed(3)}:d=0.25,format=yuv420p`;
    const audio='adelay=700:all=1,apad,afade=t=in:st=0:d=0.08';
    run('ffmpeg',['-hide_banner','-loglevel','warning','-y','-loop','1','-framerate','30','-i',path.join(frames,scene.id+'.png'),'-i',wav,'-vf',video,'-af',audio,'-t',String(seconds),'-r','30','-c:v','libx264','-preset','fast','-crf','21','-threads','4','-c:a','pcm_s16le','-ar','48000','-ac','1','-movflags','+faststart',part],scene.id+'-encode.log');
    elapsed+=seconds;
    console.log(`Encoded ${scene.id}: ${seconds.toFixed(1)}s`);
  }
  const stamp=seconds=>{const ms=Math.round(seconds*1000);return `${String(Math.floor(ms/3600000)).padStart(2,'0')}:${String(Math.floor(ms/60000)%60).padStart(2,'0')}:${String(Math.floor(ms/1000)%60).padStart(2,'0')}.${String(ms%1000).padStart(3,'0')}`;};
  for (const lang of ['en','ar']) fs.writeFileSync(path.join(media,`vydrop-extension-setup.${lang}.vtt`),'WEBVTT\n\n'+cues[lang].map((cue,i)=>`${i+1}\n${stamp(cue.start)} --> ${stamp(cue.end)}\n${cue.text}\n`).join('\n'));
  fs.writeFileSync(path.join(media,'vydrop-extension-setup.chapters.vtt'),'WEBVTT\n\n'+chapters.map((chapter,i)=>`${stamp(chapter.start)} --> ${stamp(chapter.start+chapter.duration)}\n${chapter.title}\n`).join('\n'));
  const list=path.join(work,'parts.txt');
  fs.writeFileSync(list,chapters.map(c=>`file '${path.join(work,c.id+'.mov').replaceAll('\\','/')}'`).join('\n'));
  const raw=path.join(work,'joined.mov');
  run('ffmpeg',['-hide_banner','-loglevel','warning','-y','-f','concat','-safe','0','-i',list,'-c','copy',raw],'concat.log');
  const output=path.join(media,'vydrop-extension-setup.mp4');
  run('ffmpeg',['-hide_banner','-loglevel','warning','-y','-i',raw,'-i',path.join(media,'vydrop-extension-setup.en.vtt'),'-i',path.join(media,'vydrop-extension-setup.ar.vtt'),'-map','0:v:0','-map','0:a:0','-map','1:0','-map','2:0','-c:v','copy','-af','loudnorm=I=-16:TP=-1.5:LRA=11','-c:a','aac','-b:a','128k','-ar','48000','-ac','2','-c:s','mov_text','-metadata','title='+data.title,'-metadata','artist=Ziad Hassanein','-metadata:s:s:0','language=eng','-metadata:s:s:0','handler_name=English','-metadata:s:s:1','language=ara','-metadata:s:s:1','handler_name=العربية','-disposition:s:0','0','-disposition:s:1','0','-movflags','+faststart',output],'final-encode.log');
  const receipt={title:data.title,appVersion:data.version,companionVersion:data.companionVersion,width:1920,height:1080,fps:30,narration:'English',captions:['en','ar'],durationSeconds:duration(output),bytes:fs.statSync(output).size,chapters,headlessOnly:true};
  fs.writeFileSync(path.join(work,'video-receipt.json'),JSON.stringify(receipt,null,2)+'\n');
  console.log(`Created ${output}: ${receipt.durationSeconds.toFixed(1)}s, ${(receipt.bytes/1024/1024).toFixed(1)} MiB.`);
}
main().catch(error=>{console.error(error);process.exitCode=1;});
