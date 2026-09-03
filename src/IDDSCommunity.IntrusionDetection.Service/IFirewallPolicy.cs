namespace IDDSCommunity.IntrusionDetection.Service;

internal interface IFirewallPolicy
{
    void Block(string ipAddress);

    bool IsLocked(string ipAddress);

    System.Collections.Generic.IReadOnlyCollection<string> GetBlockedAddresses();

    void RemoveIpAddressFromBlockList(string ipAddress);

    /// <summary>
    /// 宣告式比對並對齊 Windows 防火牆傳入放行規則，自動新增缺漏項目並移除過期舊規則。
    /// </summary>
    /// <param name="targetRules">目標期望開放之通訊埠規則規格清單。</param>
    /// <param name="auditRecorder">選擇性的稽核日誌紀錄委派。</param>
    void ReconcileInboundAllowRules(
        System.Collections.Generic.IReadOnlyCollection<FirewallInboundRuleDefinition> targetRules,
        System.Action<string, string, string, string?>? auditRecorder = null);

    /// <summary>
    /// 於服務停止或解除安裝時，清除所有由 IDDS 社群版所建立之傳入放行規則。
    /// </summary>
    /// <param name="auditRecorder">選擇性的稽核日誌紀錄委派。</param>
    void RemoveAllInboundAllowRules(System.Action<string, string, string, string?>? auditRecorder = null);
}
