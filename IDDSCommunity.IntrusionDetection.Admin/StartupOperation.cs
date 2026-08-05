using System;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// Ensures the synchronous application bootstrap runs once and exposes a terminal result.
/// </summary>
internal sealed class StartupOperation
{
    private int started;

    /// <summary>
    /// Gets whether the one-time startup operation completed successfully.
    /// </summary>
    internal bool Succeeded { get; private set; }

    /// <summary>
    /// Runs the startup operation once and captures a terminal failure for the splash screen.
    /// </summary>
    /// <param name="startup">The application initialization operation.</param>
    /// <param name="failure">The captured startup failure, if any.</param>
    /// <returns><see langword="true"/> for the caller that performed startup; otherwise, <see langword="false"/>.</returns>
    internal bool TryRun(Action startup, out Exception? failure)
    {
        ArgumentNullException.ThrowIfNull(startup);
        failure = null;
        if (Interlocked.Exchange(ref started, 1) != 0)
            return false;
        try
        {
            startup();
            Succeeded = true;
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        return true;
    }
}
