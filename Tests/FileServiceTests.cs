using System;
using System.IO;
using System.Linq;
using PictureRenameApp.Services;
using Xunit;

namespace PictureRenameApp.Tests
{
    public class FileServiceTests : IDisposable
    {
        private readonly IFileService _fileService;
        private readonly string _tempDir;

        public FileServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PictureRenameAppTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            var imageService = new TestImageService();
            _fileService = new FileService(new NullLogger(), imageService);
        }

        [Fact]
        public void GetImageFilesInDirectory_ReturnsOnlySupported()
        {
            // Arrange
            var supported = Path.Combine(_tempDir, "a.jpg"); File.WriteAllText(supported, "x");
            var unsupported = Path.Combine(_tempDir, "b.txt"); File.WriteAllText(unsupported, "x");

            // Act
            var files = _fileService.GetImageFilesInDirectory(_tempDir);

            // Assert
            Assert.Contains(supported, files);
            Assert.DoesNotContain(unsupported, files);
        }

        [Fact]
        public void RenameFile_MoveFile_RenamesSuccessfully()
        {
            // Arrange
            var src = Path.Combine(_tempDir, "src.jpg");
            var dest = Path.Combine(_tempDir, "dest.jpg");
            File.WriteAllText(src, "x");

            // Act
            _fileService.RenameFile(src, dest, overwrite: false);

            // Assert
            Assert.True(File.Exists(dest));
            Assert.False(File.Exists(src));
        }

        [Fact]
        public void RenameFile_WhenDestinationExists_ThrowsIfNoOverwrite()
        {
            var src = Path.Combine(_tempDir, "src2.jpg");
            var dest = Path.Combine(_tempDir, "dest2.jpg");
            File.WriteAllText(src, "x");
            File.WriteAllText(dest, "y");

            Assert.Throws<IOException>(() => _fileService.RenameFile(src, dest, overwrite: false));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { }
        }

        private class TestImageService : IImageService
        {
            public Image? CreateThumbnailImage(string filePath, Size size) => null;
            public Image? LoadImageFromFile(string filePath) => null;
            public string GetImageFormatString(Image image) => "JPEG";
            public string[] GetSupportedExtensions() => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
        }

        private class NullLogger : IApplicationLogger
        {
            public void LogDebug(string message) { }
            public void LogError(string message, Exception? exception = null) { }
            public void LogInfo(string message) { }
            public void LogWarning(string message) { }
        }
    }
}
