# XN1Console vendored web assets

These assets are pinned locally so XN1Console does not depend on CDN availability at runtime.

- jQuery: 3.7.1
- Fomantic UI: 2.9.3
- unorm: 1.4.1, used by Fomantic UI's search module for diacritic normalization

Fomantic UI's generated emoji CSS normally points to Twemoji CDN assets. XN1Console does not use Fomantic's emoji helpers, so those remote `background-image` references are disabled in the vendored CSS to keep the host offline-friendly.
