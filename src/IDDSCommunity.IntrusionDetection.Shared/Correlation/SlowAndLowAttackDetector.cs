using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

/// <summary>
/// 提供基於長時序指數衰減評分與 HyperLogLog 帳號多樣性估算之慢速隱蔽密碼噴灑 (Slow &amp; Low) 機器學習異常偵測引擎。
/// </summary>
public sealed class SlowAndLowAttackDetector
{
    private readonly ConcurrentDictionary<string, IpAttackState> ipStates = new();
    private readonly double halfLifeSeconds;
    private readonly double anomalyThreshold;
    private readonly int maxCapacity;

    /// <summary>
    /// 當偵測到長時序慢速隱蔽探測攻擊時引發之事件。
    /// </summary>
    public event Action<string, double, int, string>? SlowAndLowAttackDetected;

    /// <summary>
    /// 初始化 <see cref="SlowAndLowAttackDetector"/> 類別的新執行個體。
    /// </summary>
    /// <param name="halfLifeHours">指數衰減半衰期 (小時，預設 24 小時)。</param>
    /// <param name="anomalyThreshold">觸發封鎖之異常分數門檻 (預設 8.0)。</param>
    /// <param name="maxCapacity">記憶體內最大追蹤 IP 數量 (預設 50000)。</param>
    public SlowAndLowAttackDetector(double halfLifeHours = 24.0, double anomalyThreshold = 8.0, int maxCapacity = 50000)
    {
        this.halfLifeSeconds = Math.Max(1.0, halfLifeHours * 3600.0);
        this.anomalyThreshold = anomalyThreshold;
        this.maxCapacity = maxCapacity;
    }

    /// <summary>
    /// 記錄並分析單一入侵/登入失敗事件，計算常數時間 O(1) 增量異常分數。
    /// </summary>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="targetAccount">探測之目標帳號名稱 (可為空)。</param>
    /// <param name="agentName">發動偵測之安全代理程式名稱。</param>
    /// <param name="timestamp">事件時間 (預設 UtcNow)。</param>
    /// <returns>傳回目前計算後之複合異常分數。</returns>
    public double RecordEvent(string ipAddress, string? targetAccount, string agentName, DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return 0.0;

        DateTime now = timestamp ?? DateTime.UtcNow;
        long nowSeconds = (long)(now - DateTime.UnixEpoch).TotalSeconds;

        // 容量防護與 LRU 淘汰
        if (ipStates.Count > maxCapacity)
        {
            TrimExcess(nowSeconds);
        }

        IpAttackState state = ipStates.GetOrAdd(ipAddress, _ => new IpAttackState(nowSeconds));
        double score;
        int uniqueAccounts;

        lock (state)
        {
            long deltaSeconds = Math.Max(0, nowSeconds - state.LastTimestampSeconds);
            state.LastTimestampSeconds = nowSeconds;

            // 1. 指數衰減計算 (Exponential Decay: Score = Score * 2^(-delta / halfLife))
            double decayFactor = Math.Pow(0.5, deltaSeconds / halfLifeSeconds);
            state.DecayedScore = (state.DecayedScore * decayFactor) + 1.0;

            // 2. 帳號多樣性估算 (使用 16-register 4-bit HyperLogLog 暫存器)
            if (!string.IsNullOrWhiteSpace(targetAccount))
            {
                uint hash = ComputeFastHash(targetAccount.Trim().ToLowerInvariant());
                int registerIndex = (int)(hash & 0x0F);
                uint remaining = (hash >> 4) | 0x80000000;
                byte leadingZeros = (byte)(Math.Min(15, System.Numerics.BitOperations.LeadingZeroCount(remaining) + 1));
                if (leadingZeros > state.HllRegisters[registerIndex])
                {
                    state.HllRegisters[registerIndex] = leadingZeros;
                }
            }

            uniqueAccounts = EstimateCardinality(state.HllRegisters);

            // 3. 帳號噴灑多樣性乘數 (Account Diversity Multiplier: log2(unique + 1))
            double diversityMultiplier = Math.Max(1.0, Math.Log2(uniqueAccounts + 1));
            score = state.DecayedScore * diversityMultiplier;
        }

        // 4. 判定是否超越門檻
        if (score >= anomalyThreshold)
        {
            SlowAndLowAttackDetected?.Invoke(ipAddress, score, uniqueAccounts, agentName);
        }

        return score;
    }

    /// <summary>
    /// 取得指定 IP 目前之計算異常分數。
    /// </summary>
    public double GetCurrentScore(string ipAddress, DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || !ipStates.TryGetValue(ipAddress, out var state))
            return 0.0;

        DateTime now = timestamp ?? DateTime.UtcNow;
        long nowSeconds = (long)(now - DateTime.UnixEpoch).TotalSeconds;

        lock (state)
        {
            long deltaSeconds = Math.Max(0, nowSeconds - state.LastTimestampSeconds);
            double decayFactor = Math.Pow(0.5, deltaSeconds / halfLifeSeconds);
            double decayedScore = state.DecayedScore * decayFactor;
            int uniqueAccounts = EstimateCardinality(state.HllRegisters);
            double diversityMultiplier = Math.Max(1.0, Math.Log2(uniqueAccounts + 1));
            return decayedScore * diversityMultiplier;
        }
    }

    /// <summary>
    /// 清除所有追蹤狀態。
    /// </summary>
    public void Clear() => ipStates.Clear();

    private void TrimExcess(long nowSeconds)
    {
        long cutoff = nowSeconds - (long)(halfLifeSeconds * 3);
        foreach (var kvp in ipStates)
        {
            if (kvp.Value.LastTimestampSeconds < cutoff)
            {
                ipStates.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static uint ComputeFastHash(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash = (hash ^ c) * 16777619;
        }
        return hash;
    }

    private static int EstimateCardinality(byte[] registers)
    {
        double sum = 0.0;
        int zeros = 0;
        int m = registers.Length; // 16

        for (int i = 0; i < m; i++)
        {
            sum += Math.Pow(2.0, -registers[i]);
            if (registers[i] == 0) zeros++;
        }

        double alpha = 0.673; // 針對 m=16 之常數
        double estimate = alpha * m * m / sum;

        // 線性計數微調
        if (estimate <= 2.5 * m && zeros > 0)
        {
            estimate = m * Math.Log((double)m / zeros);
        }

        return Math.Max(1, (int)Math.Round(estimate));
    }

    private sealed class IpAttackState
    {
        public long LastTimestampSeconds { get; set; }
        public double DecayedScore { get; set; }
        public byte[] HllRegisters { get; } = new byte[16];

        public IpAttackState(long startSeconds)
        {
            LastTimestampSeconds = startSeconds;
            DecayedScore = 0.0;
        }
    }
}
