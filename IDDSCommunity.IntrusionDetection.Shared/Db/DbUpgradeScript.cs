using System;


namespace IDDSCommunity.IntrusionDetection.Shared.Db;

public class DbUpgradeScript
{
    public virtual int INTERNAL_VERSION => 0;

    /// <summary>
    /// Executes the upgrade database operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>

    public virtual void UpgradeDatabase(System.Data.IDbConnection connection)
    {

    }

    /// <summary>
    /// Executes the run command operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="command">The command value.</param>

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
