using System;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.IntrusionDetection.Admin;

/// <summary>
/// Allows the user to select and persist the application display language.
/// </summary>
public sealed class PanelLanguageSettings : UserControl
{
    private readonly ComboBox languageSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };

    /// <summary>
    /// Initializes the language settings panel.
    /// </summary>
    public PanelLanguageSettings()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;
        Label heading = new() { AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = Strings.Get("Display language") };
        Label restartNotice = new() { AutoSize = true, MaximumSize = new Size(430, 0), Text = Strings.Get("Restart the application after saving to apply the language to every open window.") };
        Button save = new() { AutoSize = true, Text = Strings.Get("&Save") };
        languageSelector.Items.AddRange([new LanguageOption("auto", Strings.Get("Use system language")), new LanguageOption("en-US", "English"), new LanguageOption("zh-TW", "正體中文")]);
        languageSelector.SelectedIndex = GetSelectedIndex(IddsConfig.Instance.Language);
        save.Click += SaveLanguage;

        FlowLayoutPanel layout = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), WrapContents = false };
        layout.Controls.Add(heading);
        layout.Controls.Add(languageSelector);
        layout.Controls.Add(restartNotice);
        layout.Controls.Add(save);
        Controls.Add(layout);
    }

    /// <summary>
    /// Maps a persisted language setting to the selector index.
    /// </summary>
    /// <param name="setting">The persisted culture name.</param>
    /// <returns>The matching selector index.</returns>
    private static int GetSelectedIndex(string setting) => setting switch { "en" or "en-US" => 1, "zh" or "zh-TW" or "zh-Hant" => 2, _ => 0 };

    /// <summary>
    /// Persists the selected language and informs the user that open windows require a restart.
    /// </summary>
    /// <param name="sender">The save button.</param>
    /// <param name="e">The event data.</param>
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
        /// Returns the localized option label displayed by the selector.
        /// </summary>
        /// <returns>The display name.</returns>
        public override string ToString() => DisplayName;
    }
}
