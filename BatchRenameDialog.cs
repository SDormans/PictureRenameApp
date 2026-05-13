using System;
using System.Windows.Forms;

namespace PictureRenameApp;

public class BatchRenameDialog : Form
{
    private readonly TextBox _pattern = new() { Text = "file {n}" };
    private readonly NumericUpDown _start = new() { Minimum = 0, Maximum = 999999, Value = 1 };
    private readonly NumericUpDown _pad   = new() { Minimum = 1, Maximum = 8, Value = 2 };
    private readonly Label _preview = new();
    private readonly int _count;

    public string Pattern => _pattern.Text;
    public int Start => (int)_start.Value;
    public int Pad => (int)_pad.Value;

    public BatchRenameDialog(int count, string sampleExt)
    {
        _count = count;
        Text = $"Batch rename {count} files";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        BackColor = Theme.Panel; ForeColor = Theme.Fg;
        Font = new System.Drawing.Font("Segoe UI", 9.5f);
        ClientSize = new System.Drawing.Size(440, 240);

        AddLabel("Pattern (use {n} for the number):", 16);
        Style(_pattern, 40);

        AddLabel("Start at:", 76);
        _start.Left = 16; _start.Top = 96; _start.Width = 100;
        StyleNum(_start);

        AddLabel("Zero pad:", 76, 200);
        _pad.Left = 200; _pad.Top = 96; _pad.Width = 100;
        StyleNum(_pad);

        _preview.Left = 16; _preview.Top = 138; _preview.AutoSize = true;
        _preview.ForeColor = Theme.Muted;

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 264, Top = 192, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 348, Top = 192, Width = 80 };
        StyleBtn(ok, true); StyleBtn(cancel, false);

        Controls.AddRange(new Control[] { _pattern, _start, _pad, _preview, ok, cancel });
        AcceptButton = ok; CancelButton = cancel;

        _pattern.TextChanged += (_, _) => UpdatePreview(sampleExt);
        _start.ValueChanged += (_, _) => UpdatePreview(sampleExt);
        _pad.ValueChanged += (_, _) => UpdatePreview(sampleExt);
        UpdatePreview(sampleExt);
    }

    private void AddLabel(string text, int top, int left = 16)
    {
        var l = new Label { Text = text, Left = left, Top = top, AutoSize = true, ForeColor = Theme.Muted };
        Controls.Add(l);
    }
    private void Style(TextBox t, int top)
    {
        t.Left = 16; t.Top = top; t.Width = 408;
        t.BackColor = Theme.Bg; t.ForeColor = Theme.Fg; t.BorderStyle = BorderStyle.FixedSingle;
    }
    private void StyleNum(NumericUpDown n)
    {
        n.BackColor = Theme.Bg; n.ForeColor = Theme.Fg; n.BorderStyle = BorderStyle.FixedSingle;
    }
    private static void StyleBtn(Button b, bool primary)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = primary ? Theme.Accent : Theme.Panel2;
        b.ForeColor = Theme.Fg;
        b.Height = 30;
    }

    private void UpdatePreview(string ext)
    {
        var first = Format(0, ext);
        var last  = Format(_count - 1, ext);
        _preview.Text = _count <= 1 ? $"Preview: {first}" : $"Preview: {first}  …  {last}";
    }

    public string Format(int i, string ext)
    {
        var num = (Start + i).ToString(new string('0', Pad));
        var name = Pattern.Contains("{n}") ? Pattern.Replace("{n}", num) : Pattern + num;
        return name + ext;
    }
}
