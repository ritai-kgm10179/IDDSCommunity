using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class SmartLabelTextbox : UserControl
{

    public event KeyPressEventHandler? TextBoxKeyPress;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartLabelTextbox"/> class.
    /// </summary>

    public SmartLabelTextbox()
    {
        InitializeComponent();
        textBox1.KeyPress += new KeyPressEventHandler(textBox1_KeyPress);
    }

    /// <summary>
    /// Handles the key press event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void textBox1_KeyPress(object? sender, KeyPressEventArgs e) => TextBoxKeyPress?.Invoke(sender, e);

    public string LabelText
    {
        get => smartLabel1.Text; set => smartLabel1.Text = value;
    }

    public string TextBoxText
    {
        get => textBox1.Text; set => textBox1.Text = value;
    }


}
