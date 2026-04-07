using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace PictureRenameApp.Services
{
    /// <summary>
    /// Concrete implementation of IImageService.
    /// Handles image operations including thumbnail generation and format detection.
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly IApplicationLogger _logger;
        private static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
        private const int MaxThumbnailAttempts = 3;

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

            try
            {
                _logger.LogDebug($"Creating thumbnail for: {filePath}");

                var bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);
                using var src = Image.FromStream(ms);

                var bmp = new Bitmap(size.Width, size.Height);
                using var g = Graphics.FromImage(bmp);

                // High-quality rendering settings
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.Clear(Color.Transparent);

                // Calculate aspect-ratio preserving dimensions
                var ratio = Math.Min((double)size.Width / src.Width, (double)size.Height / src.Height);
                var thumbW = (int)(src.Width * ratio);
                var thumbH = (int)(src.Height * ratio);
                var x = (size.Width - thumbW) / 2;
                var y = (size.Height - thumbH) / 2;

                g.DrawImage(src, x, y, thumbW, thumbH);

                _logger.LogDebug($"Thumbnail created successfully: {Path.GetFileName(filePath)}");
                return bmp;
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError($"Out of memory creating thumbnail for {Path.GetFileName(filePath)}", ex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create thumbnail for {Path.GetFileName(filePath)}", ex);
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

                var bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);
                var image = Image.FromStream(ms);

                // Create a bitmap copy to avoid stream dependencies
                var bitmap = new Bitmap(image);
                image.Dispose();

                _logger.LogDebug($"Image loaded successfully: {Path.GetFileName(filePath)}");
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load image from {Path.GetFileName(filePath)}", ex);
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
            return SupportedImageExtensions;
        }
    }
}
