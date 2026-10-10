using Aion2RiftOverlay.ViewModels;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Tests the sound-file resolution priority:
/// settings.json path &gt; "sounds" folder next to the exe &gt; extracted embedded defaults.
/// Uses temporary directories as the override/fallback folders.
/// </summary>
public class SoundResolutionTests : IDisposable
{
    private readonly string _tempRoot =
        Path.Combine(Path.GetTempPath(), "Aion2RiftOverlayTests", Guid.NewGuid().ToString("N"));

    private string OverrideDir => Path.Combine(_tempRoot, "override");
    private string FallbackDir => Path.Combine(_tempRoot, "fallback");

    public SoundResolutionTests()
    {
        Directory.CreateDirectory(OverrideDir);
        Directory.CreateDirectory(FallbackDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    [Fact]
    public void ConfiguredPath_Wins_WhenFileExists()
    {
        File.WriteAllText(Path.Combine(OverrideDir, "OpenNow.mp3"), "override");
        File.WriteAllText(Path.Combine(FallbackDir, "OpenNow.mp3"), "fallback");
        var configured = Path.Combine(_tempRoot, "custom.mp3");
        File.WriteAllText(configured, "custom");

        var resolved = MainViewModel.ResolveSoundFile(configured, "OpenNow.mp3", OverrideDir, FallbackDir);

        Assert.Equal(configured, resolved);
    }

    [Fact]
    public void ExeAdjacentSoundsFolder_Wins_WhenNoConfiguredFile()
    {
        File.WriteAllText(Path.Combine(OverrideDir, "10minutes.mp3"), "override");
        File.WriteAllText(Path.Combine(FallbackDir, "10minutes.mp3"), "fallback");

        var resolved = MainViewModel.ResolveSoundFile("", "10minutes.mp3", OverrideDir, FallbackDir);

        Assert.Equal(Path.Combine(OverrideDir, "10minutes.mp3"), resolved);
    }

    [Fact]
    public void ExeAdjacentSoundsFolder_BeatsExtractedDefaults()
    {
        // Only the extracted default exists in the override folder check order:
        // override dir empty, fallback has the file.
        File.WriteAllText(Path.Combine(FallbackDir, "OpenNow.mp3"), "fallback");

        var resolved = MainViewModel.ResolveSoundFile(null, "OpenNow.mp3", OverrideDir, FallbackDir);

        Assert.Equal(Path.Combine(FallbackDir, "OpenNow.mp3"), resolved);
    }

    [Fact]
    public void NonExistingConfiguredPath_IsSkipped()
    {
        File.WriteAllText(Path.Combine(FallbackDir, "OpenNow.mp3"), "fallback");

        var resolved = MainViewModel.ResolveSoundFile(
            Path.Combine(_tempRoot, "gone.mp3"), "OpenNow.mp3", OverrideDir, FallbackDir);

        Assert.Equal(Path.Combine(FallbackDir, "OpenNow.mp3"), resolved);
    }

    [Fact]
    public void ReturnsEmpty_WhenNothingExists()
    {
        var resolved = MainViewModel.ResolveSoundFile("", "OpenNow.mp3", OverrideDir, FallbackDir);

        Assert.Equal(string.Empty, resolved);
    }
}