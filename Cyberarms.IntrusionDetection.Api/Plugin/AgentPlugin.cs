using System;

namespace Cyberarms.IntrusionDetection.Api.Plugin;

/// <summary>
/// Base class for agents
/// </summary>
public class AgentPlugin : IAgentPlugin
{
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
        if (AttackDetected is not null)
        {
            try
            {
                AttackDetected(this, data);
            }
            catch (Exception ex)
            {
                try
                {
                    System.Diagnostics.EventLog.WriteEntry("Cyberarms.IntrusionDetection.Api.Plugin.AgentPlugin", ex.Message);
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Starts requested operation.
    /// </summary>

    public void Start()
    {
        if (!IsRunning)
        {
            OnStartAgent();
            IsRunning = true;
        }
        else
        {
            throw new InvalidOperationException(Localization.Strings.Get("Agent is already running. Operation cancelled!"));
        }
    }

    /// <summary>
    /// Stops requested operation.
    /// </summary>

    public void Stop()
    {
        if (IsRunning)
        {
            OnStopAgent();
            IsRunning = false;
        }
        else
        {
            throw new InvalidOperationException(Localization.Strings.Get("Agent is not running."));
        }
    }

    /// <summary>
    /// Executes the pause operation.
    /// </summary>

    public void Pause()
    {
        if (CanPause())
        {
            OnPauseAgent();
            IsPaused = true;
        }
        else
        {
            throw new InvalidOperationException(Localization.Strings.Get("Agent cannot be paused in this state"));
        }
    }

    /// <summary>
    /// Executes the continue operation.
    /// </summary>

    public void Continue()
    {
        if (CanContinue())
        {
            OnContinueAgent();
            IsPaused = false;
        }
        else
        {
            throw new InvalidOperationException(Localization.Strings.Get("Agent must be in paused state"));
        }
    }

    /// <summary>
    /// Determines whether n pause.
    /// </summary>
    /// <returns><see langword="true"/> if n pause; otherwise, <see langword="false"/>.</returns>

    public bool CanPause() => !IsPaused && IsRunning;
    /// <summary>
    /// Determines whether n continue.
    /// </summary>
    /// <returns><see langword="true"/> if n continue; otherwise, <see langword="false"/>.</returns>

    public bool CanContinue() => IsPaused;

    public bool IsPaused { get; set; }
    public virtual bool IsRunning { get; private set; }

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
