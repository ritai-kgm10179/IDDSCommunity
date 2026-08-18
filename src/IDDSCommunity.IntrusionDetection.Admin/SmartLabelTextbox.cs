using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 組合標籤與文字方塊之整合式輸入控制項。
/// </summary>
public partial class SmartLabelTextbox : UserControl
{

        /// <summary>
    /// 當 文字輸入方塊按鍵按下時之事件 時引發之事件。
    /// </summary>
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

        /// <summary>
    /// 取得或設定 標籤顯示文字。
    /// </summary>
public string LabelText
    {
        get => smartLabel1.Text; set => smartLabel1.Text = value;
    }

        /// <summary>
    /// 取得或設定 文字方塊內容文字。
    /// </summary>
public string TextBoxText
    {
        get => textBox1.Text; set => textBox1.Text = value;
    }


}
