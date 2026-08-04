using System;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin
{
    public partial class GenericErrorDialog : Form
    {
        public GenericErrorDialog(string caption, string text, bool cancelButton)
        {
            InitializeComponent();
            this.Text = caption;
            label1.Text = text;

            if (!cancelButton)
            {
                buttonCancel.Enabled = false;
            }
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
    }
}
