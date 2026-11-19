using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureRenameApp;

/// <summary>
/// Step-by-step review of duplicate groups. Choices are only recorded; nothing is deleted until the caller
/// acts on <see cref="Decisions"/>, so <see cref="Back"/> can change any earlier choice.
/// </summary>
public sealed class DuplicateReviewSession
{
    private readonly string?[] _keep;

    public DuplicateReviewSession(IReadOnlyList<DuplicateGroup> groups)
    {
        Groups = groups;
        _keep = new string?[groups.Count];
    }

    public IReadOnlyList<DuplicateGroup> Groups { get; }
    public int Current { get; private set; }
    public bool IsFinished => Current >= Groups.Count;
    public bool CanGoBack => Current > 0;
    public DuplicateGroup? CurrentGroup => IsFinished ? null : Groups[Current];

    /// <summary>The file chosen earlier for <paramref name="group"/>, or null if none was chosen (or it was skipped).</summary>
    public string? ChoiceFor(int group) => _keep[group];

    public void Keep(string path)
    {
        var group = CurrentGroup ?? throw new InvalidOperationException("Review is finished.");
        if (!group.Files.Any(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("File is not in the current group.", nameof(path));
        _keep[Current++] = path;
    }

    /// <summary>Keeps every file in the current group.</summary>
    public void Skip()
    {
        if (IsFinished) throw new InvalidOperationException("Review is finished.");
        _keep[Current++] = null;
    }

    public void Back()
    {
        if (CanGoBack) Current--;
    }

    /// <summary>Groups where one file was chosen: that file and the others to delete.</summary>
    public IReadOnlyList<(DuplicateCandidate Keep, IReadOnlyList<DuplicateCandidate> Remove)> Decisions =>
        Groups.Select((g, i) => (Group: g, Keep: _keep[i]))
            .Where(x => x.Keep != null)
            .Select(x => (
                x.Group.Files.First(f => string.Equals(f.Path, x.Keep, StringComparison.OrdinalIgnoreCase)),
                (IReadOnlyList<DuplicateCandidate>)x.Group.Files
                    .Where(f => !string.Equals(f.Path, x.Keep, StringComparison.OrdinalIgnoreCase)).ToList()))
            .ToList();

    public int FilesToDelete => Decisions.Sum(d => d.Remove.Count);
    public long BytesToDelete => Decisions.Sum(d => d.Remove.Sum(f => f.Length));
    public int GroupsLeftAlone => Groups.Count - Decisions.Count;
}
