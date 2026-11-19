using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace PictureRenameApp;

/// <summary>Per-file values available to rename pattern tokens.</summary>
public sealed record RenameItem(string OriginalName, string Extension, DateTime Date);

public static class RenamePattern
{
    public const string NumberToken = "{n}";
    public const string DefaultSubfolder = "Renamed";
    public const string DefaultDateFormat = "yyyy-MM-dd";

    private static readonly char[] SubfolderTrimChars = { ' ', '-', '_', '.', '(', ')', '[', ']', '#' };
    private static readonly Regex AnyToken = new(@"\{[^{}]*\}", RegexOptions.Compiled);
    private static readonly Regex Token = new(@"\{(?:(?<n>n)|(?<name>name)|date(?::(?<fmt>[^{}]*))?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Expands <c>{n}</c> (zero-padded number), <c>{name}</c> (original file name without extension) and
    /// <c>{date}</c> / <c>{date:format}</c>. If the pattern has no <c>{n}</c>, the number is appended.
    /// </summary>
    public static string Format(string pattern, int number, int pad, RenameItem item)
    {
        var num = number.ToString(new string('0', Math.Max(1, pad)));
        // Single pass so text inserted by {name} is never expanded again.
        var name = Token.Replace(pattern, m =>
            m.Groups["n"].Success ? (m.Value == NumberToken ? num : m.Value)
            : m.Groups["name"].Success ? item.OriginalName
            : FormatDate(item.Date, m.Groups["fmt"].Value));
        if (!pattern.Contains(NumberToken)) name += num;
        return name + item.Extension;
    }

    public static string Format(string pattern, int number, int pad, string ext) =>
        Format(pattern, number, pad, new RenameItem("", ext, DateTime.MinValue));

    /// <summary>Base name of the pattern (without tokens), usable as a folder name.</summary>
    public static string SubfolderName(string pattern)
    {
        var name = AnyToken.Replace(pattern, "");
        name = RemoveInvalidChars(name, "");
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

    private static string FormatDate(DateTime date, string format)
    {
        string text;
        try { text = date.ToString(format.Length == 0 ? DefaultDateFormat : format, CultureInfo.InvariantCulture); }
        catch (FormatException) { text = date.ToString(DefaultDateFormat, CultureInfo.InvariantCulture); }
        // Characters like ':' or '/' in a date format would break the file name.
        return RemoveInvalidChars(text.Replace(':', '-').Replace('/', '-'), "-");
    }

    private static string RemoveInvalidChars(string s, string replacement)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), replacement);
        return s;
    }
}
