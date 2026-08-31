using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared.Compliance;

/// <summary>
/// 表示單項 CIS Windows Server 安全基準合規評估檢查結果。
/// </summary>
public sealed class CisCheckItem
{
    /// <summary>
    /// 取得或設定 基準項目識別碼（例如 "CIS-1.1", "CIS-2.3"）。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 項目標題。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 項目類別（例如 "Audit Policy", "Firewall", "Account Policy"）。
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 是否符合安全基準要求。
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// 取得或設定 當前系統狀態描述。
    /// </summary>
    public string CurrentValue { get; set; } = string.Empty;

    /// <summary>
    /// 取得或設定 建議改善作法與基準標準值。
    /// </summary>
    public string RemediationAdvice { get; set; } = string.Empty;
}

/// <summary>
/// 表示整份 CIS Windows Server 安全基準合規掃描評估報告。
/// </summary>
public sealed class CisBenchmarkResult
{
    /// <summary>
    /// 取得或設定 掃描執行時間（UTC）。
    /// </summary>
    public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 取得或設定 主機名稱。
    /// </summary>
    public string HostName { get; set; } = Environment.MachineName;

    /// <summary>
    /// 取得或設定 評估檢查項目清單。
    /// </summary>
    public List<CisCheckItem> Items { get; set; } = [];

    /// <summary>
    /// 取得 總檢查項目數。
    /// </summary>
    public int TotalChecks => Items.Count;

    /// <summary>
    /// 取得 通過合規標準之項目數。
    /// </summary>
    public int PassedChecks => Items.FindAll(i => i.IsCompliant).Count;

    /// <summary>
    /// 取得 合規百分比分數 (0.0 至 100.0)。
    /// </summary>
    public double ComplianceScore => TotalChecks == 0 ? 100.0 : Math.Round((double)PassedChecks / TotalChecks * 100.0, 1);
}
