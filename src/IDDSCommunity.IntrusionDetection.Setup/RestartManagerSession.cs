using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IDDSCommunity.IntrusionDetection.Setup;

internal sealed class RestartManagerSession : IDisposable
{
    private const int ErrorMoreData = 234;
    private const int SessionKeyLength = 32;
    private uint sessionHandle;
    private bool disposed;

    private RestartManagerSession(uint sessionHandle)
    {
        this.sessionHandle = sessionHandle;
    }

    internal static RestartManagerSession CreateForDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        StringBuilder sessionKey = new(SessionKeyLength + 1);
        int result = RmStartSession(out uint handle, 0, sessionKey);
        ThrowIfFailed(result, nameof(RmStartSession));
        RestartManagerSession session = new(handle);
        try
        {
            string[] files = Directory.Exists(directoryPath)
                ? Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToArray()
                : [];
            result = RmRegisterResources(handle, checked((uint)files.Length), files, 0, null, 0, null);
            ThrowIfFailed(result, nameof(RmRegisterResources));
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    internal IReadOnlyList<AffectedApplication> GetAffectedApplications()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        uint needed = 0;
        uint count = 0;
        uint rebootReasons;
        int result = RmGetList(sessionHandle, out needed, ref count, null, out rebootReasons);
        if (result == 0) return [];
        if (result != ErrorMoreData) ThrowIfFailed(result, nameof(RmGetList));

        RmProcessInfo[] processes = new RmProcessInfo[needed];
        count = needed;
        result = RmGetList(sessionHandle, out needed, ref count, processes, out rebootReasons);
        ThrowIfFailed(result, nameof(RmGetList));
        return processes.Take(checked((int)count))
            .Select(process => new AffectedApplication(
                process.ApplicationName,
                checked((int)process.Process.ProcessId),
                process.ServiceShortName,
                process.Restartable,
                (ApplicationStatus)process.ApplicationStatus))
            .ToArray();
    }

    internal void ShutdownApplications()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ThrowIfFailed(RmShutdown(sessionHandle, 0, IntPtr.Zero), nameof(RmShutdown));
    }

    internal static string FormatAffectedApplications(IEnumerable<AffectedApplication> applications) =>
        string.Join(", ", applications.Select(application =>
        {
            string name = !string.IsNullOrWhiteSpace(application.ApplicationName)
                ? application.ApplicationName
                : application.ServiceShortName;
            if (string.IsNullOrWhiteSpace(name)) name = SetupText.Get("UnknownApplication");
            return $"{name} (PID {application.ProcessId})";
        }));

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        uint handle = sessionHandle;
        sessionHandle = 0;
        if (handle != 0) _ = RmEndSession(handle);
    }

    private static void ThrowIfFailed(int errorCode, string operation)
    {
        if (errorCode != 0)
            throw new Win32Exception(errorCode, SetupText.Format("RestartManagerFailed", operation, errorCode));
    }

    internal readonly record struct AffectedApplication(
        string ApplicationName,
        int ProcessId,
        string ServiceShortName,
        bool Restartable,
        ApplicationStatus Status)
    {
        internal bool IsStopped =>
            (Status & (ApplicationStatus.Stopped | ApplicationStatus.StoppedOther)) != 0 &&
            (Status & (ApplicationStatus.Running | ApplicationStatus.Restarted |
                ApplicationStatus.ErrorOnStop | ApplicationStatus.ShutdownMasked)) == 0;
    }

    [Flags]
    internal enum ApplicationStatus : uint
    {
        Unknown = 0x0,
        Running = 0x1,
        Stopped = 0x2,
        StoppedOther = 0x4,
        Restarted = 0x8,
        ErrorOnStop = 0x10,
        ErrorOnRestart = 0x20,
        ShutdownMasked = 0x40,
        RestartMasked = 0x80
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RmUniqueProcess
    {
        internal uint ProcessId;
        internal System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RmProcessInfo
    {
        internal RmUniqueProcess Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string ApplicationName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        internal string ServiceShortName;
        internal uint ApplicationType;
        internal uint ApplicationStatus;
        internal uint TerminalServicesSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        internal bool Restartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint sessionHandle, int sessionFlags, StringBuilder sessionKey);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(
        uint sessionHandle,
        uint fileCount,
        string[] fileNames,
        uint applicationCount,
        RmUniqueProcess[]? applications,
        uint serviceCount,
        string[]? serviceNames);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmGetList(
        uint sessionHandle,
        out uint processInfoNeeded,
        ref uint processInfoCount,
        [In, Out] RmProcessInfo[]? affectedApplications,
        out uint rebootReasons);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmShutdown(uint sessionHandle, uint actionFlags, IntPtr statusCallback);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint sessionHandle);
}
