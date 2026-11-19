# Architecture

## Two layers

| Layer | Files | Status |
|---|---|---|
| WinForms app | `MainForm.cs`, the dialogs, and the helper classes at the repo root | **In use**: all features live here |
| DI / MVC layer | `Controllers/`, `Services/`, `Models/`, `ServiceConfiguration.cs` | Registered in `Program.cs` and covered by tests, but **not used by the UI** yet |

`Program.cs` builds the DI container, gets the logger and controller, then runs `new MainForm()`. `MainForm` does not take any services.

## Code map (repo root)

| File | Role |
|---|---|
| `MainForm.cs` | Main window: toolbar, preview, thumbnail strip, selection, rename / undo / remove / delete, duplicate scan |
| `RenameDialog.cs` | Single rename |
| `BatchRenameDialog.cs` | Batch rename: pattern, padding, subfolder |
| `SettingsDialog.cs` | Settings |
| `DuplicateReviewForm.cs` | Step-by-step duplicate review window |
| `RenamePattern.cs` | Placeholder expansion, subfolder name, invalid and reserved names |
| `RenameOperation.cs` | Record of the last rename (old → new paths) and its undo |
| `ImageMetadata.cs`, `ExifParser.cs` | Dimensions, EXIF date taken, camera |
| `PerceptualHash.cs`, `ImageFingerprint.cs` | Look-alike fingerprint |
| `DuplicateGrouper.cs`, `DuplicateReviewSession.cs` | Grouping, ranking, review state |
| `AppSettings.cs` | Settings model, JSON load and save |
| `Theme.cs` | Dark colour palette |

Other folders:
- `PictureRenameApp/Configuration/AppConstants.cs`: constants used by the DI layer.
- `PictureRenameApp/Utilities/`: `ExtensionMethods`, `LRUCache`.
- `Utilities/SharedOperations.cs`: shared helpers such as safe image loading.

## How MainForm works

**State.** `_files` holds the folder listing (`FileInfo`) and `_index` the current picture. `_selection` and `_selectionAnchor` track multi-select, `_settings` the settings, and `_lastRename` the undo log.

**Preview.** `Render()` loads the current picture into a `MemoryStream`, so the file is not locked, and shows it with `Image.FromStream`. The previous image and stream are disposed each time.

**Thumbnails.** `RebuildThumbnails()` creates the tiles, and `QueueThumbnail` loads each one with `Task.Run`.
- Concurrency is limited by `_thumbThrottle` (half the CPU cores, at least 2).
- Results go into `_thumbCache`.
- A generation counter (`_thumbGeneration`) discards results from a folder that is no longer open.

**Rename.** `Rename_Click` calls `RenameSingle` for one file or `RenameBatch` for several.
- If a target name is taken, `SuggestUniqueName` adds ` (2)`, ` (3)`, …
- Every move is recorded in a `RenameOperation`.
- `ReplaceFileEntry` keeps the caches in sync with the new paths.

**Delete.** `DeleteFiles` uses `Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile` with `SendToRecycleBin`, or `File.Delete` when the Recycle Bin is turned off.

**Duplicates.** See [Duplicate Review](Duplicate-Review.md#code).
- `ScanDuplicatesAsync` runs on a background thread with `Parallel.For`, using 2–4 threads because decoding full-size photos uses a lot of memory.
- `_hashCache` and `_fingerprintCache` are `ConcurrentDictionary` caches keyed by path. An entry is reused only while the file's size and modified time are unchanged.

## Threading rules

- UI controls are only touched on the UI thread. Background work reports back through `BeginInvoke`, `Progress<T>`, or `await`.
- Caches shared with background work are `ConcurrentDictionary`.
- Images handed to controls are disposed when replaced.

## Design choices

- Logic without UI dependencies is kept out of the forms so it can be unit-tested: `RenamePattern`, `RenameOperation`, `ExifParser`, `PerceptualHash`, `DuplicateGrouper`, `DuplicateReviewSession`, `AppSettings`.
- Destructive actions always confirm first. Duplicate deletions are re-checked against the file state recorded during the scan.
