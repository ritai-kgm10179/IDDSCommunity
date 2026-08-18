using System;
using System.Collections.Generic;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供安全性事件統計彙整與儀表板圖表數據計算之核心類別。
/// </summary>
public class Statistics
{
    private readonly List<Guid> _agentIds = [];
    private readonly Lock _lock = new();
    private readonly Database database;
    /// <summary>
    /// 初始化 <see cref="Statistics"/> class的新執行個體。
    /// </summary>
    private Statistics() : this(Database.Instance) { }
    /// <summary>
    /// 初始化包含明確資料庫相依性的統計資料持久化服務。
    /// </summary>
    /// <param name="database">統計資料庫。</param>
    public Statistics(Database database)
    {
        ArgumentNullException.ThrowIfNull(database);
        this.database = database;
    }

    private static Statistics? _instance;
        /// <summary>
    /// 取得或設定 全域共用單例執行個體。
    /// </summary>
public static Statistics Instance => _instance ??= new();
    /// <summary>
    /// 執行increase failed login statistics作業。
    /// </summary>
    /// <param name="agent">agent參數。</param>
    public void IncreaseFailedLoginStatistics(SecurityAgent agent)
    {
        lock (_lock)
        {
            if (!_agentIds.Contains(agent.Id)) ConfigureStatistics(agent);
        }
        agent.FailedLogins++;
        IncreaseStatistics(agent, "FailedLogins");
    }
    /// <summary>
    /// 執行increase hard lock statistics作業。
    /// </summary>
    /// <param name="agent">agent參數。</param>
    public void IncreaseHardLockStatistics(SecurityAgent agent)
    {
        agent.HardLocks++;
        IncreaseStatistics(agent, "HardLocks");
    }
    /// <summary>
    /// 設定統計資料。
    /// </summary>
    /// <param name="agent">agent參數。</param>
    public void ConfigureStatistics(SecurityAgent agent)
    {
        string sqlString = "select count(*) from AgentStatistics where AgentId=@p0";
        object? result = database.ExecuteScalar(sqlString, agent.Id);
        if (Db.DbValueConverter.ToInt(result) < 1)
        {
            sqlString = "insert into AgentStatistics(AgentId, FailedLogins, SoftLocks, HardLocks) values (@p0,0,0,0)";
            database.ExecuteNonQuery(sqlString, agent.Id);
        }
        if (!_agentIds.Contains(agent.Id))
        {
            _agentIds.Add(agent.Id);
        }
    }
    /// <summary>
    /// 執行increase soft lock statistics作業。
    /// </summary>
    /// <param name="agent">agent參數。</param>
    public void IncreaseSoftLockStatistics(SecurityAgent agent)
    {
        agent.SoftLocks++;
        IncreaseStatistics(agent, "SoftLocks");
    }
    /// <summary>
    /// 執行increase statistics作業。
    /// </summary>
    /// <param name="agent">agent參數。</param>
    /// <param name="statisticsColumn">statistics column參數。</param>
    public void IncreaseStatistics(SecurityAgent agent, string statisticsColumn)
    {
        ArgumentNullException.ThrowIfNull(agent);
        string validatedColumn = statisticsColumn switch
        {
            "FailedLogins" => "FailedLogins",
            "HardLocks" => "HardLocks",
            "SoftLocks" => "SoftLocks",
            _ => throw new ArgumentException(Localization.Strings.Get("Unsupported statistics column."), nameof(statisticsColumn))
        };
        string sqlString = $"Update AgentStatistics set {validatedColumn}={validatedColumn}+1 where AgentId=@p0";
        database.ExecuteNonQuery(sqlString, agent.Id);
    }
}
