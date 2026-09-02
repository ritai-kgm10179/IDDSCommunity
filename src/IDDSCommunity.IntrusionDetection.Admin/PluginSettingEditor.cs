using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 依據代理程式設定屬性之型別與用途，呈現合適之編輯器控制項。
/// </summary>
internal sealed class PluginSettingEditor : UserControl
{
    private readonly Control editor;
    private readonly Button? browseButton;
    private readonly Label label;

    /// <summary>
    /// 初始化 <see cref="PluginSettingEditor"/> 類別的新執行個體。
    /// </summary>
    /// <param name="propertyName">設定屬性名稱。</param>
    /// <param name="propertyType">屬性型別全名。</param>
    /// <param name="value">屬性初始字串值。</param>
    /// <param name="agentName">代理程式型別全名。</param>
    internal PluginSettingEditor(string propertyName, string propertyType, string value, string agentName)
    {
        PropertyName = propertyName;
        Font defaultFont = new("Segoe UI", 9F);
        Color defaultTextColor = Color.FromArgb(102, 102, 102);

        label = new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            Font = defaultFont,
            ForeColor = defaultTextColor,
            Location = new Point(0, 7),
            Size = new Size(230, 20),
            Text = Strings.Get(propertyName),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(label);

        if (string.Equals(propertyType, typeof(bool).FullName, StringComparison.Ordinal))
        {
            CheckBox checkBox = new()
            {
                Checked = bool.TryParse(value, out bool parsed) && parsed,
                Font = defaultFont,
                ForeColor = defaultTextColor,
                Location = new Point(366, 6),
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
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = defaultFont,
                ForeColor = defaultTextColor,
                Location = new Point(268, 3),
                Minimum = minimum,
                Maximum = maximum,
                Size = new Size(120, 23),
                ThousandsSeparator = false
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
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = defaultFont,
                ForeColor = defaultTextColor,
                Location = new Point(238, 3),
                Size = new Size(IsPathProperty(propertyName) ? 120 : 150, 23),
                Text = value
            };
            textBox.TextChanged += (_, _) => OnValueChanged();
            editor = textBox;

            if (IsPathProperty(propertyName))
            {
                browseButton = new Button
                {
                    AccessibleName = Strings.Get("Browse"),
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = defaultFont,
                    ForeColor = defaultTextColor,
                    Location = new Point(360, 3),
                    Size = new Size(28, 23),
                    Text = "…",
                    UseVisualStyleBackColor = false
                };
                browseButton.Click += (_, _) => Browse(textBox, propertyName);
                Controls.Add(browseButton);
            }
        }
        Controls.Add(editor);
        SizeChanged += (_, _) => LayoutControls();
        LayoutControls();
    }

    /// <summary>
    /// 當設定值變更時引發之事件。
    /// </summary>
    internal event EventHandler? ValueChanged;

    /// <summary>
    /// 取得設定屬性名稱。
    /// </summary>
    internal string PropertyName { get; }

    /// <summary>
    /// 取得目前編輯器中的設定值字串。
    /// </summary>
    internal string Value => editor switch
    {
        CheckBox checkBox => checkBox.Checked.ToString(CultureInfo.InvariantCulture),
        NumericUpDown numeric => decimal.ToInt32(numeric.Value).ToString(CultureInfo.InvariantCulture),
        TextBox textBox => textBox.Text,
        _ => string.Empty
    };

    /// <summary>
    /// 引發 <see cref="ValueChanged"/> 事件。
    /// </summary>
    private void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// 依據目前控制項寬度動態排版標籤與編輯器，防止文字碰撞或控制項壓縮破版。
    /// </summary>
    private void LayoutControls()
    {
        int editorHeight = Math.Max(editor.PreferredSize.Height, editor.Height);
        int desiredHeight = Math.Max(32, editorHeight + 6);
        if (Height != desiredHeight) Height = desiredHeight;

        int editorTop = Math.Max(0, (Height - editorHeight) / 2);
        int labelTop = Math.Max(0, (Height - label.Height) / 2);

        label.Location = new Point(4, labelTop);
        label.Size = new Size(250, 20);

        const int editorLeft = 260;
        if (editor is CheckBox)
        {
            editor.Location = new Point(editorLeft, editorTop);
            editor.Size = new Size(18, 18);
        }
        else if (browseButton is not null)
        {
            editor.Location = new Point(editorLeft, editorTop);
            editor.Size = new Size(220, 23);

            browseButton.Size = new Size(28, 23);
            browseButton.Location = new Point(editorLeft + editor.Width + 6, editorTop);
        }
        else if (editor is NumericUpDown)
        {
            editor.Location = new Point(editorLeft, editorTop);
            editor.Size = new Size(100, 23);
        }
        else
        {
            editor.Location = new Point(editorLeft, editorTop);
            editor.Size = new Size(254, 23);
        }
    }

    /// <summary>
    /// 判斷指定屬性名稱是否代表檔案或目錄路徑。
    /// </summary>
    /// <param name="propertyName">屬性名稱。</param>
    /// <returns>若為路徑屬性傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    private static bool IsPathProperty(string propertyName) =>
        propertyName.EndsWith("Path", StringComparison.Ordinal) ||
        propertyName.EndsWith("Directory", StringComparison.Ordinal) ||
        propertyName.EndsWith("DirectoryName", StringComparison.Ordinal);

    /// <summary>
    /// 開啟路徑瀏覽對話方塊以填入文字方塊。
    /// </summary>
    /// <param name="textBox">目標文字方塊。</param>
    /// <param name="propertyName">屬性名稱。</param>
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

    /// <summary>
    /// 取得特定數值屬性允許的最小值與最大值範圍。
    /// </summary>
    /// <param name="propertyName">屬性名稱。</param>
    /// <param name="agentName">代理程式名稱。</param>
    /// <returns>最小值與最大值之 Tuple。</returns>
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
