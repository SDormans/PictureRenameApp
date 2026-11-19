using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureRenameApp;

/// <summary>Records the moves of one rename (single or batch) so it can be undone.</summary>
public sealed class RenameOperation
{
    private readonly List<(string From, string To)> _moves = new();
    private readonly List<string> _createdDirectories = new();

    public IReadOnlyList<(string From, string To)> Moves => _moves;
    public bool IsEmpty => _moves.Count == 0;

    public void AddMove(string from, string to) => _moves.Add((from, to));

    /// <summary>Creates <paramref name="dir"/> if needed and remembers it so undo can remove it again.</summary>
    public void EnsureDirectory(string dir)
    {
        if (Directory.Exists(dir)) return;
        Directory.CreateDirectory(dir);
        _createdDirectories.Add(dir);
    }

    /// <summary>
    /// Moves files back in reverse order. A file is skipped (and counted as failed) if it no longer exists
    /// or its original name is taken. Directories created by the operation are removed when empty.
    /// </summary>
    public UndoResult Undo()
    {
        var restored = new List<(string From, string To)>();
        int failed = 0;
        for (int i = _moves.Count - 1; i >= 0; i--)
        {
            var (from, to) = _moves[i];
            try
            {
                if (!File.Exists(to) || (File.Exists(from) && !string.Equals(from, to, StringComparison.OrdinalIgnoreCase)))
                {
                    failed++;
                    continue;
                }
                File.Move(to, from);
                restored.Add((from, to));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failed++;
            }
        }

        foreach (var dir in _createdDirectories.AsEnumerable().Reverse())
        {
            try
            {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        return new UndoResult(restored, failed);
    }
}

public sealed record UndoResult(IReadOnlyList<(string From, string To)> Restored, int Failed);
