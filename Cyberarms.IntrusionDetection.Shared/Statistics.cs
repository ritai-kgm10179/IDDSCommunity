using System;
using System.Collections.Generic;
using System.Threading;

namespace Cyberarms.IntrusionDetection.Shared;

public class Statistics
{
    private readonly List<Guid> _agentIds = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Statistics"/> class.
    /// </summary>

    private Statistics() { }

    private static Statistics? _instance;
    public static Statistics Instance => _instance ??= new();

    /// <summary>
    /// Executes the increase failed login statistics operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>

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
    /// Executes the increase hard lock statistics operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void IncreaseHardLockStatistics(SecurityAgent agent)
    {
        agent.HardLocks++;
        IncreaseStatistics(agent, "HardLocks");
    }

    /// <summary>
    /// Configures statistics.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void ConfigureStatistics(SecurityAgent agent)
    {
        string sqlString = "select count(*) from AgentStatistics where AgentId=@p0";
        object? result = Database.Instance.ExecuteScalar(sqlString, agent.Id);
        if (Db.DbValueConverter.ToInt(result) < 1)
        {
            sqlString = "insert into AgentStatistics(AgentId, FailedLogins, SoftLocks, HardLocks) values (@p0,0,0,0)";
            Database.Instance.ExecuteNonQuery(sqlString, agent.Id);
        }
        if (!_agentIds.Contains(agent.Id))
        {
            _agentIds.Add(agent.Id);
        }
    }

    /// <summary>
    /// Executes the increase soft lock statistics operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>

    public void IncreaseSoftLockStatistics(SecurityAgent agent)
    {
        agent.SoftLocks++;
        IncreaseStatistics(agent, "SoftLocks");
    }

    /// <summary>
    /// Executes the increase statistics operation.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    /// <param name="statisticsColumn">The statistics column value.</param>

    public static void IncreaseStatistics(SecurityAgent agent, string statisticsColumn)
    {
        string sqlString = $"Update AgentStatistics set {statisticsColumn}={statisticsColumn}+1 where AgentId=@p0";
        Database.Instance.ExecuteNonQuery(sqlString, agent.Id);
    }
}
