using System;
using System.Data;
using System.Text;
using System.Net;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class ReportGenerator
{

    const string SELECT_BY_AGENT = @"SELECT AgentId AS RawAgentId, Action, COUNT(*) AS Incidents FROM IntrusionLog WHERE IncidentTime >= @p0 AND IncidentTime < @p1 AND Action IN (100, 110, 120, 210, 310) GROUP BY AgentId, Action ORDER BY AgentId, Action";
    const string SELECT_BY_IP = @"SELECT ClientIP, COUNT(*) AS Incidents FROM IntrusionLog WHERE IncidentTime >= @p0 AND IncidentTime < @p1 AND Action IN (@p2, @p3, @p4) GROUP BY ClientIP ORDER BY COUNT(*), ClientIP";
    private readonly object syncRoot = new();
    /// <summary>
    /// 初始化 <see cref="ReportGenerator"/> class的新執行個體。
    /// </summary>
    private ReportGenerator()
    {
    }

    private static ReportGenerator? _instance;
    public static ReportGenerator Instance
    {
        get
        {
            _instance ??= new ReportGenerator();
            return _instance;
        }
    }
    /// <summary>
    /// 執行daily report作業。
    /// </summary>
    /// <returns>傳回daily report結果。</returns>
    public static string DailyReport() => string.Empty;

    public long TotalIntrusionAttempts { get; private set; }
    public long TotalSoftLocks { get; private set; }
    public long TotalHardLocks { get; private set; }
    /// <summary>
    /// 取得每個 Agent 的事件數。
    /// </summary>
    /// <param name="start">start參數。</param>
    /// <param name="end">不包含在報表內的結束時間。</param>
    /// <returns>傳回get events per agent結果。</returns>
    public string GetEventsPerAgent(DateTime start, DateTime end)
    {
        lock (syncRoot)
        {
            return GetEventsPerAgentCore(start, end);
        }
    }

    private string GetEventsPerAgentCore(DateTime start, DateTime end)
    {
        using IDataReader rdr = Database.Instance.ExecuteReader(SELECT_BY_AGENT, start, end);
        string currentAgent = string.Empty;
        bool hasValues = false;
        StringBuilder sb = new();
        long intrusionAttempts = 0;
        long softLocks = 0;
        long hardLocks = 0;
        TotalIntrusionAttempts = 0;
        TotalSoftLocks = 0;
        TotalHardLocks = 0;
        string agent = string.Empty;
        while (rdr.Read())
        {
            int action = Db.DbValueConverter.ToInt(rdr["Action"]);
            string rawAgentId = Db.DbValueConverter.ToString(rdr["RawAgentId"]);
            agent = rawAgentId;

            string resolvedName = SecurityAgents.Instance.GetDisplayName(rawAgentId);
            if (!string.IsNullOrWhiteSpace(resolvedName) && !resolvedName.Contains("is not registered") && !resolvedName.Contains("尚未註冊"))
            {
                agent = resolvedName;
            }

            long incidents = Db.DbValueConverter.ToInt64(rdr["Incidents"]);
            if (!agent.Equals(currentAgent) && hasValues)
            {
                sb.AppendLine(SetEventsPerAgentCore(currentAgent, intrusionAttempts, softLocks, hardLocks));
                currentAgent = agent;
                intrusionAttempts = 0;
                softLocks = 0;
                hardLocks = 0;
            }
            else if (!hasValues)
            {
                currentAgent = agent;
            }
            switch (action)
            {
                case IntrusionLog.STATUS_INTRUSION_ATTEMPT:
                case IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL:
                case IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE:
                    intrusionAttempts += incidents;
                    break;
                case IntrusionLog.STATUS_SOFT_LOCKED:
                    softLocks += incidents;
                    break;
                case IntrusionLog.STATUS_HARD_LOCKED:
                    hardLocks += incidents;
                    break;
            }
            hasValues = true;
        }
        if (hasValues)
        {
            sb.AppendLine(SetEventsPerAgentCore(currentAgent, intrusionAttempts, softLocks, hardLocks));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 取得依 IP 分組的事件數。
    /// </summary>
    /// <param name="action">action參數。</param>
    /// <param name="start">start參數。</param>
    /// <param name="end">不包含在報表內的結束時間。</param>
    /// <returns>傳回get incidents by ip結果。</returns>
    public string GetIncidentsByIP(int action, DateTime start, DateTime end)
    {
        lock (syncRoot)
        {
            return GetIncidentsByIPCore(action, start, end);
        }
    }

    private static string GetIncidentsByIPCore(int action, DateTime start, DateTime end)
    {
        int secondAction = action == IntrusionLog.STATUS_INTRUSION_ATTEMPT ? IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_LOCAL : action;
        int thirdAction = action == IntrusionLog.STATUS_INTRUSION_ATTEMPT ? IntrusionLog.STATUS_INTRUSION_ATTEMPT_FROM_SAFE : action;
        using IDataReader rdr = Database.Instance.ExecuteReader(SELECT_BY_IP, start, end, action, secondAction, thirdAction);
        StringBuilder sb = new();
        while (rdr.Read())
        {
            string result = GetIncidentByIPTemplate();
            string ipAddress = Db.DbValueConverter.ToString(rdr["ClientIP"]);
            long incidents = Db.DbValueConverter.ToInt64(rdr["Incidents"]);
            result = result.Replace("[%IP_ADDRESS%]", WebUtility.HtmlEncode(ipAddress));
            result = result.Replace("[%INTRUSION_ATTEMPTS%]", incidents.ToString());
            sb.AppendLine(result);
        }
        return sb.ToString();
    }
    /// <summary>
    /// 取得依 IP 範本分組的事件數。
    /// </summary>
    /// <returns>傳回get incident by iptemplate結果。</returns>
    public static string GetIncidentByIPTemplate() => Resources.IntrusionAttemptsByIp;
    /// <summary>
    /// 取得依 Agent 範本分組的事件數。
    /// </summary>
    /// <returns>傳回get events per agent template結果。</returns>
    public static string GetEventsPerAgentTemplate() => Resources.EventsPerAgent;
    /// <summary>
    /// 設定每個 Agent 的事件數。
    /// </summary>
    /// <param name="agentName">agent name參數。</param>
    /// <param name="intrusionAttempts">intrusion attempts參數。</param>
    /// <param name="softLocks">soft locks參數。</param>
    /// <param name="hardLocks">hard locks參數。</param>
    /// <returns>傳回set events per agent結果。</returns>
    public string SetEventsPerAgent(string agentName, long intrusionAttempts, long softLocks, long hardLocks)
    {
        lock (syncRoot)
        {
            return SetEventsPerAgentCore(agentName, intrusionAttempts, softLocks, hardLocks);
        }
    }

    private string SetEventsPerAgentCore(string agentName, long intrusionAttempts, long softLocks, long hardLocks)
    {
        string result = GetEventsPerAgentTemplate().Replace("[%AGENT_NAME%]", WebUtility.HtmlEncode(agentName));
        result = result.Replace("[%INTRUSION_ATTEMPTS%]", intrusionAttempts.ToString());
        result = result.Replace("[%SOFT_LOCKS%]", softLocks.ToString());
        result = result.Replace("[%HARD_LOCKS%]", hardLocks.ToString());
        TotalIntrusionAttempts += intrusionAttempts;
        TotalSoftLocks += softLocks;
        TotalHardLocks += hardLocks;
        return result;
    }
    /// <summary>
    /// 取得報表內容。
    /// </summary>
    /// <param name="title">title參數。</param>
    /// <param name="subtitle">subtitle參數。</param>
    /// <param name="installationInformation">installation information參數。</param>
    /// <param name="start">start參數。</param>
    /// <param name="end">不包含在報表內的結束時間。</param>
    /// <returns>傳回get report結果。</returns>
    public string GetReport(string title, string subtitle, string installationInformation, DateTime start, DateTime end)
    {
        if (end <= start) throw new ArgumentOutOfRangeException(nameof(end), Strings.Get("The report end time must be later than the start time."));
        lock (syncRoot)
        {
            string result = LocalizeReportTemplate(Resources.ReportTemplate);
            result = result.Replace("[%TITLE%]", WebUtility.HtmlEncode(title));
            result = result.Replace("[%SUBTITLE%]", WebUtility.HtmlEncode(subtitle));
            result = result.Replace("[%INSTALLATION_INFORMATION%]", installationInformation);

            result = result.Replace("[%EVENTS_PER_AGENT%]", GetEventsPerAgentCore(start, end));
            result = result.Replace("[%INTRUSION_ATTEMPTS_BY_IP%]", GetIncidentsByIPCore(IntrusionLog.STATUS_INTRUSION_ATTEMPT, start, end));
            result = result.Replace("[%SOFT_LOCKS_BY_IP%]", GetIncidentsByIPCore(IntrusionLog.STATUS_SOFT_LOCKED, start, end));
            result = result.Replace("[%HARD_LOCKS_BY_IP%]", GetIncidentsByIPCore(IntrusionLog.STATUS_HARD_LOCKED, start, end));
            result = result.Replace("[%TOTAL_INTRUSION_ATTEMPTS%]", TotalIntrusionAttempts.ToString());
            result = result.Replace("[%TOTAL_SOFT_LOCKS%]", TotalSoftLocks.ToString());
            result = result.Replace("[%TOTAL_HARD_LOCKS%]", TotalHardLocks.ToString());

            return result;
        }
    }
    /// <summary>
    /// 使用選取的應用程式語言替換每個使用者介面報表標籤。
    /// </summary>
    /// <param name="template">包含在地化標記的不變報表範本。</param>
    /// <returns>傳回在地化報表範本。</returns>
    internal static string LocalizeReportTemplate(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return template
            .Replace("[%LABEL_HEADER_APP_TITLE%]", WebUtility.HtmlEncode(Strings.AppTitle), StringComparison.Ordinal)
            .Replace("[%LABEL_INSTALLATION_INFORMATION%]", WebUtility.HtmlEncode(Strings.Get("Installation information")), StringComparison.Ordinal)
            .Replace("[%LABEL_EVENTS_PER_AGENT%]", WebUtility.HtmlEncode(Strings.Get("Events per agent")), StringComparison.Ordinal)
            .Replace("[%LABEL_AGENT_NAME%]", WebUtility.HtmlEncode(Strings.Get("Agent name")), StringComparison.Ordinal)
            .Replace("[%LABEL_INTRUSION_ATTEMPTS%]", WebUtility.HtmlEncode(Strings.Get("Intrusion attempts")), StringComparison.Ordinal)
            .Replace("[%LABEL_SOFT_LOCKS%]", WebUtility.HtmlEncode(Strings.Get("Soft locks")), StringComparison.Ordinal)
            .Replace("[%LABEL_HARD_LOCKS%]", WebUtility.HtmlEncode(Strings.Get("Hard locks")), StringComparison.Ordinal)
            .Replace("[%LABEL_TOTAL%]", WebUtility.HtmlEncode(Strings.Get("Total")), StringComparison.Ordinal)
            .Replace("[%LABEL_INTRUSION_ATTEMPTS_BY_IP%]", WebUtility.HtmlEncode(Strings.Get("Intrusion attempts by IP address")), StringComparison.Ordinal)
            .Replace("[%LABEL_SOFT_LOCKS_BY_IP%]", WebUtility.HtmlEncode(Strings.Get("Soft locks by IP address")), StringComparison.Ordinal)
            .Replace("[%LABEL_HARD_LOCKS_BY_IP%]", WebUtility.HtmlEncode(Strings.Get("Hard locks by IP address")), StringComparison.Ordinal)
            .Replace("[%LABEL_CLIENT_IP%]", WebUtility.HtmlEncode(Strings.Get("Client IP")), StringComparison.Ordinal)
            .Replace("[%LABEL_REPORT_CONFIGURATION_HINT%]", WebUtility.HtmlEncode(Strings.Get("To configure reporting options, use the IDDS administration software on the server.")), StringComparison.Ordinal);
    }

}
