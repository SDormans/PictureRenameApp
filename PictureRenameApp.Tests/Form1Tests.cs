using System;
using System.Drawing;
using System.IO;
using PictureRenameApp.Controllers;
using PictureRenameApp.Models;
using PictureRenameApp.Services;

namespace PictureRenameApp.Tests
{
    public class Form1Tests
    {
        private class MockLogger : IApplicationLogger
        {
            public System.Collections.Generic.List<string> InfoLogs { get; } = new();
            public System.Collections.Generic.List<string> WarningLogs { get; } = new();
            public System.Collections.Generic.List<string> ErrorLogs { get; } = new();
            public System.Collections.Generic.List<string> DebugLogs { get; } = new();

            public void LogInfo(string message) => InfoLogs.Add(message);
            public void LogWarning(string message) => WarningLogs.Add(message);
            public void LogError(string message, Exception? exception = null) => ErrorLogs.Add(message);
            public void LogDebug(string message) => DebugLogs.Add(message);
        }

        private class MockController : IApplicationController
        {
            private readonly ApplicationModel _model = new();
            public IApplicationModel GetModel() => _model;
            public System.Threading.Tasks.Task DisplayFileAsync(string filePath) => System.Threading.Tasks.Task.CompletedTask;
            public System.Threading.Tasks.Task LoadDirectoryAsync(string directoryPath) => System.Threading.Tasks.Task.CompletedTask;
            public System.Threading.Tasks.Task RenameBatchFilesAsync(System.Collections.Generic.List<string> selectedFiles, string baseName, int startIndex) => System.Threading.Tasks.Task.CompletedTask;
            public System.Threading.Tasks.Task RenameSingleFileAsync(string sourcePath, string newName) => System.Threading.Tasks.Task.CompletedTask;
            public void ClearAll() { }
        }

        [Fact]
        public void CreateThumbnailImage_WithNullPath_ReturnsNullAndLogsWarning()
        {
            var logger = new MockLogger();
            var controller = new MockController();
            var form = new Form1(logger, controller, initializeComponents: false);

            var result = form.CreateThumbnailImage(null!, new Size(64, 64));

            Assert.Null(result);
            Assert.True(logger.WarningLogs.Count > 0);
        }

        [Fact]
        public void CreateThumbnailImage_WithNonExistentPath_ReturnsNullAndLogsWarning()
        {
            var logger = new MockLogger();
            var controller = new MockController();
            var form = new Form1(logger, controller, initializeComponents: false);

            var nonExistent = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());
            var result = form.CreateThumbnailImage(nonExistent, new Size(64, 64));

            Assert.Null(result);
            Assert.True(logger.WarningLogs.Count > 0);
        }

        [Fact]
        public void CreateThumbnailImage_WithValidImage_ReturnsCorrectlySizedBitmap()
        {
            var logger = new MockLogger();
            var controller = new MockController();
            var form = new Form1(logger, controller, initializeComponents: false);

            var tempDir = Path.Combine(Path.GetTempPath(), "testimg_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var testImagePath = Path.Combine(tempDir, "test.png");
                using (var bmp = new Bitmap(200, 100))
                {
                    bmp.Save(testImagePath);
                }

                var result = form.CreateThumbnailImage(testImagePath, new Size(64, 64));

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
        public void CreateThumbnailImage_WithInvalidImageData_HandlesExceptionAndReturnsNull()
        {
            var logger = new MockLogger();
            var controller = new MockController();
            var form = new Form1(logger, controller, initializeComponents: false);

            var tempDir = Path.Combine(Path.GetTempPath(), "testimg_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var badFile = Path.Combine(tempDir, "bad.dat");
                File.WriteAllText(badFile, "this is not an image");

                var result = form.CreateThumbnailImage(badFile, new Size(64, 64));

                Assert.Null(result);
                Assert.True(logger.ErrorLogs.Count > 0 || logger.WarningLogs.Count > 0);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
