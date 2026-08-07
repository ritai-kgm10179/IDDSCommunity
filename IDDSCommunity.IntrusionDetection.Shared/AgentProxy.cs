using System;
using System.Collections.Generic;
using System.Threading;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using System.Timers;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class AgentProxy : MarshalByRefObject, IAgentPlugin
{
    private const int MaximumPerformanceRecords = 3600;
    public event AttackDetectedHandler? AttackDetected;

    private System.Timers.Timer? _watchdog;
    private TimeSpan _lastCpuTime = TimeSpan.Zero;
    private long _lastPackets;
    private readonly System.Threading.Lock _lock = new();

    private IAgentPlugin? _agent;
    private readonly AgentPluginLoadContext loadContext;
    private bool disposed;
    private int watchdogActive;
    /// <summary>
    /// 初始化 <see cref="AgentProxy"/> class的新執行個體。
    /// </summary>
    /// <param name="pluginRoot">The trusted plug-in directory.</param>
    /// <param name="assemblyFilename">assembly filename參數。</param>
    /// <param name="typeName">type name參數。</param>
    public AgentProxy(string pluginRoot, string assemblyFilename, string typeName)
    {
        string pluginPath = PluginPathValidator.Validate(pluginRoot, assemblyFilename);
        loadContext = new AgentPluginLoadContext(pluginPath);
        System.Reflection.Assembly assembly = loadContext.LoadFromAssemblyPath(pluginPath);
        Type pluginType = assembly.GetType(typeName, throwOnError: true)
            ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Unable to resolve the requested agent plugin type."));
        object? instance = Activator.CreateInstance(pluginType);
        _agent = instance as IAgentPlugin
            ?? throw new InvalidOperationException(string.Format(Localization.Strings.Get("Unable to create agent plugin '{0}' from '{1}'."), typeName, assemblyFilename));
        _agent.AttackDetected += agent_AttackDetected;
    }
    /// <summary>
    /// 處理 attack detected 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="data">The event data.</param>
    private void agent_AttackDetected(object sender, INotificationEventArgs data)
    {
        foreach (AttackDetectedHandler handler in AttackDetected?.GetInvocationList() ?? [])
        {
            try
            {
                handler(sender, data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(Localization.Strings.Format("AgentProxy AttackDetected subscriber failed: {0}", ex.GetType().Name));
            }
        }
    }
    /// <summary>
    /// Starts requested operation.
    /// </summary>
    public void Start() => GetAgent().Start();
    /// <summary>
    /// Stops requested operation.
    /// </summary>
    public void Stop() => GetAgent().Stop();
    /// <summary>
    /// 執行pause作業。
    /// </summary>
    public void Pause() => GetAgent().Pause();
    /// <summary>
    /// 執行continue作業。
    /// </summary>
    public void Continue() => GetAgent().Continue();
    /// <summary>
    /// Determines whether n pause.
    /// </summary>
    /// <returns>若n pause傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CanPause() => GetAgent().CanPause();
    /// <summary>
    /// Determines whether n continue.
    /// </summary>
    /// <returns>若n continue傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CanContinue() => GetAgent().CanContinue();

    public bool IsPaused
    {
        get => GetAgent().IsPaused;
        set => GetAgent().IsPaused = value;
    }

    public bool IsRunning => GetAgent().IsRunning;

    public IAgentConfiguration Configuration
    {
        get => GetAgent().Configuration;
        set => GetAgent().Configuration = value;
    }
    /// <summary>
    /// Gets memory usage.
    /// </summary>
    /// <returns>傳回get memory usage結果。</returns>
    public static long GetMemoryUsage() => AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
    /// <summary>
    /// Gets cpu time.
    /// </summary>
    /// <returns>傳回get cpu time結果。</returns>
    public static TimeSpan GetCpuTime() => AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
    /// <summary>
    /// 執行enable monitoring作業。
    /// </summary>
    public void EnableMonitoring()
    {
        _watchdog = new System.Timers.Timer { Interval = 1000 };
        _watchdog.Elapsed += watchdog_Elapsed;
        _lastCpuTime = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
        if (GetAgent() is INetworkListener netListener) _lastPackets = netListener.TotalPackets;
        _watchdog.Start();
        AppDomain.MonitoringIsEnabled = true;
    }

    public List<AgentPerformanceRecord> PerformanceRecords { get; set; } = [];
    /// <summary>
    /// 處理 elapsed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void watchdog_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (Interlocked.Exchange(ref watchdogActive, 1) != 0)
            return;
        try
        {
            TimeSpan currentCpuTime = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
            AgentPerformanceRecord rcd = new()
            {
                DateTime = DateTime.Now,
                MemoryValue = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize,
                CpuUsage = currentCpuTime.Subtract(_lastCpuTime)
            };
            _lastCpuTime = currentCpuTime;
            if (GetAgent() is INetworkListener netListener)
            {
                long currentPackets = netListener.TotalPackets;
                rcd.Packets = currentPackets - _lastPackets;
                _lastPackets = currentPackets;
            }
            lock (_lock)
            {
                PerformanceRecords.Add(rcd);
                if (PerformanceRecords.Count > MaximumPerformanceRecords)
                    PerformanceRecords.RemoveRange(0, PerformanceRecords.Count - MaximumPerformanceRecords);
            }
        }
        catch (ObjectDisposedException)
        {
            // A queued timer callback may race with Agent unload.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(Localization.Strings.Format("Agent performance monitoring failed: {0}", ex.GetType().Name));
        }
        finally
        {
            Interlocked.Exchange(ref watchdogActive, 0);
        }
    }
    /// <summary>
    /// 執行disable monitoring作業。
    /// </summary>
    public void DisableMonitoring()
    {
        if (_watchdog is null)
            return;
        _watchdog.Stop();
        _watchdog.Elapsed -= watchdog_Elapsed;
        _watchdog.Dispose();
        _watchdog = null;
    }
    /// <summary>
    /// 執行dispose作業。
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        DisableMonitoring();
        if (_agent is not null)
            _agent.AttackDetected -= agent_AttackDetected;
        _agent = null;
        loadContext.Unload();
        disposed = true;
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// 取得 active plug-in instance or rejects access after unload.
    /// </summary>
    /// <returns>作用中的擴充元件實體。</returns>
    private IAgentPlugin GetAgent() => _agent ?? throw new ObjectDisposedException(nameof(AgentProxy));
}
