# Duplicate Review

Finds pictures that are exact copies or look-alikes, then walks you through them one group at a time.

## Using it

1. Click **Review duplicates…**, or tick **Find duplicates after opening** and open a folder.
2. A progress bar shows the scan.
3. Each group shows "Group X of Y" and whether it holds identical copies or look-alikes. Every picture is a tile showing name, dimensions, size, and date taken (or modified).
4. The suggested file is pre-selected and marked **★ Suggested to keep**. Choose the file to keep:
   - Click a tile, or use ← / →.
   - **Keep selected** (Enter, or double-click a tile) records the choice and moves on.
   - **Keep all (skip)** leaves the group alone.
   - **◀ Back** (Backspace) returns to the previous group to change your choice.
   - **Show in Explorer** opens the folder with the file selected.
   - **Stop** jumps to the summary; groups not yet reviewed are left alone.
5. The summary lists how many files will be deleted and their total size. **Delete N file(s)** deletes them (Recycle Bin by default). **Close without deleting** discards all choices.

Nothing is deleted while you step through the groups.

## Which file is suggested

Files in a group are ranked by:
1. most pixels (highest resolution)
2. largest file
3. shortest name (prefers `photo.jpg` over `photo (2).jpg` or `photo - copy.jpg`)
4. oldest modified date

## How matching works

**Exact copies.** Only files that share their size with another file are hashed (SHA-1). Files with equal hashes are identical.

**Look-alikes.** Each picture gets a 64-bit perceptual fingerprint ("difference hash"):
1. The picture is shrunk to 9×8 pixels in grayscale, with mirrored edges so the border is not darkened.
2. Each bit records whether a pixel is brighter than its right neighbour.

Resizing, re-saving and recompressing barely change this fingerprint. Two pictures are look-alikes when at most **8 of the 64 bits** differ (`DuplicateGrouper.DefaultMaxDistance`).

- Matches are transitive: if A matches B and B matches C, all three form one group. Every file is in at most one group.
- A group is shown as identical copies only if every file in it has the same hash. Groups of identical copies come first.
- These files are never matched as look-alikes, only as exact copies:
  - very flat images, such as solid colours or almost-black pictures (`PerceptualHash.MinContrast`)
  - animated GIFs and multi-page TIFFs
- Symbolic links are skipped. Cloud files such as OneDrive placeholders are scanned.
- Rotated or mirrored copies are not detected, and burst shots may or may not match depending on how different they are.

## Safety checks before deleting

When you confirm, every group is checked again:
- If the file you kept is missing, or its size or modified time changed since the scan, nothing in that group is deleted.
- A copy whose size or modified time changed since the scan is left alone. The other unchanged copies in its group are still deleted.

The final message tells you how many files were deleted and how many were left alone because they changed.

## Code

| File | Role |
|---|---|
| `PerceptualHash.cs` | dHash from 9×8 luminance, Hamming distance |
| `ImageFingerprint.cs` | Decodes a file with GDI+ and computes its fingerprint and dimensions |
| `DuplicateGrouper.cs` | Union-find grouping, ranking (`OrderByPreference`) |
| `DuplicateReviewSession.cs` | Review state: `Keep`, `Skip`, `Back`, `Decisions` |
| `DuplicateReviewForm.cs` | The review window |
| `MainForm.cs` | `ReviewDuplicates`, `ScanDuplicatesAsync`, `DeleteReviewed`, hash and fingerprint caches |
