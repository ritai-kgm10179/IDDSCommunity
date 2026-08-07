using System;
using System.Data;
using System.Text;
using System.Net;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class ReportGenerator
{

    const string SELECT_BY_AGENT = @"SELECT a.DisplayName as AgentName, i.Action as Action, COUNT(*) as Incidents FROM IntrusionLog i INNER JOIN SecurityAgents a ON a.AgentId=i.AgentId WHERE IncidentTime>@p0 AND IncidentTime<@p1 GROUP BY a.DisplayName, i.Action ORDER BY 1";
    const string SELECT_BY_IP = @"SELECT ClientIP, COUNT(*) AS Incidents FROM IntrusionLog WHERE IncidentTime>@p0 AND IncidentTime<@p1 AND Action=@p2 GROUP BY ClientIp ORDER BY COUNT(*)";
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

    public long TotalIntrusionAttempts { get; set; }
    public long TotalSoftLocks { get; set; }
    public long TotalHardLocks { get; set; }
    /// <summary>
    /// 取得每個 Agent 的事件數。
    /// </summary>
    /// <param name="start">start參數。</param>
    /// <param name="end">end參數。</param>
    /// <returns>傳回get events per agent結果。</returns>

    public string GetEventsPerAgent(DateTime start, DateTime end)
    {
        IDataReader rdr = Database.Instance.ExecuteReader(SELECT_BY_AGENT, start, end);
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
            agent = Db.DbValueConverter.ToString(rdr["AgentName"]);
            long incidents = Db.DbValueConverter.ToInt64(rdr["Incidents"]);
            if (!agent.Equals(currentAgent) && hasValues)
            {
                sb.AppendLine(SetEventsPerAgent(currentAgent, intrusionAttempts, softLocks, hardLocks));
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
                    intrusionAttempts = incidents;
                    break;
                case IntrusionLog.STATUS_SOFT_LOCKED:
                    softLocks = incidents;
                    break;
                case IntrusionLog.STATUS_HARD_LOCKED:
                    hardLocks = incidents;
                    break;
            }
            hasValues = true;
        }
        if (hasValues)
        {
            sb.AppendLine(SetEventsPerAgent(agent, intrusionAttempts, softLocks, hardLocks));
        }
        rdr.Close();
        return sb.ToString();
    }
    /// <summary>
    /// 取得依 IP 分組的事件數。
    /// </summary>
    /// <param name="action">action參數。</param>
    /// <param name="start">start參數。</param>
    /// <param name="end">end參數。</param>
    /// <returns>傳回get incidents by ip結果。</returns>

    public string GetIncidentsByIP(int action, DateTime start, DateTime end)
    {
        IDataReader rdr = Database.Instance.ExecuteReader(SELECT_BY_IP, start, end, action);
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
    /// <param name="end">end參數。</param>
    /// <returns>傳回get report結果。</returns>

    public string GetReport(string title, string subtitle, string installationInformation, DateTime start, DateTime end)
    {
        string result = LocalizeReportTemplate(Resources.ReportTemplate);
        result = result.Replace("[%TITLE%]", WebUtility.HtmlEncode(title));
        result = result.Replace("[%SUBTITLE%]", WebUtility.HtmlEncode(subtitle));
        result = result.Replace("[%INSTALLATION_INFORMATION%]", installationInformation);

        result = result.Replace("[%EVENTS_PER_AGENT%]", GetEventsPerAgent(start, end));
        result = result.Replace("[%INTRUSION_ATTEMPTS_BY_IP%]", GetIncidentsByIP(IntrusionLog.STATUS_INTRUSION_ATTEMPT, start, end));
        result = result.Replace("[%SOFT_LOCKS_BY_IP%]", GetIncidentsByIP(IntrusionLog.STATUS_SOFT_LOCKED, start, end));
        result = result.Replace("[%HARD_LOCKS_BY_IP%]", GetIncidentsByIP(IntrusionLog.STATUS_HARD_LOCKED, start, end));
        result = result.Replace("[%TOTAL_INTRUSION_ATTEMPTS%]", TotalIntrusionAttempts.ToString());
        result = result.Replace("[%TOTAL_SOFT_LOCKS%]", TotalSoftLocks.ToString());
        result = result.Replace("[%TOTAL_HARD_LOCKS%]", TotalHardLocks.ToString());

        return result;
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
