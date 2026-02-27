using System.Windows.Forms;

namespace PictureRenameApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Text = "Picture Rename App";
            this.Size = new Size(1400, 800);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeCustomControls();
          
    }

        private void InitializeCustomControls()
        {

        }
    }
}