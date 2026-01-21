using System.Windows.Forms;

namespace PictureRenameApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private List<string> droppedFiles = new();

        private void button1_Click(object sender, EventArgs e)
        {
            if (!droppedFiles.Any())
            {
                MessageBox.Show("Geen bestanden gedropt");
                return;
            }

            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Geef een naam op");
                return;
            }

            string baseName = textBox1.Text;

            // doelmap (Documents)
            string targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                baseName
            );

            Directory.CreateDirectory(targetFolder);

            for (int i = 0; i < droppedFiles.Count; i++)
            {
                string sourceFile = droppedFiles[i];
                string extension = Path.GetExtension(sourceFile);

                string newFileName = droppedFiles.Count == 1
                    ? $"{baseName}{extension}"
                    : $"{baseName}_{(i + 1):000}{extension}";

                string targetPath = Path.Combine(targetFolder, newFileName);

                File.Copy(sourceFile, targetPath, overwrite: true);
            }

            textBox1.Text = "Bestanden succesvol verwerkt!";
        }

        private void LaunchFile(string filename)
        {
            System.Diagnostics.Process process;

            filename = textBox1.Text;
            if (System.IO.File.Exists(filename))
            {
                process = new System.Diagnostics.Process();
                process.StartInfo.FileName = filename;
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            else
            {
                MessageBox.Show(filename + "Bestaat niet!");
            }
        }
        private void flowLayoutPanel1_DoubleClick(object sender, EventArgs e)
        {
            string filename;

            filename = flowLayoutPanel1.Text;
            LaunchFile(filename);
        }

        private void AddButton(string path)
        {
            Button button;
            Icon iconForFile;

            button = new System.Windows.Forms.Button();
            button.Size = new System.Drawing.Size(80, 80);
            button.UseVisualStyleBackColor = true;
            button.Text = Path.GetFileName(path);
            button.Tag = path;
            button.TextAlign = ContentAlignment.BottomCenter;
            button.ImageAlign = ContentAlignment.TopCenter;

            iconForFile = System.Drawing.Icon.ExtractAssociatedIcon(path);
            button.Image = iconForFile.ToBitmap();
            //button.Click;
            flowLayoutPanel1.Controls.Add(button);
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();
        }

        private void deleteAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //flowLayoutPanel1.Controls.Remove();
            //verwijder de fotos
        }

        private void flowLayoutPanel1_DragDrop_1(object sender, DragEventArgs e)
        {

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            droppedFiles.AddRange(files);
            foreach (string filename in droppedFiles)
            {
                AddButton(filename);
            }
        }

        private void flowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

    }
}

