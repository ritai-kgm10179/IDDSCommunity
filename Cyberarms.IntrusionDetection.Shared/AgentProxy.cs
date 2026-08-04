using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cyberarms.IntrusionDetection.Api.Plugin;
using System.Timers;

namespace Cyberarms.IntrusionDetection.Shared;

public class AgentProxy : MarshalByRefObject, IAgentPlugin {
    public event AttackDetectedHandler? AttackDetected;

    private Timer? _watchdog;
    private TimeSpan _lastCpuTime = TimeSpan.Zero;
    private long _lastPackets;

    private readonly IAgentPlugin _agent;

    public AgentProxy(string assemblyFilename, string typeName) {
        _agent = (IAgentPlugin)Activator.CreateInstanceFrom(assemblyFilename, typeName).Unwrap();
        _agent.AttackDetected += agent_AttackDetected;
    }

    private void agent_AttackDetected(object sender, INotificationEventArgs data) => AttackDetected?.Invoke(sender, data);

    public void Start() => _agent.Start();
    public void Stop() => _agent.Stop();
    public void Pause() => _agent.Pause();
    public void Continue() => _agent.Continue();

    public bool CanPause() => _agent.CanPause();
    public bool CanContinue() => _agent.CanContinue();

    public bool IsPaused {
        get => _agent.IsPaused;
        set => _agent.IsPaused = value;
    }

    public bool IsRunning => _agent.IsRunning;

    public IAgentConfiguration Configuration {
        get => _agent.Configuration;
        set => _agent.Configuration = value;
    }

    public long GetMemoryUsage() => AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;

    public TimeSpan GetCpuTime() => AppDomain.CurrentDomain.MonitoringTotalProcessorTime;

    public void EnableMonitoring() {
        _watchdog = new Timer { Interval = 1000 };
        _watchdog.Elapsed += watchdog_Elapsed;
        _lastCpuTime = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
        if (_agent is INetworkListener netListener) _lastPackets = netListener.TotalPackets;
        _watchdog.Start();
        AppDomain.MonitoringIsEnabled = true;
    }

    public List<AgentPerformanceRecord> PerformanceRecords { get; set; } = [];

    private void watchdog_Elapsed(object? sender, ElapsedEventArgs e) {
        AgentPerformanceRecord rcd = new() {
            DateTime = DateTime.Now,
            MemoryValue = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize,
            CpuUsage = AppDomain.CurrentDomain.MonitoringTotalProcessorTime.Subtract(_lastCpuTime)
        };
        if (_agent is INetworkListener netListener) rcd.Packets = netListener.TotalPackets - _lastPackets;
        PerformanceRecords.Add(rcd);
    }

    public void DisableMonitoring() => _watchdog = null;

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}
