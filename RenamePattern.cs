using System;
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
        return name.Length == 0 ? DefaultSubfolder : name;
    }
}
