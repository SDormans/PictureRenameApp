# User Guide

## Main window

- **Toolbar** (top): Open folder…, ◀ Prev, Rename, Undo rename, Remove from list, Delete, Review duplicates…, the **Find duplicates after opening** checkbox, Settings…, Next ▶.
- **Preview** (centre): the current picture, scaled to fit.
- **Info line**: file name, position in the folder, size, modified date, plus dimensions, EXIF date taken and camera when available.
- **Thumbnail strip** (bottom): every file in the folder. Thumbnails load in the background.

## Keyboard and mouse

| Action | Shortcut |
|---|---|
| Previous / next picture | ← / → or ◀ Prev / Next ▶ |
| Rename | F2 |
| Undo last rename | Ctrl+Z |
| Delete | Delete |
| Select one thumbnail | Click |
| Add or remove a thumbnail from the selection | Ctrl+click |
| Select a range | Shift+click |

Rename, Remove from list and Delete act on the selected thumbnails, or on the current picture when nothing is selected.

## Tasks

### Open a folder
Click **Open folder…**. Files in that folder are listed; subfolders are not scanned.
The last folder reopens on the next start unless turned off in [Settings](Settings.md).
With **Find duplicates after opening** ticked, the [duplicate review](Duplicate-Review.md) starts right after you open a folder this way. It does not start when the last folder is reopened at startup.

### Rename one file
Select one picture and press **Rename** (F2) and edit the full file name. Empty names and invalid characters are rejected. If the name is already taken, a number is added, for example `photo (2).jpg`.

### Batch rename
Select several thumbnails and press **Rename**. Enter a pattern such as `Holiday {n}` and the number padding.
By default the files move into a subfolder named after the pattern (`Holiday/`). See [Rename Patterns](Rename-Patterns.md).

### Undo a rename
**Undo rename** (Ctrl+Z) moves the files from the last rename or batch rename back to their old names. A subfolder created by that rename is removed if it ends up empty. Only the last rename can be undone.

### Remove from list
**Remove from list** takes files out of the list. A prompt lets you keep them on disk or delete them.

### Delete
**Delete** asks for confirmation, then sends the files to the Recycle Bin. Permanent deletion can be chosen in [Settings](Settings.md).

### Clean up duplicates
Click **Review duplicates…**, or tick **Find duplicates after opening**. See [Duplicate Review](Duplicate-Review.md).
