using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using PictureRenameApp.Models;
using PictureRenameApp.Services;

namespace PictureRenameApp.UI
{
    /// <summary>
    /// Manages thumbnail control lifecycle: creation, disposal, and event handling.
    /// Single Responsibility: Handle only thumbnail control operations.
    /// </summary>
    public interface IThumbnailPanelManager
    {
        /// <summary>
        /// Clears all thumbnail controls and disposes resources.
        /// </summary>
        void Clear();

        /// <summary>
        /// Creates and adds a thumbnail control for the given thumbnail item.
        /// </summary>
        void AddThumbnail(ThumbnailItem thumbnail, EventHandler? onClickHandler, EventHandler? onDoubleClickHandler);

        /// <summary>
        /// Gets the number of visible thumbnails.
        /// </summary>
        int ControlCount { get; }
    }

    /// <summary>
    /// Implementation of thumbnail panel management.
    /// </summary>
    public class ThumbnailPanelManager : IThumbnailPanelManager
    {
        private readonly FlowLayoutPanel _panel;
        private readonly Size _thumbnailSize;
        private readonly IApplicationLogger _logger;
        private readonly object _panelLock = new object();

        public int ControlCount
        {
            get
            {
                lock (_panelLock)
                {
                    return _panel.Controls.OfType<PictureBox>().Count();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of ThumbnailPanelManager.
        /// </summary>
        public ThumbnailPanelManager(FlowLayoutPanel panel, Size thumbnailSize, IApplicationLogger logger)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _thumbnailSize = thumbnailSize;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_panelLock)
            {
                var toRemove = _panel.Controls.OfType<PictureBox>().ToList();
                foreach (var pb in toRemove)
                {
                    RemoveAndDisposeThumbnail(pb);
                }
            }
        }

        /// <inheritdoc/>
        public void AddThumbnail(ThumbnailItem thumbnail, EventHandler? onClickHandler, EventHandler? onDoubleClickHandler)
        {
            if (thumbnail == null)
                throw new ArgumentNullException(nameof(thumbnail));

            if (thumbnail.Thumbnail == null)
            {
                _logger.LogWarning($"Skipping thumbnail for {thumbnail.FilePath}: Image is null");
                return;
            }

            try
            {
                lock (_panelLock)
                {
                    var pb = CreatePictureBox(thumbnail);

                    if (onClickHandler != null)
                        pb.Click += onClickHandler;
                    if (onDoubleClickHandler != null)
                        pb.DoubleClick += onDoubleClickHandler;

                    var tooltip = new ToolTip();
                    tooltip.SetToolTip(pb, thumbnail.FilePath);

                    _panel.Controls.Add(pb);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create thumbnail control for {thumbnail.FilePath}", ex);
            }
        }

        /// <summary>
        /// Creates a PictureBox control for a thumbnail item.
        /// Validates image before assignment to prevent "Parameter is not valid" exception.
        /// </summary>
        private PictureBox CreatePictureBox(ThumbnailItem thumbnail)
        {
            var pb = new PictureBox
            {
                Width = _thumbnailSize.Width + 8,
                Height = _thumbnailSize.Height + 8,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Tag = thumbnail.FilePath,
                Padding = new Padding(4),
                Cursor = Cursors.Hand,
                BackColor = thumbnail.IsSelected ? Color.LightBlue : Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            if (thumbnail.Thumbnail != null)
            {
                // Validate image before assignment to prevent GDI+ errors
                try
                {
                    if (IsValidImage(thumbnail.Thumbnail))
                    {
                        pb.Image = thumbnail.Thumbnail;
                    }
                    else
                    {
                        _logger.LogWarning($"Thumbnail image is invalid (invalid dimensions): {thumbnail.FilePath}");
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError($"Failed to assign thumbnail image for {thumbnail.FilePath}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error assigning thumbnail image for {thumbnail.FilePath}", ex);
                }
            }

            return pb;
        }

        /// <summary>
        /// Validates that an image is valid and safe to use in UI controls.
        /// </summary>
        private bool IsValidImage(Image? image)
        {
            if (image == null)
                return false;

            try
            {
                // Check if image has valid dimensions
                return image.Width > 0 && image.Height > 0;
            }
            catch
            {
                // If accessing Width/Height throws, image is likely disposed or invalid
                return false;
            }
        }

        /// <summary>
        /// Safely removes and disposes a thumbnail control.
        /// Note: Must be called within the _panelLock to prevent race conditions.
        /// Validates bitmap state before removal to prevent "Parameter is not valid" exception.
        /// </summary>
        private void RemoveAndDisposeThumbnail(PictureBox pb)
        {
            try
            {
                if (pb == null || pb.IsDisposed)
                    return;

                // Verify the image/bitmap is still valid before removal
                try
                {
                    if (pb.Image is Bitmap bmp)
                    {
                        // Check bitmap validity by attempting to access its properties
                        _ = bmp.Size;
                    }
                }
                catch
                {
                    // Bitmap is already invalid, proceed with removal
                }

                // Remove from panel if it still contains the control
                if (_panel.Controls.Contains(pb))
                {
                    _panel.Controls.Remove(pb);
                }

                // Dispose managed resources
                try
                {
                    pb.Image?.Dispose();
                }
                catch
                {
                    // Image already disposed, continue
                }

                pb.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error disposing thumbnail control", ex);
            }
        }
    }
}
