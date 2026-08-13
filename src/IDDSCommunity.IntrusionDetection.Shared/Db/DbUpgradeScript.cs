using System;


namespace IDDSCommunity.IntrusionDetection.Shared.Db;

public class DbUpgradeScript
{
    public virtual int INTERNAL_VERSION => 0;
    /// <summary>
    /// 執行upgrade database作業。
    /// </summary>
    /// <param name="connection">connection參數。</param>
    public virtual void UpgradeDatabase(System.Data.IDbConnection connection)
    {

    }
    /// <summary>
    /// 執行run command作業。
    /// </summary>
    /// <param name="connection">connection參數。</param>
    /// <param name="command">command參數。</param>
    internal static void RunCommand(System.Data.IDbConnection connection, string command)
    {
        System.Data.IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = command;
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (Exception)
        {
            throw;
        }

    }
}
