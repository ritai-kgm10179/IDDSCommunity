using System;
using System.Diagnostics;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Api.Plugin;

namespace IDDSCommunity.Agents.Authentication.Common;

public abstract class AuthenticationAgentBase<TConfiguration> : AgentPlugin, IExtendedInformation
    where TConfiguration : AuthenticationAgentConfiguration, new()
{
    private readonly IAuthenticationEventSource source;
    private AuthenticationThresholdDetector? detector;

    protected AuthenticationAgentBase(IAuthenticationEventSource source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        Configuration = new AgentConfigurationBase
        {
            AgentName = GetType().Name,
            ConfigurationSettingsTypeName = typeof(TConfiguration).FullName ?? string.Empty,
            AgentSettings = new TConfiguration()
        };
    }

    protected override void OnStartAgent()
    {
        TConfiguration configuration = GetConfiguration();
        configuration.Validate();
        detector = new AuthenticationThresholdDetector(configuration);
        source.EventReceived += OnEventReceived;
        source.Error += OnSourceError;
        try { source.Start(); }
        catch
        {
            source.EventReceived -= OnEventReceived;
            source.Error -= OnSourceError;
            detector = null;
            throw;
        }
    }

    protected override void OnPauseAgent() => source.Pause();
    protected override void OnContinueAgent() => source.Resume();

    protected override void OnStopAgent()
    {
        source.EventReceived -= OnEventReceived;
        source.Error -= OnSourceError;
        source.Stop();
        detector = null;
    }

    private void OnEventReceived(object? sender, AuthenticationFailureEvent failure)
    {
        if (detector?.Analyze(failure) != true)
            return;
        OnAttackDetected(this, new NotificationEventArgs
        {
            CreateDate = failure.OccurredAt.LocalDateTime,
            EventId = failure.EventId,
            IpAddress = failure.SourceAddress.ToString(),
            EventMessage = string.Format(System.Globalization.CultureInfo.CurrentCulture, IntrusionDetection.Api.Localization.Strings.Get("{0}: authentication failure threshold exceeded."), failure.Category)
        });
    }

    private void OnSourceError(Exception exception) => Trace.TraceError("{0}: {1}", DisplayName, exception.Message);
    protected TConfiguration GetConfiguration() => Configuration.AgentSettings as TConfiguration ?? throw new InvalidOperationException(IntrusionDetection.Api.Localization.Strings.Get("Agent configuration is unavailable."));

    protected abstract Color AgentColor { get; }
    public abstract string DisplayName { get; set; }
    public abstract Guid Id { get; }
    public Image? Icon { get => AgentIconFactory.Create(AgentColor, false); set { } }
    public Image? SelectedIcon { get => AgentIconFactory.Create(AgentColor, true); set { } }
    public Image? UnselectedIcon { get => AgentIconFactory.Create(AgentColor, false); set { } }
}
