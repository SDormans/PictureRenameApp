using PictureRenameApp.Services;
using PictureRenameApp.Models;
using PictureRenameApp.Controllers;
using PictureRenameApp.UI;
using System.Drawing.Drawing2D;
using PictureRenameApp.Utilities;

namespace PictureRenameApp
{
    /// <summary>
    /// Main application form for the Picture Rename application.
    /// Delegates business logic to the controller (MVC pattern) and UI operations to specialized handlers.
    /// Responsibilities: Coordinate UI components, handle user input, display data.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// Test-friendly constructor that allows skipping UI initialization.
        /// When <paramref name="initializeComponents"/> is false the form will not
        /// create UI controls or wire up managers. This is intended for unit tests
        /// that need to call non-UI logic such as `CreateThumbnailImage` without
        /// requiring a Windows message loop.
        /// </summary>
        public Form1(IApplicationLogger logger, IApplicationController controller, bool initializeComponents)
        {
            // Basic validation and minimal wiring for logic-only usage in tests
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _model = controller.GetModel() ?? throw new ArgumentNullException(nameof(_model));

            if (initializeComponents)
            {
                InitializeComponent();

                _thumbnailPanelManager = new ThumbnailPanelManager(thumbnailPanel, _thumbnailSize, _logger);
                _thumbnailSelector = new ThumbnailSelector(_model, _logger);
                _fileInteractionHandler = new FileInteractionHandler(_logger);

                _logger.LogInfo("Application started");

                InitializeCustomControls();
                SubscribeToModelChanges();
                ShowPlaceholder();
            }
        }

        // Injected dependencies
        private readonly IApplicationLogger _logger;
        private readonly IApplicationController _controller;
        private readonly IApplicationModel _model;
        private readonly IThumbnailPanelManager _thumbnailPanelManager;
        private readonly IThumbnailSelector _thumbnailSelector;
        private readonly IFileInteractionHandler _fileInteractionHandler;

        // UI configuration
        private readonly Size _thumbnailSize = new Size(128, 128);

        /// <summary>
        /// Initializes a new instance of Form1 with dependency-injected services.
        /// </summary>
        /// <param name="logger">Logger for application diagnostics</param>
        /// <param name="controller">Application controller for business logic</param>
        public Form1(IApplicationLogger? logger, IApplicationController? controller)
        {
            InitializeComponent();

            // Validate and store dependencies
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _model = controller?.GetModel() ?? throw new ArgumentNullException(nameof(_model));

            // Initialize UI component handlers
            _thumbnailPanelManager = new ThumbnailPanelManager(thumbnailPanel, _thumbnailSize, _logger);
            _thumbnailSelector = new ThumbnailSelector(_model, _logger);
            _fileInteractionHandler = new FileInteractionHandler(_logger);

            _logger.LogInfo("Application started");

            InitializeCustomControls();
            SubscribeToModelChanges();
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

        // Debounce timer to prevent multiple concurrent RefreshThumbnailsUI calls during batch loading
        private System.Windows.Forms.Timer? _refreshDebounceTimer;
        private const int RefreshDebounceMs = 100;

        /// <summary>
        /// Subscribes to model change notifications for UI updates.
        /// Uses debouncing to prevent race conditions during rapid collection changes.
        /// </summary>
        private void SubscribeToModelChanges()
        {
            _model.Thumbnails.CollectionChanged += (sender, e) => DebounceRefreshThumbnailsUI();
        }

        /// <summary>
        /// Debounces RefreshThumbnailsUI to prevent concurrent disposal issues.
        /// Resets the timer on each collection change, ensuring refresh only occurs after a quiet period.
        /// </summary>
        private void DebounceRefreshThumbnailsUI()
        {
            // Stop existing timer if running
            _refreshDebounceTimer?.Stop();
            _refreshDebounceTimer?.Dispose();

            // Create new timer to refresh after debounce period
            _refreshDebounceTimer = new System.Windows.Forms.Timer
            {
                Interval = RefreshDebounceMs
            };

            _refreshDebounceTimer.Tick += (s, e) =>
            {
                _refreshDebounceTimer.Stop();
                RefreshThumbnailsUI();
            };

            _refreshDebounceTimer.Start();
        }

        /// <summary>
        /// Refreshes the thumbnail panel UI based on model state.
        /// Thread-safe: Uses Invoke to marshal calls to the UI thread.
        /// </summary>
        private void RefreshThumbnailsUI()
        {
            if (InvokeRequired)
            {
                Invoke((Action)RefreshThumbnailsUI);
                return;
            }

            try
            {
                _thumbnailPanelManager.Clear();

                foreach (var thumbnail in _model.Thumbnails)
                {
                    _thumbnailPanelManager.AddThumbnail(
                        thumbnail,
                        Thumbnail_Click,
                        Thumbnail_DoubleClick);
                }

                metadataTextBox.Text = _model.MetadataText;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing thumbnails UI", ex);
            }
        }


        /// Shows the placeholder label indicating no content is loaded.
        /// </summary>
        private void ShowPlaceholder()
        {
            placeholderLabel.Visible = true;
            placeholderLabel.BringToFront();
            placeholderLabel.AutoSize = false;
            placeholderLabel.Dock = DockStyle.None;
            placeholderLabel.Size = new Size(100, 23);
            // Remove the Size assignment - Dock = DockStyle.Fill will handle sizing
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
            if (!ValidateThumbnailClickSender(sender, out var filePath))
                return;

            try
            {
                SelectAndDisplayThumbnail(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error handling thumbnail click", ex);
            }
        }

        /// <summary>
        /// Validates the thumbnail click sender and extracts the file path.
        /// </summary>
        /// <param name="sender">The event sender (should be a PictureBox)</param>
        /// <param name="filePath">The extracted file path if validation succeeds</param>
        /// <returns>True if sender is valid and file exists; otherwise false</returns>
        private bool ValidateThumbnailClickSender(object? sender, out string filePath)
        {
            filePath = string.Empty;

            if (sender is not PictureBox pictureBox)
                return false;

            if (pictureBox.Tag is not string path)
                return false;

            if (!File.Exists(path))
                return false;

            filePath = path;
            return true;
        }

        /// <summary>
        /// Selects the thumbnail and displays the associated file.
        /// </summary>
        /// <param name="filePath">The path to the selected file</param>
        private async void SelectAndDisplayThumbnail(string filePath)
        {
            // Update selection in model
            bool isCtrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
            _thumbnailSelector.SelectThumbnail(filePath, isCtrlPressed);

            // Update current directory
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                _model.CurrentDirectory = directory;

            // Refresh UI to reflect new selection state
            RefreshThumbnailsUI();

            // Display the selected file if it still exists
            if (File.Exists(filePath))
            {
                await _controller.DisplayFileAsync(filePath);
            }
        }

        /// <summary>
        /// Handles double-click on thumbnails to open with default viewer.
        /// </summary>
        private void Thumbnail_DoubleClick(object? sender, EventArgs e)
        {
            if (sender is not PictureBox pb || pb.Tag is not string path || !File.Exists(path))
                return;

            _fileInteractionHandler.OpenFileWithDefaultViewer(path);
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

                var folder = dropItems.FirstOrDefault(d => Directory.Exists(d));
                if (folder != null)
                {
                    _ = _controller.LoadDirectoryAsync(folder);
                    return;
                }
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
                string? dirToOpen = GetDirectoryToOpen();

                if (!string.IsNullOrEmpty(dirToOpen) && Directory.Exists(dirToOpen))
                {
                    _ = _controller.LoadDirectoryAsync(dirToOpen);
                    _fileInteractionHandler.OpenFolderInExplorer(dirToOpen);
                    return;
                }

                string? selectedPath = _fileInteractionHandler.BrowseForFolder(this, "Select folder with images");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _ = _controller.LoadDirectoryAsync(selectedPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in OpenFolder_Click", ex);
                MessageBox.Show("An error occurred while opening a folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Determines the directory to open based on current selection or last directory.
        /// </summary>
        private string? GetDirectoryToOpen()
        {
            string? selectedFile = _model.SelectedFilePath;
            if (!string.IsNullOrEmpty(selectedFile) && File.Exists(selectedFile))
                return Path.GetDirectoryName(selectedFile);

            if (!string.IsNullOrEmpty(_model.CurrentDirectory) && Directory.Exists(_model.CurrentDirectory))
                return _model.CurrentDirectory;

            return null;
        }

        /// <summary>
        /// Handles the Rename button click for single or batch rename operations.
        /// </summary>
        private void RenameButton_Click(object sender, EventArgs e)
        {
            try
            {
                var selected = _model.SelectedFilePaths;

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
                    HandleSingleRename(selected[0]);
                }
                else
                {
                    HandleBatchRename(selected);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in RenameButton_Click", ex);
                MessageBox.Show("An error occurred during rename. Please check the logs.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles single file rename.
        /// </summary>
        private void HandleSingleRename(string sourcePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(sourcePath) ?? _model.CurrentDirectory;
                var ext = Path.GetExtension(sourcePath);
                var defaultName = Path.GetFileNameWithoutExtension(sourcePath);

                var newName = DialogHelper.PromptForString(this, "Rename File", "Enter new name (without extension):", defaultName);
                if (string.IsNullOrWhiteSpace(newName))
                {
                    _logger.LogInfo("Rename cancelled by user");
                    return;
                }

                _ = _controller.RenameSingleFileAsync(sourcePath, newName);
                MessageBox.Show("File renamed successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in single rename", ex);
                MessageBox.Show($"Rename failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles batch file rename.
        /// </summary>
        private void HandleBatchRename(List<string> selectedFiles)
        {
            try
            {
                var batchInfo = DialogHelper.PromptForBatchRename(this);
                if (batchInfo == null)
                {
                    _logger.LogInfo("Batch rename cancelled by user");
                    return;
                }

                _ = _controller.RenameBatchFilesAsync(selectedFiles, batchInfo.Value.BaseName, batchInfo.Value.StartIndex);
                MessageBox.Show(
                    $"Batch rename completed.\nSuccessfully renamed: {selectedFiles.Count} files",
                    "Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
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
                _controller.ClearAll();
                _thumbnailPanelManager.Clear();
                previewPictureBox.Image?.Dispose();
                previewPictureBox.Image = null;
                metadataTextBox.Clear();
                ShowPlaceholder();
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

        /// <summary>
        /// Creates a thumbnail image for the specified file path and size.
        /// </summary>
        public Image? CreateThumbnailImage(string filePath, Size size)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning($"Thumbnail creation skipped: file not found or invalid path: {filePath}");
                return null;
            }

            try
            {
                _logger.LogDebug($"Creating thumbnail for: {filePath}");
                // Use FileStream with sequential scan to reduce memory pressure
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                using var src = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);

                var bmp = new Bitmap(size.Width, size.Height);
                using var g = Graphics.FromImage(bmp);

                // Use faster rendering settings to reduce CPU usage
                g.CompositingQuality = CompositingQuality.Default;
                g.InterpolationMode = InterpolationMode.Low;
                g.SmoothingMode = SmoothingMode.None;
                g.Clear(Color.Transparent);

                var ratio = Math.Min((double)size.Width / src.Width, (double)size.Height / src.Height);
                var thumbW = Math.Max(1, (int)(src.Width * ratio));
                var thumbH = Math.Max(1, (int)(src.Height * ratio));
                var x = (size.Width - thumbW) / 2;
                var y = (size.Height - thumbH) / 2;

                g.DrawImage(src, x, y, thumbW, thumbH);

                _logger.LogDebug($"Thumbnail created successfully: {Path.GetFileName(filePath)}");
                return bmp;
            }
            catch (OutOfMemoryException ex)
            {
                // Avoid capturing full exception to reduce memory retention
                _logger.LogError($"Out of memory creating thumbnail for {Path.GetFileName(filePath)}");
                return null;
            }
            catch (Exception ex)
            {
                // Log concise message and swallow exception to keep UI responsive
                _logger.LogError($"Failed to create thumbnail for {Path.GetFileName(filePath)}");
                return null;
            }
        }
    }
}