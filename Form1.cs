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

        public Form1()
        {
            InitializeComponent();
            InitializeCustomControls();
            LoadRootDrives();
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
        }

        private void LoadRootDrives()
        {
            directoryTreeView.Nodes.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    var driveNode = new TreeNode(drive.Name) { Tag = drive.RootDirectory.FullName };
                    driveNode.Nodes.Add(new TreeNode("")); // dummy for lazy load
                    directoryTreeView.Nodes.Add(driveNode);
                }
            }
        }

        private void DirectoryTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is string path)
            {
                if (File.Exists(path))
                {
                    // selected a file node
                    DisplayImagePreview(path);
                    DisplayFileMetadata(path);
                }
                else if (Directory.Exists(path))
                {
                    DisplayFilesForDirectory(path);
                }
            }
        }

        private void DisplayFilesForDirectory(string directoryPath)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath)
                    .Where(f => SupportedImageExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (files.Count > 0)
                {
                    var firstFile = files[0];
                    DisplayImagePreview(firstFile);
                    DisplayFileMetadata(firstFile);
                }
                else
                {
                    previewPictureBox.Image = null;
                    metadataTextBox.Text = "No images in this directory.";
                }
            }
            catch (UnauthorizedAccessException)
            {
                metadataTextBox.Text = "Access denied.";
            }
        }

        private void DisplayImagePreview(string filePath)
        {
            try
            {
                // load image into memory to avoid locking the file
                var bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);
                var image = Image.FromStream(ms);
                // clone to avoid stream dependencies
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
                metadata.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
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

        private void DirectoryTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "")
            {
                e.Node.Nodes.Clear();
                var path = e.Node.Tag as string;
                try
                {
                    // add subdirectories
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var subNode = new TreeNode(Path.GetFileName(dir)) { Tag = dir };
                        subNode.Nodes.Add(new TreeNode("")); // dummy
                        e.Node.Nodes.Add(subNode);
                    }

                    // add image files with thumbnails
                    var files = Directory.GetFiles(path)
                        .Where(f => SupportedImageExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();

                    if (files.Count > 0)
                    {
                        e.Node.Nodes.Add(new TreeNode("--- Images ---") { ForeColor = Color.Gray, Tag = null });
                        foreach (var file in files)
                        {
                            var fileNode = new TreeNode(Path.GetFileName(file)) { Tag = file };
                            EnsureThumbnail(file);
                            // set image keys to the file path (unique)
                            fileNode.ImageKey = file;
                            fileNode.SelectedImageKey = file;
                            fileNode.ToolTipText = file;
                            e.Node.Nodes.Add(fileNode);
                        }
                    }
                }
                catch { }
            }
        }

        private void EnsureThumbnail(string file)
        {
            if (thumbnailImageList == null) return;
            try
            {
                if (thumbnailImageList.Images.IndexOfKey(file) >= 0) return;
                var thumb = CreateThumbnailImage(file, thumbnailImageList.ImageSize);
                if (thumb != null)
                {
                    thumbnailImageList.Images.Add(file, thumb);
                }
            }
            catch { /* ignore thumbnail errors */ }
        }

        private Image CreateThumbnailImage(string filePath, Size size)
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
                // preserve aspect ratio
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

        private void DirectoryTreeView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void DirectoryTreeView_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var dropItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropItems == null || dropItems.Length == 0) return;

            // determine node under drop point
            var clientPoint = directoryTreeView.PointToClient(new Point(e.X, e.Y));
            var targetNode = directoryTreeView.GetNodeAt(clientPoint);

            directoryTreeView.BeginUpdate();
            try
            {
                foreach (var path in dropItems)
                {
                    if (Directory.Exists(path))
                    {
                        var node = new TreeNode(Path.GetFileName(path)) { Tag = path };
                        node.Nodes.Add(new TreeNode("")); // dummy for lazy load
                        if (targetNode != null && Directory.Exists(targetNode.Tag as string))
                            targetNode.Nodes.Add(node);
                        else
                            directoryTreeView.Nodes.Add(node);
                    }
                    else if (File.Exists(path))
                    {
                        var ext = Path.GetExtension(path).ToLower();
                        if (!SupportedImageExtensions.Contains(ext))
                            continue;
                        var node = new TreeNode(Path.GetFileName(path)) { Tag = path };
                        EnsureThumbnail(path);
                        node.ImageKey = path;
                        node.SelectedImageKey = path;
                        node.ToolTipText = path;
                        if (targetNode != null && Directory.Exists(targetNode.Tag as string))
                            targetNode.Nodes.Add(node);
                        else
                            directoryTreeView.Nodes.Add(node);
                    }
                }
            }
            finally
            {
                directoryTreeView.EndUpdate();
            }
        }

        private void OpenFolder_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Open Map wordt later geïmplementeerd");
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
            MessageBox.Show("Wissen wordt later geïmplementeerd");
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Instellingen wordt later geïmplementeerd");
        }
    }
}

