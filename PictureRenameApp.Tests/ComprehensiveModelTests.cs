using System;
using System.Collections.Generic;
using System.Drawing;
using PictureRenameApp.Models;

namespace PictureRenameApp.Tests
{
    /// <summary>
    /// Comprehensive unit tests for ApplicationModel.
    /// Covers happy paths, edge cases, error cases, and boundary conditions.
    /// </summary>
    public class ComprehensiveApplicationModelTests
    {
        // ========== ThumbnailItem Tests ==========

        [Fact]
        public void ThumbnailItem_Constructor_InitializesProperties()
        {
            // Arrange & Act
            var item = new ThumbnailItem();

            // Assert
            Assert.Equal(string.Empty, item.FilePath);
            Assert.Null(item.Thumbnail);
            Assert.False(item.IsSelected);
        }

        [Fact]
        public void ThumbnailItem_CanSetProperties()
        {
            // Arrange
            var item = new ThumbnailItem();
            var image = new Bitmap(100, 100);

            // Act
            item.FilePath = "test.jpg";
            item.Thumbnail = image;
            item.IsSelected = true;

            // Assert
            Assert.Equal("test.jpg", item.FilePath);
            Assert.Equal(image, item.Thumbnail);
            Assert.True(item.IsSelected);
        }

        [Fact]
        public void ThumbnailItem_Dispose_DisposesImage()
        {
            // Arrange
            var item = new ThumbnailItem();
            var image = new Bitmap(100, 100);
            item.Thumbnail = image;

            // Act
            item.Dispose();

            // Assert - No exception should be thrown
            // Disposed Bitmap on Windows may throw ObjectDisposedException or ArgumentException from GDI+.
            var ex = Record.Exception(() => _ = image.Width);
            Assert.NotNull(ex);
            Assert.True(ex is ObjectDisposedException or ArgumentException, $"Unexpected: {ex.GetType().Name}");
        }

        [Fact]
        public void ThumbnailItem_Dispose_WithNullThumbnail_DoesNotThrow()
        {
            // Arrange
            var item = new ThumbnailItem { Thumbnail = null };

            // Act & Assert
            item.Dispose();
        }

        // ========== ApplicationModel Constructor Tests ==========

        [Fact]
        public void Constructor_InitializesThumbnailsCollection()
        {
            // Arrange & Act
            var model = new ApplicationModel();

            // Assert
            Assert.NotNull(model.Thumbnails);
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public void Constructor_InitializesEmptyMetadataText()
        {
            // Arrange & Act
            var model = new ApplicationModel();

            // Assert
            Assert.Equal(string.Empty, model.MetadataText);
        }

        [Fact]
        public void Constructor_InitializesNullPreviewImage()
        {
            // Arrange & Act
            var model = new ApplicationModel();

            // Assert
            Assert.Null(model.PreviewImage);
        }

        [Fact]
        public void Constructor_InitializesNullCurrentDirectory()
        {
            // Arrange & Act
            var model = new ApplicationModel();

            // Assert
            Assert.Null(model.CurrentDirectory);
        }

        // ========== CurrentDirectory Property Tests ==========

        [Fact]
        public void CurrentDirectory_CanBeSet()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            model.CurrentDirectory = "/path/to/directory";

            // Assert
            Assert.Equal("/path/to/directory", model.CurrentDirectory);
        }

        [Fact]
        public void CurrentDirectory_CanBeSetToNull()
        {
            // Arrange
            var model = new ApplicationModel();
            model.CurrentDirectory = "/some/path";

            // Act
            model.CurrentDirectory = null;

            // Assert
            Assert.Null(model.CurrentDirectory);
        }

        [Fact]
        public void CurrentDirectory_CanBeSetToEmpty()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            model.CurrentDirectory = string.Empty;

            // Assert
            Assert.Equal(string.Empty, model.CurrentDirectory);
        }

        // ========== MetadataText Property Tests ==========

        [Fact]
        public void MetadataText_CanBeSet()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            model.MetadataText = "Some metadata";

            // Assert
            Assert.Equal("Some metadata", model.MetadataText);
        }

        [Fact]
        public void MetadataText_CanBeSetToEmpty()
        {
            // Arrange
            var model = new ApplicationModel();
            model.MetadataText = "Initial text";

            // Act
            model.MetadataText = string.Empty;

            // Assert
            Assert.Equal(string.Empty, model.MetadataText);
        }

        [Fact]
        public void MetadataText_CanContainMultipleLines()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            model.MetadataText = "Line1\nLine2\nLine3";

            // Assert
            Assert.Equal("Line1\nLine2\nLine3", model.MetadataText);
        }

        // ========== PreviewImage Property Tests ==========

        [Fact]
        public void PreviewImage_CanBeSet()
        {
            // Arrange
            var model = new ApplicationModel();
            var image = new Bitmap(100, 100);

            // Act
            model.PreviewImage = image;

            // Assert
            Assert.Equal(image, model.PreviewImage);
        }

        [Fact]
        public void PreviewImage_CanBeSetToNull()
        {
            // Arrange
            var model = new ApplicationModel();
            model.PreviewImage = new Bitmap(100, 100);

            // Act
            model.PreviewImage = null;

            // Assert
            Assert.Null(model.PreviewImage);
        }

        [Fact]
        public void PreviewImage_CanBeReplacedMultipleTimes()
        {
            // Arrange
            var model = new ApplicationModel();
            var image1 = new Bitmap(100, 100);
            var image2 = new Bitmap(200, 200);

            // Act
            model.PreviewImage = image1;
            model.PreviewImage = image2;

            // Assert
            Assert.Equal(image2, model.PreviewImage);
        }

        // ========== AddThumbnail Tests ==========

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
        public void AddMultipleThumbnails_InsertsAllItems()
        {
            // Arrange
            var model = new ApplicationModel();
            var thumb1 = new ThumbnailItem { FilePath = "test1.jpg" };
            var thumb2 = new ThumbnailItem { FilePath = "test2.jpg" };
            var thumb3 = new ThumbnailItem { FilePath = "test3.jpg" };

            // Act
            model.Thumbnails.Add(thumb1);
            model.Thumbnails.Add(thumb2);
            model.Thumbnails.Add(thumb3);

            // Assert
            Assert.True(model.Thumbnails.Count == 3);
        }

        // ========== SelectedFilePath Tests ==========

        [Fact]
        public void SelectedFilePath_WithNoSelection_ReturnsNull()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act
            var selected = model.SelectedFilePath;

            // Assert
            Assert.Null(selected);
        }

        [Fact]
        public void SelectedFilePath_WithOneSelection_ReturnsThatPath()
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
        public void SelectedFilePath_WithMultipleSelections_ReturnsFirstSelected()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test3.jpg", IsSelected = false });

            // Act
            var selected = model.SelectedFilePath;

            // Assert
            Assert.Equal("test1.jpg", selected);
        }

        [Fact]
        public void SelectedFilePath_WithEmptyCollection_ReturnsNull()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            var selected = model.SelectedFilePath;

            // Assert
            Assert.Null(selected);
        }

        // ========== SelectedFilePaths Tests ==========

        [Fact]
        public void SelectedFilePaths_WithNoSelection_ReturnsEmptyList()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act
            var selected = model.SelectedFilePaths;

            // Assert
            Assert.Empty(selected);
        }

        [Fact]
        public void SelectedFilePaths_WithOneSelection_ReturnsThatPath()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = false });

            // Act
            var selected = model.SelectedFilePaths;

            // Assert
            Assert.True(selected.Count == 1);
            Assert.Contains("test1.jpg", selected);
        }

        [Fact]
        public void SelectedFilePaths_WithMultipleSelections_ReturnsAllSelected()
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
        public void SelectedFilePaths_WithEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act
            var selected = model.SelectedFilePaths;

            // Assert
            Assert.Empty(selected);
        }

        [Fact]
        public void SelectedFilePaths_ReturnsNewListEachTime()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = true });

            // Act
            var selected1 = model.SelectedFilePaths;
            var selected2 = model.SelectedFilePaths;

            // Assert
            Assert.False(ReferenceEquals(selected1, selected2)); // Different list instances
            Assert.Equal(selected1.Count, selected2.Count); // But same content
        }

        // ========== ToggleThumbnailSelection Tests (Single Select) ==========

        [Fact]
        public void ToggleThumbnailSelection_SingleSelect_SelectsTarget()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("test.jpg", multiSelect: false);

            // Assert
            Assert.True(model.Thumbnails[0].IsSelected);
        }

        [Fact]
        public void ToggleThumbnailSelection_SingleSelect_OnSoleSelectedItem_KeepsSelected()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = true });

            // Act
            model.ToggleThumbnailSelection("test.jpg", multiSelect: false);

            // Assert — single-select clears others then toggles: lone item ends selected again
            Assert.True(model.Thumbnails[0].IsSelected);
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
        public void ToggleThumbnailSelection_SingleSelect_WithMultipleSelections_ClearsAll()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test3.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("test3.jpg", multiSelect: false);

            // Assert
            Assert.False(model.Thumbnails[0].IsSelected);
            Assert.False(model.Thumbnails[1].IsSelected);
            Assert.True(model.Thumbnails[2].IsSelected);
        }

        // ========== ToggleThumbnailSelection Tests (Multi Select) ==========

        [Fact]
        public void ToggleThumbnailSelection_MultiSelect_SelectsTarget()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("test.jpg", multiSelect: true);

            // Assert
            Assert.True(model.Thumbnails[0].IsSelected);
        }

        [Fact]
        public void ToggleThumbnailSelection_MultiSelect_TogglesSelection()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = true });

            // Act
            model.ToggleThumbnailSelection("test.jpg", multiSelect: true);

            // Assert
            Assert.False(model.Thumbnails[0].IsSelected);
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
        public void ToggleThumbnailSelection_MultiSelect_CanDeselectWithOtherSelected()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });

            // Act
            model.ToggleThumbnailSelection("test1.jpg", multiSelect: true);

            // Assert
            Assert.False(model.Thumbnails[0].IsSelected);
            Assert.True(model.Thumbnails[1].IsSelected);
        }

        // ========== ToggleThumbnailSelection Tests (Invalid Path) ==========

        [Fact]
        public void ToggleThumbnailSelection_WithNonExistentPath_DoesNothing()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("nonexistent.jpg", multiSelect: false);

            // Assert
            Assert.False(model.Thumbnails[0].IsSelected);
        }

        [Fact]
        public void ToggleThumbnailSelection_WithNullPath_DoesNotThrow()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act & Assert
            model.ToggleThumbnailSelection(null!, multiSelect: false);
        }

        [Fact]
        public void ToggleThumbnailSelection_WithEmptyCollection_DoesNotThrow()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act & Assert
            model.ToggleThumbnailSelection("test.jpg", multiSelect: false);
        }

        // ========== ClearThumbnails Tests ==========

        [Fact]
        public void ClearThumbnails_RemovesAllThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg" });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg" });

            // Act
            model.ClearThumbnails();

            // Assert
            Assert.Empty(model.Thumbnails);
        }

        [Fact]
        public void ClearThumbnails_ClearsCurrentDirectory()
        {
            // Arrange
            var model = new ApplicationModel();
            model.CurrentDirectory = "/some/path";

            // Act
            model.ClearThumbnails();

            // Assert
            Assert.Null(model.CurrentDirectory);
        }

        [Fact]
        public void ClearThumbnails_DisposesAllThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            var image1 = new Bitmap(100, 100);
            var image2 = new Bitmap(100, 100);
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", Thumbnail = image1 });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", Thumbnail = image2 });

            // Act
            model.ClearThumbnails();

            // Assert - disposed Bitmap may surface ObjectDisposedException or ArgumentException (GDI+)
            foreach (var img in new[] { image1, image2 })
            {
                var ex = Record.Exception(() => _ = img.Width);
                Assert.NotNull(ex);
                Assert.True(ex is ObjectDisposedException or ArgumentException, $"Unexpected: {ex.GetType().Name}");
            }
        }

        [Fact]
        public void ClearThumbnails_WithEmptyCollection_DoesNotThrow()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act & Assert
            model.ClearThumbnails();
        }

        [Fact]
        public void ClearThumbnails_CanBeCalled_MultipleTimesSuccessively()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg" });

            // Act & Assert
            model.ClearThumbnails();
            model.ClearThumbnails(); // Should not throw
        }

        // ========== ClearSelection Tests ==========

        [Fact]
        public void ClearSelection_ClearsAllSelections()
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
        public void ClearSelection_WithNoSelection_DoesNotThrow()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test.jpg", IsSelected = false });

            // Act & Assert
            model.ClearSelection();
        }

        [Fact]
        public void ClearSelection_WithEmptyCollection_DoesNotThrow()
        {
            // Arrange
            var model = new ApplicationModel();

            // Act & Assert
            model.ClearSelection();
        }

        [Fact]
        public void ClearSelection_DoesNotRemoveThumbnails()
        {
            // Arrange
            var model = new ApplicationModel();
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = true });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = true });

            // Act
            model.ClearSelection();

            // Assert
            Assert.True(model.Thumbnails.Count == 2);
        }

        // ========== Integration Tests ==========

        [Fact]
        public void MultipleOperations_MaintainConsistentState()
        {
            // Arrange
            var model = new ApplicationModel();
            model.CurrentDirectory = "/path";
            model.MetadataText = "metadata";
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test1.jpg", IsSelected = false });
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "test2.jpg", IsSelected = false });

            // Act
            model.ToggleThumbnailSelection("test1.jpg", multiSelect: false);
            model.ToggleThumbnailSelection("test2.jpg", multiSelect: true);

            // Assert
            Assert.True(model.Thumbnails[0].IsSelected);
            Assert.True(model.Thumbnails[1].IsSelected);
            Assert.True(model.SelectedFilePaths.Count == 2);
        }

        [Fact]
        public void ClearAllAndReload_WorksCorrectly()
        {
            // Arrange
            var model = new ApplicationModel();
            model.CurrentDirectory = "/path";
            model.MetadataText = "old metadata";
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "old.jpg", IsSelected = true });

            // Act
            model.ClearThumbnails();
            model.MetadataText = "new metadata";
            model.CurrentDirectory = "/newpath";
            model.Thumbnails.Add(new ThumbnailItem { FilePath = "new.jpg", IsSelected = false });

            // Assert
            Assert.True(model.Thumbnails.Count == 1);
            Assert.Equal("new.jpg", model.Thumbnails[0].FilePath);
            Assert.Equal("new metadata", model.MetadataText);
            Assert.Equal("/newpath", model.CurrentDirectory);
        }
    }
}
