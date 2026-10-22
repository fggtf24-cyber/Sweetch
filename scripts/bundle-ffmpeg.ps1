param(
    [Parameter(Mandatory = $true)][string]$Destination,
    [string]$ArchivePath
)
$ErrorActionPreference = 'Stop'
# Gyan's "essentials" build, the same one the app downloads on demand; pinned so builds are reproducible.
$version = '9.0.2'
$expected = '60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba'
$url = "https://github.com/GyanD/codexffmpeg/releases/download/$version/ffmpeg-$version-essentials_build.zip"
$downloaded = [string]::IsNullOrWhiteSpace($ArchivePath)
if ($downloaded) {
    $ArchivePath = Join-Path ([System.IO.Path]::GetTempPath()) ("sweetch-ffmpeg-" + [guid]::NewGuid() + '.zip')
}
try {
    if ($downloaded) { Invoke-WebRequest $url -OutFile $ArchivePath }
    if ((Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) {
        throw 'FFmpeg archive checksum does not match the official build.'
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $Destination = [System.IO.Path]::GetFullPath($Destination)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead([System.IO.Path]::GetFullPath($ArchivePath))
    try {
        # FFmpeg is GPLv3 in this build: ship its licence and README (which names the source) next to it.
        foreach ($pair in @(@('bin/ffmpeg.exe', 'ffmpeg.exe'), @('LICENSE', 'FFMPEG-LICENSE.txt'), @('README.txt', 'FFMPEG-README.txt'))) {
            $entry = $archive.GetEntry("ffmpeg-$version-essentials_build/$($pair[0])")
            if ($null -eq $entry) { throw "FFmpeg archive is missing $($pair[0])" }
            $target = Join-Path $Destination $pair[1]
            $part = $target + '.part'
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $part, $true)
            Move-Item -LiteralPath $part -Destination $target -Force
        }
    }
    finally { $archive.Dispose() }
    "FFmpeg $version bundled and SHA-256 verified."
}
finally {
    if ($downloaded -and (Test-Path -LiteralPath $ArchivePath)) { Remove-Item -LiteralPath $ArchivePath -Force }
}
