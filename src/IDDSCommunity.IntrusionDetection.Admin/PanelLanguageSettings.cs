using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;
/// <summary>
/// Allows the user to select and persist the application display language.
/// </summary>
public sealed class PanelLanguageSettings : UserControl
{
    private static readonly Color BodyTextColor = Color.FromArgb(102, 102, 102);
    private static readonly Color AccentColor = Color.FromArgb(19, 184, 166);
    private readonly ComboBox languageSelector;
    /// <summary>
    /// Initializes the language settings panel.
    /// </summary>
    public PanelLanguageSettings()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;

        SmartLabel pageTitle = CreateLabel(Strings.Get("Language settings"), 11F, AccentColor, new Point(11, 8));
        Label fieldLabel = CreateLabel(Strings.Get("Display language"), 9F, BodyTextColor, new Point(15, 48));
        languageSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DropDownWidth = 240,
            Font = new Font("Segoe UI", 9F),
            ForeColor = BodyTextColor,
            Location = new Point(15, 70),
            Size = new Size(240, 23)
        };
        languageSelector.Items.AddRange([
            new LanguageOption("auto", Strings.Get("Use system language")),
            new LanguageOption("en-US", "English"),
            new LanguageOption("zh-TW", "正體中文")]);
        languageSelector.SelectedIndex = GetSelectedIndex(IddsConfig.Instance.Language);

        Label restartNotice = CreateLabel(
            Strings.Get("Restart the application after saving to apply the language to every open window."),
            9F,
            BodyTextColor,
            new Point(15, 104));
        restartNotice.MaximumSize = new Size(430, 0);

        Button save = new()
        {
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            ForeColor = BodyTextColor,
            Location = new Point(15, 142),
            Size = new Size(102, 26),
            Text = Strings.Get("&Save"),
            UseVisualStyleBackColor = false
        };
        save.Click += SaveLanguage;
        SettingsResetButtonFactory.AddTo(
            this,
            (_, _) => languageSelector.SelectedIndex = 0,
            new Point(123, 142));

        Controls.Add(pageTitle);
        Controls.Add(fieldLabel);
        Controls.Add(languageSelector);
        Controls.Add(restartNotice);
        Controls.Add(save);
    }

    private static SmartLabel CreateLabel(string text, float size, Color color, Point location) => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", size),
        ForeColor = color,
        Location = location,
        Text = text
    };

    private static int GetSelectedIndex(string setting) => setting switch { "en" or "en-US" => 1, "zh" or "zh-TW" or "zh-Hant" => 2, _ => 0 };

    private void SaveLanguage(object? sender, EventArgs e)
    {
        if (languageSelector.SelectedItem is not LanguageOption option) return;
        IddsConfig.Instance.Language = option.Value;
        IddsConfig.Instance.SaveAppConfig();
        MessageBox.Show(Strings.Get("Language setting was saved. Restart the application to apply it."), Strings.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed record LanguageOption(string Value, string DisplayName)
    {
        /// <summary>
        /// 傳回語言選項之本地化顯示名稱。
        /// </summary>
            /// <returns>語言顯示名稱字串。</returns>
        public override string ToString() => DisplayName;
    }
}
