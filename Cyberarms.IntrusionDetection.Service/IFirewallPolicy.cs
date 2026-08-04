namespace Cyberarms.IntrusionDetection.Service;

internal interface IFirewallPolicy
{
    void Block(string ipAddress);

    bool IsLocked(string ipAddress);

    void RemoveIpAddressFromBlockList(string ipAddress);
}
