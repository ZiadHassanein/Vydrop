---
title: "Vydrop: descargar vídeos gratis en Windows"
description: "Guarda vídeos públicos o su audio en Windows con Vydrop: gratis, de código abierto, con listas, subtítulos y extensión para Chrome, Edge y Firefox."
lang: es
language_code: es-ES
locale: es_ES
dir: ltr
alternate: true
english: /
arabic: /ar/
home: true
local_guide: true
---

# Vydrop: descargar vídeos y audio en Windows

[Descargar Vydrop para Windows](https://github.com/ZiadHassanein/Vydrop/releases/download/v4.2.3/Vydrop-v4.2.3-win-x64.zip) · Windows 10/11 x64 · v4.2.3

Vydrop 4.2.3, de Ziad Hassanein, es un programa gratuito y de código abierto para guardar vídeos públicos y audio. Usa yt-dlp para sitios compatibles, como YouTube, TikTok, Facebook o Vimeo. La descarga depende del acceso público al vídeo y de los formatos que facilite la fuente.

## Abrir el programa en Windows

Descarga el ZIP oficial, extrae todos sus archivos a una carpeta con permiso de escritura y abre **Vydrop.exe**. Necesitas Windows 10 u 11 x64. El entorno .NET está incluido; al pegar el primer enlace, el programa prepara y verifica las herramientas multimedia que falten.

Esta guía utiliza español de España. La interfaz de la aplicación está disponible en inglés y árabe: **Video** es vídeo, **Audio** es sonido y **Download** inicia la descarga. El mismo paquete oficial de Windows se utiliza en todos los países.

## Guardar un vídeo, una lista o solo el audio

1. Copia el enlace público del vídeo concreto, no la página principal de la red social.
2. Pégalo en Vydrop y espera a que termine la comprobación indicada en el botón de descarga.
3. Elige **Video** o **Audio**, el formato, una calidad disponible y la carpeta de destino.
4. Pulsa **Download**. Usa **Queue** para varios enlaces o selecciona entradas concretas de una lista de reproducción.
5. Al terminar, abre la carpeta del archivo desde la acción completada, la cola o **Recent downloads**.

<h2 id="formats">Formatos y calidad de la fuente</h2>

| Lo que quieres guardar | Formatos | De qué depende |
| --- | --- | --- |
| Imagen y sonido | MP4, MKV, WebM | Los flujos deben ser compatibles con el contenedor. |
| Solo el audio | MP3, M4A, Opus, WAV, FLAC | La calidad original y la codificación elegida. |
| Subtítulos | SRT, VTT | Las pistas e idiomas disponibles en el vídeo. |

Un bitrate mayor no recupera detalles que no existen en la fuente. El programa utiliza los datos medidos o proporcionados por el sitio para ofrecer opciones. Activa **Subtitles** para elegir una pista de subtítulos disponible; la selección aparece en el resumen del vídeo.

<h2 id="extension">Activar la extensión en el navegador</h2>

En Vydrop, abre **Settings → Browser integration → Enable connection**. Después instala la extensión 1.3.5 incluida en la carpeta **Browser** del ZIP extraído:

- **Chrome:** abre chrome://extensions, activa el modo de desarrollador y carga la carpeta **Browser/chromium** sin comprimir.
- **Edge:** abre edge://extensions, activa el modo de desarrollador y carga la misma carpeta.
- **Firefox:** abre about:debugging#/runtime/this-firefox y carga **Browser/firefox/manifest.json** como complemento temporal. Debes volver a cargarlo después de reiniciar Firefox.

Permite el acceso a los sitios que utilices y actualiza las pestañas de vídeo abiertas. **Download by Vydrop** aparece junto al reproductor detectado y envía el enlace al programa para revisarlo. La descarga se inicia después, cuando tú la confirmas en Vydrop.

La [guía ilustrada de la extensión](../browser-extension-setup.md) y los [límites de sitios compatibles](../supported-sites.md) están en inglés. Descarga únicamente medios que tengas permiso para guardar. No se eluden inicios de sesión ni protecciones DRM.

## Preguntas frecuentes

### ¿Vydrop tiene interfaz en español?

Esta guía está en español. La aplicación ofrece actualmente interfaz en inglés y árabe y funciona en Windows 10 y 11 x64. Los pasos de esta página indican los nombres de los botones en inglés.

### ¿Puedo descargar vídeos de cualquier web?

Vydrop utiliza yt-dlp para páginas públicas de más de 1.000 sitios compatibles. Un cambio del sitio, su reproductor o las restricciones de acceso pueden impedir la descarga. No se admiten vídeos privados, contenido que exija iniciar sesión ni medios protegidos por DRM.

### ¿Son gratuitos el programa y la extensión?

Sí. Vydrop es gratuito y de código abierto con licencia MIT. No requiere cuenta de Vydrop ni suscripción y no incluye telemetría. La extensión para Chrome, Edge o Firefox es opcional.

### ¿Un MP3 con más bitrate suena mejor que el original?

No recupera detalles que faltan en el audio de origen. Las opciones dependen de los datos de la fuente. Si se desconocen, puede aparecer Automatic. Guardar en WAV o FLAC tampoco restaura la calidad perdida de una fuente comprimida.
