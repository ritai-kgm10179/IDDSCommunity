using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// Presents one Agent setting with an editor appropriate for its declared property type and purpose.
/// </summary>
internal sealed class PluginSettingEditor : UserControl
{
    private readonly Control editor;

    internal PluginSettingEditor(string propertyName, string propertyType, string value, string agentName)
    {
        PropertyName = propertyName;
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(102, 102, 102);
        Margin = new Padding(0, 0, 0, 5);
        Size = new Size(390, 30);

        Label label = new()
        {
            AutoEllipsis = true,
            Font = Font,
            ForeColor = ForeColor,
            Location = new Point(0, 6),
            Size = new Size(255, 20),
            Text = Strings.Get(propertyName)
        };
        Controls.Add(label);

        if (string.Equals(propertyType, typeof(bool).FullName, StringComparison.Ordinal))
        {
            CheckBox checkBox = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Checked = bool.TryParse(value, out bool parsed) && parsed,
                Location = new Point(370, 6),
                Size = new Size(18, 18),
                UseVisualStyleBackColor = true
            };
            checkBox.CheckedChanged += (_, _) => OnValueChanged();
            editor = checkBox;
        }
        else if (string.Equals(propertyType, typeof(int).FullName, StringComparison.Ordinal))
        {
            (decimal minimum, decimal maximum) = GetNumericRange(propertyName, agentName);
            NumericUpDown numeric = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(268, 3),
                Minimum = minimum,
                Maximum = maximum,
                Size = new Size(120, 23),
                ThousandsSeparator = true
            };
            if (decimal.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal parsed))
                numeric.Value = Math.Clamp(parsed, minimum, maximum);
            numeric.ValueChanged += (_, _) => OnValueChanged();
            editor = numeric;
        }
        else
        {
            TextBox textBox = new()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(238, 3),
                Size = new Size(IsPathProperty(propertyName) ? 120 : 150, 23),
                Text = value
            };
            textBox.TextChanged += (_, _) => OnValueChanged();
            editor = textBox;

            if (IsPathProperty(propertyName))
            {
                textBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                Button browse = new()
                {
                    AccessibleName = Strings.Get("Browse"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(362, 2),
                    Size = new Size(26, 24),
                    Text = "…",
                    UseVisualStyleBackColor = true
                };
                browse.Click += (_, _) => Browse(textBox, propertyName);
                Controls.Add(browse);
            }
        }
        Controls.Add(editor);
        SizeChanged += (_, _) => label.Width = Math.Max(120, Width - (editor is CheckBox ? 40 : 165));
        label.Width = Math.Max(120, Width - (editor is CheckBox ? 40 : 165));
    }

    internal event EventHandler? ValueChanged;
    internal string PropertyName { get; }

    internal string Value => editor switch
    {
        CheckBox checkBox => checkBox.Checked.ToString(CultureInfo.InvariantCulture),
        NumericUpDown numeric => decimal.ToInt32(numeric.Value).ToString(CultureInfo.InvariantCulture),
        TextBox textBox => textBox.Text,
        _ => string.Empty
    };

    private void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    private static bool IsPathProperty(string propertyName) =>
        propertyName.EndsWith("Path", StringComparison.Ordinal) ||
        propertyName.EndsWith("Directory", StringComparison.Ordinal) ||
        propertyName.EndsWith("DirectoryName", StringComparison.Ordinal);

    private static void Browse(TextBox textBox, string propertyName)
    {
        if (propertyName.EndsWith("FilePath", StringComparison.Ordinal))
        {
            using OpenFileDialog dialog = new() { CheckFileExists = true, FileName = textBox.Text, RestoreDirectory = true };
            if (dialog.ShowDialog() == DialogResult.OK) textBox.Text = dialog.FileName;
            return;
        }

        using FolderBrowserDialog folder = new() { InitialDirectory = textBox.Text, ShowNewFolderButton = false };
        if (folder.ShowDialog() == DialogResult.OK) textBox.Text = folder.SelectedPath;
    }

    private static (decimal Minimum, decimal Maximum) GetNumericRange(string propertyName, string agentName)
    {
        if (propertyName.EndsWith("Port", StringComparison.Ordinal)) return (1, 65535);
        return propertyName switch
        {
            "WindowSeconds" when agentName.Contains("WindowsDns", StringComparison.Ordinal) => (1, 3600),
            "WindowSeconds" => (10, 86400),
            "FailureThreshold" => (2, 100000),
            "MaximumTrackedSources" or "MaximumTrackedClients" => (100, 1000000),
            "SourceStateRetentionSeconds" => (10, 604800),
            _ when propertyName.EndsWith("Threshold", StringComparison.Ordinal) => (1, 1000000),
            _ => (0, int.MaxValue)
        };
    }
}
