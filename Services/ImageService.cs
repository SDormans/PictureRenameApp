using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System;
using System.IO;
using System.Drawing;
using System.Linq;
using PictureRenameApp.Configuration;

namespace PictureRenameApp.Services
{
    /// <summary>
    /// Concrete implementation of IImageService.
    /// Handles image operations including thumbnail generation and format detection.
    /// Optimized for performance with lower quality settings for thumbnails
    /// and efficient resource management.
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly IApplicationLogger _logger;
        // Use HashSet for O(1) lookups
        private static readonly HashSet<string> SupportedImageExtensions = new(
            AppConstants.SupportedImageExtensions,
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Initializes a new instance of ImageService.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and error tracking</param>
        public ImageService(IApplicationLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Image? CreateThumbnailImage(string filePath, Size size)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning($"Thumbnail creation skipped: file not found or invalid path: {filePath}");
                return null;
            }

            // Validate thumbnail size parameters
            if (size.Width <= 0 || size.Height <= 0)
            {
                _logger.LogWarning($"Thumbnail size must be positive: {size.Width}x{size.Height}");
                return null;
            }

            try
            {
                _logger.LogDebug($"Creating thumbnail for: {filePath}");
                // Open stream with sequential scan to reduce memory pressure and IO overhead
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    AppConstants.DefaultFileBufferSize, FileOptions.SequentialScan);
                using var src = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);

                // Create target bitmap and draw using lower quality settings optimized for thumbnails
                var bmp = new Bitmap(size.Width, size.Height);
                using var g = Graphics.FromImage(bmp);

                // Use lower quality for faster thumbnail generation
                // Thumbnails don't need high quality rendering
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

                // Validate bitmap is valid before returning
                if (bmp.Width <= 0 || bmp.Height <= 0)
                {
                    _logger.LogWarning($"Thumbnail bitmap has invalid dimensions: {bmp.Width}x{bmp.Height}");
                    bmp.Dispose();
                    return null;
                }

                _logger.LogDebug($"Thumbnail created successfully: {Path.GetFileName(filePath)}");
                return bmp;
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError($"Out of memory creating thumbnail for {Path.GetFileName(filePath)}", ex);
                return null;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"{ex.Source}: {ex.Message} creating thumbnail for {Path.GetFileName(filePath)}", ex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create thumbnail for {Path.GetFileName(filePath)}: {ex.Message}", ex);
                return null;
            }
        }

        public Image? LoadImageFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning($"Image load failed: file not found or invalid path: {filePath}");
                return null;
            }

            try
            {
                _logger.LogDebug($"Loading image from file: {filePath}");
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    AppConstants.DefaultFileBufferSize, FileOptions.SequentialScan);
                using var image = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);

                // Create a bitmap copy to detach from the stream
                var bitmap = new Bitmap(image);

                // Validate bitmap is valid before returning
                if (bitmap.Width <= 0 || bitmap.Height <= 0)
                {
                    _logger.LogWarning($"Loaded image bitmap has invalid dimensions: {bitmap.Width}x{bitmap.Height}");
                    bitmap.Dispose();
                    return null;
                }

                _logger.LogDebug($"Image loaded successfully: {Path.GetFileName(filePath)}");
                return bitmap;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"{ex.Source}: {ex.Message} loading image from {Path.GetFileName(filePath)}", ex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load image from {Path.GetFileName(filePath)}: {ex.Message}", ex);
                return null;
            }
        }

        public string GetImageFormatString(Image image)
        {
            if (image == null)
                return "Unknown";

            try
            {
                var guid = image.RawFormat.Guid;

                return guid switch
                {
                    _ when guid == ImageFormat.Jpeg.Guid => "JPEG",
                    _ when guid == ImageFormat.Png.Guid => "PNG",
                    _ when guid == ImageFormat.Gif.Guid => "GIF",
                    _ when guid == ImageFormat.Bmp.Guid => "BMP",
                    _ when guid == ImageFormat.Tiff.Guid => "TIFF",
                    _ => image.RawFormat.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Failed to determine image format: {ex.Message}");
                return "Unknown";
            }
        }

        public string[] GetSupportedExtensions()
        {
            return SupportedImageExtensions.ToArray();
        }
    }
}
