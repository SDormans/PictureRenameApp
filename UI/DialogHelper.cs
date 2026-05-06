namespace PictureRenameApp.Utilities
{
    /// <summary>
    /// Helper class for user interaction dialogs.
    /// Encapsulates dialog creation and input collection for better UI organization.
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Shows a dialog to prompt for a single string input.
        /// </summary>
        /// <param name="owner">Parent form for dialog positioning</param>
        /// <param name="title">Dialog window title</param>
        /// <param name="prompt">Prompt text displayed to user</param>
        /// <param name="defaultValue">Default text in input field</param>
        /// <returns>User input or null if cancelled</returns>
        public static string? PromptForString(Form owner, string title, string prompt, string defaultValue = "")
        {
            using var f = new Form()
            {
                StartPosition = FormStartPosition.CenterParent,
                Width = 420,
                Height = 140,
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                Owner = owner
            };

            var lbl = new Label()
            {
                Left = 12,
                Top = 10,
                Width = 380,
                Text = prompt,
                AutoSize = false
            };

            var txt = new TextBox()
            {
                Left = 12,
                Top = 32,
                Width = 380,
                Text = defaultValue
            };

            var ok = new Button()
            {
                Text = "OK",
                Left = 232,
                Width = 80,
                Top = 64,
                DialogResult = DialogResult.OK
            };

            var cancel = new Button()
            {
                Text = "Cancel",
                Left = 320,
                Width = 80,
                Top = 64,
                DialogResult = DialogResult.Cancel
            };

            f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            return f.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
        }

        /// <summary>
        /// Shows a dialog to prompt for batch rename parameters.
        /// </summary>
        /// <param name="owner">Parent form for dialog positioning</param>
        /// <returns>Tuple of (BaseName, StartIndex) or null if cancelled</returns>
        public static (string BaseName, int StartIndex)? PromptForBatchRename(Form owner)
        {
            using var f = new Form()
            {
                StartPosition = FormStartPosition.CenterParent,
                Width = 420,
                Height = 180,
                Text = "Batch Rename",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                Owner = owner
            };

            var lbl1 = new Label()
            {
                Left = 12,
                Top = 10,
                Width = 380,
                Text = "Base name (will be used as BaseName_###.ext):",
                AutoSize = false
            };

            var txtBase = new TextBox()
            {
                Left = 12,
                Top = 32,
                Width = 380,
                Text = "Image"
            };

            var lbl2 = new Label()
            {
                Left = 12,
                Top = 62,
                Width = 200,
                Text = "Start index:"
            };

            var txtStart = new TextBox()
            {
                Left = 220,
                Top = 58,
                Width = 60,
                Text = "1"
            };

            var ok = new Button()
            {
                Text = "OK",
                Left = 232,
                Width = 80,
                Top = 98,
                DialogResult = DialogResult.OK
            };

            var cancel = new Button()
            {
                Text = "Cancel",
                Left = 320,
                Width = 80,
                Top = 98,
                DialogResult = DialogResult.Cancel
            };

            f.Controls.AddRange(new Control[] { lbl1, txtBase, lbl2, txtStart, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            if (f.ShowDialog(owner) != DialogResult.OK)
                return null;

            var baseName = txtBase.Text.Trim();
            if (string.IsNullOrEmpty(baseName))
                baseName = "Image";

            if (!int.TryParse(txtStart.Text.Trim(), out int startIndex))
                startIndex = 1;

            return (BaseName: baseName, StartIndex: Math.Max(0, startIndex));
        }
    }
}
