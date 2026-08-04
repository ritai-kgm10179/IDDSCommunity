using System;
using System.Collections.Generic;

namespace Cyberarms.IntrusionDetection.Shared;

public class Statistics
{
    private readonly List<Guid> _agentIds = [];

    private Statistics() { }

    private static Statistics? _instance;
    public static Statistics Instance => _instance ??= new();

    public void IncreaseFailedLoginStatistics(SecurityAgent agent)
    {
        if (!_agentIds.Contains(agent.Id)) ConfigureStatistics(agent);
        agent.FailedLogins++;
        IncreaseStatistics(agent, "FailedLogins");
    }

    public void IncreaseHardLockStatistics(SecurityAgent agent)
    {
        agent.HardLocks++;
        IncreaseStatistics(agent, "HardLocks");
    }

    public void ConfigureStatistics(SecurityAgent agent)
    {
        string sqlString = "select count(*) from AgentStatistics where AgentId=@p0";
        object? result = Database.Instance.ExecuteScalar(sqlString, agent.Id);
        if (Db.DbValueConverter.ToInt(result) < 1)
        {
            sqlString = "insert into AgentStatistics(AgentId, FailedLogins, SoftLocks, HardLocks) values (@p0,0,0,0)";
            Database.Instance.ExecuteNonQuery(sqlString, agent.Id);
        }
        _agentIds.Add(agent.Id);
    }

    public void IncreaseSoftLockStatistics(SecurityAgent agent)
    {
        agent.SoftLocks++;
        IncreaseStatistics(agent, "SoftLocks");
    }

    public void IncreaseStatistics(SecurityAgent agent, string statisticsColumn)
    {
        string sqlString = $"Update AgentStatistics set {statisticsColumn}={statisticsColumn}+1 where AgentId=@p0";
        Database.Instance.ExecuteNonQuery(sqlString, agent.Id);
    }
}
