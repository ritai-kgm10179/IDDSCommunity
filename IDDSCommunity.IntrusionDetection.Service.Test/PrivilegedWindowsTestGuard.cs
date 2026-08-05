using System;
using System.Security.Principal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

internal static class PrivilegedWindowsTestGuard
{
    internal const string Category = "PrivilegedWindows";

    /// <summary>
    /// Requires explicit opt-in and an elevated Windows process before a test can mutate platform state.
    /// </summary>
    internal static void RequireOptInAndAdministrator()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("IDDSCOMMUNITY_RUN_PRIVILEGED_TESTS"), "1", StringComparison.Ordinal))
            Assert.Inconclusive("Set IDDSCOMMUNITY_RUN_PRIVILEGED_TESTS=1 to run privileged Windows integration tests.");

        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            Assert.Inconclusive("Privileged Windows integration tests require an elevated process.");
    }
}
