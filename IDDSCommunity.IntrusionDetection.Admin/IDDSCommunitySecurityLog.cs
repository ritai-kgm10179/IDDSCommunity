using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunitySecurityLog : UserControl
{

    public const string ALL_AGENTS = "{46DD5CAD-3F50-4D69-8917-11505DB10553}";


    private DataSet? _intrusionLog;
    public DataSet DataSetIntrusionLog
    {
        get
        {
            if (_intrusionLog == null)
            {
                _intrusionLog = new DataSet();
                DataTable table = _intrusionLog.Tables.Add("IntrusionLog");
                table.Columns.Add("Id", typeof(int));
                table.Columns.Add("Action", typeof(int));
                table.Columns.Add("Agent", typeof(string));
                table.Columns.Add("LogIcon", typeof(Image));
                table.Columns.Add("LogType", typeof(string));
                table.Columns.Add("EventDate", typeof(DateTime));
                table.Columns.Add("IpAddress", typeof(string));
                table.Columns.Add("Message", typeof(string));
                table.Columns.Add("AgentId", typeof(string));
                table.Columns.Add("NumberOfEvents", typeof(int));
            }
            return _intrusionLog;
        }

        set => _intrusionLog = value;
    }

    private DataView? _intrusionLogView;
    public DataView IntrusionLogView
    {
        get
        {
            _intrusionLogView ??= new DataView(DataSetIntrusionLog.Tables["IntrusionLog"]!)
            {
                Sort = "EventDate desc"
            };
            return _intrusionLogView;
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="IDDSCommunitySecurityLog"/> class.
    /// </summary>

    public IDDSCommunitySecurityLog()
    {
        InitializeComponent();
        pictureBox2.Image = InterfaceIcons.CreateSecurityLog(Math.Min(pictureBox2.ClientSize.Width, pictureBox2.ClientSize.Height));
        comboBoxAgentSelection.DisplayMember = "DisplayName";
        comboBoxAgentSelection.ValueMember = "Id";
        comboBoxAgentSelection.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxAgentSelection.Items.Add(new AgentFilter(new Guid(ALL_AGENTS), Strings.Get("All Agents")));
        comboBoxAgentSelection.SelectedIndex = 0;
        comboBoxAgentSelection.SelectionChangeCommitted += new EventHandler(comboBoxAgentSelection_SelectionChangeCommitted);
        dataGridViewIntrusionLog.AutoGenerateColumns = false;
        dataGridViewIntrusionLog.DataSource = IntrusionLogView;
        //dataGridViewIntrusionLog.DataMember = "IntrusionLog";
        dataGridViewIntrusionLog.Columns["LogIcon"]!.DataPropertyName = "LogIcon";
        dataGridViewIntrusionLog.Columns["LogType"]!.DataPropertyName = "LogType";
        dataGridViewIntrusionLog.Columns["LatestEntry"]!.DataPropertyName = "EventDate";
        dataGridViewIntrusionLog.Columns["IpAddress"]!.DataPropertyName = "IpAddress";
        dataGridViewIntrusionLog.Columns["Agent"]!.DataPropertyName = "Message";
        dataGridViewIntrusionLog.Columns["AgentId"]!.DataPropertyName = "AgentId";
        dataGridViewIntrusionLog.Columns["NumberOfEvents"]!.DataPropertyName = "NumberOfEvents";

        PositionLabels();
    }

    /// <summary>
    /// Handles the filter selection changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void IDDSCommunitySecurityLog_FilterSelectionChanged(object? sender, EventArgs? e) => ApplyAdvancedFilter();

    /// <summary>
    /// 依據事件類型選取狀態、Agent 模組及關鍵字/CIDR 網段設定 DataView 的事件過濾條件。
    /// </summary>
    /// <param name="searchQuery">搜尋關鍵字或 CIDR 網段（如 192.168.1.0/24）。</param>
    public void ApplyAdvancedFilter(string? searchQuery = null)
    {
        List<string> filter = [];
        if (!checkBoxFailedLogins.Checked && !checkBoxHardLocks.Checked && !checkBoxSoftLocks.Checked && !checkBoxSystemMessages.Checked) filter.Add("0=1");
        if (checkBoxFailedLogins.Checked) filter.Add("(Action >99 and Action <200)");
        if (checkBoxSoftLocks.Checked) filter.Add("(Action >199 and Action <300)");
        if (checkBoxHardLocks.Checked) filter.Add("(Action >299 and Action <400)");
        if (checkBoxSystemMessages.Checked) filter.Add("(Action >= 500)");

        int i = 0;
        string viewFilter = filter.Count > 0 ? "(" : string.Empty;

        foreach (string f in filter)
        {
            if (i > 0) viewFilter += " or ";
            viewFilter += f;
            i++;
        }
        if (filter.Count > 0) viewFilter += ")";
        if (comboBoxAgentSelection.Text != null && comboBoxAgentSelection.SelectedItem is IAgentFilter filter2 && !filter2.Id.Equals(new Guid(ALL_AGENTS)))
        {
            viewFilter += (filter.Count > 0 ? " and " : "");
            viewFilter += string.Format("AgentId='{0}'", filter2.Id);
        }

        string query = searchQuery?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(query))
        {
            if (query.Contains('/') && CidrMatcher.TryMatchCidr(query, "127.0.0.1"))
            {
                DataTable table = DataSetIntrusionLog.Tables["IntrusionLog"]!;
                HashSet<string> matchedIps = new(StringComparer.Ordinal);
                foreach (DataRow row in table.Rows)
                {
                    string ip = row["IpAddress"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(ip) && CidrMatcher.TryMatchCidr(query, ip))
                    {
                        matchedIps.Add(ip);
                    }
                }
                if (matchedIps.Count > 0)
                {
                    string ipClause = string.Join(",", matchedIps.Select(ip => $"'{ip.Replace("'", "''")}'"));
                    viewFilter += (string.IsNullOrEmpty(viewFilter) ? "" : " and ") + $"IpAddress IN ({ipClause})";
                }
                else
                {
                    viewFilter = "0=1";
                }
            }
            else
            {
                string safeQuery = query.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]");
                viewFilter += (string.IsNullOrEmpty(viewFilter) ? "" : " and ") + $"(IpAddress LIKE '%{safeQuery}%' OR Message LIKE '%{safeQuery}%' OR Agent LIKE '%{safeQuery}%')";
            }
        }

        IntrusionLogView.RowFilter = viewFilter;
        labelEventsCount.Text = CountEvents().ToString();
    }

    /// <summary>
    /// Handles the selection change committed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    void comboBoxAgentSelection_SelectionChangeCommitted(object? sender, EventArgs? e)
    {

    }

    /// <summary>
    /// Adds log entry.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="action">The action value.</param>
    /// <param name="agentId">The agent id value.</param>
    /// <param name="logIcon">The log icon value.</param>
    /// <param name="logType">The log type value.</param>
    /// <param name="eventDate">The event date value.</param>
    /// <param name="ipAddress">The ip address value.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The add log entry result.</returns>

    public DataRow AddLogEntry(int id, int action, string agentId, Image logIcon, string logType, DateTime eventDate, string ipAddress, string message)
    {
        DataTable t = DataSetIntrusionLog.Tables["IntrusionLog"]
            ?? throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IntrusionLog table is not initialized."));
        DataRow row;
        DataRow[] rows = t.Select(string.Format("AgentId='{0}' and IpAddress='{1}' and logType='{2}' and action='{3}'", agentId, ipAddress, logType, action));
        if (rows != null && rows.Length > 0)
        {
            rows[0]["NumberOfEvents"] = int.Parse(rows[0]["NumberOfEvents"]?.ToString() ?? "0") + 1;
            rows[0]["EventDate"] = eventDate;
            row = rows[0];
        }
        else
        {
            row = t.Rows.Add(id, action,
                SecurityAgents.Instance.GetDisplayName(agentId), logIcon, logType, eventDate, ipAddress, message, agentId, 1);
        }
        labelEventsCount.Text = CountEvents().ToString();
        if (MaxLogId < id) MaxLogId = id;
        return row;
    }

    /// <summary>
    /// Executes the count events operation.
    /// </summary>
    /// <returns>The count events result.</returns>

    private int CountEvents()
    {
        int result = 0;
        foreach (DataGridViewRow row in dataGridViewIntrusionLog.Rows)
        {
            if (int.TryParse(row.Cells["NumberOfEvents"]?.Value?.ToString(), out int c))
            {
                result += c;
            }
        }
        return result;
    }

    /// <summary>
    /// Executes the fill log entry operation.
    /// </summary>
    /// <param name="maxId">The max id value.</param>
    /// <param name="action">The action value.</param>
    /// <param name="agentId">The agent id value.</param>
    /// <param name="logIcon">The log icon value.</param>
    /// <param name="logType">The log type value.</param>
    /// <param name="lastEventDate">The last event date value.</param>
    /// <param name="ipAddress">The ip address value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="numberOfEvents">The number of events value.</param>
    /// <returns>The fill log entry result.</returns>

    public DataRow FillLogEntry(int maxId, int action, string agentId, Image logIcon, string logType, DateTime lastEventDate, string ipAddress, string message, int numberOfEvents)
    {
        DataRow row = AddLogEntry(maxId, action, agentId, logIcon, logType, lastEventDate, ipAddress, message);
        row["NumberOfEvents"] = numberOfEvents;
        labelEventsCount.Text = CountEvents().ToString();
        return row;
    }

    public int MaxLogId { get; set; }

    /// <summary>
    /// Adds agent.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void AddAgent(SecurityAgent agent) => comboBoxAgentSelection.Items.Add(agent);

    /// <summary>
    /// Removes agent.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void RemoveAgent(SecurityAgent agent)
    {
        try
        {
            comboBoxAgentSelection.Items.Remove(agent);
        }
        catch
        {
            // not found
        }
    }

    /// <summary>
    /// Handles the resize event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void dataGridViewIntrusionLog_Resize(object? sender, EventArgs? e) => PositionLabels();

    /// <summary>
    /// Executes the position labels operation.
    /// </summary>

    private void PositionLabels()
    {
        smartLabelType.Left = 3;
        smartLabelLatestEntry.Left = smartLabelType.Left + dataGridViewIntrusionLog.Columns[0].Width + dataGridViewIntrusionLog.Columns[1].Width;
        smartLabelNumberOfEvents.Left = smartLabelLatestEntry.Left + dataGridViewIntrusionLog.Columns[2].Width;
        smartLabelpAddress.Left = smartLabelNumberOfEvents.Left + dataGridViewIntrusionLog.Columns[3].Width;
        smartLabelMessage.Left = smartLabelpAddress.Left + dataGridViewIntrusionLog.Columns[4].Width;
    }




}
