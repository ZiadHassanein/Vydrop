---
title: "Vydrop : téléchargement vidéo gratuit pour Windows"
description: "Téléchargez des vidéos publiques ou leur audio sur Windows avec Vydrop : logiciel libre, playlists, sous-titres et extension Chrome, Edge ou Firefox."
lang: fr
language_code: fr-FR
locale: fr_FR
dir: ltr
alternate: true
english: /
arabic: /ar/
home: true
local_guide: true
---

# Vydrop : télécharger des vidéos et de l’audio sur Windows

[Télécharger Vydrop pour Windows](https://github.com/ZiadHassanein/Vydrop/releases/download/v4.3.0/Vydrop-v4.3.0-win-x64.zip) · Windows 10/11 x64 · v4.3.0

Vydrop 4.3.0 est un logiciel libre et gratuit de Ziad Hassanein pour enregistrer des vidéos publiques et leur audio sur Windows 10 ou 11 x64. Il prend en charge YouTube, TikTok, Facebook, Vimeo et d’autres sites compatibles avec yt-dlp. Le téléchargement dépend de l’accès au média et des formats disponibles.

## Installer et lancer Vydrop

Téléchargez l’archive ZIP officielle, décompressez-la dans un dossier accessible en écriture, puis ouvrez **Vydrop.exe**. Le runtime .NET est inclus. Au premier lien, l’application prépare et vérifie les outils multimédias manquants ; une connexion Internet et de l’espace disque sont nécessaires.

L’interface du logiciel est en anglais ou en arabe. Dans les étapes ci-dessous, **Video** signifie vidéo, **Audio** signifie son et **Download** lance le téléchargement. Le présent guide est destiné aux lecteurs francophones, notamment en France ; le fichier Windows officiel est le même pour tous.

## Enregistrer une vidéo ou extraire son audio

1. Copiez le lien public de la vidéo elle-même, et non l’adresse d’accueil du réseau social.
2. Collez-le dans Vydrop et attendez la fin de la vérification indiquée sur le bouton de téléchargement.
3. Sélectionnez **Video** ou **Audio**, le format, une qualité disponible et le dossier de destination.
4. Cliquez sur **Download**. Utilisez **Queue** pour plusieurs liens ou sélectionnez les éléments d’une playlist.
5. Une fois le fichier enregistré, ouvrez son dossier depuis l’action terminée, la file ou **Recent downloads**.

<h2 id="formats">Formats et qualité réellement disponibles</h2>

| Besoin | Formats | Condition |
| --- | --- | --- |
| Conserver l’image et le son | MP4, MKV, WebM | Les flux doivent être compatibles avec le conteneur. |
| Enregistrer uniquement le son | MP3, M4A, Opus, WAV, FLAC | La conversion respecte les limites de la source. |
| Enregistrer des sous-titres | SRT, VTT | La vidéo doit proposer la langue de sous-titres choisie. |

Un débit élevé ne rétablit pas les détails perdus dans le son d’origine. Si le débit de la source est inconnu, le choix automatique évite de promettre une qualité non mesurée. Pour activer les sous-titres, utilisez **Subtitles** ; la sélection apparaît dans le panneau récapitulatif.

<h2 id="extension">Configurer l’extension du navigateur</h2>

Dans Vydrop, ouvrez **Settings → Browser integration → Manual setup and troubleshooting → Load the local companion**. Installez ensuite l’extension 1.4.1 incluse dans le dossier **Browser** de l’archive décompressée :

- **Chrome :** ouvrez chrome://extensions, activez le mode développeur et chargez le dossier **Browser/chromium**.
- **Edge :** ouvrez edge://extensions et chargez le même dossier après avoir activé le mode développeur.
- **Firefox :** ouvrez about:debugging#/runtime/this-firefox et chargez **Browser/firefox/manifest.json** comme module temporaire. Recommencez après un redémarrage de Firefox.

Autorisez l’accès aux sites souhaités et actualisez les onglets vidéo. Le bouton **Download by Vydrop**, placé à côté du lecteur détecté, envoie le lien à l’application pour vérification. Il ne lance pas automatiquement le téléchargement.

Le [guide illustré de l’extension](../browser-extension-setup.md) et la [liste des limites de prise en charge](../supported-sites.md) sont en anglais. Enregistrez uniquement les médias que vous avez le droit de télécharger. Vydrop ne contourne pas l’authentification ni les DRM.

## Questions fréquentes

### Vydrop est-il disponible en français ?

Ce guide est en français. L’application propose actuellement une interface en anglais ou en arabe ; ses boutons ne sont pas traduits en français. Elle fonctionne sur Windows 10 et 11 x64.

### Peut-on télécharger une vidéo depuis n’importe quel site ?

Vydrop utilise yt-dlp pour les pages publiques de plus de 1 000 sites pris en charge. La disponibilité du média, le lecteur et les restrictions du site peuvent empêcher un téléchargement. Les contenus privés, nécessitant une connexion ou protégés par DRM ne sont pas pris en charge.

### Le logiciel et l’extension sont-ils gratuits ?

Oui. Vydrop est gratuit et libre sous licence MIT, sans abonnement ni compte Vydrop. L’extension pour Chrome, Edge ou Firefox est facultative. Le logiciel ne collecte pas de télémétrie.

### Un MP3 à débit élevé améliore-t-il la source ?

Non. Un débit de sortie plus élevé ne recrée pas les détails absents du son d’origine. Les choix dépendent des informations mesurées ou fournies par la source. WAV et FLAC ne restaurent pas non plus ces détails.
