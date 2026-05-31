namespace PictureRenameApp.Configuration
{
    /// <summary>
    /// Contains all application-wide constants to eliminate magic numbers and strings.
    /// Provides a single source of truth for configuration values.
    /// </summary>
    public static class AppConstants
    {
        // Image-related constants
        public const int ThumbnailWidth = 128;
        public const int ThumbnailHeight = 128;
        public const int MaxThumbnailAttempts = 3;
        public const int ImageLoadTimeoutMs = 5000;

        // File operation constants
        public const int DefaultFileBufferSize = 4096;
        public const int MaxConcurrentFileOperations = 4;
        public const int FileScanTimeoutMs = 30000;

        // UI constants
        public const int ToolbarHeight = 48;
        public const int ThumbnailStripHeight = 110;
        public const int FooterHeight = 44;
        public const int MetadataHeight = 26;
        public const int DefaultWindowWidth = 960;
        public const int DefaultWindowHeight = 720;
        public const int ButtonHeight = 30;
        public const int ToolbarPadding = 8;
        public const int PreviewPadding = 16;
        public const int DefaultFontSize = 9;
        public const float SemiBoldFontSize = 11f;
        public const float LargeFontSize = 16f;

        // Caching constants
        public const int MaxCacheSize = 100;
        public const int CacheExpirationMinutes = 60;

        // Logging constants
        public const string LogsDirectory = "Logs";
        public const string LogFileFormat = "app_{0:yyyy-MM-dd}.log";

        // Threading constants
        public const int RateLimitDelay = 100;
        public const int DefaultSemaphoreCount = 2;

        // Supported formats
        public static readonly string[] SupportedImageExtensions = 
        { 
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff"
        };

        // UI Strings
        public const string WindowTitle = "Picture Rename App";
        public const string BrowseFolderDescription = "Choose a folder";
        public const string BatchRenameTitle = "Batch rename {0} files";
        public const string CounterFormat = "{0} / {1}";
        public const string CounterWithSelectionFormat = "{0} / {1}  •  {2} selected";
        public const string NoImagesMessage = "No images in this directory.";

        // Error messages
        public const string GraphicsErrorTitle = "Graphics Error";
        public const string GraphicsErrorMessage = "A graphics error occurred during startup: {0}\n\nThis may be due to corrupted image files or invalid image data.\nPlease check the logs in the Logs folder for details.";
        public const string ErrorTitle = "Error";
    }
}
