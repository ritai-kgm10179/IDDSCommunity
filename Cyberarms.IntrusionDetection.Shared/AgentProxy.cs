using System;
using System.Collections.Generic;
using System.Threading;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Timers;

namespace Cyberarms.IntrusionDetection.Shared;

public class AgentProxy : MarshalByRefObject, IAgentPlugin
{
    public event AttackDetectedHandler? AttackDetected;

    private System.Timers.Timer? _watchdog;
    private TimeSpan _lastCpuTime = TimeSpan.Zero;
    private long _lastPackets;
    private readonly System.Threading.Lock _lock = new();

    private readonly IAgentPlugin _agent;
    private readonly AgentPluginLoadContext loadContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentProxy"/> class.
    /// </summary>
    /// <param name="assemblyFilename">The assembly filename value.</param>
    /// <param name="typeName">The type name value.</param>

    public AgentProxy(string assemblyFilename, string typeName)
    {
        string pluginPath = System.IO.Path.GetFullPath(assemblyFilename);
        loadContext = new AgentPluginLoadContext(pluginPath);
        System.Reflection.Assembly assembly = loadContext.LoadFromAssemblyPath(pluginPath);
        Type pluginType = assembly.GetType(typeName, throwOnError: true)
            ?? throw new InvalidOperationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Unable to resolve the requested agent plugin type."));
        object? instance = Activator.CreateInstance(pluginType);
        _agent = instance as IAgentPlugin
            ?? throw new InvalidOperationException(string.Format(Localization.Strings.Get("Unable to create agent plugin '{0}' from '{1}'."), typeName, assemblyFilename));
        _agent.AttackDetected += agent_AttackDetected;
    }

    /// <summary>
    /// Handles the attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="data">The event data.</param>

    private void agent_AttackDetected(object sender, INotificationEventArgs data) => AttackDetected?.Invoke(sender, data);

    /// <summary>
    /// Starts requested operation.
    /// </summary>

    public void Start() => _agent.Start();
    /// <summary>
    /// Stops requested operation.
    /// </summary>

    public void Stop() => _agent.Stop();
    /// <summary>
    /// Executes the pause operation.
    /// </summary>

    public void Pause() => _agent.Pause();
    /// <summary>
    /// Executes the continue operation.
    /// </summary>

    public void Continue() => _agent.Continue();

    /// <summary>
    /// Determines whether n pause.
    /// </summary>
    /// <returns><see langword="true"/> if n pause; otherwise, <see langword="false"/>.</returns>

    public bool CanPause() => _agent.CanPause();
    /// <summary>
    /// Determines whether n continue.
    /// </summary>
    /// <returns><see langword="true"/> if n continue; otherwise, <see langword="false"/>.</returns>

    public bool CanContinue() => _agent.CanContinue();

    public bool IsPaused
    {
        get => _agent.IsPaused;
        set => _agent.IsPaused = value;
    }

    public bool IsRunning => _agent.IsRunning;

    public IAgentConfiguration Configuration
    {
        get => _agent.Configuration;
        set => _agent.Configuration = value;
    }

    /// <summary>
    /// Gets memory usage.
    /// </summary>
    /// <returns>The get memory usage result.</returns>

    public static long GetMemoryUsage() => AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;

    /// <summary>
    /// Gets cpu time.
    /// </summary>
    /// <returns>The get cpu time result.</returns>

    public static TimeSpan GetCpuTime() => AppDomain.CurrentDomain.MonitoringTotalProcessorTime;

    /// <summary>
    /// Executes the enable monitoring operation.
    /// </summary>

    public void EnableMonitoring()
    {
        _watchdog = new System.Timers.Timer { Interval = 1000 };
        _watchdog.Elapsed += watchdog_Elapsed;
        _lastCpuTime = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
        if (_agent is INetworkListener netListener) _lastPackets = netListener.TotalPackets;
        _watchdog.Start();
        AppDomain.MonitoringIsEnabled = true;
    }

    public List<AgentPerformanceRecord> PerformanceRecords { get; set; } = [];

    /// <summary>
    /// Handles the elapsed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void watchdog_Elapsed(object? sender, ElapsedEventArgs e)
    {
        AgentPerformanceRecord rcd = new()
        {
            DateTime = DateTime.Now,
            MemoryValue = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize,
            CpuUsage = AppDomain.CurrentDomain.MonitoringTotalProcessorTime.Subtract(_lastCpuTime)
        };
        if (_agent is INetworkListener netListener) rcd.Packets = netListener.TotalPackets - _lastPackets;
        lock (_lock)
        {
            PerformanceRecords.Add(rcd);
        }
    }

    /// <summary>
    /// Executes the disable monitoring operation.
    /// </summary>

    public void DisableMonitoring() => _watchdog = null;

    /// <summary>
    /// Executes the dispose operation.
    /// </summary>

    public void Dispose()
    {
        _agent.AttackDetected -= agent_AttackDetected;
        loadContext.Unload();
        GC.SuppressFinalize(this);
    }
}
