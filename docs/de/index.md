---
title: "Vydrop: Kostenloser Video-Downloader für Windows"
description: "Öffentliche Videos und Audio unter Windows speichern: Vydrop ist kostenlos und quelloffen, mit Playlists, Untertiteln und optionaler Browser-Erweiterung."
lang: de
language_code: de-DE
locale: de_DE
dir: ltr
alternate: true
english: /
arabic: /ar/
home: true
local_guide: true
---

# Vydrop: Videos und Audio unter Windows herunterladen

[Vydrop für Windows herunterladen](https://github.com/ZiadHassanein/Vydrop/releases/download/v4.4.12/Vydrop-v4.4.12-win-x64.zip) · Windows 10/11 x64 · v4.4.12

Vydrop 4.4.12 von Ziad Hassanein ist ein kostenloser, quelloffener Downloader für öffentliche Videos und Audio. Die Windows-App verwendet yt-dlp und unterstützt unter anderem YouTube, TikTok, Facebook und Vimeo, sofern ein geeigneter öffentlicher Stream verfügbar ist.

## Die Windows-App starten

Laden Sie das offizielle ZIP-Archiv herunter, entpacken Sie es in einen beschreibbaren Ordner und starten Sie **Vydrop.exe**. Benötigt wird Windows 10 oder 11 mit x64-Prozessor. Die .NET-Laufzeit ist enthalten. Fehlende Medienwerkzeuge werden beim ersten Link automatisch heruntergeladen und geprüft.

Diese Anleitung richtet sich an deutschsprachige Nutzer, insbesondere in Deutschland. Die App selbst bietet Englisch und Arabisch. **Video** steht für Video, **Audio** für Ton, **Download** startet das Herunterladen und **Recent downloads** öffnet die zuletzt gespeicherten Dateien. Weltweit wird dasselbe offizielle Windows-Paket verwendet.

## Ein Video speichern oder nur den Ton herunterladen

1. Kopieren Sie den öffentlichen Link des konkreten Videos, nicht die Startseite des sozialen Netzwerks.
2. Fügen Sie ihn in Vydrop ein. Während der Prüfung zeigt die Download-Schaltfläche den aktuellen Zustand.
3. Wählen Sie **Video** oder **Audio**, ein passendes Format, eine verfügbare Qualität und den Zielordner.
4. Klicken Sie auf **Download**. Für mehrere Links gibt es **Queue**; bei Wiedergabelisten lassen sich einzelne Einträge auswählen.
5. Nach Abschluss können Sie den Ausgabeordner über die fertige Aktion, die Warteschlange oder **Recent downloads** öffnen.

<h2 id="formats">Formate und Quellqualität</h2>

| Aufgabe | Formate | Voraussetzung |
| --- | --- | --- |
| Bild und Ton speichern | MP4, MKV, WebM | Die ermittelten Streams müssen zum Container passen. |
| Nur Audio speichern | MP3, M4A, Opus, WAV, FLAC | Die Qualität bleibt durch die Quelle begrenzt. |
| Untertitel speichern | SRT, VTT | Die gewünschte Untertitelsprache muss verfügbar sein. |

Eine höhere Bitrate erzeugt keine zusätzlichen Quelldetails. Bei unbekannten Audio-Eigenschaften kann nur **Automatic** angeboten werden. Unter **Subtitles** wählen Sie vorhandene Untertitel; die Auswahl erscheint in der Medienübersicht. Vydrop unterstützt keine verschlüsselten DRM-Inhalte und umgeht keine Anmeldung.

<h2 id="extension">Chrome, Edge und Firefox verbinden</h2>

Öffnen Sie in Vydrop **Browser integration → choose your browser → Set up**. Installieren Sie anschließend die mitgelieferte Erweiterung 1.5.8:

- **Chrome:** Öffnen Sie chrome://extensions, aktivieren Sie den Entwicklermodus und laden Sie den entpackten Ordner **Browser/chromium**.
- **Edge:** Öffnen Sie edge://extensions und laden Sie nach Aktivierung des Entwicklermodus denselben Ordner.
- **Firefox:** Öffnen Sie about:debugging#/runtime/this-firefox und laden Sie **Browser/firefox/manifest.json** als temporäres Add-on. Nach einem Firefox-Neustart muss es erneut geladen werden.

Erlauben Sie den Zugriff auf die gewünschten Websites und laden Sie Video-Tabs neu. **Download by Vydrop** erscheint oben rechts im Video. Wählen Sie Video oder Audio, Format und Qualität und bestätigen Sie **Download**. Die Ausgabe startet im gespeicherten Ordner, auch wenn Vydrop geschlossen war. Ältere App-Versionen öffnen die Vorschau.

Die [bebilderte Browser-Anleitung](../browser-extension-setup.md) und die [Hinweise zu unterstützten Websites](../supported-sites.md) sind auf Englisch. Laden Sie nur Medien herunter, für deren Speicherung Sie die nötigen Rechte haben.

## Häufige Fragen

### Gibt es Vydrop auf Deutsch?

Diese Anleitung ist auf Deutsch. Die Desktop-App bietet derzeit eine englische und eine arabische Oberfläche. Sie läuft auf Windows 10 und 11 x64. Die Namen der englischen Schaltflächen werden hier erklärt.

### Kann Vydrop Videos von jeder Website herunterladen?

Vydrop nutzt yt-dlp für öffentliche Inhalte von mehr als 1.000 unterstützten Websites. Änderungen am Anbieter, der Player und Zugriffsbeschränkungen können einen Download verhindern. Private Inhalte, Anmeldepflicht und DRM werden nicht umgangen.

### Sind die App und die Erweiterung kostenlos?

Ja. Vydrop ist kostenlos und unter der MIT-Lizenz quelloffen. Ein Vydrop-Konto oder Abonnement ist nicht nötig. Die Erweiterung für Chrome, Edge oder Firefox ist optional. Die App enthält keine Telemetrie.

### Verbessert eine höhere MP3-Bitrate die Tonqualität?

Eine höhere Ausgabebitrate stellt verlorene Details der Quelle nicht wieder her. Die angebotenen Werte richten sich nach ermittelten Quelldaten. Auch WAV und FLAC können die ursprüngliche Qualität komprimierter Audiodaten nicht wiederherstellen.
