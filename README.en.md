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

In Default Settings, the display language can be set to Follow system, Japanese, or English. The interface switches language when you select it; saving the setting applies it on the next launch as well. CLI help, warnings, and errors use the same language setting. Follow system uses Japanese when the Windows display language is Japanese and English otherwise. Markdown content and output templates are not translated.

### CLI

To convert without opening the GUI, pass `--run` and one or more `.md` or `.markdown` files. You can process multiple files together; quote paths that contain spaces. With no conversion options, the CLI writes `<input-name>.html` and `<input-name>.pdf` beside each input file. CLI conversion options do not use defaults saved in the graphical interface.

```text
mdexport.exe --run <input.md> [another.markdown ...] [options]
```

| Option | Behavior and default |
| --- | --- |
| `--run` | Convert without opening the GUI. Required for command-line conversion. |
| `-o <folder>` / `--output <folder>` | Send all outputs to this folder. By default, each output is written beside its input. Relative paths are resolved from the current working directory. |
| `--template <name-or-absolute-path>` | Use a template. The default is `default`. A name selects `%APPDATA%\MarkdownExporter\Template\<name>`; an absolute path selects that folder. |
| `--format <format>` | Choose `html`, `pdf`, or `both`; default: `both`. `html` and `pdf` produce only the named format. |
| `--embed-images` | Embed local images as Data URIs in HTML. By default, they are copied to the HTML asset folder. |
| `--orientation <orientation>` | Choose `portrait` (A4 portrait) or `landscape` (A4 landscape); default: `portrait`. |
| `--fit-figures-to-page` | Scale images and Mermaid diagrams down to the PDF printable area; off by default. |
| `--encoding <encoding>` | Choose `auto`, `utf-8`, `shift-jis`, or `euc-jp`; default: `auto`. UTF-8 is tried first, then Shift JIS/EUC-JP detection. Detection failure is an error. The choice applies to all inputs. |
| `--overwrite` | Replace existing HTML/PDF outputs; off by default. |
| `--help` / `-h` | Show command-line help. |

Examples from the distribution folder in PowerShell:

```powershell
.\mdexport.exe --run .\report.md
.\mdexport.exe --run .\chapter1.md ".\draft chapter.md" --format html -o "C:\Output Files" --template custom
.\mdexport.exe --run .\report.md --format pdf --orientation landscape --fit-figures-to-page
.\mdexport.exe --run .\legacy.md --encoding shift-jis --embed-images --overwrite
.\mdexport.exe --help
```

Without `--overwrite`, an input whose output already exists fails, while other inputs continue. If multiple inputs would produce the same output path, conversion stops before writing any output. Exit codes are `0` for success or help, `1` for conversion failure, and `2` for invalid arguments.

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

Include `mdexport.exe`, `Template/`, `WebAssets/`, and `Licence/` in a published distribution. Third-party notices, the NuGet dependency inventory, and license texts are collected in `Licence/`. `Licence/KaTeX-LICENSE.txt` contains the SIL Open Font License 1.1 and copyright notices for the KaTeX fonts; it is also copied alongside fonts in HTML output containing math or Mermaid. To rebuild `WebAssets`, run `npm ci` and `npm run build` in `tools/web-assets/`. The ZIP and installer include these files.
