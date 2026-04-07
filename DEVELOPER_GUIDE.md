# Developer Guide - PictureRenameApp Refactored

## Quick Start for Developers

### Project Structure

```
PictureRenameApp/
??? Form1.cs                      # Main UI form (refactored with DI)
??? Form1.Designer.cs             # Designer generated code
??? Program.cs                    # Application entry point with DI setup
??? ServiceConfiguration.cs       # Dependency injection configuration
??? Services/
?   ??? ILogger.cs               # Logging interface
?   ??? ApplicationLogger.cs      # Logger implementation
?   ??? IImageService.cs         # Image operations interface
?   ??? ImageService.cs          # Image operations implementation
?   ??? IFileService.cs          # File operations interface
?   ??? FileService.cs           # File operations implementation
??? Utilities/
?   ??? DialogHelper.cs          # UI dialog utilities
??? Properties/
?   ??? Resources.resx           # Application resources
??? PictureRenameApp.csproj      # Project configuration
```

---

## Understanding the Refactored Code

### 1. Dependency Injection Flow

**Old Way (Tightly Coupled):**
```csharp
public partial class Form1 : Form
{
    public Form1()
    {
        var imageService = new ImageService(); // Hard dependency
        var fileService = new FileService();    // Hard dependency
    }
}
```

**New Way (Loosely Coupled):**
```csharp
public partial class Form1 : Form
{
    private readonly IApplicationLogger _logger;
    private readonly IImageService _imageService;
    private readonly IFileService _fileService;

    public Form1(
        IApplicationLogger logger,
        IImageService imageService,
        IFileService fileService)
    {
        _logger = logger;
        _imageService = imageService;
        _fileService = fileService;
    }
}
```

### 2. Service Layer Pattern

**IImageService Interface:**
```csharp
public interface IImageService
{
    Image? CreateThumbnailImage(string filePath, Size size);
    Image? LoadImageFromFile(string filePath);
    string GetImageFormatString(Image image);
    string[] GetSupportedExtensions();
}
```

**Implementation with Logging:**
```csharp
public class ImageService : IImageService
{
    private readonly IApplicationLogger _logger;

    public ImageService(IApplicationLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Image? CreateThumbnailImage(string filePath, Size size)
    {
        try
        {
            _logger.LogDebug($"Creating thumbnail for: {filePath}");
            // Implementation with error handling
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create thumbnail", ex);
            return null;
        }
    }
}
```

### 3. Error Handling Pattern

All service methods follow this pattern:

```csharp
public void MethodName(string parameter)
{
    // 1. Validate input
    if (string.IsNullOrWhiteSpace(parameter))
        throw new ArgumentException("...", nameof(parameter));

    try
    {
        // 2. Log start
        _logger.LogInfo($"Starting operation with: {parameter}");

        // 3. Perform operation
        // ... business logic ...

        // 4. Log success
        _logger.LogInfo("Operation completed successfully");
    }
    catch (SpecificException ex)
    {
        // 5. Log specific errors
        _logger.LogError("Specific error occurred", ex);
        throw; // Re-throw for caller to handle
    }
    catch (Exception ex)
    {
        // 6. Log unexpected errors
        _logger.LogError("Unexpected error", ex);
        throw;
    }
}
```

### 4. Form1.cs UI Event Handlers

All event handlers follow this pattern:

```csharp
private void EventHandler_Click(object sender, EventArgs e)
{
    try
    {
        _logger.LogInfo("Event triggered");
        
        // Validate state
        if (GetSelectedFiles().Count == 0)
        {
            _logger.LogWarning("No files selected");
            MessageBox.Show("Please select files", "Warning");
            return;
        }

        // Perform operation using services
        _fileService.RenameFile(src, dest);

        // Show success feedback
        MessageBox.Show("Operation completed", "Success");
        _logger.LogInfo("Event handled successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError("Event handler failed", ex);
        MessageBox.Show($"Error: {ex.Message}", "Error");
    }
}
```

---

## Adding New Features

### Example: Adding Video Support

**Step 1: Extend IImageService**
```csharp
public interface IImageService
{
    // ... existing methods ...
    bool IsVideoFile(string filePath);
    Image? GetVideoThumbnail(string filePath);
}
```

**Step 2: Update ImageService**
```csharp
public class ImageService : IImageService
{
    private static readonly string[] SupportedVideoExtensions = 
        { ".mp4", ".avi", ".mkv", ".mov" };

    public bool IsVideoFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return SupportedVideoExtensions.Contains(ext);
    }

    public Image? GetVideoThumbnail(string filePath)
    {
        try
        {
            _logger.LogDebug($"Extracting thumbnail from video: {filePath}");
            // Implementation using FFmpeg or similar
            _logger.LogInfo("Video thumbnail extracted");
            return thumbnail;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to extract video thumbnail", ex);
            return null;
        }
    }
}
```

**Step 3: Use in Form1**
```csharp
private void LoadDirectory_Updated(string directoryPath)
{
    foreach (var file in files)
    {
        Image? thumb = null;

        if (_imageService.IsVideoFile(file))
        {
            thumb = _imageService.GetVideoThumbnail(file);
            _logger.LogInfo($"Loaded video thumbnail: {file}");
        }
        else
        {
            thumb = _imageService.CreateThumbnailImage(file, _thumbnailSize);
        }

        // ... rest of thumbnail creation ...
    }
}
```

---

## Testing Services

### Unit Test Example (using xUnit)

```csharp
[Fact]
public void RenameFile_WithValidInput_RenamesSuccessfully()
{
    // Arrange
    var logger = new MockLogger();
    var fileService = new FileService(logger);
    var source = "test.jpg";
    var destination = "renamed.jpg";

    // Act
    fileService.RenameFile(source, destination);

    // Assert
    Assert.True(File.Exists(destination));
    Assert.False(File.Exists(source));
}

[Fact]
public void RenameFile_WithInvalidSource_ThrowsException()
{
    // Arrange
    var logger = new MockLogger();
    var fileService = new FileService(logger);

    // Act & Assert
    Assert.Throws<FileNotFoundException>(() =>
        fileService.RenameFile("nonexistent.jpg", "new.jpg")
    );
}
```

---

## Logging Best Practices

### Do's ?

```csharp
// Do: Log with context
_logger.LogInfo($"Loading directory: {directoryPath} containing {fileCount} files");

// Do: Log errors with exceptions
catch (Exception ex)
{
    _logger.LogError("Failed to process file", ex);
}

// Do: Log state changes
_logger.LogInfo("UI refreshed with new data");

// Do: Use appropriate levels
_logger.LogDebug("Detailed operation step");     // Developer-level detail
_logger.LogInfo("Major operation completed");    // User-visible event
_logger.LogWarning("Non-critical issue");        // Recoverable problem
_logger.LogError("Critical operation failed", ex); // Error with exception
```

### Don'ts ?

```csharp
// Don't: Log with PII
_logger.LogInfo($"User password: {password}");

// Don't: Log in tight loops
for (int i = 0; i < 1000000; i++)
{
    _logger.LogDebug($"Iteration {i}"); // Performance impact!
}

// Don't: Lose exception context
catch (Exception ex)
{
    _logger.LogError("Error occurred"); // Don't include exception!
    throw;
}

// Don't: Mix concerns
_logger.LogInfo($"File renamed AND thumbnail created AND UI updated");
// Instead: Log each operation separately
```

---

## Common Patterns

### Pattern 1: Resource Cleanup

```csharp
try
{
    using var stream = File.OpenRead(filePath);
    using var image = Image.FromStream(stream);
    // ... use image ...
} // stream and image automatically disposed
```

### Pattern 2: Null Coalescing

```csharp
// Modern C#: Use ?? and ?.
var name = Path.GetFileName(path) ?? "Unknown";
var size = fileInfo?.Length ?? 0;

// Pattern matching
if (sender is not PictureBox pb)
    return;
```

### Pattern 3: Validation Guard Clauses

```csharp
public void ValidateAndProcess(string input)
{
    // Early returns prevent nesting
    if (string.IsNullOrWhiteSpace(input))
    {
        _logger.LogWarning("Empty input");
        throw new ArgumentException("Input required");
    }

    if (!File.Exists(input))
    {
        _logger.LogError($"File not found: {input}");
        throw new FileNotFoundException();
    }

    // Main logic only executes if validation passes
    ProcessFile(input);
}
```

### Pattern 4: Try-Catch-Log-Rethrow

```csharp
try
{
    // Operation
}
catch (SpecificException ex) when (condition)
{
    _logger.LogError("Specific case", ex);
    throw new ApplicationException("...", ex);
}
catch (Exception ex)
{
    _logger.LogError("Unexpected error", ex);
    throw;
}
```

---

## Performance Considerations

### Memory Management
```csharp
// ? Good: Images are disposed after use
public Image? LoadImageFromFile(string filePath)
{
    var bytes = File.ReadAllBytes(filePath);
    using var ms = new MemoryStream(bytes);
    var image = Image.FromStream(ms);
    var bitmap = new Bitmap(image);
    image.Dispose(); // Dispose original
    return bitmap;   // Return copy
}
```

### Thumbnail Creation
```csharp
// ? Good: Reuse Graphics object with using
using var g = Graphics.FromImage(bmp);
g.CompositingQuality = CompositingQuality.HighQuality;
g.InterpolationMode = InterpolationMode.HighQualityBicubic;
g.DrawImage(src, x, y, thumbW, thumbH);
// Graphics automatically disposed
```

### Thread Safety
```csharp
// ? Good: Use lock for file operations
private readonly object _lockObject = new object();

lock (_lockObject)
{
    File.AppendAllText(logFilePath, logEntry);
}
```

---

## Debugging Tips

### Enable Debug Logging
All debug logs are only written in Debug builds (conditional):
```csharp
#if DEBUG
    WriteLog("DEBUG", message);
#endif
```

### Check Log Files
Logs are automatically created in:
```
<ApplicationDirectory>/Logs/app_YYYY-MM-DD.log
```

### Use Breakpoints
Set breakpoints in service methods to trace calls:
```csharp
public void RenameFile(string src, string dest) // ? Breakpoint here
{
    // Step through to see flow
}
```

### Monitor Output Window
In Visual Studio: View ? Output (shows debug logs in real-time)

---

## Refactoring Checklist

When adding new functionality:

- [ ] Create interface in appropriate service folder
- [ ] Create implementation with logging
- [ ] Register in ServiceConfiguration
- [ ] Add error handling (try-catch-log pattern)
- [ ] Update Form1 to use service
- [ ] Add XML documentation comments
- [ ] Test with both valid and invalid inputs
- [ ] Check logs for appropriate messages
- [ ] Dispose resources properly (using statements)
- [ ] Run build to verify compilation

---

## Useful Resources

- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Exception Handling Best Practices](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/exceptions/best-practices-for-exceptions)
- [C# Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)

---

## Questions & Support

For issues or questions:
1. Check the log files in `Logs/` folder
2. Review the ARCHITECTURE.md for design patterns
3. Examine similar service implementations for patterns
4. Run in debug mode with breakpoints

Happy coding! ??
