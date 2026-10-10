using System;
using System.IO;
using Aion2RiftOverlay.Models;
using Aion2RiftOverlay.Services;
using Xunit;

namespace Aion2RiftOverlay.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Aion2RiftOverlayTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var service = new SettingsService(_tempDir);
        var settings = service.Load();

        Assert.Equal(85, settings.OpacityPercent);
        Assert.True(settings.AlwaysOnTop);
        Assert.False(settings.LockPosition);
        Assert.False(settings.HasWindowPosition);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var service = new SettingsService(_tempDir);
        var settings = new AppSettings
        {
            OpacityPercent = 42,
            AlwaysOnTop = false,
            LockPosition = true,
            MinimizeToTray = true,
            SettingsPanelOpen = true,
            HasWindowPosition = true,
            WindowX = 123.5,
            WindowY = -8,
        };
        service.Save(settings);

        var loaded = new SettingsService(_tempDir).Load();
        Assert.Equal(42, loaded.OpacityPercent);
        Assert.False(loaded.AlwaysOnTop);
        Assert.True(loaded.LockPosition);
        Assert.True(loaded.MinimizeToTray);
        Assert.True(loaded.SettingsPanelOpen);
        Assert.True(loaded.HasWindowPosition);
        Assert.Equal(123.5, loaded.WindowX);
        Assert.Equal(-8, loaded.WindowY);
    }

    [Fact]
    public void Load_MalformedJson_ReturnsDefaults()
    {
        File.WriteAllText(new SettingsService(_tempDir).SettingsFilePath, "{ not valid json !!");
        var settings = new SettingsService(_tempDir).Load();

        Assert.Equal(85, settings.OpacityPercent);
        Assert.True(settings.AlwaysOnTop);
    }

    [Fact]
    public void Load_OutOfRangeValues_AreSanitized()
    {
        var path = new SettingsService(_tempDir).SettingsFilePath;
        File.WriteAllText(path, """{"opacityPercent": 9999, "windowX": 50, "windowY": 60}""");

        var settings = new SettingsService(_tempDir).Load();
        Assert.Equal(100, settings.OpacityPercent);
        Assert.Equal(50, settings.WindowX);
        Assert.False(settings.HasWindowPosition);
    }

    [Fact]
    public void Load_WrongPropertyType_FallsBackToDefaults()
    {
        var path = new SettingsService(_tempDir).SettingsFilePath;
        File.WriteAllText(path, """{"windowX": "abc"}""");

        var settings = new SettingsService(_tempDir).Load();
        Assert.Equal(85, settings.OpacityPercent);
        Assert.False(settings.HasWindowPosition);
    }

    [Fact]
    public void Save_CreatesDirectoryAndIsAtomic()
    {
        var nested = Path.Combine(_tempDir, "nested", "deeper");
        var service = new SettingsService(nested);
        service.Save(new AppSettings { OpacityPercent = 30 });

        Assert.True(File.Exists(service.SettingsFilePath));
        Assert.Equal(30, service.Load().OpacityPercent);
        Assert.False(File.Exists(service.SettingsFilePath + ".tmp"));
    }
}

