using System;
using System.Collections.Generic;
using System.Text;
using NetFwTypeLib;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class FirewallPolicyManager : IFirewallPolicy, IDisposable
{
    private readonly INetFwPolicy2 firewallPolicyManager;
    private readonly IRuntimeLog logManager;
    private static FirewallPolicyManager? _instance;

    internal static FirewallPolicyManager Instance
    {
        get
        {
            _instance ??= new FirewallPolicyManager(WindowsLogManager.Instance);
            return _instance;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallPolicyManager"/> class.
    /// </summary>

    internal FirewallPolicyManager(IRuntimeLog logManager)
    {
        ArgumentNullException.ThrowIfNull(logManager);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Windows Firewall integration requires Windows."));
        this.logManager = logManager;
        firewallPolicyManager = CreateComObject<INetFwPolicy2>("HNetCfg.FwPolicy2");
    }

    /// <summary>
    /// Creates com object.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="progId">The prog id value.</param>
    /// <returns>The create com object result.</returns>

    private static T CreateComObject<T>(string progId) where T : class =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException(string.Format(Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("COM type {0} is unavailable."), progId))) as T
        ?? throw new InvalidOperationException(string.Format(Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Unable to create COM object {0}."), progId));

    /// <summary>
    /// Executes the block operation.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>

    public void Block(string ipAddress)
    {
        try
        {
            AddRule("BlockAttacker", 0, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY,
                NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN, NET_FW_SCOPE_.NET_FW_SCOPE_CUSTOM,
                NET_FW_ACTION_.NET_FW_ACTION_BLOCK, ipAddress);
        }
        catch (Exception ex)
        {
            logManager.WriteEntry("Create Firewall Rule: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                Globals.CYBERARMS_EVENT_ID_INVALID_FUNCTION_CALL, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
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
            INetFwRule? rule = GetRule(GetRuleName("BlockAttacker", 0));
            if (rule is null)
                return false;
            return ContainsAddress(rule.RemoteAddresses, ipAddress);
        }
        catch (Exception ex)
        {
            logManager.WriteEntry("IsLocked encountered an error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error,
                Globals.CYBERARMS_EVENT_ID_INVALID_FUNCTION_CALL, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }

    /// <summary>
    /// Returns the exact addresses currently present in the Cyberarms block rule.
    /// </summary>
    /// <returns>The normalized firewall address entries.</returns>
    public IReadOnlyCollection<string> GetBlockedAddresses()
    {
        INetFwRule? rule = GetRule(GetRuleName("BlockAttacker", 0));
        if (rule is null || !rule.Enabled)
            return [];
        List<string> addresses = [];
        foreach (string entry in rule.RemoteAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (System.Net.IPAddress.TryParse(entry, out System.Net.IPAddress? address))
                addresses.Add(address.ToString());
        }
        return addresses;
    }

    /// <summary>
    /// Removes ip address from block list.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>

    public void RemoveIpAddressFromBlockList(string ipAddress)
    {
        string ruleName = GetRuleName("BlockAttacker", 0);
        INetFwRule? rule = GetRule(ruleName);
        if (rule is null)
            throw new ArgumentException(string.Format(Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Firewall rule {0} was not found."), ruleName), nameof(ipAddress));
        if (!ContainsAddress(rule.RemoteAddresses, ipAddress))
        {
            throw new ArgumentException(string.Format(
                "The IP address {0} is not blocked and might has been automatically removed by schedule. Please refresh the list to view current locks.", ipAddress));
        }
        rule.RemoteAddresses = GetCleanedRemoteAddresses(rule.RemoteAddresses, ipAddress);
        if (rule.RemoteAddresses == "*" || string.IsNullOrEmpty(rule.RemoteAddresses.Replace(',', ' ').Trim()))
        {
            rule.Enabled = false;
        }
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

    private static string GetRuleName(string name, int port) => string.Format("{0}_{1}_{2}", Globals.CYBERARMS_WINDOWS_IDS_RULE_NAME, name, port == 0 ? "AllPorts" : port.ToString());

    /// <summary>
    /// Adds rule.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="port">The port value.</param>
    /// <param name="protocol">The protocol value.</param>
    /// <param name="direction">The direction value.</param>
    /// <param name="scope">The scope value.</param>
    /// <param name="action">The action value.</param>
    /// <param name="remoteAddress">The remote address value.</param>

    internal void AddRule(string name, int port, NET_FW_IP_PROTOCOL_ protocol, NET_FW_RULE_DIRECTION_ direction,
        NET_FW_SCOPE_ scope, NET_FW_ACTION_ action, string remoteAddress)
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
            throw new ArgumentOutOfRangeException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("IP address must be given in IP version 4 or IP version 6 format!"));
        }
        // ipAddress = String.Format("{0}/255.255.255.255", ipAddress);

        if (!ruleExists)
        {
            rule.Action = action;
            rule.Grouping = Globals.CYBERARMS_WINDOWS_IDS_GROUP_NAME;
            rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            rule.Description = Globals.CYBERARMS_WINDOWS_IDS_GROUP_NAME + " rule";
            rule.Direction = direction;
            rule.Enabled = true;

            if (port > 0) rule.LocalPorts = port.ToString();
            rule.Name = ruleName;
            rule.RemoteAddresses = ipAddress;
            //  rule.RemotePorts = "";
            firewallPolicyManager.Rules.Add(rule);
        }
        else
        {
            rule.Enabled = true;
            if (rule.RemoteAddresses.Trim().Equals("*"))
            {
                rule.RemoteAddresses = ipAddress;
            }
            else
            {
                rule.RemoteAddresses = string.Format("{0},{1}", rule.RemoteAddresses, ipAddress);
            }
        }
    }

    /// <summary>
    /// Clears up rules.
    /// </summary>

    internal void CleanUpRules()
    {
        foreach (INetFwRule rule in FindRules(Globals.CYBERARMS_WINDOWS_IDS_RULE_NAME))
        {
            //rule.RemoteAddresses = "";
            firewallPolicyManager.Rules.Remove(rule.Name);
        }
    }

    /// <summary>
    /// Gets rule.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The get rule result.</returns>

    internal INetFwRule? GetRule(string name)
    {
        foreach (INetFwRule rule in firewallPolicyManager.Rules)
        {
            if (rule.Name == name) return rule;
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
        foreach (INetFwRule rule in firewallPolicyManager.Rules)
        {
            if (rule.Name.StartsWith(name)) rules.Add(rule);
        }
        return rules;
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
