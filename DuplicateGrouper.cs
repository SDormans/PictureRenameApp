using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureRenameApp;

/// <param name="ContentHash">Hash of the file bytes, or null if it was not hashed.</param>
/// <param name="VisualHash">Perceptual hash, or null for non-images and images too flat to compare.</param>
public sealed record DuplicateCandidate(
    string Path, long Length, DateTime Modified, string? ContentHash, ulong? VisualHash, int Width, int Height)
{
    public long Pixels => (long)Width * Height;
}

public enum DuplicateKind { Exact, LookAlike }

/// <summary>A set of identical or similar files; <see cref="Files"/> starts with the suggested file to keep.</summary>
public sealed record DuplicateGroup(IReadOnlyList<DuplicateCandidate> Files, DuplicateKind Kind)
{
    public DuplicateCandidate Best => Files[0];
}

public static class DuplicateGrouper
{
    /// <summary>Maximum number of differing hash bits (out of 64) for two pictures to count as look-alikes.</summary>
    public const int DefaultMaxDistance = 8;

    /// <summary>
    /// Groups files with the same content hash, or whose visual hashes differ by at most <paramref name="maxDistance"/> bits.
    /// Matching is transitive, so every file ends up in at most one group. Exact groups come first.
    /// </summary>
    public static List<DuplicateGroup> Group(IReadOnlyList<DuplicateCandidate> items, int maxDistance = DefaultMaxDistance)
    {
        var parent = Enumerable.Range(0, items.Count).ToArray();
        int Find(int i)
        {
            while (parent[i] != i) i = parent[i] = parent[parent[i]];
            return i;
        }
        void Union(int a, int b) => parent[Find(a)] = Find(b);

        var byContent = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].ContentHash is not { } h) continue;
            if (byContent.TryGetValue(h, out var first)) Union(i, first);
            else byContent[h] = i;
        }

        var visual = Enumerable.Range(0, items.Count).Where(i => items[i].VisualHash.HasValue).ToArray();
        for (int a = 0; a < visual.Length; a++)
        {
            var ha = items[visual[a]].VisualHash!.Value;
            for (int b = a + 1; b < visual.Length; b++)
                if (PerceptualHash.Distance(ha, items[visual[b]].VisualHash!.Value) <= maxDistance)
                    Union(visual[a], visual[b]);
        }

        return Enumerable.Range(0, items.Count)
            .GroupBy(Find)
            .Where(g => g.Count() > 1)
            .Select(g =>
            {
                var files = OrderByPreference(g.Select(i => items[i])).ToList();
                var exact = files.All(f => f.ContentHash != null && f.ContentHash == files[0].ContentHash);
                return new DuplicateGroup(files, exact ? DuplicateKind.Exact : DuplicateKind.LookAlike);
            })
            .OrderBy(g => g.Kind)
            .ThenBy(g => System.IO.Path.GetFileName(g.Best.Path), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Highest resolution, then largest file, then shortest name (avoids "copy" / "(2)" names), then oldest.</summary>
    public static IEnumerable<DuplicateCandidate> OrderByPreference(IEnumerable<DuplicateCandidate> files) =>
        files.OrderByDescending(f => f.Pixels)
            .ThenByDescending(f => f.Length)
            .ThenBy(f => Path.GetFileNameWithoutExtension(f.Path).Length)
            .ThenBy(f => f.Modified)
            .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase);
}
