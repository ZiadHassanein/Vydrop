# Security

Report a suspected vulnerability privately through [GitHub security reporting](https://github.com/ZiadHassanein/Vydrop/security/advisories/new). If private reporting is unavailable, contact the email listed in the app; do not put credentials, private media URLs, or exploitable details in a public issue.

Include the Vydrop version, Windows version, affected feature, and concise reproduction steps. Redact personal paths and tokens. Support focuses on the latest stable release and the active preview.

Media URLs and metadata are untrusted. Vydrop uses bounded input handling, explicit subprocess arguments, verified tool downloads, private application state, and output validation. Browser integration is optional and requires a user action to send a link. It does not import browser cookies or collect telemetry.

Supported sites depend on upstream extractors. Public-link validation is not a full network sandbox, and this application does not bypass DRM or authentication. Download media only when you have permission.

## Update trust boundary

The active development preview persists queue jobs and recovery backups locally. This includes URLs with any signed query parameters, source pages, output preferences and completion paths. These files are not encrypted application secrets: other processes running as the same Windows user can read them. Removing a queue item changes the primary file; its previous-state backup can retain the item until a later save. Keep these files out of public bug reports.

Optional stream discovery requires an explicit browser permission grant. It observes media response metadata, not request headers, cookies, login data or response bodies. Candidate URLs stay in bounded, short-lived browser memory and are sent to the app only after an explicit action. Discovery does not establish that a resource is publicly accessible; the app downloads without importing the browser session. The native protocol retains version 1 and validates version-2 message types, sizes, source URLs and request IDs. Host registration and a recent end-to-end connection test are distinct states.

Direct-file acceleration requires a strong ETag and exact HTTP range responses. Changed validators or inconsistent ranges stop assembly; incomplete data is never promoted as a verified completed download. Current-user process/pipe limits and existing stable/preview identities remain in effect.

Release hashes, GitHub asset digests and official tool checksums detect corruption and substitutions relative to the expected publisher. They are fetched from the same publisher accounts as the binaries, so they do not protect against a compromised publisher account or release workflow. Current desktop packages are not Authenticode-signed and there is no independent embedded signing key. New app releases require the user's install action; tool checks can happen automatically.

Compatible tools may be reused from the app folder, PATH or WinGet Links after executable probes. A version probe executes that program with your Windows user's privileges. Keep those directories trusted; the probe is a compatibility check, not a malware scanner. Windows job objects contain subprocess lifetime and inherited handles, not filesystem or network access. Public-link filtering is not a complete network sandbox.

An independent signing scheme and opt-in PATH reuse are proposed changes, not shipped protections. Report suspicious publisher assets privately. No diagnostics should contain cookies, credentials, private media URLs or personal paths.

## حدود الثقة في التحديثات

تكشف البصمات والبيانات المنشورة تلف الملفات أو تبديلها مقارنة بالناشر المتوقع، لكنها تأتي من حساب الناشر نفسه ولا تحمي من اختراق حسابه أو عملية الإصدار. لا تتضمن الحزمة الحالية توقيع Authenticode أو مفتاح توقيع مستقلًا داخل التطبيق. تحديث التطبيق يتطلب اختيار التثبيت، بينما قد تُفحص تحديثات الأدوات تلقائيًا.

قد يُعاد استخدام أدوات متوافقة من مجلد التطبيق أو PATH أو روابط WinGet بعد تشغيل فحص الإصدار بصلاحيات مستخدم ويندوز. حافظ على موثوقية هذه المجلدات؛ فحص التوافق ليس فحصًا للبرمجيات الخبيثة. احتواء العمليات يضبط عمرها والمقابض الموروثة، ولا يعزل وصولها إلى الملفات أو الشبكة. التوقيع المستقل وجعل استخدام PATH اختياريًا مقترحان ولم يُنفّذا بعد.
