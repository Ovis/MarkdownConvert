# Release guide

## Artifacts

Pushing a `vX.Y.Z` tag runs `.github/workflows/release.yml` and adds three files to a draft GitHub Release:

- `MarkdownExporter-X.Y.Z-win-x64.zip`: The self-contained `mdexport.exe`, default template, rendering assets, license notices, and Japanese and English READMEs. It does not bundle WebView2 Runtime.
- `MarkdownExporter-X.Y.Z-win-x64-setup.exe`: A per-user installer built with Inno Setup 6.7.3. If WebView2 Runtime is absent, it runs the bundled Microsoft Evergreen Bootstrapper.
- `SHA256SUMS.txt`: SHA-256 checksums for the ZIP and installer.

The workflow downloads the WebView2 Bootstrapper and Inno Setup from their official sources and checks their Authenticode publisher signatures. `tools/package-release.ps1` assembles the ZIP; `installer/MarkdownExporter.iss` defines the installer.

## Before publishing

1. Confirm the build, NUnit tests, and formatting check pass.
2. Download the draft assets and verify their checksums.
3. On Windows 11 24H2 or later, manually check HTML/PDF conversion in both GUI and CLI, launching from the ZIP, and installer installation, upgrade, and uninstallation.
4. In an isolated test environment without WebView2 Runtime, verify that the installer installs it. In a separate environment with Runtime installed, verify that it is not reinstalled. Check the ZIP's GUI prompt and CLI error without Runtime.
5. Publish the draft Release after these checks pass.

The WebView2 Bootstrapper downloads the Runtime from the network. First-time offline installation is not supported. Uninstallation leaves application settings and `%APPDATA%\MarkdownExporter\Template` intact.

## WinGet

After the Release is public and the installer URL and SHA-256 are fixed, submit a WinGet manifest to `microsoft/winget-pkgs`. Choose its package ID when preparing the submission. Declare `InstallerType: inno`, `Scope: user`, `Architecture: x64`, and `MinimumOSVersion: 10.0.26100.0`. Add `Microsoft.EdgeWebView2Runtime` under `Dependencies.PackageDependencies`. The installer checks for Runtime itself even if WinGet dependencies are skipped.

Before submission, run `winget validate` and test clean installation, upgrade, and uninstallation through WinGet. Generating this repository's Release does not itself publish or validate the WinGet package.
