using System;
using System.Collections.Generic;
using System.IO;

namespace PictureRenameApp;

public static class RenamePattern
{
    public const string NumberToken = "{n}";
    public const string DefaultSubfolder = "Renamed";

    private static readonly char[] SubfolderTrimChars = { ' ', '-', '_', '.', '(', ')', '[', ']', '#' };

    public static string Format(string pattern, int number, int pad, string ext)
    {
        var num = number.ToString(new string('0', Math.Max(1, pad)));
        var name = pattern.Contains(NumberToken) ? pattern.Replace(NumberToken, num) : pattern + num;
        return name + ext;
    }

    /// <summary>Base name of the pattern (without the number token), usable as a folder name.</summary>
    public static string SubfolderName(string pattern)
    {
        var name = pattern.Replace(NumberToken, "");
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
        name = name.Trim(SubfolderTrimChars);
        if (name.Length == 0) return DefaultSubfolder;
        // Windows cannot create directories named after devices (also with an extension, e.g. "CON.x").
        return IsReservedDeviceName(name) ? "_" + name : name;
    }

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private static bool IsReservedDeviceName(string name)
    {
        var dot = name.IndexOf('.');
        var stem = (dot < 0 ? name : name[..dot]).TrimEnd();
        return ReservedDeviceNames.Contains(stem);
    }
}
