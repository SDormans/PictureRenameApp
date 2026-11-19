using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace PictureRenameApp;

public partial class MainForm : Form
{
    // ---------- State ----------
    private readonly List<FileInfo> _files = new();
    private int _index = -1;

    private readonly HashSet<int> _selection = new();
    private int _selectionAnchor = -1;

    private readonly string _settingsPath = AppSettings.DefaultPath;
    private readonly AppSettings _settings;

    private RenameOperation? _lastRename;

    private readonly ConcurrentDictionary<string, (long Length, long Ticks, string Hash)> _hashCache = new();
    private readonly ConcurrentDictionary<string, (long Length, long Ticks, ImageFingerprint? Fingerprint)> _fingerprintCache = new();
    private readonly Dictionary<string, Image> _thumbCache = new();
    private int _thumbGeneration;
    private static readonly SemaphoreSlim _thumbThrottle =
        new(Math.Max(2, Environment.ProcessorCount / 2));

    private static readonly HashSet<string> ImageExt = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff" };

    private static readonly Font ThumbCaptionFont = new("Segoe UI", 8f);

    // ---------- UI ----------
    private readonly PictureBox _preview = new();
    private MemoryStream? _previewStream;
    private readonly Label _previewFallback = new();
    private readonly Label _fileName = new();
    private readonly Label _meta = new();
    private readonly Label _counter = new();
    private readonly FlowLayoutPanel _thumbStrip = new();
    private readonly List<ThumbTile> _thumbTiles = new();
    private readonly Button _btnPrev = new();
    private readonly Button _btnNext = new();
    private readonly Button _btnOpen = new();
    private readonly Button _btnRename = new();
    private readonly Button _btnRemove = new();
    private readonly Button _btnDelete = new();
    private readonly Button _btnDuplicates = new();
    private readonly Button _btnSettings = new();
    private readonly Button _btnUndo = new();
    private readonly CheckBox _chkReviewOnOpen = new();

    public MainForm()
    {
        Text = "Picture Rename App";
        BackColor = Theme.Bg;
        ForeColor = Theme.Fg;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(960, 720);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        _settings = AppSettings.Load(_settingsPath);
        BuildLayout();
        Shown += (_, _) => ReopenLastFolder();
        KeyDown += MainForm_KeyDown;
        Render();
    }

    private FileInfo? Current =>
        _index >= 0 && _index < _files.Count ? _files[_index] : null;

    private List<int> EffectiveSelection()
    {
        if (_selection.Count >= 2) return _selection.OrderBy(i => i).ToList();
        if (_index >= 0)            return new List<int> { _index };
        return new List<int>();
    }

    // ---------- Layout ----------
    private void BuildLayout()
    {
        // Toolbar
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 48, BackColor = Theme.Panel,
            Padding = new Padding(8), WrapContents = false
        };
        StyleBtn(_btnOpen, "Open folder…", true); _btnOpen.Click += (_, _) => OpenFolder_Click();
        StyleBtn(_btnPrev, "◀ Prev", false); _btnPrev.Click += (_, _) => Navigate(-1);
        StyleBtn(_btnNext, "Next ▶", false); _btnNext.Click += (_, _) => Navigate(+1);
        StyleBtn(_btnRename, "Rename", false); _btnRename.Click += (_, _) => Rename_Click();
        StyleBtn(_btnUndo, "Undo rename", false); _btnUndo.Click += (_, _) => Undo_Click(); _btnUndo.Enabled = false;
        StyleBtn(_btnRemove, "Remove from list", false); _btnRemove.Click += (_, _) => Remove_Click();
        StyleBtn(_btnDelete, "Delete", false); _btnDelete.BackColor = Theme.Danger;
        _btnDelete.Click += (_, _) => Delete_Click();
        StyleBtn(_btnDuplicates, "Review duplicates…", false); _btnDuplicates.Click += (_, _) => ReviewDuplicates();
        _chkReviewOnOpen.Text = "Find duplicates after opening";
        _chkReviewOnOpen.AutoSize = true;
        _chkReviewOnOpen.ForeColor = Theme.Fg;
        _chkReviewOnOpen.Margin = new Padding(6, 7, 6, 0);
        _chkReviewOnOpen.Checked = _settings.ReviewDuplicatesOnOpen;
        _chkReviewOnOpen.CheckedChanged += (_, _) =>
        {
            if (_settings.ReviewDuplicatesOnOpen == _chkReviewOnOpen.Checked) return;
            _settings.ReviewDuplicatesOnOpen = _chkReviewOnOpen.Checked;
            SaveSettings();
        };
        StyleBtn(_btnSettings, "Settings…", false); _btnSettings.Click += (_, _) => Settings_Click();
        toolbar.Controls.AddRange(new Control[] {
            _btnOpen, _btnPrev, _btnRename, _btnUndo, _btnRemove, _btnDelete, _btnDuplicates, _chkReviewOnOpen, _btnSettings, _btnNext
        });

        // Thumbnail strip
        var thumbHost = new Panel
        {
            Dock = DockStyle.Bottom, Height = 110, BackColor = Theme.Panel,
            Padding = new Padding(8), AutoScroll = true,
        };
        _thumbStrip.AutoSize = true;
        _thumbStrip.WrapContents = false;
        _thumbStrip.FlowDirection = FlowDirection.LeftToRight;
        _thumbStrip.Margin = Padding.Empty;
        thumbHost.Controls.Add(_thumbStrip);

        // Footer (filename + counter)
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 44, ColumnCount = 2, BackColor = Theme.Bg,
            Padding = new Padding(12, 6, 12, 6)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _fileName.Dock = DockStyle.Fill; _fileName.AutoEllipsis = true;
        _fileName.Font = new Font("Segoe UI Semibold", 11f);
        _fileName.TextAlign = ContentAlignment.MiddleLeft;
        _counter.Dock = DockStyle.Fill; _counter.ForeColor = Theme.Muted;
        _counter.TextAlign = ContentAlignment.MiddleRight; _counter.AutoSize = false; _counter.Width = 260;
        footer.Controls.Add(_fileName, 0, 0);
        footer.Controls.Add(_counter, 1, 0);

        _meta.Dock = DockStyle.Bottom; _meta.Height = 26; _meta.ForeColor = Theme.Muted;
        _meta.Padding = new Padding(12, 0, 12, 6);
        _meta.TextAlign = ContentAlignment.MiddleLeft;

        // Preview
        var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(16) };
        _preview.Dock = DockStyle.Fill;
        _preview.SizeMode = PictureBoxSizeMode.Zoom;
        _preview.BackColor = Theme.Panel;
        _previewFallback.Dock = DockStyle.Fill;
        _previewFallback.TextAlign = ContentAlignment.MiddleCenter;
        _previewFallback.ForeColor = Theme.Muted;
        _previewFallback.Font = new Font("Segoe UI", 16f);
        _previewFallback.Visible = false;
        previewHost.Controls.Add(_previewFallback);
        previewHost.Controls.Add(_preview);

        // Order matters (docking pushes inwards)
        Controls.Add(previewHost);
        Controls.Add(_meta);
        Controls.Add(footer);
        Controls.Add(thumbHost);
        Controls.Add(toolbar);
    }

    private static void StyleBtn(Button b, string text, bool primary)
    {
        b.Text = text;
        b.AutoSize = true;
        b.Height = 30;
        b.Margin = new Padding(0, 0, 8, 0);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = primary ? Theme.Accent : Theme.Panel2;
        b.ForeColor = Theme.Fg;
        b.Padding = new Padding(10, 0, 10, 0);
    }

    // ---------- Loading ----------
    private void OpenFolder_Click()
    {
        using var dlg = new FolderBrowserDialog { Description = "Choose a folder" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        LoadFolder(dlg.SelectedPath);
        if (_settings.ReviewDuplicatesOnOpen && _files.Count > 1)
            BeginInvoke(new Action(ReviewDuplicates));
    }

    private void ReopenLastFolder()
    {
        if (_settings.ReopenLastFolder && !string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder))
            LoadFolder(_settings.LastFolder);
    }

    private void Settings_Click()
    {
        using var dlg = new SettingsDialog(_settings);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        dlg.ApplyTo(_settings);
        _chkReviewOnOpen.Checked = _settings.ReviewDuplicatesOnOpen;
        SaveSettings();
    }

    private void SaveSettings()
    {
        try { _settings.Save(_settingsPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* settings are best-effort */ }
    }

    private void LoadFolder(string path)
    {
        _files.Clear();
        _hashCache.Clear();
        _fingerprintCache.Clear();
        foreach (var b in _thumbCache.Values) b.Dispose();
        _thumbCache.Clear();
        _selection.Clear();
        _selectionAnchor = -1;
        try
        {
            foreach (var f in new DirectoryInfo(path).EnumerateFiles())
                _files.Add(f);
            _settings.LastFolder = path;
            SaveSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        _index = _files.Count > 0 ? 0 : -1;
        RebuildThumbnails();
        Render();
    }

    // ---------- Rendering ----------
    private void Render()
    {
        var f = Current;
        _counter.Text = _files.Count == 0 ? "0 / 0" : $"{_index + 1} / {_files.Count}";
        if (_selection.Count >= 2) _counter.Text += $"  •  {_selection.Count} selected";
        UpdateThumbnailSelection();

        if (f == null)
        {
            _fileName.Text = "—"; _meta.Text = "";
            SetPreviewImage(null, null);
            _previewFallback.Text = "No file selected"; _previewFallback.Visible = true;
            return;
        }

        _fileName.Text = f.Name;
        _meta.Text = BuildMetaText(f, null);

        if (ImageExt.Contains(f.Extension))
        {
            try
            {
                // Load via stream so we don't keep a file lock.
                using var fs = File.OpenRead(f.FullName);
                var ms = new MemoryStream();
                try
                {
                    fs.CopyTo(ms); ms.Position = 0;
                    var img = Image.FromStream(ms);
                    SetPreviewImage(img, ms);
                    _meta.Text = BuildMetaText(f, TryGetMetadata(img));
                }
                catch
                {
                    ms.Dispose();
                    throw;
                }
                _previewFallback.Visible = false;
                return;
            }
            catch { /* fall through */ }
        }

        SetPreviewImage(null, null);
        _previewFallback.Text = f.Extension.TrimStart('.').ToUpperInvariant() + " file";
        _previewFallback.Visible = true;
    }

    private static ImageMetadata? TryGetMetadata(Image img)
    {
        try { return ImageMetadata.FromImage(img); }
        catch (ArgumentException) { return null; }
    }

    private static string BuildMetaText(FileInfo f, ImageMetadata? m)
    {
        var parts = new List<string> { $"Size: {FormatSize(f.Length)}" };
        if (m != null)
        {
            parts.Add($"Dimensions: {m.Width} × {m.Height}");
            if (m.DateTaken is { } taken) parts.Add($"Taken: {taken:yyyy-MM-dd HH:mm}");
            if (m.Camera != null) parts.Add($"Camera: {m.Camera}");
        }
        parts.Add($"Modified: {f.LastWriteTime:yyyy-MM-dd HH:mm}");
        parts.Add($"Type: {(string.IsNullOrEmpty(f.Extension) ? "file" : f.Extension)}");
        return string.Join("    ", parts);
    }

    // GDI+ requires the stream backing an Image to stay open for the Image's lifetime.
    private void SetPreviewImage(Image? image, MemoryStream? stream)
    {
        var prevImage = _preview.Image;
        var prevStream = _previewStream;
        _preview.Image = image;
        _previewStream = stream;
        prevImage?.Dispose();
        prevStream?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            SetPreviewImage(null, null);
        base.Dispose(disposing);
    }

    private void Navigate(int delta)
    {
        if (_files.Count == 0) return;
        _index = (_index + delta + _files.Count) % _files.Count;
        _selection.Clear();
        _selectionAnchor = _index;
        Render();
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (ActiveControl is TextBox) return;
        if (e.KeyCode == Keys.Left)  { Navigate(-1); e.Handled = true; }
        if (e.KeyCode == Keys.Right) { Navigate(+1); e.Handled = true; }
        if (e.KeyCode == Keys.Delete){ Delete_Click(); e.Handled = true; }
        if (e.KeyCode == Keys.F2)    { Rename_Click(); e.Handled = true; }
        if (e.KeyCode == Keys.Z && e.Control) { Undo_Click(); e.Handled = true; }
    }

    // ---------- Thumbnails ----------
    private sealed class ThumbTile : Panel
    {
        public int Index;
        public PictureBox Pic = new();
        public Label Caption = new();
    }

    private void RebuildThumbnails()
    {
        _thumbStrip.SuspendLayout();
        foreach (Control c in _thumbStrip.Controls) c.Dispose();
        _thumbStrip.Controls.Clear();
        _thumbTiles.Clear();

        var generation = ++_thumbGeneration;

        for (int i = 0; i < _files.Count; i++)
        {
            int idx = i;
            var f = _files[i];

            var tile = new ThumbTile
            {
                Index = idx,
                Width = 92, Height = 96,
                Margin = new Padding(4, 0, 4, 0),
                BackColor = Theme.Panel2,
                Cursor = Cursors.Hand,
            };
            tile.Pic.Dock = DockStyle.Top;
            tile.Pic.Height = 64;
            tile.Pic.SizeMode = PictureBoxSizeMode.Zoom;
            tile.Pic.BackColor = Theme.Bg;
            tile.Caption.Dock = DockStyle.Bottom;
            tile.Caption.Height = 28;
            tile.Caption.Text = f.Name;
            tile.Caption.ForeColor = Theme.Muted;
            tile.Caption.TextAlign = ContentAlignment.MiddleCenter;
            tile.Caption.AutoEllipsis = true;
            tile.Caption.Font = ThumbCaptionFont;
            tile.Controls.Add(tile.Caption);
            tile.Controls.Add(tile.Pic);

            tile.MouseUp += (s, e) => Thumb_Click(idx);
            tile.Pic.MouseUp += (s, e) => Thumb_Click(idx);
            tile.Caption.MouseUp += (s, e) => Thumb_Click(idx);

            _thumbTiles.Add(tile);
            _thumbStrip.Controls.Add(tile);

            QueueThumbnail(f, tile.Pic, generation);
        }
        _thumbStrip.ResumeLayout();
        UpdateThumbnailSelection();
    }

    private void Thumb_Click(int i)
    {
        var mods = ModifierKeys;
        if (mods.HasFlag(Keys.Shift) && _selectionAnchor >= 0)
        {
            _selection.Clear();
            int lo = Math.Min(_selectionAnchor, i), hi = Math.Max(_selectionAnchor, i);
            for (int k = lo; k <= hi; k++) _selection.Add(k);
        }
        else if (mods.HasFlag(Keys.Control))
        {
            if (!_selection.Add(i)) _selection.Remove(i);
            _selectionAnchor = i;
        }
        else
        {
            _selection.Clear();
            _selectionAnchor = i;
        }
        _index = i;
        Render();
    }

    private void UpdateThumbnailSelection()
    {
        for (int i = 0; i < _thumbTiles.Count; i++)
        {
            bool isCurrent  = i == _index;
            bool isSelected = _selection.Contains(i);
            _thumbTiles[i].BackColor =
                isCurrent  ? Theme.Accent :
                isSelected ? Color.Gray   : Theme.Panel2;
            if (isCurrent) _thumbStrip.ScrollControlIntoView(_thumbTiles[i]);
        }
        _selection.RemoveWhere(i => i < 0 || i >= _files.Count);
    }

    private void QueueThumbnail(FileInfo f, PictureBox target, int generation)
    {
        if (!ImageExt.Contains(f.Extension)) return;

        if (_thumbCache.TryGetValue(f.FullName, out var cached))
        {
            target.Image = cached;
            return;
        }

        var path = f.FullName;
        _ = Task.Run(async () =>
        {
            await _thumbThrottle.WaitAsync().ConfigureAwait(false);
            try
            {
                if (generation != _thumbGeneration) return;

                Image? thumb = null;
                try
                {
                    using var fs = File.OpenRead(path);
                    using var src = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
                    int w = 160, h = (int)(src.Height * (160.0 / src.Width));
                    thumb = new Bitmap(src, new Size(w, h));
                }
                catch { /* leave blank */ }

                if (thumb == null) return;

                if (IsDisposed) { thumb.Dispose(); return; }

                BeginInvoke(new Action(() =>
                {
                    if (generation != _thumbGeneration) { thumb.Dispose(); return; }
                    _thumbCache[path] = thumb;
                    target.Image = thumb;
                }));
            }
            finally { _thumbThrottle.Release(); }
        });
    }

    // ---------- Rename ----------
    private void Rename_Click()
    {
        var sel = EffectiveSelection();
        if (sel.Count == 0) return;
        if (sel.Count == 1) RenameSingle(sel[0]);
        else RenameBatch(sel);
    }

    private void RenameSingle(int idx)
    {
        var f = _files[idx];
        var dir = f.DirectoryName!;
        using var dlg = new RenameDialog(f.Name, candidate =>
        {
            if (string.IsNullOrWhiteSpace(candidate)) return "Name cannot be empty.";
            if (candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return "Invalid characters in name.";
            return null;
        });
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var newName = dlg.NewName;
        var target = Path.Combine(dir, newName);
        if (!string.Equals(target, f.FullName, StringComparison.OrdinalIgnoreCase) && File.Exists(target))
            target = SuggestUniqueName(target);

        try
        {
            File.Move(f.FullName, target);
            var op = new RenameOperation();
            op.AddMove(f.FullName, target);
            SetLastRename(op);
            ReplaceFileEntry(idx, f.FullName, target);
            RebuildThumbnails();
            Render();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Rename failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenameBatch(List<int> indices)
    {
        var items = new RenameItem?[indices.Count];
        RenameItem ItemAt(int i) => items[i] ??= ToRenameItem(_files[indices[i]]);
        using var dlg = new BatchRenameDialog(indices.Count, ItemAt, _settings.BatchMoveToSubfolder, _settings.DefaultZeroPad);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        int errors = 0;
        var op = new RenameOperation();
        for (int i = 0; i < indices.Count; i++)
        {
            var idx = indices[i];
            var f = _files[idx];
            var dir = f.DirectoryName!;
            if (dlg.MoveToSubfolder && !string.Equals(Path.GetFileName(dir), dlg.SubfolderName, StringComparison.OrdinalIgnoreCase))
                dir = Path.Combine(dir, dlg.SubfolderName);
            var target = Path.Combine(dir, dlg.Format(i));
            if (!string.Equals(target, f.FullName, StringComparison.OrdinalIgnoreCase) && File.Exists(target))
                target = SuggestUniqueName(target);
            try
            {
                op.EnsureDirectory(dir);
                File.Move(f.FullName, target);
                op.AddMove(f.FullName, target);
                ReplaceFileEntry(idx, f.FullName, target);
            }
            catch { errors++; }
        }
        SetLastRename(op);
        _selection.Clear();
        RebuildThumbnails();
        Render();
        if (errors > 0)
            MessageBox.Show(this, $"{errors} file(s) failed to rename.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static RenameItem ToRenameItem(FileInfo f)
    {
        var taken = ImageExt.Contains(f.Extension) ? ImageMetadata.TryRead(f.FullName)?.DateTaken : null;
        return new RenameItem(Path.GetFileNameWithoutExtension(f.Name), f.Extension, taken ?? f.LastWriteTime);
    }

    private void SetLastRename(RenameOperation op)
    {
        if (op.IsEmpty) return;
        _lastRename = op;
        _btnUndo.Enabled = true;
    }

    private void Undo_Click()
    {
        var op = _lastRename;
        if (op == null) return;
        _lastRename = null;
        _btnUndo.Enabled = false;

        var result = op.Undo();
        foreach (var (from, to) in result.Restored)
        {
            var idx = _files.FindIndex(f => string.Equals(f.FullName, to, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) ReplaceFileEntry(idx, to, from);
        }
        RebuildThumbnails();
        Render();
        if (result.Failed > 0)
            MessageBox.Show(this, $"{result.Failed} file(s) could not be restored (missing, or the original name is taken).",
                "Undo rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string SuggestUniqueName(string path)
    {
        var dir = Path.GetDirectoryName(path)!;
        var stem = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (int n = 2; n < 10000; n++)
        {
            var p = Path.Combine(dir, $"{stem} ({n}){ext}");
            if (!File.Exists(p)) return p;
        }
        return Path.Combine(dir, $"{stem} ({Guid.NewGuid():N}){ext}");
    }

    private void ReplaceFileEntry(int idx, string oldPath, string newPath)
    {
        if (_hashCache.TryRemove(oldPath, out var cached))
        {
            _hashCache[newPath] = cached;
        }
        if (_fingerprintCache.TryRemove(oldPath, out var fp))
        {
            _fingerprintCache[newPath] = fp;
        }
        if (_thumbCache.TryGetValue(oldPath, out var tn))
        {
            _thumbCache.Remove(oldPath);
            _thumbCache[newPath] = tn;
        }
        var u = new FileInfo(newPath); u.Refresh();
        _files[idx] = u;
    }

    // ---------- Remove / Delete ----------
    private void Remove_Click()
    {
        var indices = EffectiveSelection();
        if (indices.Count == 0) return;

        // Ask user whether to remove from list or delete from disk
        var prompt = indices.Count == 1
            ? $"Remove '{_files[indices[0]].Name}' from the list (Yes), or {DeleteVerb} it (No)?"
            : $"Remove {indices.Count} files from the list (Yes), or {DeleteVerb} them (No)?";

        var result = MessageBox.Show(this, prompt, "Remove / Delete",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        if (result == DialogResult.Cancel)
            return;

        if (result == DialogResult.No)
        {
            // User chose to delete from disk - delegate to Delete_Click logic
            Delete_Click();
            return;
        }

        // Default: remove from list only
        foreach (var idx in indices.OrderByDescending(i => i))
        {
            _hashCache.TryRemove(_files[idx].FullName, out _);
            _files.RemoveAt(idx);
        }
        _selection.Clear();
        if (_index >= _files.Count) _index = _files.Count - 1;
        RebuildThumbnails(); Render();
    }

    private void Delete_Click()
    {
        var indices = EffectiveSelection();
        if (indices.Count == 0) return;
        var prompt = indices.Count == 1
            ? $"{Capitalize(DeleteVerb)} '{_files[indices[0]].Name}'?"
            : $"{Capitalize(DeleteVerb)} {indices.Count} files?";
        if (MessageBox.Show(this, prompt, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        DeleteFiles(indices.Select(i => _files[i].FullName).ToList());
    }

    private string DeleteVerb => _settings.UseRecycleBin ? "move to the Recycle Bin" : "permanently delete";

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private void DeleteFromDisk(string path)
    {
        if (_settings.UseRecycleBin)
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        else
            File.Delete(path);
    }

    private List<string> DeleteFiles(IReadOnlyCollection<string> paths)
    {
        int errors = 0;
        var deleted = new List<string>();
        foreach (var path in paths)
        {
            try
            {
                DeleteFromDisk(path);
                deleted.Add(path);
                _hashCache.TryRemove(path, out _);
                var idx = _files.FindIndex(f => string.Equals(f.FullName, path, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    _files.RemoveAt(idx);
                    if (idx < _index) _index--;
                }
            }
            catch { errors++; }
        }
        _selection.Clear();
        if (_index >= _files.Count) _index = _files.Count - 1;
        RebuildThumbnails(); Render();
        if (errors > 0)
            MessageBox.Show(this, $"{errors} file(s) failed to delete.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return deleted;
    }

    // ---------- Duplicates ----------
    private void ReviewDuplicates()
    {
        if (_files.Count < 2)
        {
            MessageBox.Show(this, "Open a folder with at least two files first.", "Review duplicates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var outcome = _settings.UseRecycleBin ? "moved to the Recycle Bin" : "permanently deleted";
        using var dlg = new DuplicateReviewForm(ScanDuplicatesAsync, outcome);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        DeleteReviewed(dlg.Decisions);
    }

    private Task<IReadOnlyList<DuplicateGroup>> ScanDuplicatesAsync(IProgress<(int Done, int Total)> progress, CancellationToken ct)
    {
        var paths = _files.Select(f => f.FullName).ToList();
        return Task.Run<IReadOnlyList<DuplicateGroup>>(() =>
        {
            var files = paths.Select(p => new FileInfo(p)).Where(f => f.Exists).ToList();
            // Only files that share their length with another file can be byte-identical.
            var sameLength = files.GroupBy(f => f.Length).Where(g => g.Count() > 1)
                .SelectMany(g => g).Select(f => f.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidates = new DuplicateCandidate?[files.Count];
            int done = 0;
            int reportEvery = Math.Max(1, files.Count / 100);
            // Decoding full-size photos is memory hungry, so keep parallelism modest.
            var options = new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount / 2, 2, 4) };
            Parallel.For(0, files.Count, options, i =>
            {
                var f = files[i];
                string? content = null;
                try { if (sameLength.Contains(f.FullName)) content = HashFileCached(f); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                var fp = ImageExt.Contains(f.Extension) ? FingerprintCached(f) : null;
                candidates[i] = new DuplicateCandidate(f.FullName, f.Length, f.LastWriteTimeUtc, content,
                    fp?.Hash, fp?.Width ?? 0, fp?.Height ?? 0);
                var d = Interlocked.Increment(ref done);
                if (d % reportEvery == 0 || d == files.Count) progress.Report((d, files.Count));
            });
            return DuplicateGrouper.Group(candidates.OfType<DuplicateCandidate>().ToList());
        }, ct);
    }

    private ImageFingerprint? FingerprintCached(FileInfo f)
    {
        if (_fingerprintCache.TryGetValue(f.FullName, out var entry)
            && entry.Length == f.Length && entry.Ticks == f.LastWriteTimeUtc.Ticks)
            return entry.Fingerprint;
        var fp = ImageFingerprint.TryCompute(f.FullName);
        _fingerprintCache[f.FullName] = (f.Length, f.LastWriteTimeUtc.Ticks, fp);
        return fp;
    }

    private void DeleteReviewed(IReadOnlyList<(DuplicateCandidate Keep, IReadOnlyList<DuplicateCandidate> Remove)> decisions)
    {
        // Files may have changed since the scan: only delete unchanged copies whose kept file is also unchanged.
        var toDelete = new List<string>();
        var sizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        int changed = 0;
        foreach (var (keep, remove) in decisions)
        {
            var kept = new FileInfo(keep.Path);
            if (!kept.Exists || kept.Length != keep.Length || kept.LastWriteTimeUtc != keep.Modified)
            {
                changed += remove.Count;
                continue;
            }
            foreach (var c in remove)
            {
                var fi = new FileInfo(c.Path);
                if (fi.Exists && fi.Length == c.Length && fi.LastWriteTimeUtc == c.Modified
                    && !string.Equals(fi.FullName, keep.Path, StringComparison.OrdinalIgnoreCase))
                {
                    toDelete.Add(fi.FullName);
                    sizes[fi.FullName] = fi.Length;
                }
                else changed++;
            }
        }

        var deleted = toDelete.Count > 0 ? DeleteFiles(toDelete) : new List<string>();
        var verb = _settings.UseRecycleBin ? "Moved" : "Deleted";
        var where = _settings.UseRecycleBin ? " to the Recycle Bin" : "";
        var msg = $"{verb} {deleted.Count} file(s){where} ({FormatSize(deleted.Sum(p => sizes[p]))}).";
        if (changed > 0) msg += $"\n\n{changed} file(s) changed or disappeared since the scan and were left alone.";
        MessageBox.Show(this, msg, "Review duplicates", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string HashFileCached(FileInfo f)
    {
        var key = f.FullName;
        if (_hashCache.TryGetValue(key, out var entry)
            && entry.Length == f.Length && entry.Ticks == f.LastWriteTimeUtc.Ticks)
            return entry.Hash;

        using var sha = SHA1.Create();
        using var fs = File.OpenRead(f.FullName);
        var hash = Convert.ToHexString(sha.ComputeHash(fs));
        _hashCache[key] = (f.Length, f.LastWriteTimeUtc.Ticks, hash);
        return hash;
    }

    // ---------- Helpers ----------
    private static string FormatSize(long b) => b < 1024 ? $"{b} B"
        : b < 1024 * 1024 ? $"{b / 1024.0:0.#} KB"
        : b < 1024L * 1024 * 1024 ? $"{b / (1024.0 * 1024):0.#} MB"
        : $"{b / (1024.0 * 1024 * 1024):0.##} GB";
}
