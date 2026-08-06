using System;
using System.Drawing;
using System.Windows.Forms;
using IDDSCommunity.IntrusionDetection.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared.Localization;

namespace IDDSCommunity.IntrusionDetection.Admin;

public partial class IDDSCommunityCurrentLocks : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IDDSCommunityCurrentLocks"/> class.
    /// </summary>

    public IDDSCommunityCurrentLocks()
    {
        InitializeComponent();
        pictureBox3.Image = InterfaceIcons.CreateLock(32);
        pictureBoxActionMenuUnlock.Image = InterfaceIcons.CreateLock(20, true);
    }


    /// <summary>
    /// Handles the mouse down event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void actionMenu_MouseDown(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X + 1, c.Location.Y + 1);
    }

    /// <summary>
    /// Handles the mouse up event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void actionMenu_MouseUp(object sender, MouseEventArgs e)
    {
        var c = (Control)sender;
        c.Location = new Point(c.Location.X - 1, c.Location.Y - 1);
    }

    /// <summary>
    /// Finds row.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <returns>The find row result.</returns>

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
    /// <param name="id">The id value.</param>
    /// <param name="icon">The icon value.</param>
    /// <param name="statusName">The status name value.</param>
    /// <param name="clientIp">The client ip value.</param>
    /// <param name="displayName">The display name value.</param>
    /// <param name="lockDate">The lock date value.</param>
    /// <param name="unlockDate">The unlock date value.</param>
    /// <param name="status">The status value.</param>

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
    /// <param name="number">The number value.</param>

    public void SetHardLocks(int number) => labelCurrentLocksHardLocks.Text = Strings.Format("{0} hard locks", number);

    /// <summary>
    /// Handles the checked changed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
    /// <param name="number">The number value.</param>

    public void SetSoftLocks(int number) => labelCurrentLocksSoftLocks.Text = Strings.Format("{0} soft locks", number);

    /// <summary>
    /// Handles the click event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

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
    /// <returns>The identifiers successfully changed to unlock-requested.</returns>
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
