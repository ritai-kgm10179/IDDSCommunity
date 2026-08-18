using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 建立各設定頁共用且可透過鍵盤操作的恢復預設值按鈕。
/// </summary>
internal static class SettingsResetButtonFactory
{
    private const int RightMargin = 20;

    /// <summary>
    /// 建立靠右上方配置的恢復預設值按鈕。
    /// </summary>
    /// <param name="owner">擁有按鈕的設定頁。</param>
    /// <param name="click">按鈕點擊處理常式。</param>
    /// <param name="fixedLocation">固定按鈕位置座標；若為 null 則自動靠右上排列。</param>
    /// <param name="confirmationPrompt">自訂確認提示回呼函式。</param>
    /// <returns>建立完成的按鈕。</returns>
    internal static Button AddTo(
        Control owner,
        System.EventHandler click,
        Point? fixedLocation = null,
        System.Func<IWin32Window, DialogResult>? confirmationPrompt = null)
    {
        Button button = new()
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            AutoSize = true,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(102, 102, 102),
            Location = fixedLocation ?? new Point(12, 6),
            MinimumSize = new Size(112, 26),
            Name = "buttonResetDefaults",
            TabIndex = 90,
            Text = Strings.Get("Restore defaults"),
            UseVisualStyleBackColor = false
        };
        button.AccessibleName = Strings.Get("Restore defaults");
        button.Click += (sender, eventArgs) =>
        {
            if (ConfirmRestoreDefaults(owner, confirmationPrompt))
                click(sender, eventArgs);
        };
        owner.Controls.Add(button);
        button.BringToFront();

        if (fixedLocation is null)
        {
            Control? observedParent = null;
            void PositionButton()
            {
                int visibleWidth = owner.ClientSize.Width;
                if (owner.Parent is Control parent)
                    visibleWidth = System.Math.Min(visibleWidth, System.Math.Max(0, parent.ClientSize.Width - owner.Left));
                button.Left = System.Math.Max(12, visibleWidth - button.Width - RightMargin);
                button.Top = 6;
            }

            void ParentSizeChanged(object? sender, System.EventArgs eventArgs) => PositionButton();
            void ParentChanged(object? sender, System.EventArgs eventArgs)
            {
                if (observedParent is not null)
                    observedParent.SizeChanged -= ParentSizeChanged;
                observedParent = owner.Parent;
                if (observedParent is not null)
                    observedParent.SizeChanged += ParentSizeChanged;
                PositionButton();
            }

            owner.Layout += (_, _) => PositionButton();
            owner.ParentChanged += ParentChanged;
            owner.Disposed += (_, _) =>
            {
                if (observedParent is not null)
                    observedParent.SizeChanged -= ParentSizeChanged;
            };
            ParentChanged(owner, System.EventArgs.Empty);
            PositionButton();
        }
        return button;
    }

    /// <summary>
    /// 顯示恢復預設值確認提示，避免使用者意外覆蓋尚未儲存的設定。
    /// </summary>
    /// <param name="owner">確認提示的擁有者視窗。</param>
    /// <param name="confirmationPrompt">測試或自訂確認提示；未指定時使用標準訊息方塊。</param>
    /// <returns>使用者確認操作時傳回 <see langword="true"/>。</returns>
    internal static bool ConfirmRestoreDefaults(
        IWin32Window owner,
        System.Func<IWin32Window, DialogResult>? confirmationPrompt = null)
    {
        DialogResult result = confirmationPrompt?.Invoke(owner) ?? MessageBox.Show(
            owner,
            Strings.Get("Restore the settings on this page to their defaults? Unsaved changes will be replaced."),
            Strings.Get("Confirm restore defaults"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        return result == DialogResult.Yes;
    }
}
