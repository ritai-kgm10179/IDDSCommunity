namespace IDDSCommunity.IntrusionDetection.Service;

internal interface IFirewallPolicy
{
    void Block(string ipAddress);

    bool IsLocked(string ipAddress);

    System.Collections.Generic.IReadOnlyCollection<string> GetBlockedAddresses();

    void RemoveIpAddressFromBlockList(string ipAddress);
}
