namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義定期安全性報表寄送狀態列舉。
/// </summary>
public enum ReportDeliveryState
{
        /// <summary>
    /// 定義 None 列舉值。
    /// </summary>
None,
        /// <summary>
    /// 定義 Pending 列舉值。
    /// </summary>
Pending,
        /// <summary>
    /// 定義 Sending 列舉值。
    /// </summary>
Sending,
        /// <summary>
    /// 定義 Succeeded 列舉值。
    /// </summary>
Succeeded,
        /// <summary>
    /// 定義 Failed 列舉值。
    /// </summary>
Failed
}
