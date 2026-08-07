namespace IDDSCommunity.IntrusionDetection.Shared;
/// <summary>
/// Defines how IDDS Community applies host firewall blocks.
/// </summary>
public enum FirewallBlockMode
{
    /// <summary>
    /// Blocks matching remote addresses from initiating inbound traffic.
    /// </summary>
    Inbound = 0,
    /// <summary>
    /// Blocks matching remote addresses in both inbound and outbound directions.
    /// </summary>
    Bidirectional = 1
}
