using System;
using System.Windows.Forms;
using PictureRenameApp.Models;
using PictureRenameApp.Services;

namespace PictureRenameApp.UI
{
    /// <summary>
    /// Handles thumbnail selection logic and synchronization with the model.
    /// Single Responsibility: Manage thumbnail selection state.
    /// </summary>
    public interface IThumbnailSelector
    {
        /// <summary>
        /// Selects or deselects a thumbnail by file path.
        /// </summary>
        void SelectThumbnail(string filePath, bool multiSelect);

        /// <summary>
        /// Gets the selected file path.
        /// </summary>
        string? GetSelectedFilePath();
    }

    /// <summary>
    /// Implementation of thumbnail selection.
    /// </summary>
    public class ThumbnailSelector : IThumbnailSelector
    {
        private readonly IApplicationModel _model;
        private readonly IApplicationLogger _logger;

        /// <summary>
        /// Initializes a new instance of ThumbnailSelector.
        /// </summary>
        public ThumbnailSelector(IApplicationModel model, IApplicationLogger logger)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void SelectThumbnail(string filePath, bool multiSelect)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                _model.ToggleThumbnailSelection(filePath, multiSelect);
                _logger.LogDebug($"Thumbnail selected: {System.IO.Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error selecting thumbnail", ex);
            }
        }

        /// <inheritdoc/>
        public string? GetSelectedFilePath()
        {
            return _model.SelectedFilePath;
        }
    }
}
