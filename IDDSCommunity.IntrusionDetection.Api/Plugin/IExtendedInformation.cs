using System;
using System.Drawing;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// 提供入侵偵測 Agent 擴充資訊（包含顯示名稱、主題圖示與唯一識別碼）之介面。
/// </summary>
public interface IExtendedInformation
{
    /// <summary>
    /// 取得或設定 Agent 於管理介面中顯示的區段名稱。
    /// </summary>
    string DisplayName { get; set; }

    /// <summary>
    /// 取得或設定 Agent 的預設圖示。
    /// </summary>
    Image? Icon { get; set; }

    /// <summary>
    /// 取得或設定 Agent 於選取狀態下顯示的主題圖示。
    /// </summary>
    Image? SelectedIcon { get; set; }

    /// <summary>
    /// 取得或設定 Agent 於非選取狀態下顯示的主題圖示。
    /// </summary>
    Image? UnselectedIcon { get; set; }

    /// <summary>
    /// 取得 Agent 的全域唯一識別碼 (GUID)。
    /// </summary>
    Guid Id { get; }
}
