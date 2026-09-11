namespace IDDSCommunity.IntrusionDetection.Service;

internal interface IFirewallPolicy
{
    void Block(string ipAddress);

    /// <summary>
    /// 批次將多個 IP 位址加入 Windows 防火牆阻擋規則（支援 CIDR 聚合與切片批次寫入）。
    /// </summary>
    /// <param name="ipAddresses">要批次阻擋之 IP 位址清單。</param>
    void BatchBlock(System.Collections.Generic.IReadOnlyCollection<string> ipAddresses);

    bool IsLocked(string ipAddress);

    System.Collections.Generic.IReadOnlyCollection<string> GetBlockedAddresses();

    void RemoveIpAddressFromBlockList(string ipAddress);

    /// <summary>
    /// 批次自 Windows 防火牆阻擋規則中移除多個 IP 位址，以單一 Pass 更新分片規則以杜絕 COM 昂貴耗時。
    /// </summary>
    /// <param name="ipAddresses">要批次移除之 IP 位址清單。</param>
    void BatchRemove(System.Collections.Generic.IReadOnlyCollection<string> ipAddresses);

    /// <summary>
    /// 重整並壓縮既有 Windows 防火牆分片規則，重新進行 CIDR 網段聚合並清除破碎或空洞分片。
    /// </summary>
    /// <param name="safeNetworks">選擇性的安全網路全域白名單集合。</param>
    void CompactBlockRules(System.Collections.Generic.IEnumerable<string>? safeNetworks = null);

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
