using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PictureRenameApp;

public class DuplicatesForm : Form
{
    private readonly TreeView _tree = new();
    private readonly Label _summary = new();
    private readonly ProgressBar _bar = new();
    private readonly Label _progressText = new();
    private readonly Button _cancelBtn = new();
    private readonly Button _refreshBtn = new();

    public event EventHandler? CancelRequested;
    public event EventHandler? RefreshRequested;

    public DuplicatesForm()
    {
        Text = "Duplicate files";
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Bg; ForeColor = Theme.Fg;
        Font = new System.Drawing.Font("Segoe UI", 9.5f);
        ClientSize = new System.Drawing.Size(640, 480);

        _summary.Dock = DockStyle.Top; _summary.Height = 28;
        _summary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _summary.Padding = new Padding(8, 0, 8, 0);
        _summary.ForeColor = Theme.Muted;
        _summary.Text = "Idle";

        var progRow = new Panel { Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 4, 8, 4) };
        _bar.Dock = DockStyle.Fill; _bar.Style = ProgressBarStyle.Continuous; _bar.Maximum = 1000;
        _progressText.Dock = DockStyle.Right; _progressText.Width = 320; _progressText.AutoEllipsis = true;
        _progressText.TextAlign = System.Drawing.ContentAlignment.MiddleRight; _progressText.ForeColor = Theme.Muted;
        progRow.Controls.Add(_bar);
        progRow.Controls.Add(_progressText);

        _tree.Dock = DockStyle.Fill;
        _tree.BackColor = Theme.Panel; _tree.ForeColor = Theme.Fg;
        _tree.BorderStyle = BorderStyle.None; _tree.HideSelection = false;

        var btnRow = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(8) };
        StyleBtn(_cancelBtn, false); _cancelBtn.Text = "Cancel scan"; _cancelBtn.Width = 110; _cancelBtn.Dock = DockStyle.Left;
        _cancelBtn.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        StyleBtn(_refreshBtn, true); _refreshBtn.Text = "Refresh"; _refreshBtn.Width = 90; _refreshBtn.Dock = DockStyle.Right;
        _refreshBtn.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        btnRow.Controls.Add(_cancelBtn);
        btnRow.Controls.Add(_refreshBtn);

        Controls.Add(_tree);
        Controls.Add(progRow);
        Controls.Add(_summary);
        Controls.Add(btnRow);

        EndScan();
    }

    private static void StyleBtn(Button b, bool primary)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = primary ? Theme.Accent : Theme.Panel2;
        b.ForeColor = Theme.Fg;
        b.Height = 30;
    }

    public void BeginScan(int total)
    {
        _bar.Visible = true; _progressText.Visible = true;
        _bar.Value = 0;
        _progressText.Text = $"0 / {total}";
        _summary.Text = "Hashing…";
        _cancelBtn.Enabled = true;
        _refreshBtn.Enabled = false;
    }

    public void ReportProgress(int done, int total, string current)
    {
        if (total <= 0) return;
        var v = (int)Math.Min(1000, Math.Round(1000.0 * done / total));
        _bar.Value = v;
        _progressText.Text = $"{done} / {total} — {Truncate(current, 60)}";
    }

    public void EndScan()
    {
        _bar.Visible = false; _progressText.Visible = false;
        _cancelBtn.Enabled = false;
        _refreshBtn.Enabled = true;
    }

    public void SetGroups(List<List<FileInfo>> groups)
    {
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        if (groups.Count == 0)
        {
            _summary.Text = "No duplicates found";
        }
        else
        {
            int total = groups.Sum(g => g.Count);
            _summary.Text = $"{groups.Count} duplicate group(s), {total} file(s)";
            int gi = 1;
            foreach (var g in groups)
            {
                var head = new TreeNode($"Group {gi++}  ·  {g.Count} files  ·  {FormatSize(g[0].Length)}");
                foreach (var f in g) head.Nodes.Add(new TreeNode(f.FullName));
                head.Expand();
                _tree.Nodes.Add(head);
            }
        }
        _tree.EndUpdate();
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : "…" + s[^(max - 1)..];
    private static string FormatSize(long b) => b < 1024 ? $"{b} B"
        : b < 1024 * 1024 ? $"{b / 1024.0:0.#} KB"
        : b < 1024L * 1024 * 1024 ? $"{b / (1024.0 * 1024):0.#} MB"
        : $"{b / (1024.0 * 1024 * 1024):0.##} GB";
}
