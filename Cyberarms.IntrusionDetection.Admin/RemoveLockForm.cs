using System;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class RemoveLockForm : Form
{
    public RemoveLockForm() => InitializeComponent();

    private void RemoveLockForm_Load(object sender, EventArgs e)
    {

    }

    private void buttonOK_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

    private void buttonCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;
}
