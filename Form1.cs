using System.Windows.Forms;

namespace PictureRenameApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // List to store the full paths of all files dropped by the user
        private List<string> droppedFiles = new();

        /// <summary>
        /// Event handler for the "Process" button click.
        /// Renames and copies the dropped files to a new folder in Documents.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            // Check if any files have been dropped
            if (!droppedFiles.Any())
            {
                MessageBox.Show("No files dropped");
                return;
            }

            // Check if a base name has been entered
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Please enter a base name");
                return;
            }

            string baseName = textBox1.Text;

            // Construct the target folder path inside the user's Documents folder
            string targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                baseName
            );

            // Create the target directory (does nothing if it already exists)
            Directory.CreateDirectory(targetFolder);

            // Loop through all dropped files to copy and rename them
            for (int i = 0; i < droppedFiles.Count; i++)
            {
                string sourceFile = droppedFiles[i];
                string extension = Path.GetExtension(sourceFile);

                // Determine the new file name:
                // - Single file: just the base name
                // - Multiple files: base name with _001, _002, etc.
                string newFileName = droppedFiles.Count == 1
                    ? $"{baseName}{extension}"
                    : $"{baseName}_{(i + 1):000}{extension}";

                string targetPath = Path.Combine(targetFolder, newFileName);

                // Copy the file to the new location (overwrite if it already exists)
                File.Copy(sourceFile, targetPath, overwrite: true);
            }

            // Provide feedback to the user
            textBox1.Text = "Files processed successfully!";
        }

        /// <summary>
        /// Attempts to open a file using the default associated application.
        /// </summary>
        /// <param name="filename">The full path to the file to launch.</param>
        private void LaunchFile(string filename)
        {
            System.Diagnostics.Process process;

            // Check if the file actually exists
            filename = textBox1.Text; // NOTE: This line seems incorrect (overwrites filename). Should probably use a parameter or button tag.
            if (System.IO.File.Exists(filename))
            {
                process = new System.Diagnostics.Process();
                process.StartInfo.FileName = filename;
                process.StartInfo.UseShellExecute = true; // Use shell to open with default program
                process.Start();
            }
            else
            {
                MessageBox.Show(filename + " does not exist!");
            }
        }

        /// <summary>
        /// Event handler for double-clicking the FlowLayoutPanel.
        /// Currently seems to attempt launching the panel's Text property as a file path, which is likely not intended.
        /// </summary>
        private void flowLayoutPanel1_DoubleClick(object sender, EventArgs e)
        {
            string filename;

            filename = flowLayoutPanel1.Text; // This likely holds the base name, not a file path
            LaunchFile(filename);
        }

        /// <summary>
        /// Creates a new button representing a dropped file and adds it to the FlowLayoutPanel.
        /// The button shows the file name and its associated system icon.
        /// </summary>
        /// <param name="path">Full path to the file.</param>
        private void AddButton(string path)
        {
            Button button;
            Icon iconForFile;

            button = new System.Windows.Forms.Button();
            button.Size = new System.Drawing.Size(80, 80);
            button.UseVisualStyleBackColor = true;
            button.Text = Path.GetFileName(path);      // Display only the file name, not the full path
            button.Tag = path;                          // Store the full path in the Tag property for later use
            button.TextAlign = ContentAlignment.BottomCenter;
            button.ImageAlign = ContentAlignment.TopCenter;

            // Extract and set the file's associated icon
            iconForFile = System.Drawing.Icon.ExtractAssociatedIcon(path);
            button.Image = iconForFile.ToBitmap();

            // Note: button.Click event is not wired up here. Could be used to select or launch the file.

            // Add the button to the panel
            flowLayoutPanel1.Controls.Add(button);
        }

        /// <summary>
        /// Event handler for the "Clear All" menu item.
        /// Removes all file buttons from the FlowLayoutPanel.
        /// </summary>
        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();
        }

        /// <summary>
        /// Event handler for the "Delete All" menu item.
        /// Currently commented out. Intended to delete the actual files from disk.
        /// </summary>
        private void deleteAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Implementation needed:
            // Loop through controls, get file path from Tag, delete file.
            // Then clear the panel.
            // Be careful: This would delete the original files, not the copies!
        }

        /// <summary>
        /// Event handler for when items are dropped onto the FlowLayoutPanel.
        /// Processes the dropped files, stores their paths, and creates buttons for them.
        /// </summary>
        private void flowLayoutPanel1_DragDrop_1(object sender, DragEventArgs e)
        {
            // Retrieve the array of file paths from the drag-and-drop data
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Add all new files to the master list
            droppedFiles.AddRange(files);

            // Create a button for each file
            foreach (string filename in droppedFiles)
            {
                AddButton(filename);
            }
        }

        /// <summary>
        /// Event handler for when an object is dragged into the FlowLayoutPanel area.
        /// Checks if the dragged data contains files and sets the drag effect accordingly.
        /// </summary>
        private void flowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;   // Show the "copy" cursor
            else
                e.Effect = DragDropEffects.None;    // Reject the drag
        }
    }
}