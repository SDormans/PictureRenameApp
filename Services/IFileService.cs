namespace PictureRenameApp.Services
{
    /// <summary>
    /// Interface for file system operations.
    /// Abstracts file operations for better testability and error handling.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Gets all image files in a directory.
        /// </summary>
        /// <param name="directoryPath">Path to search</param>
        /// <returns>Sorted list of image file paths</returns>
        List<string> GetImageFilesInDirectory(string directoryPath);

        /// <summary>
        /// Renames a single file with overwrite handling.
        /// </summary>
        /// <param name="sourcePath">Current file path</param>
        /// <param name="destinationPath">New file path</param>
        /// <param name="overwrite">Whether to overwrite existing files</param>
        void RenameFile(string sourcePath, string destinationPath, bool overwrite = false);

        /// <summary>
        /// Formats a file size in bytes to human-readable format (B, KB, MB, GB).
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted string (e.g., "2.5 MB")</returns>
        string FormatFileSize(long bytes);
    }
}
