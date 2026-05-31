# Picture Rename App

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![License](https://img.shields.io/badge/license-MIT-blue)]()

A lightweight, fast Windows desktop application (WinForms) for browsing, previewing, and renaming image files. Picture Rename App simplifies single and batch renaming, helps find duplicates, and supports safe file operations with clear user confirmations.

Target platform: Windows (WinForms), .NET 8

---

## What the project does

Picture Rename App provides a simple, focused UI to:

- Browse directories and view image thumbnails
- Preview images and view metadata
- Rename a single file or perform batch renames with automatic numbering
- Optionally move batch-renamed files into a dedicated subfolder named after the chosen base name
- Remove files from the current list or permanently delete them from disk (prompted)
- Detect duplicate files and (optionally) remove duplicates

This project emphasizes reliability, predictable behavior, and efficient memory use when handling large image collections.

---

## Why this project is useful

Key features and benefits:

- Fast, memory-conscious thumbnail generation
- Batch rename convenience with automatic destination-folder creation
- Clear prompts for destructive operations (delete vs remove)
- Extensible, testable service layer (image, file, logger) making it easy to adapt or extend
- Simple UI optimized for quick image renaming workflows

---

## Getting started (developer)

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

If a test project is configured:

```bash
dotnet test
```

---

## Usage (end-user)

1. Click **Open folder…** to choose a directory with images.
2. Thumbnails are generated; click any thumbnail to preview the image and metadata.
3. Use **Rename** for single or batch renames. For batch renames, the renamed files are placed into a subfolder named after your base name.
4. Use **Remove** to remove files from the displayed list — a prompt lets you choose to remove (keep on disk) or delete (permanently remove from disk).
5. Use **Find duplicates…** to scan for and manage duplicates.

Notes

- Deleting files is permanent; the app will confirm before deleting.
- Batch renames move files into `./<BaseName>/` inside the active directory by default.

---

## Developer guide & architecture

Key locations in the codebase:

- `Controllers/` — ApplicationController and interfaces
- `Services/` — ImageService, FileService, ApplicationLogger
- `Utilities/` — Extension methods and LRU cache
- `Configuration/AppConstants.cs` — centralized constants
- `Program.cs` — application entry point

Design highlights:

- Service-oriented architecture: image, file, and logging responsibilities are separated for testability
- Centralized constants and extensions to avoid duplication and magic numbers
- Controlled concurrency for thumbnail generation to keep the UI responsive

---

## Useful commands

- Build: `dotnet build`
- Run: `dotnet run --project PictureRenameApp.csproj`
- Test: `dotnet test` (if tests are present)

---

## Documentation & help

More detailed documentation is included in this repository:

- Documentation index: `DOCUMENTATION_INDEX.md`
- Quick developer reference: `QUICK_REFERENCE_GUIDE.md`
- Comprehensive refactor notes: `COMPREHENSIVE_REFACTORING_REPORT.md`
- Refactoring checklist: `REFACTORING_CHECKLIST.md`

If you need support or want to report an issue, open an issue in this repository.

---

## Contributing

Contributions are welcome. Please follow standard GitHub workflow:

1. Fork the repository
2. Create a branch for your feature or bugfix
3. Add tests and update documentation where appropriate
4. Run `dotnet build` and `dotnet test`
5. Submit a pull request

Please add a `CONTRIBUTING.md` file if you want project-specific contributor guidelines.

---

## Maintainers

See the repository owners and contributors on GitHub.

---

## License

See the `LICENSE` file in the repository for license details.

---

If you'd like, I can also:

- Add a CONTRIBUTING.md with suggested guidelines
- Add a GitHub Actions CI workflow and badge
- Add a sample screenshot and more usage examples

Tell me which you'd prefer next.
