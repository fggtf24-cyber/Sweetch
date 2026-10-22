# Third-party software

Sweetch uses MIT-licensed code (copyright notices are preserved in [LICENSE](LICENSE)) and the following software. Each component keeps its own license.

| Component | Used for | License |
|---|---|---|
| [Magick.NET](https://github.com/dlemstra/Magick.NET) / ImageMagick | Images | Apache-2.0 / ImageMagick License |
| [PDFtoImage](https://github.com/sungaila/PDFtoImage) / PDFium / SkiaSharp | PDF → images | MIT / BSD-3-Clause / MIT |
| [PdfPig](https://github.com/UglyToad/PdfPig) | PDF text | Apache-2.0 |
| [PDFsharp](https://github.com/empira/PDFsharp) 6.2 | Merge, split, rotate PDF | MIT |
| [ClosedXML](https://github.com/ClosedXML/ClosedXML) | XLSX | MIT |
| [ExcelDataReader](https://github.com/ExcelDataReader/ExcelDataReader) | XLS | MIT |
| [SharpCompress](https://github.com/adamhathcock/sharpcompress) | RAR, 7z, TAR… | MIT |
| [FFmpeg 9.0.2](https://ffmpeg.org) (included as a separate executable in `tools/`, “essentials” build by [gyan.dev](https://www.gyan.dev/ffmpeg/builds/)) | Audio and video | GPL-3.0 – source: https://github.com/FFmpeg/FFmpeg/tree/n9.0.2 |
| [Pandoc 3.12.1](https://pandoc.org) (included as a separate executable in `tools/`) | Documents | GPL-2.0-or-later – source: https://github.com/jgm/pandoc/tree/3.12.1 |
| [Inter](https://github.com/rsms/inter) and [IBM Plex Mono](https://github.com/IBM/plex) (embedded fonts) | Interface type | SIL OFL 1.1 – licence texts in `src/Sweetch.App/Assets/Fonts` |
| [LibreOffice](https://www.libreoffice.org) (optional, installed separately) | Office files → PDF | MPL-2.0 |

Pandoc is bundled from the unmodified official Windows distribution. Its `COPYING.rtf`, `COPYRIGHT.txt` and `MANUAL.html` are included in `tools/`. Source release: https://hackage.haskell.org/package/pandoc-3.12.1. The Windows ZIP is verified against SHA-256 `6ef431cb20b2c24a0371fc19200dc49d452be54b1b5c9239f3462841ef53e587` before packaging.

FFmpeg is bundled from the unmodified gyan.dev “essentials” build 9.0.2 (https://github.com/GyanD/codexffmpeg/releases/tag/9.0.2). Its licence and README, which lists the libraries it is built with and their sources, are included in `tools/` as `FFMPEG-LICENSE.txt` and `FFMPEG-README.txt`. The ZIP is verified against SHA-256 `60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba` before packaging. LibreOffice is installed separately. These components run as separate programs; Sweetch invokes them through their command-line interfaces.
