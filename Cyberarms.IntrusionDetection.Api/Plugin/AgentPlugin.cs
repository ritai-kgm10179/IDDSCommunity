using System;

namespace Cyberarms.IntrusionDetection.Api.Plugin;

/// <summary>
/// Base class for agents
/// </summary>
public class AgentPlugin : IAgentPlugin
{
    public event AttackDetectedHandler? AttackDetected;

    public AgentPlugin() => IsPaused = false;

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

    public void Start()
    {
        if (!IsRunning)
        {
            OnStartAgent();
            IsRunning = true;
        }
        else
        {
            throw new InvalidOperationException("Agent is already running. Operation cancelled!");
        }
    }

    public void Stop()
    {
        if (IsRunning)
        {
            OnStopAgent();
            IsRunning = false;
        }
        else
        {
            throw new InvalidOperationException("Agent is not running.");
        }
    }

    public void Pause()
    {
        if (CanPause())
        {
            OnPauseAgent();
            IsPaused = true;
        }
        else
        {
            throw new InvalidOperationException("Agent cannot be paused in this state");
        }
    }

    public void Continue()
    {
        if (CanContinue())
        {
            OnContinueAgent();
            IsPaused = false;
        }
        else
        {
            throw new InvalidOperationException("Agent must be in paused state");
        }
    }

    public bool CanPause() => !IsPaused && IsRunning;
    public bool CanContinue() => IsPaused;

    public bool IsPaused { get; set; }
    public virtual bool IsRunning { get; private set; }

    private IAgentConfiguration? _configuration;
    public IAgentConfiguration Configuration
    {
        get => _configuration ??= new AgentConfigurationBase();
        set => _configuration = value;
    }

    protected virtual void OnStartAgent() { }
    protected virtual void OnPauseAgent() { }
    protected virtual void OnStopAgent() { }
    protected virtual void OnContinueAgent() { }
}
