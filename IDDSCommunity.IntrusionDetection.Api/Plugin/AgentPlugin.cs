using System;

namespace IDDSCommunity.IntrusionDetection.Api.Plugin;

/// <summary>
/// Base class for agents
/// </summary>
public class AgentPlugin : IAgentPlugin
{
    private readonly object lifecycleSync = new();
    private bool isPaused;
    private bool isRunning;
    public event AttackDetectedHandler? AttackDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentPlugin"/> class.
    /// </summary>

    public AgentPlugin() => IsPaused = false;

    /// <summary>
    /// Handles the on attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="data">The event data.</param>

    protected void OnAttackDetected(object sender, INotificationEventArgs data)
    {
        foreach (AttackDetectedHandler handler in AttackDetected?.GetInvocationList() ?? [])
        {
            try
            {
                handler(this, data);
            }
            catch (Exception ex)
            {
                try
                {
                    System.Diagnostics.EventLog.WriteEntry("IDDSCommunity.IntrusionDetection.Api.Plugin.AgentPlugin", ex.ToString());
                }
                catch (Exception logException)
                {
                    System.Diagnostics.Trace.TraceError("Agent event handler failed: {0}; event-log fallback failed: {1}", ex, logException);
                }
            }
        }
    }

    /// <summary>
    /// Starts requested operation.
    /// </summary>

    public void Start()
    {
        lock (lifecycleSync)
        {
            if (isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent is already running. Operation cancelled!"));
            OnStartAgent();
            isRunning = true;
            isPaused = false;
        }
    }

    /// <summary>
    /// Stops requested operation.
    /// </summary>

    public void Stop()
    {
        lock (lifecycleSync)
        {
            if (!isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent is not running."));
            OnStopAgent();
            isRunning = false;
            isPaused = false;
        }
    }

    /// <summary>
    /// Executes the pause operation.
    /// </summary>

    public void Pause()
    {
        lock (lifecycleSync)
        {
            if (isPaused || !isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent cannot be paused in this state"));
            OnPauseAgent();
            isPaused = true;
        }
    }

    /// <summary>
    /// Executes the continue operation.
    /// </summary>

    public void Continue()
    {
        lock (lifecycleSync)
        {
            if (!isPaused || !isRunning)
                throw new InvalidOperationException(Localization.Strings.Get("Agent must be in paused state"));
            OnContinueAgent();
            isPaused = false;
        }
    }

    /// <summary>
    /// Determines whether n pause.
    /// </summary>
    /// <returns><see langword="true"/> if n pause; otherwise, <see langword="false"/>.</returns>

    public bool CanPause()
    {
        lock (lifecycleSync)
            return !isPaused && isRunning;
    }
    /// <summary>
    /// Determines whether n continue.
    /// </summary>
    /// <returns><see langword="true"/> if n continue; otherwise, <see langword="false"/>.</returns>

    public bool CanContinue()
    {
        lock (lifecycleSync)
            return isPaused && isRunning;
    }

    public bool IsPaused
    {
        get
        {
            lock (lifecycleSync)
                return isPaused;
        }
        set
        {
            lock (lifecycleSync)
                isPaused = value;
        }
    }

    public virtual bool IsRunning
    {
        get
        {
            lock (lifecycleSync)
                return isRunning;
        }
    }

    private IAgentConfiguration? _configuration;
    public IAgentConfiguration Configuration
    {
        get => _configuration ??= new AgentConfigurationBase();
        set => _configuration = value;
    }

    /// <summary>
    /// Processes the start agent notification.
    /// </summary>

    protected virtual void OnStartAgent() { }
    /// <summary>
    /// Processes the pause agent notification.
    /// </summary>

    protected virtual void OnPauseAgent() { }
    /// <summary>
    /// Processes the stop agent notification.
    /// </summary>

    protected virtual void OnStopAgent() { }
    /// <summary>
    /// Processes the continue agent notification.
    /// </summary>

    protected virtual void OnContinueAgent() { }
}
