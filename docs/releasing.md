# リリース手順 / Release guide

## 生成物

`vX.Y.Z` タグで `.github/workflows/release.yml` が走り、次の3点をGitHub Releaseの下書きへ追加します。

- `MarkdownExporter-X.Y.Z-win-x64.zip`: 自己完結型の `mdexport.exe`、標準テンプレート、描画素材、ライセンス文書、README（日英）。WebView2 Runtimeは含まない。
- `MarkdownExporter-X.Y.Z-win-x64-setup.exe`: Inno Setup 6.7.3製の現在のユーザー向けインストーラ。WebView2 Runtimeがないときだけ、同梱したMicrosoft製Evergreen Bootstrapperを実行する。
- `SHA256SUMS.txt`: 上記2ファイルのSHA-256。

ワークフローはMicrosoftのBootstrapperとInno Setupを公式配布元から取得し、発行元のAuthenticode署名を確認してから使う。ZIPは `tools/package-release.ps1`、インストーラは `installer/MarkdownExporter.iss` で構成する。

## 公開前の確認

1. CIのビルド・NUnit・フォーマット検証が通ることを確認する。
2. 下書きからZIPとインストーラを取得し、チェックサムを照合する。
3. Windows 11 24H2以降でGUI・CLIのHTML/PDF変換、ZIPからの起動、インストール・更新・アンインストールを手動確認する。
4. WebView2 Runtimeのある環境では再導入されないこと、ない環境ではインストーラが導入することを隔離したテスト環境で確認する。ZIPではGUIの案内とCLIのエラーを確認する。
5. 問題がなければ下書きReleaseを公開する。

WebView2 Bootstrapperは実行時にネットワークからRuntime本体を取得する。オフラインの新規インストールには対応しない。インストーラはアプリの設定と `%APPDATA%\MarkdownExporter\Template` をアンインストール時に消さない。

## WinGet

公開済みReleaseのインストーラURLとSHA-256が確定した後に、WinGetマニフェストを `microsoft/winget-pkgs` に提出する。パッケージIDは提出時に確定する。インストーラは `InstallerType: inno`、`Scope: user`、`Architecture: x64`、`MinimumOSVersion: 10.0.26100.0` とし、`Dependencies.PackageDependencies` に `Microsoft.EdgeWebView2Runtime` を宣言する。マニフェストの依存関係をスキップした導入でも、インストーラ自身がRuntimeを確認する。

提出前に `winget validate` とWinGet経由のクリーンインストール・更新・アンインストールを確認する。WinGet公開と審査通過は、このリポジトリのRelease生成だけでは完了しない。
