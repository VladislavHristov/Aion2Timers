using Aion2RiftOverlay.Services;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Verifies the voice-alert sound files are embedded in the application
/// assembly, so a published single-file exe is fully self-contained.
/// </summary>
public class EmbeddedSoundsTests
{
    [Fact]
    public void AlertSounds_AreEmbeddedInTheApplication()
    {
        var resourceNames = typeof(Aion2RiftOverlay.App).Assembly.GetManifestResourceNames();

        Assert.Contains("Aion2RiftOverlay.Sounds.10minutes.mp3", resourceNames);
        Assert.Contains("Aion2RiftOverlay.Sounds.OpenNow.mp3", resourceNames);
    }

    [Fact]
    public void ExtractDefaultSounds_CreatesFilesInLocalAppData()
    {
        var service = new SoundAlertService();
        service.ExtractDefaultSounds();

        Assert.True(File.Exists(Path.Combine(SoundAlertService.SoundsDirectory, "10minutes.mp3")));
        Assert.True(File.Exists(Path.Combine(SoundAlertService.SoundsDirectory, "OpenNow.mp3")));
    }
}