# MarkdownExporter

English | [日本語](README.md)

MarkdownExporter is a Windows application that converts Markdown files to HTML and PDF through drag and drop, a graphical interface, or the command line. The executable is named `mdexport.exe`.

## Requirements

- Windows 11 24H2 or later
- WebView2 Runtime (required for both HTML and PDF conversion)
- For development: .NET 10 SDK; Node.js/npm only when rebuilding the rendering bundle

## Usage

Run `mdexport.exe` to open the graphical interface. Drop Markdown files onto the executable, a shortcut, or the open window to add them to the file list. Review the settings, then click Convert.

For output, choose "Beside each source file" (the default) or "Specified folder" in the GUI. The first choice writes beside each Markdown file; the second writes to a shared folder. You can save the same choice in Default Settings.

If WebView2 Runtime is missing, the graphical interface offers to open Microsoft's download page. The CLI exits with an error containing the URL. The ZIP does not bundle the Runtime. The installer uses Microsoft's Bootstrapper to install it when needed and requires an internet connection for that step.

For command-line conversion, specify `--run`. With no conversion options, the CLI writes both HTML and PDF next to each input file. CLI conversion options do not use the defaults saved in the graphical interface.

In Default Settings, the display language can be set to Follow system, Japanese, or English. The interface switches language when you select it; saving the setting applies it on the next launch as well. CLI help, warnings, and errors use the same language setting. Follow system uses Japanese when the Windows display language is Japanese and English otherwise. Markdown content and output templates are not translated.

```text
mdexport --run report.md
mdexport --run report.md --format html -o C:\Output --template default
mdexport --run report.md --format pdf --orientation landscape --fit-figures-to-page
mdexport --run report.md --embed-images --encoding shift-jis --overwrite
mdexport --help
```

If an output file already exists, the CLI stops unless `--overwrite` is specified. If multiple inputs in one conversion would produce the same output path, the entire batch stops before writing.

## Templates

On the first launch, the application copies `Template/default` from beside the executable to `%APPDATA%\MarkdownExporter\Template\default`. Updates do not overwrite a template you have edited.

A template must contain `index.html` and `style.css`. Place `{{content}}` and `{{style}}` in `index.html`; `{{title}}` is optional. Put additional images and fonts in `assets/`. Pass an AppData template name or an absolute directory path to `--template`. Custom JavaScript is not supported.

Local images in Markdown are copied to `<document-name>_assets/content/` for HTML output. `--embed-images` embeds them as Data URIs. Template assets go into `<document-name>_assets/template/`. A PDF-only conversion does not retain an assets directory.

Local PDF links inside generated PDFs use paths relative to the generated PDF. Linked PDF files are not copied to the output directory, so keep their relative locations when moving the files.

## Development

```powershell
dotnet build MarkdownExporter.slnx -m:1
dotnet test MarkdownExporter.slnx -m:1
dotnet publish MarkdownExporter/MarkdownExporter.csproj -c Release -r win-x64 -m:1
```

`MarkdownExporter.Tests` uses NUnit to test logic such as CLI parsing, character encoding, preflight checks, assets and templates, Markdown and PDF links, and settings persistence. These tests do not cover WebView2 rendering or graphical interface interactions.

Include `mdexport.exe`, `Template/`, `WebAssets/`, `THIRD-PARTY-NOTICES.txt`, and `PDFsharp-LICENSE.txt` in a published distribution. To rebuild `WebAssets`, run `npm ci` and `npm run build` in `tools/web-assets/`. The ZIP and installer packaging are to be designed separately.
