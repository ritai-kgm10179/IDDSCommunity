using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using IDDSCommunity.IntrusionDetection.Service.Notifications;
using IDDSCommunity.IntrusionDetection.Service.Observability;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Correlation;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供入侵偵測、封包監聽、事件處理、防火牆封鎖與報表排程之核心 Windows 服務執行個體。
/// </summary>
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
    private readonly WebhookNotificationService webhookNotificationService;
    private readonly SyslogNotificationService syslogNotificationService;
    private readonly MetricsHttpServer metricsHttpServer;
    private readonly SecurityAgents securityAgents;
    private readonly ReportScheduler reportScheduler;
    private readonly Statistics statistics;
    private readonly ProtectionAuditTrail protectionAuditTrail;
    private readonly IRuntimeLog logManager;
    private CrossAgentCorrelationEngine crossAgentCorrelationEngine = new();
    private SecurityEventPipeline? securityEventPipeline;
    private DynamicDnsResolverService? dynamicDnsResolverService;
    private ThreatIntelligenceHubServer? threatHubServer;
    private ThreatIntelligenceSyncService? threatSyncService;
    private ExternalThreatFeedSubscriberService? externalThreatFeedSubscriberService;

    internal event EventHandler ClientIpAddressSoftLocked;
    internal event EventHandler ClientIpAddressUnlocked;
    internal event EventHandler ClientIpAddressHardLocked;


    // private LogAlerts logAlerts;
    private readonly System.Timers.Timer cleanupTimer = new();
    private readonly System.Timers.Timer maintenanceTimer = new();
    private int cleanupActive;
    private int maintenanceActive;


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
        this.webhookNotificationService = new WebhookNotificationService(notificationSettings);
        this.syslogNotificationService = new SyslogNotificationService(notificationSettings);
        this.metricsHttpServer = new MetricsHttpServer(notificationSettings, database);
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
    /// 處理 run monthly report 事件。
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report delivery.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    async Task Instance_RunMonthlyReportAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            DateTime end = DateTime.Today;
            DateTime lastReportedDay = end.AddDays(-1);
            DateTime start = new(lastReportedDay.Year, lastReportedDay.Month, 1, 0, 0, 0);
            string hostName = System.Net.Dns.GetHostName();
            // IncidentTime 以 UTC 儲存；標題與副標維持本機日期顯示，僅查詢邊界轉換為 UTC。
            string report = ReportGenerator.Instance.GetReport(Strings.Get("Monthly report"), Strings.Format("Report for {0:Y}", start), Strings.Format("Server: {0}", hostName),
                start.ToUniversalTime(), end.ToUniversalTime());
            await SendMailAsync(Strings.Format("Monthly report for {0}", hostName), report, true, cancellationToken, true).ConfigureAwait(false);
            TryRecordAudit("Report.Monthly", "Succeeded", hostName);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Report.Monthly", "Failed", System.Net.Dns.GetHostName(), ex.GetType().Name);
            logManager.WriteEntry(ex.Message, EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }
    /// <summary>
    /// 處理 run weekly report 事件。
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report delivery.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    async Task Instance_RunWeeklyReportAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            DateTime end = DateTime.Today;
            DateTime start = end.AddDays(-7);
            string hostName = System.Net.Dns.GetHostName();
            // IncidentTime 以 UTC 儲存；標題與副標維持本機日期顯示，僅查詢邊界轉換為 UTC。
            string report = ReportGenerator.Instance.GetReport(Strings.Get("Weekly report"), Strings.Format("Week of {0:d}", start), Strings.Format("Server: {0}", hostName),
                start.ToUniversalTime(), end.ToUniversalTime());
            await SendMailAsync(Strings.Format("Weekly report for {0}", hostName), report, true, cancellationToken, true).ConfigureAwait(false);
            TryRecordAudit("Report.Weekly", "Succeeded", hostName);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Report.Weekly", "Failed", System.Net.Dns.GetHostName(), ex.GetType().Name);
            logManager.WriteEntry(ex.Message, EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }
    /// <summary>
    /// 處理 run daily report 事件。
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of report delivery.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    async Task Instance_RunDailyReportAsync(System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            DateTime d = DateTime.Today.AddDays(-1);
            string hostName = System.Net.Dns.GetHostName();
            // IncidentTime 以 UTC 儲存；標題維持本機日期顯示，僅查詢邊界轉換為 UTC。
            string report = ReportGenerator.Instance.GetReport(Strings.Get("Daily report"), d.ToString("d", LanguageManager.Instance.CurrentCulture), Strings.Format("Server: {0}", hostName),
                d.ToUniversalTime(), d.AddDays(1).ToUniversalTime());
            await SendMailAsync(Strings.Format("Daily report for {0}", hostName), report, true, cancellationToken, true).ConfigureAwait(false);
            TryRecordAudit("Report.Daily", "Succeeded", hostName);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Report.Daily", "Failed", System.Net.Dns.GetHostName(), ex.GetType().Name);
            logManager.WriteEntry(ex.Message, EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            throw;
        }
    }

    /// <summary>
    /// Configures system.
    /// </summary>
    void ConfigureSystem()
    {
        database.Configure(IddsConfig.GetDefaultDataDirectory(), databaseOptions.FileName);

        configuration.ApplicationPath = System.Windows.Forms.Application.StartupPath;
        configuration.PluginsDirectory = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, pluginOptions.DirectoryName) + System.IO.Path.DirectorySeparatorChar;
        configuration.Load();
        reportScheduler.CheckInterval = TimeSpan.FromMinutes(reportOptions.CheckIntervalMinutes);
        securityAgents.InitializeAgents();
        securityAgents.RegisterSecurityAgents();

        if (configuration.EnableCrossAgentCorrelation)
        {
            TimeSpan correlationWindow = TimeSpan.FromMinutes(configuration.CrossAgentSlidingWindowMinutes);
            crossAgentCorrelationEngine = new CrossAgentCorrelationEngine(correlationWindow);
            crossAgentCorrelationEngine.RebuildFromDatabase(database, correlationWindow);
            RecoverPendingCorrelationObservations();
            SecurityObservationStore.DispatchPendingAlerts(database, protectionAuditTrail);
        }
        AuthenticationEventProcessingOptions.EnableRawEvents = configuration.EnableCrossAgentCorrelation;
    }

    //void Instance_ConfigurationChanged(object sender, EventArgs e) {
    //    restartPending = true;
    //    restartTimer.Enabled = true;
    //}
    /// <summary>
    /// 處理 client ip address hard locked 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void Service_ClientIpAddressHardLocked(object? sender, EventArgs e)
    {
        if (sender is not ClientOperationInformation op)
            return;
        IntrusionLog.AddEntry(DateTime.UtcNow, op.AgentId, op.IpAddress, IntrusionLog.STATUS_HARD_LOCKED, false);
        SendInfoMail(op, LockType.HardLock);
    }
    /// <summary>
    /// 處理 client ip address unlocked 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void Service_ClientIpAddressUnlocked(object? sender, EventArgs e)
    {
        if (sender is not ClientOperationInformation op)
            return;
        if (op.HasError)
        {
            IntrusionLog.AddEntry(DateTime.UtcNow, IntrusionLog.GetSystemId(), op.IpAddress, IntrusionLog.STATUS_UNLOCK_ERROR, false);
        }
        else
        {
            IntrusionLog.AddEntry(DateTime.UtcNow, IntrusionLog.GetSystemId(), op.IpAddress, IntrusionLog.STATUS_UNLOCKED, false);
        }
        SendInfoMail(op, LockType.None);
    }
    /// <summary>
    /// 處理 client ip address soft locked 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void Service_ClientIpAddressSoftLocked(object? sender, EventArgs e)
    {
        if (sender is not ClientOperationInformation op)
            return;
        IntrusionLog.AddEntry(DateTime.UtcNow, op.AgentId, op.IpAddress, IntrusionLog.STATUS_SOFT_LOCKED, false);
        SendInfoMail(op, LockType.SoftLock);
    }
    /// <summary>
    /// Processes the client ip address hard locked notification.
    /// </summary>
    /// <param name="lockItem">lock item 的值。</param>
    /// <param name="ex">The exception associated with the operation.</param>
    /// <param name="agentId">agent id 的值。</param>
    void OnClientIpAddressHardLocked(Lock lockItem, Exception? ex, Guid agentId)
    {
        if (ClientIpAddressHardLocked != null)
        {
            ClientOperationInformation co = GetClientOperationInformation(lockItem.IpAddress, ex, LockType.HardLock);
            co.AgentId = agentId;
            ClientIpAddressHardLocked(co, EventArgs.Empty);
        }
    }
    /// <summary>
    /// Gets client operation information.
    /// </summary>
    /// <param name="ipAddress">ip address 的值。</param>
    /// <param name="ex">The exception associated with the operation.</param>
    /// <param name="lockType">lockType 的值。</param>
    /// <returns>傳回 get client operation information 的結果。</returns>
    private static ClientOperationInformation GetClientOperationInformation(string ipAddress, Exception? ex, LockType lockType)
    {
        ClientOperationInformation op = new()
        {
            IpAddress = ipAddress,
            Exception = ex
        };
        if (ex != null)
        {
            op.HasError = true;
            op.Message = lockType == LockType.HardLock
                ? Strings.Format("Error while trying to hard lock client with IP address {0}:\r\n{1}", ipAddress, ex.Message)
                : Strings.Format("Error while trying to soft lock client with IP address {0}:\r\n{1}", ipAddress, ex.Message);
        }
        else
        {
            op.Message = lockType == LockType.HardLock
                ? Strings.Format("Client with IP address {0} was hard locked.", ipAddress)
                : Strings.Format("Client with IP address {0} was soft locked.", ipAddress);
        }
        return op;
    }
    /// <summary>
    /// Processes the client ip address soft locked notification.
    /// </summary>
    /// <param name="lockItem">lock item 的值。</param>
    /// <param name="ex">The exception associated with the operation.</param>
    /// <param name="agentId">agent id 的值。</param>
    void OnClientIpAddressSoftLocked(Lock lockItem, Exception? ex, Guid agentId)
    {
        if (ClientIpAddressSoftLocked != null)
        {
            ClientOperationInformation co = GetClientOperationInformation(lockItem.IpAddress, ex, LockType.SoftLock);
            co.AgentId = agentId;
            ClientIpAddressSoftLocked(co, EventArgs.Empty);
        }
    }
    /// <summary>
    /// Processes the client ip address unlocked notification.
    /// </summary>
    /// <param name="lockItem">lock item 的值。</param>
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
                op.Message = Strings.Format("Error while unlocking client with IP address {0}:\r\n{1}", lockItem.IpAddress, ex.Message);
            }
            else
            {
                op.Message = Strings.Format("Client with IP address {0} was unlocked.", lockItem.IpAddress);
            }
            ClientIpAddressUnlocked(op, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Sends info mail.
    /// </summary>
    /// <param name="o">o 的值。</param>
    /// <param name="lockOperation">lock operation 的值。</param>
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
                    if (notificationSettings.OnUnlock)
                        subject = Strings.Format("IDDS Community: Unlock notification ({0})", op.IpAddress);
                    break;
                case LockType.SoftLock:
                    if (notificationSettings.OnSoftLock)
                        subject = Strings.Format("IDDS Community: Soft lock notification ({0})", op.IpAddress);
                    break;
                case LockType.HardLock:
                    if (notificationSettings.OnHardLock)
                        subject = Strings.Format("IDDS Community: Hard lock notification ({0})", op.IpAddress);
                    break;
            }
            if (!string.IsNullOrEmpty(subject))
            {
                _ = SendMailAsync(subject, op.Message, false);
            }

            string agentName = (op.AgentId != Guid.Empty && securityAgents != null)
                ? (securityAgents.Find(a => a.Id == op.AgentId)?.DisplayName ?? Strings.AppTitle)
                : Strings.AppTitle;

            _ = webhookNotificationService.SendWebhookAlertAsync(lockOperation, op.IpAddress, agentName, op.Message);
            _ = syslogNotificationService.SendSyslogAlertAsync(lockOperation, op.IpAddress, agentName, op.Message);
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(Strings.Get("Error while sending notification email.\r\n") + ex.Message,
                        EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_PLUGIN);
        }
    }
    /// <summary>
    /// Sends mail.
    /// </summary>
    /// <param name="subject">subject 的值。</param>
    /// <param name="message">message 的值。</param>
    /// <param name="isHtml"><see langword="true"/> when the trusted report body contains HTML; otherwise, <see langword="false"/>.</param>
    /// <param name="cancellationToken">Signals cancellation of SMTP delivery.</param>
    /// <param name="rethrowOnFailure"><see langword="true"/> to propagate delivery failures to the scheduler.</param>
    /// <returns>表示非同步執行的 Task。</returns>
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
                EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            if (rethrowOnFailure)
                throw;
        }
    }


    /// <summary>
    /// 執行 init 作業。
    /// </summary>
    private void Init()
    {

        cleanupTimer.Interval = 1000;
        cleanupTimer.Elapsed += new System.Timers.ElapsedEventHandler(cleanupTimer_Elapsed);
        maintenanceTimer.Interval = TimeSpan.FromHours(protectionOptions.MaintenanceIntervalHours).TotalMilliseconds;
        maintenanceTimer.AutoReset = true;
        maintenanceTimer.Elapsed += maintenanceTimer_Elapsed;
        // restartTimer.Elapsed += new System.Timers.ElapsedEventHandler(restartTimer_Elapsed);
        logManager.WriteEntry(Strings.Get("Intrusion Detection Service was initialized successfully."), EventLogEntryType.Information,
           Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        isInitialized = true;
    }

    private void maintenanceTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) => RunScheduledDatabaseMaintenance();

    private void RunScheduledDatabaseMaintenance()
    {
        if (System.Threading.Interlocked.Exchange(ref maintenanceActive, 1) != 0) return;
        try
        {
            SqliteMaintenanceService maintenance = new(database);
            maintenance.PurgeExpired(new DatabaseRetentionPolicy(
                protectionOptions.IntrusionLogRetentionDays,
                protectionOptions.LockHistoryRetentionDays,
                protectionOptions.AuditRetentionDays,
                protectionOptions.CompletedEventRetentionDays,
                protectionOptions.MaintenanceBatchSize));
            if (protectionOptions.AutomaticBackupEnabled)
            {
                string directory = Path.GetDirectoryName(database.DataSource) ?? AppContext.BaseDirectory;
                string backupDirectory = Path.Combine(directory, "Backups");
                maintenance.CreateVerifiedBackup(backupDirectory);
                maintenance.PruneBackups(backupDirectory, protectionOptions.BackupRetentionDays, protectionOptions.MaximumBackupCount);
            }
            maintenance.Optimize();

            // 執行動態 IP 智慧假釋（Probation）轉移維護
            int decayDays = Math.Max(7, configuration.ProbationDecayDays);
            DateTime probationCutoff = DateTime.UtcNow.AddDays(-decayDays);
            List<Lock> staleLocks = Locks.GetStalePermanentLocks(probationCutoff);
            foreach (Lock l in staleLocks)
            {
                try
                {
                    Locks.SetProbation(l.Id);
                    firewallPolicy.RemoveIpAddressFromBlockList(l.IpAddress);
                    TryRecordAudit("Firewall.Probation", "Succeeded", l.IpAddress);
                    logManager.WriteEntry(
                        string.Format("IP address {0} transitioned to probation after {1} days of zero activity.", l.IpAddress, decayDays),
                        EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                }
                catch (Exception ex)
                {
                    TryRecordAudit("Firewall.Probation", "Failed", l.IpAddress, ex.GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(Strings.Format("Database maintenance failed: {0}", ex.GetType().Name),
                EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref maintenanceActive, 0);
        }
    }


    /// <summary>
    /// 處理 elapsed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void cleanupTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (System.Threading.Interlocked.Exchange(ref cleanupActive, 1) != 0)
            return;
        try
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
                    OnClientIpAddressUnlocked(l, null);
                }
                catch (Exception ex)
                {
                    TryRecordAudit("Firewall.Unlock", "Failed", l.IpAddress, ex.GetType().Name);
                    logManager.WriteEntry(Strings.Format("IP address {0} cannot be unlocked. Error details: {1}", l.IpAddress, ex.Message),
                        EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                    l.Status = firewallPolicy.IsLocked(l.IpAddress)
                        ? Lock.LOCK_STATUS_UNLOCK_ERROR
                        : Lock.LOCK_STATUS_UNLOCKED;
                    l.Save();
                    OnClientIpAddressUnlocked(l, ex);
                }
            }
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(Strings.Format("Lock cleanup failed: {0}", ex.GetType().Name),
                EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref cleanupActive, 0);
        }
    }


        /// <summary>
    /// 取得或設定 是否已達郵件寄送上限。
    /// </summary>
public bool LimitMailSent { get; set; }
    /// <summary>
    /// 執行 lock down ip 作業。
    /// </summary>
    /// <param name="lockItem">lock item 的值。</param>
    /// <param name="lockType">lock type 的值。</param>
    /// <param name="reportingAgent">reporting agent 的值。</param>
    void LockDownIp(Lock lockItem, LockType lockType, SecurityAgent reportingAgent)
    {
        int locksForToday = Locks.Today();
        LimitMailSent = false;
        try
        {
            // TO DO: Hard Lock overrides Soft Lock!
            if (firewallPolicy.IsLocked(lockItem.IpAddress))
            {
                lockItem.Status = lockType == LockType.HardLock ? Lock.LOCK_STATUS_HARDLOCK : Lock.LOCK_STATUS_SOFTLOCK;
                lockItem.Save();
                logManager.WriteEntry(Strings.Get("Received another request to lock IP address ") + lockItem.IpAddress +
                            ". This IP address is already locked.", EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION,
                            Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                return;
            }
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(Strings.Get("Intrusion Detection Service had an error:") + ex.Message, EventLogEntryType.Error,
                  Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        logManager.WriteEntry(string.Format("{0} lock: Unsuccessful login attempts from ip address {1} exceeded threshold. Firewall rule is being created to block the address specified.",
            lockType == LockType.HardLock ? "Hard" : "Soft", lockItem.IpAddress), EventLogEntryType.FailureAudit, Globals.IDDSCOMMUNITY_EVENT_ID_FIREWALL_RULE_CREATED,
                    Globals.IDDSCOMMUNITY_LOG_CATEGORY_SECURITY);
        // lockItem.Id = Locks.CreateLock(lockItem);
        bool firewallApplied = false;
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
            firewallApplied = true;
        }
        catch (Exception ex)
        {
            logManager.WriteEntry(
                Strings.Format("Firewall block failed for {0}: {1}", lockItem.IpAddress, ex.GetType().Name),
                EventLogEntryType.Error,
                Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR,
                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        if (firewallApplied)
        {
            switch (lockType)
            {
                case LockType.SoftLock:
                    OnClientIpAddressSoftLocked(lockItem, null, reportingAgent.Id);
                    break;
                case LockType.HardLock:
                    OnClientIpAddressHardLocked(lockItem, null, reportingAgent.Id);
                    break;
            }
        }
        lockItem.Save();
        TryRecordAudit(
            lockType == LockType.HardLock ? "Firewall.HardLock" : "Firewall.SoftLock",
            firewallApplied ? "Succeeded" : "Failed",
            lockItem.IpAddress,
            reportingAgent.Id.ToString());


    }


    /// <summary>
    /// Starts the complete intrusion-detection runtime and rolls back partial startup on failure.
    /// </summary>
    /// <param name="cancellationToken">Signals cancellation of host startup.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
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
            ReconcileFirewallState();
            SqliteMaintenanceService maintenance = new(database);
            maintenance.PurgeExpired(new DatabaseRetentionPolicy(
                protectionOptions.IntrusionLogRetentionDays,
                protectionOptions.LockHistoryRetentionDays,
                protectionOptions.AuditRetentionDays,
                protectionOptions.CompletedEventRetentionDays,
                protectionOptions.MaintenanceBatchSize));
            SecurityEventInbox securityEventInbox = new(database, TimeProvider.System);
            securityEventPipeline = new SecurityEventPipeline(
                protectionOptions.SecurityEventQueueCapacity,
                ProcessAttackDetected,
                LogSecurityEventFailure,
                securityEventInbox,
                ResolveAgentForReplay);
            if (!isInitialized) Init();
            InitAgentConfiguration();
            agentsLoaded = true;
            LoadAgents();
            agentsStarted = true;
            securityAgents.StartAgents();
            securityEventPipeline.RecoverPending(protectionOptions.SecurityEventRecoveryBatchSize);
            cleanupTimer.Enabled = true;
            maintenanceTimer.Enabled = true;
            reportingStarted = true;
            reportScheduler.StartReporting();

            // 啟動動態 DNS (DDNS) 安全網路解析服務
            dynamicDnsResolverService = new DynamicDnsResolverService(
                configuration,
                msg => logManager.WriteEntry(msg, EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME),
                (msg, ex) => logManager.WriteEntry(msg + ": " + ex.Message, EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME));
            dynamicDnsResolverService.Start();

            // 依主機角色啟動威脅情資中繼中心 (Hub) 或邊緣節點 (Edge Node)
            if (configuration.ThreatHubRole == Shared.ThreatIntelligence.ThreatHubRole.ThreatHub)
            {
                threatHubServer = new ThreatIntelligenceHubServer(
                    configuration,
                    HandleClusterThreatReceived,
                    msg => logManager.WriteEntry(msg, EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME),
                    (msg, ex) => logManager.WriteEntry(msg + ": " + ex.Message, EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME));
                threatHubServer.Start();
            }
            else if (configuration.ThreatHubRole == Shared.ThreatIntelligence.ThreatHubRole.EdgeNode)
            {
                threatSyncService = new ThreatIntelligenceSyncService(
                    configuration,
                    HandleClusterThreatReceived,
                    msg => logManager.WriteEntry(msg, EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME),
                    (msg, ex) => logManager.WriteEntry(msg + ": " + ex.Message, EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME));
                threatSyncService.Start();
            }

            // 啟動外部威脅情資自動訂閱與主動防護服務
            externalThreatFeedSubscriberService = new ExternalThreatFeedSubscriberService(
                configuration,
                HandleExternalThreatFeedDiscovered,
                msg => logManager.WriteEntry(msg, EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME),
                (msg, ex) => logManager.WriteEntry(msg + ": " + ex.Message, EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME));
            externalThreatFeedSubscriberService.Start();

            // 啟動 Prometheus Metrics HTTP 服務
            metricsHttpServer.Start();

            runtimeStarted = true;
            TryRecordAudit("Runtime.Start", "Succeeded", Environment.MachineName);
            logManager.WriteEntry(Strings.Get("Intrusion Detection Service was started successfully."), EventLogEntryType.Information,
                Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Runtime.Start", "Failed", Environment.MachineName, ex.GetType().Name);
            StopComponents(throwOnFailure: false);
            try
            {
                logManager.WriteEntry(Strings.Get("Intrusion Detection Service had a startup error. Details:") + ex.Message, EventLogEntryType.Error,
                    Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
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
    /// <returns>表示非同步工作完成的 Task。</returns>
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
        TryStop(metricsHttpServer.Stop, failures);
        if (externalThreatFeedSubscriberService is not null)
            TryStop(externalThreatFeedSubscriberService.Stop, failures);
        if (threatSyncService is not null)
            TryStop(threatSyncService.Stop, failures);
        if (threatHubServer is not null)
            TryStop(threatHubServer.Stop, failures);
        if (dynamicDnsResolverService is not null)
            TryStop(dynamicDnsResolverService.Stop, failures);

        if (reportingStarted)
            TryStop(reportScheduler.StopReporting, failures);
        reportingStarted = false;

        cleanupTimer.Enabled = false;
        maintenanceTimer.Enabled = false;
        if (agentsStarted)
            TryStop(securityAgents.StopAgents, failures);
        agentsStarted = false;

        if (agentsLoaded)
            TryStop(UnloadAgents, failures);
        agentsLoaded = false;

        if (securityEventPipeline is not null)
        {
            securityEventPipeline.Complete();
            TryStop(() => securityEventPipeline.Drain(TimeSpan.FromSeconds(protectionOptions.SecurityEventDrainTimeoutSeconds)), failures);
            securityEventPipeline = null;
        }
        bool wasStarted = runtimeStarted;
        runtimeStarted = false;

        if (wasStarted)
        {
            TryRecordAudit("Runtime.Stop", failures.Count == 0 ? "Succeeded" : "Failed", Environment.MachineName);
            TryStop(() => logManager.WriteEntry(Strings.Get("Intrusion Detection Service was stopped."), EventLogEntryType.Information,
                Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME), failures);
        }

        if (throwOnFailure && failures.Count == 1)
            throw failures[0];
        if (throwOnFailure && failures.Count > 1)
            throw new AggregateException(failures);
    }
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
                Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR,
                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
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
        maintenanceTimer.Dispose();
        dynamicDnsResolverService?.Dispose();
        threatHubServer?.Dispose();
        threatSyncService?.Dispose();
        externalThreatFeedSubscriberService?.Dispose();
        webhookNotificationService.Dispose();
        syslogNotificationService.Dispose();
        metricsHttpServer.Dispose();
        lifecycleLock.Dispose();
        disposed = true;
    }
    /// <summary>
    /// 執行 init agent configuration 作業。
    /// </summary>
    private void InitAgentConfiguration() => securityAgents.RegisterSecurityAgents();


    /// <summary>
    /// 處理 attack detected 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="notificationEventArgs">The event data.</param>
    void Service_AttackDetected(object sender, INotificationEventArgs notificationEventArgs)
    {
        SecurityEventPipeline? pipeline = securityEventPipeline;
        if (pipeline is not null && pipeline.Publish(sender, notificationEventArgs))
            return;
        logManager.WriteEntry(
            Strings.Get("Security event pipeline is stopping or unavailable; the event could not be accepted."),
            EventLogEntryType.Error,
            Globals.IDDSCOMMUNITY_EVENT_ID_PLUGIN_ERROR,
            Globals.IDDSCOMMUNITY_LOG_CATEGORY_PLUGIN);
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
                    throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Operation not supported. EventArgs must be passed as NotificationEventArgs"));
                }
                else
                {
                    // otherwise write to the log file
                    logManager.WriteEntry(Strings.Get("Plugin error: the lock delegate was called, but notificationEventArgs must not be null!"),
                        EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_INVALID_FUNCTION_CALL, Globals.IDDSCOMMUNITY_LOG_CATEGORY_PLUGIN);
                    return;
                }
            }
            if (sender is not IAgentPlugin reportingPlugin)
                return;
            SecurityAgent? reportingAgent = securityAgents.FindByName(reportingPlugin.Configuration.AgentName);
            if (reportingAgent is null)
                return;
            long incidentId;
            if (IpAddressCanonicalizer.TryCanonicalize(notificationEventArgs.IpAddress, out string canonicalIpAddress))
            {
                notificationEventArgs.IpAddress = canonicalIpAddress;
                if (configuration.EnableCrossAgentCorrelation)
                {
                    SecurityObservationEvent observation = CreateSecurityObservation(reportingAgent.Name, notificationEventArgs);
                    crossAgentCorrelationEngine.PrepareObservation(observation);

                    (bool isDuplicate, _) = SecurityObservationStore.PersistObservationAndWatermark(observation, database);
                    if (isDuplicate)
                    {
                        return;
                    }

                    CorrelationEvaluationResult correlationResult = CompleteCorrelationObservation(observation);
                    if (correlationResult.IsDuplicateReplay)
                    {
                        return;
                    }

                    if (correlationResult.IsCrossSourceDuplicate)
                    {
                        return;
                    }

                }

                if (!ShouldProcessLegacyDetection(notificationEventArgs))
                {
                    return;
                }

                statistics.IncreaseFailedLoginStatistics(reportingAgent);
                // Agent 提供的 CreateDate 為本機時間（例如 Windows 事件記錄的 TimeCreated）；
                // IncidentTime/LockDate/UnlockDate 資料庫欄位一律以 UTC 儲存，於此處統一轉換。
                DateTime incidentTimeUtc = notificationEventArgs.CreateDate.ToUniversalTime();
                if (System.Net.IPAddress.TryParse(notificationEventArgs.IpAddress, out System.Net.IPAddress? ipAddress) && configuration.IsIpAddressLocal(ipAddress))
                {
                    incidentId = IntrusionLog.AddEntry(incidentTimeUtc, reportingAgent.Id, notificationEventArgs.IpAddress,
                        IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL, false);
                }
                else if (configuration.UseSafeNetworkList && configuration.IsInSafeNetwork(notificationEventArgs.IpAddress))
                {
                    incidentId = IntrusionLog.AddEntry(incidentTimeUtc, reportingAgent.Id, notificationEventArgs.IpAddress,
                        IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE, false);
                }
                else
                {
                    incidentId = IntrusionLog.AddEntry(incidentTimeUtc, reportingAgent.Id, notificationEventArgs.IpAddress,
                        IntrusionLog.STATUS_INTRUSION_ATTEMPT, false);

                    try
                    {
                        if (!Locks.LockExists(notificationEventArgs.IpAddress))
                        {
                            bool isProbation = Locks.IsProbation(notificationEventArgs.IpAddress);
                            if (isProbation)
                            {
                                logManager.WriteEntry(
                                    string.Format("IP address {0} in probation state re-offended. Executing immediate one-strike permanent hard lock.", notificationEventArgs.IpAddress),
                                    EventLogEntryType.Warning, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_SECURITY);
                            }

                            LockType lockType = isProbation ? LockType.HardLockRequested : reportingAgent.GetCurrentLockType(notificationEventArgs.IpAddress);
                            if (lockType != LockType.None && lockType != LockType.SoftLock)
                            {
                                int recentLockCount = Locks.GetRecentLockCount(
                                    reportingAgent.Id,
                                    notificationEventArgs.IpAddress,
                                    DateTime.UtcNow.AddDays(-7));
                                int baseSoftLockMinutes = reportingAgent.OverrideConfig ? reportingAgent.SoftLockTimeMinutes : configuration.SoftLockTimeMinutes;
                                int configuredHardLockHours = reportingAgent.OverrideConfig ? reportingAgent.HardLockTimeHours : configuration.HardLockTimeHours;
                                bool isLockForever = reportingAgent.OverrideConfig ? reportingAgent.LockForever : configuration.LockForever;

                                bool isHardLock = lockType == LockType.HardLockRequested
                                    || isLockForever
                                    || isProbation
                                    || LockoutPolicy.ShouldEscalateToHardLock(recentLockCount, LockoutPolicy.DefaultAutoHardLockThreshold);

                                if (isHardLock)
                                {
                                    DateTime hardUnlockDate = DateTime.MaxValue;
                                    LockDownIp(
                                        Locks.CreateLock(DateTime.UtcNow, hardUnlockDate, incidentId, Lock.LOCK_STATUS_HARDLOCK_REQUESTED, 0, notificationEventArgs.IpAddress),
                                        LockType.HardLock,
                                        reportingAgent);

                                    // 主動將本機永久硬封鎖威脅推播至叢集 (Hub / Edge)
                                    Shared.ThreatIntelligence.ThreatIntelligenceItem threatItem = new()
                                    {
                                        SourceIp = notificationEventArgs.IpAddress,
                                        ThreatCategory = reportingAgent.Name ?? "BRUTE_FORCE",
                                        ConfidenceScore = 1.0,
                                        ReportedUtc = DateTime.UtcNow,
                                        ReporterNodeName = Environment.MachineName
                                    };
                                    threatHubServer?.IngestLocalThreat(threatItem);
                                    threatSyncService?.EnqueueLocalThreat(threatItem);
                                }
                                else
                                {
                                    int maxSoftLockMinutes = Math.Max(baseSoftLockMinutes, Math.Max(1, configuredHardLockHours) * 60);
                                    int softLockMinutes = LockoutPolicy.CalculateSoftLockMinutes(
                                        baseSoftLockMinutes,
                                        recentLockCount,
                                        maxSoftLockMinutes);
                                    DateTime softUnlockDate = DateTime.UtcNow.AddMinutes(softLockMinutes);
                                    LockDownIp(
                                        Locks.CreateLock(DateTime.UtcNow, softUnlockDate, incidentId, Lock.LOCK_STATUS_SOFTLOCK_REQUESTED, 0, notificationEventArgs.IpAddress),
                                        LockType.SoftLock,
                                        reportingAgent);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logManager.WriteEntry(string.Format("Unrecoverable error: {0}",
                                ex.Message), EventLogEntryType.FailureAudit, Globals.IDDSCOMMUNITY_EVENT_ID_PLUGIN_ERROR,
                                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
                        // OnClientIpAddressSoftLocked(new Lock( new Client(notificationEventArgs.IpAddress), ex);
                        throw;
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
                EventLogEntryType.Error, Globals.IDDSCOMMUNITY_EVENT_ID_PLUGIN_ERROR, Globals.IDDSCOMMUNITY_LOG_CATEGORY_PLUGIN);
            throw;
        }
    }

    /// <summary>
    /// 處理自 Threat Hub 或邊緣節點接收到之跨主機聯防威脅情資。
    /// </summary>
    /// <param name="item">威脅情資項目。</param>
    private void HandleClusterThreatReceived(Shared.ThreatIntelligence.ThreatIntelligenceItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.SourceIp)) return;
        string ip = IpAddressCanonicalizer.Canonicalize(item.SourceIp);
        if (configuration.UseSafeNetworkList && configuration.IsInSafeNetwork(ip)) return;
        if (Locks.LockExists(ip) || firewallPolicy.IsLocked(ip)) return;

        try
        {
            firewallPolicy.Block(ip);
            TryRecordAudit("Firewall.ClusterLock", "Succeeded", ip);
            long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, WellKnownAgentIds.ClusterThreatHub, ip, IntrusionLog.STATUS_HARD_LOCKED, false);
            Locks.CreateLock(DateTime.UtcNow, item.ExpiresUtc, incidentId, Lock.LOCK_STATUS_HARDLOCK, 0, ip);
            logManager.WriteEntry(
                string.Format("IP address {0} was locked via Threat Intelligence Cluster sync (reported by {1}).", ip, item.ReporterNodeName),
                EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_SECURITY);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Firewall.ClusterLock", "Failed", ip, ex.GetType().Name);
        }
    }

    /// <summary>
    /// 處理自外部威脅情報（Threat Feeds）訂閱來源發現之惡意 IP。
    /// </summary>
    /// <param name="item">外部威脅情報項目。</param>
    private void HandleExternalThreatFeedDiscovered(Shared.ThreatIntelligence.ThreatIntelligenceItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.SourceIp)) return;
        string ip = IpAddressCanonicalizer.Canonicalize(item.SourceIp);
        if (Shared.ThreatIntelligence.BogonIpFilter.IsBogonOrReserved(ip)) return;
        if (configuration.UseSafeNetworkList && configuration.IsInSafeNetwork(ip)) return;

        // 若本主機為 Threat Hub，主動將外部情報注入 Hub 威脅庫以廣播給邊緣節點
        threatHubServer?.IngestLocalThreat(item);

        if (Locks.LockExists(ip) || firewallPolicy.IsLocked(ip)) return;

        try
        {
            firewallPolicy.Block(ip);
            TryRecordAudit("Firewall.ExternalThreatFeedLock", "Succeeded", ip);
            long incidentId = IntrusionLog.AddEntry(DateTime.UtcNow, WellKnownAgentIds.ExternalThreatFeed, ip, IntrusionLog.STATUS_HARD_LOCKED, false);
            Locks.CreateLock(DateTime.UtcNow, item.ExpiresUtc, incidentId, Lock.LOCK_STATUS_HARDLOCK, 0, ip);
            logManager.WriteEntry(
                string.Format("IP address {0} was preemptively locked via External Threat Feed subscription ({1}).", ip, item.ReporterNodeName),
                EventLogEntryType.Information, Globals.IDDSCOMMUNITY_EVENT_ID_INFORMATION, Globals.IDDSCOMMUNITY_LOG_CATEGORY_SECURITY);
        }
        catch (Exception ex)
        {
            TryRecordAudit("Firewall.ExternalThreatFeedLock", "Failed", ip, ex.GetType().Name);
        }
    }
    /// <summary>
    /// Records an isolated security-event consumer failure.
    /// </summary>
    /// <param name="exception">The processing failure.</param>
    private void LogSecurityEventFailure(Exception exception) => logManager.WriteEntry(
        Strings.Format("Security event processing failed: {0}", exception.GetType().Name),
        EventLogEntryType.Error,
        Globals.IDDSCOMMUNITY_EVENT_ID_PLUGIN_ERROR,
        Globals.IDDSCOMMUNITY_LOG_CATEGORY_PLUGIN);
    /// <summary>
    /// Resolves a persisted Agent name to its currently loaded proxy for event replay.
    /// </summary>
    /// <param name="agentName">The stable Agent configuration name.</param>
    /// <returns>已載入的 Agent 代理物件；若無可用擴充元件則傳回 <see langword="null"/>。</returns>
    private object? ResolveAgentForReplay(string agentName)
    {
        foreach (KeyValuePair<SecurityAgent, AgentProxy> pair in securityAgents.LoadedAgents)
        {
            if (string.Equals(pair.Value.Configuration.AgentName, agentName, StringComparison.Ordinal))
                return pair.Value;
        }
        return null;
    }
    /// <summary>
    /// Reconciles the persisted desired lock state with the IDDSCommunity Windows Firewall rule.
    /// </summary>
    private void ReconcileFirewallState()
    {
        FirewallStateReconciler reconciler = new(
            firewallPolicy,
            Locks.GetActiveLocks,
            static lockItem => lockItem.Save(),
            TryRecordAudit,
            (address, exception) => logManager.WriteEntry(
                Strings.Format("Firewall reconciliation failed for {0}: {1}", address, exception.GetType().Name),
                EventLogEntryType.Error,
                Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR,
                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME));
        reconciler.Reconcile();
    }


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
    /// 執行 unload agents 作業。
    /// </summary>
    private void UnloadAgents() => securityAgents.UnloadAgents();

    private static string ParseAccountFromEvent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        int userIndex = message.IndexOf("User:", StringComparison.OrdinalIgnoreCase);
        if (userIndex >= 0)
        {
            string remainder = message[(userIndex + 5)..].Trim();
            int end = remainder.IndexOfAny([' ', '\r', '\n', ';', ',']);
            return end > 0 ? remainder[..end].Trim() : remainder;
        }

        int accountIndex = message.IndexOf("Account:", StringComparison.OrdinalIgnoreCase);
        if (accountIndex >= 0)
        {
            string remainder = message[(accountIndex + 8)..].Trim();
            int end = remainder.IndexOfAny([' ', '\r', '\n', ';', ',']);
            return end > 0 ? remainder[..end].Trim() : remainder;
        }

        return string.Empty;
    }

    /// <summary>
    /// 將擴充元件通知轉換為中央關聯引擎使用的標準化安全性觀察事件。
    /// </summary>
    /// <param name="sourceAgentName">來源 Agent 名稱。</param>
    /// <param name="notificationEventArgs">擴充元件通知事件。</param>
    /// <returns>完整保留驗證語意的安全性觀察事件。</returns>
    internal static SecurityObservationEvent CreateSecurityObservation(
        string sourceAgentName,
        INotificationEventArgs notificationEventArgs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAgentName);
        ArgumentNullException.ThrowIfNull(notificationEventArgs);

        AuthenticationNotificationEventArgs? authentication = notificationEventArgs as AuthenticationNotificationEventArgs;
        DateTimeOffset receivedTimeUtc = DateTimeOffset.UtcNow;
        DateTimeOffset eventTimeUtc = notificationEventArgs.CreateDate.ToUniversalTime();
        if (eventTimeUtc > receivedTimeUtc.AddMinutes(5))
            eventTimeUtc = receivedTimeUtc;
        SecurityObservationEvent observation = new()
        {
            SourceAgentName = sourceAgentName,
            ProviderOrChannel = authentication?.ProviderOrChannel ?? string.Empty,
            ComputerName = authentication?.ComputerName ?? string.Empty,
            SourceEventRecordId = authentication?.SourceEventRecordId,
            NormalizedIpAddress = IpAddressCanonicalizer.Canonicalize(notificationEventArgs.IpAddress),
            NormalizedAccount = authentication?.AccountName ?? ParseAccountFromEvent(notificationEventArgs.EventMessage),
            NormalizedDomain = authentication?.AccountDomain ?? string.Empty,
            AccountSid = authentication?.AccountSid ?? string.Empty,
            EventTimeUtc = eventTimeUtc,
            ReceivedTimeUtc = receivedTimeUtc,
            OriginalEventReference = authentication?.SourceEventRecordId?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                ?? notificationEventArgs.EventId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Provenance = $"Agent={sourceAgentName};Provider={authentication?.ProviderOrChannel};EventId={notificationEventArgs.EventId}",
            IsCredentialFailure = authentication?.IsCredentialFailure ?? true,
            ActivityId = authentication?.ActivityId,
            ConfidenceScore = authentication?.ConfidenceScore ?? 1.0,
            TargetResource = authentication?.TargetResource,
            ErrorCode = authentication?.ErrorCode
        };
        AccountIdentityNormalizer.Normalize(observation);
        return observation;
    }

    /// <summary>
    /// 判斷通知是否可進入既有失敗統計、入侵記錄與封鎖流程。
    /// </summary>
    /// <param name="notificationEventArgs">擴充元件通知事件。</param>
    /// <returns>若為密碼或帳號憑證失敗，或為不具新契約的舊版通知則傳回 <see langword="true"/>。</returns>
    internal static bool ShouldProcessLegacyDetection(INotificationEventArgs notificationEventArgs)
    {
        ArgumentNullException.ThrowIfNull(notificationEventArgs);
        return notificationEventArgs is not AuthenticationNotificationEventArgs authentication || authentication.IsCredentialFailure;
    }

    private CorrelationEvaluationResult CompleteCorrelationObservation(SecurityObservationEvent observation)
    {
        CorrelationEvaluationResult result = crossAgentCorrelationEngine.Ingest(observation, configuration);
        if (result.Action != CorrelationAction.AlertAndScoreOnly)
        {
            SecurityObservationStore.UpdateCorrelationMetadata(observation, database);
            return result;
        }

        string targetSubject = result.SprayType == SprayAttackType.MultipleIpsToOneAccount
            ? observation.NormalizedAccount
            : observation.NormalizedIpAddress;
        string alertId = SecurityObservationStore.ComputeAlertId(result.SprayType, targetSubject, result.ContributingIdempotencyKeys);
        bool enqueued = SecurityObservationStore.CompleteCorrelationAndEnqueueAlert(
            observation,
            alertId,
            "CrossAgentSprayDetected",
            "AlertOnly",
            observation.SourceAgentName,
            targetSubject,
            result.Message,
            database);
        if (enqueued)
        {
            SecurityAgent? reportingAgent = securityAgents.FindByName(observation.SourceAgentName);
            if (reportingAgent is not null && IpAddressCanonicalizer.TryCanonicalize(observation.NormalizedIpAddress, out string alertIpAddress))
            {
                IntrusionLog.AddEntry(
                    observation.EventTimeUtc.UtcDateTime,
                    reportingAgent.Id,
                    alertIpAddress,
                    IntrusionLog.STATUS_CROSS_AGENT_SPRAY_ALERT,
                    false);
            }
            SecurityObservationStore.DispatchPendingAlerts(database, protectionAuditTrail);
        }

        return result;
    }

    private void RecoverPendingCorrelationObservations()
    {
        foreach (SecurityObservationEvent observation in SecurityObservationStore.LoadPendingCorrelationObservations(database))
        {
            CompleteCorrelationObservation(observation);
        }
    }
}
