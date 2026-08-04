using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Cyberarms.IntrusionDetection.Shared.Localization;

namespace Cyberarms.IntrusionDetection.Admin;

public partial class CyberarmsSettingsNavigation : UserControl
{

    public event EventHandler? PluginsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="CyberarmsSettingsNavigation"/> class.
    /// </summary>

    public CyberarmsSettingsNavigation() => InitializeComponent();

    public event EventHandler? NavigationChanged;

    public Color SeparatorColor { get; set; }

    public bool ShowSeparator { get; set; }

    public bool ShowTopMenu { get; set; }

    /// <summary>
    /// Handles the on paint event.
    /// </summary>
    /// <param name="e">The event data.</param>

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
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void cyberarmsSettingsNavigationItem_Click(object? sender, EventArgs e)
    {
        if (sender is CyberarmsSettingsNavigationItem item && !item.IsSelected)
        {
            UnselectAll();
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }

    private List<CyberarmsSettingsNavigationItem>? _navigationItems;
    private List<CyberarmsSettingsNavigationItem> NavigationItems
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
    /// <param name="item">The item value.</param>

    public void AddNavigationItem(CyberarmsSettingsNavigationItem item) => NavigationItems.Add(item);

    /// <summary>
    /// Adds navigation item.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="selectedIcon">The selected icon value.</param>
    /// <param name="unselectedIcon">The unselected icon value.</param>

    public void AddNavigationItem(string name, Image selectedIcon, Image unselectedIcon)
    {
        CyberarmsSettingsNavigationItem item = new()
        {
            SelectedIcon = selectedIcon,
            DisplayName = name,
            UnselectedIcon = unselectedIcon
        };
        flowLayoutPanelNavigationItems.Controls.Add(item);
        item.NavigationClicked += new EventHandler(cyberarmsSettingsNavigationItem_Click);
        if (flowLayoutPanelNavigationItems.Controls.Count == 1)
        {
            item.IsSelected = true;
            OnNavigationChanged(item);
        }
    }

    /// <summary>
    /// Clears requested operation.
    /// </summary>

    public void Clear() => NavigationItems.Clear();


    public CyberarmsSettingsNavigationItem? SelectedItem
    {
        get
        {
            foreach (Control c in flowLayoutPanelNavigationItems.Controls)
            {
                if (c is CyberarmsSettingsNavigationItem item && item.IsSelected) return item;
            }
            return null;
        }
    }

    /// <summary>
    /// Sets selected item.
    /// </summary>
    /// <param name="name">The name value.</param>

    public void SetSelectedItem(string name)
    {
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is CyberarmsSettingsNavigationItem item && item.DisplayName.Equals(name, StringComparison.Ordinal))
            {
                UnselectAll();
                item.IsSelected = true;
                OnNavigationChanged(this);
            }
        }
    }

    /// <summary>
    /// Processes the navigation changed notification.
    /// </summary>
    /// <param name="sender">The source of the event.</param>

    private void OnNavigationChanged(object sender) => NavigationChanged?.Invoke(sender, EventArgs.Empty);

    public string SelectedName
    {
        get
        {
            foreach (Control c in flowLayoutPanelNavigationItems.Controls)
            {
                if (c is CyberarmsSettingsNavigationItem item && item.IsSelected) return item.DisplayName;
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Executes the unselect all operation.
    /// </summary>

    public void UnselectAll()
    {
        foreach (Control c in flowLayoutPanelNavigationItems.Controls)
        {
            if (c is CyberarmsSettingsNavigationItem item)
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
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxAdd_MouseDown(object sender, MouseEventArgs e) => pictureBoxAdd.Location = new Point(pictureBoxAdd.Location.X + 1, pictureBoxAdd.Location.Y + 1);

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxAdd_MouseUp(object sender, MouseEventArgs e) => pictureBoxAdd.Location = new Point(pictureBoxAdd.Location.X - 1, pictureBoxAdd.Location.Y - 1);

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxRemove_MouseUp(object sender, MouseEventArgs e) => pictureBoxRemove.Location = new Point(pictureBoxRemove.Location.X - 1, pictureBoxRemove.Location.Y - 1);

    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxRemove_MouseDown(object sender, MouseEventArgs e) => pictureBoxRemove.Location = new Point(pictureBoxRemove.Location.X + 1, pictureBoxRemove.Location.Y + 1);

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void pictureBoxAdd_Click(object sender, EventArgs e)
    {
        OpenFileDialog openFile = new()
        {
            CheckPathExists = true,
            CheckFileExists = true,
            Filter = "Assemblies (*.dll)|*.dll",
            Title = "Please select plugin assembly",
            Multiselect = true
        };
        if (openFile.ShowDialog() == DialogResult.OK)
        {
            string pluginDirectory = Shared.IddsConfig.Instance.PluginsDirectory;
            string chosenDirectory = openFile.FileNames[0][..openFile.FileNames[0].LastIndexOf('\\')];
            if (openFile.FileNames.Length <= 0)
            {
                GenericErrorDialog error = new("No file was selected!", "Please choose at least one assembly to load.", false);
                error.ShowDialog();
                return;
            }
            if (chosenDirectory == pluginDirectory)
            {
                GenericErrorDialog error = new("Invalid directory", "Please choose a directory other than the plugin directory. These assemblies are already loaded.", false);
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
                    GenericErrorDialog error = new("Plugin directory not found!", ex.Message, false);
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
                        GenericErrorDialog error = new("Assembly cannot be copied.", ex.Message, false);
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
