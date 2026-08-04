using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Cyberarms.IntrusionDetection.Api.Plugin;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace Cyberarms.IntrusionDetection.Service;

public sealed class Service : IIntrusionDetectionRuntime, IDisposable
{
    private readonly IFirewallPolicy firewallPolicy;
    private readonly DatabaseOptions databaseOptions;
    private readonly PluginOptions pluginOptions;
    private readonly ReportOptions reportOptions;
    private readonly ProtectionOptions protectionOptions;
    private readonly System.Threading.SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly Database database;
    private readonly IddsConfig configuration;
    private readonly NotificationSettings notificationSettings;
    private readonly SecurityAgents securityAgents;
    private readonly ReportScheduler reportScheduler;
    private readonly Statistics statistics;
    private readonly ProtectionAuditTrail protectionAuditTrail;
    private readonly IRuntimeLog logManager;
    private SecurityEventPipeline? securityEventPipeline;

    internal event EventHandler ClientIpAddressSoftLocked;
    internal event EventHandler ClientIpAddressUnlocked;
    internal event EventHandler ClientIpAddressHardLocked;


    // private LogAlerts logAlerts;
    private readonly System.Timers.Timer cleanupTimer = new();


    // private bool restartPending = false;
    // private System.Timers.Timer restartTimer = new System.Timers.Timer(2000);
    private bool isInitialized;
    private bool agentsLoaded;
    private bool agentsStarted;
    private bool reportingStarted;
    private bool runtimeStarted;
    private bool disposed;

    /// <summary>
    /// Initializes a service with an explicit firewall policy implementation.
    /// </summary>
    /// <param name="firewallPolicy">The firewall operations used for address blocking.</param>
    /// <param name="databaseOptions">The validated database settings.</param>
    /// <param name="pluginOptions">The validated plug-in settings.</param>
    /// <param name="reportOptions">The validated report scheduler settings.</param>
    /// <param name="protectionOptions">The validated protection evidence settings.</param>
    /// <param name="database">The runtime database.</param>
    /// <param name="configuration">The runtime configuration.</param>
    /// <param name="notificationSettings">The notification settings.</param>
    /// <param name="securityAgents">The managed security agents.</param>
    /// <param name="reportScheduler">The report scheduler.</param>
    /// <param name="statistics">The attack statistics service.</param>
    /// <param name="protectionAuditTrail">The persistent protection-control audit trail.</param>
    /// <param name="logManager">The Windows runtime logger.</param>
    internal Service(
        IFirewallPolicy firewallPolicy,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<PluginOptions> pluginOptions,
        IOptions<ReportOptions> reportOptions,
        IOptions<ProtectionOptions> protectionOptions,
        Database database,
        IddsConfig configuration,
        NotificationSettings notificationSettings,
        SecurityAgents securityAgents,
        ReportScheduler reportScheduler,
        Statistics statistics,
        ProtectionAuditTrail protectionAuditTrail,
        IRuntimeLog logManager)
    {
        ArgumentNullException.ThrowIfNull(firewallPolicy);
        ArgumentNullException.ThrowIfNull(databaseOptions);
        ArgumentNullException.ThrowIfNull(pluginOptions);
        ArgumentNullException.ThrowIfNull(reportOptions);
        ArgumentNullException.ThrowIfNull(protectionOptions);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(notificationSettings);
        ArgumentNullException.ThrowIfNull(securityAgents);
        ArgumentNullException.ThrowIfNull(reportScheduler);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(protectionAuditTrail);
        ArgumentNullException.ThrowIfNull(logManager);
        this.firewallPolicy = firewallPolicy;
        this.databaseOptions = databaseOptions.Value;
        this.pluginOptions = pluginOptions.Value;
        this.reportOptions = reportOptions.Value;
        this.protectionOptions = protectionOptions.Value;
        this.database = database;
        this.configuration = configuration;
        this.notificationSettings = notificationSettings;
        this.securityAgents = securityAgents;
        this.reportScheduler = reportScheduler;
        this.statistics = statistics;
        this.protectionAuditTrail = protectionAuditTrail;
        this.logManager = logManager;
        isInitialized = false;
        ClientIpAddressSoftLocked += new EventHandler(Service_ClientIpAddressSoftLocked);
        ClientIpAddressUnlocked += new EventHandler(Service_ClientIpAddressUnlocked);
        ClientIpAddressHardLocked += new EventHandler(Service_ClientIpAddressHardLocked);
        // IntrusionDetectionConfiguration.PluginDirectory = System.Windows.Forms.Application.StartupPath + "\\Plugins\\";
        reportScheduler.RunDailyReportAsync += Instance_RunDailyReportAsync;
        reportScheduler.RunWeeklyReportAsync += Instance_RunWeeklyReportAsync;
        reportScheduler.RunMonthlyReportAsync += Instance_RunMonthlyReportAsync;
        // Configuration.Instance.ConfigurationChanged += new EventHandler(Instance_ConfigurationChanged);

    }

    /// <summary>
    /// Handles the run monthly report event.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report delivery.</param>
    /// <returns>A task that completes after the report is delivered.</returns>

    async Task Instance_RunMonthlyReportAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            DateTime end = DateTime.Now.AddDays(-1);
            DateTime start = new(end.Year, end.Month, 1, 0, 0, 0);
            string hostName = System.Net.Dns.GetHostName();
            string report = ReportGenerator.Instance.GetReport(Strings.Get("Monthly report"), Strings.Format("Report for {0:Y}", start), Strings.Format("Server: {0}", hostName),
                start, new DateTime(end.Year, end.Month, end.Day, 23, 59, 59));
            await SendMailAsync(Strings.Format("Monthly report for {0}", hostName), report, true, cancellationToken, true).ConfigureAwait(false);
            TryRecordAudit("Report.Monthly", "Succeeded", hostName);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Report.Monthly", "Failed", System.Net.Dns.GetHostName(), ex.GetType().Name);
            logManager.WriteEntry(ex.Message, EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_CONFIGURATION_ERROR, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }

    /// <summary>
    /// Handles the run weekly report event.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report delivery.</param>
    /// <returns>A task that completes after the report is delivered.</returns>

    async Task Instance_RunWeeklyReportAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            DateTime end = DateTime.Now.AddDays(-1);
            DateTime start = end.AddDays(-6);
            string hostName = System.Net.Dns.GetHostName();
            string report = ReportGenerator.Instance.GetReport(Strings.Get("Weekly report"), Strings.Format("Week of {0:d}", start), Strings.Format("Server: {0}", hostName),
                new DateTime(start.Year, start.Month, start.Day, 0, 0, 0), new DateTime(end.Year, end.Month, end.Day, 23, 59, 59));
            await SendMailAsync(Strings.Format("Weekly report for {0}", hostName), report, true, cancellationToken, true).ConfigureAwait(false);
            TryRecordAudit("Report.Weekly", "Succeeded", hostName);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Report.Weekly", "Failed", System.Net.Dns.GetHostName(), ex.GetType().Name);
            logManager.WriteEntry(ex.Message, EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_CONFIGURATION_ERROR, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }

    /// <summary>
    /// Handles the run daily report event.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report delivery.</param>
    /// <returns>A task that completes after the report is delivered.</returns>

    async Task Instance_RunDailyReportAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            DateTime d = DateTime.Now.AddDays(-1);
            string hostName = System.Net.Dns.GetHostName();
            string report = ReportGenerator.Instance.GetReport(Strings.Get("Daily report"), d.ToString("d", LanguageManager.Instance.CurrentCulture), Strings.Format("Server: {0}", hostName),
                new DateTime(d.Year, d.Month, d.Day, 0, 0, 0), new DateTime(d.Year, d.Month, d.Day, 23, 59, 59));
            await SendMailAsync(Strings.Format("Daily report for {0}", hostName), report, true, cancellationToken, true).ConfigureAwait(false);
            TryRecordAudit("Report.Daily", "Succeeded", hostName);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Report.Daily", "Failed", System.Net.Dns.GetHostName(), ex.GetType().Name);
            logManager.WriteEntry(ex.Message, EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_CONFIGURATION_ERROR, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }


    /// <summary>
    /// Configures system.
    /// </summary>

    void ConfigureSystem()
    {
        database.Configure(System.Windows.Forms.Application.StartupPath, databaseOptions.FileName);

        configuration.ApplicationPath = System.Windows.Forms.Application.StartupPath;
        configuration.PluginsDirectory = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, pluginOptions.DirectoryName) + System.IO.Path.DirectorySeparatorChar;
        configuration.Load();
        reportScheduler.CheckInterval = TimeSpan.FromMinutes(reportOptions.CheckIntervalMinutes);
        securityAgents.InitializeAgents();
        securityAgents.RegisterSecurityAgents();
    }

    //void Instance_ConfigurationChanged(object sender, EventArgs e) {
    //    restartPending = true;
    //    restartTimer.Enabled = true;
    //}

    /// <summary>
    /// Handles the client ip address hard locked event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void Service_ClientIpAddressHardLocked(object? sender, EventArgs e)
    {
        if (sender is not ClientOperationInformation op)
            return;
        IntrusionLog.AddEntry(DateTime.Now, op.AgentId, op.IpAddress, IntrusionLog.STATUS_HARD_LOCKED, false);
        SendInfoMail(op, LockType.HardLock);
    }

    /// <summary>
    /// Handles the client ip address unlocked event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void Service_ClientIpAddressUnlocked(object? sender, EventArgs e)
    {
        if (sender is not ClientOperationInformation op)
            return;
        if (op.HasError)
        {
            IntrusionLog.AddEntry(DateTime.Now, IntrusionLog.GetSystemId(), op.IpAddress, IntrusionLog.STATUS_UNLOCK_ERROR, false);
        }
        else
        {
            IntrusionLog.AddEntry(DateTime.Now, IntrusionLog.GetSystemId(), op.IpAddress, IntrusionLog.STATUS_UNLOCKED, false);
        }
        SendInfoMail(op, LockType.None);
    }

    /// <summary>
    /// Handles the client ip address soft locked event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void Service_ClientIpAddressSoftLocked(object? sender, EventArgs e)
    {
        if (sender is not ClientOperationInformation op)
            return;
        IntrusionLog.AddEntry(DateTime.Now, op.AgentId, op.IpAddress, IntrusionLog.STATUS_SOFT_LOCKED, false);
        SendInfoMail(op, LockType.SoftLock);
    }

    /// <summary>
    /// Processes the client ip address hard locked notification.
    /// </summary>
    /// <param name="lockItem">The lock item value.</param>
    /// <param name="ex">The exception associated with the operation.</param>
    /// <param name="agentId">The agent id value.</param>

    void OnClientIpAddressHardLocked(Lock lockItem, Exception? ex, Guid agentId)
    {
        if (ClientIpAddressHardLocked != null)
        {
            ClientOperationInformation co = GetClientOperationInformation(lockItem.IpAddress, ex, "hard");
            co.AgentId = agentId;
            ClientIpAddressHardLocked(co, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets client operation information.
    /// </summary>
    /// <param name="ipAddress">The ip address value.</param>
    /// <param name="ex">The exception associated with the operation.</param>
    /// <param name="info">The info value.</param>
    /// <returns>The get client operation information result.</returns>

    private static ClientOperationInformation GetClientOperationInformation(string ipAddress, Exception? ex, string info)
    {
        ClientOperationInformation op = new()
        {
            IpAddress = ipAddress,
            Exception = ex
        };
        if (ex != null)
        {
            op.HasError = true;
            op.Message = "Error while trying to " + info + " lock client with IP address " + ipAddress + ":\r\n" + ex.Message;
        }
        else
        {
            op.Message = "Client with IP address " + ipAddress + " was " + info + " locked";
        }
        return op;
    }

    /// <summary>
    /// Processes the client ip address soft locked notification.
    /// </summary>
    /// <param name="lockItem">The lock item value.</param>
    /// <param name="ex">The exception associated with the operation.</param>
    /// <param name="agentId">The agent id value.</param>

    void OnClientIpAddressSoftLocked(Lock lockItem, Exception? ex, Guid agentId)
    {
        if (ClientIpAddressSoftLocked != null)
        {
            ClientOperationInformation co = GetClientOperationInformation(lockItem.IpAddress, ex, "soft");
            co.AgentId = agentId;
            ClientIpAddressSoftLocked(co, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Processes the client ip address unlocked notification.
    /// </summary>
    /// <param name="lockItem">The lock item value.</param>
    /// <param name="ex">The exception associated with the operation.</param>

    void OnClientIpAddressUnlocked(Lock lockItem, Exception? ex)
    {
        if (ClientIpAddressUnlocked != null)
        {
            ClientOperationInformation op = new()
            {
                IpAddress = lockItem.IpAddress,
                Exception = ex,
                AgentId = IntrusionLog.GetSystemId()
            };
            if (ex != null)
            {
                op.HasError = true;
                op.Message = "Error while unlocking " + lockItem.IpAddress + ":\r\n" + ex.Message;
            }
            else
            {
                op.Message = "Client with IP address " + lockItem.IpAddress + " was unlocked";
            }
            ClientIpAddressUnlocked(op, EventArgs.Empty);
        }
    }


    /// <summary>
    /// Sends info mail.
    /// </summary>
    /// <param name="o">The o value.</param>
    /// <param name="lockOperation">The lock operation value.</param>

    void SendInfoMail(object o, LockType lockOperation)
    {
        if (o == null || !(o is ClientOperationInformation)) return;
        var op = (ClientOperationInformation)o;
        try
        {
            string subject = string.Empty;
            switch (lockOperation)
            {
                case LockType.None:
                    if (!notificationSettings.OnUnlock) return;
                    subject = "Cyberarms IDDS: Unlock notification (" + op.IpAddress + ")";
                    break;
                case LockType.SoftLock:
                    if (!notificationSettings.OnSoftLock) return;
                    subject = "Cyberarms IDDS: Soft lock notification (" + op.IpAddress + ")";
                    break;
                case LockType.HardLock:
                    if (!notificationSettings.OnHardLock) return;
                    subject = "Cyberarms IDDS: Hard lock notification (" + op.IpAddress + ")";
                    break;
            }
            _ = SendMailAsync(subject, op.Message, false);
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(Strings.Get("Error while sending notification email.\r\n") + ex.Message,
                        EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_INVALID_FUNCTION_CALL, Globals.CYBERARMS_LOG_CATEGORY_PLUGIN);
        }
    }

    /// <summary>
    /// Sends mail.
    /// </summary>
    /// <param name="subject">The subject value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="isHtml"><see langword="true"/> when the trusted report body contains HTML; otherwise, <see langword="false"/>.</param>
    /// <param name="cancellationToken">Signals cancellation of SMTP delivery.</param>
    /// <param name="rethrowOnFailure"><see langword="true"/> to propagate delivery failures to the scheduler.</param>
    /// <returns>A task representing SMTP delivery.</returns>

    async Task SendMailAsync(string subject, string message, bool isHtml, System.Threading.CancellationToken cancellationToken = default, bool rethrowOnFailure = false)
    {
        try
        {
            if (string.IsNullOrEmpty(configuration.SmtpServer) || string.IsNullOrEmpty(configuration.SenderEmailAddress)
                || string.IsNullOrEmpty(configuration.NotificationEmailAddress))
            {
                if (rethrowOnFailure)
                    throw new InvalidOperationException(Strings.Get("SMTP configuration is incomplete."));
                return;
            }

            var mimeMessage = new MimeKit.MimeMessage();
            mimeMessage.From.Add(MimeKit.MailboxAddress.Parse(configuration.SenderEmailAddress));
            mimeMessage.To.Add(MimeKit.MailboxAddress.Parse(configuration.NotificationEmailAddress));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new MimeKit.TextPart(isHtml ? "html" : "plain") { Text = message };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            int port = configuration.SmtpPort == 0 ? 25 : configuration.SmtpPort;
            SecureSocketOptions secureOption = configuration.SmtpSslRequired ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto;
            using System.Threading.CancellationTokenSource timeout = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await client.ConnectAsync(configuration.SmtpServer, port, secureOption, timeout.Token).ConfigureAwait(false);

            if (configuration.SmtpRequiresAuthentication)
            {
                await client.AuthenticateAsync(configuration.SmtpUsername, configuration.GetSmtpPassword(), timeout.Token).ConfigureAwait(false);
            }
            await client.SendAsync(mimeMessage, timeout.Token).ConfigureAwait(false);
            await client.DisconnectAsync(true, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            string safeMessage = ex.Message.Replace('\r', ' ').Replace('\n', ' ');
            logManager.WriteEntry(Strings.Get("Error while sending notification email: ") + safeMessage,
                EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_INVALID_FUNCTION_CALL, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
            if (rethrowOnFailure)
                throw;
        }
    }



    /// <summary>
    /// Executes the init operation.
    /// </summary>

    private void Init()
    {

        cleanupTimer.Interval = 1000;
        cleanupTimer.Elapsed += new System.Timers.ElapsedEventHandler(cleanupTimer_Elapsed);
        // restartTimer.Elapsed += new System.Timers.ElapsedEventHandler(restartTimer_Elapsed);
        logManager.WriteEntry(Strings.Get("Intrusion Detection Service was initialized successfully."), EventLogEntryType.Information,
           Globals.CYBERARMS_EVENT_ID_INFORMATION, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
        isInitialized = true;
    }



    /// <summary>
    /// Handles the elapsed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void cleanupTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        List<Lock> timedOutLocks = Locks.GetUnlockList();
        foreach (Lock l in timedOutLocks)
        {
            l.Status = Lock.LOCK_STATUS_UNLOCKED;
            l.Save();
        }
        foreach (Lock l in timedOutLocks)
        {
            try
            {
                firewallPolicy.RemoveIpAddressFromBlockList(l.IpAddress);
                TryRecordAudit("Firewall.Unlock", "Succeeded", l.IpAddress);
                // IntrusionLog.AddEntry(DateTime.Now, Guid.Empty, l.IpAddress, IntrusionLog.STATUS_UNLOCK_REQUESTED, false);
                OnClientIpAddressUnlocked(l, null);
                //l.Save();
            }
            catch (Exception ex)
            {
                TryRecordAudit("Firewall.Unlock", "Failed", l.IpAddress, ex.GetType().Name);
                // IntrusionLog.AddEntry(DateTime.Now, Guid.Empty, l.IpAddress, IntrusionLog.STATUS_UNLOCK_ERROR, false);
                logManager.WriteEntry(string.Format("IP address {0} cannot be unlocked. Error details: {1}",
                    l.IpAddress, ex.Message),
                    EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_INVALID_FUNCTION_CALL, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
                if (firewallPolicy.IsLocked(l.IpAddress))
                {
                    l.Status = Lock.LOCK_STATUS_UNLOCK_ERROR;
                }
                else
                {
                    l.Status = Lock.LOCK_STATUS_UNLOCKED;
                }
                l.Save();
                OnClientIpAddressUnlocked(l, ex);
            }
            //if (l.UnlockDate < DateTime.Now.AddDays(-1) || (l.Status == Lock.LOCK_STATUS_LOCK_ERROR || l.Status == Lock.LOCK_STATUS_UNLOCK_ERROR)) {
            //    l.Status = Lock.LOCK_STATUS_HISTORY;
            //}
        }
    }


    public bool LimitMailSent { get; set; }

    /// <summary>
    /// Executes the lock down ip operation.
    /// </summary>
    /// <param name="lockItem">The lock item value.</param>
    /// <param name="lockType">The lock type value.</param>
    /// <param name="reportingAgent">The reporting agent value.</param>

    void LockDownIp(Lock lockItem, LockType lockType, SecurityAgent reportingAgent)
    {
        int locksForToday = Locks.Today();
        LimitMailSent = false;
        try
        {
            // TO DO: Hard Lock overrides Soft Lock!
            if (firewallPolicy.IsLocked(lockItem.IpAddress))
            {
                logManager.WriteEntry(Strings.Get("Received another request to lock IP address ") + lockItem.IpAddress +
                            ". This IP address is already locked.", EventLogEntryType.Information, Globals.CYBERARMS_EVENT_ID_INFORMATION,
                            Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
                return;
            }
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(Strings.Get("Intrusion Detection Service had an error:") + ex.Message, EventLogEntryType.Error,
                  Globals.CYBERARMS_EVENT_ID_CONFIGURATION_ERROR, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
        }
        logManager.WriteEntry(string.Format("{0} lock: Unsuccessful login attempts from ip address {1} exceeded threshold. Firewall rule is being created to block the address specified.",
            lockType == LockType.HardLock ? "Hard" : "Soft", lockItem.IpAddress), EventLogEntryType.FailureAudit, Globals.CYBERARMS_EVENT_ID_FIREWALL_RULE_CREATED,
                    Globals.CYBERARMS_LOG_CATEGORY_SECURITY);
        // lockItem.Id = Locks.CreateLock(lockItem);
        try
        {
            firewallPolicy.Block(lockItem.IpAddress);
            switch (lockType)
            {
                case LockType.SoftLock:
                    lockItem.Status = Lock.LOCK_STATUS_SOFTLOCK;
                    statistics.IncreaseSoftLockStatistics(reportingAgent);
                    break;
                case LockType.HardLock:
                    lockItem.Status = Lock.LOCK_STATUS_HARDLOCK;
                    statistics.IncreaseHardLockStatistics(reportingAgent);
                    break;
            }
        }
        catch
        {
            lockItem.Status = Lock.LOCK_STATUS_LOCK_ERROR;
        }
        switch (lockType)
        {
            case LockType.SoftLock:
                OnClientIpAddressSoftLocked(lockItem, null, reportingAgent.Id);
                break;
            case LockType.HardLock:
                OnClientIpAddressHardLocked(lockItem, null, reportingAgent.Id);
                break;
        }
        lockItem.Save();
        TryRecordAudit(
            lockType == LockType.HardLock ? "Firewall.HardLock" : "Firewall.SoftLock",
            lockItem.Status == Lock.LOCK_STATUS_LOCK_ERROR ? "Failed" : "Succeeded",
            lockItem.IpAddress,
            reportingAgent.Id.ToString());


    }



    /// <summary>
    /// Starts the complete intrusion-detection runtime and rolls back partial startup on failure.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of host startup.</param>
    /// <returns>A task that completes when every component has started.</returns>
    public async Task StartAsync(System.Threading.CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (runtimeStarted)
                return;

            cancellationToken.ThrowIfCancellationRequested();
            ConfigureSystem();
            await protectionAuditTrail.PurgeOlderThanAsync(TimeSpan.FromDays(protectionOptions.AuditRetentionDays), cancellationToken).ConfigureAwait(false);
            securityEventPipeline = new SecurityEventPipeline(
                protectionOptions.SecurityEventQueueCapacity,
                ProcessAttackDetected,
                LogSecurityEventFailure);
            if (!isInitialized) Init();
            InitAgentConfiguration();
            agentsLoaded = true;
            LoadAgents();
            agentsStarted = true;
            securityAgents.StartAgents();
            cleanupTimer.Enabled = true;
            reportingStarted = true;
            reportScheduler.StartReporting();
            runtimeStarted = true;
            TryRecordAudit("Runtime.Start", "Succeeded", Environment.MachineName);
            logManager.WriteEntry(Strings.Get("Intrusion Detection Service was started successfully."), EventLogEntryType.Information,
                Globals.CYBERARMS_EVENT_ID_INFORMATION, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Runtime.Start", "Failed", Environment.MachineName, ex.GetType().Name);
            StopComponents(throwOnFailure: false);
            try
            {
                logManager.WriteEntry(Strings.Get("Intrusion Detection Service had a startup error. Details:") + ex.Message, EventLogEntryType.Error,
                    Globals.CYBERARMS_EVENT_ID_CONFIGURATION_ERROR, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
            }
            catch (Exception)
            {
                // Preserve the original startup exception when Event Log is unavailable.
            }
            throw;
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Stops the runtime once and releases every component that completed startup.
    /// </summary>
    /// <param name="cancellationToken">Signals that graceful shutdown has exceeded its deadline.</param>
    /// <returns>A task that completes after shutdown.</returns>
    public async Task StopAsync(System.Threading.CancellationToken cancellationToken)
    {
        await lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            StopComponents(throwOnFailure: true);
        }
        finally
        {
            lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Stops started components in reverse order and optionally propagates shutdown failures.
    /// </summary>
    /// <param name="throwOnFailure"><see langword="true"/> to propagate one or more shutdown failures.</param>
    private void StopComponents(bool throwOnFailure)
    {
        List<Exception> failures = [];
        if (reportingStarted)
            TryStop(reportScheduler.StopReporting, failures);
        reportingStarted = false;

        cleanupTimer.Enabled = false;
        if (agentsStarted)
            TryStop(securityAgents.StopAgents, failures);
        agentsStarted = false;

        if (agentsLoaded)
            TryStop(UnloadAgents, failures);
        agentsLoaded = false;

        if (securityEventPipeline is not null)
        {
            securityEventPipeline.Complete();
            TryStop(() => securityEventPipeline.Completion.WaitAsync(TimeSpan.FromSeconds(protectionOptions.SecurityEventDrainTimeoutSeconds)).GetAwaiter().GetResult(), failures);
            securityEventPipeline = null;
        }
        bool wasStarted = runtimeStarted;
        runtimeStarted = false;

        if (wasStarted)
        {
            TryRecordAudit("Runtime.Stop", failures.Count == 0 ? "Succeeded" : "Failed", Environment.MachineName);
            TryStop(() => logManager.WriteEntry(Strings.Get("Intrusion Detection Service was stopped."), EventLogEntryType.Information,
                Globals.CYBERARMS_EVENT_ID_INFORMATION, Globals.CYBERARMS_LOG_CATEGORY_RUNTIME), failures);
        }

        if (throwOnFailure && failures.Count == 1)
            throw failures[0];
        if (throwOnFailure && failures.Count > 1)
            throw new AggregateException(failures);
    }

    /// <summary>
    /// Executes one shutdown action while preserving failures so later cleanup still runs.
    /// </summary>
    /// <param name="action">The shutdown action.</param>
    /// <param name="failures">The collection receiving any failure.</param>
    /// <summary>
    /// Writes operational evidence without allowing an audit-store outage to disable protection.
    /// </summary>
    /// <param name="eventType">The stable protection event type.</param>
    /// <param name="outcome">The stable outcome code.</param>
    /// <param name="subject">The protected resource or address.</param>
    /// <param name="details">Optional non-sensitive diagnostic details.</param>
    private void TryRecordAudit(string eventType, string outcome, string subject, string? details = null)
    {
        try
        {
            string actor = Environment.UserDomainName + "\\" + Environment.UserName;
            protectionAuditTrail.Record(eventType, outcome, actor, subject, details);
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(
                Strings.Format("Protection audit recording failed: {0}", ex.GetType().Name),
                EventLogEntryType.Error,
                Globals.CYBERARMS_EVENT_ID_CONFIGURATION_ERROR,
                Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
        }
    }

    private static void TryStop(Action action, List<Exception> failures)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            failures.Add(ex);
        }
    }

    /// <summary>
    /// Releases lifecycle resources and event subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        StopComponents(throwOnFailure: false);
        reportScheduler.RunDailyReportAsync -= Instance_RunDailyReportAsync;
        reportScheduler.RunWeeklyReportAsync -= Instance_RunWeeklyReportAsync;
        reportScheduler.RunMonthlyReportAsync -= Instance_RunMonthlyReportAsync;
        cleanupTimer.Dispose();
        lifecycleLock.Dispose();
        disposed = true;
    }

    /// <summary>
    /// Executes the init agent configuration operation.
    /// </summary>

    private void InitAgentConfiguration() => securityAgents.RegisterSecurityAgents();



    /// <summary>
    /// Handles the attack detected event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="notificationEventArgs">The event data.</param>

    void Service_AttackDetected(object sender, INotificationEventArgs notificationEventArgs)
    {
        SecurityEventPipeline? pipeline = securityEventPipeline;
        if (pipeline is not null && pipeline.Publish(sender, notificationEventArgs))
            return;
        logManager.WriteEntry(
            Strings.Get("Security event pipeline is stopping or unavailable; the event could not be accepted."),
            EventLogEntryType.Error,
            Globals.CYBERARMS_EVENT_ID_PLUGIN_ERROR,
            Globals.CYBERARMS_LOG_CATEGORY_PLUGIN);
    }

    /// <summary>
    /// Processes one accepted Agent detection on the dedicated protection consumer.
    /// </summary>
    /// <param name="sender">The reporting Agent.</param>
    /// <param name="notificationEventArgs">The detection information.</param>
    private void ProcessAttackDetected(object sender, INotificationEventArgs notificationEventArgs)
    {
        try
        {
            if (notificationEventArgs == null)
            {
                if (configuration.IsDebug)
                {
                    // the following error should just be thrown when running in debug mode.
                    throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Operation not supported. EventArgs must be passed as NotificationEventArgs"));
                }
                else
                {
                    // otherwise write to the log file
                    logManager.WriteEntry(Strings.Get("Plugin error: the lock delegate was called, but notificationEventArgs must not be null!"),
                        EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_INVALID_FUNCTION_CALL, Globals.CYBERARMS_LOG_CATEGORY_PLUGIN);
                    return;
                }
            }
            if (sender is not IAgentPlugin reportingPlugin)
                return;
            SecurityAgent? reportingAgent = securityAgents.FindByName(reportingPlugin.Configuration.AgentName);
            if (reportingAgent is null)
                return;
            long incidentId;
            if (IddsConfig.IsValidIpAddress(notificationEventArgs.IpAddress))
            {
                statistics.IncreaseFailedLoginStatistics(reportingAgent);
                if (System.Net.IPAddress.TryParse(notificationEventArgs.IpAddress, out System.Net.IPAddress? ipAddress) && configuration.IsIpAddressLocal(ipAddress))
                {
                    incidentId = IntrusionLog.AddEntry(notificationEventArgs.CreateDate, reportingAgent.Id, notificationEventArgs.IpAddress,
                        IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL, false);
                }
                else if (configuration.UseSafeNetworkList && configuration.IsInSafeNetwork(notificationEventArgs.IpAddress))
                {
                    incidentId = IntrusionLog.AddEntry(notificationEventArgs.CreateDate, reportingAgent.Id, notificationEventArgs.IpAddress,
                        IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE, false);
                }
                else
                {
                    incidentId = IntrusionLog.AddEntry(notificationEventArgs.CreateDate, reportingAgent.Id, notificationEventArgs.IpAddress,
                        IntrusionLog.STATUS_INTRUSION_ATTEMPT, false);

                    try
                    {
                        if (!Locks.LockExists(notificationEventArgs.IpAddress))
                        {
                            LockType lockType = reportingAgent.GetCurrentLockType(notificationEventArgs.IpAddress);
                            switch (lockType)
                            {
                                case LockType.SoftLockRequested:
                                    //IntrusionLog.AddEntry(notificationEventArgs.CreateDate, reportingAgent.Id,
                                    //    notificationEventArgs.IpAddress, IntrusionLog.STATUS_SOFT_LOCK_REQUESTED, false);
                                    LockDownIp(Locks.CreateLock(DateTime.Now, DateTime.Now.AddMinutes(configuration.GetSoftLockMinutes(reportingAgent)), incidentId, Lock.LOCK_STATUS_SOFTLOCK, 0, notificationEventArgs.IpAddress), LockType.SoftLock, reportingAgent);
                                    break;
                                case LockType.SoftLock:
                                    // already locked, ignore
                                    break;
                                case LockType.HardLockRequested:
                                    //IntrusionLog.AddEntry(notificationEventArgs.CreateDate, reportingAgent.Id,
                                    //    notificationEventArgs.IpAddress, IntrusionLog.STATUS_HARD_LOCK_REQUESTED, false);
                                    LockDownIp(Locks.CreateLock(DateTime.Now, DateTime.Now.AddHours(configuration.GetHardLockHours(reportingAgent)), incidentId, Lock.LOCK_STATUS_HARDLOCK, 0, notificationEventArgs.IpAddress), LockType.HardLock, reportingAgent);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logManager.WriteEntry(string.Format("Unrecoverable error: {0}",
                                ex.Message), EventLogEntryType.FailureAudit, Globals.CYBERARMS_EVENT_ID_PLUGIN_ERROR,
                                Globals.CYBERARMS_LOG_CATEGORY_RUNTIME);
                        // OnClientIpAddressSoftLocked(new Lock( new Client(notificationEventArgs.IpAddress), ex);
                    }
                }
            }
            else
            {
                return;
            }
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(string.Format("AttackDetected delegate invocation of {0} caused a problem. \r\nDetails:\r\n{1}", sender != null ? sender.GetType().Name : "unknown", ex.Message),
                EventLogEntryType.Error, Globals.CYBERARMS_EVENT_ID_PLUGIN_ERROR, Globals.CYBERARMS_LOG_CATEGORY_PLUGIN);
        }
    }

    /// <summary>
    /// Records an isolated security-event consumer failure.
    /// </summary>
    /// <param name="exception">The processing failure.</param>
    private void LogSecurityEventFailure(Exception exception) => logManager.WriteEntry(
        Strings.Format("Security event processing failed: {0}", exception.GetType().Name),
        EventLogEntryType.Error,
        Globals.CYBERARMS_EVENT_ID_PLUGIN_ERROR,
        Globals.CYBERARMS_LOG_CATEGORY_PLUGIN);



    /// <summary>
    /// Loads agents.
    /// </summary>

    private void LoadAgents()
    {
        securityAgents.LoadAgents();
        foreach (SecurityAgent agent in securityAgents.LoadedAgents.Keys)
        {
            AgentProxy agentPlugin = securityAgents.LoadedAgents[agent];
            if (agent.Enabled)
            {
                agentPlugin.AttackDetected += new AttackDetectedHandler(Service_AttackDetected);
            }
        }
    }


    /// <summary>
    /// Executes the unload agents operation.
    /// </summary>

    private void UnloadAgents() => securityAgents.UnloadAgents();

}
