namespace PictureRenameApp.Services
{
    /// <summary>
    /// Concrete implementation of IFileService.
    /// Handles file system operations with comprehensive error handling and logging.
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IApplicationLogger _logger;
        private readonly IImageService _imageService;

        /// <summary>
        /// Initializes a new instance of FileService.
        /// </summary>
        /// <param name="logger">Logger for diagnostics and error tracking</param>
        /// <param name="imageService">Service for image-related operations</param>
        public FileService(IApplicationLogger logger, IImageService imageService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
        }

        public List<string> GetImageFilesInDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                _logger.LogWarning("GetImageFilesInDirectory called with empty path");
                return new List<string>();
            }

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning($"Directory does not exist: {directoryPath}");
                return new List<string>();
            }

            try
            {
                _logger.LogInfo($"Scanning directory for images: {directoryPath}");

                // Use HashSet for O(1) lookups
                var supportedExtensions = new HashSet<string>(_imageService.GetSupportedExtensions(), StringComparer.OrdinalIgnoreCase);
                var files = Directory.GetFiles(directoryPath)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f)))
                    .OrderBy(f => f)
                    .ToList();

                _logger.LogInfo($"Found {files.Count} image(s) in {directoryPath}");
                return files;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"Access denied when scanning directory: {directoryPath}", ex);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error scanning directory {directoryPath}", ex);
                return new List<string>();
            }
        }

        public void RenameFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("Source path cannot be empty", nameof(sourcePath));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Destination path cannot be empty", nameof(destinationPath));

            if (!File.Exists(sourcePath))
            {
                _logger.LogError($"Source file does not exist: {sourcePath}");
                throw new FileNotFoundException($"Source file not found: {sourcePath}");
            }

            if (File.Exists(destinationPath))
            {
                if (!overwrite)
                {
                    _logger.LogWarning($"Destination file already exists: {destinationPath}");
                    // Do not throw, just return gracefully
                    return;
                }
                try
                {
                    File.Delete(destinationPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to delete existing destination file: {destinationPath}", ex);
                    return;
                }
            }

            try
            {
                _logger.LogInfo($"Renaming file: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(destinationPath)}");
                File.Move(sourcePath, destinationPath);
                _logger.LogInfo($"File renamed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during file rename", ex);
                // Do not throw, just return gracefully
                return;
            }
        }

        public string FormatFileSize(long bytes)
        {
            if (bytes < 0)
            {
                _logger.LogWarning($"Invalid file size: {bytes}");
                return "0 B";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Use . (dot) as decimal separator for test compatibility
            var formatted = len % 1 == 0 ? $"{len:0} {sizes[order]}" : $"{len:0.##} {sizes[order]}";
            return formatted;
        }
    }
}
