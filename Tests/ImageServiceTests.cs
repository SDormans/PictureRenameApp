using System.Drawing;
using PictureRenameApp.Services;
using Xunit;

namespace PictureRenameApp.Tests
{
    public class ImageServiceTests
    {
        private readonly IImageService _imageService;

        public ImageServiceTests()
        {
            // Use a null logger implementation to avoid file I/O in tests
            _imageService = new ImageService(new NullLogger());
        }

        [Fact]
        public void CreateThumbnailImage_ValidImage_ReturnsBitmap()
        {
            // Arrange: create a small in-memory bitmap and save to temp file
            var temp = System.IO.Path.GetTempFileName();
            using (var bmp = new Bitmap(200, 100))
            {
                bmp.Save(temp, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            // Act
            var thumb = _imageService.CreateThumbnailImage(temp, new Size(64, 64));

            // Assert
            Assert.NotNull(thumb);
            Assert.IsType<Bitmap>(thumb);

            // Cleanup
            thumb?.Dispose();
            System.IO.File.Delete(temp);
        }

        [Fact]
        public void LoadImageFromFile_ValidImage_ReturnsBitmap()
        {
            var temp = System.IO.Path.GetTempFileName();
            using (var bmp = new Bitmap(50, 50))
            {
                bmp.Save(temp, System.Drawing.Imaging.ImageFormat.Png);
            }

            var img = _imageService.LoadImageFromFile(temp);
            Assert.NotNull(img);
            Assert.IsType<Bitmap>(img);
            img?.Dispose();
            System.IO.File.Delete(temp);
        }

        [Fact]
        public void GetSupportedExtensions_ReturnsNonEmptyArray()
        {
            var exts = _imageService.GetSupportedExtensions();
            Assert.NotNull(exts);
            Assert.True(exts.Length > 0);
        }

        // Minimal NullLogger used for tests
        private class NullLogger : IApplicationLogger
        {
            public void LogDebug(string message) { }
            public void LogError(string message, Exception? exception = null) { }
            public void LogInfo(string message) { }
            public void LogWarning(string message) { }
        }
    }
}
