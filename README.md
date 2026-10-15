# MarkdownExporter

[English](README.en.md) | 日本語

Markdownファイルをドラッグ＆ドロップ、GUI、CLIからHTMLとPDFに変換するWindowsアプリです。実行ファイル名は `mdexport.exe` です。

## 動作環境

- Windows 11 24H2以降
- WebView2 Runtime（HTMLとPDFのどちらにも必要）
- 開発時: .NET 10 SDK、描画バンドルの再生成時のみNode.js/npm

## 使い方

`mdexport.exe` を起動するとGUIが開きます。Markdownファイルを実行ファイル・ショートカット・開いた画面へドロップすると一覧に追加され、設定を確認してから「変換」を押せます。

GUIの出力先は「元ファイルと同じ場所」（既定）と「指定したフォルダー」から選べます。前者は各Markdownファイルの隣に出力し、後者は指定した共通フォルダーに出力します。「既定の設定」でも同じ選択を保存できます。

WebView2 Runtimeがない場合、GUIはMicrosoftのダウンロードページを案内します。CLIはURLを含むエラーを表示して終了します。ZIP版はRuntimeを同梱しません。インストーラ版は未導入時にMicrosoftのBootstrapperからRuntimeを導入します（ネットワーク接続が必要です）。

インストーラは「MarkdownExporterをユーザーのPATHに追加する」を既定で選択します。追加すると、新しく開いたターミナルから `mdexport.exe` を名前だけで実行できます。不要ならインストール時にチェックを外してください。サイレントインストールでは `/MERGETASKS="!addtopath"` で追加を無効にできます。アンインストール時はインストーラが追加したパスだけを削除します。ZIP版はPATHを変更しません。

「既定の設定」の表示言語は「システムに合わせる・日本語・英語」から選べます。画面の表示は選択時に切り替わり、保存すると次回起動にも適用されます。CLIのヘルプ、警告、エラーもこの言語設定を参照します。「システムに合わせる」ではWindowsの表示言語が日本語なら日本語、それ以外なら英語を使います。Markdown本文と出力テンプレートの内容は翻訳しません。

### CLI

GUIを開かずに変換するには `--run` と1個以上の `.md` または `.markdown` ファイルを指定します。複数ファイルをまとめて指定でき、パスに空白がある場合は引用符で囲みます。オプションを省略すると、各入力ファイルの隣に同名の `.html` と `.pdf` を作成します。CLIの変換オプションはGUIに保存した既定値を参照しません。

```text
mdexport.exe --run <入力.md> [別の入力.markdown ...] [オプション]
```

| オプション | 説明・省略時の動作 |
| --- | --- |
| `--run` | GUIを開かずに変換します。CLI変換では必須です。 |
| `-o <フォルダー>` / `--output <フォルダー>` | すべての入力の出力先を指定します。省略時は各入力ファイルと同じフォルダーです。相対パスは現在の作業フォルダーから解決します。 |
| `--template <名前または絶対パス>` | 使用するテンプレート。省略時は `default`。名前を指定すると `%APPDATA%\MarkdownExporter\Template\<名前>`、絶対パスを指定するとそのフォルダーを読み込みます。 |
| `--format <形式>` | `html`、`pdf`、`both` から選びます。省略時は `both`。`html` と `pdf` はそれぞれ指定した形式のみ出力します。 |
| `--embed-images` | HTMLのローカル画像をData URIで埋め込みます。省略時はHTMLの素材フォルダーへコピーします。 |
| `--orientation <向き>` | PDFの用紙方向。`portrait`（A4縦）または `landscape`（A4横）を指定します。省略時は `portrait` です。 |
| `--fit-figures-to-page` | PDFの画像・Mermaid図を印刷領域に収まるよう縮小します。省略時は適用しません。 |
| `--encoding <文字コード>` | `auto`、`utf-8`、`shift-jis`、`euc-jp` から選びます。省略時は `auto`。UTF-8を優先し、それ以外はShift JIS／EUC-JPを自動判定します。判定できない場合はエラーになります。指定値はすべての入力に適用します。 |
| `--overwrite` | 既存のHTML・PDFを上書きします。省略時は上書きしません。 |
| `--help` / `-h` | コマンドのヘルプを表示します。 |

PowerShellで配布フォルダーから実行する例:

```powershell
.\mdexport.exe --run .\report.md
.\mdexport.exe --run .\chapter1.md ".\draft chapter.md" --format html -o "C:\Output Files" --template custom
.\mdexport.exe --run .\report.md --format pdf --orientation landscape --fit-figures-to-page
.\mdexport.exe --run .\legacy.md --encoding shift-jis --embed-images --overwrite
.\mdexport.exe --help
```

`--overwrite` を指定せずに既存の出力ファイルがある場合、その入力は失敗し、ほかの入力の処理は続きます。複数の入力が同じ出力パスを使う場合は、書き込み前に変換全体を停止します。終了コードは成功・ヘルプ表示が `0`、変換失敗が `1`、引数の誤りが `2` です。

## テンプレート

初回起動時に実行ファイルの隣の `Template/default` を `%APPDATA%\MarkdownExporter\Template\default` へコピーします。編集したテンプレートは更新時に上書きしません。

テンプレートは `index.html` と `style.css` が必須です。`index.html` には `{{content}}` と `{{style}}` を入れ、必要なら `{{title}}` を使えます。追加の画像とフォントは `assets/` に置きます。`--template` にはAppDataのテンプレート名または絶対フォルダを渡せます。カスタムJavaScriptは使えません。

Markdown内のローカル画像はHTMLの `<文書名>_assets/content/` にコピーします。`--embed-images` では画像をData URIで埋め込みます。テンプレート素材は `<文書名>_assets/template/` に配置します。PDFのみの変換では素材フォルダを残しません。

PDF内のローカルPDFリンクには、出力PDFからリンク先への相対パスを記録します。リンク先PDFは出力先にコピーされないため、出力PDFとリンク先PDFの位置関係を維持してください。

## 開発

```powershell
./tools/prepare-qpdf.ps1 -DestinationDir ./artifacts/qpdf
dotnet build MarkdownExporter.slnx -m:1
dotnet test MarkdownExporter.slnx -m:1
dotnet publish MarkdownExporter/MarkdownExporter.csproj -c Release -r win-x64 -m:1
```

`MarkdownExporter.Tests` はNUnitでCLI解析、文字コード、事前検査、素材・テンプレート、Markdown/PDFリンク、設定保存などのロジックを検証します。WebView2の描画と画面操作はこのテストには含みません。

発行物には `mdexport.exe` と `Template/`、`WebAssets/`、`Licence/`、`Tools/qpdf/` を含めてください。qpdfはビルド前に上記スクリプトで取得します。サードパーティーの通知、NuGet依存の一覧とライセンスは `Licence/` にまとめています。KaTeXフォントのSIL Open Font License 1.1と著作権表示は `Licence/KaTeX-LICENSE.txt` に収録し、数式・Mermaidを含むHTMLの素材にもコピーします。`WebAssets` は `tools/web-assets/` で `npm ci` と `npm run build` により再生成できます。配布用ZIPとインストーラはこれらのファイルを同梱します。
