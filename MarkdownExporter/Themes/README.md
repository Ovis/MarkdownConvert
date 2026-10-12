# MarkdownExporterのWPF画面テーマ

このフォルダはアプリ画面用の共通テーマであり、Markdownから出力するHTMLのテンプレートとは別物。

- `Palette.Light.xaml`と`Palette.Dark.xaml`は同じ役割名のBrushキーを持つ。色変更はここで行う。
- `Metrics.xaml`は余白、角丸、コントロール高さの共通値を持つ。
- `Controls.xaml`はボタン、テキスト入力、非編集型の選択欄、チェック・ラジオボタン、DataGridとスクロールバー、パネル、ドロップ領域、文字状態のスタイルを持つ。各画面では色コードを直接指定せず、これらのスタイルとリソースを参照する。
- `AppThemeManager.cs`は配色辞書だけを差し替える。画面テーマの選択肢は「システムに合わせる・ライト・ダーク」の3つ。`System`選択時はWindowsのアプリ配色設定を参照し、その変更通知にも追従する。未設定時は`System`とし、GUIの設定ファイルに保存する。

製品WPFプロジェクトは、これらのXAMLを`Page`として含め、`App.xaml`で`Metrics.xaml`と`Controls.xaml`をマージする。起動時に`AppThemeManager.SetPreference`を呼んで配色辞書を挿入し、各Windowに`UiStyle.Window`を指定する。既定設定などのダイアログも同じ方法でテーマを適用する。
