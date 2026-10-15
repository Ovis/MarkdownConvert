param(
    [Parameter(Mandatory = $true)][string]$DestinationDir,
    [string]$ArchivePath
)

$ErrorActionPreference = 'Stop'
$version = '12.4.2'
$expectedHash = 'db87077e683630c1217e0e8f9a20a9749d952ab676e881c3689187763a5de25d'
$downloadedArchive = $null
if (-not $ArchivePath) {
    $downloadedArchive = Join-Path ([System.IO.Path]::GetTempPath()) "mdexport-qpdf-$([guid]::NewGuid().ToString('N')).zip"
    $ArchivePath = $downloadedArchive
    Invoke-WebRequest -Uri "https://github.com/qpdf/qpdf/releases/download/v$version/qpdf-$version-msvc64.zip" -OutFile $ArchivePath
}

try {
    $actualHash = (Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "qpdf archive SHA-256 mismatch: $actualHash"
    }
    $destination = [System.IO.Path]::GetFullPath($DestinationDir)
    [System.IO.Directory]::CreateDirectory($destination) | Out-Null
    $files = @(
        'qpdf.exe', 'qpdf30.dll', 'concrt140.dll', 'msvcp140.dll', 'msvcp140_1.dll',
        'msvcp140_2.dll', 'msvcp140_atomic_wait.dll', 'msvcp140_codecvt_ids.dll',
        'vcruntime140.dll', 'vcruntime140_1.dll'
    )
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        foreach ($file in $files) {
            $entryName = "qpdf-$version-msvc64/bin/$file"
            $entry = $archive.GetEntry($entryName)
            if (-not $entry) { throw "Missing qpdf archive entry: $entryName" }
            $target = Join-Path $destination $file
            $inputStream = $entry.Open()
            $outputStream = [System.IO.File]::Create($target)
            try { $inputStream.CopyTo($outputStream) }
            finally {
                $outputStream.Dispose()
                $inputStream.Dispose()
            }
        }
    }
    finally { $archive.Dispose() }
    & (Join-Path $destination 'qpdf.exe') --version
    if ($LASTEXITCODE -ne 0) { throw 'Extracted qpdf did not start.' }
}
finally {
    if ($downloadedArchive -and (Test-Path -LiteralPath $downloadedArchive)) {
        Remove-Item -LiteralPath $downloadedArchive
    }
}
