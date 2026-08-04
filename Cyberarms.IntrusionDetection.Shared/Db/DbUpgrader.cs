using System;
using System.Collections.Generic;


namespace Cyberarms.IntrusionDetection.Shared.Db;

public class DbUpgrader
{



    /// <summary>
    /// Executes the run upgrade scripts operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>

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
    /// Executes the upgrade all operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="latestVersionNumber">The latest version number value.</param>

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
    /// Executes the init scripts operation.
    /// </summary>

    private void InitScripts()
    {
        upgradeScripts = new SortedList<int, DbUpgradeScript>
        {
            { 1, new Version_2_1() }
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbUpgrader"/> class.
    /// </summary>

    public DbUpgrader()
    {

    }
}
