using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using PictureRenameApp.Models;
using PictureRenameApp.Services;

namespace PictureRenameApp.Controllers
{
    /// <summary>
    /// Implementation of the application controller for business logic orchestration.
    /// Handles all business logic operations with thread-safe model updates.
    /// </summary>
    public class ApplicationController : IApplicationController
    {
        private readonly IApplicationLogger _logger;
        private readonly IImageService _imageService;
        private readonly IFileService _fileService;
        private readonly IApplicationModel _model;
        private readonly Control? _uiSynchronizationContext;
        private readonly Size _thumbnailSize = new Size(128, 128);

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationController"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics</param>
        /// <param name="imageService">Service for image operations</param>
        /// <param name="fileService">Service for file operations</param>
        /// <param name="model">Application model</param>
        /// <param name="uiControl">UI control for thread synchronization (optional)</param>
        public ApplicationController(
            IApplicationLogger logger,
            IImageService imageService,
            IFileService fileService,
            IApplicationModel model,
            Control? uiControl = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _uiSynchronizationContext = uiControl;
        }

        /// <inheritdoc/>
        public IApplicationModel GetModel() => _model;

        /// <inheritdoc/>
        public async Task LoadDirectoryAsync(string directoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    _logger.LogWarning($"Invalid directory path: {directoryPath}");
                    ClearModelState();
                    return;
                }

                _logger.LogInfo($"Loading directory: {directoryPath}");
                _model.CurrentDirectory = directoryPath;

                // Clear existing thumbnails with suspended notifications
                MarshalToUIThread(() => ClearThumbnailsWithSuspendedNotifications());

                var files = _fileService.GetImageFilesInDirectory(directoryPath);

                if (files.Count == 0)
                {
                    _logger.LogInfo("No images found in directory");
                    MarshalToUIThread(() =>
                    {
                        _model.MetadataText = "No images in this directory.";
                        _model.PreviewImage?.Dispose();
                        _model.PreviewImage = null;
                    });
                    return;
                }

                // Load thumbnails asynchronously
                await LoadThumbnailsAsync(files);

                // Select first thumbnail on UI thread
                if (_model.Thumbnails.Count > 0)
                {
                    MarshalToUIThread(() =>
                    {
                        _model.ToggleThumbnailSelection(_model.Thumbnails[0].FilePath, false);
                    });
                    
                    await DisplayFileAsync(_model.Thumbnails[0].FilePath);
                }

                _logger.LogInfo($"Successfully loaded {files.Count} thumbnails");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading directory thumbnails", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DisplayFileAsync(string filePath
            )
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found: {filePath}");
                    return;
                }

                // Load preview image asynchronously
                var previewImage = await Task.Run(() => _imageService.LoadImageFromFile(filePath));
                
                // Validate image before assigning to model
                if (previewImage != null && (previewImage.Width <= 0 || previewImage.Height <= 0))
                {
                    _logger.LogWarning($"Loaded preview image has invalid dimensions: {previewImage.Width}x{previewImage.Height}");
                    previewImage.Dispose();
                    previewImage = null;
                }

                // Update model on UI thread if necessary
                MarshalToUIThread(() =>
                {
                    _model.PreviewImage?.Dispose();
                    _model.PreviewImage = previewImage;
                });

                // Get metadata
                var fileInfo = new FileInfo(filePath);
                var imageFormat = previewImage != null 
                    ? _imageService.GetImageFormatString(previewImage) 
                    : "Unknown";
                var fileSize = _fileService.FormatFileSize(fileInfo.Length);
                var metadata = $"Size: {fileSize}\nFormat: {imageFormat}\nPath: {filePath}";
                
                MarshalToUIThread(() =>
                {
                    _model.MetadataText = metadata;
                });

                _logger.LogDebug($"Displayed file: {Path.GetFileName(filePath)}");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"GDI+ error displaying file {Path.GetFileName(filePath)}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error displaying file {filePath}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task RenameSingleFileAsync(string sourcePath, string newName)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    _logger.LogWarning($"File not found for rename: {sourcePath}");
                    return;
                }

                var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                var extension = Path.GetExtension(sourcePath);
                var destinationPath = Path.Combine(directory, $"{newName}{extension}");

                await Task.Run(() => _fileService.RenameFile(sourcePath, destinationPath, overwrite: false));
                _logger.LogInfo($"File renamed: {Path.GetFileName(sourcePath)} -> {newName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error renaming file {sourcePath}", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RenameBatchFilesAsync(List<string> selectedFiles, string baseName, int startIndex)
        {
            try
            {
                if (selectedFiles == null || selectedFiles.Count == 0)
                {
                    _logger.LogWarning("No files selected for batch rename");
                    return;
                }

                int index = startIndex;
                foreach (var filePath in selectedFiles)
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning($"File not found for batch rename: {filePath}");
                        continue;
                    }

                    var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                    var extension = Path.GetExtension(filePath);
                    var newNamePart = $"{baseName}_{index:D3}";
                    var destinationPath = Path.Combine(directory, $"{newNamePart}{extension}");
                    
                    await Task.Run(() => _fileService.RenameFile(filePath, destinationPath, overwrite: false));
                    _logger.LogDebug($"Batch renamed: {Path.GetFileName(filePath)} -> {newNamePart}");
                    
                    index++;
                }

                _logger.LogInfo($"Batch rename completed for {selectedFiles.Count} files");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during batch rename", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            try
            {
                MarshalToUIThread(() => ClearModelState());
                _logger.LogInfo("Application state cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error clearing application state", ex);
            }
        }

        /// <summary>
        /// Clears thumbnails while suspending collection change notifications to prevent UI race conditions.
        /// </summary>
        private void ClearThumbnailsWithSuspendedNotifications()
        {
            if (_model.Thumbnails is ObservableCollection<ThumbnailItem> collection)
            {
                // Use ToArray instead of ToList for slightly better performance
                var itemsToDispose = collection.ToArray();
                collection.Clear();
                foreach (var item in itemsToDispose)
                {
                    item.Dispose();
                }
            }
            else
            {
                _model.ClearThumbnails();
            }
        }

        /// <summary>
        /// Loads thumbnails from a list of file paths asynchronously.
        /// Ensures model updates are thread-safe by marshaling to UI thread.
        /// Validates thumbnails before adding to model.
        /// </summary>
        private async Task LoadThumbnailsAsync(List<string> files)
        {
            // Limit concurrency to avoid overwhelming the system (e.g., 4 at a time)
            int maxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4);
            using var semaphore = new System.Threading.SemaphoreSlim(maxDegreeOfParallelism);
            var thumbnailTasks = new List<Task<(string file, Image? thumb)>>();

            foreach (var file in files)
            {
                await semaphore.WaitAsync();
                thumbnailTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var thumb = _imageService.CreateThumbnailImage(file, _thumbnailSize);
                        return (file, thumb);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            var results = await Task.WhenAll(thumbnailTasks);

            foreach (var (file, thumb) in results)
            {
                try
                {
                    if (thumb == null)
                    {
                        _logger.LogDebug($"Thumbnail creation returned null for {Path.GetFileName(file)}");
                        continue;
                    }

                    if (thumb.Width <= 0 || thumb.Height <= 0)
                    {
                        _logger.LogWarning($"Thumbnail has invalid dimensions: {thumb.Width}x{thumb.Height} for {Path.GetFileName(file)}");
                        thumb.Dispose();
                        continue;
                    }

                    var thumbnail = new ThumbnailItem
                    {
                        FilePath = file,
                        Thumbnail = thumb,
                        IsSelected = false
                    };

                    MarshalToUIThread(() =>
                    {
                        _model.Thumbnails.Add(thumbnail);
                    });
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError($"GDI+ error creating thumbnail for {Path.GetFileName(file)}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create thumbnail for {Path.GetFileName(file)}", ex);
                }
            }
        }

        /// <summary>
        /// Clears all model state.
        /// </summary>
        private void ClearModelState()
        {
            ClearThumbnailsWithSuspendedNotifications();
            _model.MetadataText = string.Empty;
            _model.PreviewImage?.Dispose();
            _model.PreviewImage = null;
            _model.ClearSelection();
        }

        /// <summary>
        /// Marshals an action to the UI thread if necessary, otherwise executes immediately.
        /// </summary>
        private void MarshalToUIThread(Action action)
        {
            if (_uiSynchronizationContext?.InvokeRequired == true)
            {
                _uiSynchronizationContext.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
