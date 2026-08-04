using System;
using System.Collections.Generic;
using System.Text;
using NetFwTypeLib;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service;

internal sealed class FirewallPolicyManager : IFirewallPolicy
{
    private readonly INetFwPolicy2 firewallPolicyManager;
    private static FirewallPolicyManager? _instance;

    internal static FirewallPolicyManager Instance
    {
        get
        {
            _instance ??= new FirewallPolicyManager();
            return _instance;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallPolicyManager"/> class.
    /// </summary>

    private FirewallPolicyManager() => firewallPolicyManager = CreateComObject<INetFwPolicy2>("HNetCfg.FwPolicy2");

    /// <summary>
    /// Creates com object.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="progId">The prog id value.</param>
    /// <returns>The create com object result.</returns>

    private static T CreateComObject<T>(string progId) where T : class =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException($"COM type {progId} is unavailable.")) as T
        ?? throw new InvalidOperationException($"Unable to create COM object {progId}.");

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
            System.Diagnostics.EventLog.WriteEntry("Create Firewall Rule", ex.Message, System.Diagnostics.EventLogEntryType.Error);
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
            System.Diagnostics.EventLog.WriteEntry("IsLocked encountered an error: ", ex.Message, System.Diagnostics.EventLogEntryType.Error);
        }
        return false;
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
            throw new ArgumentException($"Firewall rule {ruleName} was not found.", nameof(ipAddress));
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
    /// <returns><see langword="true"/> when an exact address entry exists.</returns>
    internal static bool ContainsAddress(string addresses, string candidate)
    {
        if (!System.Net.IPAddress.TryParse(candidate.Trim(), out System.Net.IPAddress? expected))
            return false;
        foreach (string entry in addresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string host = entry.Split('/', 2, StringSplitOptions.TrimEntries)[0];
            if (System.Net.IPAddress.TryParse(host, out System.Net.IPAddress? parsed) && parsed.Equals(expected))
                return true;
        }
        return false;
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

}
