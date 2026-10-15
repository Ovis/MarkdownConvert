param(
    [Parameter(Mandatory = $true)][string]$Version,
    [Parameter(Mandatory = $true)][string]$PublishDir,
    [Parameter(Mandatory = $true)][string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$publish = (Resolve-Path -LiteralPath $PublishDir).Path
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDir).Path
$stage = Join-Path $output "MarkdownExporter-$Version-win-x64"
if (Test-Path -LiteralPath $stage) {
    throw "Staging directory already exists: $stage"
}
New-Item -ItemType Directory -Path $stage | Out-Null

Copy-Item -LiteralPath (Join-Path $publish 'mdexport.exe') -Destination $stage
foreach ($folder in @('Template', 'WebAssets', 'Licence', 'Tools')) {
    Copy-Item -LiteralPath (Join-Path $publish $folder) -Destination $stage -Recurse
}
foreach ($file in @('README.md', 'README.en.md')) {
    Copy-Item -LiteralPath (Join-Path $repository $file) -Destination $stage
}

$zip = Join-Path $output "MarkdownExporter-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
Remove-Item -LiteralPath $stage -Recurse
Write-Output $zip
