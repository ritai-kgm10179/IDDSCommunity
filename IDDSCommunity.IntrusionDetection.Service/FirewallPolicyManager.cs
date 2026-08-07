using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDDSCommunity.IntrusionDetection.Shared;
using Windows.Win32.NetworkManagement.WindowsFirewall;

namespace IDDSCommunity.IntrusionDetection.Service;

#pragma warning disable CA1416 // This Windows-only component validates the platform before activating FirewallAPI COM.
internal sealed class FirewallPolicyManager : IFirewallPolicy, IDisposable
{
    private readonly INetFwPolicy2 firewallPolicyManager;
    private readonly IRuntimeLog logManager;
    private readonly FirewallBlockMode blockMode;
    private static FirewallPolicyManager? _instance;

    internal static FirewallPolicyManager Instance
    {
        get
        {
            _instance ??= new FirewallPolicyManager(WindowsLogManager.Instance, IddsConfig.Instance.FirewallBlockMode);
            return _instance;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallPolicyManager"/> class.
    /// </summary>

    internal FirewallPolicyManager(IRuntimeLog logManager, FirewallBlockMode blockMode = FirewallBlockMode.Inbound)
    {
        ArgumentNullException.ThrowIfNull(logManager);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Windows Firewall integration requires Windows."));
        this.logManager = logManager;
        this.blockMode = Enum.IsDefined(blockMode) ? blockMode : throw new ArgumentOutOfRangeException(nameof(blockMode));
        firewallPolicyManager = CreateComObject<INetFwPolicy2>("HNetCfg.FwPolicy2");
        if (blockMode == FirewallBlockMode.Inbound)
            RemoveRuleIfPresent(GetRuleName("BlockAttackerOutbound", 0));
    }

    /// <summary>
    /// Creates com object.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="progId">The prog id value.</param>
    /// <returns>The create com object result.</returns>

    private static T CreateComObject<T>(string progId) where T : class =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException(string.Format(IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("COM type {0} is unavailable."), progId))) as T
        ?? throw new InvalidOperationException(string.Format(IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unable to create COM object {0}."), progId));

    /// <summary>
    /// Executes the block operation.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>

    public void Block(string ipAddress)
    {
        try
        {
            AddRule("BlockAttacker", 0, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN,
                NET_FW_ACTION.NET_FW_ACTION_BLOCK, ipAddress);
            if (blockMode == FirewallBlockMode.Bidirectional)
            {
                AddRule("BlockAttackerOutbound", 0, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT,
                    NET_FW_ACTION.NET_FW_ACTION_BLOCK, ipAddress);
            }
        }
        catch (Exception ex)
        {
            logManager.WriteEntry("Create Firewall Rule: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }

    /// <summary>
    /// Determines whether locked.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>
    /// <returns><see langword="true"/> if locked; otherwise, <see langword="false"/>.</returns>

    public bool IsLocked(string ipAddress)
    {
        try
        {
            INetFwRule? inboundRule = GetRule(GetRuleName("BlockAttacker", 0));
            if (!IsEffectiveRule(inboundRule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN)
                || !ContainsAddress(FirewallComString.Get(inboundRule!.RemoteAddresses), ipAddress))
                return false;
            if (blockMode == FirewallBlockMode.Inbound)
                return true;
            INetFwRule? outboundRule = GetRule(GetRuleName("BlockAttackerOutbound", 0));
            return IsEffectiveRule(outboundRule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT)
                && ContainsAddress(FirewallComString.Get(outboundRule!.RemoteAddresses), ipAddress);
        }
        catch (Exception ex)
        {
            logManager.WriteEntry("IsLocked encountered an error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }

    /// <summary>
    /// Returns the exact addresses currently present in the IDDSCommunity block rule.
    /// </summary>
    /// <returns>The normalized firewall address entries.</returns>
    public IReadOnlyCollection<string> GetBlockedAddresses()
    {
        HashSet<string> addresses = new(StringComparer.Ordinal);
        foreach (string ruleName in GetActiveRuleNames())
        {
            INetFwRule? rule = GetRule(ruleName);
            if (rule is null || !rule.Enabled)
                continue;
            foreach (string entry in FirewallComString.Get(rule.RemoteAddresses).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (System.Net.IPAddress.TryParse(entry, out System.Net.IPAddress? address))
                    addresses.Add(address.ToString());
            }
        }
        return [.. addresses];
    }

    /// <summary>
    /// Removes ip address from block list.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>

    public void RemoveIpAddressFromBlockList(string ipAddress)
    {
        bool removed = false;
        foreach (string ruleName in GetActiveRuleNames())
        {
            INetFwRule? rule = GetRule(ruleName);
            if (rule is null)
                continue;
            string remoteAddresses = FirewallComString.Get(rule.RemoteAddresses);
            if (!ContainsAddress(remoteAddresses, ipAddress))
                continue;
            string cleanedAddresses = GetCleanedRemoteAddresses(remoteAddresses, ipAddress);
            FirewallComString.Set(cleanedAddresses, value => rule.RemoteAddresses = value);
            if (cleanedAddresses == "*" || string.IsNullOrWhiteSpace(cleanedAddresses.Replace(',', ' ')))
                rule.Enabled = false;
            removed = true;
        }
        if (!removed)
            throw new ArgumentException(string.Format(
                "The IP address {0} is not blocked and might have been automatically removed by schedule. Please refresh the list to view current locks.", ipAddress), nameof(ipAddress));
    }

    /// <summary>
    /// Gets cleaned remote addresses.
    /// </summary>
    /// <param name="addresses">The addresses value.</param>
    /// <param name="removeAddress">The remove address value.</param>
    /// <returns>The get cleaned remote addresses result.</returns>

    private static string GetCleanedRemoteAddresses(string addresses, string removeAddress)
    {
        StringBuilder result = new();
        string[] addressList;
        if (addresses.Contains(','))
        {
            addressList = addresses.Split(',');
        }
        else
        {
            addressList = new string[1];
            addressList[0] = addresses;
        }
        foreach (string address in addressList)
        {
            string part1;
            if (address.Contains('/'))
            {
                part1 = address.Split('/')[0];
            }
            else
            {
                part1 = address;
            }
            if (!part1.Trim().Equals(removeAddress.Trim()) && !address.Trim().Equals(removeAddress.Trim()))
            {
                result.Append(address + ",");
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Determines whether a firewall address list contains an exact IP address or matching host CIDR entry.
    /// </summary>
    /// <param name="addresses">The comma-delimited firewall address list.</param>
    /// <param name="candidate">The IP address to locate.</param>
    /// <returns><see langword="true"/> when a wildcard, exact address, or containing CIDR entry exists.</returns>
    internal static bool ContainsAddress(string addresses, string candidate)
    {
        if (!System.Net.IPAddress.TryParse(candidate.Trim(), out System.Net.IPAddress? expected))
            return false;
        foreach (string entry in addresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (entry == "*")
                return true;

            string[] cidr = entry.Split('/', 2, StringSplitOptions.TrimEntries);
            if (!System.Net.IPAddress.TryParse(cidr[0], out System.Net.IPAddress? network) || network.AddressFamily != expected.AddressFamily)
                continue;
            if (cidr.Length == 1 && network.Equals(expected))
                return true;
            if (cidr.Length == 2 && int.TryParse(cidr[1], out int prefixLength) && IsInSubnet(expected, network, prefixLength))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Determines whether an IP address belongs to a CIDR network.
    /// </summary>
    /// <param name="candidate">The address being tested.</param>
    /// <param name="network">The network address.</param>
    /// <param name="prefixLength">The CIDR prefix length.</param>
    /// <returns><see langword="true"/> when the candidate belongs to the network.</returns>
    private static bool IsInSubnet(System.Net.IPAddress candidate, System.Net.IPAddress network, int prefixLength)
    {
        byte[] candidateBytes = candidate.GetAddressBytes();
        byte[] networkBytes = network.GetAddressBytes();
        if (prefixLength < 0 || prefixLength > candidateBytes.Length * 8)
            return false;

        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        for (int i = 0; i < fullBytes; i++)
        {
            if (candidateBytes[i] != networkBytes[i])
                return false;
        }

        if (remainingBits == 0)
            return true;

        int mask = 0xFF << (8 - remainingBits);
        return (candidateBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }

    /// <summary>
    /// Gets rule name.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="port">The port value.</param>
    /// <returns>The get rule name result.</returns>

    private static string GetRuleName(string name, int port) => string.Format("{0}_{1}_{2}", Globals.IDDSCOMMUNITY_WINDOWS_IDS_RULE_NAME, name, port == 0 ? "AllPorts" : port.ToString());

    private IEnumerable<string> GetActiveRuleNames()
    {
        yield return GetRuleName("BlockAttacker", 0);
        if (blockMode == FirewallBlockMode.Bidirectional)
            yield return GetRuleName("BlockAttackerOutbound", 0);
    }

    private static bool IsEffectiveRule(INetFwRule? rule, NET_FW_RULE_DIRECTION direction) =>
        rule is not null
        && rule.Enabled
        && rule.Action == NET_FW_ACTION.NET_FW_ACTION_BLOCK
        && rule.Direction == direction
        && rule.Protocol == 256;

    private void RemoveRuleIfPresent(string ruleName)
    {
        if (GetRule(ruleName) is not null)
            FirewallComString.Set(ruleName, firewallPolicyManager.Rules.Remove);
    }

    /// <summary>
    /// Adds rule.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="port">The port value.</param>
    /// <param name="direction">The direction value.</param>
    /// <param name="action">The action value.</param>
    /// <param name="remoteAddress">The remote address value.</param>

    internal void AddRule(string name, int port, NET_FW_RULE_DIRECTION direction,
        NET_FW_ACTION action, string remoteAddress)
    {
        bool ruleExists = false;
        string ipAddress;
        string ruleName = GetRuleName(name, port);
        INetFwRule? rule = GetRule(ruleName);
        if (rule != null)
        {
            ruleExists = true;
        }
        else
        {
            try
            {
                rule = CreateComObject<INetFwRule>("HNetCfg.FWRule");
            }
            catch (Exception)
            {
                throw;
            }
        }
        if (IddsConfig.IsValidIpAddress(remoteAddress))
        {
            ipAddress = remoteAddress;
        }
        else
        {
            throw new ArgumentOutOfRangeException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IP address must be given in IP version 4 or IP version 6 format!"));
        }
        // ipAddress = String.Format("{0}/255.255.255.255", ipAddress);

        if (!ruleExists)
        {
            rule.Action = action;
            FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, value => rule.Grouping = value);
            rule.Protocol = 256;
            FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME + " rule", value => rule.Description = value);
            rule.Direction = direction;
            rule.Enabled = true;

            if (port > 0)
                FirewallComString.Set(port.ToString(), value => rule.LocalPorts = value);
            FirewallComString.Set(ruleName, value => rule.Name = value);
            FirewallComString.Set(ipAddress, value => rule.RemoteAddresses = value);
            //  rule.RemotePorts = "";
            firewallPolicyManager.Rules.Add(rule);
        }
        else
        {
            rule.Action = action;
            rule.Direction = direction;
            rule.Protocol = 256;
            rule.Enabled = true;
            string existingAddresses = FirewallComString.Get(rule.RemoteAddresses);
            FirewallComString.Set(MergeRemoteAddresses(existingAddresses, ipAddress), value => rule.RemoteAddresses = value);
        }
    }

    internal static string MergeRemoteAddresses(string existingAddresses, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        if (string.IsNullOrWhiteSpace(existingAddresses) || existingAddresses.Trim().Equals("*", StringComparison.Ordinal))
            return address;
        if (existingAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(address, StringComparer.OrdinalIgnoreCase)) return existingAddresses;
        return string.Concat(existingAddresses, ",", address);
    }

    /// <summary>
    /// Clears up rules.
    /// </summary>

    internal void CleanUpRules()
    {
        foreach (INetFwRule rule in FindRules(Globals.IDDSCOMMUNITY_WINDOWS_IDS_RULE_NAME))
        {
            //rule.RemoteAddresses = "";
            FirewallComString.Set(FirewallComString.Get(rule.Name), firewallPolicyManager.Rules.Remove);
        }
    }

    /// <summary>
    /// Gets rule.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The get rule result.</returns>

    internal INetFwRule? GetRule(string name)
    {
        foreach (INetFwRule rule in (dynamic)firewallPolicyManager.Rules)
        {
            if (FirewallComString.Get(rule.Name) == name) return rule;
        }
        return null;
    }

    /// <summary>
    /// Finds rules.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The find rules result.</returns>

    internal List<INetFwRule> FindRules(string name)
    {
        List<INetFwRule> rules = [];
        foreach (INetFwRule rule in (dynamic)firewallPolicyManager.Rules)
        {
            if (FirewallComString.Get(rule.Name).StartsWith(name, StringComparison.Ordinal)) rules.Add(rule);
        }
        return rules;
    }

    /// <summary>
    /// 聚合 IP 位址。當同一 /24 C 段子網出現超過指定門檻個 IP 時，自動轉換為 CIDR 條目。
    /// 若目標 C 段中含有 Safe Networks 白名單 IP，則跳過 CIDR 聚合以避免誤殺合法流量。
    /// </summary>
    /// <param name="addresses">原始 IP 位址集合。</param>
    /// <param name="safeNetworks">全域白名單 / 安全網路 IP 與 CIDR 集合。</param>
    /// <param name="subnetThreshold">觸發 C 段聚合的 IP 數量門檻。</param>
    /// <returns>傳回經過 CIDR 聚合與白名單保護後的位址清單。</returns>
    internal static List<string> AggregateIpAddresses(IEnumerable<string> addresses, IEnumerable<string>? safeNetworks = null, int subnetThreshold = 5)
    {
        List<string> result = [];
        Dictionary<string, List<string>> subnetGroups = new(StringComparer.Ordinal);
        List<string> nonIpv4OrCidr = [];
        HashSet<string> safeIpPrefixes = new(StringComparer.OrdinalIgnoreCase);

        if (safeNetworks is not null)
        {
            foreach (string safe in safeNetworks)
            {
                string s = safe.Trim();
                if (string.IsNullOrEmpty(s)) continue;
                if (System.Net.IPAddress.TryParse(s.Split('/')[0], out System.Net.IPAddress? safeIp) &&
                    safeIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    byte[] bytes = safeIp.GetAddressBytes();
                    safeIpPrefixes.Add($"{bytes[0]}.{bytes[1]}.{bytes[2]}");
                }
            }
        }

        foreach (string addr in addresses)
        {
            string trimmed = addr.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (System.Net.IPAddress.TryParse(trimmed, out System.Net.IPAddress? ip) &&
                ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] bytes = ip.GetAddressBytes();
                string prefix = $"{bytes[0]}.{bytes[1]}.{bytes[2]}";
                if (!subnetGroups.TryGetValue(prefix, out List<string>? group))
                {
                    group = [];
                    subnetGroups[prefix] = group;
                }
                if (!group.Contains(trimmed)) group.Add(trimmed);
            }
            else
            {
                nonIpv4OrCidr.Add(trimmed);
            }
        }

        foreach (var (prefix, group) in subnetGroups)
        {
            if (group.Count >= subnetThreshold && !safeIpPrefixes.Contains(prefix))
            {
                result.Add($"{prefix}.0/24");
            }
            else
            {
                result.AddRange(group);
            }
        }

        result.AddRange(nonIpv4OrCidr);
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Releases the COM firewall policy object owned by this manager.
    /// </summary>
    public void Dispose()
    {
        if (System.Runtime.InteropServices.Marshal.IsComObject(firewallPolicyManager))
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(firewallPolicyManager);
    }

}
#pragma warning restore CA1416
