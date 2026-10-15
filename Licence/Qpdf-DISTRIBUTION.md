# Bundled qpdf binaries

MarkdownExporter distributes the qpdf 12.4.2 official MSVC x64 binary archive from
https://github.com/qpdf/qpdf/releases/tag/v12.4.2 . The archive SHA-256 is
`db87077e683630c1217e0e8f9a20a9749d952ab676e881c3689187763a5de25d`.
`tools/prepare-qpdf.ps1` verifies this hash and extracts only `qpdf.exe`,
`qpdf30.dll`, and the eight Visual C++ runtime DLLs required beside them.

qpdf's Apache 2.0 license and NOTICE are in `Qpdf-LICENSE.txt` and
`Qpdf-NOTICE.md`. License texts from the release's matching vcpkg archive for
the static build dependencies are in the `Qpdf-*-LICENSE.txt` files: OpenSSL,
zlib, libjpeg-turbo, and libspng. The Visual C++ runtime files are Microsoft
redistributables; see the [Visual Studio 2026 redistribution terms](https://learn.microsoft.com/en-us/visualstudio/releases/2026/redistribution).

When upgrading qpdf, recheck the archive hash, included DLLs, and dependency
notices before publishing a release.
