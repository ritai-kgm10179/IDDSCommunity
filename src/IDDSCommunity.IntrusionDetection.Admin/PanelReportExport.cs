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
    private readonly Label status = new();

    private System.Threading.CancellationTokenSource? statusCts;

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
            string html = await Task.Run(() => ReportGenerator.Instance.GetReport(
                Strings.Get("Security report"),
                Strings.Format("Report period: {0:d} - {1:d}", from, through),
                Strings.Format("Server: {0}", Dns.GetHostName()), from, endExclusive));
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

    private static Label CreateLabel(string text, float size, Color color, int x, int y) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", size),
        ForeColor = color,
        Location = new Point(x, y),
        Text = text
    };
}
