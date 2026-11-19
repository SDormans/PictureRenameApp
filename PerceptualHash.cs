using System;
using System.Numerics;

namespace PictureRenameApp;

/// <summary>
/// 64-bit difference hash (dHash): the image is shrunk to 9×8 grayscale and each bit says whether a pixel is
/// brighter than its right neighbour. Resized, re-saved or recompressed copies give (nearly) the same hash.
/// </summary>
public static class PerceptualHash
{
    public const int Width = 9;
    public const int Height = 8;

    /// <summary>Images whose 9×8 thumbnail varies less than this are too flat to compare reliably.</summary>
    public const int MinContrast = 8;

    /// <param name="luminance">Row-major 9×8 grayscale values.</param>
    /// <returns>The hash, or null when the image has too little detail.</returns>
    public static ulong? FromLuminance(ReadOnlySpan<byte> luminance)
    {
        if (luminance.Length != Width * Height)
            throw new ArgumentException($"Expected {Width * Height} values.", nameof(luminance));

        byte min = byte.MaxValue, max = byte.MinValue;
        foreach (var v in luminance)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
        if (max - min < MinContrast) return null;

        ulong hash = 0;
        int bit = 0;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width - 1; x++, bit++)
                if (luminance[y * Width + x] > luminance[y * Width + x + 1])
                    hash |= 1UL << bit;
        return hash;
    }

    public static int Distance(ulong a, ulong b) => BitOperations.PopCount(a ^ b);
}
