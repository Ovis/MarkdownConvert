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

CLIでは `--run` を指定します。オプション省略時はHTMLとPDFの両方を入力の隣に出力します。CLIの変換オプションはGUIに保存した既定値を参照しません。

「既定の設定」の表示言語は「システムに合わせる・日本語・英語」から選べます。画面の表示は選択時に切り替わり、保存すると次回起動にも適用されます。CLIのヘルプ、警告、エラーもこの言語設定を参照します。「システムに合わせる」ではWindowsの表示言語が日本語なら日本語、それ以外なら英語を使います。Markdown本文と出力テンプレートの内容は翻訳しません。

```text
mdexport --run report.md
mdexport --run report.md --format html -o C:\Output --template default
mdexport --run report.md --format pdf --orientation landscape --fit-figures-to-page
mdexport --run report.md --embed-images --encoding shift-jis --overwrite
mdexport --help
```

出力先に同名ファイルがある場合、CLIは `--overwrite` がなければ停止します。同じ変換回で出力パスが重複する場合は、書き込み前にバッチ全体を停止します。

## テンプレート

初回起動時に実行ファイルの隣の `Template/default` を `%APPDATA%\MarkdownExporter\Template\default` へコピーします。編集したテンプレートは更新時に上書きしません。

テンプレートは `index.html` と `style.css` が必須です。`index.html` には `{{content}}` と `{{style}}` を入れ、必要なら `{{title}}` を使えます。追加の画像とフォントは `assets/` に置きます。`--template` にはAppDataのテンプレート名または絶対フォルダを渡せます。カスタムJavaScriptは使えません。

Markdown内のローカル画像はHTMLの `<文書名>_assets/content/` にコピーします。`--embed-images` では画像をData URIで埋め込みます。テンプレート素材は `<文書名>_assets/template/` に配置します。PDFのみの変換では素材フォルダを残しません。

PDF内のローカルPDFリンクには、出力PDFからリンク先への相対パスを記録します。リンク先PDFは出力先にコピーされないため、出力PDFとリンク先PDFの位置関係を維持してください。

## 開発

```powershell
dotnet build MarkdownExporter.slnx -m:1
dotnet test MarkdownExporter.slnx -m:1
dotnet publish MarkdownExporter/MarkdownExporter.csproj -c Release -r win-x64 -m:1
```

`MarkdownExporter.Tests` はNUnitでCLI解析、文字コード、事前検査、素材・テンプレート、Markdown/PDFリンク、設定保存などのロジックを検証します。WebView2の描画と画面操作はこのテストには含みません。

発行物には `mdexport.exe` と `Template/`、`WebAssets/`、`Licence/` を含めてください。サードパーティーの通知、NuGet依存の一覧とライセンスは `Licence/` にまとめています。KaTeXフォントのSIL Open Font License 1.1と著作権表示は `Licence/KaTeX-LICENSE.txt` に収録し、数式・Mermaidを含むHTMLの素材にもコピーします。`WebAssets` は `tools/web-assets/` で `npm ci` と `npm run build` により再生成できます。配布用ZIPとインストーラはこれらのファイルを同梱します。
