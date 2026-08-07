namespace IDDSCommunity.IntrusionDetection.Shared;

using System;

public class NotificationSettings
{
    private readonly IddsConfig configuration;
    public const string NOTIFICATION_ON_UNLOCK = "{BE227461-8622-4168-A15C-644773070A5D}";
    public const string NOTIFICATION_ON_SOFT_LOCK = "{C4A9EC33-44E0-445A-8134-009F559E22A1}";
    public const string NOTIFICATION_ON_HARD_LOCK = "{AD963FA3-1D19-4CFE-9F6A-C1A8A4DBBA33}";
    public const string NOTIFICATION_SUMMARY_REPORT = "{AADCD484-483A-4CAB-AA91-8C83942AE3AB}";
    public const string NOTIFICATION_SUMMARY_REPORT_DAILY = "{92993EF9-5C9E-4E58-9250-F2EF1E838914}";
    public const string NOTIFICATION_SUMMARY_REPORT_WEEKLY = "{06191D72-17B5-488F-BB59-F6E61B4C4B42}";
    public const string NOTIFICATION_SUMMARY_REPORT_MONTHLY = "{8B0995D6-6820-4FBE-9983-4A9533FB0BFD}";
    public const string LAST_DAILY_REPORT = "{78F53752-3167-4A81-BBC2-1CEFAF3211CE}";
    public const string LAST_WEEKLY_REPORT = "{10C8A9BC-A1CA-4BD5-818C-39A4696E9C80}";
    public const string LAST_MONTHLY_REPORT = "{4D3BC893-8C13-41ED-BEF8-35BEB768C7E8}";
    public const string DAILY_REPORT_STATE = "Reports.Daily.State";
    public const string WEEKLY_REPORT_STATE = "Reports.Weekly.State";
    public const string MONTHLY_REPORT_STATE = "Reports.Monthly.State";

    private static NotificationSettings? _instance;

    public static NotificationSettings Instance
    {
        get
        {
            _instance ??= new NotificationSettings(IddsConfig.Instance);
            return _instance;
        }
    }


    public bool OnUnlock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ON_UNLOCK)); set => configuration.SetConfigValue(NOTIFICATION_ON_UNLOCK, value.ToString());
    }



    public bool OnSoftLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ON_SOFT_LOCK)); set => configuration.SetConfigValue(NOTIFICATION_ON_SOFT_LOCK, value.ToString());
    }

    public bool OnHardLock
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_ON_HARD_LOCK)); set => configuration.SetConfigValue(NOTIFICATION_ON_HARD_LOCK, value.ToString());
    }
    public bool SummaryReport
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT, value.ToString());
    }
    public bool SummaryReportDaily
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT_DAILY)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT_DAILY, value.ToString());
    }

    public bool SummaryReportWeekly
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT_WEEKLY)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT_WEEKLY, value.ToString());
    }

    public bool SummaryReportMonthly
    {
        get => StringToBool(configuration.GetConfigValue(NOTIFICATION_SUMMARY_REPORT_MONTHLY)); set => configuration.SetConfigValue(NOTIFICATION_SUMMARY_REPORT_MONTHLY, value.ToString());
    }


    public string LastDailyReport
    {
        get => configuration.GetConfigValue(LAST_DAILY_REPORT); set => configuration.SetConfigValue(LAST_DAILY_REPORT, value);
    }

    public string LastWeeklyReport
    {
        get => configuration.GetConfigValue(LAST_WEEKLY_REPORT); set => configuration.SetConfigValue(LAST_WEEKLY_REPORT, value);
    }

    public string LastMonthlyReport
    {
        get => configuration.GetConfigValue(LAST_MONTHLY_REPORT); set => configuration.SetConfigValue(LAST_MONTHLY_REPORT, value);
    }

    public ReportDeliveryState DailyReportState
    {
        get => GetReportState(DAILY_REPORT_STATE);
        set => configuration.SetConfigValue(DAILY_REPORT_STATE, value.ToString());
    }

    public ReportDeliveryState WeeklyReportState
    {
        get => GetReportState(WEEKLY_REPORT_STATE);
        set => configuration.SetConfigValue(WEEKLY_REPORT_STATE, value.ToString());
    }

    public ReportDeliveryState MonthlyReportState
    {
        get => GetReportState(MONTHLY_REPORT_STATE);
        set => configuration.SetConfigValue(MONTHLY_REPORT_STATE, value.ToString());
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
