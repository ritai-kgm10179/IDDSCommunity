using System;
using System.Collections.Generic;
using System.Drawing;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using System.Windows.Forms;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class PanelSafeNetworks : UserControl
{

    public event EventHandler? SafeNetworksChanged;
    /// <summary>
    /// 初始化 <see cref="PanelSafeNetworks"/> 類別的新執行個體。
    /// </summary>
    public PanelSafeNetworks()
    {
        InitializeComponent();
        BackColor = Color.White;
        listBoxSafeNetworks.Sorted = true;
        listBoxSafeNetworks.DisplayMember = "DisplayName";
        Load += new EventHandler(PanelSafeNetworks_Load);
    }
    /// <summary>
    /// 處理 load 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    void PanelSafeNetworks_Load(object? sender, EventArgs e) => LoadData();
    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void textBoxAddNetwork_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == 13)
        {
            AddNetwork();
            e.Handled = true;
        }
        if (e.KeyChar == 27)
        {
            HideNetworkPanel();
            e.Handled = true;
        }
    }
    /// <summary>
    /// Adds network.
    /// </summary>
    private void AddNetwork()
    {
        smartLabelInvalidNetwork.Visible = false;
        try
        {
            string ipnet = IddsConfig.ConvertStringToIpAddressNetwork(textBoxAddNetwork.Text);
            if (EditExisting)
            {
                if (listBoxSafeNetworks.SelectedItem is object selectedItem)
                {
                    listBoxSafeNetworks.Items.Remove(selectedItem);
                }
            }

            listBoxSafeNetworks.Items.Add(new IddsConfig.CSafeNetwork(ipnet.Split('/')[0], ipnet.Split('/')[1]));
            HideNetworkPanel();
            listBoxSafeNetworks.Focus();
        }
        catch (ArgumentException)
        {
            smartLabelInvalidNetwork.Text = Strings.Get("Enter a valid IPv4 or IPv6 address, optionally followed by a CIDR prefix.");
            smartLabelInvalidNetwork.Visible = true;
        }
    }
    /// <summary>
    /// 執行 show add network panel 作業。
    /// </summary>
    private void ShowAddNetworkPanel()
    {
        smartPanelAdd.Visible = true;
        textBoxAddNetwork.Focus();
    }
    /// <summary>
    /// 執行 hide network panel 作業。
    /// </summary>
    private void HideNetworkPanel()
    {
        textBoxAddNetwork.Text = "";
        EditExisting = false;
        smartPanelAdd.Visible = false;
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxAdd_Click(object sender, EventArgs e)
    {
        ShowAddNetworkPanel();
        SetEditMode(true);
    }
    /// <summary>
    /// 處理 double click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void listBoxSafeNetworks_DoubleClick(object sender, EventArgs e)
    {
        if (listBoxSafeNetworks.SelectedItems.Count == 1)
        {
            textBoxAddNetwork.Text = listBoxSafeNetworks.SelectedItem?.ToString() ?? string.Empty;
            EditExisting = true;
            ShowAddNetworkPanel();
        }
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxDelete_Click(object sender, EventArgs e)
    {
        List<IddsConfig.CSafeNetwork> selected = [];
        foreach (object o in listBoxSafeNetworks.SelectedItems)
        {
            if (o is IddsConfig.CSafeNetwork network) selected.Add(network);
        }
        foreach (IddsConfig.CSafeNetwork net in selected)
        {
            listBoxSafeNetworks.Items.Remove(net);
        }
        SetEditMode(true);
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void button1_Click(object sender, EventArgs e) => HideNetworkPanel();
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonAddNetwork_Click(object sender, EventArgs e) => AddNetwork();
    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void listBoxSafeNetworks_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == 13 && listBoxSafeNetworks.SelectedItem != null)
        {
            textBoxAddNetwork.Text = listBoxSafeNetworks.SelectedItem?.ToString() ?? string.Empty;
            EditExisting = true;
            ShowAddNetworkPanel();
        }
        if (e.KeyChar == '+')
        {
            ShowAddNetworkPanel();
        }
    }
    /// <summary>
    /// Processes the safe networks changed notification.
    /// </summary>
    private void OnSafeNetworksChanged() => SafeNetworksChanged?.Invoke(this, EventArgs.Empty);

    public bool EditExisting { get; set; }
    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X + 1, loc.Y + 1);
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        if (sender is not Control control) return;
        Point loc = control.Location;
        control.Location = new Point(loc.X - 1, loc.Y - 1);
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxSave_Click(object sender, EventArgs e)
    {

    }
    /// <summary>
    /// Loads data.
    /// </summary>
    private void LoadData()
    {
        listBoxSafeNetworks.Items.Clear();
        checkBoxConfigureSafeNetworks.Checked = IddsConfig.Instance.UseSafeNetworkList;
        foreach (IddsConfig.CSafeNetwork net in IddsConfig.Instance.SafeNetworks)
        {
            listBoxSafeNetworks.Items.Add(net);
        }
        SetEditMode(false);
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void pictureBoxEdit_Click(object sender, EventArgs e)
    {
        if (IsInEditMode) LoadData();
        ToggleEditMode();
    }

    public bool IsInEditMode { get; set; }
    /// <summary>
    /// 執行 toggle edit mode 作業。
    /// </summary>
    private void ToggleEditMode()
    {
        if (!IsInEditMode)
        {
            pictureBoxEdit.Image = Properties.Resources.button25px_delete;
            IsInEditMode = true;
        }
        else
        {
            pictureBoxEdit.Image = Properties.Resources.button25px_edit;
            IsInEditMode = false;
        }
        pictureBoxSave.Visible = IsInEditMode;
        pictureBoxAdd.Enabled = IsInEditMode;
        pictureBoxDelete.Enabled = IsInEditMode;
        listBoxSafeNetworks.Enabled = IsInEditMode;
        checkBoxConfigureSafeNetworks.Enabled = IsInEditMode;
    }
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonDiscard_Click(object sender, EventArgs e) => LoadData();
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void buttonSave_Click(object sender, EventArgs e)
    {
        IddsConfig.CSafeNetworks nets = [];
        foreach (object o in listBoxSafeNetworks.Items)
        {
            if (o is IddsConfig.CSafeNetwork)
            {
                nets.Add((IddsConfig.CSafeNetwork)o);
            }
        }
        IddsConfig.Instance.SafeNetworks = nets;
        IddsConfig.Instance.SaveSafeNetworks();
        IddsConfig.Instance.UseSafeNetworkList = checkBoxConfigureSafeNetworks.Checked;
        IddsConfig.Instance.Save();

        OnSafeNetworksChanged();
        SetEditMode(false);
    }
    /// <summary>
    /// 處理 key press 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void textBox_KeyPress(object sender, KeyPressEventArgs e) => SetEditMode(true);
    /// <summary>
    /// Sets edit mode.
    /// </summary>
    /// <param name="hasChanges">A value indicating whether s changes.</param>
    private void SetEditMode(bool hasChanges)
    {
        buttonSave.Visible = hasChanges;
        buttonDiscard.Visible = hasChanges;
    }
    /// <summary>
    /// 處理 checked changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void checkBox_CheckedChanged(object sender, EventArgs e) => SetEditMode(true);


}
