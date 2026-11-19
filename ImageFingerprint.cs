using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace PictureRenameApp;

public readonly record struct ImageFingerprint(ulong? Hash, int Width, int Height)
{
    /// <summary>Decodes the image and computes its <see cref="PerceptualHash"/>; returns null if the file can't be read as an image.</summary>
    public static ImageFingerprint? TryCompute(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            using var img = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
            using var small = new Bitmap(PerceptualHash.Width, PerceptualHash.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(img, new Rectangle(0, 0, small.Width, small.Height));
            }

            var lum = new byte[PerceptualHash.Width * PerceptualHash.Height];
            for (int y = 0; y < small.Height; y++)
                for (int x = 0; x < small.Width; x++)
                {
                    var c = small.GetPixel(x, y);
                    lum[y * small.Width + x] = (byte)((c.R * 299 + c.G * 587 + c.B * 114) / 1000);
                }
            return new ImageFingerprint(PerceptualHash.FromLuminance(lum), img.Width, img.Height);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException or ExternalException)
        {
            return null;
        }
    }
}
