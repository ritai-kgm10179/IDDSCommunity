using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using System.Windows.Forms.VisualStyles;

namespace IDDSCommunity.IntrusionDetection.Admin;

/// <summary>
/// 提供目前已封鎖 IP 清單檢視與手動解除封鎖功能之使用者控制項。
/// </summary>
public partial class IDDSCommunityCurrentLocks : UserControl
{
    /// <summary>
    /// 初始化 <see cref="IDDSCommunityCurrentLocks"/> 類別的新執行個體。
    /// </summary>
    public IDDSCommunityCurrentLocks()
    {
        InitializeComponent();
        ConfigureGrid();
        pictureBox3.Image = InterfaceIcons.CreateLock(Math.Min(pictureBox3.ClientSize.Width, pictureBox3.ClientSize.Height));
        pictureBoxActionMenuUnlock.Image = InterfaceIcons.CreateLock(22, true);
    }

    private void ConfigureGrid()
    {
        smartLabel2.Visible = false;
        smartLabel4.Visible = false;
        smartLabel5.Visible = false;
        smartLabel6.Visible = false;
        smartLabel7.Visible = false;
        panelCurrentLocksGrid.AutoScroll = false;
        panelCurrentLocksGrid.Padding = new Padding(1);
        dataGridViewLocks.Dock = DockStyle.Fill;
        AdminGridChrome.Apply(dataGridViewLocks);

        dataGridViewColumnTypeName.SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridViewColumnIpAddress.SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridViewColumnAgent.SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridViewColumnLockDate.SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridViewColumnUnlockDate.SortMode = DataGridViewColumnSortMode.Automatic;
        dataGridViewColumnUnlockDate.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        dataGridViewColumnUnlockDate.MinimumWidth = 160;

        checkBoxSelectAllLocks.Visible = false;
        dataGridViewLocks.CellPainting += DataGridViewLocks_CellPainting;
        dataGridViewLocks.ColumnHeaderMouseClick += DataGridViewLocks_ColumnHeaderMouseClick;
        dataGridViewLocks.Sort(dataGridViewColumnLockDate, System.ComponentModel.ListSortDirection.Descending);
    }

    private void DataGridViewLocks_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex != -1 || e.ColumnIndex != dataGridViewSelectItem.Index)
            return;

        if (e.Graphics is null)
            return;

        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
        CheckBoxState state = checkBoxSelectAllLocks.Checked
            ? CheckBoxState.CheckedNormal
            : CheckBoxState.UncheckedNormal;
        Size glyphSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
        Rectangle glyphBounds = new(
            e.CellBounds.Left + (e.CellBounds.Width - glyphSize.Width) / 2,
            e.CellBounds.Top + (e.CellBounds.Height - glyphSize.Height) / 2,
            glyphSize.Width,
            glyphSize.Height);
        CheckBoxRenderer.DrawCheckBox(e.Graphics, glyphBounds.Location, state);
        e.Handled = true;
    }

    private void DataGridViewLocks_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex != dataGridViewSelectItem.Index)
            return;

        checkBoxSelectAllLocks.Checked = !checkBoxSelectAllLocks.Checked;
        dataGridViewLocks.InvalidateCell(dataGridViewSelectItem.Index, -1);
    }

    /// <summary>
    /// 處理 mouse down 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void actionMenu_MouseDown(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X + 1, c.Location.Y + 1);
    }
    /// <summary>
    /// 處理 mouse up 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void actionMenu_MouseUp(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X - 1, c.Location.Y - 1);
    }
    /// <summary>
    /// Finds row.
    /// </summary>
    /// <param name="id">id 的值。</param>
    /// <returns>搜尋到的 DataGridViewRow 傳回結果。</returns>
    public DataGridViewRow? FindRow(int id)
    {
        foreach (DataGridViewRow row in dataGridViewLocks.Rows)
        {
            if (string.Equals(row.Cells[7].Value?.ToString(), id.ToString(), StringComparison.Ordinal))
            {
                return row;
            }
        }
        return null;
    }
    /// <summary>
    /// Clears requested operation.
    /// </summary>
    public void Clear() => dataGridViewLocks.Rows.Clear();
    /// <summary>
    /// Adds requested operation.
    /// </summary>
    /// <param name="id">id 的值。</param>
    /// <param name="icon">icon 的值。</param>
    /// <param name="statusName">status name 的值。</param>
    /// <param name="clientIp">client ip 的值。</param>
    /// <param name="displayName">display name 的值。</param>
    /// <param name="lockDate">lock date 的值。</param>
    /// <param name="unlockDate">unlock date 的值。</param>
    /// <param name="status">status 的值。</param>
    public void Add(int id, Image icon, string statusName, string clientIp, string displayName, DateTime lockDate, DateTime unlockDate, int status)
    {
        DataGridViewRow? row = FindRow(id);
        if (row != null)
        {
            if (string.Equals(row.Cells[2].Value?.ToString(), status.ToString(), StringComparison.Ordinal))
            {
                return;
            }
        }
        else
        {
            dataGridViewLocks.Rows.Insert(0, new DataGridViewRow());
            row = dataGridViewLocks.Rows[0];
        }
        if (row.Cells[1] is DataGridViewImageCell imageCell) imageCell.Value = icon;
        row.Cells[2].Value = statusName;
        row.Cells[3].Value = clientIp;
        row.Cells[4].Value = displayName;
        row.Cells[5].Value = lockDate;
        row.Cells[6].Value = unlockDate;
        row.Cells[7].Value = id.ToString();
        row.Cells[8].Value = status;
    }
    /// <summary>
    /// Sets hard locks.
    /// </summary>
    /// <param name="number">number 的值。</param>
    public void SetHardLocks(int number) => labelCurrentLocksHardLocks.Text = Strings.Format("{0} hard locks", number);
    /// <summary>
    /// 處理 checked changed 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void checkBoxSelectAllLocks_CheckedChanged(object sender, EventArgs e)
    {
        foreach (DataGridViewRow r in dataGridViewLocks.Rows)
        {
            if (r.Cells["dataGridViewSelectItem"] is DataGridViewCheckBoxCell c && sender is CheckBox checkBox)
            {
                c.Value = checkBox.Checked;
            }
        }
    }

    /// <summary>
    /// Sets soft locks.
    /// </summary>
    /// <param name="number">number 的值。</param>
    public void SetSoftLocks(int number) => labelCurrentLocksSoftLocks.Text = Strings.Format("{0} soft locks", number);
    /// <summary>
    /// 處理 click 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private async void actionMenuUnlock_Click(object sender, EventArgs e)
    {
        List<(long LockId, DataGridViewRow Row)> requests = [];
        foreach (DataGridViewRow row in dataGridViewLocks.Rows)
        {
            if (row.Cells["dataGridViewSelectItem"] is not DataGridViewCheckBoxCell c) continue;
            //if (c.Value == null) {
            //    if (c.Selected) { c.Value = c.TrueValue; } else { c.Value = c.FalseValue; }
            //}
            if (c.EditedFormattedValue is true && (row.Cells[8].Value?.ToString() == Lock.LOCK_STATUS_SOFTLOCK.ToString() ||
                          row.Cells[8].Value?.ToString() == Lock.LOCK_STATUS_HARDLOCK.ToString()))
            {
                if (long.TryParse(row.Cells[7].Value?.ToString(), out long lockId))
                    requests.Add((lockId, row));
            }
        }
        if (requests.Count == 0)
            return;
        actionMenuUnlock.Enabled = false;
        try
        {
            HashSet<long> completed = await Task.Run(() => RequestUnlocks(requests.ConvertAll(static request => request.LockId))).ConfigureAwait(false);
            await this.InvokeAsync(() =>
            {
                foreach ((long lockId, DataGridViewRow row) in requests)
                {
                    if (completed.Contains(lockId))
                        row.Cells[2].Value = LockStatusAdapter.GetLockStatusName(Lock.LOCK_STATUS_MANUAL);
                }
            });
        }
        finally
        {
            try
            {
                if (!IsDisposed && IsHandleCreated)
                    await this.InvokeAsync(() => actionMenuUnlock.Enabled = true);
            }
            catch (InvalidOperationException) when (IsDisposed || !IsHandleCreated)
            {
                // Form shutdown can destroy the window handle while the database operation completes.
            }
        }
    }
    /// <summary>
    /// Persists manual-unlock requests without accessing WinForms controls.
    /// </summary>
    /// <param name="lockIds">The selected durable lock identifiers.</param>
    /// <returns>成功變更為解鎖要求的標籤識別碼集合。</returns>
    private static HashSet<long> RequestUnlocks(IReadOnlyList<long> lockIds)
    {
        HashSet<long> completed = [];
        ProtectionAuditTrail auditTrail = new(Database.Instance, TimeProvider.System);
        foreach (long lockId in lockIds)
        {
            Lock l = Locks.GetLockById(lockId);
            if (l is null)
                continue;
            l.Status = Lock.LOCK_STATUS_UNLOCK_REQUESTED;
            l.Save();
            auditTrail.Record(
                "Firewall.ManualUnlockRequested",
                "Succeeded",
                Environment.UserDomainName + "\\" + Environment.UserName,
                l.IpAddress,
                lockId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            completed.Add(lockId);
        }
        return completed;
    }






}
