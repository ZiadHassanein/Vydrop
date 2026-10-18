---
title: "إعداد إضافة Vydrop في Chrome وEdge وFirefox"
description: "فعّل ربط Vydrop بالمتصفح وثبّت الإضافة في Chrome أوEdge أوFirefox بالصور، مع خطوات الأذونات وإعادة التحميل وحل المشكلات."
lang: ar
dir: rtl
alternate: true
english: /browser-extension-setup.html
arabic: /ar/browser-extension-setup.html
---

# إعداد إضافة Vydrop في Chrome وEdge وFirefox

الإضافة اختيارية ومجانية؛ تنسخ رابط الفيديو الجاري تشغيله وتفتحه في Vydrop للمراجعة. لا يبدأ التنزيل تلقائيًا ولا تُستورد بيانات الحساب.

شرح Chrome وإرسال الفيديو يستخدم **لقطات حقيقية** بواجهة إنجليزية، محفوظة كملفات PNG الأصلية، مع تحديد الأزرار المطلوبة. اختر **كبّر التفاصيل بالحجم الأصلي** أسفل أي صورة لقراءة الأزرار دون تصغيرها، ومرّر داخل العرض المكبّر، أو اضغط الصورة لفتح الأصل. صور تفعيل الاتصال وEdge وFirefox رسومات توضيحية مذكورة بوضوح.

## جهّز التطبيق

تُظهر لقطات Chrome الإضافة 1.3.5. تستخدم الإضافة المرفقة 1.3.6 خطوات الإعداد نفسها؛ تأكد من ظهور الإصدار الجديد بعد تحميلها أو إعادة تحميلها.

نزّل [أحدث إصدار رسمي](https://github.com/ZiadHassanein/Vydrop/releases/latest) وفكّ الضغط في مجلد دائم قابل للكتابة. تتضمن حزمة **Vydrop 4.2.4** إضافة **1.3.6**. افتح **Vydrop.exe** واحتفظ بمجلد **Browser** بجانبه. اسم المجلد وحرف القرص في الصور مجرد مثال؛ استخدم مكان فك الضغط عندك.

{% include setup-shot.html id="folders" %}

اختر **الإعدادات ← الربط بالمتصفح ← تفعيل الاتصال** وانتظر تأكيد التفعيل. إذا كانت الواجهة بالإنجليزية فالمسار هو **Settings → Browser integration → Enable connection**. عند نقل التطبيق فعّل الربط من مكانه الجديد.

![توضيح تفعيل الاتصال من إعدادات Vydrop](../images/extension/02-enable-connection.svg)

## Google Chrome

### افتح صفحة الإضافات

اكتب `chrome://extensions` في شريط العنوان، أو افتح قائمة Chrome ذات النقاط الثلاث ثم **Extensions → Manage extensions**.

{% include setup-shot.html id="menu" %}

### فعّل وضع المطوّر واضغط Load unpacked

فعّل **Developer mode** في صفحة الإضافات، ثم اضغط **Load unpacked** لتحميل إضافة غير مضغوطة. اللقطة التالية مركّزة على زر التحميل؛ مفتاح وضع المطوّر خارج حدودها.

{% include setup-shot.html id="unpacked" %}

### انسخ مسار مجلد الإضافة الصحيح

داخل ملفات Vydrop المستخرجة افتح **Browser** ثم **chromium**. اضغط شريط العنوان في مستكشف الملفات وانسخ المسار الكامل. لازم ينتهي بـ **Browser\chromium** ويحتوي المجلد على ملف **manifest.json**.

{% include setup-shot.html id="path" %}

### اختر المجلد من نافذة Chrome

في نافذة **Select the extension directory** الصق المسار الكامل في شريط العنوان واضغط **Enter**. تأكد أنه ينتهي بـ **Browser\chromium**، ثم اضغط **Select Folder**.

اللقطة التالية تعرض النافذة قبل إكمال المسار. **لا تختَر مجلد Vydrop الرئيسي الظاهر في شريطها**؛ أضف **\Browser\chromium** أو الصق المسار الكامل من الخطوة السابقة قبل الضغط على Select Folder. كذلك لا تختَر ملف ZIP أو مجلد Browser الخارجي فقط.

{% include setup-shot.html id="picker" %}

### تأكد من تشغيل الإضافة

تأكد من ظهور بطاقة **Vydrop 1.3.6** وأن مفتاحها مفعّل. من **Details → Site access** اسمح بالوصول للمواقع المطلوبة؛ خيار **On all sites** يتيح الاكتشاف على المواقع المدعومة. يمكنك تثبيت زر الإضافة في شريط المتصفح إن رغبت.

{% include setup-shot.html id="enabled" %}

بعد التثبيت أعد تحميل صفحات الفيديو المفتوحة. [تعليمات Chrome الرسمية](https://developer.chrome.com/docs/extensions/get-started/tutorial/hello-world#load-unpacked).

## Microsoft Edge

1. افتح `edge://extensions` وفعّل وضع المطوّر.
2. اختر **Load unpacked** ثم `Browser/chromium` نفسه؛ لا تختَر ملف ZIP.
3. وافق على الوصول للمواقع المطلوبة وأعد تحميل صفحة الفيديو.

![توضيح Edge: تحميل مجلد Chromium من حزمة Vydrop](../images/extension/04-edge-setup.svg)

## Mozilla Firefox

1. افتح `about:debugging#/runtime/this-firefox`.
2. اختر **Load Temporary Add-on** ثم `Browser/firefox/manifest.json`.
3. وافق على الأذونات المطلوبة وأعد تحميل صفحة الفيديو.

**هذه إضافة محلية مؤقتة وتحتاج إلى تحميلها من جديد بعد إعادة تشغيل Firefox.** الحزم المحلية ليست منشورة في متجر المتصفح.

![توضيح Firefox: اختيار manifest.json كإضافة مؤقتة](../images/extension/05-firefox-setup.svg)

## إرسال الفيديو إلى Vydrop

حدّث صفحة الفيديو وشغّله، ثم اضغط **Download by Vydrop / تنزيل بواسطة Vydrop** خارج زاويته العليا اليمنى. إذا تعذّر وضع الزر استخدم زر الإضافة في شريط المتصفح أو مشاركة ثم نسخ الرابط.

{% include setup-shot.html id="player" %}

يفتح البرنامج أولًا، ثم يستقبل الرابط ويفحص الفيديو. قد يظهر الحقل فارغًا أو يظل زر التنزيل غير متاح للحظات أثناء هذه المرحلة؛ انتظر حتى تظهر المعلومات.

{% include setup-shot.html id="opening" %}

### راجع المعلومات وابدأ التنزيل

بعد ظهور الرابط ومعاينة الفيديو، اختر **فيديو / Video** أو **صوت / Audio**، ثم الصيغة والجودة ومجلد الحفظ. اضغط **تنزيل / Download** لبدء حفظ الملف بنفسك؛ الإضافة لا تبدأ التنزيل تلقائيًا.

{% include setup-shot.html id="review" %}

## حل المشكلات

عند تحديث المجلد أعد تحميل الإضافة ثم صفحة الفيديو. إذا كان التطبيق مشغولًا، أنهِ التنزيل الحالي أو ألغِه ثم حاول مجددًا. عند فشل الربط افتح التطبيق وفعّله من الإعدادات. استخدم مجلد Browser من حزمة التطبيق المستقرة نفسها، وتجنّب خلطه مع Preview.

لا تدعم الإضافة كل مشغّل، ولا تعني معرفة الرابط إمكان تنزيل فيديو خاص أو محمي أو يتطلب تسجيل دخول. قد تختلف مواضع الأزرار بحسب إصدار المتصفح. [تعليمات Edge الرسمية](https://learn.microsoft.com/en-us/microsoft-edge/extensions/getting-started/extension-sideloading) · [شرح التثبيت المؤقت في Firefox](https://extensionworkshop.com/documentation/develop/temporary-installation-in-firefox/).
