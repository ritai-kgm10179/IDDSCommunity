using System;
using System.Collections.Generic;
using System.Threading;

namespace IDDSCommunity.IntrusionDetection.Shared;

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
        string sqlString = $"Update AgentStatistics set {statisticsColumn}={statisticsColumn}+1 where AgentId=@p0";
        database.ExecuteNonQuery(sqlString, agent.Id);
    }
}
