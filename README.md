# Picture Rename App

[![build](https://github.com/SDormans/PictureRenameApp/actions/workflows/build.yml/badge.svg)](https://github.com/SDormans/PictureRenameApp/actions/workflows/build.yml)

A lightweight, fast Windows desktop application (WinForms) for browsing, previewing, and renaming image files. Picture Rename App simplifies single and batch renaming, helps find duplicates, and supports safe file operations with clear user confirmations.

Target platform: Windows (WinForms), .NET 8

---

## Overview

Picture Rename App provides a simple, focused UI to:

- Browse directories and view image thumbnails
- Preview images and view metadata
- Rename a single file or perform batch renames with automatic numbering
- Optionally move batch-renamed files into a dedicated subfolder named after the chosen base name
- Remove files from the current list or delete them from disk (Recycle Bin by default, prompted)
- Detect duplicate files and manage them (open, keep one and delete the other copies, show in Explorer)
- Remember preferences (subfolder default, Recycle Bin, zero padding, last folder) in a settings file

The project emphasizes reliability, predictable behavior, and efficient memory use when handling large image collections. The codebase follows a service-oriented design (image, file, logger services), centralized configuration, and safe UI threading patterns.

---

## Screenshots

![Main window](screenshots/main.png)

![Open folder](screenshots/open-folder.png)

![Batch rename dialog](screenshots/batch-rename.png)

![Remove or Delete prompt](screenshots/confirm-remove.png)

---

## Quick Start (Developer)

### Prerequisites

- .NET 8 SDK
- Windows (WinForms desktop application)
- Optional: Visual Studio 2022/2023 or Visual Studio Code

### Build and run

```bash
# Restore and build
dotnet restore
dotnet build

# Run the application
dotnet run --project PictureRenameApp.csproj
```

Alternatively, open the project in Visual Studio and run the `PictureRenameApp` project.

### Run tests

```bash
dotnet test PictureRenameApp.Tests/PictureRenameApp.Tests.csproj
```

The tests need Windows (the test host uses the Windows Desktop runtime). CI runs build and tests on `windows-latest` for every push to `master` and every pull request.

---

## Usage (End-user)

1. Click **Open folder…** to choose a directory with images. The last folder is reopened on the next start (can be turned off in **Settings…**).
2. Thumbnails are generated; click any thumbnail to preview the image and metadata.
3. Use **Rename** for single or batch renames. Batch renames use a pattern such as `Holiday {n}`; by default the renamed files are moved into a subfolder named after the base name (`./Holiday/`). Untick **Move into subfolder** in the dialog, or change the default in **Settings…**, to rename in place.
4. Use **Remove from list** to remove files from the displayed list. The prompt lets you either keep them on disk or delete them.
5. Use **Delete** to delete files. By default they go to the Recycle Bin; switch to permanent deletion in **Settings…**.
6. Use **Find duplicates…** to scan for identical files. Select a file in a group and use **Keep this, delete others**, **Open** (or double-click) to preview it, or **Show in Explorer**.

Notes

- The app always confirms before deleting.
- Settings are stored in `%APPDATA%\PictureRenameApp\settings.json`.

---

## Architecture (Developer)

Key locations in the codebase:

- `Controllers/` — `ApplicationController` orchestrates operations and updates the model
- `Services/` — `ImageService`, `FileService`, `ApplicationLogger`
- `Utilities/` — extension methods and `LRUCache`
- `Configuration/AppConstants.cs` — centralized constants
- `MainForm.cs` — WinForms UI and the app logic in use today (thumbnail strip, preview, toolbar, rename/delete/duplicates)
- `BatchRenameDialog.cs`, `RenameDialog.cs`, `DuplicatesForm.cs`, `SettingsDialog.cs` — dialogs
- `RenamePattern.cs` — batch rename pattern formatting and subfolder naming
- `AppSettings.cs` — persisted user settings (JSON)
- `Program.cs` — application entry point

Design highlights:

- Service-oriented architecture: image, file, and logging responsibilities are separated for testability
- Centralized constants and extensions to avoid duplication and magic numbers
- Controlled concurrency for thumbnail generation to keep the UI responsive

---

## Notable Behaviors

- Batch rename: by default creates a subfolder with the chosen base name and moves renamed files into it (re-running on files already in that subfolder does not nest it again)
- Remove action: prompts to either remove from list or delete; the delete path confirms again before removing files
- Delete: sends files to the Recycle Bin unless permanent deletion is chosen in Settings
- Thumbnail generation: optimized for speed and memory using controlled concurrency and lower-quality rendering suitable for thumbnails

---

## Contributing

Contributions are welcome. Please follow standard GitHub workflow:

1. Fork the repository
2. Create a branch for your feature or bugfix
3. Add tests and update documentation where appropriate
4. Run `dotnet build` and `dotnet test`
5. Submit a pull request

Please add a `CONTRIBUTING.md` if you want project-specific contributor guidelines.

---

## License

No license file has been added yet.

---

## Contact / Support

If you need support or want to report an issue, open an issue in this repository. Include screenshots and steps to reproduce the problem.

---

