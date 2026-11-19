# Development

## Requirements
- Windows
- .NET 8 SDK
- Optional: Visual Studio 2022

## Build and run
```bash
dotnet build PictureRenameApp.sln
dotnet run --project PictureRenameApp.csproj
```

## Tests
```bash
dotnet test PictureRenameApp.Tests/PictureRenameApp.Tests.csproj
```
- xUnit, targeting `net8.0-windows`. The test project is not part of `PictureRenameApp.sln`, so run it by path.
- `PictureRenameApp.csproj` excludes `PictureRenameApp.Tests/**` from compilation, because the test project sits inside the app folder.
- `PictureRenameApp.Tests/Tests/PictureRenameApp.Tests.csproj` is an older, unused project file. CI does not run it.
- Tests cover the DI layer (controller, model, services) and the pure helpers: rename patterns, undo, EXIF parsing, settings, perceptual hash, grouping and review session.
- Forms and GDI+ code (`MainForm`, dialogs, `ImageFingerprint`) have no automated tests. Check them by running the app.

## CI
`.github/workflows/build.yml` runs on every push to `master` and every pull request. On `windows-latest` with .NET 8 it runs:
1. `dotnet build PictureRenameApp.sln -c Release`
2. `dotnet test PictureRenameApp.Tests/PictureRenameApp.Tests.csproj -c Release`

## Building on Linux or macOS
The app can be compiled, but not run, outside Windows:
```bash
dotnet build PictureRenameApp.sln -p:EnableWindowsTargeting=true
```
The tests need the Windows Desktop runtime and fail to start on other systems. Rely on CI or a Windows machine for them.

## Conventions
- New behaviour goes in the active WinForms path (`MainForm.cs` and helpers). Keep UI-free logic in small classes with tests.
- Dispose `Image` and stream objects you replace. Do UI updates on the UI thread.
- Confirm before anything destructive. Prefer the Recycle Bin.
- Branch, open a pull request, and make sure CI passes.
