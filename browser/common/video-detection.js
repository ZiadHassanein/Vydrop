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
          const activity = path.match(/^\/feed\/update\/urn:li:activity:(\d{8,25})\/?$/u);
          if (activity) return `https://www.linkedin.com/feed/update/urn:li:activity:${activity[1]}/`;
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
    function linkedInAsset(video) {
      const posters = [video?.poster ?? ""];
      for (const image of Array.from(video?.parentElement?.querySelectorAll("img") ?? []).slice(0, 10))
        if (image.closest(".vjs-poster")) posters.push(image.src);
      const assets = new Set();
      for (const poster of posters) {
        try {
          const url = new URL(poster);
          const asset = url.pathname.match(/^\/dms\/image\/(?:v2\/)?([A-Za-z0-9_-]+)\/videocover/u)?.[1];
          if (url.protocol === "https:" && url.hostname.endsWith(".licdn.com") && asset) assets.add(asset);
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
      if (local) {
        const asset = platform(location.href) === "linkedin" && !ranked[0].video.mediaKeys && linkedInAsset(ranked[0].video);
        return asset ? { url: local, reason: "player", linkedinAsset: asset, sourcePage: linkedInSourcePage || local } : { url: local, reason: "player" };
      }
      if (!platform(location.href)) {
        const video = ranked[0].video;
        const source = publicUrl(video.currentSrc || video.src);
        if (source) return { url: source, reason: "source" };
        const wrapped = publicUrl(video.closest("a[href]")?.href);
        if (wrapped && wrapped !== publicUrl(location.href) && new URL(wrapped).pathname !== "/") return { url: wrapped, reason: "player" };
        if (allVideos.length === 1) return { url: publicUrl(location.href), reason: "page" };
      }
      if (platform(location.href) === "dailymotion" && location.hostname === "geo.dailymotion.com" && /^\/player\/[\w-]+\.html$/u.test(new URL(location.href).pathname) && !permalink(location.href))
        return { url: publicUrl(location.href), reason: "frame" };
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
  async function resolveSource(found) {
    // Dailymotion configures its cross-origin player through messages; its
    // frame URL may contain only a player ID. The browser supplies the actual
    // containing tab URL, which must be a matching public Dailymotion video.
    try {
      const frame = new URL(found.frameUrl), tab = new URL(found.tabUrl);
      if (found.url === frame.href && frame.protocol === "https:" && frame.hostname === "geo.dailymotion.com" &&
        !frame.username && !frame.password && !frame.port && /^\/player\/[\w-]+\.html$/u.test(frame.pathname) &&
        tab.protocol === "https:" && /(^|\.)dailymotion\.com$/u.test(tab.hostname) && !tab.username && !tab.password && !tab.port) {
        const video = tab.pathname.match(/^\/video\/([a-z0-9]+)(?:_[^/]*)?\/?$/iu)?.[1];
        if (video) return `https://www.dailymotion.com/video/${video}`;
      }
    } catch { /* Other sites continue through ordinary source resolution. */ }
    if (!found?.linkedinAsset || !found.sourcePage) return found?.url ?? "";
    let post, page;
    try { post = new URL(found.url); page = new URL(found.sourcePage); } catch { return found.url; }
    const activity = post.pathname.match(/^\/feed\/update\/(urn:li:activity:\d{8,25})\/$/u)?.[1];
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
  async function resolve(tab, info = {}) {
    if (info.linkUrl) return info.linkUrl;
    const platform = socialPlatform(tab?.url ?? "");
    if (!platform) return tab?.url ?? "";
    const { api } = globalThis.ViFreeConnection;
    const fail = (message, code) => Object.assign(new Error(message), { code });
    if (!Number.isInteger(tab?.id)) throw fail("Open the video in a browser tab, then try again.", "inspect");
    let results;
    try { results = await api.scripting.executeScript({ target: { tabId: tab.id }, func: inspectVideoPage, args: [info.srcUrl ?? ""] }); }
    catch { throw fail("Could not inspect this page. Reload the extension and page, then try again.", "inspect"); }
    let found = results?.find(result => result.frameId === 0)?.result;
    if (platform === "facebook" && found?.reason === "missing-link") {
      try {
        results = await api.scripting.executeScript({ target: { tabId: tab.id }, func: inspectVideoPage, args: [info.srcUrl ?? "", true] });
        found = results?.find(result => result.frameId === 0)?.result;
      } catch { throw fail("Could not inspect this page. Reload the extension and page, then try again.", "inspect"); }
    }
    if (found?.url && socialPlatform(found.url) === platform) return resolveSource(found);
    if (platform === "linkedin" && found?.reason === "source") {
      try {
        const media = new URL(found.url);
        if (media.protocol === "https:" && !media.username && !media.password && !media.port && media.hostname.endsWith(".licdn.com") &&
          /^\/playlist\/vid\/v2\/[A-Za-z0-9_-]+\/mp4-/u.test(media.pathname)) return media.href;
      } catch { /* Only LinkedIn's matched progressive media can replace its post link. */ }
    }
    throw found?.reason === "ambiguous"
      ? fail("More than one video is visible. Open the video you want, then try again.", "ambiguous")
      : fail("Could not find this video's link. Open the video itself or use Share → Copy link.", "missing");
  }
  return { resolve, resolveSource, inspectVideoPage };
})();

if (typeof document !== "undefined") {
  const api = globalThis.browser ?? globalThis.chrome;
  api?.runtime?.onMessage?.addListener((message, sender, respond) => {
    if (message?.type !== "vifree-linkedin-source" || sender.id !== api.runtime.id ||
      !/^https:\/\/www\.linkedin\.com\//u.test(location.href) || !/^[A-Za-z0-9_-]{8,100}$/u.test(message.asset ?? "")) return false;
    respond({ ...globalThis.ViFreeVideoDetection.inspectVideoPage("", false, null, message.asset), pageUrl: location.href });
    return false;
  });
}
