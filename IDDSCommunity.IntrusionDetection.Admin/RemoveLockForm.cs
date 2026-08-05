using System;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class RemoveLockForm : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveLockForm"/> class.
    /// </summary>

    public RemoveLockForm() => InitializeComponent();

    /// <summary>
    /// Handles the load event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void RemoveLockForm_Load(object sender, EventArgs e)
    {

    }

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonOK_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void buttonCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;
}
