using System;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 定義 IDDS 社群版內部監聽服務在 Windows 防火牆中所配置的傳入放行規則規格。
/// </summary>
internal sealed class FirewallInboundRuleDefinition : IEquatable<FirewallInboundRuleDefinition>
{
    /// <summary>
    /// 初始化 <see cref="FirewallInboundRuleDefinition"/> 類別的新執行個體。
    /// </summary>
    /// <param name="featureKey">功能唯一識別碼（例如 SelfServicePortal、ManagementApi、ThreatHub、Honeypot）。</param>
    /// <param name="displayName">Windows 防火牆規則之易讀名稱。</param>
    /// <param name="port">監聽通訊埠號（1-65535）。</param>
    /// <param name="protocol">通訊協定名稱（例如 TCP 或 UDP，預設為 TCP）。</param>
    /// <param name="description">規則詳細描述。</param>
    internal FirewallInboundRuleDefinition(string featureKey, string displayName, int port, string protocol = "TCP", string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (port < 1 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

        FeatureKey = featureKey;
        DisplayName = displayName;
        Port = port;
        Protocol = protocol.ToUpperInvariant();
        Description = description ?? $"IDDS Community Inbound Allow Rule for {featureKey} on {Protocol} port {port}";
    }

    /// <summary>
    /// 取得功能識別碼。
    /// </summary>
    public string FeatureKey { get; }

    /// <summary>
    /// 取得防火牆規則之顯示名稱。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 取得監聽之本機通訊埠。
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// 取得通訊協定（TCP 或 UDP）。
    /// </summary>
    public string Protocol { get; }

    /// <summary>
    /// 取得規則描述。
    /// </summary>
    public string Description { get; }

    /// <inheritdoc />
    public bool Equals(FirewallInboundRuleDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(FeatureKey, other.FeatureKey, StringComparison.OrdinalIgnoreCase)
            && Port == other.Port
            && string.Equals(Protocol, other.Protocol, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as FirewallInboundRuleDefinition);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(
        StringComparer.OrdinalIgnoreCase.GetHashCode(FeatureKey),
        Port,
        StringComparer.OrdinalIgnoreCase.GetHashCode(Protocol));

    /// <inheritdoc />
    public override string ToString() => $"{DisplayName} ({Protocol} {Port})";
}