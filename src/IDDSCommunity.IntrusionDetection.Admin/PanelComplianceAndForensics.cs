using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Compliance;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供 CIS Windows Server 安全基準合規掃描與取證評估面板。
/// </summary>
public sealed class PanelComplianceAndForensics : UserControl
{
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);

    private readonly Button btnRunScan;
    private readonly Label lblScore;
    private readonly ListView listChecks;
    private readonly Button btnExportReport;

    private CisBenchmarkResult? latestResult;

    /// <summary>
    /// 初始化 <see cref="PanelComplianceAndForensics"/> 類別的新執行個體。
    /// </summary>
    public PanelComplianceAndForensics()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;

        Font defaultFont = new("Segoe UI", 9F);
        Font headerFont = new("Segoe UI", 11F, FontStyle.Bold);

        // Top Control Panel
        Panel topPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 130,
            Padding = new Padding(20, 15, 20, 10)
        };
        Controls.Add(topPanel);

        Label title = new()
        {
            Text = Strings.Get("CIS Windows Server Benchmark & Forensics"),
            Font = headerFont,
            ForeColor = AccentColor,
            Location = new Point(20, 15),
            AutoSize = true
        };
        topPanel.Controls.Add(title);

        btnRunScan = new Button
        {
            Text = Strings.Get("Run CIS Benchmark Scan"),
            Location = new Point(20, 48),
            Size = new Size(160, 32),
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont
        };
        btnRunScan.Click += (_, _) => RunCisScan();
        topPanel.Controls.Add(btnRunScan);

        btnExportReport = new Button
        {
            Text = Strings.Get("Export Report"),
            Location = new Point(190, 48),
            Size = new Size(160, 32),
            BackColor = Color.White,
            ForeColor = BodyTextColor,
            FlatStyle = FlatStyle.Flat,
            Font = defaultFont,
            Enabled = false
        };
        btnExportReport.Click += (_, _) => ExportReport();
        topPanel.Controls.Add(btnExportReport);

        lblScore = new Label
        {
            Text = Strings.Get("Scan not executed"),
            Location = new Point(20, 92),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = BodyTextColor
        };
        topPanel.Controls.Add(lblScore);

        // ListView
        listChecks = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = defaultFont
        };
        listChecks.Columns.Add(Strings.Get("status"), 100);
        listChecks.Columns.Add(Strings.Get("Check ID"), 80);
        listChecks.Columns.Add(Strings.Get("Category"), 140);
        listChecks.Columns.Add(Strings.Get("Title"), 260);
        listChecks.Columns.Add(Strings.Get("Current Value"), 200);
        listChecks.Columns.Add(Strings.Get("Remediation Advice"), 300);

        Controls.Add(listChecks);
        listChecks.BringToFront();
        listChecks.Resize += (_, _) => AutoResizeListViewColumns();
        AutoResizeListViewColumns();
    }

    private bool _isResizingColumns;

    /// <summary>
    /// 自動計算並最佳化 ListView 各欄位寬度，使其符合內容與標頭尺寸並自適應容器寬度。
    /// </summary>
    public void AutoResizeListViewColumns()
    {
        if (_isResizingColumns || listChecks == null || listChecks.Columns.Count == 0) return;
        _isResizingColumns = true;
        listChecks.SuspendLayout();
        try
        {
            if (listChecks.Items.Count > 0)
            {
                listChecks.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                for (int i = 0; i < listChecks.Columns.Count; i++)
                {
                    int contentWidth = listChecks.Columns[i].Width;
                    listChecks.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.HeaderSize);
                    int headerWidth = listChecks.Columns[i].Width;
                    listChecks.Columns[i].Width = Math.Max(contentWidth, headerWidth) + 16;
                }
            }
            else
            {
                listChecks.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                for (int i = 0; i < listChecks.Columns.Count; i++)
                {
                    listChecks.Columns[i].Width += 20;
                }
            }

            int totalWidth = 0;
            for (int i = 0; i < listChecks.Columns.Count; i++)
            {
                totalWidth += listChecks.Columns[i].Width;
            }
            int availableWidth = listChecks.ClientSize.Width;
            if (availableWidth > totalWidth && listChecks.Columns.Count >= 6)
            {
                int remaining = availableWidth - totalWidth;
                int col4Share = remaining / 3;
                int col5Share = remaining - col4Share;
                listChecks.Columns[4].Width += col4Share;
                listChecks.Columns[5].Width += col5Share;
            }
        }
        finally
        {
            listChecks.ResumeLayout();
            _isResizingColumns = false;
        }
    }

    /// <inheritdoc/>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AutoResizeListViewColumns();
    }

    /// <summary>
    /// 執行本機 CIS 安全基準合規掃描並更新清單顯示。
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal void RunCisScan()
    {
        btnRunScan.Enabled = false;
        try
        {
            latestResult = CisBenchmarkScanner.RunScan();
            listChecks.Items.Clear();

            var sortedItems = latestResult.Items
                .OrderBy(item => GetCategoryOrder(item.Category))
                .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var item in sortedItems)
            {
                string statusText = item.IsCompliant ? Strings.Get("PASS") : Strings.Get("FAIL");
                var lvi = new ListViewItem(statusText)
                {
                    ForeColor = item.IsCompliant ? Color.DarkGreen : Color.Red
                };
                lvi.SubItems.Add(item.Id);
                lvi.SubItems.Add(Strings.Get(item.Category));
                lvi.SubItems.Add(Strings.Get(item.Title));
                lvi.SubItems.Add(Strings.Get(item.CurrentValue));
                lvi.SubItems.Add(Strings.Get(item.RemediationAdvice));
                listChecks.Items.Add(lvi);
            }

            lblScore.ForeColor = latestResult.ComplianceScore >= 80.0 ? Color.DarkGreen : Color.DarkOrange;
            lblScore.Text = $"{latestResult.ComplianceScore}% ({latestResult.PassedChecks}/{latestResult.TotalChecks})";
            btnExportReport.Enabled = true;
            AutoResizeListViewColumns();
        }
        catch (Exception ex)
        {
            if (Environment.GetEnvironmentVariable("IDDS_TEST_MODE") != "1")
            {
                MessageBox.Show(ex.Message, Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            btnRunScan.Enabled = true;
        }
    }

    private static int GetCategoryOrder(string category) => category switch
    {
        "Account Policy" => 10,
        "Network Policy" => 20,
        "Firewall" => 30,
        "Audit Policy" => 40,
        "Application Security" => 50,
        _ => 90
    };

    private void ExportReport()
    {
        if (latestResult == null) return;

        using SaveFileDialog dlg = new()
        {
            Filter = "JSON report (*.json)|*.json|Text report (*.txt)|*.txt",
            FileName = $"IDDS_CIS_Benchmark_{Environment.MachineName}_{DateTime.UtcNow:yyyyMMdd}.json"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            string json = JsonSerializer.Serialize(latestResult, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dlg.FileName, json);
            MessageBox.Show(Strings.Get("Configuration was saved successfully."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
