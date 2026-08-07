using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class SmartLabelTextbox : UserControl
{

    public event KeyPressEventHandler? TextBoxKeyPress;

    /// <summary>
    /// 初始化 <see cref="SmartLabelTextbox"/> 類別的新執行個體。
    /// </summary>

    public SmartLabelTextbox()
    {
        InitializeComponent();
        textBox1.KeyPress += new KeyPressEventHandler(textBox1_KeyPress);
    }

    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

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
