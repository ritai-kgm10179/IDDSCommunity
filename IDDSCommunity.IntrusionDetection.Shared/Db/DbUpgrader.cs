using System;
using System.Collections.Generic;


namespace IDDSCommunity.IntrusionDetection.Shared.Db;

public class DbUpgrader
{



    /// <summary>
    /// 執行run upgrade scripts作業。
    /// </summary>
    /// <param name="connection">connection參數。</param>

    public void RunUpgradeScripts(System.Data.IDbConnection connection)
    {
        System.Data.IDbCommand cmd = connection.CreateCommand();
        cmd.Connection = connection;
        cmd.CommandText = "SELECT VersionNumber from DbConfig order by VersionNumber desc LIMIT 1";
        int latestVersion = 0;
        object? result;
        try
        {
            result = cmd.ExecuteScalar();
        }
        catch (System.Data.Common.DbException)
        {
            result = null;
            latestVersion = 0;
        }
        catch (Exception)
        {
            throw;
        }
        if (result != null && !string.IsNullOrEmpty(result.ToString()))
        {
            if (!int.TryParse(result.ToString(), out latestVersion))
            {
                latestVersion = 0;
            }
        }
        InitScripts();
        UpgradeAll(connection, latestVersion);
    }

    /// <summary>
    /// 執行upgrade all作業。
    /// </summary>
    /// <param name="connection">connection參數。</param>
    /// <param name="latestVersionNumber">latest version number參數。</param>

    public void UpgradeAll(System.Data.IDbConnection connection, int latestVersionNumber)
    {
        foreach (int key in upgradeScripts.Keys)
        {
            if (key > latestVersionNumber)
            {
                upgradeScripts[key].UpgradeDatabase(connection);
            }
        }
    }


    private SortedList<int, DbUpgradeScript> upgradeScripts = [];

    /// <summary>
    /// 執行init scripts作業。
    /// </summary>

    private void InitScripts()
    {
        upgradeScripts = new SortedList<int, DbUpgradeScript>
        {
            { 1, new Version_2_1() }
        };
    }

    /// <summary>
    /// 初始化 <see cref="DbUpgrader"/> class的新執行個體。
    /// </summary>

    public DbUpgrader()
    {

    }
}
