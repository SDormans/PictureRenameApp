using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PictureRenameApp;

public sealed record ImageMetadata(int Width, int Height, DateTime? DateTaken, string? Camera)
{
    public static ImageMetadata FromImage(Image image)
    {
        var ids = image.PropertyIdList;
        byte[]? Value(int id) => ids.Contains(id) ? image.GetPropertyItem(id)?.Value : null;

        var taken = ExifParser.ParseDate(Value(ExifParser.TagDateTimeOriginal)) ?? ExifParser.ParseDate(Value(ExifParser.TagDateTime));
        var camera = ExifParser.CameraName(ExifParser.ParseAscii(Value(ExifParser.TagMake)), ExifParser.ParseAscii(Value(ExifParser.TagModel)));
        return new ImageMetadata(image.Width, image.Height, taken, camera);
    }

    /// <summary>Reads metadata without decoding the pixel data; returns null if the file is not a readable image.</summary>
    public static ImageMetadata? TryRead(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            using var img = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
            return FromImage(img);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
        {
            return null;
        }
    }
}
