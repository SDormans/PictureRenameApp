```markdown
# 📸 PictureRenameApp

PictureRenameApp is a Windows Forms application that allows users to drag and drop files, automatically rename them using a custom base name, and copy them into a newly created folder inside the user's **Documents** directory.

The application also displays file icons for dropped files and allows basic file interaction.

---

## ✨ Features

- Drag & Drop file support
- Automatic folder creation in **Documents**
- Batch file renaming with numbering
- Automatic numbering for multiple files (`_001`, `_002`, etc.)
- Displays file icons inside a `FlowLayoutPanel`
- Clear all displayed files option
- Basic file launching support

---

## 🖥 How It Works

### 1️⃣ Drag and Drop Files

- Drag one or multiple files into the main panel.
- Each file will be displayed as a button containing:
  - The file name
  - The associated system icon

### 2️⃣ Enter a Base Name

Type a base name into the textbox.

This name will be used as:
- The folder name
- The new file name

### 3️⃣ Click the Process Button

When you click the button:

- A new folder is created in:

```

Documents<BaseName>\

```

- Files are copied into this folder.

---

## 🏷 Naming Rules

| Situation        | Result |
|------------------|--------|
| One file        | `BaseName.extension` |
| Multiple files  | `BaseName_001.extension`<br>`BaseName_002.extension`<br>`BaseName_003.extension` |

⚠ Existing files will be overwritten.

---

## 📂 Example

If the base name entered is:

---

Holiday

---

And 3 files are dropped:

---

photo1.jpg
photo2.jpg
photo3.jpg

---

They will be copied to:

---

Documents\Holiday\

---

And renamed to:

---

Holiday_001.jpg
Holiday_002.jpg
Holiday_003.jpg

---

---

## 🧩 Project Structure

### `Form1.cs`

Contains:
- Drag & Drop handling
- File renaming and copying logic
- Dynamic button creation
- File launching logic

---

## ⚠️ Known Limitations

- Files are **copied**, not moved.
- Existing files are overwritten without confirmation.
- Double-click open functionality may require improvement.
- "Delete All" feature is not implemented.
- No file type filtering (all file types are accepted).

---

## 🛠 Requirements

- Windows OS
- .NET (Windows Forms)
- Visual Studio (recommended)

---

## 🚀 Possible Improvements

- Add file type filtering (e.g. images only)
- Add overwrite confirmation dialog
- Implement delete functionality
- Add progress indicator
- Improve double-click file launching
- Add move instead of copy option
- Add drag-to-reorder support

---

## 📜 License

This project is intended for educational and demonstration purposes.
```
