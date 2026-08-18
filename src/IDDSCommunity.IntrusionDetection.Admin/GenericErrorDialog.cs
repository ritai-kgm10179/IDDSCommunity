using System;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供標準化全域未處理例外狀況與錯誤訊息顯示對話方塊。
/// </summary>
public partial class GenericErrorDialog : Form
{
    /// <summary>
    /// 初始化 <see cref="GenericErrorDialog"/> 類別的新執行個體。
    /// </summary>
    /// <param name="caption">caption 的值。</param>
    /// <param name="text">text 的值。</param>
    /// <param name="cancelButton">cancel button 的值。</param>
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
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonOK_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;
}
