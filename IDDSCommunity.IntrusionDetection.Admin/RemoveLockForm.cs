using System;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class RemoveLockForm : Form
{
    /// <summary>
    /// 初始化 <see cref="RemoveLockForm"/> 類別的新執行個體。
    /// </summary>

    public RemoveLockForm() => InitializeComponent();
    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    private void RemoveLockForm_Load(object sender, EventArgs e)
    {

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
