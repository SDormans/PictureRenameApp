namespace PictureRenameApp.Services
{
    /// <summary>
    /// Interface for image file operations and thumbnail generation.
    /// Abstracts image handling to support dependency injection and testing.
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Creates a high-quality thumbnail image from a file.
        /// </summary>
        /// <param name="filePath">Full path to the image file</param>
        /// <param name="size">Desired thumbnail dimensions</param>
        /// <returns>Generated bitmap thumbnail or null if creation failed</returns>
        Image? CreateThumbnailImage(string filePath, Size size);

        /// <summary>
        /// Loads an image from file into memory to avoid file locking.
        /// </summary>
        /// <param name="filePath">Full path to the image file</param>
        /// <returns>Bitmap copy of the image</returns>
        Image? LoadImageFromFile(string filePath);

        /// <summary>
        /// Gets human-readable format string for an image.
        /// </summary>
        /// <param name="image">Image to analyze</param>
        /// <returns>Format name (e.g., "JPEG", "PNG")</returns>
        string GetImageFormatString(Image image);

        /// <summary>
        /// Gets list of supported image file extensions.
        /// </summary>
        string[] GetSupportedExtensions();
    }
}
