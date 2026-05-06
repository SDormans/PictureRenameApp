using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using PictureRenameApp.Models;
using PictureRenameApp.Controllers;
using PictureRenameApp.Services;

namespace PictureRenameApp.Tests
{
    /// <summary>
    /// Comprehensive unit tests for ApplicationController.
    /// Covers happy paths, edge cases, error cases, and boundary conditions.
    /// </summary>
    public class ComprehensiveApplicationControllerTests
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
            private readonly bool _shouldFailThumbnail;
            private readonly bool _shouldFailImage;

            public MockImageService(bool failThumbnail = false, bool failImage = false)
            {
                _shouldFailThumbnail = failThumbnail;
                _shouldFailImage = failImage;
            }

            public Image? CreateThumbnailImage(string filePath, Size size)
            {
                if (_shouldFailThumbnail) return null;
                return File.Exists(filePath) ? new Bitmap(size.Width, size.Height) : null;
            }

            public Image? LoadImageFromFile(string filePath)
            {
                if (_shouldFailImage) return null;
                return File.Exists(filePath) ? new Bitmap(100, 100) : null;
            }

            public string GetImageFormatString(Image image)
            {
                return image != null ? "JPEG" : "Unknown";
            }

            public string[] GetSupportedExtensions()
            {
                return new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
            }
        }

        private class MockFileService : IFileService
        {
            private readonly bool _shouldFailRename;
            private readonly bool _fileExists;

            public MockFileService(bool failRename = false, bool fileExists = true)
            {
                _shouldFailRename = failRename;
                _fileExists = fileExists;
            }

            public List<string> GetImageFilesInDirectory(string directoryPath)
            {
                if (!_fileExists) return new List<string>();
                if (directoryPath == null || !Directory.Exists(directoryPath))
                    return new List<string>();

                var files = new List<string>();
                if (Directory.Exists(directoryPath))
                {
                    foreach (var file in Directory.GetFiles(directoryPath, "*.jpg"))
                        files.Add(file);
                    foreach (var file in Directory.GetFiles(directoryPath, "*.png"))
                        files.Add(file);
                }
                return files;
            }

            public void RenameFile(string sourcePath, string destinationPath, bool overwrite = false)
            {
                if (_shouldFailRename)
                    throw new IOException("Rename failed");
            }

            public string FormatFileSize(long bytes)
            {
                return "1 MB";
            }
        }

        // ========== Constructor Tests ==========

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();

            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationController(null!, imageService, fileService, model));
        }

        [Fact]
        public void Constructor_WithNullImageService_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            var logger = new MockLogger();
            var fileService = new MockFileService();
            var model = new ApplicationModel();

            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationController(logger, null!, fileService, model));
        }

        [Fact]
        public void Constructor_WithNullFileService_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var model = new ApplicationModel();

            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationController(logger, imageService, null!, model));
        }

        [Fact]
        public void Constructor_WithNullModel_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();

            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationController(logger, imageService, fileService, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_Succeeds()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();

            // Act
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Assert
            Assert.NotNull(controller);
        }

        // ========== GetModel Tests ==========

        [Fact]
        public void GetModel_ReturnsInjectedModel()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            var returnedModel = controller.GetModel();

            // Assert
            Assert.Equal(model, returnedModel);
        }

        // ========== ClearAll Tests ==========

        [Fact]
        public void ClearAll_ClearsThumbnails()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg" });
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            controller.ClearAll();

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public void ClearAll_ClearsMetadata()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            model.MetadataText = "Some metadata";
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            controller.ClearAll();

            // Assert
            Assert.Empty(model.MetadataText);
        }

        [Fact]
        public void ClearAll_ClearsPreviewImage()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            model.PreviewImage = new Bitmap(100, 100);
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            controller.ClearAll();

            // Assert
            Assert.Null(model.PreviewImage);
        }

        [Fact]
        public void ClearAll_ClearsSelection()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            controller.ClearAll();

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        // ========== LoadDirectoryAsync Tests ==========

        [Fact]
        public async Task LoadDirectoryAsync_WithNullPath_ClearsThumbnails()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            await controller.LoadDirectoryAsync(null!);

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public async Task LoadDirectoryAsync_WithEmptyPath_ClearsThumbnails()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            await controller.LoadDirectoryAsync(string.Empty);

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public async Task LoadDirectoryAsync_WithNonExistentDirectory_ClearsThumbnails()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());

            // Act
            await controller.LoadDirectoryAsync(nonExistentPath);

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public async Task LoadDirectoryAsync_WithValidEmptyDirectory_SetsMetadata()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Act
                await controller.LoadDirectoryAsync(tempDir);

                // Assert
                Assert.Equal(tempDir, model.CurrentDirectory);
                Assert.True(model.MetadataText.Contains("No images"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task LoadDirectoryAsync_WithImageFiles_LoadsThumbnails()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test image files
                var file1 = Path.Combine(tempDir, "image1.jpg");
                var file2 = Path.Combine(tempDir, "image2.png");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                // Act
                await controller.LoadDirectoryAsync(tempDir);

                // Assert
                Assert.Equal(2, model.Thumbnails.Count);
                Assert.NotNull(model.PreviewImage);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task LoadDirectoryAsync_SetsCurrentDirectory()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Act
                await controller.LoadDirectoryAsync(tempDir);

                // Assert
                Assert.Equal(tempDir, model.CurrentDirectory);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task LoadDirectoryAsync_SelectsFirstThumbnail()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create test image files
                var file1 = Path.Combine(tempDir, "image1.jpg");
                var file2 = Path.Combine(tempDir, "image2.png");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                // Act
                await controller.LoadDirectoryAsync(tempDir);

                // Assert
                Assert.True(model.Thumbnails.Count > 0);
                Assert.True(model.Thumbnails[0].IsSelected);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== DisplayFileAsync Tests ==========

        [Fact]
        public async Task DisplayFileAsync_WithNullPath_DoesNotCrash()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act & Assert
            await controller.DisplayFileAsync(null!);
        }

        [Fact]
        public async Task DisplayFileAsync_WithNonExistentFile_DoesNotCrash()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());

            // Act & Assert
            await controller.DisplayFileAsync(nonExistentPath);
        }

        [Fact]
        public async Task DisplayFileAsync_WithValidFile_SetsPreviewImage()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var testFile = Path.Combine(tempDir, "test.jpg");
                File.WriteAllText(testFile, "");

                // Act
                await controller.DisplayFileAsync(testFile);

                // Assert
                Assert.NotNull(model.PreviewImage);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task DisplayFileAsync_WithValidFile_SetsMetadata()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var testFile = Path.Combine(tempDir, "test.jpg");
                File.WriteAllText(testFile, "test content");

                // Act
                await controller.DisplayFileAsync(testFile);

                // Assert
                Assert.False(string.IsNullOrEmpty(model.MetadataText));
                Assert.True(model.MetadataText.Contains("Size:"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== RenameSingleFileAsync Tests ==========

        [Fact]
        public async Task RenameSingleFileAsync_WithNonExistentFile_DoesNotThrow()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());

            // Act & Assert
            await controller.RenameSingleFileAsync(nonExistentPath, "newname");
        }

        [Fact]
        public async Task RenameSingleFileAsync_WithValidFile_CallsRenameFile()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var sourcePath = Path.Combine(tempDir, "original.jpg");
                File.WriteAllText(sourcePath, "");

                // Act
                await controller.RenameSingleFileAsync(sourcePath, "renamed");

                // Assert - The file should be renamed (mock behavior)
                Assert.True(logger.InfoLogs.Count > 0);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task RenameSingleFileAsync_PreservesFileExtension()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var sourcePath = Path.Combine(tempDir, "original.jpg");
                File.WriteAllText(sourcePath, "");

                // Act
                await controller.RenameSingleFileAsync(sourcePath, "renamed");

                // Assert - Extension should be preserved in logs
                Assert.True(logger.InfoLogs.Any(l => l.Contains(".jpg")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== RenameBatchFilesAsync Tests ==========

        [Fact]
        public async Task RenameBatchFilesAsync_WithNullList_DoesNotThrow()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act & Assert
            await controller.RenameBatchFilesAsync(null!, "base", 1);
        }

        [Fact]
        public async Task RenameBatchFilesAsync_WithEmptyList_DoesNotThrow()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act & Assert
            await controller.RenameBatchFilesAsync(new List<string>(), "base", 1);
        }

        [Fact]
        public async Task RenameBatchFilesAsync_WithValidFiles_RenamesAll()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var file1 = Path.Combine(tempDir, "file1.jpg");
                var file2 = Path.Combine(tempDir, "file2.jpg");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                var filesToRename = new List<string> { file1, file2 };

                // Act
                await controller.RenameBatchFilesAsync(filesToRename, "photo", 1);

                // Assert
                Assert.True(logger.InfoLogs.Any(l => l.Contains("Batch rename completed")));
                Assert.True(logger.InfoLogs.Any(l => l.Contains("2 files")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task RenameBatchFilesAsync_WithStartIndex_AppliesCorrectIndexes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var file1 = Path.Combine(tempDir, "file1.jpg");
                var file2 = Path.Combine(tempDir, "file2.jpg");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                var filesToRename = new List<string> { file1, file2 };

                // Act
                await controller.RenameBatchFilesAsync(filesToRename, "photo", 5);

                // Assert
                Assert.True(logger.DebugLogs.Any(l => l.Contains("photo_005")));
                Assert.True(logger.DebugLogs.Any(l => l.Contains("photo_006")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task RenameBatchFilesAsync_SkipsNonExistentFiles()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var existingFile = Path.Combine(tempDir, "existing.jpg");
                var nonExistentFile = Path.Combine(tempDir, "nonexistent.jpg");
                File.WriteAllText(existingFile, "");

                var filesToRename = new List<string> { existingFile, nonExistentFile };

                // Act
                await controller.RenameBatchFilesAsync(filesToRename, "photo", 1);

                // Assert
                Assert.True(logger.WarningLogs.Any(l => l.Contains("not found")));
                Assert.True(logger.InfoLogs.Any(l => l.Contains("Batch rename completed")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task RenameBatchFilesAsync_WithDifferentExtensions_PresservesEachExtension()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var file1 = Path.Combine(tempDir, "file1.jpg");
                var file2 = Path.Combine(tempDir, "file2.png");
                File.WriteAllText(file1, "");
                File.WriteAllText(file2, "");

                var filesToRename = new List<string> { file1, file2 };

                // Act
                await controller.RenameBatchFilesAsync(filesToRename, "photo", 1);

                // Assert
                Assert.True(logger.DebugLogs.Any(l => l.Contains("photo_001") && l.Contains(".jpg")));
                Assert.True(logger.DebugLogs.Any(l => l.Contains("photo_002") && l.Contains(".png")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ========== Edge Case Tests ==========

        [Fact]
        public async Task LoadDirectoryAsync_ReplacesPreviousThumbnails()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Add initial thumbnails
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "old1.jpg" });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "old2.jpg" });

            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var file1 = Path.Combine(tempDir, "new1.jpg");
                File.WriteAllText(file1, "");

                // Act
                await controller.LoadDirectoryAsync(tempDir);

                // Assert - Old thumbnails should be replaced
                Assert.DoesNotContain("old1.jpg", model.SelectedFilePaths);
                Assert.DoesNotContain("old2.jpg", model.SelectedFilePaths);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ClearAll_CanBeCalledMultipleTimes()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg" });

            // Act
            controller.ClearAll();
            controller.ClearAll();

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public async Task RenameBatchFilesAsync_WithSingleFile_Works()
        {
            // Arrange
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();
            var controller = new ApplicationController(logger, imageService, fileService, model);
            var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var file1 = Path.Combine(tempDir, "file1.jpg");
                File.WriteAllText(file1, "");

                var filesToRename = new List<string> { file1 };

                // Act
                await controller.RenameBatchFilesAsync(filesToRename, "photo", 1);

                // Assert
                Assert.True(logger.InfoLogs.Any(l => l.Contains("Batch rename completed")));
                Assert.True(logger.InfoLogs.Any(l => l.Contains("1 files")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
