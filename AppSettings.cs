using System;
using System.IO;
using System.Text.Json;

namespace PictureRenameApp;

public sealed class AppSettings
{
    public const int MinZeroPad = 1;
    public const int MaxZeroPad = 8;

    public bool BatchMoveToSubfolder { get; set; } = true;
    public bool UseRecycleBin { get; set; } = true;
    public bool ReopenLastFolder { get; set; } = true;
    public int DefaultZeroPad { get; set; } = 2;
    public bool ReviewDuplicatesOnOpen { get; set; }
    public string? LastFolder { get; set; }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PictureRenameApp", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Loads settings from <paramref name="path"/>; returns defaults if the file is missing or unreadable.</summary>
    public static AppSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions);
                if (loaded != null)
                {
                    loaded.DefaultZeroPad = Math.Clamp(loaded.DefaultZeroPad, MinZeroPad, MaxZeroPad);
                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
        }
        return new AppSettings();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
