using System;
using System.Collections.Generic;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Service;

/// <summary>
/// Reconciles durable desired locks with the Cyberarms Windows Firewall rule.
/// </summary>
internal sealed class FirewallStateReconciler(
    IFirewallPolicy firewallPolicy,
    Func<IReadOnlyList<Lock>> readDesiredLocks,
    Action<Lock> saveLock,
    Action<string, string, string, string?> recordAudit,
    Action<string, Exception> reportFailure)
{
    /// <summary>
    /// Adds missing desired addresses, finalizes pending locks, and removes stale firewall addresses.
    /// </summary>
    internal void Reconcile()
    {
        IReadOnlyList<Lock> desiredLocks = readDesiredLocks();
        HashSet<string> desiredAddresses = new(StringComparer.Ordinal);
        foreach (Lock desiredLock in desiredLocks)
        {
            desiredAddresses.Add(desiredLock.IpAddress);
            try
            {
                if (!firewallPolicy.IsLocked(desiredLock.IpAddress))
                    firewallPolicy.Block(desiredLock.IpAddress);
                if (desiredLock.Status == Lock.LOCK_STATUS_SOFTLOCK_REQUESTED)
                {
                    desiredLock.Status = Lock.LOCK_STATUS_SOFTLOCK;
                    saveLock(desiredLock);
                }
                else if (desiredLock.Status == Lock.LOCK_STATUS_HARDLOCK_REQUESTED)
                {
                    desiredLock.Status = Lock.LOCK_STATUS_HARDLOCK;
                    saveLock(desiredLock);
                }
                recordAudit("Firewall.Reconcile", "Succeeded", desiredLock.IpAddress, "AddOrVerify");
            }
            catch (Exception ex)
            {
                recordAudit("Firewall.Reconcile", "Failed", desiredLock.IpAddress, ex.GetType().Name);
                reportFailure(desiredLock.IpAddress, ex);
            }
        }
        foreach (string actualAddress in firewallPolicy.GetBlockedAddresses())
        {
            if (desiredAddresses.Contains(actualAddress))
                continue;
            try
            {
                firewallPolicy.RemoveIpAddressFromBlockList(actualAddress);
                recordAudit("Firewall.Reconcile", "Succeeded", actualAddress, "RemoveStale");
            }
            catch (Exception ex)
            {
                recordAudit("Firewall.Reconcile", "Failed", actualAddress, ex.GetType().Name);
                reportFailure(actualAddress, ex);
            }
        }
    }
}
