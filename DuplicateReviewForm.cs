using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureRenameApp;

/// <summary>
/// Scans for duplicates, then shows one group at a time so the user can pick the file to keep.
/// Nothing is deleted here: when the user confirms on the summary page the dialog returns OK and
/// <see cref="Decisions"/> holds what to delete.
/// </summary>
public sealed class DuplicateReviewForm : Form
{
    public delegate Task<IReadOnlyList<DuplicateGroup>> ScanFunc(IProgress<(int Done, int Total)> progress, CancellationToken ct);

    private enum Phase { Scanning, Reviewing, Summary }

    private static readonly Size TileSize = new(240, 270);
    private static readonly Size PictureSize = new(228, 180);
    private static readonly Font CaptionFont = new("Segoe UI", 8.5f);

    private readonly ScanFunc _scan;
    private readonly string _deleteOutcome;
    private readonly CancellationTokenSource _cts = new();
    private DuplicateReviewSession? _session;
    private Phase _phase;
    private string? _selected;
    private int _loadGeneration;
    private readonly List<Tile> _tiles = new();

    private readonly Label _header = new();
    private readonly Label _hint = new();
    private readonly FlowLayoutPanel _tilePanel = new();
    private readonly Panel _scanPanel = new();
    private readonly ProgressBar _bar = new();
    private readonly Label _scanText = new();
    private readonly Label _summary = new();
    private readonly Button _backBtn = new();
    private readonly Button _explorerBtn = new();
    private readonly Button _skipBtn = new();
    private readonly Button _keepBtn = new();
    private readonly Button _stopBtn = new();
    private readonly Button _deleteBtn = new();
    private readonly Button _closeBtn = new();

    /// <param name="deleteOutcome">What happens to deleted files, e.g. "moved to the Recycle Bin".</param>
    public DuplicateReviewForm(ScanFunc scan, string deleteOutcome)
    {
        _scan = scan;
        _deleteOutcome = deleteOutcome;

        Text = "Review duplicates";
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Bg; ForeColor = Theme.Fg;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(560, 420);
        KeyPreview = true;

        _header.Dock = DockStyle.Top; _header.Height = 36;
        _header.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        _header.Padding = new Padding(12, 8, 12, 0);
        _hint.Dock = DockStyle.Top; _hint.Height = 26;
        _hint.Padding = new Padding(12, 0, 12, 4);
        _hint.ForeColor = Theme.Muted;

        _tilePanel.Dock = DockStyle.Fill;
        _tilePanel.AutoScroll = true;
        _tilePanel.Padding = new Padding(8);
        _tilePanel.BackColor = Theme.Bg;

        _scanPanel.Dock = DockStyle.Fill;
        _scanPanel.Padding = new Padding(40, 120, 40, 0);
        _bar.Dock = DockStyle.Top; _bar.Height = 22; _bar.Style = ProgressBarStyle.Continuous;
        _scanText.Dock = DockStyle.Top; _scanText.Height = 32; _scanText.ForeColor = Theme.Muted;
        _scanText.TextAlign = ContentAlignment.MiddleCenter;
        _scanPanel.Controls.Add(_bar);
        _scanPanel.Controls.Add(_scanText);

        _summary.Dock = DockStyle.Fill;
        _summary.Padding = new Padding(24);
        _summary.Font = new Font("Segoe UI", 11f);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Panel,
            Padding = new Padding(8), WrapContents = false, FlowDirection = FlowDirection.RightToLeft,
        };
        StyleBtn(_keepBtn, "Keep selected", true);
        StyleBtn(_skipBtn, "Keep all (skip)", false);
        StyleBtn(_backBtn, "◀ Back", false);
        StyleBtn(_explorerBtn, "Show in Explorer", false);
        StyleBtn(_stopBtn, "Stop", false);
        StyleBtn(_deleteBtn, "Delete", false); _deleteBtn.BackColor = Theme.Danger;
        StyleBtn(_closeBtn, "Close", false);
        buttons.Controls.AddRange(new Control[] { _closeBtn, _deleteBtn, _keepBtn, _skipBtn, _backBtn, _explorerBtn, _stopBtn });

        _keepBtn.Click += (_, _) => KeepSelected();
        _skipBtn.Click += (_, _) => { _session?.Skip(); ShowCurrent(); };
        _backBtn.Click += (_, _) => GoBack();
        _explorerBtn.Click += (_, _) => { if (_selected != null) ShowInExplorer(_selected); };
        _stopBtn.Click += (_, _) => SetPhase(Phase.Summary);
        _deleteBtn.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        _closeBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(_tilePanel);
        Controls.Add(_scanPanel);
        Controls.Add(_summary);
        Controls.Add(_hint);
        Controls.Add(_header);
        Controls.Add(buttons);

        SetPhase(Phase.Scanning);
    }

    public IReadOnlyList<(DuplicateCandidate Keep, IReadOnlyList<DuplicateCandidate> Remove)> Decisions =>
        _session?.Decisions ?? Array.Empty<(DuplicateCandidate, IReadOnlyList<DuplicateCandidate>)>();

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var progress = new Progress<(int Done, int Total)>(p =>
        {
            if (IsDisposed) return;
            _bar.Maximum = Math.Max(1, p.Total);
            _bar.Value = Math.Min(p.Done, _bar.Maximum);
            _scanText.Text = $"Comparing pictures… {p.Done} / {p.Total}";
        });
        IReadOnlyList<DuplicateGroup> groups;
        try
        {
            groups = await _scan(progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (IsDisposed) return;
        _session = new DuplicateReviewSession(groups);
        ShowCurrent();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _cts.Cancel();
        ClearTiles();
        base.OnFormClosed(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_phase == Phase.Reviewing)
        {
            switch (keyData)
            {
                case Keys.Left: MoveSelection(-1); return true;
                case Keys.Right: MoveSelection(+1); return true;
                case Keys.Enter: KeepSelected(); return true;
                case Keys.Back: GoBack(); return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ShowCurrent()
    {
        var group = _session?.CurrentGroup;
        if (_session == null || group == null)
        {
            SetPhase(Phase.Summary);
            return;
        }
        SetPhase(Phase.Reviewing);
        _header.Text = $"Group {_session.Current + 1} of {_session.Groups.Count}  —  " +
            (group.Kind == DuplicateKind.Exact ? $"{group.Files.Count} identical copies" : $"{group.Files.Count} look-alikes");
        _hint.Text = "Click the picture to keep (← → to switch, Enter to keep, Backspace to go back). The others are deleted at the end.";
        _backBtn.Enabled = _session.CanGoBack;

        ClearTiles();
        var gen = ++_loadGeneration;
        _tilePanel.SuspendLayout();
        foreach (var file in group.Files)
        {
            var tile = new Tile(file, file == group.Best);
            tile.Clicked += (_, _) => Select(file.Path);
            tile.DoubleClicked += (_, _) => { Select(file.Path); KeepSelected(); };
            _tiles.Add(tile);
            _tilePanel.Controls.Add(tile.Panel);
            _ = LoadTileAsync(tile, gen);
        }
        _tilePanel.ResumeLayout();
        Select(_session.ChoiceFor(_session.Current) ?? group.Best.Path);
    }

    private async Task LoadTileAsync(Tile tile, int gen)
    {
        var path = tile.File.Path;
        var (image, taken) = await Task.Run(() => LoadThumbnail(path));
        if (IsDisposed || gen != _loadGeneration)
        {
            image?.Dispose();
            return;
        }
        tile.SetImage(image, taken);
    }

    private static (Image? Image, DateTime? Taken) LoadThumbnail(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            using var img = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
            DateTime? taken = null;
            try { taken = ImageMetadata.FromImage(img).DateTaken; } catch (ArgumentException) { }
            var scale = Math.Min((double)PictureSize.Width / img.Width, (double)PictureSize.Height / img.Height);
            scale = Math.Min(scale, 1.0);
            var w = Math.Max(1, (int)(img.Width * scale));
            var h = Math.Max(1, (int)(img.Height * scale));
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(img, 0, 0, w, h);
            }
            return (bmp, taken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
        {
            return (null, null);
        }
    }

    private void Select(string path)
    {
        _selected = path;
        foreach (var t in _tiles)
            t.SetSelected(string.Equals(t.File.Path, path, StringComparison.OrdinalIgnoreCase));
        _keepBtn.Enabled = _explorerBtn.Enabled = true;
    }

    private void MoveSelection(int delta)
    {
        if (_tiles.Count == 0) return;
        var i = _tiles.FindIndex(t => string.Equals(t.File.Path, _selected, StringComparison.OrdinalIgnoreCase));
        i = (i + delta + _tiles.Count) % _tiles.Count;
        Select(_tiles[i].File.Path);
    }

    private void KeepSelected()
    {
        if (_session?.CurrentGroup == null || _selected == null) return;
        _session.Keep(_selected);
        ShowCurrent();
    }

    private void GoBack()
    {
        if (_session == null) return;
        // From the summary, Back returns to the group the user stopped at (or the last one).
        if (_phase == Phase.Reviewing || _session.IsFinished) _session.Back();
        ShowCurrent();
    }

    private void SetPhase(Phase phase)
    {
        _phase = phase;
        _scanPanel.Visible = phase == Phase.Scanning;
        _tilePanel.Visible = phase == Phase.Reviewing;
        _summary.Visible = phase == Phase.Summary;
        _hint.Visible = phase != Phase.Summary;

        var reviewing = phase == Phase.Reviewing;
        _keepBtn.Visible = _skipBtn.Visible = _explorerBtn.Visible = _stopBtn.Visible = reviewing;
        _backBtn.Visible = reviewing || (phase == Phase.Summary && _session is { Groups.Count: > 0 });
        _backBtn.Enabled = phase == Phase.Summary || (_session?.CanGoBack ?? false);
        _deleteBtn.Visible = phase == Phase.Summary && (_session?.FilesToDelete ?? 0) > 0;
        _closeBtn.Text = phase switch
        {
            Phase.Scanning => "Cancel",
            Phase.Reviewing => "Cancel",
            _ => (_session?.FilesToDelete ?? 0) > 0 ? "Close without deleting" : "Close",
        };

        if (phase == Phase.Scanning)
        {
            _header.Text = "Looking for duplicates and look-alikes…";
            _hint.Text = "Identical files and resized, re-saved or very similar pictures are grouped together.";
        }
        else if (phase == Phase.Summary)
        {
            ClearTiles();
            _header.Text = "Summary";
            _summary.Text = BuildSummary();
            _deleteBtn.Text = $"Delete {_session?.FilesToDelete} file(s)";
        }
    }

    private string BuildSummary()
    {
        if (_session == null || _session.Groups.Count == 0)
            return "No duplicates or look-alikes found.";
        var reviewed = _session.IsFinished ? _session.Groups.Count : _session.Current;
        var lines = new List<string> { $"Reviewed {reviewed} of {_session.Groups.Count} group(s)." };
        if (_session.FilesToDelete == 0)
            lines.Add("Nothing selected for deletion.");
        else
            lines.Add($"{_session.FilesToDelete} file(s) ({FormatSize(_session.BytesToDelete)}) will be {_deleteOutcome}.");
        if (_session.GroupsLeftAlone > 0)
            lines.Add($"{_session.GroupsLeftAlone} group(s) are left as they are.");
        lines.Add("");
        lines.Add("Use Back to change a choice.");
        return string.Join(Environment.NewLine, lines);
    }

    private void ClearTiles()
    {
        _loadGeneration++;
        _tilePanel.Controls.Clear();
        foreach (var t in _tiles) t.Dispose();
        _tiles.Clear();
    }

    private static void ShowInExplorer(string path)
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { }
    }

    private static void StyleBtn(Button b, string text, bool primary)
    {
        b.Text = text;
        b.AutoSize = true;
        b.Height = 30;
        b.Margin = new Padding(6, 0, 0, 0);
        b.Padding = new Padding(8, 0, 8, 0);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = primary ? Theme.Accent : Theme.Panel2;
        b.ForeColor = Theme.Fg;
    }

    internal static string FormatSize(long b) => b < 1024 ? $"{b} B"
        : b < 1024 * 1024 ? $"{b / 1024.0:0.#} KB"
        : b < 1024L * 1024 * 1024 ? $"{b / (1024.0 * 1024):0.#} MB"
        : $"{b / (1024.0 * 1024 * 1024):0.##} GB";

    private sealed class Tile : IDisposable
    {
        private readonly PictureBox _picture = new();
        private readonly Label _caption = new();
        private readonly bool _suggested;

        public Tile(DuplicateCandidate file, bool suggested)
        {
            File = file;
            _suggested = suggested;
            Panel = new Panel { Size = TileSize, Margin = new Padding(6), Padding = new Padding(6), Cursor = Cursors.Hand };
            _picture.Dock = DockStyle.Top; _picture.Height = PictureSize.Height;
            _picture.SizeMode = PictureBoxSizeMode.Zoom; _picture.BackColor = Theme.Bg;
            _caption.Dock = DockStyle.Fill; _caption.Font = CaptionFont; _caption.ForeColor = Theme.Fg;
            _caption.Padding = new Padding(0, 4, 0, 0);
            Panel.Controls.Add(_caption);
            Panel.Controls.Add(_picture);
            foreach (var c in new Control[] { Panel, _picture, _caption })
            {
                c.Click += (_, e) => Clicked?.Invoke(this, e);
                c.DoubleClick += (_, e) => DoubleClicked?.Invoke(this, e);
            }
            SetCaption(null);
        }

        public DuplicateCandidate File { get; }
        public Panel Panel { get; }
        public event EventHandler? Clicked;
        public event EventHandler? DoubleClicked;

        public void SetImage(Image? image, DateTime? taken)
        {
            var old = _picture.Image;
            _picture.Image = image;
            old?.Dispose();
            SetCaption(taken);
        }

        public void SetSelected(bool selected) => Panel.BackColor = selected ? Theme.Accent : Theme.Panel2;

        private void SetCaption(DateTime? taken)
        {
            var lines = new List<string> { Path.GetFileName(File.Path) };
            var size = FormatSize(File.Length);
            lines.Add(File.Width > 0 ? $"{File.Width} × {File.Height}    {size}" : size);
            lines.Add(taken is { } t ? $"Taken {t:yyyy-MM-dd HH:mm}" : $"Modified {File.Modified.ToLocalTime():yyyy-MM-dd HH:mm}");
            if (_suggested) lines.Add("★ Suggested to keep");
            _caption.Text = string.Join(Environment.NewLine, lines);
        }

        public void Dispose()
        {
            _picture.Image?.Dispose();
            _picture.Image = null;
            Panel.Dispose();
        }
    }
}
