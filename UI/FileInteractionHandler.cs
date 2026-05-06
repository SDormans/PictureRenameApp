using System;
using System;
using System.IO;
using System.Windows.Forms;
using PictureRenameApp.Services;

namespace PictureRenameApp.UI
{
    /// <summary>
    /// Handles file operations triggered by UI interactions.
    /// Single Responsibility: Manage file-related user interactions.
    /// </summary>
    public interface IFileInteractionHandler
    {
        /// <summary>
        /// Opens a file with the system default viewer.
        /// </summary>
        void OpenFileWithDefaultViewer(string filePath);

        /// <summary>
        /// Opens the system file browser for a directory.
        /// </summary>
        void OpenFolderInExplorer(string folderPath);

        /// <summary>
        /// Prompts user to select a folder.
        /// </summary>
        string? BrowseForFolder(IWin32Window owner, string description);
    }

    /// <summary>
    /// Implementation of file interaction handling.
    /// </summary>
    public class FileInteractionHandler : IFileInteractionHandler
    {
        private readonly IApplicationLogger _logger;

        public FileInteractionHandler(IApplicationLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void OpenFileWithDefaultViewer(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            try
            {
                _logger.LogInfo($"Opening file with default viewer: {Path.GetFileName(filePath)}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to open file with default viewer", ex);
                MessageBox.Show(
                    "Could not open file with default viewer.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <inheritdoc/>
        public void OpenFolderInExplorer(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Could not open Explorer window: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public string? BrowseForFolder(IWin32Window owner, string description)
        {
            using var dlg = new FolderBrowserDialog { Description = description };
            if (dlg.ShowDialog(owner) == DialogResult.OK)
            {
                return dlg.SelectedPath;
            }
            return null;
        }
    }
}
