using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供外部情資下載、動態 DNS、叢集同步與系統維護等內部作業稽核日誌檢視與匯出之面板。
/// </summary>
public sealed class PanelSystemOperationsLog : UserControl
{
    private readonly SmartPanel panelFilterBar;
    private readonly SmartPanel panelGridContainer;
    private readonly Label labelCategory;
    private readonly ComboBox comboBoxCategory;
    private readonly Label labelOutcome;
    private readonly ComboBox comboBoxOutcome;
    private readonly Label labelSearch;
    private readonly TextBox textBoxSearch;
    private readonly Button buttonRefresh;
    private readonly Button buttonExport;
    private readonly Label labelRecordCount;
    private readonly DataGridView dataGridViewLogs;

    private readonly SemaphoreSlim queryLock = new(1, 1);
    private CancellationTokenSource? currentQueryCts;

    /// <summary>
    /// 初始化 <see cref="PanelSystemOperationsLog"/> 類別之新執行個體。
    /// </summary>
    public PanelSystemOperationsLog()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        panelFilterBar = new SmartPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(24, 16),
            Size = new Size(810, 72),
            BackColor = Color.FromArgb(243, 246, 248),
            BorderColor = Color.FromArgb(191, 191, 191),
            PaintBorder = true
        };

        labelCategory = new Label
        {
            AutoSize = true,
            Location = new Point(12, 14),
            Text = Strings.Get("Event category:"),
            ForeColor = Color.FromArgb(102, 102, 102)
        };

        comboBoxCategory = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(90, 10),
            Size = new Size(180, 24)
        };
        PopulateCategories();
        comboBoxCategory.SelectedIndex = 0;
        comboBoxCategory.SelectedIndexChanged += (_, _) => _ = LoadLogsAsync();

        labelOutcome = new Label
        {
            AutoSize = true,
            Location = new Point(285, 14),
            Text = Strings.Get("Outcome:"),
            ForeColor = Color.FromArgb(102, 102, 102)
        };

        comboBoxOutcome = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(355, 10),
            Size = new Size(120, 24)
        };
        comboBoxOutcome.Items.Add(Strings.Get("All Outcomes"));
        comboBoxOutcome.Items.Add(Strings.Get("Succeeded"));
        comboBoxOutcome.Items.Add(Strings.Get("Failed"));
        comboBoxOutcome.SelectedIndex = 0;
        comboBoxOutcome.SelectedIndexChanged += (_, _) => _ = LoadLogsAsync();

        labelSearch = new Label
        {
            AutoSize = true,
            Location = new Point(12, 44),
            Text = Strings.Get("Search keyword:"),
            ForeColor = Color.FromArgb(102, 102, 102)
        };

        textBoxSearch = new TextBox
        {
            Location = new Point(115, 41),
            Size = new Size(250, 24)
        };
        textBoxSearch.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = LoadLogsAsync();
            }
        };

        buttonRefresh = new Button
        {
            Location = new Point(375, 40),
            Size = new Size(85, 26),
            Text = Strings.Get("Refresh"),
            UseVisualStyleBackColor = true,
            Cursor = Cursors.Hand
        };
        buttonRefresh.Click += (_, _) => _ = LoadLogsAsync();

        buttonExport = new Button
        {
            Location = new Point(468, 40),
            Size = new Size(95, 26),
            Text = Strings.Get("Export CSV"),
            UseVisualStyleBackColor = true,
            Cursor = Cursors.Hand
        };
        buttonExport.Click += (_, _) => ExportCsv();

        labelRecordCount = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(570, 44),
            Size = new Size(225, 20),
            Text = string.Format(Strings.Get("Total records: {0}"), 0),
            ForeColor = Color.FromArgb(128, 128, 128)
        };

        panelFilterBar.Controls.Add(labelCategory);
        panelFilterBar.Controls.Add(comboBoxCategory);
        panelFilterBar.Controls.Add(labelOutcome);
        panelFilterBar.Controls.Add(comboBoxOutcome);
        panelFilterBar.Controls.Add(labelSearch);
        panelFilterBar.Controls.Add(textBoxSearch);
        panelFilterBar.Controls.Add(buttonRefresh);
        panelFilterBar.Controls.Add(buttonExport);
        panelFilterBar.Controls.Add(labelRecordCount);

        panelGridContainer = new SmartPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(24, 96),
            Size = new Size(810, 400),
            BackColor = SystemColors.Window,
            BorderColor = Color.FromArgb(191, 191, 191),
            PaintBorder = true,
            Padding = new Padding(1)
        };

        dataGridViewLogs = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 32,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(235, 238, 240),
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false
        };

        typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(dataGridViewLogs, true, null);

        dataGridViewLogs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(243, 246, 248),
            ForeColor = Color.FromArgb(70, 70, 70),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };

        dataGridViewLogs.DefaultCellStyle = new DataGridViewCellStyle
        {
            ForeColor = Color.FromArgb(40, 40, 40),
            SelectionBackColor = Color.FromArgb(232, 242, 252),
            SelectionForeColor = Color.FromArgb(20, 20, 20),
            Font = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Padding(4, 2, 4, 2)
        };

        dataGridViewLogs.RowTemplate.Height = 26;

        BuildGridColumns();
        dataGridViewLogs.CellFormatting += DataGridViewLogs_CellFormatting;

        panelGridContainer.Controls.Add(dataGridViewLogs);

        Controls.Add(panelGridContainer);
        Controls.Add(panelFilterBar);
    }

    private void PopulateCategories()
    {
        comboBoxCategory.Items.Clear();
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("All Categories"), string.Empty));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Threat Intelligence Feeds"), "ThreatFeed."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Dynamic Bogon Prefixes"), "Bogon."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("GeoIP Database"), "GeoIp."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Dynamic DNS"), "DynamicDns."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Cluster Threat Sync"), "Cluster."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Firewall & Probation"), "Firewall."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Firewall Inbound Rules"), "Firewall.Rule"));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Database maintenance"), "Database."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("Service Runtime"), "Runtime."));
        comboBoxCategory.Items.Add(new CategoryItem(Strings.Get("System Reports"), "Report."));
    }

    private void BuildGridColumns()
    {
        dataGridViewLogs.Columns.Clear();

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTime",
            HeaderText = Strings.Get("Time"),
            Width = 145,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colCategory",
            HeaderText = Strings.Get("Event category:").TrimEnd('：', ':'),
            Width = 150,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colOutcome",
            HeaderText = Strings.Get("Outcome:").TrimEnd('：', ':'),
            Width = 90,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colActor",
            HeaderText = Strings.Get("Actor"),
            Width = 130,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSubject",
            HeaderText = Strings.Get("Target / Subject"),
            Width = 180,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        dataGridViewLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDetails",
            HeaderText = Strings.Get("Details"),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void DataGridViewLogs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        if (dataGridViewLogs.Columns[e.ColumnIndex].Name == "colOutcome" && e.Value is string outcome)
        {
            if (string.Equals(outcome, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle!.ForeColor = Color.FromArgb(22, 101, 52); // Dark green
                e.CellStyle.Font = new Font(dataGridViewLogs.Font, FontStyle.Bold);
            }
            else if (string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle!.ForeColor = Color.FromArgb(185, 28, 28); // Red
                e.CellStyle.Font = new Font(dataGridViewLogs.Font, FontStyle.Bold);
            }
        }
    }

    /// <summary>
    /// 以非同步方式依據目前選取條件載入並顯示系統作業稽核日誌。
    /// </summary>
    /// <returns>表示非同步載入作業之 Task。</returns>
    public async Task LoadLogsAsync()
    {
        if (!Database.Instance.IsConfigured)
            return;

        currentQueryCts?.Cancel();
        CancellationTokenSource cts = new();
        currentQueryCts = cts;

        await queryLock.WaitAsync(cts.Token).ConfigureAwait(false);
        try
        {
            string categoryPrefix = string.Empty;
            string outcomeFilter = string.Empty;
            string searchKeyword = string.Empty;

            Invoke(() =>
            {
                if (comboBoxCategory.SelectedItem is CategoryItem catItem)
                    categoryPrefix = catItem.Prefix;
                int outcomeIndex = comboBoxOutcome.SelectedIndex;
                if (outcomeIndex == 1) outcomeFilter = "Succeeded";
                else if (outcomeIndex == 2) outcomeFilter = "Failed";
                searchKeyword = textBoxSearch.Text.Trim();
                buttonRefresh.Enabled = false;
            });

            StringBuilder sql = new("SELECT Id, OccurredUtc, EventType, Outcome, Actor, Subject, Details FROM ProtectionAuditLog WHERE 1=1");
            List<object> parameters = [];
            int paramIndex = 0;

            if (!string.IsNullOrEmpty(categoryPrefix))
            {
                sql.Append($" AND EventType LIKE @p{paramIndex}");
                parameters.Add(categoryPrefix + "%");
                paramIndex++;
            }

            if (!string.IsNullOrEmpty(outcomeFilter))
            {
                sql.Append($" AND Outcome = @p{paramIndex}");
                parameters.Add(outcomeFilter);
                paramIndex++;
            }

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                sql.Append($" AND (Subject LIKE @p{paramIndex} OR Details LIKE @p{paramIndex} OR EventType LIKE @p{paramIndex})");
                parameters.Add("%" + searchKeyword + "%");
                paramIndex++;
            }

            sql.Append(" ORDER BY Id DESC LIMIT 1000");

            List<AuditDisplayRow> rows = [];
            using (IDataReader reader = Database.Instance.ExecuteReader(sql.ToString(), [.. parameters]))
            {
                while (reader.Read())
                {
                    if (cts.Token.IsCancellationRequested) return;

                    string occurredUtcStr = Shared.Db.DbValueConverter.ToString(reader["OccurredUtc"]);
                    DateTime localTime = DateTime.TryParse(occurredUtcStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed)
                        ? parsed.ToLocalTime()
                        : DateTime.MinValue;

                    rows.Add(new AuditDisplayRow(
                        localTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        Shared.Db.DbValueConverter.ToString(reader["EventType"]),
                        Shared.Db.DbValueConverter.ToString(reader["Outcome"]),
                        Shared.Db.DbValueConverter.ToString(reader["Actor"]),
                        Shared.Db.DbValueConverter.ToString(reader["Subject"]),
                        Shared.Db.DbValueConverter.ToString(reader["Details"])));
                }
            }

            if (cts.Token.IsCancellationRequested) return;

            Invoke(() =>
            {
                if (IsDisposed) return;
                dataGridViewLogs.SuspendLayout();
                dataGridViewLogs.Rows.Clear();
                foreach (AuditDisplayRow r in rows)
                {
                    dataGridViewLogs.Rows.Add(r.Time, r.EventType, r.Outcome, r.Actor, r.Subject, r.Details);
                }
                dataGridViewLogs.ResumeLayout();
                labelRecordCount.Text = string.Format(Strings.Get("Total records: {0}"), rows.Count);
                buttonRefresh.Enabled = true;
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Invoke(() =>
            {
                if (!IsDisposed)
                    buttonRefresh.Enabled = true;
            });
            _ = RollingDiagnosticLog.Write("PanelSystemOperationsLog", "Failed to load audit logs", ex);
        }
        finally
        {
            queryLock.Release();
        }
    }

    private void ExportCsv()
    {
        if (dataGridViewLogs.Rows.Count == 0)
        {
            MessageBox.Show(Strings.Get("No records available to export."), Strings.Get("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using SaveFileDialog sfd = new()
        {
            Title = Strings.Get("Export CSV"),
            Filter = Strings.Get("CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"),
            FileName = $"idds-system-log-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            StringBuilder sb = new();
            sb.AppendLine("\"Time\",\"EventType\",\"Outcome\",\"Actor\",\"Subject\",\"Details\"");
            foreach (DataGridViewRow row in dataGridViewLogs.Rows)
            {
                if (row.IsNewRow) continue;
                string time = EscapeCsv(row.Cells["colTime"].Value?.ToString() ?? string.Empty);
                string evt = EscapeCsv(row.Cells["colCategory"].Value?.ToString() ?? string.Empty);
                string outcome = EscapeCsv(row.Cells["colOutcome"].Value?.ToString() ?? string.Empty);
                string actor = EscapeCsv(row.Cells["colActor"].Value?.ToString() ?? string.Empty);
                string subject = EscapeCsv(row.Cells["colSubject"].Value?.ToString() ?? string.Empty);
                string details = EscapeCsv(row.Cells["colDetails"].Value?.ToString() ?? string.Empty);
                sb.AppendLine($"\"{time}\",\"{evt}\",\"{outcome}\",\"{actor}\",\"{subject}\",\"{details}\"");
            }

            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show(
                string.Format(Strings.Get("System operations and audit log exported: {0}"), Path.GetFileName(sfd.FileName)),
                Strings.Get("Information"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Strings.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string EscapeCsv(string text) => text.Replace("\"", "\"\"");

    private sealed record CategoryItem(string DisplayName, string Prefix)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record AuditDisplayRow(string Time, string EventType, string Outcome, string Actor, string Subject, string Details);
}
