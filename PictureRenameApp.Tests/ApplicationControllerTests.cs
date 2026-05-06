using System;
using System.Collections.Generic;
using System.Drawing;
using PictureRenameApp.Models;
using PictureRenameApp.Controllers;
using PictureRenameApp.Services;

namespace PictureRenameApp.Tests
{
    /// <summary>
    /// Unit tests for ApplicationController.
    /// </summary>
    public class ApplicationControllerTests
    {
        private class MockLogger : IApplicationLogger
        {
            public void LogInfo(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
            public void LogDebug(string message) { }
        }

        private class MockImageService : IImageService
        {
            public Image? CreateThumbnailImage(string filePath, Size size)
            {
                return new Bitmap(size.Width, size.Height);
            }

            public Image? LoadImageFromFile(string filePath)
            {
                return new Bitmap(100, 100);
            }

            public string GetImageFormatString(Image image)
            {
                return "JPEG";
            }

            public string[] GetSupportedExtensions()
            {
                return new[] { ".jpg", ".png", ".bmp", ".gif" };
            }
        }

        private class MockFileService : IFileService
        {
            public List<string> GetImageFilesInDirectory(string directoryPath)
            {
                return new List<string>();
            }

            public void RenameFile(string sourcePath, string destinationPath, bool overwrite = false)
            {
                // Mock implementation
            }

            public string FormatFileSize(long bytes)
            {
                return "1 MB";
            }
        }

        [Fact]
        public void Constructor_ThrowsIfModelIsNull()
        {
            // Arrange, Act & Assert
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();

            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationController(logger, imageService, fileService, null!));
        }

        [Fact]
        public void Constructor_ThrowsIfLoggerIsNull()
        {
            // Arrange, Act & Assert
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var model = new ApplicationModel();

            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationController(null!, imageService, fileService, model));
        }

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

        [Fact]
        public async Task ClearAll_ClearsThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg" });
            model.MetadataText = "Test";
            
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            controller.ClearAll();

            // Assert
            Assert.True(model.Thumbnails.Count == 0);
            Assert.Empty(model.MetadataText);
        }

        [Fact]
        public async Task LoadDirectoryAsync_WithNullPath_ClearsThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            await controller.LoadDirectoryAsync(null!);

            // Assert
            Assert.True(model.Thumbnails.Count == 0);
        }

        [Fact]
        public async Task LoadDirectoryAsync_WithInvalidDirectory_ClearsThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            var logger = new MockLogger();
            var imageService = new MockImageService();
            var fileService = new MockFileService();
            var controller = new ApplicationController(logger, imageService, fileService, model);

            // Act
            await controller.LoadDirectoryAsync("C:\\NonExistentDirectory");

            // Assert
            Assert.True(model.Thumbnails.Count == 0);
        }
    }
}
