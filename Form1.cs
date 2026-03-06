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
                DisplayImagePreview(path);
                DisplayFileMetadata(path);
                // highlight selected thumbnail
                foreach (Control c in thumbnailPanel.Controls.OfType<PictureBox>())
                {
                    c.BackColor = Color.White;
                }
                pb.BackColor = Color.LightBlue;

                // update currentDirectory to the directory of the selected item
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

        private static string GetImageFormatString(Image img)
        {
            try
            {
                var guid = img.RawFormat.Guid;
                if (guid == ImageFormat.Jpeg.Guid) return "JPEG";
                if (guid == ImageFormat.Png.Guid) return "PNG";
                if (guid == ImageFormat.Gif.Guid) return "GIF";
                if (guid == ImageFormat.Bmp.Guid) return "BMP";
                if (guid == ImageFormat.Tiff.Guid) return "TIFF";
                return img.RawFormat.ToString();
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
            MessageBox.Show("Hernoem wordt later geïmplementeerd");
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

