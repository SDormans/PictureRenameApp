using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureRenameApp;

public partial class MainForm : Form
{
    // ---------- State ----------
    private readonly List<FileInfo> _files = new();
    private int _index = -1;

    private readonly HashSet<int> _selection = new();
    private int _selectionAnchor = -1;

    private DuplicatesForm? _duplicatesForm;
    private CancellationTokenSource? _scanCts;

    private readonly Dictionary<string, (long Length, long Ticks, string Hash)> _hashCache = new();
    private readonly Dictionary<string, Image> _thumbCache = new();
    private int _thumbGeneration;
    private static readonly SemaphoreSlim _thumbThrottle =
        new(Math.Max(2, Environment.ProcessorCount / 2));

    private static readonly string[] ImageExt =
        { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff" };

    // ---------- UI ----------
    private readonly PictureBox _preview = new();
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

    public MainForm()
    {
        Text = "Picture Rename App";
        BackColor = Theme.Bg;
        ForeColor = Theme.Fg;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(960, 720);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        BuildLayout();
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
        StyleBtn(_btnRemove, "Remove from list", false); _btnRemove.Click += (_, _) => Remove_Click();
        StyleBtn(_btnDelete, "Delete", false); _btnDelete.BackColor = Theme.Danger;
        _btnDelete.Click += (_, _) => Delete_Click();
        StyleBtn(_btnDuplicates, "Find duplicates…", false); _btnDuplicates.Click += (_, _) => ShowDuplicates_Click();
        toolbar.Controls.AddRange(new Control[] {
            _btnOpen, _btnPrev, _btnRename, _btnRemove, _btnDelete, _btnDuplicates, _btnNext
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
        if (dlg.ShowDialog(this) == DialogResult.OK)
            LoadFolder(dlg.SelectedPath);
    }

    private void LoadFolder(string path)
    {
        _files.Clear();
        _hashCache.Clear();
        foreach (var b in _thumbCache.Values) b.Dispose();
        _thumbCache.Clear();
        _selection.Clear();
        _selectionAnchor = -1;
        try
        {
            foreach (var f in new DirectoryInfo(path).EnumerateFiles())
                _files.Add(f);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        _index = _files.Count > 0 ? 0 : -1;
        RebuildThumbnails();
        Render();
        _ = RefreshDuplicatesAsync();
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
            _preview.Image = null;
            _previewFallback.Text = "No file selected"; _previewFallback.Visible = true;
            return;
        }

        _fileName.Text = f.Name;
        _meta.Text = $"Size: {FormatSize(f.Length)}    Modified: {f.LastWriteTime:yyyy-MM-dd HH:mm}    Type: {(string.IsNullOrEmpty(f.Extension) ? "file" : f.Extension)}";

        if (ImageExt.Contains(f.Extension.ToLowerInvariant()))
        {
            try
            {
                // Load via stream so we don't keep a file lock.
                using var fs = File.OpenRead(f.FullName);
                var ms = new MemoryStream(); fs.CopyTo(ms); ms.Position = 0;
                var prev = _preview.Image;
                _preview.Image = Image.FromStream(ms);
                prev?.Dispose();
                _previewFallback.Visible = false;
                return;
            }
            catch { /* fall through */ }
        }

        _preview.Image = null;
        _previewFallback.Text = f.Extension.TrimStart('.').ToUpperInvariant() + " file";
        _previewFallback.Visible = true;
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
            tile.Caption.Font = new Font("Segoe UI", 8f);
            tile.Controls.Add(tile.Caption);
            tile.Controls.Add(tile.Pic);

            EventHandler<MouseEventArgs> click = (_, e) => Thumb_Click(idx);
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
        if (!ImageExt.Contains(f.Extension.ToLowerInvariant())) return;

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
            ReplaceFileEntry(idx, f.FullName, target);
            RebuildThumbnails();
            Render();
            _ = RefreshDuplicatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Rename failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenameBatch(List<int> indices)
    {
        var sampleExt = _files[indices[0]].Extension;
        using var dlg = new BatchRenameDialog(indices.Count, sampleExt);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        int errors = 0;
        for (int i = 0; i < indices.Count; i++)
        {
            var idx = indices[i];
            var f = _files[idx];
            var dir = f.DirectoryName!;
            var target = Path.Combine(dir, dlg.Format(i, f.Extension));
            if (!string.Equals(target, f.FullName, StringComparison.OrdinalIgnoreCase) && File.Exists(target))
                target = SuggestUniqueName(target);
            try
            {
                File.Move(f.FullName, target);
                ReplaceFileEntry(idx, f.FullName, target);
            }
            catch { errors++; }
        }
        _selection.Clear();
        RebuildThumbnails();
        Render();
        _ = RefreshDuplicatesAsync();
        if (errors > 0)
            MessageBox.Show(this, $"{errors} file(s) failed to rename.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        if (_hashCache.TryGetValue(oldPath, out var cached))
        {
            _hashCache.Remove(oldPath);
            _hashCache[newPath] = cached;
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
            ? $"Remove '{_files[indices[0]].Name}' from the list (Yes), or permanently delete it from disk (No)?"
            : $"Remove {indices.Count} files from the list (Yes), or permanently delete them from disk (No)?";

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
            _hashCache.Remove(_files[idx].FullName);
            _files.RemoveAt(idx);
        }
        _selection.Clear();
        if (_index >= _files.Count) _index = _files.Count - 1;
        RebuildThumbnails(); Render();
        _ = RefreshDuplicatesAsync();
    }

    private void Delete_Click()
    {
        var indices = EffectiveSelection();
        if (indices.Count == 0) return;
        var prompt = indices.Count == 1
            ? $"Permanently delete '{_files[indices[0]].Name}'?"
            : $"Permanently delete {indices.Count} files?";
        if (MessageBox.Show(this, prompt, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        int errors = 0;
        foreach (var idx in indices.OrderByDescending(i => i))
        {
            var f = _files[idx];
            try
            {
                File.Delete(f.FullName);
                _hashCache.Remove(f.FullName);
                _files.RemoveAt(idx);
            }
            catch { errors++; }
        }
        _selection.Clear();
        if (_index >= _files.Count) _index = _files.Count - 1;
        RebuildThumbnails(); Render();
        _ = RefreshDuplicatesAsync();
        if (errors > 0)
            MessageBox.Show(this, $"{errors} file(s) failed to delete.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ---------- Duplicates ----------
    private void ShowDuplicates_Click()
    {
        if (_duplicatesForm == null || _duplicatesForm.IsDisposed)
        {
            _duplicatesForm = new DuplicatesForm();
            _duplicatesForm.CancelRequested += (_, _) => _scanCts?.Cancel();
            _duplicatesForm.RefreshRequested += (_, _) => _ = RefreshDuplicatesAsync();
            _duplicatesForm.FormClosed += (_, _) => { _scanCts?.Cancel(); _duplicatesForm = null; };
        }
        _duplicatesForm.Show(this);
        _duplicatesForm.BringToFront();
        _ = RefreshDuplicatesAsync();
    }

    private async Task RefreshDuplicatesAsync()
    {
        if (_duplicatesForm == null || _duplicatesForm.IsDisposed) return;

        _scanCts?.Cancel();
        var cts = new CancellationTokenSource();
        _scanCts = cts;
        var ct = cts.Token;

        // Group by length first; only hash candidates.
        var bySize = _files.GroupBy(f => f.Length).Where(g => g.Count() > 1).ToList();
        var candidates = bySize.SelectMany(g => g).ToList();

        _duplicatesForm.BeginScan(candidates.Count);
        var snapshot = candidates.ToList();
        var hashes = new Dictionary<string, string>();
        int done = 0;
        int reportEvery = Math.Max(1, snapshot.Count / 100);

        try
        {
            await Task.Run(() =>
            {
                foreach (var f in snapshot)
                {
                    if (ct.IsCancellationRequested) return;
                    var h = HashFileCached(f);
                    lock (hashes) hashes[f.FullName] = h;
                    int d = Interlocked.Increment(ref done);
                    if (d % reportEvery == 0 || d == snapshot.Count)
                    {
                        var name = f.Name;
                        BeginInvoke(new Action(() =>
                        {
                            if (_duplicatesForm is { IsDisposed: false } w) w.ReportProgress(d, snapshot.Count, name);
                        }));
                    }
                }
            }, ct);
        }
        catch (OperationCanceledException) { }

        if (ct.IsCancellationRequested || _duplicatesForm == null || _duplicatesForm.IsDisposed) return;

        var groups = candidates
            .Where(f => hashes.ContainsKey(f.FullName))
            .GroupBy(f => hashes[f.FullName])
            .Where(g => g.Count() > 1)
            .Select(g => g.ToList())
            .OrderByDescending(g => g.Count)
            .ToList();

        _duplicatesForm.SetGroups(groups);
        _duplicatesForm.EndScan();
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
        lock (_hashCache)
            _hashCache[key] = (f.Length, f.LastWriteTimeUtc.Ticks, hash);
        return hash;
    }

    // ---------- Helpers ----------
    private static string FormatSize(long b) => b < 1024 ? $"{b} B"
        : b < 1024 * 1024 ? $"{b / 1024.0:0.#} KB"
        : b < 1024L * 1024 * 1024 ? $"{b / (1024.0 * 1024):0.#} MB"
        : $"{b / (1024.0 * 1024 * 1024):0.##} GB";
}
