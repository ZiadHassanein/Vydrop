"""Render cursor-free, explicitly labelled SVG documentation illustrations."""
from pathlib import Path
from html import escape
import re

ROOT = Path(__file__).resolve().parent.parent
DEST = ROOT / 'docs/images/extension'
DEST.mkdir(parents=True, exist_ok=True)
MARK = re.findall(r'<path\b[^>]*/>', (ROOT/'assets/brand-mark.svg').read_text(encoding='utf-8'))
def text(x,y,value,size=22,fill='#272329',weight=400,anchor='start'):
    return f'<text x="{x}" y="{y}" font-size="{size}" fill="{fill}" font-weight="{weight}" text-anchor="{anchor}">{escape(value)}</text>'
def rect(x,y,w,h,fill='#FFFFFF',stroke='#DFD7DC',radius=10):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}" stroke="{stroke}"/>'
def badge(x,y,n):
    return f'<circle cx="{x}" cy="{y}" r="17" fill="#BA263A"/>'+text(x,y+6,str(n),19,'#FFFFFF',600,'middle')
def icon(x,y,size=46):
    return f'<svg x="{x}" y="{y}" width="{size}" height="{size}" viewBox="0 0 64 64">'+''.join(MARK)+'</svg>'
def save(path,title,description,body,width=1200,height=760):
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img" aria-labelledby="title desc"><title id="title">{escape(title)}</title><desc id="desc">{escape(description)}</desc><g font-family="Segoe UI,Arial,sans-serif">{body}</g></svg>\n'
    path.write_text(svg,encoding='utf-8')
def frame(title,url):
    return rect(0,0,1200,760,'#F8F7F8','#DFD7DC',0)+text(40,52,title,30,weight=600)+text(40,86,'Setup illustration · browser layout may vary',18,'#655963')+rect(40,118,1120,58,'#FFFFFF')+text(64,155,url,25)

for browser,url,name,edge in [('Google Chrome','chrome://extensions','03-chromium-setup.svg',False),('Microsoft Edge','edge://extensions','04-edge-setup.svg',True)]:
    body=frame(f'Install Vydrop in {browser}',url)
    if edge:
        body+=rect(40,198,240,280,'#F1EDF0')+text(62,239,'Extensions',24,weight=600)+text(62,289,'My extensions',21)+badge(70,371,1)+text(96,378,'Developer mode',18)+rect(214,357,46,26,'#BA263A','#BA263A',13)+'<circle cx="246" cy="370" r="9" fill="#FFFFFF"/>'
        left=306
    else:
        body+=text(60,225,'Extensions',25,weight=600)+badge(874,218,1)+text(899,225,'Developer mode',18)+rect(1090,204,46,26,'#BA263A','#BA263A',13)+'<circle cx="1122" cy="217" r="9" fill="#FFFFFF"/>'
        left=60
    body+=badge(left+17,280,2)+rect(left+46,254,205,53,'#FFFFFF','#8E7D87')+text(left+67,288,'Load unpacked',22,weight=600)
    body+=rect(left,337,1120-(left-40)-40,126,'#FFFFFF')+icon(left+18,360)+text(left+78,382,'Vydrop',25,weight=600)+text(left+78,418,'Enabled · Details · Reload',19,'#655963')
    body+=badge(64,526,3)+text(92,533,'Select this extracted folder:',23,weight=600)+rect(62,553,1076,57,'#F8E3E9')+text(84,590,'Documents / Vydrop / Browser / chromium',25)
    body+=badge(64,657,4)+text(92,664,'Details → Site access → On all sites; refresh your video page.',22)+text(62,715,'Keep the folder in place. Chrome and Edge use the same chromium companion.',20,'#655963')
    save(DEST/name,f'{browser} extension setup',f'Illustration: open {url}, enable Developer mode, choose Load unpacked, select Browser/chromium, allow the desired sites and refresh the video page.',body)

body=frame('Install Vydrop temporarily in Firefox','about:debugging#/runtime/this-firefox')
body+=rect(40,198,230,290,'#F1EDF0')+text(62,240,'Setup',22)+rect(54,269,202,55,'#F8E3E9')+badge(78,296,1)+text(104,303,'This Firefox',21,weight=600)
body+=text(304,237,'Temporary Extensions',27,weight=600)+badge(321,291,2)+rect(350,264,330,54,'#FFFFFF','#8E7D87')+text(370,299,'Load Temporary Add-on…',22,weight=600)
body+=rect(304,346,832,115,'#FFFFFF')+icon(324,369)+text(383,393,'Vydrop',25,weight=600)+text(383,429,'Temporary extension · Reload · Remove',20,'#655963')
body+=badge(64,526,3)+text(92,533,'Select this file from your extracted app folder:',23,weight=600)+rect(62,553,1076,57,'#F8E3E9')+text(84,590,'Documents / Vydrop / Browser / firefox / manifest.json',24)
body+=badge(64,657,4)+text(92,664,'Refresh the page, play the video, then use Download by Vydrop.',22)+text(62,715,'Temporary installation ends when Firefox restarts. Load it again next session.',21,'#A12232',600)
save(DEST/'05-firefox-setup.svg','Firefox temporary extension setup','Illustration of This Firefox, Load Temporary Add-on and Browser/firefox/manifest.json. This unsigned local companion must be loaded again after a Firefox restart.',body)

body=rect(0,0,1200,760,'#F8F7F8',radius=0)+text(40,52,'Send the video you are watching to Vydrop',30,weight=600)+text(40,86,'Playback illustration · the tab stays outside the video picture',18,'#655963')
body+=rect(40,130,1120,540,'#FFFFFF')+rect(84,252,1032,330,'#20242A','#20242A')+text(600,425,'Your playing video',34,'#F6F2F4',500,'middle')
body+=rect(814,186,302,52,'#F1EDF0','#8E7D87')+icon(825,194,34)+text(868,219,'Download by Vydrop',20,weight=600)+badge(782,212,1)
body+=text(84,627,'Click the tab, then review the detected link, format and save folder in the app.',22)+text(62,719,'No tab visible? Open the Vydrop toolbar popup. Click Download in the app to start saving.',21,'#655963')
save(DEST/'04-send-video.svg','Send a playing video to Vydrop','Playback diagram showing the V-only icon beside Download by Vydrop above the right video border, with review in the desktop app before download.',body)

print('Rendered Chrome, Edge, Firefox and playback guides.')
