using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供設定功能導覽樹狀選單之使用者控制項。
/// </summary>
public partial class IDDSCommunitySettingsNavigation : UserControl
{

        /// <summary>
    /// 當 PluginsChanged 時引發之事件。
    /// </summary>
public event EventHandler? PluginsChanged;
    /// <summary>
    /// 初始化 <see cref="IDDSCommunitySettingsNavigation"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunitySettingsNavigation()
    {
        InitializeComponent();
        flowLayoutPanelNavigationItems.AutoScroll = true;
        flowLayoutPanelNavigationItems.WrapContents = false;
        flowLayoutPanelNavigationItems.HorizontalScroll.Enabled = false;
        flowLayoutPanelNavigationItems.HorizontalScroll.Visible = false;
    }

    /// <summary>
    /// 當 NavigationChanged 時引發之事件。
    /// </summary>
    public event EventHandler? NavigationChanged;

    /// <summary>
    /// 取得或設定 SeparatorColor。
    /// </summary>
    public Color SeparatorColor { get; set; }

    /// <summary>
    /// 取得或設定 ShowSeparator。
    /// </summary>
    public bool ShowSeparator { get; set; }

    /// <summary>
    /// 取得或設定 ShowTopMenu。
    /// </summary>
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
        int clientW = flowLayoutPanelNavigationItems.ClientSize.Width;
        int targetWidth = Math.Max(200, clientW > 10 ? clientW - 8 : 330);
        IDDSCommunitySettingsNavigationItem item = new()
        {
            SelectedIcon = selectedIcon,
            DisplayName = name,
            UnselectedIcon = unselectedIcon,
            Width = targetWidth
        };
        flowLayoutPanelNavigationItems.Controls.Add(item);
        item.NavigationClicked += new EventHandler(iddscommunitySettingsNavigationItem_Click);
        UpdateItemWidths();
        if (flowLayoutPanelNavigationItems.Controls.Count == 1)
        {
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }

    /// <inheritdoc/>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateItemWidths();
    }

    /// <inheritdoc/>
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        UpdateItemWidths();
    }

    /// <summary>
    /// 依據導覽容器實際可視 Client 寬度自動調整所有項目寬度，消除水平捲軸。
    /// </summary>
    private void UpdateItemWidths()
    {
        if (flowLayoutPanelNavigationItems == null) return;
        flowLayoutPanelNavigationItems.HorizontalScroll.Enabled = false;
        flowLayoutPanelNavigationItems.HorizontalScroll.Visible = false;

        int clientW = flowLayoutPanelNavigationItems.ClientSize.Width;
        if (clientW <= 0) return;

        int targetWidth = Math.Max(200, clientW - 8);
        flowLayoutPanelNavigationItems.SuspendLayout();
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is IDDSCommunitySettingsNavigationItem item && item.Width != targetWidth)
            {
                item.Width = targetWidth;
            }
        }
        flowLayoutPanelNavigationItems.ResumeLayout(true);
    }
    /// <summary>
    /// Clears requested operation.
    /// </summary>
    public void Clear()
    {
        NavigationItems.Clear();
        while (flowLayoutPanelNavigationItems.Controls.Count > 0)
        {
            Control child = flowLayoutPanelNavigationItems.Controls[0];
            flowLayoutPanelNavigationItems.Controls.RemoveAt(0);
            child.Dispose();
        }
    }


        /// <summary>
    /// 取得或設定 SelectedItem。
    /// </summary>
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
                flowLayoutPanelNavigationItems.AutoScrollPosition = new Point(0, flowLayoutPanelNavigationItems.VerticalScroll.Value);
                OnNavigationChanged(item);
            }
        }
    }
    /// <summary>
    /// Processes the navigation changed notification.
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    private void OnNavigationChanged(object sender) => NavigationChanged?.Invoke(sender, EventArgs.Empty);

        /// <summary>
    /// 取得或設定 SelectedName。
    /// </summary>
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
            if (openFile.FileNames.Length <= 0)
            {
                GenericErrorDialog error = new(Strings.Get("No file was selected!"), Strings.Get("Please choose at least one assembly to load."), false);
                error.ShowDialog();
                return;
            }
            string chosenDirectory = Path.GetDirectoryName(openFile.FileNames[0]) ?? string.Empty;
            if (string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(chosenDirectory)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(pluginDirectory)),
                StringComparison.OrdinalIgnoreCase))
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
                string assemblyName = Path.GetFileName(fileName);
                string destination = Path.Combine(pluginDirectory, assemblyName);
                if (!File.Exists(destination) ||
                    MessageBox.Show(Strings.Get("This assembly already exists. Do you want to overwrite the existing?"), Strings.Get("Overwrite existing?"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    try
                    {
                        File.Copy(fileName, destination, true);
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
