using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using PictureRenameApp.Services;
using PictureRenameApp.Utilities;

namespace PictureRenameApp
{
    /// <summary>
    /// Main application form for the Picture Rename application.
    /// Handles UI interactions, file management, and renaming operations.
    /// </summary>
    public partial class Form1 : Form
    {
        // Injected services
        private readonly IApplicationLogger _logger;
        private readonly IImageService _imageService;
        private readonly IFileService _fileService;

        // UI state
        private string? _currentDirectory;
        private readonly Size _thumbnailSize = new Size(128, 128);

        /// <summary>
        /// Initializes a new instance of Form1 with dependency-injected services.
        /// </summary>
        /// <param name="logger">Logger for application diagnostics</param>
        /// <param name="imageService">Service for image operations</param>
        /// <param name="fileService">Service for file operations</param>
        public Form1(IApplicationLogger logger, IImageService imageService, IFileService fileService)
        {
            InitializeComponent();

            // Store injected dependencies
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            _logger.LogInfo("Application started");

            InitializeCustomControls();
            ShowPlaceholder();
        }

        /// <summary>
        /// Initializes toolbar buttons and UI configuration.
        /// </summary>
        private void InitializeCustomControls()
        {
            try
            {
                topToolStrip.Items.Add(new ToolStripButton(" 📂 Open Folder", null, OpenFolder_Click));
                topToolStrip.Items.Add(new ToolStripSeparator());
                topToolStrip.Items.Add(new ToolStripButton(" ✂️ Rename", null, RenameButton_Click));
                topToolStrip.Items.Add(new ToolStripButton(" 🔍 Duplicates", null, FindDuplicates_Click));
                topToolStrip.Items.Add(new ToolStripSeparator());
                topToolStrip.Items.Add(new ToolStripButton(" 🗑️ Clear", null, ClearAll_Click));
                topToolStrip.Items.Add(new ToolStripButton(" ⚙️ Settings", null, Settings_Click));

                _logger.LogDebug("UI controls initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize UI controls", ex);
                MessageBox.Show("Failed to initialize application UI", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads and displays thumbnail images for a directory.
        /// </summary>
        /// <param name="directoryPath">Full path to directory to scan</param>
        private void LoadDirectoryThumbnails(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                _logger.LogWarning($"Invalid directory path: {directoryPath}");
                _currentDirectory = null;
                ShowPlaceholder();
                return;
            }

            try
            {
                _logger.LogInfo($"Loading directory: {directoryPath}");
                _currentDirectory = directoryPath;

                ClearThumbnails();

                var files = _fileService.GetImageFilesInDirectory(directoryPath);

                if (files.Count == 0)
                {
                    _logger.LogInfo("No images found in directory");
                    metadataTextBox.Text = "No images in this directory.";
                    previewPictureBox.Image?.Dispose();
                    previewPictureBox.Image = null;
                    ShowPlaceholder();
                    return;
                }

                HidePlaceholder();

                foreach (var file in files)
                {
                    try
                    {
                        var thumb = _imageService.CreateThumbnailImage(file, _thumbnailSize) 
                            ?? new Bitmap(_thumbnailSize.Width, _thumbnailSize.Height);
                        
                        var pb = new PictureBox
                        {
                            Width = _thumbnailSize.Width + 8,
                            Height = _thumbnailSize.Height + 8,
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
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create thumbnail for {Path.GetFileName(file)}", ex);
                    }
                }

                // Select and display first thumbnail
                var firstPb = thumbnailPanel.Controls.OfType<PictureBox>().FirstOrDefault();
                if (firstPb != null)
                {
                    firstPb.BackColor = Color.LightBlue;
                    DisplayImagePreview(firstPb.Tag as string ?? string.Empty);
                    DisplayFileMetadata(firstPb.Tag as string ?? string.Empty);
                }

                _logger.LogInfo($"Successfully loaded {files.Count} thumbnails");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading directory thumbnails", ex);
                MessageBox.Show("Failed to load directory. Please check the logs for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowPlaceholder();
            }
        }

        /// <summary>
        /// Clears all thumbnail controls and resets directory tracking.
        /// </summary>
        private void ClearThumbnails()
        {
            var toRemove = thumbnailPanel.Controls.OfType<PictureBox>().ToList();
            foreach (var pb in toRemove)
            {
                try
                {
                    thumbnailPanel.Controls.Remove(pb);
                    pb.Image?.Dispose();
                    pb.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disposing thumbnail control", ex);
                }
            }

            _currentDirectory = null;
        }

        /// <summary>
        /// Shows the placeholder label indicating no content is loaded.
        /// </summary>
        private void ShowPlaceholder()
        {
            placeholderLabel.Visible = true;
            placeholderLabel.BringToFront();
        }

        /// <summary>
        /// Hides the placeholder label.
        /// </summary>
        private void HidePlaceholder()
        {
            placeholderLabel.Visible = false;
        }

        /// <summary>
        /// Handles thumbnail click events with Ctrl support for multi-select.
        /// </summary>
        private void Thumbnail_Click(object? sender, EventArgs e)
        {
            if (sender is not PictureBox pb || pb.Tag is not string path || !File.Exists(path))
                return;

            try
            {
                // Support Ctrl for multi-select
                bool isCtrl = (ModifierKeys & Keys.Control) == Keys.Control;

                if (isCtrl)
                {
                    // Toggle selection
                    pb.BackColor = pb.BackColor == Color.LightBlue ? Color.White : Color.LightBlue;
                }
                else
                {
                    // Single select - deselect all others
                    foreach (Control c in thumbnailPanel.Controls.OfType<PictureBox>())
                    {
                        c.BackColor = Color.White;
                    }
                    pb.BackColor = Color.LightBlue;
                }

                DisplayImagePreview(path);
                DisplayFileMetadata(path);
                _currentDirectory = Path.GetDirectoryName(path);

                _logger.LogDebug($"Thumbnail selected: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error handling thumbnail click", ex);
            }
        }

        /// <summary>
        /// Handles double-click on thumbnails to open with default viewer.
        /// </summary>
        private void Thumbnail_DoubleClick(object? sender, EventArgs e)
        {
            if (sender is not PictureBox pb || pb.Tag is not string path || !File.Exists(path))
                return;

            try
            {
                _logger.LogInfo($"Opening file with default viewer: {Path.GetFileName(path)}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open file with default viewer", ex);
                MessageBox.Show("Could not open file with default viewer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Displays the selected image in the preview panel.
        /// </summary>
        private void DisplayImagePreview(string filePath)
        {
            try
            {
                var image = _imageService.LoadImageFromFile(filePath);
                previewPictureBox.Image?.Dispose();
                previewPictureBox.Image = image;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to display image preview", ex);
                previewPictureBox.Image = null;
                metadataTextBox.Text = "Could not load image preview.";
            }
        }

        /// <summary>
        /// Displays file metadata including size, dates, and image properties.
        /// </summary>
        private void DisplayFileMetadata(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var metadata = new System.Text.StringBuilder();

                metadata.AppendLine($"File: {fileInfo.Name}");
                metadata.AppendLine($"Size: {_fileService.FormatFileSize(fileInfo.Length)}");
                metadata.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                metadata.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                metadata.AppendLine($"Path: {fileInfo.FullName}");

                if (previewPictureBox.Image != null)
                {
                    metadata.AppendLine($"Dimensions: {previewPictureBox.Image.Width} x {previewPictureBox.Image.Height}");
                    metadata.AppendLine($"Format: {_imageService.GetImageFormatString(previewPictureBox.Image)}");
                }

                metadataTextBox.Text = metadata.ToString();

                _logger.LogDebug($"Metadata displayed for: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to display file metadata", ex);
                metadataTextBox.Text = "Could not retrieve file metadata.";
            }
        }

        /// <summary>
        /// Handles drag-enter event to enable drop feedback.
        /// </summary>
        private void ThumbnailPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// Handles drag-drop event to load directories or image files.
        /// </summary>
        private void ThumbnailPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return;

            var dropItems = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropItems == null || dropItems.Length == 0)
                return;

            try
            {
                _logger.LogInfo($"Files/folders dropped ({dropItems.Length} items)");

                // Prefer folders over files
                var folder = dropItems.FirstOrDefault(d => Directory.Exists(d));
                if (folder != null)
                {
                    LoadDirectoryThumbnails(folder);
                    return;
                }

                // If only files dropped, load their images
                var supportedExtensions = _imageService.GetSupportedExtensions();
                var imageFiles = dropItems
                    .Where(f => File.Exists(f) && supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (imageFiles.Count == 0)
                {
                    _logger.LogWarning("No supported image files found in dropped items");
                    return;
                }

                ClearThumbnails();
                HidePlaceholder();

                foreach (var file in imageFiles)
                {
                    try
                    {
                        var thumb = _imageService.CreateThumbnailImage(file, _thumbnailSize) 
                            ?? new Bitmap(_thumbnailSize.Width, _thumbnailSize.Height);
                        
                        var pb = new PictureBox
                        {
                            Width = _thumbnailSize.Width + 8,
                            Height = _thumbnailSize.Height + 8,
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
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to create thumbnail for dropped file", ex);
                    }
                }

                _currentDirectory = Path.GetDirectoryName(imageFiles[0]);
                DisplayImagePreview(imageFiles[0]);
                DisplayFileMetadata(imageFiles[0]);

                _logger.LogInfo($"Successfully loaded {imageFiles.Count} dropped image(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error handling drag-drop operation", ex);
                MessageBox.Show("Failed to load dropped files. Please check the logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles Open Folder button click to select and load a directory.
        /// </summary>
        private void OpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                string? selectedFile = GetSelectedThumbnailFilePath();
                string? dirToOpen = null;

                // Determine directory to open based on current selection
                if (!string.IsNullOrEmpty(selectedFile) && File.Exists(selectedFile))
                {
                    dirToOpen = Path.GetDirectoryName(selectedFile);
                }
                else if (!string.IsNullOrEmpty(_currentDirectory) && Directory.Exists(_currentDirectory))
                {
                    dirToOpen = _currentDirectory;
                }

                // If we have a directory, load it
                if (!string.IsNullOrEmpty(dirToOpen) && Directory.Exists(dirToOpen))
                {
                    try
                    {
                        LoadDirectoryThumbnails(dirToOpen);
                        _logger.LogInfo($"Loaded directory via Open Folder: {dirToOpen}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to load directory", ex);
                    }

                    // Also open in Explorer
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = dirToOpen,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Could not open Explorer window: {ex.Message}");
                    }

                    return;
                }

                // Fallback: show folder browser dialog
                using var dlg = new FolderBrowserDialog { Description = "Select folder with images" };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadDirectoryThumbnails(dlg.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in OpenFolder_Click", ex);
                MessageBox.Show("An error occurred while opening a folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets the file path of the currently selected thumbnail.
        /// </summary>
        private string? GetSelectedThumbnailFilePath()
        {
            var pb = thumbnailPanel.Controls.OfType<PictureBox>().FirstOrDefault(p => p.BackColor == Color.LightBlue);
            return pb?.Tag as string;
        }

        /// <summary>
        /// Gets all selected thumbnail file paths (supports multi-select via Ctrl).
        /// </summary>
        private List<string> GetSelectedThumbnailFilePaths()
        {
            return thumbnailPanel.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.BackColor == Color.LightBlue && pb.Tag is string && File.Exists(pb.Tag as string))
                .Select(pb => pb.Tag as string ?? string.Empty)
                .ToList();
        }

        /// <summary>
        /// Handles the Rename button click for single or batch rename operations.
        /// Supports format: BaseName_001, BaseName_002, etc.
        /// </summary>
        private void RenameButton_Click(object sender, EventArgs e)
        {
            try
            {
                var selected = GetSelectedThumbnailFilePaths();

                if (selected.Count == 0)
                {
                    _logger.LogInfo("Rename attempted with no files selected");
                    MessageBox.Show(
                        "No files selected. Hold Ctrl and click thumbnails to select multiple, or click one thumbnail.",
                        "Rename",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (selected.Count == 1)
                {
                    RenameSingleFile(selected[0]);
                }
                else
                {
                    RenameBatchFiles(selected);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in RenameButton_Click", ex);
                MessageBox.Show("An error occurred during rename. Please check the logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Renames a single file with user-provided name.
        /// </summary>
        private void RenameSingleFile(string sourcePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(sourcePath) ?? _currentDirectory;
                var ext = Path.GetExtension(sourcePath);
                var defaultName = Path.GetFileNameWithoutExtension(sourcePath);

                var newName = DialogHelper.PromptForString(this, "Rename File", "Enter new name (without extension):", defaultName);
                if (string.IsNullOrWhiteSpace(newName))
                {
                    _logger.LogInfo("Rename cancelled by user");
                    return;
                }

                var dest = Path.Combine(dir ?? string.Empty, newName + ext);

                if (string.Equals(sourcePath, dest, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo("Source and destination are the same, operation cancelled");
                    return;
                }

                if (File.Exists(dest))
                {
                    var ans = MessageBox.Show(
                        $"File {Path.GetFileName(dest)} already exists. Overwrite?",
                        "File Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (ans != DialogResult.Yes)
                    {
                        _logger.LogInfo("Overwrite declined by user");
                        return;
                    }
                }

                _fileService.RenameFile(sourcePath, dest, overwrite: true);
                _logger.LogInfo($"File renamed successfully: {Path.GetFileName(sourcePath)} → {Path.GetFileName(dest)}");

                // Refresh display
                if (dir != null && Directory.Exists(dir))
                    LoadDirectoryThumbnails(dir);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error renaming single file", ex);
                MessageBox.Show($"Rename failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Renames multiple selected files with pattern: BaseName_###.ext
        /// </summary>
        private void RenameBatchFiles(List<string> selectedFiles)
        {
            try
            {
                var batchInfo = DialogHelper.PromptForBatchRename(this);
                if (batchInfo == null)
                {
                    _logger.LogInfo("Batch rename cancelled by user");
                    return;
                }

                string baseName = batchInfo.Value.BaseName;
                int startIndex = batchInfo.Value.StartIndex;
                var dirForReload = Path.GetDirectoryName(selectedFiles[0]) ?? _currentDirectory;

                // Sort files for consistent ordering
                var ordered = selectedFiles.OrderBy(s => s).ToList();

                _logger.LogInfo($"Starting batch rename of {ordered.Count} files with base name '{baseName}'");

                int successCount = 0;
                for (int i = 0; i < ordered.Count; i++)
                {
                    var src = ordered[i];
                    var ext = Path.GetExtension(src);
                    int idx = startIndex + i;
                    var destName = $"{baseName}_{idx:D3}{ext}"; // Format with leading zeros
                    var dest = Path.Combine(Path.GetDirectoryName(src) ?? dirForReload ?? string.Empty, destName);

                    try
                    {
                        if (File.Exists(dest))
                        {
                            var ans = MessageBox.Show(
                                $"Target file {destName} already exists. Overwrite?\nSource: {Path.GetFileName(src)}",
                                "File Exists",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question);

                            if (ans == DialogResult.Cancel)
                            {
                                _logger.LogInfo("Batch rename cancelled by user");
                                break;
                            }

                            if (ans == DialogResult.No)
                            {
                                _logger.LogInfo($"Skipped: {Path.GetFileName(src)}");
                                continue;
                            }
                        }

                        _fileService.RenameFile(src, dest, overwrite: true);
                        successCount++;
                        _logger.LogInfo($"Renamed: {Path.GetFileName(src)} → {destName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to rename file: {Path.GetFileName(src)}", ex);
                        MessageBox.Show(
                            $"Failed to rename {Path.GetFileName(src)}: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }

                MessageBox.Show(
                    $"Batch rename completed.\nSuccessfully renamed: {successCount}/{ordered.Count} files",
                    "Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Refresh display
                if (dirForReload != null && Directory.Exists(dirForReload))
                    LoadDirectoryThumbnails(dirForReload);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in batch rename", ex);
                MessageBox.Show($"Batch rename failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles Find Duplicates button (placeholder for future implementation).
        /// </summary>
        private void FindDuplicates_Click(object sender, EventArgs e)
        {
            _logger.LogInfo("Find Duplicates clicked");
            MessageBox.Show("Duplicate detection will be implemented in a future version.", "Feature Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Clears all thumbnails and resets the UI.
        /// </summary>
        private void ClearAll_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInfo("Clear All clicked");
                ClearThumbnails();
                previewPictureBox.Image?.Dispose();
                previewPictureBox.Image = null;
                metadataTextBox.Clear();
                ShowPlaceholder();
                _logger.LogInfo("UI cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error clearing UI", ex);
            }
        }

        /// <summary>
        /// Handles Settings button (placeholder for future implementation).
        /// </summary>
        private void Settings_Click(object sender, EventArgs e)
        {
            _logger.LogInfo("Settings clicked");
            MessageBox.Show("Settings will be available in a future version.", "Feature Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}