namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

/// <summary>
/// 代表跨 Agent 關聯與密碼噴灑偵測動作之列舉型別。
/// </summary>
public enum CorrelationAction
{
    /// <summary>
    /// 無需採取動作或功能已停用。
    /// </summary>
    None = 0,

    /// <summary>
    /// 僅記錄警示日誌與加成安全評分，嚴禁執行防火牆封鎖（Phase 0 規範）。
    /// </summary>
    AlertAndScoreOnly = 1
}

/// <summary>
/// 代表密碼噴灑攻擊型態之列舉型別。
/// </summary>
public enum SprayAttackType
{
    /// <summary>
    /// 未偵測到噴灑型態。
    /// </summary>
    None = 0,

    /// <summary>
    /// 單一來源 IP 對多個帳號之傳統密碼噴灑 (1-to-N)。
    /// </summary>
    OneIpToMultipleAccounts = 1,

    /// <summary>
    /// 多個分散來源 IP 針對單一帳號之分散式密碼噴灑 (N-to-1)。
    /// </summary>
    MultipleIpsToOneAccount = 2
}

/// <summary>
/// 代表跨 Agent 關聯與分析評估結果之封裝類別。
/// </summary>
public sealed class CorrelationEvaluationResult
{
    /// <summary>
    /// 初始化 <see cref="CorrelationEvaluationResult"/> 類別的新執行個體。
    /// </summary>
    public CorrelationEvaluationResult()
    {
        ObservationId = Guid.Empty;
        Action = CorrelationAction.None;
        SprayType = SprayAttackType.None;
        AssociatedAccounts = Array.Empty<string>();
        AssociatedIps = Array.Empty<string>();
    }

    /// <summary>
    /// 取得或設定評估之觀察事件識別碼。
    /// </summary>
    public Guid ObservationId { get; set; }

    /// <summary>
    /// 取得或設定指派之關聯群組識別碼。
    /// </summary>
    public Guid? CorrelationGroupId { get; set; }

    /// <summary>
    /// 取得或設定建議採取之防護動作（Phase 0 僅限 <see cref="CorrelationAction.AlertAndScoreOnly"/> 或 <see cref="CorrelationAction.None"/>）。
    /// </summary>
    public CorrelationAction Action { get; set; }

    /// <summary>
    /// 取得或設定偵測到的密碼噴灑攻擊型態。
    /// </summary>
    public SprayAttackType SprayType { get; set; }

    /// <summary>
    /// 取得或設定觀察事件是否因重複重播而遭冪等篩選器剔除。
    /// </summary>
    public bool IsDuplicateReplay { get; set; }

    /// <summary>
    /// 取得或設定來源 IP 是否位於安全網路全域允許清單中。
    /// </summary>
    public bool IsSafeNetworkExempted { get; set; }

    /// <summary>
    /// 取得或設定關聯之相異帳號清單。
    /// </summary>
    public IReadOnlyList<string> AssociatedAccounts { get; set; }

    /// <summary>
    /// 取得或設定關聯之相異來源 IP 清單。
    /// </summary>
    public IReadOnlyList<string> AssociatedIps { get; set; }

    /// <summary>
    /// 取得或設定貢獻達成噴灑門檻之觀察事件確定性識別清單。
    /// </summary>
    public IReadOnlyList<string> ContributingIdempotencyKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 取得或設定評估產生的診斷或警示訊息文字。
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 提供跨 Agent 安全性觀察事件正規化、冪等去重、多來源關聯群組與密碼噴灑偵測之核心引擎。
/// </summary>
public sealed class CrossAgentCorrelationEngine
{
    private readonly ObservationIdempotencyFilter idempotencyFilter;
    private readonly ConcurrentSlidingBucket<string, string> ipToAccountsBucket;
    private readonly ConcurrentSlidingBucket<string, string> accountToIpsBucket;
    private readonly ConcurrentDictionary<string, ObservationWatermark> watermarks;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// 初始化 <see cref="CrossAgentCorrelationEngine"/> 類別的新執行個體。
    /// </summary>
    /// <param name="slidingWindowDuration">滑動時間窗持續時間，預設為 10 分鐘。</param>
    /// <param name="timeProvider">時間提供者執行個體，預設為系統時間。</param>
    public CrossAgentCorrelationEngine(
        TimeSpan? slidingWindowDuration = null,
        TimeProvider? timeProvider = null)
    {
        TimeSpan window = slidingWindowDuration ?? TimeSpan.FromMinutes(10);
        this.timeProvider = timeProvider ?? TimeProvider.System;

        idempotencyFilter = new ObservationIdempotencyFilter(retentionPeriod: window * 2, timeProvider: this.timeProvider);
        ipToAccountsBucket = new ConcurrentSlidingBucket<string, string>(windowDuration: window, timeProvider: this.timeProvider);
        accountToIpsBucket = new ConcurrentSlidingBucket<string, string>(windowDuration: window, timeProvider: this.timeProvider);
        watermarks = new ConcurrentDictionary<string, ObservationWatermark>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 取得目前持有的來源水位點字典快照。
    /// </summary>
    public IReadOnlyDictionary<string, ObservationWatermark> Watermarks => watermarks;

    /// <summary>
    /// 評估傳入之安全性觀察事件，執行冪等性驗證、安全網路判定、跨來源關聯與噴灑偵測。
    /// </summary>
    /// <param name="observation">欲評估之標準化安全性觀察事件。</param>
    /// <param name="config">目前系統全域設定執行個體。</param>
    /// <returns>傳回包含關聯群組與偵測動作之評估結果。</returns>
    public CorrelationEvaluationResult Evaluate(SecurityObservationEvent observation, IddsConfig? config = null) => Ingest(observation, config);

    /// <summary>
    /// 評估傳入之安全性觀察事件，執行冪等性驗證、安全網路判定、跨來源關聯與噴灑偵測。
    /// </summary>
    /// <param name="observation">欲評估之標準化安全性觀察事件。</param>
    /// <param name="config">目前系統全域設定執行個體。</param>
    /// <returns>傳回包含關聯群組與偵測動作之評估結果。</returns>
    public CorrelationEvaluationResult Ingest(SecurityObservationEvent observation, IddsConfig? config)
    {
        ArgumentNullException.ThrowIfNull(observation);

        CorrelationEvaluationResult result = new()
        {
            ObservationId = observation.Id
        };

        // 1. 若功能開關關閉，直接忽略並傳回無動作（Phase 0 預設關閉以保證既有行為零差異）
        if (config != null && !config.EnableCrossAgentCorrelation)
        {
            result.Action = CorrelationAction.None;
            result.Message = "Cross-agent correlation is disabled by feature flag.";
            return result;
        }

        // 2. 第一層語意：確定性冪等去重（僅消除完全相同來源事件的重播，不改變不同事件）
        if (!idempotencyFilter.TryAccept(observation, out string idempotencyKey))
        {
            result.IsDuplicateReplay = true;
            result.Action = CorrelationAction.None;
            result.Message = $"Duplicate event replay rejected by idempotency filter. Key={idempotencyKey}";
            return result;
        }

        // 3. 更新來源處理水位點
        UpdateWatermark(observation);

        // 4. 安全網路檢查（使用全域允許清單，支援 IPv4、IPv6 與 CIDR）
        if (config != null && config.UseSafeNetworkList && !string.IsNullOrWhiteSpace(observation.NormalizedIpAddress))
        {
            if (config.IsInSafeNetwork(observation.NormalizedIpAddress))
            {
                result.IsSafeNetworkExempted = true;
                result.Action = CorrelationAction.None;
                result.Message = $"Source IP {observation.NormalizedIpAddress} is within the safe network whitelist.";
                return result;
            }
        }

        // 5. 第二層語意：跨來源關聯群組指派（不刪除或合併原始事件）
        if (observation.CorrelationGroupId == null)
        {
            observation.CorrelationGroupId = Guid.NewGuid();
        }
        result.CorrelationGroupId = observation.CorrelationGroupId;

        // 6. 滑動時間窗噴灑偵測（僅針對明確之認證憑證失敗事件，授權拒絕與原則錯誤不計入）
        if (!observation.IsCredentialFailure)
        {
            result.Action = CorrelationAction.None;
            result.Message = "Observation is authorization, policy, or telemetry event; excluded from credential spray detection.";
            return result;
        }

        string ip = observation.NormalizedIpAddress.Trim();
        string account = observation.NormalizedAccount.Trim();

        int sprayAccountThreshold = config?.CrossAgentSprayAccountThreshold ?? 5;
        int sprayIpThreshold = config?.CrossAgentSprayIpThreshold ?? 5;

        // 記錄 1-to-N: IP -> Accounts
        if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(account))
        {
            ipToAccountsBucket.Add(ip, account, observation.EventTimeUtc);
            accountToIpsBucket.Add(account, ip, observation.EventTimeUtc);

            IReadOnlyList<string> activeAccountsForIp = ipToAccountsBucket.GetActiveItems(ip).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            IReadOnlyList<string> activeIpsForAccount = accountToIpsBucket.GetActiveItems(account).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            result.AssociatedAccounts = activeAccountsForIp;
            result.AssociatedIps = activeIpsForAccount;

            // 檢查 1-to-N (單一 IP 嘗試多個不同帳號)
            if (activeAccountsForIp.Count >= sprayAccountThreshold)
            {
                result.SprayType = SprayAttackType.OneIpToMultipleAccounts;
                result.ContributingIdempotencyKeys = activeAccountsForIp.Select(acc => $"{ip}:{acc}").ToList();
                // Phase 0 規範：一律 alert/score only，禁止新偵測直接封鎖
                result.Action = CorrelationAction.AlertAndScoreOnly;
                result.Message = $"1-to-N password spray detected: IP {ip} attempted {activeAccountsForIp.Count} distinct accounts.";
                return result;
            }

            // 檢查 N-to-1 (多個分散 IP 嘗試同一帳號)
            if (activeIpsForAccount.Count >= sprayIpThreshold)
            {
                result.SprayType = SprayAttackType.MultipleIpsToOneAccount;
                result.ContributingIdempotencyKeys = activeIpsForAccount.Select(srcIp => $"{srcIp}:{account}").ToList();
                // Phase 0 規範：N-to-1 分散式攻擊嚴禁自動封鎖所有涉及的 IP，僅記錄警示
                result.Action = CorrelationAction.AlertAndScoreOnly;
                result.Message = $"N-to-1 distributed spray detected: Account {account} targeted by {activeIpsForAccount.Count} distinct IPs.";
                return result;
            }
        }

        result.Action = CorrelationAction.None;
        return result;
    }

    /// <summary>
    /// 重設水位點至特定記錄狀態（用於服務崩潰或重啟後的歷史狀態復原）。
    /// </summary>
    /// <param name="watermark">欲復原之水位點資訊。</param>
    public void RestoreWatermark(ObservationWatermark watermark)
    {
        ArgumentNullException.ThrowIfNull(watermark);
        string key = $"{watermark.SourceAgentName}|{watermark.ProviderOrChannel}";
        watermarks[key] = watermark;
    }

    /// <summary>
    /// 重設所有內部快取與聚合貯列。
    /// </summary>
    public void Reset()
    {
        idempotencyFilter.Reset();
        ipToAccountsBucket.Reset();
        accountToIpsBucket.Reset();
        watermarks.Clear();
    }

    private void UpdateWatermark(SecurityObservationEvent observation)
    {
        if (string.IsNullOrWhiteSpace(observation.SourceAgentName))
            return;

        string key = $"{observation.SourceAgentName}|{observation.ProviderOrChannel}";
        watermarks.AddOrUpdate(
            key,
            _ => new ObservationWatermark(observation.SourceAgentName, observation.ProviderOrChannel, observation.SourceEventRecordId, observation.EventTimeUtc),
            (_, existing) =>
            {
                if (observation.SourceEventRecordId.HasValue && existing.LastEventRecordId.HasValue)
                {
                    if (observation.SourceEventRecordId.Value > existing.LastEventRecordId.Value)
                    {
                        existing.LastEventRecordId = observation.SourceEventRecordId.Value;
                    }
                }
                else if (observation.SourceEventRecordId.HasValue)
                {
                    existing.LastEventRecordId = observation.SourceEventRecordId.Value;
                }

                if (observation.EventTimeUtc > existing.LastTimestampUtc)
                {
                    existing.LastTimestampUtc = observation.EventTimeUtc;
                }
                existing.UpdatedUtc = timeProvider.GetUtcNow();
                return existing;
            });
    }

    /// <summary>
    /// 從持久化資料庫載入時間窗內的觀察事件與來源水位點，安全重建記憶體滑動窗與冪等去重狀態。
    /// </summary>
    /// <param name="database">應用程式資料庫執行個體。</param>
    /// <param name="windowDuration">欲載入之滑動時間窗範圍。</param>
    /// <param name="nowUtc">目前 UTC 參考基準時間。</param>
    public void RebuildFromDatabase(Database database, TimeSpan? windowDuration = null, DateTimeOffset? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        DateTimeOffset now = nowUtc ?? timeProvider.GetUtcNow();
        TimeSpan window = windowDuration ?? TimeSpan.FromMinutes(10);
        string windowStart = now.Subtract(window).ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        // 1. 重建來源水位點
        Dictionary<string, ObservationWatermark> loadedWatermarks = SecurityObservationStore.LoadWatermarks(database);
        foreach (KeyValuePair<string, ObservationWatermark> pair in loadedWatermarks)
        {
            watermarks[pair.Key] = pair.Value;
        }

        // 2. 重建時間窗內之觀察事件
        IEnumerable<SecurityObservationEventRebuildRow> rows = database.Query<SecurityObservationEventRebuildRow>(
            "SELECT IdempotencyKey, EventTimeUtc, NormalizedIpAddress, NormalizedAccount FROM SecurityObservationEvents WHERE EventTimeUtc >= @WindowStart ORDER BY EventTimeUtc, Id",
            new { WindowStart = windowStart });

        foreach (SecurityObservationEventRebuildRow row in rows)
        {
            if (DateTimeOffset.TryParse(row.EventTimeUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTimeOffset evtTime))
            {
                // 標記冪等性已看過
                if (!string.IsNullOrWhiteSpace(row.IdempotencyKey))
                {
                    idempotencyFilter.TryAccept(new SecurityObservationEvent
                    {
                        SourceAgentName = row.IdempotencyKey,
                        EventTimeUtc = evtTime
                    }, out _);
                }

                // 填入滑動窗
                string ip = row.NormalizedIpAddress?.Trim() ?? string.Empty;
                string account = row.NormalizedAccount?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(account))
                {
                    ipToAccountsBucket.Add(ip, account, evtTime);
                    accountToIpsBucket.Add(account, ip, evtTime);
                }
            }
        }
    }

    private sealed class SecurityObservationEventRebuildRow
    {
                /// <summary>
        /// 取得或設定 IdempotencyKey。
        /// </summary>
public string IdempotencyKey { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 EventTimeUtc。
        /// </summary>
public string EventTimeUtc { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 NormalizedIpAddress。
        /// </summary>
public string NormalizedIpAddress { get; set; } = string.Empty;
                /// <summary>
        /// 取得或設定 NormalizedAccount。
        /// </summary>
public string NormalizedAccount { get; set; } = string.Empty;
    }
}
