using System;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class GenericErrorDialog : Form
{
    public GenericErrorDialog(string caption, string text, bool cancelButton)
    {
        InitializeComponent();
        Text = caption;
        label1.Text = text;

        if (!cancelButton)
        {
            buttonCancel.Enabled = false;
        }
    }


    private void buttonOK_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

    private void buttonCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;
}
