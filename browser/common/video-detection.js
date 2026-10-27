globalThis.ViFreeVideoDetection = (() => {
  // Self-contained: Chrome/Firefox serialize this function into the active tab's isolated world.
  function inspectVideoPage(sourceHint = "", materializeLinks = false, selectedVideo = null, assetHint = "") {
    let warmedLink = false, linkedInSourcePage = "", advertisement = false;
    function platform(value) {
      const host = new URL(value).hostname.toLowerCase();
      if (/(^|\.)tiktok\.com$/u.test(host)) return "tiktok";
      if (/(^|\.)(facebook\.com|fb\.watch)$/u.test(host)) return "facebook";
      if (/(^|\.)(youtube\.com|youtu\.be|youtube-nocookie\.com)$/u.test(host)) return "youtube";
      if (/(^|\.)linkedin\.com$/u.test(host)) return "linkedin";
      if (/(^|\.)reddit\.com$/u.test(host) || host === "redd.it") return "reddit";
      if (/(^|\.)twitch\.tv$/u.test(host)) return "twitch";
      if (/(^|\.)dailymotion\.com$/u.test(host) || host === "dai.ly") return "dailymotion";
      return "";
    }
    const parent = node => node.parentElement || node.getRootNode?.()?.host;
    function publicUrl(value) {
      try {
        const url = new URL(value);
        return /^https?:$/u.test(url.protocol) && !url.username && !url.password && !url.port ? url.href : "";
      } catch { return ""; }
    }
    function permalink(value) {
      if (typeof value !== "string" || !value.trim()) return "";
      try {
        const url = new URL(value, location.href);
        if (!/^https?:$/u.test(url.protocol) || url.username || url.password || url.port || platform(url.href) !== platform(location.href)) return "";
        if (platform(location.href) === "youtube") {
          const id = url.hostname === "youtu.be" ? url.pathname.slice(1) : url.pathname.match(/^\/(?:shorts|embed)\/([\w-]+)/u)?.[1] ?? (url.pathname === "/watch" ? url.searchParams.get("v") : "");
          return /^[\w-]{11}$/u.test(id ?? "") ? `https://www.youtube.com/watch?v=${id}` : "";
        }
        if (!platform(location.href)) return "";
        if (platform(url.href) === "reddit") {
          const post = url.hostname === "redd.it" ? url.pathname.match(/^\/([a-z0-9]{1,12})\/?$/iu)?.[1] : url.pathname.match(/^\/(?:r\/[^/]+\/)?comments\/([a-z0-9]{1,12})(?:\/|$)/iu)?.[1];
          return post ? `https://www.reddit.com/comments/${post}/` : "";
        }
        if (platform(url.href) === "twitch") {
          const clip = url.hostname === "clips.twitch.tv" ? url.pathname === "/embed" ? url.searchParams.get("clip") : url.pathname.match(/^\/([\w-]+)\/?$/u)?.[1] : url.pathname.match(/^\/\w+\/clip\/([\w-]+)\/?$/u)?.[1];
          if (/^[\w-]+$/u.test(clip ?? "")) return `https://clips.twitch.tv/${clip}`;
          const video = url.pathname.match(/^\/videos\/(\d+)\/?$/u)?.[1];
          return video ? `https://www.twitch.tv/videos/${video}` : "";
        }
        if (platform(url.href) === "dailymotion") {
          const video = url.hostname === "dai.ly" ? url.pathname.match(/^\/([a-z0-9]+)\/?$/iu)?.[1] : url.pathname.match(/^\/(?:embed\/)?video\/([a-z0-9]+)(?:_[^/]*)?\/?$/iu)?.[1] ?? url.searchParams.get("video");
          return /^[a-z0-9]+$/iu.test(video ?? "") ? `https://www.dailymotion.com/video/${video}` : "";
        }
        if (platform(url.href) === "linkedin") {
          const path = decodeURIComponent(url.pathname);
          const activity = path.match(/^\/(?:embed\/)?feed\/update\/urn:li:(activity|ugcPost|share):(\d{8,25})\/?$/u);
          if (activity) return `https://www.linkedin.com/${url.pathname.startsWith("/embed/") ? "embed/" : ""}feed/update/urn:li:${activity[1]}:${activity[2]}/`;
          const highlighted = url.searchParams.getAll("highlightedUpdateUrn");
          const sharedActivity = highlighted.length === 1 && highlighted[0].match(/^urn:li:activity:(\d{8,25})$/u);
          if ((/^\/feed\/update\/urn:li:groupPost:\d+-\d+\/?$/u.test(path) || /^\/groups\/\d+\/?$/u.test(path)) && sharedActivity)
            return `https://www.linkedin.com/feed/update/urn:li:activity:${sharedActivity[1]}/`;
          return /^\/posts\/[^\s/?#]+-\d{8,25}-\w{4}\/?$/u.test(path) ? "https://www.linkedin.com" + url.pathname.replace(/\/$/u, "") : "";
        }
        if (platform(url.href) === "tiktok") {
          const match = url.pathname.match(/^\/@([\w.-]+)\/video\/(\d+)\/?$/u);
          if (match) return `https://www.tiktok.com/@${match[1]}/video/${match[2]}`;
          if (/^(vm|vt)\.tiktok\.com$/u.test(url.hostname) && /^\/[\w-]+\/?$/u.test(url.pathname)) return url.origin + url.pathname;
          return "";
        }
        const reel = url.pathname.match(/^\/reel\/(\d+)\/?$/u);
        if (reel) return `https://www.facebook.com/reel/${reel[1]}/`;
        const video = url.pathname.match(/^\/[^/]+\/videos\/(?:[^/]+\/)?(\d+)\/?$/u);
        const id = video?.[1] ?? (/^\/(watch\/?|video\.php)$/u.test(url.pathname) ? url.searchParams.get("v") : "");
        if (/^\d+$/u.test(id ?? "")) return `https://www.facebook.com/watch/?v=${id}`;
        if (url.hostname === "fb.watch" && /^\/[\w-]+\/?$/u.test(url.pathname)) return url.origin + url.pathname;
        const post = url.pathname.match(/^\/(?:[^/]+\/posts|groups\/[^/]+\/(?:posts|permalink))\/(?:pfbid[A-Za-z0-9]+|\d+)\/?$/u);
        if (post) return "https://www.facebook.com" + url.pathname;
        if (/^\/(story|permalink)\.php$/u.test(url.pathname) && /^(pfbid[A-Za-z0-9]+|\d+)$/u.test(url.searchParams.get("story_fbid") ?? "")) {
          const owner = url.searchParams.get("id");
          return "https://www.facebook.com" + url.pathname + "?story_fbid=" + url.searchParams.get("story_fbid") + (/^\d+$/u.test(owner ?? "") ? "&id=" + owner : "");
        }
        const share = url.pathname.match(/^\/share\/(r|v)\/([\w-]+)\/?$/u);
        return share ? `https://www.facebook.com/share/${share[1]}/${share[2]}/` : "";
      } catch { return ""; }
    }
    function visible(video) {
      if (video.closest('.ad-container,.dm-sdk-ima-slot')) return null;
      let box = video.getBoundingClientRect(), style = getComputedStyle(video);
      if (style.display === "none" || style.visibility === "hidden") return null;
      for (let ancestor = parent(video), depth = 0; ancestor && ancestor !== document.body && depth < 40; ancestor = parent(ancestor), depth++) {
        const paint = getComputedStyle(ancestor);
        if (paint.display === "none" || paint.visibility === "hidden" || Number(paint.opacity) === 0) return null;
      }
      // TikTok can paint the active video through a visible sibling canvas while hiding the video element.
      if (Number(style.opacity) === 0) {
        const canvas = platform(location.href) === "tiktok" && Array.from(video.parentElement?.querySelectorAll("canvas") ?? []).slice(0, 5).find(item => {
          const paint = getComputedStyle(item), rect = item.getBoundingClientRect();
          return paint.display !== "none" && paint.visibility !== "hidden" && Number(paint.opacity) > 0 && rect.width >= 80 && rect.height >= 60;
        });
        if (!canvas) return null;
        box = canvas.getBoundingClientRect();
      }
      const width = Math.max(0, Math.min(box.right, innerWidth) - Math.max(box.left, 0));
      const height = Math.max(0, Math.min(box.bottom, innerHeight) - Math.max(box.top, 0));
      const pip = document.pictureInPictureElement === video;
      if (!pip && (box.width < 80 || box.height < 60 || width * height < 2400)) return null;
      const playing = !video.paused && !video.ended && video.readyState >= 2;
      return { video, score: (pip ? 20000 : 0) + (playing ? 10000 : 0) + Math.min(width * height, 500000) / 1000 + width * height / Math.max(1, box.width * box.height) * 50 };
    }
    function playerMedia(video) {
      if (video?.mediaKeys) return "";
      const id = video?.closest('.jwplayer')?.id;
      if (!/^[\w-]{1,80}$/u.test(id ?? "")) return "";
      const urls = new Set(); let total = 0;
      const unquote = value => value.replace(/\\(x[0-9a-f]{2}|u[0-9a-f]{4}|[\s\S])/giu, (_, escape) =>
        escape[0] === 'x' || escape[0] === 'u' ? String.fromCharCode(parseInt(escape.slice(1), 16)) : ({ n: '\n', r: '\r', t: '\t' })[escape] ?? escape);
      for (const script of Array.from(document.querySelectorAll('script')).slice(0, 32)) {
        if (script.getAttribute('src')) continue;
        let config = script.textContent ?? '';
        if (config.length > 65536 || (total += config.length) > 262144) continue;
        // Decode the bounded string/token table used by this public player.
        // This parses data only; no page JavaScript is evaluated or executed.
        const packed = config.match(/\}\(\s*'((?:\\.|[^'\\])*)'\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*'((?:\\.|[^'\\])*)'\.split\('\|'\)/su);
        if (packed) {
          const radix = Number(packed[2]), count = Number(packed[3]), words = unquote(packed[4]).split('|');
          if (radix < 2 || radix > 36 || count > 512 || words.length !== count) continue;
          const alphabet = '0123456789abcdefghijklmnopqrstuvwxyz'.slice(0, radix);
          config = unquote(packed[1]).replace(/\b[0-9a-z]+\b/giu, token =>
            [...token.toLowerCase()].every(char => alphabet.includes(char)) ? words[parseInt(token, radix)] || token : token);
        }
        for (const setup of config.matchAll(/jwplayer\s*\(\s*(["'])([\w-]{1,80})\1\s*\)\s*\.\s*setup\s*\(\s*\{\s*(?:["']sources["']|sources)\s*:\s*\[([^\]]{1,8192})\]/gu)) {
          if (setup[2] !== id) continue;
          for (const source of setup[3].matchAll(/(?:["']file["']|\bfile)\s*:\s*(?:"((?:\\.|[^"\\])*)"|'((?:\\.|[^'\\])*)')/gu)) {
            const raw = unquote(source[1] ?? source[2]);
            const url = !/[\u0000-\u001f\u007f]/u.test(raw) && publicUrl(raw);
            if (url && /\.(?:m3u8|mp4|webm)$/iu.test(new URL(url).pathname)) urls.add(url);
          }
        }
      }
      return urls.size === 1 ? [...urls][0] : '';
    }
    function redditMedia(video, post) {
      if (platform(location.href) !== "reddit" || video.mediaKeys) return null;
      for (let node = parent(video), depth = 0; node && depth < 12; node = parent(node), depth++) {
        if (node.getAttribute("comment-id") !== null || node.getAttribute("post-promoted") !== null) return null;
        if (!node.matches("shreddit-player")) continue;
        const id = node.getAttribute("post-id")?.match(/^t3_([a-z0-9]{1,12})$/iu)?.[1];
        if (!id || post !== `https://www.reddit.com/comments/${id}/`) return null;
        const value = node.getAttribute("src") ?? "";
        if (value.length > 8192 || /[\u0000-\u0020\u007f]/u.test(value)) return null;
        try {
          const url = new URL(value);
          const asset = url.pathname.match(/^\/([a-z0-9]{5,32})\/HLSPlaylist\.m3u8$/u)?.[1];
          if (url.protocol === "https:" && url.hostname === "v.redd.it" && !url.username && !url.password && !url.port && asset)
            return { url: url.href, reason: "source", sourcePage: post, mediaId: asset };
        } catch { /* Only the selected main player's public master manifest qualifies. */ }
        return null;
      }
      return null;
    }
    async function linkedInMenu(video, asset) {
      const card = video.closest('[role="listitem"],.feed-shared-update-v2,article');
      if (!card || !asset || video.mediaKeys) return null;
      const buttons = Array.from(card.querySelectorAll('button[aria-expanded]')).filter(button =>
        button.querySelectorAll('svg[id="ellipses-horizontal-fill-small"]').length === 1 && Boolean(button.compareDocumentPosition(video) & 4));
      if (buttons.length !== 1) return null;
      const button = buttons[0];
      // Do not dismiss or replace a menu the user already opened.
      if (button.getAttribute('aria-expanded') !== 'false' || document.querySelectorAll('[data-menu-portal-anchor] [aria-owns]').length) return null;
      let ownedMenu = "", expanded = false;
      try {
        button.click();
        const deadline = Date.now() + 4000;
        // React can commit aria-expanded and the menu portal after click() returns.
        // Wait for this exact button, and stop if its opened menu is dismissed.
        while (Date.now() < deadline && card.contains(button)) {
          if (button.getAttribute('aria-expanded') === 'true') {
            expanded = true;
            const portal = button.parentElement?.querySelector('[data-menu-portal-anchor] [aria-owns]');
            const id = portal?.getAttribute('aria-owns');
            if (id) {
              if (ownedMenu && ownedMenu !== id) break;
              ownedMenu = id;
              const root = document.getElementById(id);
              const menus = root?.getAttribute('role') === 'menu' ? [root] : Array.from(root?.querySelectorAll('[role="menu"]') ?? []);
              if (menus.length === 1) {
                const targets = new Set();
                for (const link of Array.from(menus[0].querySelectorAll('a[href]')).slice(0, 30)) {
                  try {
                    const url = new URL(link.href);
                    if (url.protocol !== 'https:' || url.hostname !== 'www.linkedin.com' || url.username || url.password || url.port ||
                        url.pathname !== '/preload/embed-modal/' || url.searchParams.getAll('targetUrn').length !== 1 || url.searchParams.get('hasLinkedInVideo') !== 'true') continue;
                    const urn = url.searchParams.get('targetUrn');
                    if (/^urn:li:(?:activity|ugcPost|share):\d{8,25}$/u.test(urn)) targets.add(urn);
                  } catch { /* Ignore unrelated menu destinations. */ }
                }
                if (targets.size === 1) {
                  const url = `https://www.linkedin.com/embed/feed/update/${[...targets][0]}/`;
                  return { url, reason: "player", sourcePage: url, linkedinAsset: asset };
                }
              }
            }
          } else if (expanded) break;
          await new Promise(resolve => setTimeout(resolve, 100));
        }
      } finally {
        const current = () => button.parentElement?.querySelector('[data-menu-portal-anchor] [aria-owns]')?.getAttribute('aria-owns');
        if (card.contains(button) && button.getAttribute('aria-expanded') === 'true' && (!ownedMenu || current() === ownedMenu)) {
          button.click();
          // Closing is asynchronous too. Do not return with our menu over the video,
          // and never toggle again or dismiss a replacement menu owned by the user.
          const deadline = Date.now() + 1000;
          while (Date.now() < deadline && card.contains(button) && button.getAttribute('aria-expanded') === 'true' &&
            (!ownedMenu || current() === ownedMenu)) await new Promise(resolve => setTimeout(resolve, 25));
        }
      }
      return null;
    }
    function linkedInAsset(video) {
      const posters = [video?.poster ?? ""];
      for (const image of Array.from(video?.parentElement?.querySelectorAll("img") ?? []).slice(0, 10))
        if (image.closest(".vjs-poster")) posters.push(image.src);
      const assets = new Set();
      for (const poster of posters) {
        try {
          const url = new URL(poster);
          const asset = url.pathname.match(/^\/dms\/image\/(?:v2\/)?([A-Za-z0-9_-]+)\/videocover/u)?.[1] ??
            url.pathname.match(/^\/playlist\/vid\/v2\/([A-Za-z0-9_-]{8,100})\/thumbnail-[A-Za-z0-9_-]{1,80}\//u)?.[1];
          if (url.protocol === "https:" && !url.username && !url.password && !url.port && url.hostname.endsWith(".licdn.com") && asset) assets.add(asset);
        } catch { /* An absent poster cannot identify another player's media. */ }
      }
      return assets.size === 1 ? [...assets][0] : "";
    }
    function linkedInMedia(video, assetHint = "") {
      if (platform(location.href) !== "linkedin" || video?.mediaKeys) return "";
      function source(value, asset = "") {
        try {
          const url = new URL(value);
          const match = url.pathname.match(/^\/playlist\/vid\/v2\/([A-Za-z0-9_-]+)\/mp4-/u);
          return url.protocol === "https:" && !url.username && !url.password && !url.port &&
            url.hostname.endsWith(".licdn.com") && match && (!asset || match[1] === asset) ? url.href : "";
        } catch { return ""; }
      }
      const direct = source(video?.currentSrc || video?.src);
      if (direct) return direct;
      const asset = assetHint || linkedInAsset(video);
      if (!asset) return "";
      // LinkedIn's blob player keeps progressive MP4s in DOM JSON. Match the
      // selected poster's exact asset ID; never borrow another post's stream.
      const choices = new Map();
      // Public embeds expose bounded JSON on the actual video element.
      // Match the requested feed poster's asset before accepting any source.
      for (const node of (video ? [video] : Array.from(document.querySelectorAll('video[data-sources]')).slice(0, 20))) {
        if (node.mediaKeys) continue;
        const raw = node.getAttribute('data-sources') ?? '';
        if (!raw || raw.length > 65536) continue;
        let streams;
        try { streams = JSON.parse(raw); } catch { continue; }
        if (!Array.isArray(streams)) continue;
        for (const stream of streams.slice(0, 20)) {
          if (stream.type !== 'video/mp4') continue;
          const url = source(stream.src, asset);
          if (url) choices.set(url, { url, area: 0, bitrate: Number(stream['data-bitrate']) || 0 });
        }
      }
      let total = 0, visited = 0;
      for (const code of Array.from(document.querySelectorAll("code")).slice(0, 80)) {
        const text = code.textContent ?? "";
        if (text.length > 2 * 1024 * 1024 || (total += text.length) > 6 * 1024 * 1024) break;
        if (!text.includes(asset) || !text.includes("progressiveStreams")) continue;
        let data;
        try { data = JSON.parse(text); } catch { continue; }
        const pending = [data];
        while (pending.length && visited++ < 40000) {
          const node = pending.pop();
          if (!node || typeof node !== "object") continue;
          if (node.$type === "com.linkedin.videocontent.VideoPlayMetadata" && node.hasDrm !== true && node.isEncrypted !== true) {
            for (const stream of Array.isArray(node.progressiveStreams) ? node.progressiveStreams.slice(0, 20) : []) {
              if (stream.mediaType !== "video/mp4") continue;
              for (const location of Array.isArray(stream.streamingLocations) ? stream.streamingLocations.slice(0, 5) : []) {
                const url = source(location.url, asset);
                if (url) choices.set(url, { url, area: Number(stream.width) * Number(stream.height) || 0, bitrate: Number(stream.bitRate) || 0 });
              }
            }
          }
          for (const child of Object.values(node)) if (child && typeof child === "object") pending.push(child);
        }
      }
      return [...choices.values()].sort((a, b) => b.area - a.area || b.bitrate - a.bitrate)[0]?.url ?? "";
    }
    function localLink(video, visibleVideos) {
      const wrapped = permalink(video.closest("a[href]")?.href ?? "");
      if (wrapped) return wrapped;
      let scope = video, mediaId = "";
      for (let level = 0; scope && scope !== document.body && level < 40; level++, scope = parent(scope)) {
        // Never borrow a permalink from a different visible feed item or a sidebar.
        if (visibleVideos.some(other => other !== video && scope.contains(other))) break;
        if (platform(location.href) === "reddit") {
          if (scope.getAttribute("post-promoted") !== null) { advertisement = true; return ""; }
          const post = scope.getAttribute("post-id")?.match(/^t3_([a-z0-9]{1,12})$/iu)?.[1];
          if (post) return `https://www.reddit.com/comments/${post}/`;
        }
        if (platform(location.href) === "linkedin") {
          const activity = [scope.getAttribute("data-urn"), scope.getAttribute("data-id")]
            .map(value => value?.match(/^urn:li:activity:(\d{8,25})$/u)).find(Boolean);
          if (activity) return `https://www.linkedin.com/feed/update/urn:li:activity:${activity[1]}/`;
        }
        const id = scope.getAttribute("data-video-id") ?? scope.getAttribute("data-item-id") ?? (platform(location.href) === "tiktok" ? scope.id?.match(/^xgwrapper-\d+-(\d{15,25})$/u)?.[1] : "");
        if (/^\d{8,25}$/u.test(id ?? "")) mediaId = id;
        if (mediaId) {
          if (platform(location.href) === "facebook") return `https://www.facebook.com/watch/?v=${mediaId}`;
          const author = platform(location.href) === "tiktok" && Array.from(scope.querySelectorAll('a[href]')).map(link => { try { return platform(link.href) === "tiktok" ? new URL(link.href).pathname.match(/^\/@([\w.-]+)\/?$/u)?.[1] : null; } catch { return null; } }).find(Boolean);
          if (author) return `https://www.tiktok.com/@${author}/video/${mediaId}`;
        }
        const anchors = Array.from(scope.querySelectorAll("a[href]")).slice(0, 200);
        const links = new Set(anchors.map(link => permalink(link.href)).filter(Boolean));
        if (links.size === 1) {
          const value = [...links][0];
          if (platform(location.href) === "linkedin") {
            const activity = new URL(value).pathname.match(/^\/feed\/update\/(urn:li:activity:\d+)\/$/u)?.[1];
            for (const anchor of anchors) {
              try {
                const group = new URL(anchor.href, location.href).pathname.match(/^\/groups\/(\d+)\/?$/u)?.[1];
                if (group && activity && permalink(anchor.href) === value)
                  linkedInSourcePage = `https://www.linkedin.com/groups/${group}/?q=highlightedFeedForGroups&highlightedUpdateUrn=${encodeURIComponent(activity)}`;
              } catch { /* Unrelated malformed anchors cannot stop the selected post lookup. */ }
            }
          }
          return value;
        }
        if (links.size > 1) return "";
        // Facebook lazily assigns the public timestamp URL on pointer entry. Never click it.
        if (materializeLinks && !warmedLink && platform(location.href) === "facebook") {
          const timestamps = anchors.filter(link => {
            try {
              const url = new URL(link.href, location.href);
              return url.origin === new URL(location.href).origin && url.pathname === "/" &&
                (url.searchParams.get("__tn__") ?? "").split(",").includes("O");
            } catch { return false; }
          });
          if (timestamps.length === 1) {
            warmedLink = true;
            timestamps[0].dispatchEvent(new MouseEvent("mouseover", { bubbles: true }));
            timestamps[0].dispatchEvent(new MouseEvent("mouseenter"));
          }
        }
        if (scope.matches('article,[role="article"],[role="dialog"],[data-e2e="feed-item"],[data-e2e="recommend-list-item-container"],[data-pagelet^="FeedUnit"],ytd-rich-item-renderer,ytd-video-renderer,ytd-reel-video-renderer,yt-lockup-view-model,shreddit-post') ||
          platform(location.href) === "linkedin" && scope.matches('[role="listitem"],.feed-shared-update-v2,[data-urn^="urn:li:activity:"],[data-id^="urn:li:activity:"],[data-urn^="urn:li:ugcPost:"],[data-urn^="urn:li:groupPost:"],[data-urn^="urn:li:share:"]') ||
          platform(location.href) === "facebook" && scope.querySelectorAll('h2,h3,h4,[role="heading"]').length) break;
      }
      return "";
    }
    if (!publicUrl(location.href)) return { url: "", reason: "unsupported" };
    if (selectedVideo?.closest('.ad-container,.dm-sdk-ima-slot')) return { url: "", reason: "advertisement" };
    // The episode wrapper stays usable even when its selected mirror has no
    // video element. Only its named main iframe can supply this episode.
    const wrapper = new URL(location.href);
    if (wrapper.protocol === "https:" && wrapper.hostname === "w.anaplayer.online" &&
        !wrapper.username && !wrapper.password && !wrapper.port && /^\/albaplayer\/[a-z0-9-]{1,160}\/$/u.test(wrapper.pathname)) {
      const frame = Array.from(document.querySelectorAll("iframe")).find(node => node.id === "iframe");
      let player;
      try { player = new URL(frame?.src); } catch { /* A missing main iframe cannot identify an episode. */ }
      const known = player?.protocol === "https:" && !player.username && !player.password && !player.port &&
        (/^(?:cdnplus\.sbs|mp4plus\.space|anafast\.space|vidoba\.space|vidspeed\.space)$/u.test(player.hostname) && /^\/embed-[a-z0-9]{12}\.html$/u.test(player.pathname) ||
          player.hostname === "ok.ru" && /^\/videoembed\/\d{1,25}$/u.test(player.pathname));
      if (known && (!selectedVideo || selectedVideo === frame)) {
        let mirrorPage = "";
        for (const link of Array.from(document.querySelectorAll("a[href]")).slice(0, 100)) {
          if (link.textContent.trim() !== "AnaFast" || !link.closest(".aplr-menu")) continue;
          try {
            const mirror = new URL(link.href, wrapper.href);
            if (mirror.origin === wrapper.origin && mirror.pathname === wrapper.pathname && !mirror.username && !mirror.password &&
                !mirror.hash && [...mirror.searchParams.keys()].length === 1 && mirror.searchParams.getAll("serv").length === 1 &&
                /^\d{1,2}$/u.test(mirror.searchParams.get("serv"))) mirrorPage = mirror.href;
          } catch { /* Unrelated navigation is not an episode mirror. */ }
        }
        return { url: player.origin + player.pathname, reason: "mirror", sourcePage: wrapper.href, mirrorPage };
      }
    }

    // The click belongs to Dailymotion's player frame, whose video node can be
    // replaced during ads/navigation. Let the background use the browser's
    // current containing watch URL instead of depending on that transient node.
    if (platform(location.href) === "dailymotion" && location.hostname === "geo.dailymotion.com" && /^\/player\/[\w-]+\.html$/u.test(new URL(location.href).pathname) && !permalink(location.href))
      return { url: publicUrl(location.href), reason: "frame" };
    if (platform(location.href) === "linkedin" && typeof assetHint === "string" && /^[A-Za-z0-9_-]{8,100}$/u.test(assetHint)) {
      const url = linkedInMedia(null, assetHint);
      return { url, reason: url ? "source" : "missing-link" };
    }
    const roots = [document, ...Array.from(document.querySelectorAll("shreddit-player")).filter(player => player.getAttribute("comment-id") === null).slice(0, 100).map(player => player.shadowRoot).filter(Boolean)];
    const allVideos = roots.flatMap(root => Array.from(root.querySelectorAll("video")).slice(0, 100)).slice(0, 100);
    const videos = allVideos.map(visible).filter(Boolean).sort((a, b) => b.score - a.score);
    const hinted = selectedVideo ? videos.find(item => item.video === selectedVideo) : sourceHint && videos.find(item => item.video.currentSrc === sourceHint || item.video.src === sourceHint);
    if ((selectedVideo || sourceHint) && !hinted) return { url: "", reason: "no-video" };
    const ranked = hinted ? [hinted] : videos;
    if (ranked.length > 1 && Math.abs(ranked[0].score - ranked[1].score) < 30) return { url: "", reason: "ambiguous" };
    if (ranked.length) {
      const media = linkedInMedia(ranked[0].video);
      if (media) return { url: media, reason: "source", sourcePage: localLink(ranked[0].video, videos.map(item => item.video)) || permalink(location.href) };
      const local = localLink(ranked[0].video, videos.map(item => item.video));
      if (advertisement) return { url: "", reason: "advertisement" };
      const reddit = local && redditMedia(ranked[0].video, local);
      if (reddit) return reddit;
      if (local) {
        const asset = platform(location.href) === "linkedin" && !ranked[0].video.mediaKeys && linkedInAsset(ranked[0].video);
        const identified = asset ? { url: local, reason: "player", linkedinAsset: asset, sourcePage: linkedInSourcePage || local } : { url: local, reason: "player" };
        return asset && materializeLinks ? linkedInMenu(ranked[0].video, asset).then(found => found ?? identified) : identified;
      }
      if (materializeLinks && platform(location.href) === "linkedin" && !ranked[0].video.mediaKeys) {
        const asset = linkedInAsset(ranked[0].video);
        if (asset) return linkedInMenu(ranked[0].video, asset).then(found => found ?? { url: "", reason: "missing-link" });
      }
      if (!platform(location.href)) {
        const video = ranked[0].video;
        const configured = playerMedia(video);
        if (configured) return { url: configured, reason: "source", sourcePage: publicUrl(location.href),
          mediaId: new URL(location.href).pathname.match(/^\/embed-([\w-]{1,80})\.html$/u)?.[1] };
        const source = publicUrl(video.currentSrc || video.src);
        if (source) return { url: source, reason: "source" };
        const wrapped = publicUrl(video.closest("a[href]")?.href);
        if (wrapped && wrapped !== publicUrl(location.href) && new URL(wrapped).pathname !== "/") return { url: wrapped, reason: "player" };
        if (allVideos.length === 1) return { url: publicUrl(location.href), reason: "page" };
      }
    }
    const current = permalink(location.href);
    if (current) return { url: current, reason: "page" };
    if (warmedLink) return new Promise(resolve => {
      let attempts = 0;
      const recheck = () => {
        const found = inspectVideoPage(sourceHint, false, selectedVideo);
        if (found.url || found.reason !== "missing-link" || ++attempts >= 8) resolve(found);
        else setTimeout(recheck, 125);
      };
      setTimeout(recheck, 125);
    });
    return { url: "", reason: ranked.length ? "missing-link" : "no-video" };
  }

  function socialPlatform(value) {
    try {
      const host = new URL(value).hostname.toLowerCase();
      if (/(^|\.)tiktok\.com$/u.test(host)) return "tiktok";
      if (/(^|\.)(facebook\.com|fb\.watch)$/u.test(host)) return "facebook";
      if (/(^|\.)linkedin\.com$/u.test(host)) return "linkedin";
      if (/(^|\.)reddit\.com$/u.test(host) || host === "redd.it") return "reddit";
      if (/(^|\.)twitch\.tv$/u.test(host)) return "twitch";
      if (/(^|\.)dailymotion\.com$/u.test(host) || host === "dai.ly") return "dailymotion";
      return "";
    }
    catch { return ""; }
  }
  async function resolveEpisodeMirror(found) {
    let frame, mirror;
    try { frame = new URL(found.frameUrl); mirror = new URL(found.mirrorPage); } catch { return found.url; }
    if (frame.protocol !== "https:" || frame.hostname !== "w.anaplayer.online" || frame.username || frame.password || frame.port ||
        !/^\/albaplayer\/[a-z0-9-]{1,160}\/$/u.test(frame.pathname) || mirror.origin !== frame.origin || mirror.pathname !== frame.pathname ||
        mirror.username || mirror.password || mirror.hash || [...mirror.searchParams.keys()].length !== 1 ||
        mirror.searchParams.getAll("serv").length !== 1 || !/^\d{1,2}$/u.test(mirror.searchParams.get("serv"))) return found.url;
    try {
      const selected = new URL(found.url);
      if (selected.protocol === "https:" && selected.hostname === "anafast.space" && !selected.username && !selected.password && !selected.port &&
          /^\/embed-[a-z0-9]{12}\.html$/u.test(selected.pathname)) return selected.origin + selected.pathname;
    } catch { /* Resolve the same-episode alternative below. */ }
    const api = globalThis.ViFreeConnection?.api;
    if (!api?.tabs?.create || !api.tabs.sendMessage || !api.tabs.remove) return found.url;
    let tab;
    try {
      // A normal inactive page uses the existing content-script permissions.
      // No browser cookies, host permission expansion or page scripts are read.
      tab = await api.tabs.create({ url: mirror.href, active: false });
      const deadline = Date.now() + 12000;
      while (Date.now() < deadline) {
        try {
          const result = await api.tabs.sendMessage(tab.id, { type: "vydrop-player-source" }, { frameId: 0 });
          const source = new URL(result?.url);
          if (result.pageUrl === mirror.href && source.protocol === "https:" && source.hostname === "anafast.space" &&
              !source.username && !source.password && !source.port && /^\/embed-[a-z0-9]{12}\.html$/u.test(source.pathname))
            return source.origin + source.pathname;
        } catch { /* Wait for this wrapper's main iframe, never for an advertisement. */ }
        await new Promise(resolve => setTimeout(resolve, 250));
      }
    } catch { /* Keep the selected main-player URL if the same-episode mirror is unavailable. */ }
    finally {
      if (Number.isInteger(tab?.id)) {
        let current;
        try {
          current = (await api.tabs.get?.(tab.id))?.url;
          if (!current) current = (await api.tabs.sendMessage(tab.id, { type: "vydrop-player-source" }, { frameId: 0 }))?.pageUrl;
        } catch { /* Do not close a page whose current location cannot be confirmed. */ }
        if (current === mirror.href) await api.tabs.remove(tab.id).catch(() => {});
      }
    }
    return found.url;
  }

  async function resolveSource(found) {
    if (found?.mirrorPage) return resolveEpisodeMirror(found);
    // Dailymotion configures its cross-origin player through messages; its
    // frame URL may contain only a player ID. The browser supplies the actual
    // containing tab URL, which must be a matching public Dailymotion video.
    try {
      const frame = new URL(found.frameUrl);
      // AnaFast's CDN links are temporary and can reject the browser-issued
      // signature outside that playback session. Re-extract this exact public
      // player so yt-dlp obtains fresh main-video URLs and request context.
      if (frame.protocol === "https:" && frame.hostname === "anafast.space" &&
        !frame.username && !frame.password && !frame.port && /^\/embed-[a-z0-9]{12}\.html$/u.test(frame.pathname))
        return frame.origin + frame.pathname;
      if (frame.protocol === "https:" && frame.hostname === "geo.dailymotion.com" &&
        !frame.username && !frame.password && !frame.port && /^\/player\/[\w-]+\.html$/u.test(frame.pathname)) {
        const embedded = frame.searchParams.getAll('video');
        if (embedded.length === 1 && /^[a-z0-9]+$/iu.test(embedded[0])) return `https://www.dailymotion.com/video/${embedded[0]}`;
        let tab;
        try { tab = new URL(found.tabUrl); } catch { /* An unidentifiable player cannot supply a media URL. */ }
        if (tab?.protocol === "https:" && /(^|\.)dailymotion\.com$/u.test(tab.hostname) && !tab.username && !tab.password && !tab.port) {
          const video = tab.pathname.match(/^\/video\/([a-z0-9]+)(?:_[^/]*)?\/?$/iu)?.[1];
          if (video) return `https://www.dailymotion.com/video/${video}`;
        }
        // Never forward a transient ad/CDN source from this player. Without a
        // known content ID, leave only the player URL for normal unsupported handling.
        return frame.href;
      }
    } catch { /* Other sites continue through ordinary source resolution. */ }
    if (!found?.linkedinAsset || !found.sourcePage) return found?.url ?? "";
    let post, page;
    try { post = new URL(found.url); page = new URL(found.sourcePage); } catch { return found.url; }
    const activity = post.pathname.match(/^\/(?:embed\/)?feed\/update\/(urn:li:(?:activity|ugcPost|share):\d{8,25})\/$/u)?.[1];
    if (!activity || !/^[A-Za-z0-9_-]{8,100}$/u.test(found.linkedinAsset) ||
      [post, page].some(u => u.protocol !== "https:" || u.hostname !== "www.linkedin.com" || u.username || u.password || u.port) ||
      !(page.href === post.href || /^\/groups\/\d+\/?$/u.test(page.pathname) && page.searchParams.getAll("highlightedUpdateUrn").length === 1 && page.searchParams.get("highlightedUpdateUrn") === activity)) return found.url;
    const api = globalThis.ViFreeConnection?.api;
    if (!api?.tabs?.create || !api.tabs.sendMessage || !api.tabs.remove) return found.url;
    let tab;
    try {
      // A normal background post page exposes DOM media metadata that the new
      // feed omits. Use the user's existing session; never read cookies or call
      // account APIs, and close only the tab created for this explicit request.
      tab = await api.tabs.create({ url: page.href, active: false });
      const deadline = Date.now() + 16000;
      while (Date.now() < deadline) {
        try {
          const response = await api.tabs.sendMessage(tab.id, { type: "vifree-linkedin-source", asset: found.linkedinAsset }, { frameId: 0 });
          const media = new URL(response?.url);
          if (media.protocol === "https:" && !media.username && !media.password && !media.port && media.hostname.endsWith(".licdn.com") &&
            media.pathname.match(/^\/playlist\/vid\/v2\/([A-Za-z0-9_-]+)\/mp4-/u)?.[1] === found.linkedinAsset) return media.href;
        } catch { /* The page/content script or its DOM metadata may still be loading. */ }
        await new Promise(resolve => setTimeout(resolve, 350));
      }
    } catch { /* Public post extraction remains available if the browser lookup fails. */ }
    finally {
      if (Number.isInteger(tab?.id)) {
        let unchanged = false;
        try {
          const current = api.tabs.get ? await api.tabs.get(tab.id) : null;
          if (current?.url) unchanged = current.url === page.href;
          else {
            // tabs.get omits URLs without the broad tabs permission. The
            // post's own content script can confirm its current location.
            const currentPage = await api.tabs.sendMessage(tab.id, { type: "vifree-linkedin-source", asset: found.linkedinAsset }, { frameId: 0 });
            unchanged = currentPage?.pageUrl === page.href;
          }
        } catch { /* Never close a tab whose current page cannot be confirmed. */ }
        if (unchanged) await api.tabs.remove(tab.id).catch(() => {});
      }
    }
    return found.url;
  }
  async function resolveDetails(tab, info = {}) {
    if (info.linkUrl) return { url: info.linkUrl };
    const platform = socialPlatform(tab?.url ?? "");
    if (!platform) return { url: tab?.url ?? "" };
    const { api } = globalThis.ViFreeConnection;
    const fail = (message, code) => Object.assign(new Error(message), { code });
    if (!Number.isInteger(tab?.id)) throw fail("Open the video in a browser tab, then try again.", "inspect");
    let results;
    try { results = await api.scripting.executeScript({ target: { tabId: tab.id }, func: inspectVideoPage, args: [info.srcUrl ?? "", platform === "linkedin"] }); }
    catch { throw fail("Could not inspect this page. Reload the extension and page, then try again.", "inspect"); }
    let found = results?.find(result => result.frameId === 0)?.result;
    if (["facebook", "linkedin"].includes(platform) && found?.reason === "missing-link") {
      try {
        results = await api.scripting.executeScript({ target: { tabId: tab.id }, func: inspectVideoPage, args: [info.srcUrl ?? "", true] });
        found = results?.find(result => result.frameId === 0)?.result;
      } catch { throw fail("Could not inspect this page. Reload the extension and page, then try again.", "inspect"); }
    }
    if (found?.url && socialPlatform(found.url) === platform) return { ...found, url: await resolveSource(found) };
    if (platform === "reddit" && found?.reason === "source") {
      try {
        const media = new URL(found.url);
        if (media.protocol === "https:" && media.hostname === "v.redd.it" && !media.username && !media.password && !media.port &&
            /^\/[a-z0-9]{5,32}\/HLSPlaylist\.m3u8$/u.test(media.pathname)) return found;
      } catch { /* Only the selected Reddit master can replace its post link. */ }
    }
    if (platform === "linkedin" && found?.reason === "source") {
      try {
        const media = new URL(found.url);
        if (media.protocol === "https:" && !media.username && !media.password && !media.port && media.hostname.endsWith(".licdn.com") &&
          /^\/playlist\/vid\/v2\/[A-Za-z0-9_-]+\/mp4-/u.test(media.pathname)) return { ...found, url: media.href };
      } catch { /* Only LinkedIn's matched progressive media can replace its post link. */ }
    }
    throw found?.reason === "ambiguous"
      ? fail("More than one video is visible. Open the video you want, then try again.", "ambiguous")
      : fail("Could not find this video's link. Open the video itself or use Share → Copy link.", "missing");
  }
  async function resolve(tab, info = {}) { return (await resolveDetails(tab, info)).url; }
  function playlistFor(videoValue, pageValue) {
    try {
      const video = new URL(videoValue), page = new URL(pageValue);
      const trusted = url => url.protocol === "https:" && !url.username && !url.password && !url.port &&
        /^(?:(?:www|m|music)\.)?(?:youtube\.com|youtu\.be|youtube-nocookie\.com)$/u.test(url.hostname);
      const id = url => url.hostname === "youtu.be" ? url.pathname.slice(1) : url.pathname === "/watch" ? url.searchParams.get("v") : url.pathname.match(/^\/(?:shorts|embed)\/([\w-]+)$/u)?.[1];
      const videoId = id(video), list = page.searchParams.get("list");
      if (!trusted(video) || !trusted(page) || !/^[\w-]{11}$/u.test(videoId ?? "") || id(page) !== videoId ||
          page.searchParams.getAll("list").length !== 1 || !/^[\w-]{10,128}$/u.test(list ?? "")) return "";
      return list.startsWith("RD") ? `https://www.youtube.com/watch?v=${videoId}&list=${list}&start_radio=1` : `https://www.youtube.com/playlist?list=${list}`;
    } catch { return ""; }
  }
  return { resolve, resolveDetails, resolveSource, inspectVideoPage, playlistFor };
})();

if (typeof document !== "undefined") {
  const api = globalThis.browser ?? globalThis.chrome;
  api?.runtime?.onMessage?.addListener((message, sender, respond) => {
    if (message?.type === "vydrop-player-source" && sender.id === api.runtime.id) {
      const found = globalThis.ViFreeVideoDetection.inspectVideoPage();
      respond({ url: found?.reason === "mirror" ? found.url : "", pageUrl: location.href });
      return false;
    }
    if (message?.type !== "vifree-linkedin-source" || sender.id !== api.runtime.id ||
      !/^https:\/\/www\.linkedin\.com\//u.test(location.href) || !/^[A-Za-z0-9_-]{8,100}$/u.test(message.asset ?? "")) return false;
    respond({ ...globalThis.ViFreeVideoDetection.inspectVideoPage("", false, null, message.asset), pageUrl: location.href });
    return false;
  });
}
