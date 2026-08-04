using System;
using System.Data;
using Microsoft.Data.Sqlite;

namespace Cyberarms.IntrusionDetection.Shared;

public class Database
{
    private bool _isConfigured = false;
    public bool IsConfigured => _isConfigured;

    private readonly SqliteConnectionStringBuilder connBuilder = new();
    private SqliteConnection? _connection;

    public void Configure(string directory)
    {
        connBuilder.DataSource = System.IO.Path.Combine(directory, "cyberarms.idds.dbf");
        connBuilder.Mode = SqliteOpenMode.ReadWriteCreate;
        connBuilder.Cache = SqliteCacheMode.Shared;

        string? dbDir = System.IO.Path.GetDirectoryName(connBuilder.DataSource);
        if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
        {
            try
            {
                System.IO.Directory.CreateDirectory(dbDir);
            }
            catch { }
        }

        _connection = new SqliteConnection(connBuilder.ConnectionString);
        _connection.Open();
        _connection.StateChange += _connection_StateChange;
        _isConfigured = true;

        OpenOrCreate();
    }

    private void _connection_StateChange(object sender, StateChangeEventArgs e)
    {
        System.Diagnostics.Debug.Print("Db state {0} --> {1}", e.OriginalState, e.CurrentState);
    }

    public SqliteConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                throw new ApplicationException("Sorry, cannot return requested connection object. Please run Configure first to set database path.");
            }
            if (_connection.State == ConnectionState.Broken || _connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
            return _connection;
        }
    }

    private static Database? _instance;
    public static Database Instance
    {
        get
        {
            _instance ??= new Database();
            return _instance;
        }
    }

    private Database() { }

    public IDataReader ExecuteReader(string sqlString, params object[] parameters)
    {
        return ExecuteReader(sqlString, null, parameters);
    }

    public IDataReader ExecuteReader(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        IDbCommand cmd = PrepareCommand(sqlString, parameters);
        if (transaction != null) cmd.Transaction = transaction;
        try
        {
            return cmd.ExecuteReader();
        }
        catch
        {
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(500);
                try
                {
                    return cmd.ExecuteReader();
                }
                catch { }
            }
            throw;
        }
    }

    public void ExecuteNonQuery(string sqlString, params object[] parameters)
    {
        ExecuteNonQuery(sqlString, null, parameters);
    }

    public void ExecuteNonQuery(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        IDbCommand cmd = PrepareCommand(sqlString, parameters);
        try
        {
            if (transaction != null) cmd.Transaction = transaction;
            cmd.ExecuteNonQuery();
        }
        catch
        {
            try
            {
                using IDbConnection conn = new SqliteConnection(Connection.ConnectionString);
                if (conn.State != ConnectionState.Open) conn.Open();
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
            }
            catch
            {
                for (int i = 0; i < 5; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return;
                    }
                    catch { }
                }
                throw;
            }
        }
    }

    private IDbCommand PrepareCommand(string sqlString, params object[] parameters)
    {
        IDbCommand cmd = Connection.CreateCommand();
        cmd.CommandText = sqlString;
        cmd.CommandType = CommandType.Text;
        for (int i = 0; i < parameters.Length; i++)
        {
            IDbDataParameter p = cmd.CreateParameter();
            p.ParameterName = "@p" + i;
            p.Value = parameters[i] ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        cmd.Prepare();
        return cmd;
    }

    public object? ExecuteScalar(string sqlString, params object[] parameters)
    {
        return ExecuteScalar(sqlString, null, parameters);
    }

    public object? ExecuteScalar(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        IDbCommand cmd = PrepareCommand(sqlString, parameters);
        if (transaction != null) cmd.Transaction = transaction;

        try
        {
            return cmd.ExecuteScalar();
        }
        catch
        {
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(500);
                try
                {
                    return cmd.ExecuteScalar();
                }
                catch { }
            }
            throw;
        }
    }

    public int DatabaseVersion { get; set; }

    private void OpenOrCreate()
    {
        IDbCommand cmd = Connection.CreateCommand();
        string? version = null;
        try
        {
            cmd.CommandText = "Select Version from DbConfig";
            version = cmd.ExecuteScalar()?.ToString();
        }
        catch (Exception sqEx)
        {
            System.Diagnostics.Debug.Print(sqEx.Message);
        }

        if (string.IsNullOrEmpty(version))
        {
            Db.DbUpgrader upgrader = new();
            upgrader.RunUpgradeScripts(Connection);
            if (int.TryParse(cmd.ExecuteScalar()?.ToString(), out int versionNumber))
            {
                DatabaseVersion = versionNumber;
            }
            else
            {
                throw new ApplicationException("Error while accessing or creating the database");
            }
        }
        else
        {
            int.TryParse(version, out int versionNumber);
            DatabaseVersion = versionNumber;
        }
    }
}
