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
            components = new System.ComponentModel.Container();
            mainSplitContainer = new SplitContainer();
            thumbnailPanel = new FlowLayoutPanel();
            placeholderLabel = new Label();
            rightSplitContainer = new SplitContainer();
            previewPictureBox = new PictureBox();
            metadataTextBox = new TextBox();
            thumbnailImageList = new ImageList(components);
            topToolStrip = new ToolStrip();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripSeparator2 = new ToolStripSeparator();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            thumbnailPanel.SuspendLayout();
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
            mainSplitContainer.Panel1.Controls.Add(thumbnailPanel);
            mainSplitContainer.Panel1MinSize = 200;
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.BackColor = Color.White;
            mainSplitContainer.Panel2.Controls.Add(rightSplitContainer);
            mainSplitContainer.Panel2MinSize = 300;
            mainSplitContainer.Size = new Size(1574, 903);
            mainSplitContainer.SplitterDistance = 940;
            mainSplitContainer.TabIndex = 0;
            // 
            // thumbnailPanel
            // 
            thumbnailPanel.AllowDrop = true;
            thumbnailPanel.AutoScroll = true;
            thumbnailPanel.BackColor = SystemColors.InactiveCaption;
            thumbnailPanel.Controls.Add(placeholderLabel);
            thumbnailPanel.Dock = DockStyle.Fill;
            thumbnailPanel.Location = new Point(0, 0);
            thumbnailPanel.Name = "thumbnailPanel";
            thumbnailPanel.Padding = new Padding(10);
            thumbnailPanel.Size = new Size(938, 901);
            thumbnailPanel.TabIndex = 0;
            thumbnailPanel.DragDrop += ThumbnailPanel_DragDrop;
            thumbnailPanel.DragEnter += ThumbnailPanel_DragEnter;
            // 
            // placeholderLabel
            // 
            placeholderLabel.BackColor = Color.Transparent;
            placeholderLabel.Dock = DockStyle.Fill;
            placeholderLabel.Font = new Font("Segoe UI", 14F);
            placeholderLabel.ForeColor = Color.Black;
            placeholderLabel.Location = new Point(10, 10);
            placeholderLabel.Margin = new Padding(0);
            placeholderLabel.Name = "placeholderLabel";
            placeholderLabel.Size = new Size(100, 0);
            placeholderLabel.TabIndex = 0;
            placeholderLabel.Text = "Drop your files here";
            placeholderLabel.TextAlign = ContentAlignment.MiddleCenter;
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
            rightSplitContainer.Size = new Size(630, 903);
            rightSplitContainer.SplitterDistance = 449;
            rightSplitContainer.TabIndex = 0;
            // 
            // previewPictureBox
            // 
            previewPictureBox.BackColor = SystemColors.InactiveCaption;
            previewPictureBox.Dock = DockStyle.Fill;
            previewPictureBox.Location = new Point(0, 0);
            previewPictureBox.Name = "previewPictureBox";
            previewPictureBox.Size = new Size(628, 447);
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
            metadataTextBox.Size = new Size(628, 448);
            metadataTextBox.TabIndex = 0;
            metadataTextBox.Text = "Metadata";
            // 
            // thumbnailImageList
            // 
            thumbnailImageList.ColorDepth = ColorDepth.Depth32Bit;
            thumbnailImageList.ImageSize = new Size(128, 128);
            thumbnailImageList.TransparentColor = Color.Transparent;
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
            thumbnailPanel.ResumeLayout(false);
            rightSplitContainer.Panel1.ResumeLayout(false);
            rightSplitContainer.Panel2.ResumeLayout(false);
            rightSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).EndInit();
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
        private FlowLayoutPanel thumbnailPanel;
        private PictureBox previewPictureBox;
        private TextBox metadataTextBox;
        private ImageList thumbnailImageList;
        private Label placeholderLabel;
    }
}
