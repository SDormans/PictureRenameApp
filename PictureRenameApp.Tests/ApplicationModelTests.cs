using PictureRenameApp.Models;
using System.Drawing;

namespace PictureRenameApp.Tests
{
    /// <summary>
    /// Unit tests for ApplicationModel.
    /// </summary>
    public class ApplicationModelTests
    {
        [Fact]
        public void Constructor_InitializesThumbnailsCollection()
        {
            // Arrange & Act
            var model = new ApplicationModel();

            // Assert
            Assert.NotNull(model.Thumbnails);
            Assert.True(model.Thumbnails.Count == 0);
        }

        [Fact]
        public void AddThumbnail_AddsItemToCollection()
        {
            // Arrange
            var model = new ApplicationModel();
            var thumbnail = new ThumbnailItem { FilePath = "test.jpg" };

            // Act
            model.Thumbnails.Add(thumbnail);

            // Assert
            Assert.True(model.Thumbnails.Count == 1);
            Assert.Equal("test.jpg", model.Thumbnails[0].FilePath);
        }

        [Fact]
        public void SelectedFilePath_ReturnsFirstSelectedThumbnail()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = false });

            // Act
            var selected = model.SelectedFilePath;

            // Assert
            Assert.Equal("test1.jpg", selected);
        }

        [Fact]
        public void SelectedFilePaths_ReturnsAllSelectedThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test3.jpg", IsSelected = false });

            // Act
            var selected = model.SelectedFilePaths;

            // Assert
            Assert.True(selected.Count == 2);
            Assert.Contains("test1.jpg", selected);
            Assert.Contains("test2.jpg", selected);
        }

        [Fact]
        public void ToggleThumbnailSelection_SingleSelect_ClearsOtherSelections()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("test2.jpg", multiSelect: false);

            // Assert
            Assert.False(model.Thumbnails[0].IsSelected);
            Assert.True(model.Thumbnails[1].IsSelected);
        }

        [Fact]
        public void ToggleThumbnailSelection_MultiSelect_KeepsOtherSelections()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("test2.jpg", multiSelect: true);

            // Assert
            Assert.True(model.Thumbnails[0].IsSelected);
            Assert.True(model.Thumbnails[1].IsSelected);
        }

        [Fact]
        public void ClearThumbnails_RemovesAllItems()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg" });
            model.CurrentDirectory = "C:\\test";

            // Act
            model.ClearThumbnails();

            // Assert
            Assert.True(model.Thumbnails.Count == 0);
            Assert.Null(model.CurrentDirectory);
        }

        [Fact]
        public void ClearSelection_DeselectsAllThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });

            // Act
            model.ClearSelection();

            // Assert
            Assert.False(model.Thumbnails[0].IsSelected);
            Assert.False(model.Thumbnails[1].IsSelected);
        }

        [Fact]
        public void CurrentDirectory_CanBeSet()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            model.CurrentDirectory = "C:\\test";

            // Assert
            Assert.Equal("C:\\test", model.CurrentDirectory);
        }

        [Fact]
        public void MetadataText_CanBeSet()
        {
            // Arrange
            var model = new ApplicationModel();
            var text = "Test metadata";

            // Act
            model.MetadataText = text;

            // Assert
            Assert.Equal(text, model.MetadataText);
        }

        [Fact]
        public void PreviewImage_CanBeSet()
        {
            // Arrange
            var model = new ApplicationModel();
            using var bitmap = new Bitmap(100, 100);

            // Act
            model.PreviewImage = bitmap;

            // Assert
            Assert.NotNull(model.PreviewImage);
            Assert.Equal(100, model.PreviewImage.Width);
        }
    }
}
