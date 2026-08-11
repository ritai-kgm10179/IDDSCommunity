using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 表示指定時間區間內的登入失敗統計快照。
/// </summary>
/// <param name="Total">所有 Agent 的登入失敗總數。</param>
/// <param name="AttemptsByAgent">以 Agent 識別碼索引的登入失敗數量。</param>
public sealed record FailedLoginStatisticsSnapshot(int Total, IReadOnlyDictionary<Guid, int> AttemptsByAgent);
