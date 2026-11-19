using System;
using System.Windows.Forms;

namespace PictureRenameApp;

public class SettingsDialog : Form
{
    private readonly CheckBox _moveToSubfolder = new() { Text = "Batch rename: move files into a subfolder named after the base name" };
    private readonly CheckBox _recycleBin = new() { Text = "Delete: send files to the Recycle Bin (instead of deleting permanently)" };
    private readonly CheckBox _reopenLast = new() { Text = "Reopen the last folder on startup" };
    private readonly NumericUpDown _pad = new() { Minimum = AppSettings.MinZeroPad, Maximum = AppSettings.MaxZeroPad };

    public SettingsDialog(AppSettings settings)
    {
        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        BackColor = Theme.Panel; ForeColor = Theme.Fg;
        Font = new System.Drawing.Font("Segoe UI", 9.5f);
        ClientSize = new System.Drawing.Size(520, 230);

        _moveToSubfolder.Checked = settings.BatchMoveToSubfolder;
        _recycleBin.Checked = settings.UseRecycleBin;
        _reopenLast.Checked = settings.ReopenLastFolder;
        _pad.Value = Math.Clamp(settings.DefaultZeroPad, AppSettings.MinZeroPad, AppSettings.MaxZeroPad);

        int top = 16;
        foreach (var cb in new[] { _moveToSubfolder, _recycleBin, _reopenLast })
        {
            cb.Left = 16; cb.Top = top; cb.AutoSize = true;
            top += 32;
        }

        var padLabel = new Label { Text = "Default zero pad for batch rename:", Left = 16, Top = top + 4, AutoSize = true, ForeColor = Theme.Muted };
        _pad.Left = 260; _pad.Top = top; _pad.Width = 80;
        _pad.BackColor = Theme.Bg; _pad.ForeColor = Theme.Fg; _pad.BorderStyle = BorderStyle.FixedSingle;

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 344, Top = 184, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 428, Top = 184, Width = 80 };
        StyleBtn(ok, true); StyleBtn(cancel, false);

        Controls.AddRange(new Control[] { _moveToSubfolder, _recycleBin, _reopenLast, padLabel, _pad, ok, cancel });
        AcceptButton = ok; CancelButton = cancel;
    }

    public void ApplyTo(AppSettings settings)
    {
        settings.BatchMoveToSubfolder = _moveToSubfolder.Checked;
        settings.UseRecycleBin = _recycleBin.Checked;
        settings.ReopenLastFolder = _reopenLast.Checked;
        settings.DefaultZeroPad = (int)_pad.Value;
    }

    private static void StyleBtn(Button b, bool primary)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = primary ? Theme.Accent : Theme.Panel2;
        b.ForeColor = Theme.Fg;
        b.Height = 30;
    }
}
