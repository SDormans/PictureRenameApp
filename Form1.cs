using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace PictureRenameApp
{
    public partial class Form1 : Form
    {
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
        private readonly Size ThumbnailSize = new Size(128, 128);

        // new: track current directory shown in the thumbnail panel (null when none)
        private string? currentDirectory;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomControls();
            // start with placeholder visible (no directory loaded)
            ShowPlaceholder();
        }

        private void InitializeCustomControls()
        {
            topToolStrip.Items.Add(new ToolStripButton(" 📂 Open Map", null, OpenFolder_Click));
            topToolStrip.Items.Add(new ToolStripSeparator());
            topToolStrip.Items.Add(new ToolStripButton(" ✂️ Rename", null, RenameButton_Click));
            topToolStrip.Items.Add(new ToolStripButton(" 🔍 Duplicates", null, FindDuplicates_Click));
            topToolStrip.Items.Add(new ToolStripSeparator());
            topToolStrip.Items.Add(new ToolStripButton(" 🗑️ Delete", null, ClearAll_Click));
            topToolStrip.Items.Add(new ToolStripButton(" ⚙️ Settings", null, Settings_Click));

            // ensure thumbnail list size matches panel usage
            thumbnailImageList.ImageSize = ThumbnailSize;
        }

        private void LoadDirectoryThumbnails(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                currentDirectory = null;
                ShowPlaceholder();
                return;
            }

            // set current directory as the displayed folder
            currentDirectory = directoryPath;

            ClearThumbnails();

            List<string> files;
            try
            {
                files = Directory.GetFiles(directoryPath)
                    .Where(f => SupportedImageExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => f)
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                metadataTextBox.Text = "Access denied.";
                ShowPlaceholder();
                return;
            }

            if (files.Count == 0)
            {
                metadataTextBox.Text = "No images in this directory.";
                previewPictureBox.Image?.Dispose();
                previewPictureBox.Image = null;
                ShowPlaceholder();
                return;
            }

            HidePlaceholder();

            foreach (var file in files)
            {
                var thumb = CreateThumbnailImage(file, ThumbnailSize) ?? new Bitmap(ThumbnailSize.Width, ThumbnailSize.Height);
                var pb = new PictureBox
                {
                    Width = ThumbnailSize.Width + 8,
                    Height = ThumbnailSize.Height + 8,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Image = thumb,
                    Tag = file,
                    Padding = new Padding(4),
                    Cursor = Cursors.Hand,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                pb.Click += Thumbnail_Click;
                pb.DoubleClick += Thumbnail_DoubleClick;
                var tooltip = new ToolTip();
                tooltip.SetToolTip(pb, file);
                thumbnailPanel.Controls.Add(pb);
            }

            // optionally highlight first thumbnail
            var firstPb = thumbnailPanel.Controls.OfType<PictureBox>().FirstOrDefault();
            if (firstPb != null)
            {
                firstPb.BackColor = Color.LightBlue;
                DisplayImagePreview(firstPb.Tag as string ?? string.Empty);
                DisplayFileMetadata(firstPb.Tag as string ?? string.Empty);
            }
        }

        private void ClearThumbnails()
        {
            // dispose only PictureBox thumbnails, keep placeholderLabel
            var toRemove = thumbnailPanel.Controls.OfType<PictureBox>().ToList();
            foreach (var pb in toRemove)
            {
                pb.Image?.Dispose();
                thumbnailPanel.Controls.Remove(pb);
                pb.Dispose();
            }

            // clear current directory when thumbnails are cleared
            currentDirectory = null;
        }

        private void ShowPlaceholder()
        {
            placeholderLabel.Visible = true;
            placeholderLabel.BringToFront();
        }

        private void HidePlaceholder()
        {
            placeholderLabel.Visible = false;
        }

        private void Thumbnail_Click(object? sender, EventArgs e)
        {
            if (sender is PictureBox pb && pb.Tag is string path && File.Exists(path))
            {
                // support Ctrl to toggle selection for multi-rename
                bool isCtrl = (ModifierKeys & Keys.Control) == Keys.Control;
                if (isCtrl)
                {
                    // toggle this thumbnail selection
                    pb.BackColor = pb.BackColor == Color.LightBlue ? Color.White : Color.LightBlue;
                }
                else
                {
                    // single-select (clear others)
                    foreach (Control c in thumbnailPanel.Controls.OfType<PictureBox>())
                    {
                        c.BackColor = Color.White;
                    }
                    pb.BackColor = Color.LightBlue;
                }

                DisplayImagePreview(path);
                DisplayFileMetadata(path);

                // update currentDirectory to the directory of the clicked item
                currentDirectory = Path.GetDirectoryName(path);
            }
        }

        private void Thumbnail_DoubleClick(object? sender, EventArgs e)
        {
            // open with default viewer
            if (sender is PictureBox pb && pb.Tag is string path && File.Exists(path))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch { /* ignore */ }
            }
        }

        private void DisplayImagePreview(string filePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);
                var image = Image.FromStream(ms);
                previewPictureBox.Image?.Dispose();
                previewPictureBox.Image = new Bitmap(image);
            }
            catch
            {
                previewPictureBox.Image = null;
                metadataTextBox.Text = "Could not load image.";
            }
        }

        private void DisplayFileMetadata(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var metadata = new System.Text.StringBuilder();
                metadata.AppendLine($"File: {fileInfo.Name}");
                metadata.AppendLine($"Size: {FormatBytes(fileInfo.Length)}");
                metadata.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                metadata.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");   // optional: add more metadata like EXIF if needed and the modification time is before creation time 

                metadata.AppendLine($"Path: {fileInfo.FullName}");

                if (previewPictureBox.Image != null)
                {
                    metadata.AppendLine($"Dimensions: {previewPictureBox.Image.Width} x {previewPictureBox.Image.Height}");
                    metadata.AppendLine($"Format: {GetImageFormatString(previewPictureBox.Image)}");
                }

                metadataTextBox.Text = metadata.ToString();
            }
            catch
            {
                metadataTextBox.Text = "Could not retrieve metadata.";
            }
        }

        private static string GetImageFormatString(Image imag)
        {
            try
            {
                var guid = imag.RawFormat.Guid;
                if (guid == ImageFormat.Jpeg.Guid) return "JPEG";
                if (guid == ImageFormat.Png.Guid) return "PNG";
                if (guid == ImageFormat.Gif.Guid) return "GIF";
                if (guid == ImageFormat.Bmp.Guid) return "BMP";
                if (guid == ImageFormat.Tiff.Guid) return "TIFF";
                return imag.RawFormat.ToString();
            }
            catch { return "Unknown"; }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {       
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private Image? CreateThumbnailImage(string filePath, Size size)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);
                using var src = Image.FromStream(ms);
                var bmp = new Bitmap(size.Width, size.Height);
                using var g = Graphics.FromImage(bmp);
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.Clear(Color.Transparent);
                var ratio = Math.Min((double)size.Width / src.Width, (double)size.Height / src.Height);
                var thumbW = (int)(src.Width * ratio);
                var thumbH = (int)(src.Height * ratio);
                var x = (size.Width - thumbW) / 2;
                var y = (size.Height - thumbH) / 2;
                g.DrawImage(src, x, y, thumbW, thumbH);
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private void ThumbnailPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ThumbnailPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var dropItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropItems == null || dropItems.Length == 0) return;

            // if a folder was dropped, load that folder; if files dropped, try to load images
            var folder = dropItems.FirstOrDefault(d => Directory.Exists(d));
            if (folder != null)
            {
                LoadDirectoryThumbnails(folder);
                return;
            }

            // if files dropped, filter images and add to panel (do not copy)
            var imageFiles = dropItems.Where(f => File.Exists(f) && SupportedImageExtensions.Contains(Path.GetExtension(f).ToLower())).ToList();
            if (imageFiles.Count == 0) return;

            ClearThumbnails();
            HidePlaceholder();

            foreach (var file in imageFiles)
            {
                var thumb = CreateThumbnailImage(file, ThumbnailSize) ?? new Bitmap(ThumbnailSize.Width, ThumbnailSize.Height);
                var pb = new PictureBox
                {
                    Width = ThumbnailSize.Width + 8,
                    Height = ThumbnailSize.Height + 8,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Image = thumb,
                    Tag = file,
                    Padding = new Padding(4),
                    Cursor = Cursors.Hand,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                pb.Click += Thumbnail_Click;
                pb.DoubleClick += Thumbnail_DoubleClick;
                var tooltip = new ToolTip();
                tooltip.SetToolTip(pb, file);
                thumbnailPanel.Controls.Add(pb);
            }

            // record currentDirectory as the parent of the first dropped file (useful for Open)
            currentDirectory = Path.GetDirectoryName(imageFiles[0]);

            // show first dropped image
            DisplayImagePreview(imageFiles[0]);
            DisplayFileMetadata(imageFiles[0]);
        }

        private void OpenFolder_Click(object sender, EventArgs e)
        {
            // if a thumbnail is selected, open its folder; otherwise open the currentDirectory; fallback to folder browser.
            string? selectedFile = GetSelectedThumbnailFilePath();
            string? dirToOpen = null;

            if (!string.IsNullOrEmpty(selectedFile) && File.Exists(selectedFile))
            {
                dirToOpen = Path.GetDirectoryName(selectedFile);
            }
            else if (!string.IsNullOrEmpty(currentDirectory) && Directory.Exists(currentDirectory))
            {
                dirToOpen = currentDirectory;
            }

            if (!string.IsNullOrEmpty(dirToOpen) && Directory.Exists(dirToOpen))
            {
                // load thumbnails in-app so the user sees content immediately
                try
                {
                    LoadDirectoryThumbnails(dirToOpen);
                }
                catch
                {
                    /* ignore loading errors, still try to open Explorer */
                }

                // also open Explorer at that directory (preserve previous behavior)
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = dirToOpen,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch
                {
                    // ignore explorer-launch errors
                }

                return;
            }

            // fallback: let user pick a folder =>duurt tering lang
            using var dlg = new FolderBrowserDialog { Description = "Select folder" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadDirectoryThumbnails(dlg.SelectedPath);
            }
        }

        // helper: find the file path of the currently selected thumbnail (background highlight)
        private string? GetSelectedThumbnailFilePath()
        {
            var pb = thumbnailPanel.Controls.OfType<PictureBox>().FirstOrDefault(p => p.BackColor == Color.LightBlue);
            return pb?.Tag as string;
        }

        private void RenameButton_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedThumbnailFilePaths();
            if (selected.Count == 0)
            {
                MessageBox.Show("No files selected. Hold Ctrl and click thumbnails to select multiple, or click one thumbnail and try again.", "Rename", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selected.Count == 1)
            {
                // single rename
                var src = selected[0];
                var dir = Path.GetDirectoryName(src) ?? currentDirectory;
                var ext = Path.GetExtension(src);
                var defaultName = Path.GetFileNameWithoutExtension(src);
                var newName = PromptForString("Rename file", "Enter new name (without extension):", defaultName);
                if (string.IsNullOrWhiteSpace(newName)) return;

                var dest = Path.Combine(dir ?? string.Empty, newName + ext);
                if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) return;

                if (File.Exists(dest))
                {
                    var ans = MessageBox.Show($"File {Path.GetFileName(dest)} already exists. Overwrite?", "Overwrite", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (ans != DialogResult.Yes) return;
                }

                try
                {
                    File.Move(src, dest, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Rename failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // refresh view
                LoadDirectoryThumbnails(dir ?? currentDirectory ?? Path.GetDirectoryName(dest) ?? string.Empty);
                return;
            }

            // multiple rename: ask for base name and start index
            var batchInfo = PromptForBatchRename();
            if (batchInfo == null) return;

            string baseName = batchInfo.Value.BaseName;
            int startIndex = batchInfo.Value.StartIndex;
            bool aborted = false;

            var dirForReload = Path.GetDirectoryName(selected[0]) ?? currentDirectory;

            // sort selected for consistent ordering
            var ordered = selected.OrderBy(s => s).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var src = ordered[i];
                var ext = Path.GetExtension(src);
                int idx = startIndex + i;
                var destName = $"{baseName}_{idx}{ext}";
                var dest = Path.Combine(Path.GetDirectoryName(src) ?? dirForReload ?? string.Empty, destName);

                try
                {
                    if (File.Exists(dest))
                    {
                        var ans = MessageBox.Show($"Target file {destName} already exists. Overwrite?\nSource: {Path.GetFileName(src)}", "Overwrite", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (ans == DialogResult.Cancel)
                        {
                            aborted = true;
                            break;
                        }
                        if (ans == DialogResult.No)
                        {
                            continue; // skip this file
                        }
                        // else overwrite
                    }

                    File.Move(src, dest, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to rename {Path.GetFileName(src)}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // reload directory thumbnails so names update
            if (dirForReload != null && Directory.Exists(dirForReload))
                LoadDirectoryThumbnails(dirForReload);
        }

        // helper: returns file paths of selected thumbnails (multi-select supported via Ctrl toggles)
        private List<string> GetSelectedThumbnailFilePaths()
        {
            return thumbnailPanel.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.BackColor == Color.LightBlue && pb.Tag is string && File.Exists(pb.Tag as string))
                .Select(pb => pb.Tag as string ?? string.Empty)
                .ToList();
        }

        // simple prompt for single string
        private string? PromptForString(string title, string prompt, string defaultValue = "")
        {
            using var f = new Form()
            {
                StartPosition = FormStartPosition.CenterParent,
                Width = 420,
                Height = 140,
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false
            };
            var lbl = new Label() { Left = 12, Top = 10, Width = 380, Text = prompt };
            var txt = new TextBox() { Left = 12, Top = 32, Width = 380, Text = defaultValue };
            var ok = new Button() { Text = "OK", Left = 232, Width = 80, Top = 64, DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "Cancel", Left = 320, Width = 80, Top = 64, DialogResult = DialogResult.Cancel };
            f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            return f.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
        }

        private (string BaseName, int StartIndex)? PromptForBatchRename()
        {
            using var f = new Form()
            {
                StartPosition = FormStartPosition.CenterParent,
                Width = 420,
                Height = 180,
                Text = "Batch rename",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false
            };

            var lbl1 = new Label() { Left = 12, Top = 10, Width = 380, Text = "Base name (will be used as BaseName_index.ext):" };
            var txtBase = new TextBox() { Left = 12, Top = 32, Width = 380, Text = "Image" };

            var lbl2 = new Label() { Left = 12, Top = 62, Width = 200, Text = "Start index:" };
            var txtStart = new TextBox() { Left = 220, Top = 58, Width = 60, Text = "1" };

            var ok = new Button() { Text = "OK", Left = 232, Width = 80, Top = 98, DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "Cancel", Left = 320, Width = 80, Top = 98, DialogResult = DialogResult.Cancel };

            f.Controls.AddRange(new Control[] { lbl1, txtBase, lbl2, txtStart, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            if (f.ShowDialog(this) != DialogResult.OK) return null;

            var baseName = txtBase.Text.Trim();
            if (string.IsNullOrEmpty(baseName)) baseName = "Image";
            if (!int.TryParse(txtStart.Text.Trim(), out int startIndex)) startIndex = 1;

            return (BaseName: baseName, StartIndex: Math.Max(0, startIndex));
        }

        private void RenameButton_Click_OLD(object sender, EventArgs e)
        {
            MessageBox.Show("Hernoem wordt later geïmplementeerd");
        }

        private void RenameButton_Click_PLACEHOLDER(object sender, EventArgs e)
        {
            // kept for reference, not used.
        }

        private void RenameButton_Click_DEPRECATED(object sender, EventArgs e)
        {
            // kept for reference, not used.
        }

        private void FindDuplicates_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Duplicaten zoeken wordt later geïmplementeerd");
        }

        private void ClearAll_Click(object sender, EventArgs e)
        {
            // clear thumbnails and preview and show placeholder
            ClearThumbnails();
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;
            metadataTextBox.Clear();
            ShowPlaceholder();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Instellingen wordt later geïmplementeerd");
        }
    }
}

        