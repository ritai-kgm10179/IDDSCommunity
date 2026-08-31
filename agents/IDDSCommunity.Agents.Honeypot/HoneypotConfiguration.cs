using System;
using System.Collections.Generic;
using System.Linq;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Honeypot;

/// <summary>
/// 提供誘餌蜜罐安全性代理程式之組態設定模型。
/// </summary>
public class HoneypotConfiguration : PluginConfiguration
{
    /// <summary>
    /// 取得或設定監聽的誘餌通訊埠清單（逗號分隔字串，例如 "23, 2222, 33890"）。
    /// </summary>
    public string DecoyPortsString { get; set; } = "23, 2222, 33890";

    /// <summary>
    /// 解析並取得整數通訊埠清單。
    /// </summary>
    /// <returns>傳回有效且介於 1 到 65535 之間的通訊埠集合。</returns>
    public IReadOnlyList<int> GetDecoyPorts()
    {
        if (string.IsNullOrWhiteSpace(DecoyPortsString))
            return [23, 2222, 33890];

        List<int> ports = [];
        string[] tokens = DecoyPortsString.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (int.TryParse(token.Trim(), out int port) && port >= 1 && port <= 65535 && !ports.Contains(port))
            {
                ports.Add(port);
            }
        }

        return ports.Count > 0 ? ports : [23, 2222, 33890];
    }
}
