namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 定義 Windows 防火牆規則管理與操作之核心介面。
/// </summary>
internal interface IFirewallPolicy
{
    /// <summary>
    /// 將指定之 IP 位址加入 Windows 防火牆阻擋規則。
    /// </summary>
    /// <param name="ipAddress">要阻擋之遠端 IP 位址。</param>
    void Block(string ipAddress);

    /// <summary>
    /// 非同步將指定之 IP 位址加入 Windows 防火牆阻擋規則。
    /// </summary>
    /// <param name="ipAddress">要阻擋之遠端 IP 位址。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    System.Threading.Tasks.ValueTask BlockAsync(string ipAddress, System.Threading.CancellationToken cancellationToken = default)
    {
        Block(ipAddress);
        return System.Threading.Tasks.ValueTask.CompletedTask;
    }

    /// <summary>
    /// 批次將多個 IP 位址加入 Windows 防火牆阻擋規則（支援 CIDR 聚合與切片批次寫入）。
    /// </summary>
    /// <param name="ipAddresses">要批次阻擋之 IP 位址清單。</param>
    void BatchBlock(System.Collections.Generic.IReadOnlyCollection<string> ipAddresses);

    /// <summary>
    /// 非同步批次將多個 IP 位址加入 Windows 防火牆阻擋規則。
    /// </summary>
    /// <param name="ipAddresses">要批次阻擋之 IP 位址清單。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    System.Threading.Tasks.ValueTask BatchBlockAsync(System.Collections.Generic.IReadOnlyCollection<string> ipAddresses, System.Threading.CancellationToken cancellationToken = default)
    {
        BatchBlock(ipAddresses);
        return System.Threading.Tasks.ValueTask.CompletedTask;
    }

    /// <summary>
    /// 判斷指定之 IP 位址是否已被 Windows 防火牆規則阻擋。
    /// </summary>
    /// <param name="ipAddress">欲檢查之 IP 位址。</param>
    /// <returns>若已被阻擋則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    bool IsLocked(string ipAddress);

    /// <summary>
    /// 批次篩選給定之 IP 位址清單中，哪些位址已處於 Windows 防火牆阻擋規則中。
    /// </summary>
    /// <param name="ipAddresses">欲檢驗之 IP 位址清單。</param>
    /// <returns>已被防火牆阻擋之 IP 位址集合。</returns>
    System.Collections.Generic.HashSet<string> FilterLockedIps(System.Collections.Generic.IEnumerable<string> ipAddresses);

    /// <summary>
    /// 取得目前受 Windows 防火牆規則阻擋之所有有效 IP 位址清單。
    /// </summary>
    /// <returns>已被防火牆阻擋之 IP 位址唯讀集合。</returns>
    System.Collections.Generic.IReadOnlyCollection<string> GetBlockedAddresses();

    /// <summary>
    /// 取得目前 Windows 防火牆阻擋規則之完整狀態快照。
    /// </summary>
    /// <returns>傳回包含有效位址與各方向位址之快照物件。</returns>
    FirewallBlockState GetBlockState();

    /// <summary>
    /// 自 Windows 防火牆阻擋規則中移除指定之 IP 位址。
    /// </summary>
    /// <param name="ipAddress">要移除之 IP 位址。</param>
    void RemoveIpAddressFromBlockList(string ipAddress);

    /// <summary>
    /// 非同步自 Windows 防火牆阻擋規則中移除指定之 IP 位址。
    /// </summary>
    /// <param name="ipAddress">要移除之 IP 位址。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    System.Threading.Tasks.ValueTask RemoveIpAddressFromBlockListAsync(string ipAddress, System.Threading.CancellationToken cancellationToken = default)
    {
        RemoveIpAddressFromBlockList(ipAddress);
        return System.Threading.Tasks.ValueTask.CompletedTask;
    }

    /// <summary>
    /// 批次自 Windows 防火牆阻擋規則中移除多個 IP 位址，以單一 Pass 更新分片規則以杜絕 COM 昂貴耗時。
    /// </summary>
    /// <param name="ipAddresses">要批次移除之 IP 位址清單。</param>
    void BatchRemove(System.Collections.Generic.IReadOnlyCollection<string> ipAddresses);

    /// <summary>
    /// 非同步批次自 Windows 防火牆阻擋規則中移除多個 IP 位址。
    /// </summary>
    /// <param name="ipAddresses">要批次移除之 IP 位址清單。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    System.Threading.Tasks.ValueTask BatchRemoveAsync(System.Collections.Generic.IReadOnlyCollection<string> ipAddresses, System.Threading.CancellationToken cancellationToken = default)
    {
        BatchRemove(ipAddresses);
        return System.Threading.Tasks.ValueTask.CompletedTask;
    }

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

    /// <summary>
    /// 強制將調步佇列中所有待處理的防火牆規則立即排空並寫入底層 Windows 防火牆 COM 介面。
    /// </summary>
    void Flush() { }

    /// <summary>
    /// 非同步強制將調步佇列中所有待處理的防火牆規則立即排空並寫入底層 Windows 防火牆 COM 介面。
    /// </summary>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步排空作業的任務。</returns>
    System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        Flush();
        return System.Threading.Tasks.Task.CompletedTask;
    }
}

internal sealed record FirewallBlockState(
    System.Collections.Generic.IReadOnlyCollection<string> EffectiveAddresses,
    System.Collections.Generic.IReadOnlyCollection<string> AnyDirectionAddresses);

internal sealed record FirewallManagedRuleSnapshot(
    string Name,
    int Direction,
    int Action,
    int Protocol,
    bool Enabled,
    string RemoteAddresses);

internal sealed record FirewallBlockRuleTarget(
    string Name,
    int Direction,
    int Action,
    int Protocol,
    bool Enabled,
    string RemoteAddresses);

internal sealed record FirewallBlockReconciliationPlan(
    System.Collections.Generic.IReadOnlyList<FirewallBlockRuleTarget> RulesToCreate,
    System.Collections.Generic.IReadOnlyList<FirewallBlockRuleTarget> RulesToPatch,
    System.Collections.Generic.IReadOnlyList<string> RulesToDelete,
    System.Collections.Generic.IReadOnlyList<FirewallShardAssignment> Assignments);

internal sealed record FirewallShardAssignment(string Address, string ShardId);
