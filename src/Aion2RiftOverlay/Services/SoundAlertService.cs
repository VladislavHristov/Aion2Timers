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
///
/// Sound file resolution priority (highest first, see MainViewModel.ResolveSoundFile):
///   1. Path configured in settings.json
///   2. <see cref="OverridesDirectory"/> - a "sounds" folder next to the exe
///      (lets anyone swap voices without recompiling)
///   3. <see cref="SoundsDirectory"/> - embedded defaults extracted to
///      %LOCALAPPDATA%\Aion2RiftOverlay\sounds (always re-extracted on startup,
///      so repo-built exes always play the sounds bundled in the build)
/// </summary>
public sealed class SoundAlertService
{
    /// <summary>App-managed default sounds, extracted from embedded resources.</summary>
    public static readonly string SoundsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Aion2RiftOverlay", "sounds");

    /// <summary>User overrides: a "sounds" folder next to the exe (best-effort).</summary>
    public static string OverridesDirectory => Path.Combine(AppContext.BaseDirectory, "sounds");

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
    /// Creates the user-override folder (best effort, so it is easy to discover)
    /// and writes the embedded MP3 files to <see cref="SoundsDirectory"/>,
    /// overwriting previous extractions. Safe to call on every startup; never throws.
    /// </summary>
    public void ExtractDefaultSounds()
    {
        try
        {
            Directory.CreateDirectory(SoundsDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return;
        }

        try
        {
            // Discoverable "sounds" folder next to the exe for user overrides.
            Directory.CreateDirectory(OverridesDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Read-only install location: overrides folder is simply unavailable.
        }

        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var fileName in EmbeddedSoundResources)
            {
                var target = Path.Combine(SoundsDirectory, fileName);

                // Always overwrite: the embedded sounds belong to the build that
                // is running, so a repo user who swapped Sounds\*.mp3 and rebuilt
                // hears the new files, not stale extractions.
                using var stream = assembly.GetManifestResourceStream($"Aion2RiftOverlay.Sounds.{fileName}");
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