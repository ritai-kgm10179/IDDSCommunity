using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;

namespace IDDSCommunity.IntrusionDetection.Shared;

public class IntrusionLog
{
    public const int STATUS_INTRUSION_ATTEMPT = 100;
    public const int STATUS_INTRUSION_ATTEMPT_FROM_LOCAL = 110;
    public const int STATUS_INTRUSION_ATTEMPT_FROM_SAFE = 120;
    public const int STATUS_SOFT_LOCK_REQUESTED = 200;
    public const int STATUS_SOFT_LOCKED = 210;
    public const int STATUS_SOFT_LOCK_ERROR = 290;
    public const int STATUS_HARD_LOCK_REQUESTED = 300;
    public const int STATUS_HARD_LOCKED = 310;
    public const int STATUS_HARD_LOCK_ERROR = 390;
    public const int STATUS_UNLOCK_REQUESTED = 500;
    public const int STATUS_UNLOCKED = 510;
    public const int STATUS_UNLOCK_ERROR = 590;
    // Retains persisted legacy status value 999 without exposing the removed licensing feature.
    public const int STATUS_PROTECTION_UNAVAILABLE = 999;
    public const string SYSTEM_ID = "{DF7D1183-5033-4C94-AACB-CEFE9009B60F}";

    /// <summary>
    /// 取得所有代表登入失敗的事件狀態碼。
    /// </summary>
    public static IReadOnlyList<int> FailedLoginActions { get; } = Array.AsReadOnly<int>(
    [
        STATUS_INTRUSION_ATTEMPT,
        STATUS_INTRUSION_ATTEMPT_FROM_LOCAL,
        STATUS_INTRUSION_ATTEMPT_FROM_SAFE
    ]);

    /// <summary>
    /// 判斷指定狀態是否代表登入失敗事件。
    /// </summary>
    /// <param name="action">事件狀態碼。</param>
    /// <returns>若為登入失敗事件則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool IsFailedLoginAction(int action) => action is
        STATUS_INTRUSION_ATTEMPT or STATUS_INTRUSION_ATTEMPT_FROM_LOCAL or STATUS_INTRUSION_ATTEMPT_FROM_SAFE;
    /// <summary>
    /// Gets system id.
    /// </summary>
    /// <returns>傳回get system id結果。</returns>
    public static Guid GetSystemId() => new(SYSTEM_ID);

    private static Dictionary<int, string>? _statusNames;

    public static Dictionary<int, string> StatusNames
    {
        get
        {
            _statusNames ??= new Dictionary<int, string>
                {
                    { STATUS_INTRUSION_ATTEMPT, "Possible intrusion attempt." },
                    { STATUS_INTRUSION_ATTEMPT_FROM_LOCAL, "Invalid logon from localhost. Local addresses will not be blocked" },
                    { STATUS_INTRUSION_ATTEMPT_FROM_SAFE, "Invalid logon from safe network!" },
                    { STATUS_SOFT_LOCK_REQUESTED, "Soft lock threshold exceeded. Soft lock requested." },
                    { STATUS_SOFT_LOCKED, "This client was soft locked." },
                    { STATUS_SOFT_LOCK_ERROR, "There was a soft lock error. Please see event viewer for details." },
                    { STATUS_HARD_LOCK_REQUESTED, "Hard lock threshold exceeded. Hard lock requested." },
                    { STATUS_HARD_LOCKED, "The client was hard locked." },
                    { STATUS_HARD_LOCK_ERROR, "There was a hard lock error. Please see event viewer for details." },
                    { STATUS_UNLOCK_REQUESTED, "Lock has expired. Unlock requested." },
                    { STATUS_UNLOCKED, "This client was unlocked." },
                    { STATUS_UNLOCK_ERROR, "There was an unlock error. Please see event viewer for details." },
                    { STATUS_PROTECTION_UNAVAILABLE, "Protection is unavailable." }
                };
            return _statusNames;
        }
    }

    private static Dictionary<int, string>? _statusClasses;

    public static Dictionary<int, string> StatusClasses
    {
        get
        {
            _statusClasses ??= new Dictionary<int, string>
                {
                    { STATUS_INTRUSION_ATTEMPT, "Intrusion" },
                    { STATUS_INTRUSION_ATTEMPT_FROM_LOCAL, "Intrusion" },
                    { STATUS_INTRUSION_ATTEMPT_FROM_SAFE, "Intrusion" },
                    { STATUS_SOFT_LOCK_REQUESTED, "Soft lock" },
                    { STATUS_SOFT_LOCKED, "Soft lock" },
                    { STATUS_SOFT_LOCK_ERROR, "Error" },
                    { STATUS_HARD_LOCK_REQUESTED, "Hard lock" },
                    { STATUS_HARD_LOCKED, "Hard lock" },
                    { STATUS_HARD_LOCK_ERROR, "Error" },
                    { STATUS_UNLOCK_REQUESTED, "Unlock" },
                    { STATUS_UNLOCKED, "Unlock" },
                    { STATUS_UNLOCK_ERROR, "Error" },
                    { STATUS_PROTECTION_UNAVAILABLE, "Error" }
                };
            return _statusClasses;
        }
    }

    private static Dictionary<int, Image>? _statusIcons;
    public static Dictionary<int, Image> StatusIcons
    {
        get
        {
            _statusIcons ??= new Dictionary<int, Image>
                {
                    { STATUS_INTRUSION_ATTEMPT, Resources.logIcon_loginAttempt },
                    { STATUS_INTRUSION_ATTEMPT_FROM_LOCAL, Resources.logIcon_loginAttempt },
                    { STATUS_INTRUSION_ATTEMPT_FROM_SAFE, Resources.logIcon_loginAttempt },
                    { STATUS_SOFT_LOCK_REQUESTED, Resources.logIcon_softLock },
                    { STATUS_SOFT_LOCKED, Resources.logIcon_softLock },
                    { STATUS_SOFT_LOCK_ERROR, Resources.logIcon_warning },
                    { STATUS_HARD_LOCK_REQUESTED, Resources.logIcon_hardLock },
                    { STATUS_HARD_LOCKED, Resources.logIcon_hardLock },
                    { STATUS_HARD_LOCK_ERROR, Resources.logIcon_warning },
                    { STATUS_UNLOCK_REQUESTED, Resources.logIcon_unlock },
                    { STATUS_UNLOCKED, Resources.logIcon_unlock },
                    { STATUS_UNLOCK_ERROR, Resources.logIcon_warning },
                    { STATUS_PROTECTION_UNAVAILABLE, Resources.logIcon_warning }
                };
            return _statusIcons;
        }
    }
    /// <summary>
    /// Gets status icon.
    /// </summary>
    /// <param name="status">status參數。</param>
    /// <returns>傳回get status icon結果。</returns>
    public static Image GetStatusIcon(int status)
    {
        if (StatusIcons.TryGetValue(status, out Image? value))
        {
            return value;
        }
        else
        {
            return Resources.logIcon_systemMessage;
        }
    }
    /// <summary>
    /// Gets status class.
    /// </summary>
    /// <param name="status">status參數。</param>
    /// <returns>傳回get status class結果。</returns>
    public static string GetStatusClass(int status)
    {
        if (StatusClasses.TryGetValue(status, out string? value))
        {
            return Localization.Strings.Get(value);
        }
        else
        {
            return Localization.Strings.Get("System");
        }
    }
    /// <summary>
    /// Gets status name.
    /// </summary>
    /// <param name="status">status參數。</param>
    /// <returns>傳回get status name結果。</returns>
    public static string GetStatusName(int status)
    {
        if (StatusNames.TryGetValue(status, out string? value))
        {
            return Localization.Strings.Get(value);
        }
        else
        {
            return Localization.Strings.Format("Display name for status {0} was not found.", status);
        }
    }
    /// <summary>
    /// Reads interval.
    /// </summary>
    /// <param name="timeSpan">time span參數。</param>
    /// <returns>傳回read interval結果。</returns>
    public static IDataReader ReadInterval(TimeSpan timeSpan)
    {
        if (Database.Instance.IsConfigured)
        {
            return Database.Instance.ExecuteReader("select * from IntrusionLog where IncidentTime>@p0 order by Id desc", DateTime.UtcNow.Subtract(timeSpan));
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }

    /// <summary>
    /// Gets last log id.
    /// </summary>
    /// <returns>傳回get last log id結果。</returns>
    public static int GetLastLogId()
    {
        object? result = Database.Instance.ExecuteScalar("select Max(Id) from IntrusionLog");
        if (int.TryParse(result?.ToString(), out int maxLogId)) return maxLogId;
        return -1;
    }

    //table IntrusionLog
    //Id INTEGER PRIMARY KEY AUTOINCREMENT not null,
    //IncidentTime DateTime null,
    //AgentId uniqueidentifier null,
    //ClientIP nvarchar(80) null,
    //Action int null,
    //ActionTriggeredByUser bit null
    /// <summary>
    /// Reads interval grouped.
    /// </summary>
    /// <param name="timeSpan">time span參數。</param>
    /// <returns>傳回read interval grouped結果。</returns>
    public static IDataReader ReadIntervalGrouped(TimeSpan timeSpan)
    {
        DateTime endDate = DateTime.UtcNow;
        return ReadIntervalGrouped(endDate.Subtract(timeSpan), endDate);
    }

    /// <summary>
    /// 讀取指定半開時間區間內依 Agent、來源位址及狀態分組的事件。
    /// </summary>
    /// <param name="startDate">包含在查詢內的開始時間。</param>
    /// <param name="endDate">不包含在查詢內的結束時間。</param>
    /// <returns>分組事件資料讀取器。</returns>
    public static IDataReader ReadIntervalGrouped(DateTime startDate, DateTime endDate)
    {
        if (!Database.Instance.IsConfigured)
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        if (endDate <= startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), "結束時間必須晚於開始時間。");

        return Database.Instance.ExecuteReader(
            "select MAX(Id) as MaxId, MAX(IncidentTime) as LatestEvent, Count(*) as NumberOfEvents, AgentId, ClientIP, Action from IntrusionLog where IncidentTime>=@p0 and IncidentTime<@p1 group by AgentId, ClientIP, Action order by 1 desc",
            startDate,
            endDate);
    }
    /// <summary>
    /// Determines whether s updates.
    /// </summary>
    /// <param name="lastSequenceNumber">last sequence number參數。</param>
    /// <returns>若s updates傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool HasUpdates(int lastSequenceNumber)
    {
        if (Database.Instance.IsConfigured)
        {
            object? result = Database.Instance.ExecuteScalar("select max(Id) from IntrusionLog");
            if (result != null && int.TryParse(result.ToString(), out int lastId))
            {
                return lastSequenceNumber != lastId;
            }
            else
            {
                return false;
            }
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// Reads differential.
    /// </summary>
    /// <param name="lastSequenceNumber">last sequence number參數。</param>
    /// <returns>傳回read differential結果。</returns>
    public static IDataReader ReadDifferential(int lastSequenceNumber)
    {
        if (Database.Instance.IsConfigured)
        {
            return Database.Instance.ExecuteReader("select * from IntrusionLog where Id>@p0", lastSequenceNumber);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }

    /// <summary>
    /// Reads unsuccessful attempts.
    /// </summary>
    /// <param name="startDate">start date參數。</param>
    /// <returns>傳回read unsuccessful attempts結果。</returns>
    public static int ReadUnsuccessfulAttempts(DateTime startDate)
    {
        if (Database.Instance.IsConfigured)
        {
            int.TryParse(Database.Instance.ExecuteScalar("select count(*) from IntrusionLog where IncidentTime>@p0", startDate)?.ToString(), out int result);
            return result;
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// Adds entry.
    /// </summary>
    /// <param name="incidentTime">事件發生時間；必須為 UTC，與資料庫欄位儲存慣例一致。</param>
    /// <param name="agentId">agent id參數。</param>
    /// <param name="clientIp">client ip參數。</param>
    /// <param name="action">action參數。</param>
    /// <param name="actionTriggeredByUser">action triggered by user參數。</param>
    /// <returns>傳回add entry結果。</returns>
    public static long AddEntry(DateTime incidentTime, Guid agentId, string clientIp, int action, bool actionTriggeredByUser)
    {
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"insert into IntrusionLog(IncidentTime, AgentId, ClientIP, Action, ActionTriggeredByUser)
values (@p0,@p1,@p2,@p3,@p4) RETURNING Id";
            object? result = Database.Instance.ExecuteScalar(sqlString, incidentTime, agentId, clientIp, action, actionTriggeredByUser);
            return Db.DbValueConverter.ToInt64(result);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
    /// <summary>
    /// Gets incidents by agent id.
    /// </summary>
    /// <param name="agentId">agent id參數。</param>
    /// <param name="IpAddress">ip address參數。</param>
    /// <returns>傳回get incidents by agent id結果。</returns>
    public static int GetIncidentsByAgentId(Guid agentId, string IpAddress)
    {
        if (Database.Instance.IsConfigured)
        {
            string sqlString = @"select count(*) from IntrusionLog where AgentId=@p0 and IncidentTime>@p1 and ClientIP=@p2";
            object? queryResult = Database.Instance.ExecuteScalar(sqlString, agentId, DateTime.UtcNow.AddDays(-1), IpAddress);
            return Db.DbValueConverter.ToInt(queryResult);
        }
        else
        {
            throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Database not initialized"));
        }
    }
}
