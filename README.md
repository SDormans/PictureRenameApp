# 📸 PictureRenameApp

PictureRenameApp is a feature-rich Windows Forms application for advanced file management, renaming, and duplicate detection. Built with a modern UI and professional-grade features for both casual users and power users.

---

## ✨ Features

### 📁 Core File Management
- **Drag & Drop** support for files and folders
- **Multi-view display** - Switch between thumbnail tiles and detailed list view
- **Live thumbnails** for images with async loading
- **File preview** panel with tabs (Preview, Metadata, Info)
- **Double-click** to open files with default OS program

### 🔄 Advanced Renaming
- **Single file rename** with custom dialogs
- **Batch rename** with automatic numbering (`basename_001`, `_002`, etc.)
- **Configurable start index** for batch operations
- **Live preview** of renaming pattern
- **Overwrite confirmation** with detailed file info
- **Multi-select support** (Ctrl+click, Shift+click)

### 🔍 Duplicate Detection
- **Find duplicates** by filename across selected files
- **Visual comparison** with up to 16 thumbnails per group
- **Click to select** which version to keep (blue highlight)
- **Keep Selected & Delete Others** button with confirmation
- **Double-click to preview** before deleting
- **Progress tracking** through duplicate groups

### 🖼️ Enhanced UI/UX
- **SplitContainer layout** with resizable panels
- **Tabbed preview** (Preview | Metadata | Info)
- **EXIF metadata display** for images
- **File information panel** with size, date, dimensions
- **Dark/Light mode** ready architecture
- **Responsive design** with dynamic panel sizing

---

## 🖥 How It Works

### 1️⃣ Load Files
- **Drag & drop** files directly into the thumbnail panel
- Or use **Open Folder** button to load entire directories
- Files appear as **thumbnails** (images) or **icons** (other files)

### 2️⃣ Select Files
- **Single click** - Select one file (blue highlight)
- **Ctrl+click** - Select multiple files
- **Shift+click** - Select a range of files
- Selected files show in **preview panel** with metadata

### 3️⃣ Choose Operation

#### **Rename Files**
- Click **Rename** button
- For **single file**: Enter new name (without extension)
- For **multiple files**: Enter base name and start index
- Preview the naming pattern before confirming
- Files are **moved** (not copied) with overwrite protection

#### **Find Duplicates**
- Select multiple files (at least 2)
- Click **Find Duplicates** button
- Review duplicate groups (max 16 thumbnails per group)
- **Click** on the version you want to keep
- Click **Keep Selected & Delete Others**
- Confirm deletion in the warning dialog

---

## 🏷 Naming Rules

| Situation | Result | Example |
|-----------|--------|---------|
| Single file | `NewName.ext` | `holiday.jpg` |
| Multiple files | `BaseName_###.ext` | `holiday_001.jpg` |
| Custom start index | `BaseName_###.ext` | `holiday_100.jpg` |

⚠ **Files are moved** (not copied) and existing files prompt for confirmation.

---

## 📂 Example Workflow

### Batch Rename Example:
Selected files:
photo1.jpg
photo2.jpg
photo3.jpg

Base name: "vacation"
Start index: 1

Result:
vacation_001.jpg
vacation_002.jpg
vacation_003.jpg

### Duplicate Detection Example:
Selected files with duplicate names:
C:\Photos\beach.jpg
C:\Backup\beach.jpg
C:\Downloads\beach.jpg

User selects: C:\Photos\beach.jpg (blue highlight)
Click "Keep Selected & Delete Others"
Result: Only C:\Photos\beach.jpg remains

---

## 🎯 Key Improvements

### From Original Version:
- ✅ **Multi-select** support (Ctrl/Shift)
- ✅ **Batch rename** with configurable numbering
- ✅ **Duplicate detection** with visual comparison
- ✅ **Live preview** panel with tabs
- ✅ **Thumbnails** for images (not just icons)
- ✅ **Overwrite confirmation** dialogs
- ✅ **File metadata** display (EXIF, size, dates)
- ✅ **Professional UI** with split containers
- ✅ **Async loading** for better performance
- ✅ **Memory-safe** image disposal
- ✅ **Unit tests** Validate expected behaviour 
- ✅ **Dependency injection** clean code
- ✅ **Error handling and Logging** maintainable code

---

## ⚠️ Current Limitations

- Duplicate detection is based on **filename only** (not content)
- Maximum **16 thumbnails** shown per duplicate group
- Files are **permanently deleted** (no recycle bin)
- No **undo** functionality (yet)

---

## 🛠 Requirements

- Windows 10/11
- .NET 6.0 or higher
- Visual Studio 2022 (recommended for development)

---

## 🚀 Future Improvements

### Code Quality
- [ ] Optimize memory usage and disposal

### Features
- [ ] Content-based duplicate detection (hash)
- [ ] Recycle Bin support
- [ ] Undo/Redo functionality
- [ ] Export reports (CSV/PDF)
---

## 📜 License

This project is intended for educational and demonstration purposes. Free to use and modify for personal projects.

---
