using System;
using System.Collections.Generic;
using System.Linq;
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
        Dictionary<string, Lock> desiredByAddress = new(StringComparer.OrdinalIgnoreCase);
        FirewallBlockState blockState = firewallPolicy.GetBlockState();
        HashSet<string> currentBlocked = new(blockState.EffectiveAddresses, StringComparer.OrdinalIgnoreCase);
        HashSet<string> currentAnyDirection = new(blockState.AnyDirectionAddresses, StringComparer.OrdinalIgnoreCase);

        foreach (Lock desiredLock in desiredLocks)
        {
            string? normalized = FirewallPolicyManager.NormalizeRemoteAddressEntry(desiredLock.IpAddress);
            if (normalized is not null)
                desiredByAddress[normalized] = desiredLock;
        }

        List<Lock> missingLocks = desiredByAddress
            .Where(pair => !currentBlocked.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToList();
        HashSet<string> missingAddresses = new(
            missingLocks.Select(lockEntry => FirewallPolicyManager.NormalizeRemoteAddressEntry(lockEntry.IpAddress))
                .OfType<string>(),
            StringComparer.OrdinalIgnoreCase);

        if (missingLocks.Count > 0)
        {
            try
            {
                firewallPolicy.BatchBlock(missingLocks.ConvertAll(l => l.IpAddress));
                blockState = firewallPolicy.GetBlockState();
                currentBlocked = new HashSet<string>(blockState.EffectiveAddresses, StringComparer.OrdinalIgnoreCase);
                currentAnyDirection = new HashSet<string>(blockState.AnyDirectionAddresses, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                blockState = firewallPolicy.GetBlockState();
                currentBlocked = new HashSet<string>(blockState.EffectiveAddresses, StringComparer.OrdinalIgnoreCase);
                currentAnyDirection = new HashSet<string>(blockState.AnyDirectionAddresses, StringComparer.OrdinalIgnoreCase);
                foreach (Lock missing in missingLocks)
                {
                    string? normalized = FirewallPolicyManager.NormalizeRemoteAddressEntry(missing.IpAddress);
                    if (normalized is not null && currentBlocked.Contains(normalized))
                        continue;
                    recordAudit("Firewall.Reconcile", "Failed", missing.IpAddress, ex.GetType().Name);
                    reportFailure(missing.IpAddress, ex);
                }
            }
        }

        foreach (var (address, desiredLock) in desiredByAddress)
        {
            if (!currentBlocked.Contains(address))
                continue;

            try
            {
                bool changed = false;
                if (desiredLock.Status == Lock.LOCK_STATUS_SOFTLOCK_REQUESTED)
                {
                    desiredLock.Status = Lock.LOCK_STATUS_SOFTLOCK;
                    saveLock(desiredLock);
                    changed = true;
                }
                else if (desiredLock.Status == Lock.LOCK_STATUS_HARDLOCK_REQUESTED)
                {
                    desiredLock.Status = Lock.LOCK_STATUS_HARDLOCK;
                    saveLock(desiredLock);
                    changed = true;
                }
                if (changed || missingAddresses.Contains(address))
                    recordAudit("Firewall.Reconcile", "Succeeded", address, "AddOrVerify");
            }
            catch (Exception ex)
            {
                recordAudit("Firewall.Reconcile", "Failed", desiredLock.IpAddress, ex.GetType().Name);
                reportFailure(desiredLock.IpAddress, ex);
            }
        }

        List<string> staleAddresses = [];
        foreach (string actualAddress in currentAnyDirection)
        {
            if (!desiredByAddress.ContainsKey(actualAddress))
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
