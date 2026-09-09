using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供管理主控台清單一致的表格外觀與互動設定。
/// </summary>
internal static class AdminGridChrome
{
    /// <summary>
    /// 將管理主控台的共用視覺樣式套用至指定表格。
    /// </summary>
    /// <param name="grid">要設定的表格控制項。</param>
    public static void Apply(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 32;
        grid.ColumnHeadersVisible = true;
        grid.GridColor = Color.FromArgb(218, 224, 228);
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 28;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AllowUserToResizeColumns = true;
        grid.AllowUserToResizeRows = false;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(243, 246, 248),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(19, 184, 166),
            SelectionBackColor = Color.FromArgb(243, 246, 248),
            SelectionForeColor = Color.FromArgb(19, 184, 166),
            WrapMode = DataGridViewTriState.False
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            BackColor = SystemColors.Window,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(102, 102, 102),
            SelectionBackColor = Color.FromArgb(224, 244, 241),
            SelectionForeColor = Color.FromArgb(62, 62, 62),
            WrapMode = DataGridViewTriState.False
        };

        typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(grid, true);
    }
}
