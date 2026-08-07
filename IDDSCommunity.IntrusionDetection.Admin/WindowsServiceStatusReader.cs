using System;
using System.ComponentModel;
using System.ServiceProcess;

namespace IDDSCommunity.IntrusionDetection.Admin;
/// <summary>
/// Converts expected Windows service discovery failures into an explicit unavailable result.
/// </summary>
internal static class WindowsServiceStatusReader
{
    /// <summary>
    /// Executes one service-status read without allowing a missing or inaccessible service to escape a timer callback.
    /// </summary>
    /// <param name="readStatus">The status operation, including any required controller refresh.</param>
    /// <param name="status">The observed service status when the operation succeeds.</param>
    /// <param name="failure">The expected discovery failure when the operation cannot read the service.</param>
    /// <returns><see langword="true"/> when a status was read; otherwise, <see langword="false"/>.</returns>
    internal static bool TryRead(
        Func<ServiceControllerStatus> readStatus,
        out ServiceControllerStatus status,
        out Exception? failure)
    {
        ArgumentNullException.ThrowIfNull(readStatus);
        try
        {
            status = readStatus();
            failure = null;
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            status = default;
            failure = ex;
            return false;
        }
    }
}
