using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace PictureRenameApp
{
    public partial class Form1 : Form
    {
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
        private readonly Size ThumbnailSize = new Size(128, 128);

        // new: track current directory shown in the thumbnail panel (null when none)
        private string? currentDirectory;

        public class RenameOptions
        {
            public string BaseName { get; set; } = "Image";
            public int StartIndex { get; set; } = 1;
            public bool OverwriteExisting { get; set; } = false;
        }

        public class RenameResult
        {
            public bool Success { get; set; }
            public string SourcePath { get; set; } = string.Empty;
            public string DestinationPath { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
        }


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
            try
            {
                // Get all PictureBox controls from the thumbnail panel, keep placeholderLabel
                var toRemove = thumbnailPanel.Controls.OfType<PictureBox>().ToList();

                foreach (var pb in toRemove)
                {
                    try
                    {
                        // Safely dispose the image
                        if (pb.Image != null)
                        {
                            // Create a local reference and dispose on the correct thread
                            var img = pb.Image;
                            pb.Image = null; // First disconnect the image

                            // Only dispose if it's a bitmap and not a dummy placeholder
                            if (img != null && !img.Tag?.Equals("dummy") == true)
                            {
                                img.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing image: {ex.Message}");
                        // Continue with the next PictureBox
                    }

                    try
                    {
                        // Remove from panel and dispose the PictureBox itself
                        thumbnailPanel.Controls.Remove(pb);
                        pb.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error removing PictureBox: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearThumbnails: {ex.Message}");
            }
            finally
            {
                // Clear current directory when thumbnails are cleared
                currentDirectory = null;
            }
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
            if (sender is PictureBox pb && pb.Tag is string path && System.IO.File.Exists(path))
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
            if (sender is PictureBox pb && pb.Tag is string path && System.IO.File.Exists(path))
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


        // Main rename handler
        private void RenameButton_Click(object sender, EventArgs e)
        {
            try
            {
                var selectedFiles = GetSelectedThumbnailFilePaths();

                if (!selectedFiles.Any())
                {
                    ShowNoSelectionMessage();
                    return;
                }

                if (selectedFiles.Count == 1)
                {
                    HandleSingleFileRename(selectedFiles[0]);
                }
                else
                {
                    HandleBatchRename(selectedFiles);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error during rename: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper: get selected file paths (multi-select supported)
        private List<string> GetSelectedThumbnailFilePaths()
        {
            return thumbnailPanel.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.BackColor == Color.LightBlue && pb.Tag is string path && File.Exists(path))
                .Select(pb => pb.Tag as string ?? string.Empty)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();
        }
        private string GetSelectedThumbnailFilePath()
        {
            return thumbnailPanel.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.BackColor == Color.LightBlue && pb.Tag is string path && File.Exists(path))
                .Select(pb => pb.Tag as string ?? string.Empty)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToString() ?? string.Empty;
        }
        private void ShowNoSelectionMessage()
        {
            MessageBox.Show(
                "No files selected.\n\n" +
                "• Click a thumbnail to select a single file\n" +
                "• Hold Ctrl and click to select multiple files\n" +
                "• Hold Shift and click to select a range",
                "Rename Files",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void HandleSingleFileRename(string sourcePath)
        {
            var newName = PromptForSingleRename(sourcePath);
            if (string.IsNullOrWhiteSpace(newName)) return;

            var result = RenameSingleFile(sourcePath, newName);

            if (result.Success)
            {
                RefreshViewAfterRename(Path.GetDirectoryName(sourcePath) ?? currentDirectory ?? string.Empty);
            }
            else
            {
                MessageBox.Show($"Rename failed: {result.Error}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string? PromptForSingleRename(string sourcePath)
        {
            var defaultName = Path.GetFileNameWithoutExtension(sourcePath);

            using var dialog = new SingleRenameDialog(defaultName);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.NewFileName : null;
        }

        private RenameResult RenameSingleFile(string sourcePath, string newNameWithoutExtension)
        {
            var result = new RenameResult { SourcePath = sourcePath };

            try
            {
                var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                var extension = Path.GetExtension(sourcePath);
                var destinationPath = Path.Combine(directory, newNameWithoutExtension + extension);

                // No change needed
                if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = true;
                    result.DestinationPath = sourcePath;
                    return result;
                }

                // Check if destination exists
                if (File.Exists(destinationPath))
                {
                    var shouldOverwrite = ConfirmOverwrite(Path.GetFileName(destinationPath));
                    if (!shouldOverwrite.HasValue)
                    {
                        return result; // User cancelled
                    }
                    if (!shouldOverwrite.Value)
                    {
                        result.Success = true; // User chose to skip
                        return result;
                    }
                }

                File.Move(sourcePath, destinationPath, true);

                result.Success = true;
                result.DestinationPath = destinationPath;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        // Custom dialog for single rename
        public class SingleRenameDialog : Form
        {
            public string NewFileName { get; private set; } = string.Empty;
            private TextBox txtFileName;

            public SingleRenameDialog(string defaultName)
            {
                InitializeComponent(defaultName);
            }

            private void InitializeComponent(string defaultName)
            {
                this.StartPosition = FormStartPosition.CenterParent;
                this.Width = 420;
                this.Height = 150;
                this.Text = "Rename File";
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MinimizeBox = false;
                this.MaximizeBox = false;

                var lbl = new Label
                {
                    Left = 12,
                    Top = 10,
                    Width = 380,
                    Text = "Enter new name (without extension):"
                };

                txtFileName = new TextBox
                {
                    Left = 12,
                    Top = 32,
                    Width = 380,
                    Text = defaultName
                };
                txtFileName.SelectAll();

                var btnOk = new Button
                {
                    Text = "Rename",
                    Left = 232,
                    Width = 80,
                    Top = 64,
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Left = 320,
                    Width = 80,
                    Top = 64,
                    DialogResult = DialogResult.Cancel
                };

                this.Controls.AddRange(new Control[] { lbl, txtFileName, btnOk, btnCancel });
                this.AcceptButton = btnOk;
                this.CancelButton = btnCancel;

                btnOk.Click += (s, e) =>
                {
                    NewFileName = txtFileName.Text.Trim();
                    if (string.IsNullOrWhiteSpace(NewFileName))
                    {
                        MessageBox.Show("Please enter a valid file name.",
                            "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        this.DialogResult = DialogResult.None;
                    }
                };
            }
        }

        // Batch rename handling
        private void HandleBatchRename(List<string> selectedFiles)
        {
            var options = PromptForBatchRenameOptions();
            if (options == null) return;

            var results = RenameMultipleFiles(selectedFiles, options);
            var summary = ProcessRenameResults(results);

            if (!string.IsNullOrEmpty(summary))
            {
                //ShowRenameSummary(summary);
            }

            // Refresh view
            var directory = Path.GetDirectoryName(selectedFiles[0]) ?? currentDirectory;
            if (directory != null && Directory.Exists(directory))
            {
                LoadDirectoryThumbnails(directory);
            }
        }

        private RenameOptions? PromptForBatchRenameOptions()
        {
            using var dialog = new BatchRenameDialog();
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.GetOptions() : null;
        }

        // Custom dialog for batch rename
        public class BatchRenameDialog : Form
        {
            private TextBox txtBaseName;
            private TextBox txtStartIndex;
            private NumericUpDown numStartIndex;

            public BatchRenameDialog()
            {
                InitializeComponent();
            }

            private void InitializeComponent()
            {
                this.StartPosition = FormStartPosition.CenterParent;
                this.Width = 420;
                this.Height = 220;
                this.Text = "Batch Rename Files";
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MinimizeBox = false;
                this.MaximizeBox = false;

                // Base name
                var lblBase = new Label
                {
                    Left = 12,
                    Top = 10,
                    Width = 380,
                    Text = "Base name (will be used as: BaseName_001.ext):"
                };

                txtBaseName = new TextBox
                {
                    Left = 12,
                    Top = 32,
                    Width = 380,
                    Text = "Image"
                };
                txtBaseName.SelectAll();

                // Start index
                var lblIndex = new Label
                {
                    Left = 12,
                    Top = 62,
                    Width = 120,
                    Text = "Start index:"
                };

                numStartIndex = new NumericUpDown
                {
                    Left = 140,
                    Top = 58,
                    Width = 80,
                    Minimum = 0,
                    Maximum = 9999,
                    Value = 1
                };

                // Preview label
                var lblPreview = new Label
                {
                    Left = 12,
                    Top = 92,
                    Width = 380,
                    Text = "Preview: Image_001.jpg, Image_002.jpg, ...",
                    ForeColor = Color.Gray
                };

                // Update preview when values change
                txtBaseName.TextChanged += (s, e) => UpdatePreview(lblPreview);
                numStartIndex.ValueChanged += (s, e) => UpdatePreview(lblPreview);

                // Buttons
                var btnOk = new Button
                {
                    Text = "Rename",
                    Left = 232,
                    Width = 80,
                    Top = 132,
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Left = 320,
                    Width = 80,
                    Top = 132,
                    DialogResult = DialogResult.Cancel
                };

                this.Controls.AddRange(new Control[] {
            lblBase, txtBaseName, lblIndex, numStartIndex, lblPreview, btnOk, btnCancel
        });

                this.AcceptButton = btnOk;
                this.CancelButton = btnCancel;

                UpdatePreview(lblPreview);
            }

            private void UpdatePreview(Label previewLabel)
            {
                var baseName = string.IsNullOrWhiteSpace(txtBaseName.Text) ? "Image" : txtBaseName.Text.Trim();
                var start = (int)numStartIndex.Value;
                previewLabel.Text = $"Preview: {baseName}_{start:D3}.jpg, {baseName}_{start + 1:D3}.jpg, ...";
            }

            public RenameOptions GetOptions()
            {
                return new RenameOptions
                {
                    BaseName = string.IsNullOrWhiteSpace(txtBaseName.Text) ? "Image" : txtBaseName.Text.Trim(),
                    StartIndex = (int)numStartIndex.Value
                };
            }
        }

        private List<RenameResult> RenameMultipleFiles(List<string> files, RenameOptions options)
        {
            var results = new List<RenameResult>();
            var orderedFiles = files.OrderBy(f => f).ToList();

            for (int i = 0; i < orderedFiles.Count; i++)
            {
                var sourcePath = orderedFiles[i];
                var extension = Path.GetExtension(sourcePath);
                var index = options.StartIndex + i;
                var newFileName = $"{options.BaseName}_{index:D3}{extension}";
                var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                var destinationPath = Path.Combine(directory, newFileName);

                try
                {
                    // Check if destination exists
                    if (File.Exists(destinationPath))
                    {
                        var shouldOverwrite = ConfirmBatchOverwrite(
                            Path.GetFileName(sourcePath),
                            Path.GetFileName(destinationPath),
                            i, orderedFiles.Count);

                        if (!shouldOverwrite.HasValue)
                        {
                            break; // User cancelled the entire operation
                        }
                        if (!shouldOverwrite.Value)
                        {
                            results.Add(new RenameResult
                            {
                                SourcePath = sourcePath,
                                DestinationPath = sourcePath,
                                Success = true // Skipped
                            });
                            continue; // Skip this file
                        }
                    }

                    File.Move(sourcePath, destinationPath, true);

                    results.Add(new RenameResult
                    {
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new RenameResult
                    {
                        SourcePath = sourcePath,
                        Error = ex.Message
                    });

                    // Ask if user wants to continue after error
                    var continueResponse = MessageBox.Show(
                        $"Error renaming {Path.GetFileName(sourcePath)}: {ex.Message}\n\nContinue with remaining files?",
                        "Rename Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (continueResponse == DialogResult.No)
                    {
                        break;
                    }
                }
            }

            return results;
        }

        private bool? ConfirmOverwrite(string fileName)
        {
            var result = MessageBox.Show(
                $"File '{fileName}' already exists. Overwrite?",
                "Confirm Overwrite",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            return result switch
            {
                DialogResult.Yes => true,
                DialogResult.No => false,
                _ => null
            };
        }

        private bool? ConfirmBatchOverwrite(string sourceName, string destName, int currentIndex, int totalCount)
        {
            var result = MessageBox.Show(
                $"File {currentIndex + 1} of {totalCount}\n\n" +
                $"Source: {sourceName}\n" +
                $"Destination: {destName}\n\n" +
                "Destination already exists. Overwrite?",
                "Confirm Overwrite",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            return result switch
            {
                DialogResult.Yes => true,
                DialogResult.No => false,
                _ => null
            };
        }

        private string ProcessRenameResults(List<RenameResult> results)
        {
            var successful = results.Where(r => r.Success && r.SourcePath != r.DestinationPath).ToList();
            var skipped = results.Where(r => r.SourcePath == r.DestinationPath).ToList();
            var failed = results.Where(r => !r.Success).ToList();

            var summary = new System.Text.StringBuilder();

            if (successful.Any())
            {
                summary.AppendLine($"✅ Successfully renamed: {successful.Count} files");
            }
            if (skipped.Any())
            {
                summary.AppendLine($"⏭️ Skipped (already exist): {skipped.Count} files");
            }
            if (failed.Any())
            {
                summary.AppendLine($"❌ Failed: {failed.Count} files");
                foreach (var fail in failed.Take(5)) // Show first 5 errors
                {
                    summary.AppendLine($"   • {Path.GetFileName(fail.SourcePath)}: {fail.Error}");
                }
                if (failed.Count > 5)
                {
                    summary.AppendLine($"   ... and {failed.Count - 5} more errors");
                }
            }

            return summary.ToString();
        }

        //private void ShowRenameSummary(List<RenameResult> results)
        //{
        //    var successful = results.Where(r => r.Success && r.SourcePath != r.DestinationPath).ToList();
        //    var skipped = results.Where(r => r.SourcePath == r.DestinationPath).ToList();
        //    var failed = results.Where(r => !r.Success).ToList();

        //    var summary = new System.Text.StringBuilder();

        //    if (successful.Any())
        //        summary.AppendLine($"✅ Successfully renamed: {successful.Count} files");

        //    if (skipped.Any())
        //        summary.AppendLine($"⏭️ Skipped: {skipped.Count} files");

        //    if (failed.Any())
        //        summary.AppendLine($"❌ Failed: {failed.Count} files");

        //    var icon = failed.Any() ? MessageBoxIcon.Warning : MessageBoxIcon.Information;

        //    MessageBox.Show(summary.ToString(), "Rename Complete",
        //        MessageBoxButtons.OK, icon);
        //}

        private void RefreshViewAfterRename(string directory)
        {
            if (Directory.Exists(directory))
            {
                LoadDirectoryThumbnails(directory);
            }
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

