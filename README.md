📸 PictureRenameApp

PictureRenameApp is a Windows Forms application that allows users to drag and drop image files, automatically rename them using a custom base name, and copy them into a newly created folder inside the user's Documents directory.

The application also displays file icons for dropped files and allows basic file interaction.

✨ Features

Drag & Drop file support

Automatic folder creation in Documents

Batch file renaming with numbering

Automatic numbering for multiple files (_001, _002, etc.)

Displays file icons inside a FlowLayoutPanel

Clear all displayed files option

Opens files via double click (basic implementation)

🖥 How It Works
1️⃣ Drag and Drop Files

Drag one or multiple files into the main panel.

Each file will be displayed as a button with:

The file name

The associated system icon

2️⃣ Enter a Base Name

Type a base name into the textbox.

This name will be used:

As the folder name

As the new file name

3️⃣ Click the Process Button

When you click the button:

A new folder is created in:

Documents\<BaseName>\


Files are copied into this folder.

Naming Rules:
Situation	Result
One file	BaseName.extension
Multiple files	BaseName_001.extension
BaseName_002.extension
BaseName_003.extension

Existing files will be overwritten.

📂 Example

If the base name entered is:

Holiday


And 3 files are dropped:

photo1.jpg
photo2.jpg
photo3.jpg


They will be copied to:

Documents\Holiday\


And renamed to:

Holiday_001.jpg
Holiday_002.jpg
Holiday_003.jpg

🧩 Project Structure

Main components:

Form1.cs

Drag & Drop handling

File renaming and copying

Dynamic button creation

File launching logic

⚠️ Known Limitations

Files are copied, not moved.

Overwrites existing files without confirmation.

Double-click open functionality may need improvement.

"Delete All" option is not implemented yet.

No file type filtering (all file types are accepted).

🛠 Requirements

.NET (Windows Forms)

Windows OS

Visual Studio (recommended for development)

🚀 Possible Improvements

Add file type filtering (e.g. images only)

Add confirmation before overwriting files

Implement delete functionality

Add progress indication

Improve double-click file launching

Add drag reordering

Add move instead of copy option

📜 License

This project is for educational/demo purposes.
