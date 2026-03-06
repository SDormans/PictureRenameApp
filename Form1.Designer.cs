namespace PictureRenameApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            mainSplitContainer = new SplitContainer();
            directoryTreeView = new TreeView();
            thumbnailImageList = new ImageList();
            rightSplitContainer = new SplitContainer();
            previewPictureBox = new PictureBox();
            metadataTextBox = new TextBox();
            topToolStrip = new ToolStrip();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripSeparator2 = new ToolStripSeparator();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).BeginInit();
            rightSplitContainer.Panel1.SuspendLayout();
            rightSplitContainer.Panel2.SuspendLayout();
            rightSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
            topToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.BorderStyle = BorderStyle.FixedSingle;
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Location = new Point(0, 25);
            mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.BackColor = Color.White;
            mainSplitContainer.Panel1.Controls.Add(directoryTreeView);
            mainSplitContainer.Panel1MinSize = 200;
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.BackColor = Color.White;
            mainSplitContainer.Panel2.Controls.Add(rightSplitContainer);
            mainSplitContainer.Panel2MinSize = 300;
            mainSplitContainer.Size = new Size(1574, 903);
            // make the left directory panel bigger
            mainSplitContainer.SplitterDistance = 800;
            mainSplitContainer.TabIndex = 0;
            // 
            // directoryTreeView
            // 
            directoryTreeView.Dock = DockStyle.Fill;
            directoryTreeView.Location = new Point(0, 0);
            directoryTreeView.Name = "directoryTreeView";
            directoryTreeView.Size = new Size(798, 901);
            directoryTreeView.TabIndex = 0;
            directoryTreeView.AfterSelect += DirectoryTreeView_AfterSelect;
            directoryTreeView.BeforeExpand += DirectoryTreeView_BeforeExpand;
            // enable drag & drop
            directoryTreeView.AllowDrop = true;
            directoryTreeView.DragEnter += DirectoryTreeView_DragEnter;
            directoryTreeView.DragDrop += DirectoryTreeView_DragDrop;
            directoryTreeView.ShowNodeToolTips = true;
            // associate image list for thumbnails
            thumbnailImageList.ColorDepth = ColorDepth.Depth32Bit;
            thumbnailImageList.ImageSize = new Size(64, 64);
            directoryTreeView.ImageList = thumbnailImageList;
            // 
            // rightSplitContainer
            // 
            rightSplitContainer.BackColor = SystemColors.Control;
            rightSplitContainer.BorderStyle = BorderStyle.FixedSingle;
            rightSplitContainer.Dock = DockStyle.Fill;
            rightSplitContainer.Location = new Point(0, 0);
            rightSplitContainer.Name = "rightSplitContainer";
            rightSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // rightSplitContainer.Panel1
            // 
            rightSplitContainer.Panel1.BackColor = Color.White;
            rightSplitContainer.Panel1.Controls.Add(previewPictureBox);
            rightSplitContainer.Panel1MinSize = 200;
            // 
            // rightSplitContainer.Panel2
            // 
            rightSplitContainer.Panel2.BackColor = Color.White;
            rightSplitContainer.Panel2.Controls.Add(metadataTextBox);
            rightSplitContainer.Panel2MinSize = 150;
            rightSplitContainer.Size = new Size(762, 903);
            rightSplitContainer.SplitterDistance = 450;
            rightSplitContainer.TabIndex = 0;
            // 
            // previewPictureBox
            // 
            previewPictureBox.BackColor = SystemColors.InactiveCaption;
            previewPictureBox.Dock = DockStyle.Fill;
            previewPictureBox.Location = new Point(0, 0);
            previewPictureBox.Name = "previewPictureBox";
            previewPictureBox.Size = new Size(760, 448);
            previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewPictureBox.TabIndex = 0;
            previewPictureBox.TabStop = false;
            // 
            // metadataTextBox
            // 
            metadataTextBox.BackColor = Color.White;
            metadataTextBox.Dock = DockStyle.Fill;
            metadataTextBox.Location = new Point(0, 0);
            metadataTextBox.Multiline = true;
            metadataTextBox.Name = "metadataTextBox";
            metadataTextBox.ReadOnly = true;
            metadataTextBox.ScrollBars = ScrollBars.Both;
            metadataTextBox.Size = new Size(760, 449);
            metadataTextBox.TabIndex = 0;
            // 
            // topToolStrip
            // 
            topToolStrip.BackColor = Color.White;
            topToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            topToolStrip.Items.AddRange(new ToolStripItem[] { toolStripSeparator1, toolStripSeparator2 });
            topToolStrip.Location = new Point(0, 0);
            topToolStrip.Name = "topToolStrip";
            topToolStrip.Size = new Size(1574, 25);
            topToolStrip.TabIndex = 0;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // Form1
            // 
            ClientSize = new Size(1574, 928);
            Controls.Add(mainSplitContainer);
            Controls.Add(topToolStrip);
            MinimumSize = new Size(1000, 600);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Picture Rename App";
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).EndInit();
            rightSplitContainer.Panel1.ResumeLayout(false);
            rightSplitContainer.Panel2.ResumeLayout(false);
            rightSplitContainer.Panel2.PerformLayout();
            rightSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
            topToolStrip.ResumeLayout(false);
            topToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }


        #endregion

        private SplitContainer mainSplitContainer;
        private SplitContainer rightSplitContainer;
        private ToolStrip topToolStrip;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private TreeView directoryTreeView;
        private PictureBox previewPictureBox;
        private TextBox metadataTextBox;
        private ImageList thumbnailImageList;
    }
}
