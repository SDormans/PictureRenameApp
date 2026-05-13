using System;
using System.Windows.Forms;

namespace PictureRenameApp;

public class RenameDialog : Form
{
    private readonly TextBox _txt = new();
    private readonly Label _err = new();
    public string NewName => _txt.Text.Trim();

    public RenameDialog(string currentName, Func<string, string?> validator)
    {
        Text = "Rename file";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        BackColor = Theme.Panel;
        ForeColor = Theme.Fg;
        Font = new System.Drawing.Font("Segoe UI", 9.5f);
        ClientSize = new System.Drawing.Size(420, 150);

        var lbl = new Label { Text = "New name:", Left = 16, Top = 16, AutoSize = true, ForeColor = Theme.Muted };
        _txt.Left = 16; _txt.Top = 40; _txt.Width = 388; _txt.Text = currentName;
        _txt.BackColor = Theme.Bg; _txt.ForeColor = Theme.Fg; _txt.BorderStyle = BorderStyle.FixedSingle;

        _err.Left = 16; _err.Top = 70; _err.AutoSize = true; _err.ForeColor = Theme.Danger; _err.Text = "";

        var ok = new Button { Text = "OK", DialogResult = DialogResult.None, Left = 244, Top = 102, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 328, Top = 102, Width = 80 };
        StyleButton(ok, true); StyleButton(cancel, false);

        ok.Click += (_, _) =>
        {
            var msg = validator(_txt.Text.Trim());
            if (msg != null) { _err.Text = msg; return; }
            DialogResult = DialogResult.OK;
        };

        Controls.AddRange(new Control[] { lbl, _txt, _err, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
        _txt.SelectAll();
    }

    private static void StyleButton(Button b, bool primary)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = primary ? Theme.Accent : Theme.Panel2;
        b.ForeColor = Theme.Fg;
        b.Height = 30;
    }
}
