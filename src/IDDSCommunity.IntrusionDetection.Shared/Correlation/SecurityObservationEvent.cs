namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 代表標準化之跨來源安全性觀察事件模型，封裝事件來源、主機、身分、時間與關聯資訊。
/// </summary>
public class SecurityObservationEvent
{
    /// <summary>
    /// 初始化 <see cref="SecurityObservationEvent"/> 類別的新執行個體。
    /// </summary>
    public SecurityObservationEvent()
    {
        Id = Guid.NewGuid();
        ReceivedTimeUtc = DateTimeOffset.UtcNow;
        EventTimeUtc = DateTimeOffset.UtcNow;
        NormalizedIpAddress = string.Empty;
        NormalizedAccount = string.Empty;
        NormalizedDomain = string.Empty;
        AccountSid = string.Empty;
        SourceAgentName = string.Empty;
        ProviderOrChannel = string.Empty;
        ComputerName = string.Empty;
        OriginalEventReference = string.Empty;
        Provenance = string.Empty;
        ConfidenceScore = 1.0;
        IsCredentialFailure = true;
    }

    /// <summary>
    /// 取得或設定此觀察事件是否為明確之認證憑證失敗（密碼錯誤/帳號不存在）。
    /// 若為授權拒絕（CAP/RAP）、原則不符或系統故障，此值應為 <see langword="false"/>，不計入密碼噴灑門檻。
    /// </summary>
    public bool IsCredentialFailure { get; set; } = true;

    /// <summary>
    /// 取得或設定事件之全域唯一識別碼。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 取得或設定回報此觀察事件之來源擴充元件代理名稱。
    /// </summary>
    public string SourceAgentName { get; set; }

    /// <summary>
    /// 取得或設定 Windows 記錄提供者或事件通道名稱（例如 Microsoft-Windows-Security-Auditing）。
    /// </summary>
    public string ProviderOrChannel { get; set; }

    /// <summary>
    /// 取得或設定產生事件之主機電腦名稱。
    /// </summary>
    public string ComputerName { get; set; }

    /// <summary>
    /// 取得或設定 Windows 事件記錄專屬之 EventRecordID（若來源支援）。
    /// </summary>
    public long? SourceEventRecordId { get; set; }

    /// <summary>
    /// 取得或設定來源檔案或資料流位元組位移（若來源為文字日誌）。
    /// </summary>
    public long? SourceFileOffset { get; set; }

    /// <summary>
    /// 取得或設定來源檔案唯一識別（例如檔案 inode 或名稱雜湊）。
    /// </summary>
    public string? SourceEventIdentity { get; set; }

    /// <summary>
    /// 取得或設定事件實際發生之 UTC 時間戳記。
    /// </summary>
    public DateTimeOffset EventTimeUtc { get; set; }

    /// <summary>
    /// 取得或設定入侵偵測服務接收此事件之 UTC 時間戳記。
    /// </summary>
    public DateTimeOffset ReceivedTimeUtc { get; set; }

    /// <summary>
    /// 取得或設定經正規化之來源 IP 位址（支援標準 IPv4 與 IPv6 格式）。
    /// </summary>
    public string NormalizedIpAddress { get; set; }

    /// <summary>
    /// 取得或設定經正規化之目標使用者帳號名稱。
    /// </summary>
    public string NormalizedAccount { get; set; }

    /// <summary>
    /// 取得或設定經正規化之網域名稱或機器名稱。
    /// </summary>
    public string NormalizedDomain { get; set; }

    /// <summary>
    /// 取得或設定 Windows 安全性識別碼；若來源未提供則為空字串。
    /// </summary>
    public string AccountSid { get; set; }

    /// <summary>
    /// 取得或設定此事件是否被判定為跨來源重複事件。
    /// </summary>
    public bool IsCrossSourceDuplicate { get; set; }

    /// <summary>
    /// 取得或設定此事件所對應之主要觀察事件識別碼。
    /// </summary>
    public Guid? DuplicateOfObservationId { get; set; }

    /// <summary>
    /// 取得或設定原始事件之非敏感摘要引用識別（例如日誌流水號，不包含機密憑證或密碼）。
    /// </summary>
    public string OriginalEventReference { get; set; }

    /// <summary>
    /// 取得或設定事件來源軌跡證明資訊（Provenance）。
    /// </summary>
    public string Provenance { get; set; }

    /// <summary>
    /// 取得或設定 Windows 登入型態代碼（例如 3 代表 Network、8 代表 NetworkCleartext、10 代表 RemoteInteractive）。
    /// </summary>
    public int? LogonType { get; set; }

    /// <summary>
    /// 取得或設定次要狀態碼（例如 0xC000006A 代表密碼錯誤、0xC0000064 代表帳號不存在）。
    /// </summary>
    public string? SubStatus { get; set; }

    /// <summary>
    /// 取得或設定關聯群組識別碼（由關聯引擎指定，同一次多來源攻擊事件將被標記相同群組）。
    /// </summary>
    public Guid? CorrelationGroupId { get; set; }

    /// <summary>
    /// 取得或設定此事件作為獨立攻擊指標之可信度評分（0.0 至 1.0）。
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// 取得或設定事件之活動關聯識別碼（ActivityId / Correlation ID）。
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// 取得或設定目標資源名稱（例如 RDP 終端主機名稱）。
    /// </summary>
    public string? TargetResource { get; set; }

    /// <summary>
    /// 取得或設定失敗錯誤碼（例如 0x80070005 或 23003）。
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 計算此觀察事件之確定性冪等去重索引鍵（Idempotency Key）。
    /// 僅依據實體來源標識產生，絕不使用時間戳記向下取整（Floor Hashing）。
    /// </summary>
    /// <returns>傳回確定性冪等唯一索引鍵字串。</returns>
    public string ComputeIdempotencyKey()
    {
        if (SourceEventRecordId.HasValue && !string.IsNullOrWhiteSpace(ProviderOrChannel) && !string.IsNullOrWhiteSpace(ComputerName))
        {
            return $"REC:{SourceAgentName}|{ProviderOrChannel}|{ComputerName}|{SourceEventRecordId.Value}";
        }

        if (SourceFileOffset.HasValue && !string.IsNullOrWhiteSpace(SourceEventIdentity))
        {
            return $"FILE:{SourceAgentName}|{SourceEventIdentity}|{SourceFileOffset.Value}";
        }

        // 備援通用確定性識別：組合不可變之實體來源屬性與精確微秒級時間
        string rawKey = $"GEN:{SourceAgentName}|{NormalizedIpAddress}|{NormalizedAccount}|{NormalizedDomain}|{EventTimeUtc.ToUnixTimeMilliseconds()}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(hash);
    }
}
