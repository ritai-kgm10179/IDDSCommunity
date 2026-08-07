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
    /// 初始化 <see cref="IDDSCommunitySecurityLog"/> 類別的新執行個體。
    /// </summary>

    private readonly System.Windows.Forms.Timer searchDebounceTimer = new() { Interval = 250 };
    private string? pendingSearchQuery;

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
        EnableDoubleBuffering(dataGridViewIntrusionLog);
        dataGridViewIntrusionLog.DataSource = IntrusionLogView;
        //dataGridViewIntrusionLog.DataMember = "IntrusionLog";
        dataGridViewIntrusionLog.Columns["LogIcon"]!.DataPropertyName = "LogIcon";
        dataGridViewIntrusionLog.Columns["LogType"]!.DataPropertyName = "LogType";
        dataGridViewIntrusionLog.Columns["LatestEntry"]!.DataPropertyName = "EventDate";
        dataGridViewIntrusionLog.Columns["IpAddress"]!.DataPropertyName = "IpAddress";
        dataGridViewIntrusionLog.Columns["Agent"]!.DataPropertyName = "Message";
        dataGridViewIntrusionLog.Columns["AgentId"]!.DataPropertyName = "AgentId";
        dataGridViewIntrusionLog.Columns["NumberOfEvents"]!.DataPropertyName = "NumberOfEvents";

        searchDebounceTimer.Tick += (_, _) =>
        {
            searchDebounceTimer.Stop();
            ExecuteAdvancedFilter(pendingSearchQuery);
        };

        PositionLabels();
    }

    private static void EnableDoubleBuffering(Control control)
    {
        System.Reflection.PropertyInfo? property = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        property?.SetValue(control, true, null);
    }
    /// <summary>
    /// 處理 filter selection changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void IDDSCommunitySecurityLog_FilterSelectionChanged(object? sender, EventArgs? e) => ApplyAdvancedFilter();
    /// <summary>
    /// 依據事件類型選取狀態、Agent 模組及關鍵字/CIDR 網段設定 DataView 的事件過濾條件（附帶 250ms 防抖遲延）。
    /// </summary>
    /// <param name="searchQuery">搜尋關鍵字或 CIDR 網段（如 192.168.1.0/24）。</param>
    public void ApplyAdvancedFilter(string? searchQuery = null)
    {
        pendingSearchQuery = searchQuery;
        searchDebounceTimer.Stop();
        searchDebounceTimer.Start();
    }

    private void ExecuteAdvancedFilter(string? searchQuery)
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
                HashSet<string> distinctIps = new(StringComparer.Ordinal);
                foreach (DataRow row in table.Rows)
                {
                    string ip = row["IpAddress"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(ip)) distinctIps.Add(ip);
                }

                List<string> matchedIps = [];
                foreach (string ip in distinctIps)
                {
                    if (CidrMatcher.TryMatchCidr(query, ip)) matchedIps.Add(ip);
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
    /// 處理 selection change committed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    void comboBoxAgentSelection_SelectionChangeCommitted(object? sender, EventArgs? e)
    {

    }
    /// <summary>
    /// Adds log entry.
    /// </summary>
    /// <param name="id">id 的值。</param>
    /// <param name="action">action 的值。</param>
    /// <param name="agentId">agent id 的值。</param>
    /// <param name="logIcon">log icon 的值。</param>
    /// <param name="logType">log type 的值。</param>
    /// <param name="eventDate">event date 的值。</param>
    /// <param name="ipAddress">ip address 的值。</param>
    /// <param name="message">message 的值。</param>
    /// <returns>新增日誌紀錄的 DataRow 傳回結果。</returns>

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
    /// 執行 count events 作業。
    /// </summary>
    /// <returns>計算所得的事件總數。</returns>

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
    /// 執行 fill log entry 作業。
    /// </summary>
    /// <param name="maxId">max id 的值。</param>
    /// <param name="action">action 的值。</param>
    /// <param name="agentId">agent id 的值。</param>
    /// <param name="logIcon">log icon 的值。</param>
    /// <param name="logType">log type 的值。</param>
    /// <param name="lastEventDate">last event date 的值。</param>
    /// <param name="ipAddress">ip address 的值。</param>
    /// <param name="message">message 的值。</param>
    /// <param name="numberOfEvents">number of events 的值。</param>
    /// <returns>填入日誌紀錄的 DataRow 傳回結果。</returns>

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
    /// <param name="agent">agent 的值。</param>

    public void AddAgent(SecurityAgent agent) => comboBoxAgentSelection.Items.Add(agent);
    /// <summary>
    /// Removes agent.
    /// </summary>
    /// <param name="agent">agent 的值。</param>

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
    /// 處理 resize 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>

    private void dataGridViewIntrusionLog_Resize(object? sender, EventArgs? e) => PositionLabels();
    /// <summary>
    /// 執行 position labels 作業。
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
