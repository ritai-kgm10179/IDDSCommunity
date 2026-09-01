using System;
using System.Drawing;
using System.IO;
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
        Font sectionHeaderFont = new("Segoe UI", 10F, FontStyle.Bold);

        // Top Panel
        Panel topPanel = new()
        {
            Dock = DockStyle.Top,
            Height = 135,
            BackColor = Color.White
        };
        Controls.Add(topPanel);

        Label lblTitle = new()
        {
            Text = Strings.Get("CIS Windows Server Benchmark & Forensics"),
            Font = sectionHeaderFont,
            ForeColor = AccentColor,
            Location = new Point(20, 15),
            AutoSize = true
        };
        topPanel.Controls.Add(lblTitle);

        btnRunScan = new Button
        {
            Text = Strings.Get("Run CIS Benchmark Scan"),
            Location = new Point(20, 48),
            Size = new Size(180, 32),
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnRunScan.Click += (s, e) => RunCisScan();
        topPanel.Controls.Add(btnRunScan);

        btnExportReport = new Button
        {
            Text = Strings.Get("Export Report"),
            Location = new Point(210, 48),
            Size = new Size(180, 32),
            Font = defaultFont,
            Enabled = false
        };
        btnExportReport.Click += (s, e) => ExportReport();
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
        listChecks.Columns.Add("Status", 100);
        listChecks.Columns.Add("ID", 80);
        listChecks.Columns.Add("Category", 140);
        listChecks.Columns.Add("Title", 260);
        listChecks.Columns.Add("Current Value", 200);
        listChecks.Columns.Add("Remediation Advice", 300);

        Controls.Add(listChecks);
        listChecks.BringToFront();
    }

    [SupportedOSPlatform("windows")]
    private void RunCisScan()
    {
        btnRunScan.Enabled = false;
        try
        {
            latestResult = CisBenchmarkScanner.RunScan();
            listChecks.Items.Clear();

            foreach (var item in latestResult.Items)
            {
                var lvi = new ListViewItem(item.IsCompliant ? "PASS" : "FAIL")
                {
                    ForeColor = item.IsCompliant ? Color.DarkGreen : Color.Red
                };
                lvi.SubItems.Add(item.Id);
                lvi.SubItems.Add(item.Category);
                lvi.SubItems.Add(item.Title);
                lvi.SubItems.Add(item.CurrentValue);
                lvi.SubItems.Add(item.RemediationAdvice);
                listChecks.Items.Add(lvi);
            }

            lblScore.ForeColor = latestResult.ComplianceScore >= 80.0 ? Color.DarkGreen : Color.DarkOrange;
            lblScore.Text = $"{latestResult.ComplianceScore}% ({latestResult.PassedChecks}/{latestResult.TotalChecks})";
            btnExportReport.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRunScan.Enabled = true;
        }
    }

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
