using System;


namespace IDDSCommunity.IntrusionDetection.Shared.Db;

/// <summary>
/// 代表資料庫結構版本升級腳本之抽象基底類別。
/// </summary>
public class DbUpgradeScript
{
        /// <summary>
    /// 取得或設定 INTERNAL_VERSION。
    /// </summary>
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
        using System.Data.IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = command;
        cmd.ExecuteNonQuery();
    }
}
