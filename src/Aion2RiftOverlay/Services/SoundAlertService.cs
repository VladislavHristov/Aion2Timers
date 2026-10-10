using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace Aion2RiftOverlay.Services;

/// <summary>
/// Fire-and-forget voice/sound alert playback using WPF MediaPlayer
/// (supports WAV and MP3). Missing or unreadable files are ignored silently;
/// playback must never crash the overlay.
/// Also extracts the embedded default alert sounds to the user's local
/// application-data folder on first run, so a single published exe carries
/// everything needed for voice alerts.
/// </summary>
public sealed class SoundAlertService
{
    public static readonly string SoundsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Aion2RiftOverlay", "sounds");

    private static readonly string[] EmbeddedSoundResources =
    {
        "10minutes.mp3",
        "OpenNow.mp3",
    };

    // MediaPlayer is garbage-collected while playing unless referenced,
    // so active players are kept until playback finishes.
    private readonly List<MediaPlayer> _activePlayers = new();
    private readonly object _gate = new();

    /// <summary>
    /// Writes the embedded MP3 files to <see cref="SoundsDirectory"/> unless they
    /// already exist. Safe to call on every startup. Never throws.
    /// </summary>
    public void ExtractDefaultSounds()
    {
        try
        {
            Directory.CreateDirectory(SoundsDirectory);
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var fileName in EmbeddedSoundResources)
            {
                var target = Path.Combine(SoundsDirectory, fileName);
                if (File.Exists(target))
                {
                    continue;
                }

                var resourceName = $"Aion2RiftOverlay.Sounds.{fileName}";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    continue; // resource not embedded; alerts simply stay silent
                }

                using var output = File.Create(target);
                stream.CopyTo(output);
            }
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            System.Security.SecurityException)
        {
            // Extraction is best-effort; alerts fall back to silence.
        }
    }

    /// <param name="filePath">Absolute path to a WAV or MP3 file.</param>
    public void Play(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        try
        {
            var player = new MediaPlayer();
            player.MediaEnded += (_, _) => Release(player);
            player.MediaFailed += (_, _) => Release(player);
            player.Open(new Uri(filePath, UriKind.Absolute));

            lock (_gate)
            {
                _activePlayers.Add(player);
            }

            player.Play();
        }
        catch (Exception ex) when (
            ex is IOException or
            UriFormatException or
            InvalidOperationException or
            NotSupportedException)
        {
            // Sound alerts are best-effort only.
        }
    }

    private void Release(MediaPlayer player)
    {
        lock (_gate)
        {
            _activePlayers.Remove(player);
        }

        try
        {
            player.Close();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}