using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;
/// <summary>
/// Exports a localized security report for an administrator-selected interval.
/// </summary>
public sealed class PanelReportExport : UserControl
{
    private readonly DateTimePicker start = new() { Format = DateTimePickerFormat.Short, Width = 140 };
    private readonly DateTimePicker end = new() { Format = DateTimePickerFormat.Short, Width = 140 };
    private readonly Button export = new();
    private readonly Button exportIso = new();
    private readonly Button exportStix = new();
    private readonly Label status = new();

    private System.Threading.CancellationTokenSource? statusCts;

    /// <summary>
    /// 初始化 <see cref="PanelReportExport"/> 類別之新執行個體。
    /// </summary>
    public PanelReportExport()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        start.Value = DateTime.Today.AddDays(-30);
        end.Value = DateTime.Today;
        Controls.Add(CreateLabel(Strings.Get("Report export"), 11F, Color.FromArgb(19, 184, 166), 11, 8));
        Controls.Add(CreateLabel(Strings.Get("Export a localized HTML security report for the selected date range."), 9F, Color.FromArgb(102, 102, 102), 15, 43));
        Controls.Add(CreateLabel(Strings.Get("Start date"), 9F, Color.FromArgb(102, 102, 102), 15, 84));
        start.Location = new Point(130, 80);
        Controls.Add(start);
        Controls.Add(CreateLabel(Strings.Get("End date"), 9F, Color.FromArgb(102, 102, 102), 15, 120));
        end.Location = new Point(130, 116);
        Controls.Add(end);

        export.Text = Strings.Get("Export HTML report");
        export.Font = new Font("Segoe UI", 9F);
        export.Location = new Point(15, 160);
        export.Size = new Size(160, 30);
        export.Click += Export;
        Controls.Add(export);

        exportIso.Text = Strings.Get("Export ISO 27001 report");
        exportIso.Font = new Font("Segoe UI", 9F);
        exportIso.Location = new Point(185, 160);
        exportIso.Size = new Size(190, 30);
        exportIso.Click += ExportIso;
        Controls.Add(exportIso);

        exportStix.Text = Strings.Get("Export STIX 2.1 bundle");
        exportStix.Font = new Font("Segoe UI", 9F);
        exportStix.Location = new Point(385, 160);
        exportStix.Size = new Size(180, 30);
        exportStix.Click += ExportStix;
        Controls.Add(exportStix);

        status.AutoSize = false;
        status.Font = new Font("Segoe UI", 9F);
        status.ForeColor = Color.FromArgb(102, 102, 102);
        status.Location = new Point(15, 210);
        status.Size = new Size(620, 100);
        Controls.Add(status);
        VisibleChanged += (_, _) => ResetStatus();
    }

    private void ResetStatus()
    {
        statusCts?.Cancel();
        statusCts?.Dispose();
        statusCts = null;
        status.Text = string.Empty;
    }

    private void SetTransientStatus(string text, int delaySeconds = 5)
    {
        statusCts?.Cancel();
        statusCts?.Dispose();
        statusCts = new System.Threading.CancellationTokenSource();
        System.Threading.CancellationToken token = statusCts.Token;
        status.Text = text;
        _ = Task.Delay(TimeSpan.FromSeconds(delaySeconds), token).ContinueWith(t =>
        {
            if (!t.IsCanceled && !IsDisposed && IsHandleCreated)
            {
                if (InvokeRequired) BeginInvoke(new Action(() => { if (!IsDisposed) status.Text = string.Empty; }));
                else status.Text = string.Empty;
            }
        }, TaskScheduler.Default);
    }

    private async void Export(object? sender, EventArgs e)
    {
        DateTime from = start.Value.Date;
        DateTime through = end.Value.Date;
        DateTime endExclusive = through.AddDays(1);
        if (through < from)
        {
            SetTransientStatus(Strings.Get("The end date must not be earlier than the start date."));
            return;
        }

        using SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = "html",
            Filter = Strings.Get("HTML report (*.html)|*.html"),
            FileName = $"idds-community-report-{from:yyyyMMdd}-{through:yyyyMMdd}.html",
            RestoreDirectory = true,
            Title = Strings.Get("Export security report")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        export.Enabled = false;
        try
        {
            // IncidentTime 以 UTC 儲存；報表標題與檔名維持本機日期，僅查詢邊界轉換為 UTC。
            string html = await Task.Run(() => ReportGenerator.Instance.GetReport(
                Strings.Get("Security report"),
                Strings.Format("Report period: {0:d} - {1:d}", from, through),
                Strings.Format("Server: {0}", Dns.GetHostName()), from.ToUniversalTime(), endExclusive.ToUniversalTime()));
            await File.WriteAllTextAsync(dialog.FileName, html, new System.Text.UTF8Encoding(false));
            SetTransientStatus(Strings.Format("Report exported: {0}", dialog.FileName));
        }
        catch (Exception exception)
        {
            Trace.TraceError("Report export failed: {0}", exception);
            string? diagnosticPath = RollingDiagnosticLog.Write("Admin-ReportExport", "Report export failed", exception);
            string errorText = Strings.Get("Report export failed. Review the application log for details.");
            SetTransientStatus(errorText);
            string details = string.IsNullOrWhiteSpace(diagnosticPath)
                ? errorText
                : errorText + Environment.NewLine + diagnosticPath;
            MessageBox.Show(this, details, Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { export.Enabled = true; }
    }

    private async void ExportIso(object? sender, EventArgs e)
    {
        using SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = "html",
            Filter = Strings.Get("HTML report (*.html)|*.html"),
            FileName = $"idds-iso27001-compliance-report-{DateTime.Today:yyyyMMdd}.html",
            RestoreDirectory = true,
            Title = Strings.Get("Export ISO 27001 compliance report")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        exportIso.Enabled = false;
        try
        {
            string html = await Task.Run(() =>
            {
                var stats = new Shared.Reports.Iso27001ReportStats
                {
                    TotalBlockedIps = Locks.GetActiveLocks().Count,
                    ActiveFirewallRules = Locks.GetActiveLocks().Count,
                    ThreatFeedIndicatorsCount = 0,
                    HoneypotProbeCount = 0
                };
                return Shared.Reports.Iso27001ComplianceReportGenerator.GenerateHtmlReport(stats);
            });
            await File.WriteAllTextAsync(dialog.FileName, html, new System.Text.UTF8Encoding(false));
            SetTransientStatus(Strings.Format("Report exported: {0}", dialog.FileName));
        }
        catch (Exception exception)
        {
            Trace.TraceError("ISO 27001 report export failed: {0}", exception);
            SetTransientStatus(Strings.Get("Report export failed. Review the application log for details."));
        }
        finally { exportIso.Enabled = true; }
    }

    private async void ExportStix(object? sender, EventArgs e)
    {
        DateTime from = start.Value.Date;
        DateTime through = end.Value.Date;
        DateTime endExclusive = through.AddDays(1);

        using SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = "json",
            Filter = Strings.Get("STIX 2.1 JSON bundle (*.json)|*.json"),
            FileName = $"idds-stix21-threat-bundle-{DateTime.Today:yyyyMMdd}.json",
            RestoreDirectory = true,
            Title = Strings.Get("Export STIX 2.1 threat intelligence bundle")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        exportStix.Enabled = false;
        try
        {
            string json = await Task.Run(() =>
            {
                var activeLocks = Locks.GetActiveLocks();
                var items = new System.Collections.Generic.List<Shared.ThreatIntelligence.StixExportItem>();
                foreach (var l in activeLocks)
                {
                    items.Add(new Shared.ThreatIntelligence.StixExportItem
                    {
                        IpAddress = l.IpAddress ?? string.Empty,
                        EventTimeUtc = l.LockDate,
                        Description = $"Blocked by IDDS Community (Status: {l.Status})",
                        ConfidenceScore = 85,
                        AgentName = "IDDS Community"
                    });
                }
                return Shared.ThreatIntelligence.StixBundleExporter.ExportBundle(items);
            });
            await File.WriteAllTextAsync(dialog.FileName, json, new System.Text.UTF8Encoding(false));
            SetTransientStatus(Strings.Format("STIX 2.1 bundle exported: {0}", dialog.FileName));
        }
        catch (Exception exception)
        {
            Trace.TraceError("STIX 2.1 export failed: {0}", exception);
            SetTransientStatus(Strings.Get("Report export failed. Review the application log for details."));
        }
        finally { exportStix.Enabled = true; }
    }

    private static Label CreateLabel(string text, float size, Color color, int x, int y) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", size),
        ForeColor = color,
        Location = new Point(x, y),
        Text = text
    };
}
