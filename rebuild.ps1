$dotnet = 'I:\Aion2RiftOverlay\.dotnet\dotnet.exe'
& $dotnet build 'I:\Aion2RiftOverlay\Aion2RiftOverlay.sln' -c Debug *> 'I:\Aion2RiftOverlay\.b1.log'
& $dotnet test 'I:\Aion2RiftOverlay\tests\Aion2RiftOverlay.Tests\Aion2RiftOverlay.Tests.csproj' --no-build -v normal *> 'I:\Aion2RiftOverlay\.b2.log'
& $dotnet publish 'I:\Aion2RiftOverlay\src\Aion2RiftOverlay\Aion2RiftOverlay.csproj' `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o 'I:\Aion2RiftOverlay\publish' *> 'I:\Aion2RiftOverlay\.b3.log'
$LASTEXITCODE | Out-File -Encoding ascii 'I:\Aion2RiftOverlay\.b-exit.txt'