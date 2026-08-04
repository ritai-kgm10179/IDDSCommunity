using System;
using NetFwTypeLib;

namespace Cyberarms.IntrusionDetection.Service;

internal class FirewallManager
{
    private static FirewallManager? _instance;
    private readonly INetFwMgr firewallManager;
    internal static FirewallManager Instance
    {
        get
        {
            _instance ??= new FirewallManager();
            return _instance;

        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallManager"/> class.
    /// </summary>

    private FirewallManager() => firewallManager = CreateComObject<INetFwMgr>("HNetCfg.FwMgr");

    /// <summary>
    /// Creates com object.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="progId">The prog id value.</param>
    /// <returns>The create com object result.</returns>

    private static T CreateComObject<T>(string progId) where T : class =>
        Activator.CreateInstance(Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException(string.Format(Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("COM type {0} is unavailable."), progId))) as T
        ?? throw new InvalidOperationException(string.Format(Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Unable to create COM object {0}."), progId));

    /// <summary>
    /// Adds port.
    /// </summary>
    /// <param name="strName">The str name value.</param>
    /// <param name="Port">The port value.</param>
    /// <param name="Scope">The scope value.</param>
    /// <param name="Protocol">The protocol value.</param>
    /// <param name="remoteAddresses">The remote addresses value.</param>

    internal void AddPort(string strName,
                               int Port,
                               NET_FW_SCOPE_ Scope,
                               NET_FW_IP_PROTOCOL_ Protocol,
                               string remoteAddresses)
    {
        var fireWallPort = CreateComObject<INetFwOpenPort>("HNetCfg.FWOpenPort");
        fireWallPort.RemoteAddresses = remoteAddresses;
        fireWallPort.Enabled = true;
        fireWallPort.Name = strName;
        fireWallPort.Port = Port;
        fireWallPort.Protocol = Protocol;

        firewallManager.LocalPolicy.CurrentProfile
                                   .GloballyOpenPorts.Add(fireWallPort);
    }



    /// <summary>
    /// Removes port.
    /// </summary>
    /// <param name="Port">The port value.</param>
    /// <param name="Protocol">The protocol value.</param>

    internal void RemovePort(int Port,
                                  NET_FW_IP_PROTOCOL_ Protocol)
    {
        firewallManager.LocalPolicy.CurrentProfile
           .GloballyOpenPorts.Remove(Port, Protocol);
    }

    /// <summary>
    /// Adds authorized application.
    /// </summary>
    /// <param name="strName">The str name value.</param>
    /// <param name="processImageFileName">The process image file name value.</param>
    /// <param name="Scope">The scope value.</param>

    internal void AddAuthorizedApplication(string strName,
                                            string processImageFileName,
                                            NET_FW_SCOPE_ Scope)
    {
        var authorizedApplication = CreateComObject<INetFwAuthorizedApplication>("HNetCfg.FwAuthorizedApplication");
        authorizedApplication.Name = strName;
        authorizedApplication.Scope = Scope;
        authorizedApplication.Enabled = true;
        authorizedApplication.ProcessImageFileName = processImageFileName;
        firewallManager.LocalPolicy.CurrentProfile
                       .AuthorizedApplications.Add(authorizedApplication);
    }

    /// <summary>
    /// Removes authorized application.
    /// </summary>
    /// <param name="processFileName">The process file name value.</param>

    internal void RemoveAuthorizedApplication(string processFileName)
    {
        firewallManager.LocalPolicy.CurrentProfile
                       .AuthorizedApplications.Remove(processFileName);
    }

    /// <summary>
    /// Reads port.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The read port result.</returns>

    internal INetFwOpenPort? ReadPort(string name)
    {
        INetFwOpenPorts ports = firewallManager.LocalPolicy.CurrentProfile.GloballyOpenPorts;
        foreach (INetFwOpenPort port in ports)
        {
            System.Diagnostics.Debug.Print(port.Name);
            if (port.Name == name) return port;
        }
        return null;

    }

}
