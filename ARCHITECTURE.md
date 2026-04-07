# Refactored Architecture Guide

## Overview

The PictureRenameApp has been refactored with a professional architecture following SOLID principles, dependency injection, comprehensive logging, and error handling.

---

## Architecture Layers

### 1. **Presentation Layer** (UI)
- **Form1.cs** - Main application window
  - Handles all user interactions
  - Receives injected services via constructor
  - Delegates business logic to services
  - Includes comprehensive error handling and logging

### 2. **Service Layer** (Business Logic)
Located in `Services/` folder:

#### **IApplicationLogger** & **ApplicationLogger**
- Thread-safe logging with file and debug output
- Automatic log file creation in `Logs/` folder
- Log levels: INFO, WARNING, ERROR, DEBUG
- Use: Track application flow and diagnose issues

#### **IImageService** & **ImageService**
- Thumbnail generation with high-quality rendering
- Image loading from file (memory-safe)
- Image format detection
- Use: All image-related operations

#### **IFileService** & **FileService**
- Directory scanning for image files
- File renaming with validation and overwrite protection
- File size formatting
- Use: File system operations with error handling

### 3. **Utility Layer**
- **DialogHelper.cs** - Encapsulates UI dialogs
  - PromptForString() - Single string input
  - PromptForBatchRename() - Batch rename parameters

### 4. **Configuration**
- **ServiceConfiguration.cs** - Dependency Injection setup
  - Registers all services as singletons
  - Centralizes DI configuration

---

## Dependency Injection Pattern

### How DI Works

Services are registered in `ServiceConfiguration.cs`:

```csharp
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    services.AddSingleton<IApplicationLogger, ApplicationLogger>();
    services.AddSingleton<IImageService, ImageService>();
    services.AddSingleton<IFileService, FileService>();
    return services.BuildServiceProvider();
}
```

### Constructor Injection

Form1.cs receives dependencies via constructor:

```csharp
public Form1(
    IApplicationLogger logger,
    IImageService imageService,
    IFileService fileService)
{
    _logger = logger;
    _imageService = imageService;
    _fileService = fileService;
}
```

### Benefits
- ? Loose coupling between components
- ? Easy to test with mock implementations
- ? Centralized configuration
- ? Singleton pattern prevents duplicate instances

---

## Logging Architecture

### Log Levels

| Level  | When Used | Example |
|--------|-----------|---------|
| **INFO** | Major operations | "File loaded successfully" |
| **DEBUG** | Detailed flow | "Thumbnail created for file.jpg" |
| **WARN** | Non-critical issues | "Directory not found" |
| **ERROR** | Exceptions and failures | "Failed to load image" |

### Log Output

1. **Console/Debug Output** - Always written to debug window
2. **File Logging** - Written to `Logs/app_YYYY-MM-DD.log`

### Usage Example

```csharp
_logger.LogInfo($"Loading directory: {path}");
_logger.LogDebug($"Processing file: {filename}");
_logger.LogWarning($"File skipped: already renamed");
_logger.LogError($"Critical error occurred", exception);
```

---

## Error Handling Strategy

### Three-Layer Error Handling

#### 1. **Service Layer** (Preventive)
Services validate inputs and handle expected errors:

```csharp
public void RenameFile(string src, string dest, bool overwrite = false)
{
    if (string.IsNullOrWhiteSpace(src))
        throw new ArgumentException("Source cannot be empty");
    
    if (!File.Exists(src))
    {
        _logger.LogError($"File not found: {src}");
        throw new FileNotFoundException();
    }
}
```

#### 2. **UI Layer** (Recovery)
Form catches service exceptions and shows user-friendly dialogs:

```csharp
try
{
    _fileService.RenameFile(src, dest);
}
catch (Exception ex)
{
    _logger.LogError("Rename failed", ex);
    MessageBox.Show($"Rename failed: {ex.Message}", "Error");
}
```

#### 3. **Application Layer** (Fallback)
Program.cs catches startup errors:

```csharp
catch (Exception ex)
{
    MessageBox.Show($"Failed to start: {ex.Message}");
    logger.LogError("Startup failed", ex);
}
```

### Error Recovery Patterns

| Scenario | Action | Example |
|----------|--------|---------|
| File not found | Log & skip | Missing image in directory |
| Permission denied | Log & show dialog | Can't write to protected folder |
| Out of memory | Log & fallback | Thumbnail generation for huge image |
| Invalid user input | Log & prompt again | Empty rename field |

---

## Configuration & Startup Flow

### Program.cs Startup

```
1. ApplicationConfiguration.Initialize()
   ?
2. ServiceConfiguration.ConfigureServices()
   - Creates ServiceCollection
   - Registers all services
   - Returns configured ServiceProvider
   ?
3. Resolve Form1 with injected services
   ?
4. Application.Run(form)
   ?
5. Log application end
```

---

## Service Interfaces (Contracts)

### IApplicationLogger
```csharp
public interface IApplicationLogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
    void LogDebug(string message);
}
```

### IImageService
```csharp
public interface IImageService
{
    Image? CreateThumbnailImage(string filePath, Size size);
    Image? LoadImageFromFile(string filePath);
    string GetImageFormatString(Image image);
    string[] GetSupportedExtensions();
}
```

### IFileService
```csharp
public interface IFileService
{
    List<string> GetImageFilesInDirectory(string directoryPath);
    void RenameFile(string sourcePath, string destinationPath, bool overwrite = false);
    string FormatFileSize(long bytes);
}
```

---

## Benefits of This Architecture

### ? **Maintainability**
- Clear separation of concerns
- Easy to locate and modify code
- Self-documenting through interfaces

### ? **Testability**
- Services can be mocked for unit testing
- No hard dependencies on UI framework
- Deterministic behavior through DI

### ? **Scalability**
- Easy to add new features
- Services can be extended without breaking existing code
- Logging can be enhanced without code changes

### ? **Reliability**
- Comprehensive error handling at each layer
- Detailed logging for diagnostics
- Graceful degradation on errors

### ? **Performance**
- Singleton services prevent duplicate instantiation
- Async-ready (future enhancement)
- Memory-safe image disposal

---

## Future Enhancements

### Planned Improvements
1. **Async Operations** - Background thread for long-running tasks
2. **Configuration File** - Settings file for user preferences
3. **Unit Tests** - Test suite for services
4. **Content-Based Duplicates** - Hash-based detection
5. **Undo/Redo** - Operation history tracking
6. **Advanced Logging** - Serilog integration

### Adding a New Service

1. Create interface in `Services/INewService.cs`
2. Create implementation in `Services/NewService.cs`
3. Register in `ServiceConfiguration.cs`:
   ```csharp
   services.AddSingleton<INewService, NewService>();
   ```
4. Inject into Form1 or other services
5. Use throughout application

---

## Best Practices Implemented

? **SOLID Principles**
- Single Responsibility: Each service has one purpose
- Open/Closed: Interfaces allow extension
- Liskov Substitution: Can swap implementations
- Interface Segregation: Focused, small interfaces
- Dependency Inversion: Depends on abstractions

? **Error Handling**
- Try-catch at service boundaries
- Meaningful error messages
- Exception logging with context
- User-friendly dialogs

? **Logging**
- Structured logging at all major operations
- Thread-safe log writing
- Separate debug and file output
- Automatic log directory creation

? **Documentation**
- XML doc comments on public methods
- Inline comments for complex logic
- Architecture guide (this file)
- Clear variable naming

? **Resource Management**
- Proper image disposal
- Lock objects for thread safety
- Stream cleanup with using statements

---

## Troubleshooting

### Issue: Application won't start
**Check:**
- `Logs/` folder for error details
- Windows event viewer for framework errors
- Ensure .NET 8 is installed

### Issue: Missing log files
**Check:**
- Application has write permissions to installation directory
- Antivirus isn't blocking file creation
- Disk space is available

### Issue: Slow thumbnail loading
**Check:**
- System memory usage
- Image sizes (very large images slow processing)
- Hard disk speed
- Consider async loading in future versions

### Issue: File rename fails with "Access denied"
**Check:**
- File is not open in another application
- User has write permissions
- Check logs for detailed error

---

## Summary

The refactored architecture provides:
- Professional-grade code organization
- Comprehensive error handling
- Detailed logging for diagnostics
- Dependency injection for flexibility
- Clear separation of concerns
- Foundation for future scaling

For questions or improvements, refer to the inline documentation and comments in the source code.
