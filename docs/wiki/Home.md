# Picture Rename App

A Windows desktop app (WinForms, .NET 8) for browsing a folder of pictures, renaming them one at a time or in batches, and cleaning up duplicates and look-alikes.

## Pages

| Page | For | Contents |
|---|---|---|
| [User Guide](User-Guide.md) | Users | Main window, toolbar, keyboard shortcuts, everyday tasks |
| [Rename Patterns](Rename-Patterns.md) | Users | `{n}`, `{name}`, `{date}` placeholders, subfolders, undo |
| [Duplicate Review](Duplicate-Review.md) | Users and developers | How exact copies and look-alikes are found and reviewed |
| [Settings](Settings.md) | Users | Every option and where it is stored |
| [Architecture](Architecture.md) | Developers | Code map, how the main window works, threading and caching |
| [Development](Development.md) | Developers | Build, run, test, CI, contributing |

## Features at a glance

- Thumbnail strip and large preview with metadata: size, modified date, dimensions, EXIF date taken and camera
- Single and batch rename with placeholders, optional move into a subfolder, and undo
- Remove files from the list, or delete them (Recycle Bin by default)
- Step-by-step review of exact duplicates and look-alikes (resized, re-saved, recompressed)
- Settings saved between runs, including reopening the last folder

Supported picture types: `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`, `.webp`, `.tiff` (case-insensitive).
Pictures are loaded with GDI+, so a format only previews if Windows can decode it.
