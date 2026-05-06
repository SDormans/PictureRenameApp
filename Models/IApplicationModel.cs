using System.Collections.ObjectModel;

namespace PictureRenameApp.Models
{
    /// <summary>
    /// Represents a thumbnail item in the gallery.
    /// </summary>
    public class ThumbnailItem
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the thumbnail image.
        /// </summary>
        public Image? Thumbnail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is selected.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Thumbnail?.Dispose();
        }
    }

    /// <summary>
    /// Represents the application state and data.
    /// </summary>
    public interface IApplicationModel
    {
        /// <summary>
        /// Gets the collection of thumbnails.
        /// </summary>
        ObservableCollection<ThumbnailItem> Thumbnails { get; }

        /// <summary>
        /// Gets the current directory path.
        /// </summary>
        string? CurrentDirectory { get; set; }

        /// <summary>
        /// Gets the currently selected file path.
        /// </summary>
        string? SelectedFilePath { get; }

        /// <summary>
        /// Gets all selected file paths.
        /// </summary>
        List<string> SelectedFilePaths { get; }

        /// <summary>
        /// Gets the preview image.
        /// </summary>
        Image? PreviewImage { get; set; }

        /// <summary>
        /// Gets the metadata text.
        /// </summary>
        string MetadataText { get; set; }

        /// <summary>
        /// Clears all thumbnails.
        /// </summary>
        void ClearThumbnails();

        /// <summary>
        /// Toggles selection of a thumbnail.
        /// </summary>
        void ToggleThumbnailSelection(string filePath, bool multiSelect);

        /// <summary>
        /// Clears all selections.
        /// </summary>
        void ClearSelection();
    }

    /// <summary>
    /// Implementation of the application model.
    /// </summary>
    public class ApplicationModel : IApplicationModel
    {
        private string? _currentDirectory;
        private Image? _previewImage;
        private string _metadataText = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationModel"/> class.
        /// </summary>
        public ApplicationModel()
        {
            Thumbnails = new ObservableCollection<ThumbnailItem>();
        }

        /// <inheritdoc/>
        public ObservableCollection<ThumbnailItem> Thumbnails { get; }

        /// <inheritdoc/>
        public string? CurrentDirectory
        {
            get => _currentDirectory;
            set => _currentDirectory = value;
        }

        /// <inheritdoc/>
        public string? SelectedFilePath =>
            Thumbnails.FirstOrDefault(t => t.IsSelected)?.FilePath;

        /// <inheritdoc/>
        public List<string> SelectedFilePaths =>
            Thumbnails
                .Where(t => t.IsSelected)
                .Select(t => t.FilePath)
                .ToList();

        /// <inheritdoc/>
        public Image? PreviewImage
        {
            get => _previewImage;
            set => _previewImage = value;
        }

        /// <inheritdoc/>
        public string MetadataText
        {
            get => _metadataText;
            set => _metadataText = value;
        }

        /// <inheritdoc/>
        public void ClearThumbnails()
        {
            foreach (var thumbnail in Thumbnails)
            {
                thumbnail.Dispose();
            }
            Thumbnails.Clear();
            _currentDirectory = null;
        }

        /// <inheritdoc/>
        public void ToggleThumbnailSelection(string filePath, bool multiSelect)
        {
            var thumbnail = Thumbnails.FirstOrDefault(t => t.FilePath == filePath);
            if (thumbnail == null)
                return;

            if (!multiSelect)
            {
                // Clear other selections
                foreach (var t in Thumbnails)
                {
                    t.IsSelected = false;
                }
            }

            // Toggle current selection
            thumbnail.IsSelected = !thumbnail.IsSelected;
        }

        /// <inheritdoc/>
        public void ClearSelection()
        {
            foreach (var thumbnail in Thumbnails)
            {
                thumbnail.IsSelected = false;
            }
        }
    }
}
