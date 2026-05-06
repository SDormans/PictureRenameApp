using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PictureRenameApp.Models;
using PictureRenameApp.Services;

namespace PictureRenameApp.Tests
{
    /// <summary>
    /// Comprehensive unit tests for FileService and ImageService.
    /// Covers happy paths, edge cases, error cases, and boundary conditions.
    /// </summary>
    public class FileServiceTests
    {
        private class MockLogger : IApplicationLogger
        {
            public List<string> InfoLogs { get; } = new();
            public List<string> WarningLogs { get; } = new();
            public List<string> ErrorLogs { get; } = new();
            public List<string> DebugLogs { get; } = new();

            public void LogInfo(string message) => InfoLogs.Add(message);
            public void LogWarning(string message) => WarningLogs.Add(message);
            public void LogError(string message, Exception? exception = null) => ErrorLogs.Add(message);
            public void LogDebug(string message) => DebugLogs.Add(message);
        }

        private class MockImageService : IImageService
        {
            public Image? CreateThumbnailImage(string filePath, Size size) => new Bitmap(size.Width, size.Height);
            public Image? LoadImageFromFile(string filePath) => new Bitmap(100, 100);
            public string GetImageFormatString(Image image) => "JPEG";
            public string[] GetSupportedExtensions() => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
        }

        // ========== GetImageFilesInDirectory Tests ==========

        [Fact]
        public void GetImageFilesInDirectory_WithValidDirectory_ReturnsEmptyList()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Act
                var result = fileService.GetImageFilesInDirectory(tempDir);

                // Assert
                Assert.Empty(result);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetImageFilesInDirectory_WithNullPath_ReturnsEmptyList()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.GetImageFilesInDirectory(null!);

            // Assert
            Assert.Empty(result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("empty path")));
        }

        [Fact]
        public void GetImageFilesInDirectory_WithEmptyPath_ReturnsEmptyList()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.GetImageFilesInDirectory(string.Empty);

            // Assert
            Assert.Empty(result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("empty path")));
        }

        [Fact]
        public void GetImageFilesInDirectory_WithWhitespaceOnlyPath_ReturnsEmptyList()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.GetImageFilesInDirectory("   ");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetImageFilesInDirectory_WithNonExistentDirectory_ReturnsEmptyList()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());

            // Act
            var result = fileService.GetImageFilesInDirectory(nonExistentPath);

            // Assert
            Assert.Empty(result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("does not exist")));
        }

        [Fact]
        public void GetImageFilesInDirectory_WithMultipleImageFiles_ReturnsAllImages()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test image files
                var file1 = Path.Combine(tempDir, "image1.jpg");
                var file2 = Path.Combine(tempDir, "image2.png");
                var file3 = Path.Combine(tempDir, "image3.bmp");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");
                File.WriteAllText(file3, "");

                // Act
                var result = fileService.GetImageFilesInDirectory(tempDir);

                // Assert
                Assert.True(result.Count == 3);
                Assert.Contains(file1, result);
                Assert.Contains(file2, result);
                Assert.Contains(file3, result);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetImageFilesInDirectory_IgnoresNonImageFiles()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test files
                var imageFile = Path.Combine(tempDir, "image.jpg");
                var textFile = Path.Combine(tempDir, "document.txt");
                var exeFile = Path.Combine(tempDir, "program.exe");
                File.WriteAllText(imageFile, "");
                File.WriteAllText(textFile, "");
                File.WriteAllText(exeFile, "");

                // Act
                var result = fileService.GetImageFilesInDirectory(tempDir);

                // Assert
                Assert.True(result.Count == 1);
                Assert.Contains(imageFile, result);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetImageFilesInDirectory_ReturnsSortedResults()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test files in non-alphabetical order
                var file3 = Path.Combine(tempDir, "zebra.jpg");
                var file1 = Path.Combine(tempDir, "apple.jpg");
                var file2 = Path.Combine(tempDir, "banana.png");
                File.WriteAllText(file3, "");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                // Act
                var result = fileService.GetImageFilesInDirectory(tempDir);

                // Assert
                Assert.True(result.Count == 3);
                Assert.Equal(file1, result[0]);
                Assert.Equal(file2, result[1]);
                Assert.Equal(file3, result[2]);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetImageFilesInDirectory_IsCaseInsensitiveForExtensions()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test files with uppercase extensions
                var file1 = Path.Combine(tempDir, "image1.JPG");
                var file2 = Path.Combine(tempDir, "image2.PNG");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                // Act
                var result = fileService.GetImageFilesInDirectory(tempDir);

                // Assert
                Assert.True(result.Count == 2);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== RenameFile Tests ==========

        [Fact]
        public void RenameFile_WithValidPaths_RenamesFile()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var sourcePath = Path.Combine(tempDir, "original.txt");
                var destPath = Path.Combine(tempDir, "renamed.txt");
                File.WriteAllText(sourcePath, "test content");

                // Act
                fileService.RenameFile(sourcePath, destPath);

                // Assert
                Assert.True(File.Exists(destPath));
                Assert.False(File.Exists(sourcePath));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void RenameFile_WithNullSourcePath_ThrowsArgumentException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => fileService.RenameFile(null!, "dest.txt"));
        }

        [Fact]
        public void RenameFile_WithNullDestinationPath_ThrowsArgumentException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => fileService.RenameFile("source.txt", null!));
        }

        [Fact]
        public void RenameFile_WithEmptySourcePath_ThrowsArgumentException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => fileService.RenameFile(string.Empty, "dest.txt"));
        }

        [Fact]
        public void RenameFile_WithEmptyDestinationPath_ThrowsArgumentException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => fileService.RenameFile("source.txt", string.Empty));
        }

        [Fact]
        public void RenameFile_WithNonExistentSource_ThrowsFileNotFoundException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var nonExistent = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());
            var destination = Path.Combine(Path.GetTempPath(), "destination_" + Guid.NewGuid());

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => fileService.RenameFile(nonExistent, destination));
        }

        [Fact]
        public void RenameFile_WithExistingDestination_WithoutOverwrite_ThrowsIOException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var sourcePath = Path.Combine(tempDir, "source.txt");
                var destPath = Path.Combine(tempDir, "dest.txt");
                File.WriteAllText(sourcePath, "source");
                File.WriteAllText(destPath, "dest");

                // Act & Assert
                Assert.Throws<IOException>(() => fileService.RenameFile(sourcePath, destPath, overwrite: false));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void RenameFile_WithExistingDestination_WithOverwrite_OverwritesFile()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var sourcePath = Path.Combine(tempDir, "source.txt");
                var destPath = Path.Combine(tempDir, "dest.txt");
                File.WriteAllText(sourcePath, "new content");
                File.WriteAllText(destPath, "old content");

                // Act
                fileService.RenameFile(sourcePath, destPath, overwrite: true);

                // Assert
                Assert.True(File.Exists(destPath));
                Assert.False(File.Exists(sourcePath));
                Assert.Equal("new content", File.ReadAllText(destPath));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void RenameFile_WithWhitespaceSourcePath_ThrowsArgumentException()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => fileService.RenameFile("   ", "dest.txt"));
        }

        // ========== FormatFileSize Tests ==========

        [Fact]
        public void FormatFileSize_WithZeroBytes_ReturnsZeroB()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(0);

            // Assert
            Assert.Equal("0 B", result);
        }

        [Fact]
        public void FormatFileSize_WithSmallBytes_ReturnsBytes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result1 = fileService.FormatFileSize(1);
            var result512 = fileService.FormatFileSize(512);
            var result1023 = fileService.FormatFileSize(1023);

            // Assert
            Assert.Equal("1 B", result1);
            Assert.Equal("512 B", result512);
            Assert.Equal("1023 B", result1023);
        }

        [Fact]
        public void FormatFileSize_With1KB_ReturnsKilobytes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(1024);

            // Assert
            Assert.Equal("1 KB", result);
        }

        [Fact]
        public void FormatFileSize_With1MB_ReturnsMegabytes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(1024 * 1024);

            // Assert
            Assert.Equal("1 MB", result);
        }

        [Fact]
        public void FormatFileSize_With1GB_ReturnsGigabytes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(1024L * 1024 * 1024);

            // Assert
            Assert.Equal("1 GB", result);
        }

        [Fact]
        public void FormatFileSize_With1TB_ReturnsTerabytes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(1024L * 1024 * 1024 * 1024);

            // Assert
            Assert.Equal("1 TB", result);
        }

        [Fact]
        public void FormatFileSize_WithExceededTB_KeepsTB()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(1024L * 1024 * 1024 * 1024 * 1024);

            // Assert
            Assert.Equal("1024 TB", result);
        }

        [Fact]
        public void FormatFileSize_WithDecimalValues_FormatsCorrectly()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(2560); // 2.5 KB

            // Assert
            Assert.True(result.Contains("2.5") || result.Contains("2,5"));
            Assert.True(result.Contains("KB"));
        }

        [Fact]
        public void FormatFileSize_WithNegativeBytes_ReturnsZeroB()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new FileService(logger, imageService);

            // Act
            var result = fileService.FormatFileSize(-1);

            // Assert
            Assert.Equal("0 B", result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("Invalid file size")));
        }

        // ========== Constructor Tests ==========

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileService(null!, new MockImageService()));
        }

        [Fact]
        public void Constructor_WithNullImageService_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileService(new MockLogger(), null!));
        }
    }

    /// <summary>
    /// Comprehensive unit tests for ImageService.
    /// </summary>
    public class ImageServiceTests
    {
        private class MockLogger : IApplicationLogger
        {
            public List<string> InfoLogs { get; } = new();
            public List<string> WarningLogs { get; } = new();
            public List<string> ErrorLogs { get; } = new();
            public List<string> DebugLogs { get; } = new();

            public void LogInfo(string message) => InfoLogs.Add(message);
            public void LogWarning(string message) => WarningLogs.Add(message);
            public void LogError(string message, Exception? exception = null) => ErrorLogs.Add(message);
            public void LogDebug(string message) => DebugLogs.Add(message);
        }

        // ========== GetSupportedExtensions Tests ==========

        [Fact]
        public void GetSupportedExtensions_ReturnsCorrectExtensions()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var extensions = imageService.GetSupportedExtensions();

            // Assert
            Assert.NotNull(extensions);
            Assert.True(extensions.Length > 0);
            Assert.Contains(".jpg", extensions);
            Assert.Contains(".jpeg", extensions);
            Assert.Contains(".png", extensions);
            Assert.Contains(".bmp", extensions);
            Assert.Contains(".gif", extensions);
        }

        [Fact]
        public void GetSupportedExtensions_ReturnsConsistentResults()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var result1 = imageService.GetSupportedExtensions();
            var result2 = imageService.GetSupportedExtensions();

            // Assert
            Assert.Equal(result1.Length, result2.Length);
            Assert.True(result1.SequenceEqual(result2));
        }

        // ========== CreateThumbnailImage Tests ==========

        [Fact]
        public void CreateThumbnailImage_WithNullPath_ReturnsNull()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var result = imageService.CreateThumbnailImage(null!, new Size(128, 128));

            // Assert
            Assert.Null(result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("Thumbnail creation skipped")));
        }

        [Fact]
        public void CreateThumbnailImage_WithEmptyPath_ReturnsNull()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var result = imageService.CreateThumbnailImage(string.Empty, new Size(128, 128));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateThumbnailImage_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());

            // Act
            var result = imageService.CreateThumbnailImage(nonExistentPath, new Size(128, 128));

            // Assert
            Assert.Null(result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("file not found")));
        }

        [Fact]
        public void CreateThumbnailImage_WithSmallSize_CreatesCorrectlySizedBitmap()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a simple test image
                var testImagePath = Path.Combine(tempDir, "test.png");
                using (var bitmap = new Bitmap(200, 200))
                {
                    bitmap.Save(testImagePath);
                }

                // Act
                var result = imageService.CreateThumbnailImage(testImagePath, new Size(64, 64));

                // Assert
                Assert.NotNull(result);
                Assert.Equal(64, result.Width);
                Assert.Equal(64, result.Height);
                result.Dispose();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void CreateThumbnailImage_WithLargeSize_CreatesCorrectlySizedBitmap()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a simple test image
                var testImagePath = Path.Combine(tempDir, "test.png");
                using (var bitmap = new Bitmap(100, 100))
                {
                    bitmap.Save(testImagePath);
                }

                // Act
                var result = imageService.CreateThumbnailImage(testImagePath, new Size(256, 256));

                // Assert
                Assert.NotNull(result);
                Assert.Equal(256, result.Width);
                Assert.Equal(256, result.Height);
                result.Dispose();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== LoadImageFromFile Tests ==========

        [Fact]
        public void LoadImageFromFile_WithNullPath_ReturnsNull()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var result = imageService.LoadImageFromFile(null!);

            // Assert
            Assert.Null(result);
            Assert.True(logger.WarningLogs.Any(l => l.Contains("file not found")));
        }

        [Fact]
        public void LoadImageFromFile_WithEmptyPath_ReturnsNull()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var result = imageService.LoadImageFromFile(string.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LoadImageFromFile_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());

            // Act
            var result = imageService.LoadImageFromFile(nonExistentPath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LoadImageFromFile_WithValidImageFile_ReturnsBitmap()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a simple test image
                var testImagePath = Path.Combine(tempDir, "test.png");
                using (var bitmap = new Bitmap(100, 100))
                {
                    bitmap.Save(testImagePath);
                }

                // Act
                var result = imageService.LoadImageFromFile(testImagePath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(100, result.Width);
                Assert.Equal(100, result.Height);
                result.Dispose();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== GetImageFormatString Tests ==========

        [Fact]
        public void GetImageFormatString_WithNullImage_ReturnsUnknown()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);

            // Act
            var result = imageService.GetImageFormatString(null!);

            // Assert
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void GetImageFormatString_WithJpegImage_ReturnsJPEG()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create and save as JPEG
                var testImagePath = Path.Combine(tempDir, "test.jpg");
                using (var bitmap = new Bitmap(100, 100))
                {
                    bitmap.Save(testImagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                // Load and check format
                using (var image = Image.FromFile(testImagePath))
                {
                    // Act
                    var result = imageService.GetImageFormatString(image);

                    // Assert
                    Assert.Equal("JPEG", result);
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetImageFormatString_WithPngImage_ReturnsPNG()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create and save as PNG
                var testImagePath = Path.Combine(tempDir, "test.png");
                using (var bitmap = new Bitmap(100, 100))
                {
                    bitmap.Save(testImagePath, System.Drawing.Imaging.ImageFormat.Png);
                }

                // Load and check format
                using (var image = Image.FromFile(testImagePath))
                {
                    // Act
                    var result = imageService.GetImageFormatString(image);

                    // Assert
                    Assert.Equal("PNG", result);
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetImageFormatString_WithBmpImage_ReturnsBMP()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new ImageService(logger);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create and save as BMP
                var testImagePath = Path.Combine(tempDir, "test.bmp");
                using (var bitmap = new Bitmap(100, 100))
                {
                    bitmap.Save(testImagePath, System.Drawing.Imaging.ImageFormat.Bmp);
                }

                // Load and check format
                using (var image = Image.FromFile(testImagePath))
                {
                    // Act
                    var result = imageService.GetImageFormatString(image);

                    // Assert
                    Assert.Equal("BMP", result);
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== Constructor Tests ==========

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ImageService(null!));
        }
    }
}
