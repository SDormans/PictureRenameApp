# Quick Reference - PictureRenameApp Refactored

## Dependency Injection Cheat Sheet

### Services Available

```csharp
// Injected into Form1
private readonly IApplicationLogger _logger;      // For logging
private readonly IImageService _imageService;    // For image operations
private readonly IFileService _fileService;      // For file operations
```

### How to Use Each Service

#### IApplicationLogger
```csharp
_logger.LogInfo("Operation started");
_logger.LogDebug("Detailed step");
_logger.LogWarning("Non-critical issue");
_logger.LogError("Failed to process", exception);
```

#### IImageService
```csharp
var thumbnail = _imageService.CreateThumbnailImage(filePath, size);
var image = _imageService.LoadImageFromFile(filePath);
var format = _imageService.GetImageFormatString(image);
var extensions = _imageService.GetSupportedExtensions();
```

#### IFileService
```csharp
var files = _fileService.GetImageFilesInDirectory(path);
_fileService.RenameFile(src, dest, overwrite: true);
var sizeString = _fileService.FormatFileSize(fileSize);
```

---

## Common Patterns

### Pattern: Rename Single File
```csharp
try
{
    var src = GetSelectedFile();
    var ext = Path.GetExtension(src);
    var newName = DialogHelper.PromptForString(this, "Rename", "New name:");
    var dest = Path.Combine(Path.GetDirectoryName(src), newName + ext);
    
    _fileService.RenameFile(src, dest, overwrite: true);
    _logger.LogInfo($"Renamed: {Path.GetFileName(src)} ? {newName}");
    LoadDirectoryThumbnails(Path.GetDirectoryName(src));
}
catch (Exception ex)
{
    _logger.LogError("Rename failed", ex);
    MessageBox.Show($"Error: {ex.Message}");
}
```

### Pattern: Batch Rename Multiple Files
```csharp
var selected = GetSelectedThumbnailFilePaths();
var batchInfo = DialogHelper.PromptForBatchRename(this);

foreach (var (src, index) in selected.Select((f, i) => (f, i)))
{
    var ext = Path.GetExtension(src);
    var destName = $"{batchInfo.Value.BaseName}_{batchInfo.Value.StartIndex + index:D3}{ext}";
    var dest = Path.Combine(Path.GetDirectoryName(src), destName);
    
    _fileService.RenameFile(src, dest);
}
```

### Pattern: Load Directory with Error Handling
```csharp
private void LoadDirectoryThumbnails(string directoryPath)
{
    if (!Directory.Exists(directoryPath))
    {
        _logger.LogWarning($"Directory not found: {directoryPath}");
        ShowPlaceholder();
        return;
    }

    try
    {
        _logger.LogInfo($"Loading directory: {directoryPath}");
        var files = _fileService.GetImageFilesInDirectory(directoryPath);
        
        // ... create thumbnails ...
        
        _logger.LogInfo($"Loaded {files.Count} images");
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to load directory", ex);
        MessageBox.Show("Error loading directory", "Error");
    }
}
```

### Pattern: Display File Metadata
```csharp
private void DisplayFileMetadata(string filePath)
{
    try
    {
        var fileInfo = new FileInfo(filePath);
        var metadata = new StringBuilder();
        
        metadata.AppendLine($"File: {fileInfo.Name}");
        metadata.AppendLine($"Size: {_fileService.FormatFileSize(fileInfo.Length)}");
        metadata.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
        
        if (previewPictureBox.Image != null)
        {
            metadata.AppendLine($"Format: {_imageService.GetImageFormatString(previewPictureBox.Image)}");
        }
        
        metadataTextBox.Text = metadata.ToString();
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to display metadata", ex);
    }
}
```

---

## Adding a New Service

### Step 1: Create Interface
```csharp
// File: Services/IMyService.cs
public interface IMyService
{
    /// <summary>Describe method</summary>
    void DoSomething(string parameter);
}
```

### Step 2: Implement Service
```csharp
// File: Services/MyService.cs
public class MyService : IMyService
{
    private readonly IApplicationLogger _logger;

    public MyService(IApplicationLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void DoSomething(string parameter)
    {
        try
        {
            _logger.LogInfo($"Starting operation: {parameter}");
            // Implementation
            _logger.LogInfo("Operation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Operation failed", ex);
            throw;
        }
    }
}
```

### Step 3: Register in DI
```csharp
// File: ServiceConfiguration.cs
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    services.AddSingleton<IApplicationLogger, ApplicationLogger>();
    services.AddSingleton<IImageService, ImageService>();
    services.AddSingleton<IFileService, FileService>();
    services.AddSingleton<IMyService, MyService>();  // ? ADD THIS
    
    return services.BuildServiceProvider();
}
```

### Step 4: Inject into Form1
```csharp
// File: Form1.cs
private readonly IMyService _myService;

public Form1(
    IApplicationLogger logger,
    IImageService imageService,
    IFileService fileService,
    IMyService myService)  // ? ADD THIS
{
    _logger = logger;
    _imageService = imageService;
    _fileService = fileService;
    _myService = myService;  // ? ADD THIS
}
```

### Step 5: Use in Code
```csharp
_myService.DoSomething(parameter);
```

---

## Logging Cheat Sheet

### Log Levels

```csharp
_logger.LogInfo("User clicked rename button");        // User action
_logger.LogDebug("Processing file #5 of 10");         // Development detail
_logger.LogWarning("File already exists, skipping");  // Recoverable issue
_logger.LogError("Failed to create thumbnail", ex);   // Exception
```

### When to Log What

| Situation | Level | Example |
|-----------|-------|---------|
| Application starting | INFO | `"=== Application Starting ==="`|
| User action | INFO | `"Rename button clicked"` |
| File operation complete | INFO | `"File renamed successfully"` |
| Detailed step | DEBUG | `"Creating thumbnail for image.jpg"` |
| Config loaded | DEBUG | `"Loaded 5 supported extensions"` |
| Missing optional data | WARN | `"No metadata available"` |
| Recoverable error | WARN | `"File skipped (invalid format)"` |
| Operation failed | ERROR | `"Failed to process file"` |
| Exception occurred | ERROR | `"Unexpected error", ex` |

### Best Practices

? **DO:**
```csharp
_logger.LogInfo("Starting batch rename with 5 files");
_logger.LogError("Failed to rename file", exception);
_logger.LogDebug("Thumbnail resolution: 128x128");
```

? **DON'T:**
```csharp
_logger.LogInfo("Operation completed");  // Too vague
_logger.LogError("Error");               // No context
_logger.LogInfo(userPassword);           // Security risk
```

---

## File Organization Map

```
Services/
??? ILogger.cs                    ? Logging interface
??? ApplicationLogger.cs          ? Logging implementation
??? IImageService.cs             ? Image interface
??? ImageService.cs              ? Image implementation
??? IFileService.cs              ? File interface
??? FileService.cs               ? File implementation

Utilities/
??? DialogHelper.cs              ? Dialog utilities

Form1.cs                          ? Main UI form (uses services)
Program.cs                        ? Entry point (configures DI)
ServiceConfiguration.cs           ? DI setup
```

---

## Troubleshooting Quick Guide

### Issue: Service not found in DI
**Solution:** Check ServiceConfiguration.cs - ensure service is registered
```csharp
services.AddSingleton<IMyService, MyService>();  // Add if missing
```

### Issue: NullReferenceException on _logger
**Solution:** Ensure service is injected in Form1 constructor
```csharp
public Form1(IApplicationLogger logger, ...)  // Add parameter
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### Issue: No log files created
**Solution:** Check permissions on application directory. Logs should be in `Logs/` subfolder.

### Issue: Logging not working
**Solution:** Check log levels - DEBUG only logs in Debug builds
```csharp
#if DEBUG
    WriteLog("DEBUG", message);
#endif
```

### Issue: Form won't start
**Solution:** Check log file for startup exception:
- Navigate to `Logs/app_YYYY-MM-DD.log`
- Look for errors at application start
- See DEVELOPER_GUIDE.md for debugging

---

## Performance Tips

### Optimize Thumbnail Loading
```csharp
// Good: Create once, reuse
var supportedExtensions = _imageService.GetSupportedExtensions();
foreach (var file in files)
{
    if (supportedExtensions.Contains(ext))
        // Process
}

// Bad: Create each iteration
foreach (var file in files)
{
    if (_imageService.GetSupportedExtensions().Contains(ext))
        // Process
}
```

### Memory Management
```csharp
// Good: Dispose images when done
try
{
    var image = _imageService.LoadImageFromFile(path);
    // Use image
}
finally
{
    image?.Dispose();
}

// Better: Use using statement
using var image = _imageService.LoadImageFromFile(path);
// Image auto-disposed after block
```

---

## Testing Service Locally

### Create Mock Service for Testing
```csharp
public class MockImageService : IImageService
{
    public Image? CreateThumbnailImage(string filePath, Size size)
    {
        return new Bitmap(size.Width, size.Height);
    }

    public Image? LoadImageFromFile(string filePath)
    {
        return new Bitmap(100, 100);
    }

    public string GetImageFormatString(Image image) => "JPEG";
    
    public string[] GetSupportedExtensions() 
        => new[] { ".jpg", ".png" };
}

// Use mock for testing
var mockLogger = new MockLogger();
var mockImageService = new MockImageService();
var fileService = new FileService(mockLogger, mockImageService);
```

---

## Key Files Reference

| File | Purpose | Edit? |
|------|---------|-------|
| `Program.cs` | Application entry + DI setup | ?? Only add services |
| `Form1.cs` | Main UI window | ? Frequently |
| `ServiceConfiguration.cs` | DI registration | ? When adding services |
| `Services/*` | Business logic | ? Implement features |
| `Utilities/*` | Helper functions | ? As needed |
| `ARCHITECTURE.md` | Design overview | ?? Reference |
| `DEVELOPER_GUIDE.md` | Development patterns | ?? Reference |

---

## Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `NullReferenceException: _logger` | Service not injected | Check constructor injection |
| `FileNotFoundException` | File doesn't exist | Use `File.Exists()` check first |
| `UnauthorizedAccessException` | No write permission | Check file/directory permissions |
| `OutOfMemoryException` | Image too large | Implement async loading |
| `Service not found` | Not registered in DI | Add to ServiceConfiguration.cs |

---

## Quick Shortcuts

### Format Date/Time
```csharp
DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")  // 2024-01-15 14:30:45
```

### Format File Size
```csharp
_fileService.FormatFileSize(1024 * 1024)  // Returns "1 MB"
```

### Get File Extension
```csharp
Path.GetExtension(filePath).ToLower()  // Returns ".jpg"
```

### Get File Name Only
```csharp
Path.GetFileName(filePath)  // Returns "photo.jpg"
```

### Get Directory Path
```csharp
Path.GetDirectoryName(filePath)  // Returns "C:\Photos"
```

---

## Resources

?? **Documentation**
- ARCHITECTURE.md - System design
- DEVELOPER_GUIDE.md - Development patterns
- REFACTORING_SUMMARY.md - What changed

?? **External**
- [Microsoft DI Docs](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [C# Logging Best Practices](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)

?? **Log Location**
- `<AppDirectory>/Logs/app_YYYY-MM-DD.log`

---

**Print this sheet and keep it handy!** ??
