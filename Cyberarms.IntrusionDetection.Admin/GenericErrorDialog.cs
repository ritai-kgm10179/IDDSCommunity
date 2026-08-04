using System;
using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class GenericErrorDialog : Form
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenericErrorDialog"/> class.
    /// </summary>
    /// <param name="caption">The caption value.</param>
    /// <param name="text">The text value.</param>
    /// <param name="cancelButton">The cancel button value.</param>

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
