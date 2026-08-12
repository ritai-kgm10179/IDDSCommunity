using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunitySettingsNavigation : UserControl
{

    public event EventHandler? PluginsChanged;
    /// <summary>
    /// 初始化 <see cref="IDDSCommunitySettingsNavigation"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunitySettingsNavigation() => InitializeComponent();

    public event EventHandler? NavigationChanged;

    public Color SeparatorColor { get; set; }

    public bool ShowSeparator { get; set; }

    public bool ShowTopMenu { get; set; }
    /// <summary>
    /// 處理 on paint 事件。
    /// </summary>
    /// <param name="e">事件資料。</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!ShowTopMenu)
        {
            flowLayoutPanelNavigationItems.Top = 0;
            flowLayoutPanelNavigationItems.Height = Height - 1;
            smartPanelActionBar.Hide();
        }
        else
        {
            flowLayoutPanelNavigationItems.Top = 33;
            flowLayoutPanelNavigationItems.Height = Height - 34;
            smartPanelActionBar.Show();
        }
        if (ShowSeparator)
        {
            e.Graphics.DrawLine(new Pen(SeparatorColor, 1), Width - 5, 0, Width - 5, Height);
        }
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void iddscommunitySettingsNavigationItem_Click(object? sender, EventArgs e)
    {
        if (sender is IDDSCommunitySettingsNavigationItem item && !item.IsSelected)
        {
            UnselectAll();
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }

    private List<IDDSCommunitySettingsNavigationItem>? _navigationItems;
    private List<IDDSCommunitySettingsNavigationItem> NavigationItems
    {
        get
        {
            _navigationItems ??= [];
            return _navigationItems;
        }
    }
    /// <summary>
    /// Adds navigation item.
    /// </summary>
    /// <param name="item">item 的值。</param>
    public void AddNavigationItem(IDDSCommunitySettingsNavigationItem item) => NavigationItems.Add(item);
    /// <summary>
    /// Adds navigation item.
    /// </summary>
    /// <param name="name">name 的值。</param>
    /// <param name="selectedIcon">selected icon 的值。</param>
    /// <param name="unselectedIcon">unselected icon 的值。</param>
    public void AddNavigationItem(string name, Image? selectedIcon, Image? unselectedIcon)
    {
        IDDSCommunitySettingsNavigationItem item = new()
        {
            SelectedIcon = selectedIcon,
            DisplayName = name,
            UnselectedIcon = unselectedIcon
        };
        flowLayoutPanelNavigationItems.Controls.Add(item);
        item.NavigationClicked += new EventHandler(iddscommunitySettingsNavigationItem_Click);
        if (flowLayoutPanelNavigationItems.Controls.Count == 1)
        {
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }
    /// <summary>
    /// Clears requested operation.
    /// </summary>
    public void Clear()
    {
        NavigationItems.Clear();
        flowLayoutPanelNavigationItems.Controls.Clear();
    }


    public IDDSCommunitySettingsNavigationItem? SelectedItem
    {
        get
        {
            foreach (Control c in flowLayoutPanelNavigationItems.Controls)
            {
                if (c is IDDSCommunitySettingsNavigationItem item && item.IsSelected) return item;
            }
            return null;
        }
    }
    /// <summary>
    /// Sets selected item.
    /// </summary>
    /// <param name="name">name 的值。</param>
    public void SetSelectedItem(string name)
    {
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is IDDSCommunitySettingsNavigationItem item && item.DisplayName.Equals(name, StringComparison.Ordinal))
            {
                UnselectAll();
                item.IsSelected = true;
                flowLayoutPanelNavigationItems.ScrollControlIntoView(item);
                OnNavigationChanged(this);
            }
        }
    }
    /// <summary>
    /// Processes the navigation changed notification.
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    private void OnNavigationChanged(object sender) => NavigationChanged?.Invoke(sender, EventArgs.Empty);

    public string SelectedName
    {
        get
        {
            foreach (Control c in flowLayoutPanelNavigationItems.Controls)
            {
                if (c is IDDSCommunitySettingsNavigationItem item && item.IsSelected) return item.DisplayName;
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// 執行 unselect all 作業。
    /// </summary>
    public void UnselectAll()
    {
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is IDDSCommunitySettingsNavigationItem item)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    c.Invalidate();
                }
            }
        }
    }

    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxAdd_MouseDown(object sender, MouseEventArgs e) => pictureBoxAdd.Location = new Point(pictureBoxAdd.Location.X + 1, pictureBoxAdd.Location.Y + 1);
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxAdd_MouseUp(object sender, MouseEventArgs e) => pictureBoxAdd.Location = new Point(pictureBoxAdd.Location.X - 1, pictureBoxAdd.Location.Y - 1);
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxRemove_MouseUp(object sender, MouseEventArgs e) => pictureBoxRemove.Location = new Point(pictureBoxRemove.Location.X - 1, pictureBoxRemove.Location.Y - 1);
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxRemove_MouseDown(object sender, MouseEventArgs e) => pictureBoxRemove.Location = new Point(pictureBoxRemove.Location.X + 1, pictureBoxRemove.Location.Y + 1);
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxAdd_Click(object sender, EventArgs e)
    {
        OpenFileDialog openFile = new()
        {
            CheckPathExists = true,
            CheckFileExists = true,
            Filter = Strings.Get("Assemblies (*.dll)|*.dll"),
            Title = Strings.Get("Please select plugin assembly"),
            Multiselect = true
        };
        if (openFile.ShowDialog() == DialogResult.OK)
        {
            string pluginDirectory = Shared.IddsConfig.Instance.PluginsDirectory;
            string chosenDirectory = openFile.FileNames[0][..openFile.FileNames[0].LastIndexOf('\\')];
            if (openFile.FileNames.Length <= 0)
            {
                GenericErrorDialog error = new(Strings.Get("No file was selected!"), Strings.Get("Please choose at least one assembly to load."), false);
                error.ShowDialog();
                return;
            }
            if (chosenDirectory == pluginDirectory)
            {
                GenericErrorDialog error = new(Strings.Get("Invalid directory"), Strings.Get("Please choose a directory other than the plugin directory. These assemblies are already loaded."), false);
                error.ShowDialog();
                return;
            }
            if (!Directory.Exists(pluginDirectory))
            {
                try
                {
                    Directory.CreateDirectory(pluginDirectory);
                }
                catch (Exception ex)
                {
                    GenericErrorDialog error = new(Strings.Get("Plugin directory not found!"), ex.Message, false);
                    error.ShowDialog();
                    return;
                }
            }
            foreach (string fileName in openFile.FileNames)
            {
                string assemblyName = fileName[(fileName.LastIndexOf('\\') + 1)..];
                if (!File.Exists(pluginDirectory + assemblyName) ||
                    MessageBox.Show(Strings.Get("This assembly already exists. Do you want to overwrite the existing?"), Strings.Get("Overwrite existing?"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    try
                    {
                        File.Copy(fileName, pluginDirectory + assemblyName, true);
                    }
                    catch (Exception ex)
                    {
                        GenericErrorDialog error = new(Strings.Get("Assembly cannot be copied."), ex.Message, false);
                        error.ShowDialog();
                    }
                }
            }
            Shared.SecurityAgents.Instance.InitializeAgents();
            OnPluginsChanged();
        }
    }
    /// <summary>
    /// Processes the plugins changed notification.
    /// </summary>
    private void OnPluginsChanged() => PluginsChanged?.Invoke(this, EventArgs.Empty);


}
