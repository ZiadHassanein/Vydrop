"""Build Google's supported text fallback and verify the deployed sitemap files.

The Jekyll XML sitemap remains the source of URLs. No hand-maintained second list,
build-time lastmod dates, credentials, Google ping API, or indexing promises.
"""
import argparse
import json
from pathlib import Path
import re
import sys
import time
from urllib.parse import unquote, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
NS = "http://www.sitemaps.org/schemas/sitemap/0.9"
MAX_BYTES = 50 * 1024 * 1024


def site_base():
    config = (ROOT / "docs/_config.yml").read_text(encoding="utf-8")
    origin = re.search(r"^url: (\S+)$", config, re.M)[1]
    base = re.search(r"^baseurl: (\S+)$", config, re.M)[1]
    return origin.rstrip("/") + "/" + base.strip("/") + "/"


def checked_urls(urls, base):
    if not 1 <= len(urls) <= 50000 or len(set(urls)) != len(urls):
        raise ValueError("Sitemap must contain 1–50000 unique URLs")
    scope = urlsplit(base)
    for url in urls:
        parsed = urlsplit(url)
        decoded_path = unquote(parsed.path)
        if (parsed.scheme != "https" or parsed.netloc != scope.netloc
                or not decoded_path.startswith(scope.path)
                or parsed.query or parsed.fragment or parsed.username or parsed.password
                or any(c.isspace() for c in url) or "\\" in decoded_path
                or any(part in (".", "..") for part in decoded_path.split("/"))):
            raise ValueError(f"URL outside the canonical site scope: {url}")
    return urls


def xml_urls(data, base):
    if len(data) > MAX_BYTES:
        raise ValueError("Sitemap exceeds the uncompressed 50 MiB limit")
    # ElementTree does not resolve external entities; reject DTDs as unnecessary.
    text = data.decode("utf-8-sig")
    if "<!DOCTYPE" in text or "<!ENTITY" in text:
        raise ValueError("Sitemaps must not contain a DTD or entity declarations")
    root = ET.fromstring(text)
    if root.tag != f"{{{NS}}}urlset":
        raise ValueError("Expected a namespaced XML URL sitemap")
    urls = []
    for entry in root:
        locations = entry.findall(f"{{{NS}}}loc")
        if entry.tag != f"{{{NS}}}url" or len(locations) != 1 or not locations[0].text:
            raise ValueError("Each sitemap entry must have exactly one loc")
        urls.append(locations[0].text.strip())
    return checked_urls(urls, base)


def text_urls(data, base):
    if len(data) > MAX_BYTES:
        raise ValueError("Sitemap exceeds the uncompressed 50 MiB limit")
    return checked_urls([line for line in data.decode("utf-8-sig").splitlines() if line], base)


class NoRedirects(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError(f"Sitemap must return directly, without HTTP {code}: {req.full_url}")


def fetch(url, media_types):
    request = Request(url, headers={"User-Agent": "Vydrop-publication-check", "Cache-Control": "no-cache"})
    with build_opener(NoRedirects).open(request, timeout=30) as response:
        if response.status != 200 or response.headers.get_content_type() not in media_types:
            raise ValueError(f"Unexpected sitemap HTTP response: {url}")
        if "noindex" in response.headers.get("X-Robots-Tag", "").lower():
            raise ValueError(f"Sitemap is marked noindex: {url}")
        data = response.read(MAX_BYTES + 1)
        if len(data) > MAX_BYTES:
            raise ValueError("Sitemap exceeds the uncompressed 50 MiB limit")
        return data


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("build", "verify-published"))
    parser.add_argument("site", type=Path, help="The validated Jekyll output/artifact directory")
    args = parser.parse_args()
    base = site_base()
    expected = xml_urls((args.site / "sitemap.xml").read_bytes(), base)
    if args.mode == "build":
        (args.site / "sitemap.txt").write_bytes(("\n".join(expected) + "\n").encode("utf-8"))
        print(f"Built text sitemap from {len(expected)} canonical XML URLs.")
        return
    if set(text_urls((args.site / "sitemap.txt").read_bytes(), base)) != set(expected):
        raise ValueError("The built XML and text sitemap differ")
    for attempt in range(6):
        try:
            live_xml = xml_urls(fetch(base + "sitemap.xml", {"application/xml", "text/xml"}), base)
            live_text = text_urls(fetch(base + "sitemap.txt", {"text/plain"}), base)
            if set(live_xml) != set(expected) or set(live_text) != set(expected):
                raise ValueError("Published sitemaps do not yet match the validated build")
            print(json.dumps({"status": "passed", "base": base, "urls": len(expected),
                              "sitemaps": [base + "sitemap.xml", base + "sitemap.txt"]}))
            return
        except (OSError, ValueError, ET.ParseError) as error:
            if attempt == 5:
                raise
            print(f"Waiting for the published sitemap ({attempt + 1}/6): {error}", file=sys.stderr)
            time.sleep(10)


if __name__ == "__main__":
    main()
