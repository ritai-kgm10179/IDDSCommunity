using System;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly object _firewallLock = new();

    internal static FirewallPolicyManager Instance
    {
        get
        {
            _instance ??= new FirewallPolicyManager(WindowsLogManager.Instance, IddsConfig.Instance.FirewallBlockMode);
            return _instance;
        }
    }
    /// <summary>
    /// 初始化 <see cref="FirewallPolicyManager"/> 類別的新執行個體。
    /// </summary>
    internal FirewallPolicyManager(IRuntimeLog logManager, FirewallBlockMode blockMode = FirewallBlockMode.Inbound)
    {
        ArgumentNullException.ThrowIfNull(logManager);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Windows Firewall integration requires Windows."));
        this.logManager = logManager;
        this.blockMode = Enum.IsDefined(blockMode) ? blockMode : throw new ArgumentOutOfRangeException(nameof(blockMode));
        lock (_firewallLock)
        {
            firewallPolicyManager = CreateComObject<INetFwPolicy2>("HNetCfg.FwPolicy2");
            if (blockMode == FirewallBlockMode.Inbound)
            {
                foreach (string name in GetActiveShardedRuleNames(GetRuleName("BlockAttackerOutbound", 0)))
                    RemoveRuleIfPresent(name);
            }
        }
    }
    /// <summary>
    /// Creates com object.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="progId">prog id 的值。</param>
    /// <returns>傳回 create com object 的結果。</returns>
    private static T CreateComObject<T>(string progId) where T : class =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException(string.Format(IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("COM type {0} is unavailable."), progId))) as T
        ?? throw new InvalidOperationException(string.Format(IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unable to create COM object {0}."), progId));
    /// <summary>
    /// 執行 block 作業。
    /// </summary>
    /// <param name="ipAddress">ip address 的值。</param>
    public void Block(string ipAddress)
    {
        lock (_firewallLock)
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
    }

    /// <summary>
    /// 批次將多個 IP 位址加入 Windows 防火牆阻擋規則（支援 CIDR 聚合與切片批次寫入）。
    /// </summary>
    /// <param name="ipAddresses">要批次阻擋之 IP 位址清單。</param>
    public void BatchBlock(IReadOnlyCollection<string> ipAddresses)
    {
        if (ipAddresses == null || ipAddresses.Count == 0) return;

        List<string> validIps = [];
        foreach (string raw in ipAddresses)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string trimmed = raw.Trim();
            if (IddsConfig.IsValidIpAddress(trimmed) || System.Net.IPNetwork.TryParse(trimmed, out _))
            {
                validIps.Add(trimmed);
            }
        }
        if (validIps.Count == 0) return;

        List<string> aggregated = AggregateIpAddresses(
            validIps,
            IddsConfig.Instance.UseSafeNetworkList ? IddsConfig.Instance.SafeNetworks.ConvertAll(s => s.IpAddress) : null);

        lock (_firewallLock)
        {
            try
            {
                AddShardedRulesBatch("BlockAttacker", 0, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN,
                    NET_FW_ACTION.NET_FW_ACTION_BLOCK, aggregated);
                if (blockMode == FirewallBlockMode.Bidirectional)
                {
                    AddShardedRulesBatch("BlockAttackerOutbound", 0, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT,
                        NET_FW_ACTION.NET_FW_ACTION_BLOCK, aggregated);
                }
            }
            catch (Exception ex)
            {
                logManager.WriteEntry("Batch Create Firewall Rules: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                    Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                throw;
            }
        }
    }
    /// <summary>
    /// Determines whether locked.
    /// </summary>
    /// <param name="ipAddress">ip address 的值。</param>
    /// <returns><see langword="true"/> if locked; otherwise, <see langword="false"/>.</returns>
    public bool IsLocked(string ipAddress)
    {
        lock (_firewallLock)
        {
            try
            {
                string inBase = GetRuleName("BlockAttacker", 0);
                bool lockedIn = false;
                foreach (string ruleName in GetActiveShardedRuleNames(inBase))
                {
                    INetFwRule? inboundRule = GetRule(ruleName);
                    if (IsEffectiveRule(inboundRule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN)
                        && ContainsAddress(FirewallComString.Get(inboundRule!.RemoteAddresses), ipAddress))
                    {
                        lockedIn = true;
                        break;
                    }
                }

                if (!lockedIn)
                    return false;

                if (blockMode == FirewallBlockMode.Inbound)
                    return true;

                string outBase = GetRuleName("BlockAttackerOutbound", 0);
                foreach (string ruleName in GetActiveShardedRuleNames(outBase))
                {
                    INetFwRule? outboundRule = GetRule(ruleName);
                    if (IsEffectiveRule(outboundRule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT)
                        && ContainsAddress(FirewallComString.Get(outboundRule!.RemoteAddresses), ipAddress))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                logManager.WriteEntry("IsLocked encountered an error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                    Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                throw;
            }
        }
    }

    /// <summary>
    /// 批次篩選給定之 IP 位址清單中，哪些位址已處於 Windows 防火牆阻擋規則中。
    /// </summary>
    /// <param name="ipAddresses">欲檢驗之 IP 位址清單。</param>
    /// <returns>已被防火牆阻擋之 IP 位址集合。</returns>
    public HashSet<string> FilterLockedIps(IEnumerable<string> ipAddresses)
    {
        if (ipAddresses is null) return [];

        lock (_firewallLock)
        {
            try
            {
                ParsedRuleAddresses inboundParsed = new();
                string inBase = GetRuleName("BlockAttacker", 0);
                foreach (string ruleName in GetActiveShardedRuleNames(inBase))
                {
                    INetFwRule? inboundRule = GetRule(ruleName);
                    if (IsEffectiveRule(inboundRule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN))
                    {
                        inboundParsed.AddRange(FirewallComString.Get(inboundRule!.RemoteAddresses));
                    }
                }

                ParsedRuleAddresses? outboundParsed = null;
                if (blockMode == FirewallBlockMode.Bidirectional)
                {
                    outboundParsed = new();
                    string outBase = GetRuleName("BlockAttackerOutbound", 0);
                    foreach (string ruleName in GetActiveShardedRuleNames(outBase))
                    {
                        INetFwRule? outboundRule = GetRule(ruleName);
                        if (IsEffectiveRule(outboundRule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT))
                        {
                            outboundParsed.AddRange(FirewallComString.Get(outboundRule!.RemoteAddresses));
                        }
                    }
                }

                HashSet<string> lockedIps = new(StringComparer.OrdinalIgnoreCase);
                foreach (string rawIp in ipAddresses)
                {
                    if (string.IsNullOrWhiteSpace(rawIp)) continue;
                    string candidate = rawIp.Trim();
                    if (!System.Net.IPAddress.TryParse(candidate, out System.Net.IPAddress? parsedIp))
                        continue;

                    if (!inboundParsed.Contains(parsedIp))
                        continue;

                    if (outboundParsed is not null && !outboundParsed.Contains(parsedIp))
                        continue;

                    lockedIps.Add(candidate);
                }

                return lockedIps;
            }
            catch (Exception ex)
            {
                logManager.WriteEntry("FilterLockedIps encountered an error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                    Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                throw;
            }
        }
    }

    internal sealed class ParsedRuleAddresses
    {
        public bool Wildcard { get; set; }
        public HashSet<System.Net.IPAddress> ExactAddresses { get; } = [];
        public List<(System.Net.IPAddress Network, int PrefixLength)> Subnets { get; } = [];

        public void AddRange(string remoteAddresses)
        {
            if (string.IsNullOrWhiteSpace(remoteAddresses)) return;
            foreach (string entry in remoteAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (entry == "*")
                {
                    Wildcard = true;
                    continue;
                }
                string[] cidr = entry.Split('/', 2, StringSplitOptions.TrimEntries);
                if (!System.Net.IPAddress.TryParse(cidr[0], out System.Net.IPAddress? network))
                    continue;
                if (cidr.Length == 1)
                {
                    ExactAddresses.Add(network);
                }
                else if (cidr.Length == 2)
                {
                    if (int.TryParse(cidr[1], out int prefixLength)
                        || TryConvertSubnetMaskToPrefixLength(cidr[1], out prefixLength))
                    {
                        Subnets.Add((network, prefixLength));
                    }
                }
            }
        }

        public bool Contains(System.Net.IPAddress address)
        {
            if (Wildcard) return true;
            if (ExactAddresses.Contains(address)) return true;
            foreach (var (network, prefixLength) in Subnets)
            {
                if (network.AddressFamily == address.AddressFamily && IsInSubnet(address, network, prefixLength))
                    return true;
            }
            return false;
        }
    }
    /// <summary>
    /// Returns the exact addresses currently present in the IDDSCommunity block rule.
    /// </summary>
    /// <returns>傳回 normalized firewall address entries 的結果。</returns>
    public IReadOnlyCollection<string> GetBlockedAddresses()
        => GetBlockState().EffectiveAddresses;

    public FirewallBlockState GetBlockState()
    {
        lock (_firewallLock)
        {
            HashSet<string> inbound = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> outbound = new(StringComparer.OrdinalIgnoreCase);
            string inboundBase = GetRuleName("BlockAttacker", 0);
            string outboundBase = GetRuleName("BlockAttackerOutbound", 0);
            foreach (INetFwRule rule in FindRules(Globals.IDDSCOMMUNITY_WINDOWS_IDS_RULE_NAME))
            {
                string ruleName = FirewallComString.Get(rule.Name);
                HashSet<string>? target = ruleName.StartsWith(inboundBase, StringComparison.Ordinal)
                    && IsEffectiveRule(rule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN)
                    ? inbound
                    : ruleName.StartsWith(outboundBase, StringComparison.Ordinal)
                        && IsEffectiveRule(rule, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT)
                        ? outbound
                        : null;
                if (target is null)
                    continue;
                foreach (string entry in FirewallComString.Get(rule.RemoteAddresses).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    string? normalized = NormalizeRemoteAddressEntry(entry);
                    if (normalized is not null)
                        target.Add(normalized);
                }
            }
            HashSet<string> anyDirection = new(inbound, StringComparer.OrdinalIgnoreCase);
            anyDirection.UnionWith(outbound);
            if (blockMode == FirewallBlockMode.Bidirectional)
                inbound.IntersectWith(outbound);
            return new FirewallBlockState(
                [.. inbound.Order(StringComparer.Ordinal)],
                [.. anyDirection.Order(StringComparer.Ordinal)]);
        }
    }
    /// <summary>
    /// 將 Windows 防火牆位址項目正規化為單一位址或 CIDR 表示法。
    /// </summary>
    /// <param name="entry">防火牆遠端位址項目。</param>
    /// <returns>正規化位址；無法辨識時傳回 <see langword="null"/>。</returns>
    internal static string? NormalizeRemoteAddressEntry(string entry)
    {
        string[] parts = entry.Split('/', 2, StringSplitOptions.TrimEntries);
        if (!System.Net.IPAddress.TryParse(parts[0], out System.Net.IPAddress? address))
            return null;
        if (parts.Length == 1)
            return address.ToString();

        int prefixLength;
        if (!int.TryParse(parts[1], out prefixLength)
            && !TryConvertSubnetMaskToPrefixLength(parts[1], out prefixLength))
            return null;
        int maximumPrefixLength = address.GetAddressBytes().Length * 8;
        if (prefixLength < 0 || prefixLength > maximumPrefixLength)
            return null;
        return FormatNetwork(MaskAddress(address, prefixLength), prefixLength);
    }
    /// <summary>
    /// Removes ip address from block list.
    /// </summary>
    /// <param name="ipAddress">ip address 的值。</param>
    public void RemoveIpAddressFromBlockList(string ipAddress)
    {
        lock (_firewallLock)
        {
            bool removed = false;
            foreach (string ruleName in GetActiveRuleNames().ToList())
            {
                INetFwRule? rule = GetRule(ruleName);
                if (rule is null)
                    continue;
                string remoteAddresses = FirewallComString.Get(rule.RemoteAddresses);
                if (!ContainsAddress(remoteAddresses, ipAddress))
                    continue;
                string cleanedAddresses = GetCleanedRemoteAddresses(remoteAddresses, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ipAddress.Trim() });
                if (string.IsNullOrWhiteSpace(cleanedAddresses.Replace(',', ' ')))
                {
                    RemoveRuleIfPresent(ruleName);
                }
                else
                {
                    FirewallComString.Set(cleanedAddresses.TrimEnd(','), value => rule.RemoteAddresses = value);
                }
                removed = true;
            }
            if (!removed)
                throw new ArgumentException(string.Format(
                    "The IP address {0} is not blocked and might have been automatically removed by schedule. Please refresh the list to view current locks.", ipAddress), nameof(ipAddress));
        }
    }

    /// <summary>
    /// 批次自 Windows 防火牆阻擋規則中移除多個 IP 位址，以單一 Pass 更新分片規則以杜絕 COM 昂貴耗時。
    /// </summary>
    /// <param name="ipAddresses">要批次移除之 IP 位址清單。</param>
    public void BatchRemove(IReadOnlyCollection<string> ipAddresses)
    {
        if (ipAddresses == null || ipAddresses.Count == 0) return;
        HashSet<string> removeSet = new(ipAddresses.Where(ip => !string.IsNullOrWhiteSpace(ip)).Select(ip => ip.Trim()), StringComparer.OrdinalIgnoreCase);
        if (removeSet.Count == 0) return;

        lock (_firewallLock)
        {
            foreach (string ruleName in GetActiveRuleNames().ToList())
            {
                INetFwRule? rule = GetRule(ruleName);
                if (rule is null)
                    continue;
                string remoteAddresses = FirewallComString.Get(rule.RemoteAddresses);
                bool hasMatch = removeSet.Any(address => ContainsAddress(remoteAddresses, address));
                if (!hasMatch)
                    continue;

                string cleanedAddresses = GetCleanedRemoteAddresses(remoteAddresses, removeSet);
                if (string.IsNullOrWhiteSpace(cleanedAddresses.Replace(',', ' ')))
                {
                    RemoveRuleIfPresent(ruleName);
                }
                else
                {
                    FirewallComString.Set(cleanedAddresses.TrimEnd(','), value => rule.RemoteAddresses = value);
                }
            }
        }
    }

    /// <summary>
    /// 重整並壓縮既有 Windows 防火牆分片規則，重新進行 CIDR 網段聚合並清除破碎或空洞分片。
    /// </summary>
    /// <param name="safeNetworks">選擇性的安全網路全域白名單集合。</param>
    public void CompactBlockRules(IEnumerable<string>? safeNetworks = null)
    {
        lock (_firewallLock)
        {
            try
            {
                IReadOnlyCollection<string> current = GetBlockedAddresses();
                if (current.Count == 0)
                {
                    CleanUpRules();
                    return;
                }

                safeNetworks ??= IddsConfig.Instance.UseSafeNetworkList
                    ? IddsConfig.Instance.SafeNetworks.ConvertAll(s => s.IpAddress)
                    : null;

                List<string> aggregated = AggregateIpAddresses(current, safeNetworks, subnetThreshold: 5);

                ApplyCompactedShards("BlockAttacker", 0, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN,
                    NET_FW_ACTION.NET_FW_ACTION_BLOCK, aggregated);

                if (blockMode == FirewallBlockMode.Bidirectional)
                {
                    ApplyCompactedShards("BlockAttackerOutbound", 0, NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_OUT,
                        NET_FW_ACTION.NET_FW_ACTION_BLOCK, aggregated);
                }
                else
                {
                    string outBase = GetRuleName("BlockAttackerOutbound", 0);
                    foreach (string name in GetActiveShardedRuleNames(outBase))
                    {
                        RemoveRuleIfPresent(name);
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.WriteEntry("CompactBlockRules failed: " + ex.Message,
                    System.Diagnostics.EventLogEntryType.Warning,
                    Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL,
                    Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            }
        }
    }

    private void ApplyCompactedShards(string name, int port, NET_FW_RULE_DIRECTION direction,
        NET_FW_ACTION action, IReadOnlyList<string> addresses)
    {
        string baseRuleName = GetRuleName(name, port);
        List<INetFwRule> existingShards = [];
        foreach (INetFwRule r in FindRules(baseRuleName))
        {
            string rName = FirewallComString.Get(r.Name);
            if (rName == baseRuleName || rName.StartsWith(baseRuleName + "_", StringComparison.Ordinal))
            {
                existingShards.Add(r);
            }
        }
        existingShards.Sort((left, right) => StringComparer.Ordinal.Compare(FirewallComString.Get(left.Name), FirewallComString.Get(right.Name)));

        int chunkCount = addresses.Count == 0 ? 0 : (addresses.Count + MaxAddressesPerRule - 1) / MaxAddressesPerRule;

        for (int i = 0; i < chunkCount; i++)
        {
            int start = i * MaxAddressesPerRule;
            int count = Math.Min(MaxAddressesPerRule, addresses.Count - start);
            List<string> chunk = addresses.Skip(start).Take(count).ToList();
            string chunkAddresses = string.Join(",", chunk);
            string shardName = i == 0 ? baseRuleName : $"{baseRuleName}_{i}";

            if (i < existingShards.Count)
            {
                INetFwRule shard = existingShards[i];
                shard.Action = action;
                shard.Direction = direction;
                shard.Protocol = 256;
                shard.Enabled = true;
                FirewallComString.Set(chunkAddresses, val => shard.RemoteAddresses = val);
            }
            else
            {
                INetFwRule newRule = CreateComObject<INetFwRule>("HNetCfg.FWRule");
                newRule.Action = action;
                FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, value => newRule.Grouping = value);
                newRule.Protocol = 256;
                FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME + " rule", value => newRule.Description = value);
                newRule.Direction = direction;
                newRule.Enabled = true;
                if (port > 0)
                    FirewallComString.Set(port.ToString(), value => newRule.LocalPorts = value);
                FirewallComString.Set(shardName, value => newRule.Name = value);
                FirewallComString.Set(chunkAddresses, value => newRule.RemoteAddresses = value);
                firewallPolicyManager.Rules.Add(newRule);
            }
        }

        for (int i = chunkCount; i < existingShards.Count; i++)
        {
            string excessName = FirewallComString.Get(existingShards[i].Name);
            RemoveRuleIfPresent(excessName);
        }
    }

    /// <summary>
    /// Gets cleaned remote addresses.
    /// </summary>
    /// <param name="addresses">addresses 的值。</param>
    /// <param name="removeAddress">remove address 的值。</param>
    /// <returns>傳回 get cleaned remote addresses 的結果。</returns>
    private static string GetCleanedRemoteAddresses(string addresses, string removeAddress) =>
        GetCleanedRemoteAddresses(addresses, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { removeAddress.Trim() });

    internal static string GetCleanedRemoteAddresses(string addresses, HashSet<string> removeSet)
    {
        List<string> result = [];
        foreach (string address in addresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string trimmed = address.Trim();
            List<string> remaining = [trimmed];
            foreach (string removal in removeSet)
            {
                List<string> next = [];
                foreach (string entry in remaining)
                    next.AddRange(ExceptAddress(entry, removal));
                remaining = next;
                if (remaining.Count == 0)
                    break;
            }
            result.AddRange(remaining);
        }
        return string.Join(',', result.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    internal static IReadOnlyList<string> ExceptAddress(string entry, string removal)
    {
        string? normalizedEntry = NormalizeRemoteAddressEntry(entry);
        string? normalizedRemoval = NormalizeRemoteAddressEntry(removal);
        if (normalizedEntry is null || normalizedRemoval is null)
            return [entry];

        (System.Net.IPAddress Network, int PrefixLength) source = ParseNetwork(normalizedEntry);
        (System.Net.IPAddress Network, int PrefixLength) excluded = ParseNetwork(normalizedRemoval);
        if (source.Network.AddressFamily != excluded.Network.AddressFamily || !NetworksOverlap(source, excluded))
            return [normalizedEntry];
        if (excluded.PrefixLength <= source.PrefixLength)
            return [];

        List<string> result = [];
        SubtractNetwork(source.Network, source.PrefixLength, excluded, result);
        return result;
    }

    private static void SubtractNetwork(System.Net.IPAddress network, int prefixLength,
        (System.Net.IPAddress Network, int PrefixLength) excluded, List<string> result)
    {
        int maximumPrefixLength = network.GetAddressBytes().Length * 8;
        if (prefixLength == maximumPrefixLength)
            return;
        int childPrefix = prefixLength + 1;
        System.Net.IPAddress first = MaskAddress(network, childPrefix);
        byte[] secondBytes = first.GetAddressBytes();
        secondBytes[prefixLength / 8] |= (byte)(1 << (7 - prefixLength % 8));
        System.Net.IPAddress second = new(secondBytes);
        foreach (System.Net.IPAddress child in new[] { first, second })
        {
            var childNetwork = (Network: child, PrefixLength: childPrefix);
            if (!NetworksOverlap(childNetwork, excluded))
                result.Add(FormatNetwork(child, childPrefix));
            else if (excluded.PrefixLength > childPrefix)
                SubtractNetwork(child, childPrefix, excluded, result);
        }
    }

    private static (System.Net.IPAddress Network, int PrefixLength) ParseNetwork(string value)
    {
        string[] parts = value.Split('/', 2);
        System.Net.IPAddress address = System.Net.IPAddress.Parse(parts[0]);
        int prefixLength = parts.Length == 1 ? address.GetAddressBytes().Length * 8 : int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
        return (MaskAddress(address, prefixLength), prefixLength);
    }

    private static bool NetworksOverlap((System.Net.IPAddress Network, int PrefixLength) left,
        (System.Net.IPAddress Network, int PrefixLength) right) =>
        IsInSubnet(left.Network, right.Network, Math.Min(left.PrefixLength, right.PrefixLength));

    private static System.Net.IPAddress MaskAddress(System.Net.IPAddress address, int prefixLength)
    {
        byte[] bytes = address.GetAddressBytes();
        for (int bit = prefixLength; bit < bytes.Length * 8; bit++)
            bytes[bit / 8] &= (byte)~(1 << (7 - bit % 8));
        return new System.Net.IPAddress(bytes);
    }

    private static string FormatNetwork(System.Net.IPAddress network, int prefixLength)
    {
        int maximumPrefixLength = network.GetAddressBytes().Length * 8;
        return prefixLength == maximumPrefixLength ? network.ToString() : $"{network}/{prefixLength}";
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
            if (cidr.Length == 2)
            {
                if (int.TryParse(cidr[1], out int prefixLength) && IsInSubnet(expected, network, prefixLength))
                    return true;
                if (TryConvertSubnetMaskToPrefixLength(cidr[1], out prefixLength)
                    && IsInSubnet(expected, network, prefixLength))
                    return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 將 Windows 防火牆可回傳的 IPv4 點分十進位子網路遮罩轉換成前綴長度。
    /// </summary>
    /// <param name="maskText">IPv4 子網路遮罩。</param>
    /// <param name="prefixLength">轉換成功時的 CIDR 前綴長度。</param>
    /// <returns>遮罩有效且位元連續時傳回 <see langword="true"/>。</returns>
    private static bool TryConvertSubnetMaskToPrefixLength(string maskText, out int prefixLength)
    {
        prefixLength = 0;
        if (!System.Net.IPAddress.TryParse(maskText, out System.Net.IPAddress? mask)
            || mask.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        bool encounteredZero = false;
        foreach (byte value in mask.GetAddressBytes())
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                bool set = (value & (1 << bit)) != 0;
                if (encounteredZero && set)
                    return false;
                if (set)
                    prefixLength++;
                else
                    encounteredZero = true;
            }
        }
        return true;
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
    /// <param name="name">name 的值。</param>
    /// <param name="port">port 的值。</param>
    /// <returns>傳回 get rule name 的結果。</returns>
    private static string GetRuleName(string name, int port) => string.Format("{0}_{1}_{2}", Globals.IDDSCOMMUNITY_WINDOWS_IDS_RULE_NAME, name, port == 0 ? "AllPorts" : port.ToString());

    private const int MaxAddressesPerRule = 1000;

    private List<string> GetActiveShardedRuleNames(string baseName)
    {
        List<string> result = [];
        foreach (INetFwRule rule in FindRules(baseName))
        {
            string name = FirewallComString.Get(rule.Name);
            if (name == baseName || name.StartsWith(baseName + "_", StringComparison.Ordinal))
            {
                result.Add(name);
            }
        }
        if (result.Count == 0)
        {
            result.Add(baseName);
        }
        return result;
    }

    private IEnumerable<string> GetActiveRuleNames()
    {
        string inBase = GetRuleName("BlockAttacker", 0);
        foreach (string name in GetActiveShardedRuleNames(inBase))
            yield return name;

        if (blockMode == FirewallBlockMode.Bidirectional)
        {
            string outBase = GetRuleName("BlockAttackerOutbound", 0);
            foreach (string name in GetActiveShardedRuleNames(outBase))
                yield return name;
        }
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
    /// 新增分組切片阻擋規則。若既有規則已滿（達 1,000 筆 IP），自動擴展建立新切片。
    /// </summary>
    /// <param name="name">規則名稱前綴識別碼。</param>
    /// <param name="port">本機連接埠。</param>
    /// <param name="direction">規則方向（傳入或傳出）。</param>
    /// <param name="action">規則動作（阻擋或允許）。</param>
    /// <param name="remoteAddress">遠端 IP 位址。</param>
    internal void AddShardedRule(string name, int port, NET_FW_RULE_DIRECTION direction,
        NET_FW_ACTION action, string remoteAddress)
    {
        if (!IddsConfig.IsValidIpAddress(remoteAddress))
        {
            throw new ArgumentOutOfRangeException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IP address must be given in IP version 4 or IP version 6 format!"));
        }

        string baseRuleName = GetRuleName(name, port);
        List<INetFwRule> existingShards = [];
        foreach (INetFwRule r in FindRules(baseRuleName))
        {
            string rName = FirewallComString.Get(r.Name);
            if (rName == baseRuleName || rName.StartsWith(baseRuleName + "_", StringComparison.Ordinal))
            {
                existingShards.Add(r);
            }
        }
        existingShards.Sort((left, right) => StringComparer.Ordinal.Compare(FirewallComString.Get(left.Name), FirewallComString.Get(right.Name)));

        foreach (INetFwRule shard in existingShards)
        {
            string existing = FirewallComString.Get(shard.RemoteAddresses);
            if (ContainsAddress(existing, remoteAddress))
            {
                shard.Action = action;
                shard.Direction = direction;
                shard.Protocol = 256;
                shard.Enabled = true;
                return;
            }
        }

        foreach (INetFwRule shard in existingShards)
        {
            string existing = FirewallComString.Get(shard.RemoteAddresses);
            int count = existing.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
            if (count < MaxAddressesPerRule)
            {
                shard.Action = action;
                shard.Direction = direction;
                shard.Protocol = 256;
                shard.Enabled = true;
                FirewallComString.Set(MergeRemoteAddresses(existing, remoteAddress), val => shard.RemoteAddresses = val);
                return;
            }
        }

        int nextShardIndex = existingShards.Count;
        string newRuleName = nextShardIndex == 0 ? baseRuleName : $"{baseRuleName}_{nextShardIndex}";

        INetFwRule newRule = CreateComObject<INetFwRule>("HNetCfg.FWRule");
        newRule.Action = action;
        FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, value => newRule.Grouping = value);
        newRule.Protocol = 256;
        FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME + " rule", value => newRule.Description = value);
        newRule.Direction = direction;
        newRule.Enabled = true;

        if (port > 0)
            FirewallComString.Set(port.ToString(), value => newRule.LocalPorts = value);
        FirewallComString.Set(newRuleName, value => newRule.Name = value);
        FirewallComString.Set(remoteAddress, value => newRule.RemoteAddresses = value);
        firewallPolicyManager.Rules.Add(newRule);
    }

    /// <summary>
    /// Adds rule.
    /// </summary>
    /// <param name="name">name 的值。</param>
    /// <param name="port">port 的值。</param>
    /// <param name="direction">direction 的值。</param>
    /// <param name="action">action 的值。</param>
    /// <param name="remoteAddress">remote address 的值。</param>
    internal void AddRule(string name, int port, NET_FW_RULE_DIRECTION direction,
        NET_FW_ACTION action, string remoteAddress)
    {
        AddShardedRule(name, port, direction, action, remoteAddress);
    }

    /// <summary>
    /// 批次新增分組切片阻擋規則。先補滿既有未滿 1,000 筆之切片，其餘按 1,000 筆/切片一次性建立新規則。
    /// </summary>
    /// <param name="name">規則名稱前綴識別碼。</param>
    /// <param name="port">本機連接埠。</param>
    /// <param name="direction">規則方向（傳入或傳出）。</param>
    /// <param name="action">規則動作（阻擋或允許）。</param>
    /// <param name="remoteAddresses">遠端 IP 位址或 CIDR 集合。</param>
    internal void AddShardedRulesBatch(string name, int port, NET_FW_RULE_DIRECTION direction,
        NET_FW_ACTION action, IReadOnlyList<string> remoteAddresses)
    {
        if (remoteAddresses == null || remoteAddresses.Count == 0) return;

        string baseRuleName = GetRuleName(name, port);
        List<INetFwRule> existingShards = [];
        foreach (INetFwRule r in FindRules(baseRuleName))
        {
            string rName = FirewallComString.Get(r.Name);
            if (rName == baseRuleName || rName.StartsWith(baseRuleName + "_", StringComparison.Ordinal))
            {
                existingShards.Add(r);
            }
        }

        HashSet<string> existingAddresses = new(StringComparer.OrdinalIgnoreCase);
        List<(INetFwRule Shard, List<string> Addresses)> shardLists = [];
        foreach (INetFwRule shard in existingShards)
        {
            string existing = FirewallComString.Get(shard.RemoteAddresses);
            List<string> list = existing.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            foreach (string a in list)
            {
                existingAddresses.Add(a);
            }
            shardLists.Add((shard, list));
        }

        List<string> pendingToAdd = [];
        foreach (string candidate in remoteAddresses)
        {
            if (!existingAddresses.Contains(candidate))
            {
                pendingToAdd.Add(candidate);
                existingAddresses.Add(candidate);
            }
        }
        if (pendingToAdd.Count == 0) return;

        int pendingIndex = 0;
        foreach (var (shard, list) in shardLists)
        {
            int capacity = MaxAddressesPerRule - list.Count;
            if (capacity > 0 && pendingIndex < pendingToAdd.Count)
            {
                int take = Math.Min(capacity, pendingToAdd.Count - pendingIndex);
                list.AddRange(pendingToAdd.GetRange(pendingIndex, take));
                pendingIndex += take;

                shard.Action = action;
                shard.Direction = direction;
                shard.Protocol = 256;
                shard.Enabled = true;
                FirewallComString.Set(string.Join(",", list), val => shard.RemoteAddresses = val);
            }
        }

        int shardIndex = existingShards.Count;
        while (pendingIndex < pendingToAdd.Count)
        {
            int count = Math.Min(MaxAddressesPerRule, pendingToAdd.Count - pendingIndex);
            List<string> chunk = pendingToAdd.GetRange(pendingIndex, count);
            pendingIndex += count;

            string newRuleName = shardIndex == 0 ? baseRuleName : $"{baseRuleName}_{shardIndex}";
            shardIndex++;

            INetFwRule newRule = CreateComObject<INetFwRule>("HNetCfg.FWRule");
            newRule.Action = action;
            FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, value => newRule.Grouping = value);
            newRule.Protocol = 256;
            FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME + " rule", value => newRule.Description = value);
            newRule.Direction = direction;
            newRule.Enabled = true;

            if (port > 0)
                FirewallComString.Set(port.ToString(), value => newRule.LocalPorts = value);
            FirewallComString.Set(newRuleName, value => newRule.Name = value);
            FirewallComString.Set(string.Join(",", chunk), value => newRule.RemoteAddresses = value);
            firewallPolicyManager.Rules.Add(newRule);
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
            FirewallComString.Set(FirewallComString.Get(rule.Name), firewallPolicyManager.Rules.Remove);
        }
    }

    /// <summary>
    /// Gets rule.
    /// </summary>
    /// <param name="name">name 的值。</param>
    /// <returns>傳回 get rule 的結果。</returns>
    internal INetFwRule? GetRule(string name)
    {
        try
        {
            return FirewallComString.Call(name, bstr => firewallPolicyManager.Rules.Item(bstr));
        }
        catch (System.IO.FileNotFoundException)
        {
            return null;
        }
        catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.HResult == 0x80070002 || (uint)ex.HResult == 0x80070643)
        {
            return null;
        }
        catch (Exception)
        {
            foreach (INetFwRule rule in (dynamic)firewallPolicyManager.Rules)
            {
                if (FirewallComString.Get(rule.Name) == name) return rule;
            }
            return null;
        }
    }
    /// <summary>
    /// Finds rules.
    /// </summary>
    /// <param name="name">name 的值。</param>
    /// <returns>傳回 find rules 的結果。</returns>
    internal List<INetFwRule> FindRules(string name)
    {
        List<INetFwRule> rules = [];
        foreach (INetFwRule rule in (dynamic)firewallPolicyManager.Rules)
        {
            string candidate = FirewallComString.Get(rule.Name);
            bool matches = candidate.Equals(name, StringComparison.Ordinal)
                || (name.EndsWith('_')
                    ? candidate.StartsWith(name, StringComparison.Ordinal)
                    : candidate.StartsWith(name + "_", StringComparison.Ordinal));
            if (matches)
                rules.Add(rule);
        }
        return rules;
    }
    /// <summary>
    /// 正規化 IP 位址，並排除與安全網路重疊的封鎖範圍。
    /// </summary>
    /// <param name="addresses">原始 IP 位址集合。</param>
    /// <param name="safeNetworks">全域白名單 / 安全網路 IP 與 CIDR 集合。</param>
    /// <param name="subnetThreshold">觸發 C 段聚合的 IP 數量門檻。</param>
    /// <returns>傳回經過 CIDR 聚合與白名單保護後的位址清單。</returns>
    internal static List<string> AggregateIpAddresses(IEnumerable<string> addresses, IEnumerable<string>? safeNetworks = null, int subnetThreshold = 5)
    {
        _ = subnetThreshold;
        List<string> safe = [];
        foreach (string value in safeNetworks ?? [])
        {
            string? normalized = NormalizeRemoteAddressEntry(value.Trim());
            if (normalized is not null)
                safe.Add(normalized);
        }

        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (string value in addresses)
        {
            string? normalized = NormalizeRemoteAddressEntry(value.Trim());
            if (normalized is null)
                continue;
            List<string> remaining = [normalized];
            foreach (string safeNetwork in safe)
            {
                List<string> next = [];
                foreach (string entry in remaining)
                    next.AddRange(ExceptAddress(entry, safeNetwork));
                remaining = next;
            }
            result.UnionWith(remaining);
        }
        return result.Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 定義由 IDDS 社群版所建立之傳入放行規則名稱前綴。
    /// </summary>
    internal const string INBOUND_ALLOW_RULE_PREFIX = "IDDSCommunity_Allow_";

    /// <summary>
    /// 產生傳入放行規則之標準化名稱。
    /// </summary>
    /// <param name="featureKey">功能識別碼。</param>
    /// <param name="protocol">通訊協定。</param>
    /// <param name="port">通訊埠號。</param>
    /// <returns>傳回標準化之規則名稱字串。</returns>
    internal static string GetInboundAllowRuleName(string featureKey, string protocol, int port) =>
        $"{INBOUND_ALLOW_RULE_PREFIX}{featureKey}_{protocol.ToUpperInvariant()}_{port}";

    /// <inheritdoc />
    public void ReconcileInboundAllowRules(
        IReadOnlyCollection<FirewallInboundRuleDefinition> targetRules,
        Action<string, string, string, string?>? auditRecorder = null)
    {
        ArgumentNullException.ThrowIfNull(targetRules);
        lock (_firewallLock)
        {
            try
            {
                Dictionary<string, FirewallInboundRuleDefinition> expectedRules = new(StringComparer.OrdinalIgnoreCase);
                foreach (FirewallInboundRuleDefinition tr in targetRules)
                {
                    string name = GetInboundAllowRuleName(tr.FeatureKey, tr.Protocol, tr.Port);
                    expectedRules[name] = tr;
                }

                List<INetFwRule> existingManagedRules = FindRules(INBOUND_ALLOW_RULE_PREFIX);
                Dictionary<string, INetFwRule> existingByName = new(StringComparer.OrdinalIgnoreCase);

                foreach (INetFwRule existing in existingManagedRules)
                {
                    string existingName = FirewallComString.Get(existing.Name);
                    existingByName.TryAdd(existingName, existing);
                    if (!expectedRules.ContainsKey(existingName))
                    {
                        try
                        {
                            FirewallComString.Set(existingName, firewallPolicyManager.Rules.Remove);
                            logManager.WriteEntry(
                                $"Removed stale firewall inbound allow rule: {existingName}",
                                System.Diagnostics.EventLogEntryType.Information,
                                Globals.IDDSCOMMUNITY_EVENT_ID_FIREWALL_RULE_ALTERED,
                                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                            auditRecorder?.Invoke("Firewall.RuleRemove", "Succeeded", existingName, "Removed obsolete inbound allow rule");
                        }
                        catch (Exception ex)
                        {
                            logManager.WriteEntry(
                                $"Failed to remove firewall rule {existingName}: {ex.Message}",
                                System.Diagnostics.EventLogEntryType.Warning,
                                Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL,
                                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                            auditRecorder?.Invoke("Firewall.RuleRemove", "Failed", existingName, ex.Message);
                        }
                    }
                }

                foreach (var (ruleName, def) in expectedRules)
                {
                    if (!existingByName.TryGetValue(ruleName, out INetFwRule? existing))
                    {
                        try
                        {
                            INetFwRule newRule = CreateComObject<INetFwRule>("HNetCfg.FWRule");
                            FirewallComString.Set(ruleName, value => newRule.Name = value);
                            FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, value => newRule.Grouping = value);
                            FirewallComString.Set(def.Description, value => newRule.Description = value);
                            newRule.Direction = NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN;
                            newRule.Action = NET_FW_ACTION.NET_FW_ACTION_ALLOW;
                            newRule.Protocol = def.Protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase) ? 17 : 6;
                            FirewallComString.Set(def.Port.ToString(), value => newRule.LocalPorts = value);
                            newRule.Profiles = (int)NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL;
                            newRule.Enabled = true;

                            firewallPolicyManager.Rules.Add(newRule);

                            logManager.WriteEntry(
                                $"Created firewall inbound allow rule: {ruleName} ({def.Protocol} {def.Port})",
                                System.Diagnostics.EventLogEntryType.Information,
                                Globals.IDDSCOMMUNITY_EVENT_ID_FIREWALL_RULE_CREATED,
                                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                            auditRecorder?.Invoke("Firewall.RuleAdd", "Succeeded", $"{def.FeatureKey} ({def.Protocol} {def.Port})", $"Created inbound allow rule: {ruleName}");
                        }
                        catch (Exception ex)
                        {
                            logManager.WriteEntry(
                                $"Failed to create firewall rule {ruleName}: {ex.Message}",
                                System.Diagnostics.EventLogEntryType.Error,
                                Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL,
                                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                            auditRecorder?.Invoke("Firewall.RuleAdd", "Failed", $"{def.FeatureKey} ({def.Protocol} {def.Port})", ex.Message);
                        }
                    }
                    else
                    {
                        int protocol = def.Protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase) ? 17 : 6;
                        string port = def.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (!FirewallComString.Get(existing.Description).Equals(def.Description, StringComparison.Ordinal))
                            FirewallComString.Set(def.Description, value => existing.Description = value);
                        if (!FirewallComString.Get(existing.Grouping).Equals(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, StringComparison.Ordinal))
                            FirewallComString.Set(Globals.IDDSCOMMUNITY_WINDOWS_IDS_GROUP_NAME, value => existing.Grouping = value);
                        if (existing.Direction != NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN)
                            existing.Direction = NET_FW_RULE_DIRECTION.NET_FW_RULE_DIR_IN;
                        if (existing.Action != NET_FW_ACTION.NET_FW_ACTION_ALLOW)
                            existing.Action = NET_FW_ACTION.NET_FW_ACTION_ALLOW;
                        if (existing.Protocol != protocol)
                            existing.Protocol = protocol;
                        if (!FirewallComString.Get(existing.LocalPorts).Equals(port, StringComparison.Ordinal))
                            FirewallComString.Set(port, value => existing.LocalPorts = value);
                        if (existing.Profiles != (int)NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL)
                            existing.Profiles = (int)NET_FW_PROFILE_TYPE2.NET_FW_PROFILE2_ALL;
                        if (!existing.Enabled)
                            existing.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.WriteEntry(
                    $"ReconcileInboundAllowRules encountered an error: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error,
                    Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL,
                    Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            }
        }
    }

    /// <inheritdoc />
    public void RemoveAllInboundAllowRules(Action<string, string, string, string?>? auditRecorder = null)
    {
        lock (_firewallLock)
        {
            try
            {
                List<INetFwRule> rules = FindRules(INBOUND_ALLOW_RULE_PREFIX);
                foreach (INetFwRule rule in rules)
                {
                    string ruleName = FirewallComString.Get(rule.Name);
                    try
                    {
                        FirewallComString.Set(ruleName, firewallPolicyManager.Rules.Remove);
                        logManager.WriteEntry(
                            $"Cleaned up firewall inbound allow rule: {ruleName}",
                            System.Diagnostics.EventLogEntryType.Information,
                            Globals.IDDSCOMMUNITY_EVENT_ID_FIREWALL_RULE_ALTERED,
                            Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                        auditRecorder?.Invoke("Firewall.RuleRemove", "Succeeded", ruleName, "Cleaned up inbound allow rule on service shutdown");
                    }
                    catch (Exception ex)
                    {
                        logManager.WriteEntry(
                            $"Failed to remove firewall rule {ruleName}: {ex.Message}",
                            System.Diagnostics.EventLogEntryType.Warning,
                            Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL,
                            Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.WriteEntry(
                    $"RemoveAllInboundAllowRules encountered an error: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Warning,
                    Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL,
                    Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            }
        }
    }

    /// <summary>
    /// Releases the COM firewall policy object owned by this manager.
    /// </summary>
    public void Dispose()
    {
        lock (_firewallLock)
        {
            if (System.Runtime.InteropServices.Marshal.IsComObject(firewallPolicyManager))
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(firewallPolicyManager);
        }
    }

}
#pragma warning restore CA1416
