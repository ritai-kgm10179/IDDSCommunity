namespace IDDSCommunity.IntrusionDetection.Shared;

using System;

/// <summary>
/// 代表電子郵件警報與定期報表寄送組態模型。
/// </summary>
public class NotificationSettings
{
    private readonly IddsConfig configuration;
        /// <summary>
    /// 定義 NOTIFICATION_ON_UNLOCK 之數值。
    /// </summary>
public const string NOTIFICATION_ON_UNLOCK = "{BE227461-8622-4168-A15C-644773070A5D}";
        /// <summary>
    /// 定義 NOTIFICATION_ON_SOFT_LOCK 之數值。
    /// </summary>
public const string NOTIFICATION_ON_SOFT_LOCK = "{C4A9EC33-44E0-445A-8134-009F559E22A1}";
        /// <summary>
    /// 定義 NOTIFICATION_ON_HARD_LOCK 之數值。
    /// </summary>
public const string NOTIFICATION_ON_HARD_LOCK = "{AD963FA3-1D19-4CFE-9F6A-C1A8A4DBBA33}";
        /// <summary>
    /// 定義 NOTIFICATION_SUMMARY_REPORT 之數值。
    /// </summary>
public const string NOTIFICATION_SUMMARY_REPORT = "{AADCD484-483A-4CAB-AA91-8C83942AE3AB}";
        /// <summary>
    /// 定義 NOTIFICATION_SUMMARY_REPORT_DAILY 之數值。
    /// </summary>
public const string NOTIFICATION_SUMMARY_REPORT_DAILY = "{92993EF9-5C9E-4E58-9250-F2EF1E838914}";
        /// <summary>
    /// 定義 NOTIFICATION_SUMMARY_REPORT_WEEKLY 之數值。
    /// </summary>
public const string NOTIFICATION_SUMMARY_REPORT_WEEKLY = "{06191D72-17B5-488F-BB59-F6E61B4C4B42}";
        /// <summary>
    /// 定義 NOTIFICATION_SUMMARY_REPORT_MONTHLY 之數值。
    /// </summary>
public const string NOTIFICATION_SUMMARY_REPORT_MONTHLY = "{8B0995D6-6820-4FBE-9983-4A9533FB0BFD}";
        /// <summary>
    /// 定義 LAST_DAILY_REPORT 之數值。
    /// </summary>
public const string LAST_DAILY_REPORT = "{78F53752-3167-4A81-BBC2-1CEFAF3211CE}";
        /// <summary>
    /// 定義 LAST_WEEKLY_REPORT 之數值。
    /// </summary>
public const string LAST_WEEKLY_REPORT = "{10C8A9BC-A1CA-4BD5-818C-39A4696E9C80}";
        /// <summary>
    /// 定義 LAST_MONTHLY_REPORT 之數值。
    /// </summary>
public const string LAST_MONTHLY_REPORT = "{4D3BC893-8C13-41ED-BEF8-35BEB768C7E8}";
        /// <summary>
    /// 定義 DAILY_REPORT_STATE 之數值。
    /// </summary>
public const string DAILY_REPORT_STATE = "Reports.Daily.State";
        /// <summary>
    /// 定義 WEEKLY_REPORT_STATE 之數值。
    /// </summary>
public const string WEEKLY_REPORT_STATE = "Reports.Weekly.State";
        /// <summary>
    /// 定義 MONTHLY_REPORT_STATE 之數值。
    /// </summary>
public const string MONTHLY_REPORT_STATE = "Reports.Monthly.State";
        /// <summary>
    /// 定義 NOTIFICATION_ENABLE_WEBHOOK 之數值。
    /// </summary>
public const string NOTIFICATION_ENABLE_WEBHOOK = "Notifications.Webhook.Enable";
        /// <summary>
    /// 定義 NOTIFICATION_WEBHOOK_PLATFORM 之數值。
    /// </summary>
public const string NOTIFICATION_WEBHOOK_PLATFORM = "Notifications.Webhook.Platform";
        /// <summary>
    /// 定義 NOTIFICATION_WEBHOOK_URL 之數值。
    /// </summary>
public const string NOTIFICATION_WEBHOOK_URL = "Notifications.Webhook.Url";
        /// <summary>
    /// 定義 NOTIFICATION_TELEGRAM_BOT_TOKEN 之數值。
    /// </summary>
public const string NOTIFICATION_TELEGRAM_BOT_TOKEN = "Notifications.Telegram.BotToken";
        /// <summary>
    /// 定義 NOTIFICATION_TELEGRAM_CHAT_ID 之數值。
    /// </summary>
public const string NOTIFICATION_TELEGRAM_CHAT_ID = "Notifications.Telegram.ChatId";
        /// <summary>
    /// 定義 NOTIFICATION_WEBHOOK_ON_SOFT_LOCK 之數值。
    /// </summary>
public const string NOTIFICATION_WEBHOOK_ON_SOFT_LOCK = "Notifications.Webhook.OnSoftLock";
        /// <summary>
    /// 定義 NOTIFICATION_WEBHOOK_ON_HARD_LOCK 之數值。
    /// </summary>
public const string NOTIFICATION_WEBHOOK_ON_HARD_LOCK = "Notifications.Webhook.OnHardLock";
        /// <summary>
    /// 定義 NOTIFICATION_WEBHOOK_ON_UNLOCK 之數值。
    /// </summary>
public const string NOTIFICATION_WEBHOOK_ON_UNLOCK = "Notifications.Webhook.OnUnlock";

    private static NotificationSettings? _instance;

        /// <summary>
    /// 取得或設定 全域共用單例執行個體。
    /// </summary>
public static NotificationSettings Instance
    {
        get
        {
            _instance ??= new NotificationSettings(IddsConfig.Instance);
            return _instance;
        }
    }


        /// <summary>
    /// 取得或設定 OnUnlock。
    /// </summary>
public bool OnUnlock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ON_UNLOCK)); set => configuration.SetConfigValue(NOTIFICATION_ON_UNLOCK, value.ToString());
    }



        /// <summary>
    /// 取得或設定 OnSoftLock。
    /// </summary>
public bool OnSoftLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ON_SOFT_LOCK)); set => configuration.SetConfigValue(NOTIFICATION_ON_SOFT_LOCK, value.ToString());
    }

        /// <summary>
    /// 取得或設定 OnHardLock。
    /// </summary>
public bool OnHardLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ON_HARD_LOCK)); set => configuration.SetConfigValue(NOTIFICATION_ON_HARD_LOCK, value.ToString());
    }
        /// <summary>
    /// 取得或設定 SummaryReport。
    /// </summary>
public bool SummaryReport
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT, value.ToString());
    }
        /// <summary>
    /// 取得或設定 SummaryReportDaily。
    /// </summary>
public bool SummaryReportDaily
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT_DAILY)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT_DAILY, value.ToString());
    }

        /// <summary>
    /// 取得或設定 SummaryReportWeekly。
    /// </summary>
public bool SummaryReportWeekly
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT_WEEKLY)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT_WEEKLY, value.ToString());
    }

        /// <summary>
    /// 取得或設定 SummaryReportMonthly。
    /// </summary>
public bool SummaryReportMonthly
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT_MONTHLY)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT_MONTHLY, value.ToString());
    }


        /// <summary>
    /// 取得或設定 LastDailyReport。
    /// </summary>
public string LastDailyReport
    {
        get => configuration.GetConfigValue(LAST_DAILY_REPORT); set => configuration.SetConfigValue(LAST_DAILY_REPORT, value);
    }

        /// <summary>
    /// 取得或設定 LastWeeklyReport。
    /// </summary>
public string LastWeeklyReport
    {
        get => configuration.GetConfigValue(LAST_WEEKLY_REPORT); set => configuration.SetConfigValue(LAST_WEEKLY_REPORT, value);
    }

        /// <summary>
    /// 取得或設定 LastMonthlyReport。
    /// </summary>
public string LastMonthlyReport
    {
        get => configuration.GetConfigValue(LAST_MONTHLY_REPORT); set => configuration.SetConfigValue(LAST_MONTHLY_REPORT, value);
    }

        /// <summary>
    /// 取得或設定 DailyReportState。
    /// </summary>
public ReportDeliveryState DailyReportState
    {
        get => GetReportState(DAILY_REPORT_STATE);
        set => configuration.SetConfigValue(DAILY_REPORT_STATE, value.ToString());
    }

        /// <summary>
    /// 取得或設定 WeeklyReportState。
    /// </summary>
public ReportDeliveryState WeeklyReportState
    {
        get => GetReportState(WEEKLY_REPORT_STATE);
        set => configuration.SetConfigValue(WEEKLY_REPORT_STATE, value.ToString());
    }

        /// <summary>
    /// 取得或設定 MonthlyReportState。
    /// </summary>
public ReportDeliveryState MonthlyReportState
    {
        get => GetReportState(MONTHLY_REPORT_STATE);
        set => configuration.SetConfigValue(MONTHLY_REPORT_STATE, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否啟用 Webhook 即時告警。
    /// </summary>
    public bool EnableWebhook
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ENABLE_WEBHOOK));
        set => configuration.SetConfigValue(NOTIFICATION_ENABLE_WEBHOOK, value.ToString());
    }

    /// <summary>
    /// 取得或設定 Webhook 目標平台類型。
    /// </summary>
    public WebhookPlatform WebhookPlatform
    {
        get => Enum.TryParse(configuration.GetConfigValue(NOTIFICATION_WEBHOOK_PLATFORM), ignoreCase: true, out WebhookPlatform platform) ? platform : WebhookPlatform.None;
        set => configuration.SetConfigValue(NOTIFICATION_WEBHOOK_PLATFORM, value.ToString());
    }

    /// <summary>
    /// 取得或設定 Webhook 端點 URL。
    /// </summary>
    public string WebhookUrl
    {
        get => configuration.GetConfigValue(NOTIFICATION_WEBHOOK_URL) ?? string.Empty;
        set => configuration.SetConfigValue(NOTIFICATION_WEBHOOK_URL, value);
    }

    /// <summary>
    /// 取得或設定 Telegram Bot Token。
    /// </summary>
    public string TelegramBotToken
    {
        get => configuration.GetConfigValue(NOTIFICATION_TELEGRAM_BOT_TOKEN) ?? string.Empty;
        set => configuration.SetConfigValue(NOTIFICATION_TELEGRAM_BOT_TOKEN, value);
    }

    /// <summary>
    /// 取得或設定 Telegram 頻道或群組 Chat ID。
    /// </summary>
    public string TelegramChatId
    {
        get => configuration.GetConfigValue(NOTIFICATION_TELEGRAM_CHAT_ID) ?? string.Empty;
        set => configuration.SetConfigValue(NOTIFICATION_TELEGRAM_CHAT_ID, value);
    }

    /// <summary>
    /// 取得或設定是否在軟封鎖事件觸發時發送 Webhook。
    /// </summary>
    public bool WebhookOnSoftLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_WEBHOOK_ON_SOFT_LOCK));
        set => configuration.SetConfigValue(NOTIFICATION_WEBHOOK_ON_SOFT_LOCK, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否在硬封鎖事件觸發時發送 Webhook。
    /// </summary>
    public bool WebhookOnHardLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_WEBHOOK_ON_HARD_LOCK));
        set => configuration.SetConfigValue(NOTIFICATION_WEBHOOK_ON_HARD_LOCK, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否在解除封鎖事件觸發時發送 Webhook。
    /// </summary>
    public bool WebhookOnUnlock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_WEBHOOK_ON_UNLOCK));
        set => configuration.SetConfigValue(NOTIFICATION_WEBHOOK_ON_UNLOCK, value.ToString());
    }

    /// <summary>
    /// 定義 NOTIFICATION_ENABLE_SYSLOG 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_ENABLE_SYSLOG = "Notifications.EnableSyslog";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_HOST 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_HOST = "Notifications.SyslogHost";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_PORT 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_PORT = "Notifications.SyslogPort";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_PROTOCOL 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_PROTOCOL = "Notifications.SyslogProtocol";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_FORMAT 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_FORMAT = "Notifications.SyslogFormat";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_ON_SOFT_LOCK 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_ON_SOFT_LOCK = "Notifications.SyslogOnSoftLock";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_ON_HARD_LOCK 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_ON_HARD_LOCK = "Notifications.SyslogOnHardLock";

    /// <summary>
    /// 定義 NOTIFICATION_SYSLOG_ON_UNLOCK 之設定鍵值。
    /// </summary>
    public const string NOTIFICATION_SYSLOG_ON_UNLOCK = "Notifications.SyslogOnUnlock";

    /// <summary>
    /// 定義 OBSERVABILITY_ENABLE_METRICS 之設定鍵值。
    /// </summary>
    public const string OBSERVABILITY_ENABLE_METRICS = "Observability.EnableMetrics";

    /// <summary>
    /// 定義 OBSERVABILITY_METRICS_LISTEN_IP 之設定鍵值。
    /// </summary>
    public const string OBSERVABILITY_METRICS_LISTEN_IP = "Observability.MetricsListenIp";

    /// <summary>
    /// 定義 OBSERVABILITY_METRICS_PORT 之設定鍵值。
    /// </summary>
    public const string OBSERVABILITY_METRICS_PORT = "Observability.MetricsPort";

    /// <summary>
    /// 定義 OBSERVABILITY_METRICS_ALLOWED_NETWORKS 之設定鍵值。
    /// </summary>
    public const string OBSERVABILITY_METRICS_ALLOWED_NETWORKS = "Observability.MetricsAllowedNetworks";

    /// <summary>
    /// 取得或設定是否啟用 Syslog / CEF 即時轉送。
    /// </summary>
    public bool EnableSyslog
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ENABLE_SYSLOG));
        set => configuration.SetConfigValue(NOTIFICATION_ENABLE_SYSLOG, value.ToString());
    }

    /// <summary>
    /// 取得或設定 Syslog 伺服器主機名稱或 IP。
    /// </summary>
    public string SyslogHost
    {
        get => configuration.GetConfigValue(NOTIFICATION_SYSLOG_HOST) ?? string.Empty;
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_HOST, value);
    }

    /// <summary>
    /// 取得或設定 Syslog 伺服器通訊埠（預設 514）。
    /// </summary>
    public int SyslogPort
    {
        get => int.TryParse(configuration.GetConfigValue(NOTIFICATION_SYSLOG_PORT), out int port) ? port : 514;
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_PORT, value.ToString());
    }

    /// <summary>
    /// 取得或設定 Syslog 傳輸協定（UDP、TCP、TLS）。
    /// </summary>
    public SyslogProtocol SyslogProtocol
    {
        get => Enum.TryParse(configuration.GetConfigValue(NOTIFICATION_SYSLOG_PROTOCOL), ignoreCase: true, out SyslogProtocol proto) ? proto : SyslogProtocol.Udp;
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_PROTOCOL, value.ToString());
    }

    /// <summary>
    /// 取得或設定 Syslog 訊息格式（RFC5424、RFC3164、CEF）。
    /// </summary>
    public SyslogFormat SyslogFormat
    {
        get => Enum.TryParse(configuration.GetConfigValue(NOTIFICATION_SYSLOG_FORMAT), ignoreCase: true, out SyslogFormat fmt) ? fmt : SyslogFormat.Rfc5424;
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_FORMAT, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否在軟封鎖時發送 Syslog。
    /// </summary>
    public bool SyslogOnSoftLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SYSLOG_ON_SOFT_LOCK));
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_ON_SOFT_LOCK, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否在硬封鎖時發送 Syslog。
    /// </summary>
    public bool SyslogOnHardLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SYSLOG_ON_HARD_LOCK));
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_ON_HARD_LOCK, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否在解鎖時發送 Syslog。
    /// </summary>
    public bool SyslogOnUnlock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SYSLOG_ON_UNLOCK));
        set => configuration.SetConfigValue(NOTIFICATION_SYSLOG_ON_UNLOCK, value.ToString());
    }

    /// <summary>
    /// 取得或設定是否啟用 Prometheus / OpenMetrics 指標監控端點。
    /// </summary>
    public bool EnableMetricsEndpoint
    {
        get => StringToBool(configuration.GetConfigValue(OBSERVABILITY_ENABLE_METRICS));
        set => configuration.SetConfigValue(OBSERVABILITY_ENABLE_METRICS, value.ToString());
    }

    /// <summary>
    /// 取得或設定 Prometheus 監聽位址（例如 0.0.0.0 或 127.0.0.1）。
    /// </summary>
    public string MetricsListenIp
    {
        get => configuration.GetConfigValue(OBSERVABILITY_METRICS_LISTEN_IP) ?? "0.0.0.0";
        set => configuration.SetConfigValue(OBSERVABILITY_METRICS_LISTEN_IP, value);
    }

    /// <summary>
    /// 取得或設定 Prometheus 監聽通訊埠（預設 9100）。
    /// </summary>
    public int MetricsPort
    {
        get => int.TryParse(configuration.GetConfigValue(OBSERVABILITY_METRICS_PORT), out int port) ? port : 9100;
        set => configuration.SetConfigValue(OBSERVABILITY_METRICS_PORT, value.ToString());
    }

    /// <summary>
    /// 取得或設定允許 Scraping 的監控伺服器 CIDR 網段清單（逗號分隔）。
    /// </summary>
    public string MetricsAllowedNetworks
    {
        get => configuration.GetConfigValue(OBSERVABILITY_METRICS_ALLOWED_NETWORKS) ?? string.Empty;
        set => configuration.SetConfigValue(OBSERVABILITY_METRICS_ALLOWED_NETWORKS, value);
    }

    /// <summary>
    /// 初始化 <see cref="NotificationSettings"/> class的新執行個體。
    /// </summary>
    public NotificationSettings(IddsConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        this.configuration = configuration;
    }
    /// <summary>
    /// 執行string to bool作業。
    /// </summary>
    /// <param name="value">要處理的value。</param>
    /// <returns>若作業成功傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    private static bool StringToBool(string value)
    {
        bool.TryParse(value, out bool result);
        return result;
    }
    /// <summary>
    /// 讀取持久化的報表傳送狀態，並安全處理缺失或無效的數值。
    /// </summary>
    /// <param name="key">報表狀態金鑰。</param>
    /// <returns>傳回解析後的狀態；若無效則傳回 <see cref="ReportDeliveryState.None"/>。</returns>
    private ReportDeliveryState GetReportState(string key) =>
        Enum.TryParse(configuration.GetConfigValue(key), ignoreCase: true, out ReportDeliveryState state) ? state : ReportDeliveryState.None;
    /// <summary>
    /// 執行reload作業。
    /// </summary>
    public void Reload() => configuration.LoadAppConfig();
    /// <summary>
    /// 儲存設定變更作業。
    /// </summary>
    public void Save() => configuration.SaveAppConfig();
}

/// <summary>
/// 代表支援的 Webhook 即時通知平台類型。
/// </summary>
public enum WebhookPlatform
{
    /// <summary>
    /// 未指定或停用。
    /// </summary>
    None = 0,

    /// <summary>
    /// Microsoft Teams（Adaptive Cards 1.6 格式）。
    /// </summary>
    MicrosoftTeams = 1,

    /// <summary>
    /// Slack（Block Kit 格式）。
    /// </summary>
    Slack = 2,

    /// <summary>
    /// Discord（Rich Embeds 格式）。
    /// </summary>
    Discord = 3,

    /// <summary>
    /// Telegram（Bot API sendMessage 格式）。
    /// </summary>
    Telegram = 4,

    /// <summary>
    /// 通用標準 JSON 格式。
    /// </summary>
    GenericJson = 5
}

/// <summary>
/// 代表 Syslog 網路傳輸協定類型。
/// </summary>
public enum SyslogProtocol
{
    /// <summary>
    /// UDP 協定（預設埠 514）。
    /// </summary>
    Udp = 0,

    /// <summary>
    /// TCP 協定（預設埠 514）。
    /// </summary>
    Tcp = 1,

    /// <summary>
    /// TLS 加密 TCP 協定（預設埠 6514）。
    /// </summary>
    Tls = 2
}

/// <summary>
/// 代表 Syslog 訊息格式標準。
/// </summary>
public enum SyslogFormat
{
    /// <summary>
    /// RFC 5424 標準結構化 Syslog 協定。
    /// </summary>
    Rfc5424 = 0,

    /// <summary>
    /// RFC 3164 傳統 BSD Syslog 協定。
    /// </summary>
    Rfc3164 = 1,

    /// <summary>
    /// Micro Focus ArcSight CEF (Common Event Format) 格式。
    /// </summary>
    Cef = 2
}
