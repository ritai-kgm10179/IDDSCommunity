using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service;
/// <summary>
/// Reconciles durable desired locks with the IDDSCommunity Windows Firewall rule.
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
        HashSet<string> currentBlocked = new(firewallPolicy.GetBlockedAddresses(), StringComparer.OrdinalIgnoreCase);

        List<Lock> missingLocks = [];
        foreach (Lock desiredLock in desiredLocks)
        {
            desiredAddresses.Add(desiredLock.IpAddress);
            if (!currentBlocked.Contains(desiredLock.IpAddress))
            {
                missingLocks.Add(desiredLock);
            }
        }

        if (missingLocks.Count > 0)
        {
            try
            {
                firewallPolicy.BatchBlock(missingLocks.ConvertAll(l => l.IpAddress));
                foreach (Lock missing in missingLocks)
                {
                    currentBlocked.Add(missing.IpAddress);
                }
            }
            catch (Exception ex)
            {
                foreach (Lock missing in missingLocks)
                {
                    recordAudit("Firewall.Reconcile", "Failed", missing.IpAddress, ex.GetType().Name);
                    reportFailure(missing.IpAddress, ex);
                }
            }
        }

        foreach (Lock desiredLock in desiredLocks)
        {
            if (!currentBlocked.Contains(desiredLock.IpAddress))
                continue;

            try
            {
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

        List<string> staleAddresses = [];
        foreach (string actualAddress in currentBlocked)
        {
            if (!desiredAddresses.Contains(actualAddress))
            {
                staleAddresses.Add(actualAddress);
            }
        }

        if (staleAddresses.Count > 0)
        {
            try
            {
                firewallPolicy.BatchRemove(staleAddresses);
                foreach (string actualAddress in staleAddresses)
                {
                    recordAudit("Firewall.Reconcile", "Succeeded", actualAddress, "RemoveStale");
                }
            }
            catch (Exception ex)
            {
                foreach (string actualAddress in staleAddresses)
                {
                    recordAudit("Firewall.Reconcile", "Failed", actualAddress, ex.GetType().Name);
                    reportFailure(actualAddress, ex);
                }
            }
        }
    }
}
