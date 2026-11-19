# Settings

Open **Settings…** on the toolbar. Settings are saved to `%APPDATA%\PictureRenameApp\settings.json`. If the file is missing or unreadable, defaults are used.

| Setting | Default | Effect |
|---|---|---|
| Move batch-renamed files into a subfolder | On | Default for the checkbox in the batch rename dialog ([Rename Patterns](Rename-Patterns.md#subfolder)) |
| Send deleted files to the Recycle Bin | On | Off deletes permanently |
| Reopen the last folder on startup | On | Reopens the folder from the previous run |
| Find duplicates and look-alikes after opening a folder | Off | Same as the toolbar checkbox; starts the [Duplicate Review](Duplicate-Review.md) after **Open folder…** |
| Default zero padding | 2 | Digits for `{n}` in batch rename (`01`, `02`, …) |

The last opened folder is also stored (`LastFolder`).
