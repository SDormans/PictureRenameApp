using System;
using System.Globalization;
using System.Text;

namespace PictureRenameApp;

/// <summary>Decodes raw EXIF property values (as exposed by GDI+ <c>PropertyItem.Value</c>).</summary>
public static class ExifParser
{
    public const int TagMake = 0x010F;
    public const int TagModel = 0x0110;
    public const int TagDateTime = 0x0132;
    public const int TagDateTimeOriginal = 0x9003;

    public static string? ParseAscii(byte[]? value)
    {
        if (value == null || value.Length == 0) return null;
        var s = Encoding.ASCII.GetString(value).TrimEnd('\0').Trim();
        return s.Length == 0 ? null : s;
    }

    /// <summary>Parses the EXIF date format <c>yyyy:MM:dd HH:mm:ss</c>.</summary>
    public static DateTime? ParseDate(byte[]? value)
    {
        var s = ParseAscii(value);
        if (s != null && DateTime.TryParseExact(s, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        return null;
    }

    /// <summary>Joins make and model, dropping the make when the model already starts with it (e.g. "Canon Canon EOS R6").</summary>
    public static string? CameraName(string? make, string? model)
    {
        if (string.IsNullOrEmpty(model)) return string.IsNullOrEmpty(make) ? null : make;
        if (string.IsNullOrEmpty(make) || model.StartsWith(make, StringComparison.OrdinalIgnoreCase)) return model;
        return $"{make} {model}";
    }
}
