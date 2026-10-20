"""Exercise actual XML parsing and Google-compatible text sitemap boundaries."""
from pathlib import Path
import sys
import unittest
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from sitemaps import NS, xml_urls, text_urls

BASE = "https://ziadhassanein.github.io/Vydrop/"


def xml(*urls):
    root = ET.Element(f"{{{NS}}}urlset")
    for url in urls:
        ET.SubElement(ET.SubElement(root, f"{{{NS}}}url"), f"{{{NS}}}loc").text = url
    return ET.tostring(root, encoding="utf-8", xml_declaration=True)


class SitemapChecks(unittest.TestCase):
    def test_both_formats_preserve_canonical_localized_pages(self):
        urls = [BASE, BASE + "ar/", BASE + "fr/", BASE + "browser-extension-setup.html"]
        self.assertEqual(xml_urls(xml(*urls), BASE), urls)
        self.assertEqual(text_urls(("\n".join(urls) + "\n").encode(), BASE), urls)

    def test_rejects_malformed_xml_wrong_namespace_and_dtd(self):
        for data in [b"<urlset>", b"<urlset><url><loc>https://example.com/</loc></url></urlset>",
                     b"<!DOCTYPE urlset><urlset/>", b"<html>Not a sitemap</html>"]:
            with self.subTest(data=data), self.assertRaises((ValueError, ET.ParseError)):
                xml_urls(data, BASE)

    def test_rejects_empty_and_duplicate_sitemaps(self):
        for data in [xml(), xml(BASE, BASE)]:
            with self.subTest(data=data), self.assertRaises(ValueError):
                xml_urls(data, BASE)
        for data in [b"", (BASE + "\n" + BASE).encode()]:
            with self.subTest(data=data), self.assertRaises(ValueError):
                text_urls(data, BASE)

    def test_rejects_noncanonical_out_of_scope_or_authenticated_urls(self):
        for url in ["http://ziadhassanein.github.io/Vydrop/", "https://example.com/",
                    "https://ziadhassanein.github.io/Vydrop-other/", BASE + "../private/",
                    BASE + "%2e%2e/private/", BASE + "?q=1", BASE + "#section",
                    BASE.replace("https://", "https://user:pass@"), BASE + "two words.html"]:
            with self.subTest(url=url), self.assertRaises(ValueError):
                xml_urls(xml(url), BASE)
            with self.subTest(url=url), self.assertRaises(ValueError):
                text_urls(url.encode(), BASE)

    def test_rejects_multiple_locations_and_mixed_text_content(self):
        data = xml(BASE).replace(b"</ns0:url>", b"<ns0:loc>" + BASE.encode() + b"</ns0:loc></ns0:url>")
        with self.assertRaises(ValueError):
            xml_urls(data, BASE)
        with self.assertRaises(ValueError):
            text_urls((BASE + "\nThis is not a URL").encode(), BASE)


if __name__ == "__main__":
    unittest.main()
