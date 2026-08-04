using System.Windows.Forms;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class SmartLabelTextbox : UserControl
{

    public event KeyPressEventHandler? TextBoxKeyPress;

    public SmartLabelTextbox()
    {
        InitializeComponent();
        textBox1.KeyPress += new KeyPressEventHandler(textBox1_KeyPress);
    }

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
