# Publishes Aion2RiftOverlay as a self-contained, single-file Windows x64 executable.
# The result does not require the .NET Desktop Runtime on the target machine.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Join-Path $root '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' } # fall back to a system-wide SDK

$project = Join-Path $root 'src\Aion2RiftOverlay\Aion2RiftOverlay.csproj'
$output = Join-Path $root 'publish'

& $dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o $output

Write-Host ''
Write-Host "Published executable: $(Join-Path $output 'Aion2RiftOverlay.exe')"
