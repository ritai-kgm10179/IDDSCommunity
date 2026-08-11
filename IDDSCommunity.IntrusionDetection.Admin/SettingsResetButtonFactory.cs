using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 建立各設定頁共用且可透過鍵盤操作的恢復預設值按鈕。
/// </summary>
internal static class SettingsResetButtonFactory
{
    /// <summary>
    /// 建立靠右上方配置的恢復預設值按鈕。
    /// </summary>
    /// <param name="owner">擁有按鈕的設定頁。</param>
    /// <param name="click">按鈕點擊處理常式。</param>
    /// <returns>建立完成的按鈕。</returns>
    internal static Button AddTo(Control owner, System.EventHandler click)
    {
        Button button = new()
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(102, 102, 102),
            Location = new Point(System.Math.Max(280, owner.ClientSize.Width - 132), 6),
            MinimumSize = new Size(112, 26),
            Name = "buttonResetDefaults",
            TabIndex = 90,
            Text = Strings.Get("Restore defaults"),
            UseVisualStyleBackColor = false
        };
        button.AccessibleName = Strings.Get("Restore defaults");
        button.Click += click;
        owner.Controls.Add(button);
        button.BringToFront();
        return button;
    }
}
