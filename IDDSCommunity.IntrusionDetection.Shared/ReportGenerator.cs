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
    /// Initializes a new instance of the <see cref="ReportGenerator"/> class.
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
    /// Executes the daily report operation.
    /// </summary>
    /// <returns>The daily report result.</returns>

    public static string DailyReport() => string.Empty;

    public long TotalIntrusionAttempts { get; set; }
    public long TotalSoftLocks { get; set; }
    public long TotalHardLocks { get; set; }

    /// <summary>
    /// Gets events per agent.
    /// </summary>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <returns>The get events per agent result.</returns>

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
    /// Gets incidents by ip.
    /// </summary>
    /// <param name="action">The action value.</param>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <returns>The get incidents by ip result.</returns>

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
    /// Gets incident by iptemplate.
    /// </summary>
    /// <returns>The get incident by iptemplate result.</returns>

    public static string GetIncidentByIPTemplate() => Resources.IntrusionAttemptsByIp;

    /// <summary>
    /// Gets events per agent template.
    /// </summary>
    /// <returns>The get events per agent template result.</returns>

    public static string GetEventsPerAgentTemplate() => Resources.EventsPerAgent;

    /// <summary>
    /// Sets events per agent.
    /// </summary>
    /// <param name="agentName">The agent name value.</param>
    /// <param name="intrusionAttempts">The intrusion attempts value.</param>
    /// <param name="softLocks">The soft locks value.</param>
    /// <param name="hardLocks">The hard locks value.</param>
    /// <returns>The set events per agent result.</returns>

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
    /// Gets report.
    /// </summary>
    /// <param name="title">The title value.</param>
    /// <param name="subtitle">The subtitle value.</param>
    /// <param name="installationInformation">The installation information value.</param>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <returns>The get report result.</returns>

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
    /// Replaces every user-facing report label with the selected application language.
    /// </summary>
    /// <param name="template">The invariant report template containing localization tokens.</param>
    /// <returns>The localized report template.</returns>
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
