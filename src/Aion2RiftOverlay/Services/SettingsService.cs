using System;
using System.IO;
using System.Text.Json;
using Aion2RiftOverlay.Models;

namespace Aion2RiftOverlay.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under
/// %LOCALAPPDATA%\Aion2RiftOverlay\settings.json (or a custom directory in tests).
/// Malformed/unreadable files fall back to safe defaults instead of crashing.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _directory;

    /// <param name="baseDirectory">
    /// Optional override used by unit tests. When null, the user's
    /// LocalApplicationData directory is used.
    /// </param>
    public SettingsService(string? baseDirectory = null)
    {
        _directory = baseDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Aion2RiftOverlay");
    }

    public string SettingsFilePath => Path.Combine(_directory, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
            {
                return new AppSettings();
            }

            settings.Sanitize();
            return settings;
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            JsonException or
            NotSupportedException or
            System.Security.SecurityException)
        {
            // Malformed, unreadable or partially written file: fall back to defaults.
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_directory);

            // Write to a temp file first, then atomically replace, so a crash
            // mid-write cannot leave a corrupt settings file behind.
            var tempPath = SettingsFilePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(tempPath, SettingsFilePath, overwrite: true);
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            System.Security.SecurityException)
        {
            // Saving must never crash the overlay; changes simply are not persisted.
        }
    }
}
