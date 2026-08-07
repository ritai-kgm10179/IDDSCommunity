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
    private readonly Button? browseButton;

    internal PluginSettingEditor(string propertyName, string propertyType, string value, string agentName)
    {
        PropertyName = propertyName;
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(102, 102, 102);
        Margin = new Padding(0, 0, 0, 6);
        Size = new Size(390, 34);

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
                browseButton = new Button
                {
                    AccessibleName = Strings.Get("Browse"),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(362, 2),
                    Size = new Size(26, 24),
                    Text = "…",
                    UseVisualStyleBackColor = true
                };
                browseButton.Click += (_, _) => Browse(textBox, propertyName);
                Controls.Add(browseButton);
            }
        }
        Controls.Add(editor);
        SizeChanged += (_, _) => LayoutControls(label);
        LayoutControls(label);
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

    private void LayoutControls(Label label)
    {
        int editorHeight = Math.Max(editor.PreferredSize.Height, editor.Height);
        int desiredHeight = Math.Max(34, editorHeight + 10);
        if (Height != desiredHeight) Height = desiredHeight;

        int editorTop = Math.Max(0, (Height - editorHeight) / 2);
        if (editor is CheckBox)
        {
            editor.Location = new Point(Math.Max(0, Width - editor.Width - 2), Math.Max(0, (Height - editor.Height) / 2));
        }
        else if (browseButton is not null)
        {
            browseButton.Location = new Point(Math.Max(0, Width - browseButton.Width - 2), Math.Max(0, (Height - browseButton.Height) / 2));
            editor.Width = 120;
            editor.Location = new Point(Math.Max(0, browseButton.Left - editor.Width - 4), editorTop);
        }
        else
        {
            editor.Width = editor is NumericUpDown ? 120 : 150;
            editor.Location = new Point(Math.Max(0, Width - editor.Width - 2), editorTop);
        }

        label.Location = new Point(0, Math.Max(0, (Height - label.Height) / 2));
        label.Width = Math.Max(120, editor.Left - 10);
    }

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
