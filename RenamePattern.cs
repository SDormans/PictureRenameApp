using System;
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
    private static readonly Regex DateToken = new(@"\{date(?::(?<fmt>[^{}]*))?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Expands <c>{n}</c> (zero-padded number), <c>{name}</c> (original file name without extension) and
    /// <c>{date}</c> / <c>{date:format}</c>. If the pattern has no <c>{n}</c>, the number is appended.
    /// </summary>
    public static string Format(string pattern, int number, int pad, RenameItem item)
    {
        var num = number.ToString(new string('0', Math.Max(1, pad)));
        var name = pattern.Contains(NumberToken) ? pattern.Replace(NumberToken, num) : pattern + num;
        name = name.Replace("{name}", item.OriginalName, StringComparison.OrdinalIgnoreCase);
        name = DateToken.Replace(name, m => FormatDate(item.Date, m.Groups["fmt"].Value));
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
        return name.Length == 0 ? DefaultSubfolder : name;
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
