using System;


namespace Cyberarms.IntrusionDetection.Shared.Db;

public class DbUpgradeScript
{
    public virtual int INTERNAL_VERSION => 0;

    public virtual void UpgradeDatabase(System.Data.IDbConnection connection)
    {

    }

    internal static void RunCommand(System.Data.IDbConnection connection, string command)
    {
        System.Data.IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = command;
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw;
        }

    }
}
