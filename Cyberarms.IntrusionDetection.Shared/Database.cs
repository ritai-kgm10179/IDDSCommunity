using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Cyberarms.IntrusionDetection.Shared;

public class Database
{
    private bool _isConfigured = false;
    public bool IsConfigured => _isConfigured;

    private readonly SqliteConnectionStringBuilder connBuilder = [];
    private SqliteConnection? _connection;

    /// <summary>
    /// Configures requested operation.
    /// </summary>
    /// <param name="directory">The directory value.</param>

    public void Configure(string directory)
    {
        if (_connection is not null)
        {
            _connection.StateChange -= _connection_StateChange;
            _connection.Dispose();
        }

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

    /// <summary>
    /// Handles the state change event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>

    private void _connection_StateChange(object sender, StateChangeEventArgs e) => System.Diagnostics.Debug.Print("Db state {0} --> {1}", e.OriginalState, e.CurrentState);

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Database"/> class.
    /// </summary>

    private Database() { }

    /// <summary>
    /// Executes reader.
    /// </summary>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The execute reader result.</returns>

    public IDataReader ExecuteReader(string sqlString, params object[] parameters) => ExecuteReader(sqlString, null, parameters);

    /// <summary>
    /// Executes reader.
    /// </summary>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The execute reader result.</returns>

    public IDataReader ExecuteReader(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        DynamicParameters? paramObj = BuildDynamicParameters(parameters);
        try
        {
            return Connection.ExecuteReader(sqlString, paramObj, transaction);
        }
        catch
        {
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(500);
                try
                {
                    return Connection.ExecuteReader(sqlString, paramObj, transaction);
                }
                catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// Executes non query.
    /// </summary>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="parameters">The parameters value.</param>

    public void ExecuteNonQuery(string sqlString, params object[] parameters) => ExecuteNonQuery(sqlString, null, parameters);

    /// <summary>
    /// Executes non query.
    /// </summary>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="parameters">The parameters value.</param>

    public void ExecuteNonQuery(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        DynamicParameters? paramObj = BuildDynamicParameters(parameters);
        try
        {
            Connection.Execute(sqlString, paramObj, transaction);
        }
        catch
        {
            if (transaction is not null)
            {
                throw;
            }

            try
            {
                using IDbConnection conn = new SqliteConnection(Connection.ConnectionString);
                if (conn.State != ConnectionState.Open) conn.Open();
                conn.Execute(sqlString, paramObj);
            }
            catch
            {
                for (int i = 0; i < 5; i++)
                {
                    System.Threading.Thread.Sleep(500);
                    try
                    {
                        Connection.Execute(sqlString, paramObj, transaction);
                        return;
                    }
                    catch { }
                }
                throw;
            }
        }
    }

    /// <summary>
    /// Executes scalar.
    /// </summary>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The execute scalar result.</returns>

    public object? ExecuteScalar(string sqlString, params object[] parameters) => ExecuteScalar(sqlString, null, parameters);

    /// <summary>
    /// Executes scalar.
    /// </summary>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The execute scalar result.</returns>

    public object? ExecuteScalar(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        DynamicParameters? paramObj = BuildDynamicParameters(parameters);
        try
        {
            return Connection.ExecuteScalar(sqlString, paramObj, transaction);
        }
        catch
        {
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(500);
                try
                {
                    return Connection.ExecuteScalar(sqlString, paramObj, transaction);
                }
                catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// Executes the query operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="param">The param value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <returns>The query result.</returns>

    public IEnumerable<T> Query<T>(string sqlString, object? param = null, IDbTransaction? transaction = null) => Connection.Query<T>(sqlString, param, transaction);

    /// <summary>
    /// Executes the query first or default operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sqlString">The sql string value.</param>
    /// <param name="param">The param value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <returns>The query first or default result.</returns>

    public T? QueryFirstOrDefault<T>(string sqlString, object? param = null, IDbTransaction? transaction = null) => Connection.QueryFirstOrDefault<T>(sqlString, param, transaction);

    /// <summary>
    /// Builds dynamic parameters.
    /// </summary>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The build dynamic parameters result.</returns>

    private static DynamicParameters? BuildDynamicParameters(object[] parameters)
    {
        if (parameters == null || parameters.Length == 0) return null;
        DynamicParameters dynParams = new();
        for (int i = 0; i < parameters.Length; i++)
        {
            object? val = parameters[i];
            if (val is DBNull) val = null;
            dynParams.Add("p" + i, val);
        }
        return dynParams;
    }

    public int DatabaseVersion { get; set; }

    /// <summary>
    /// Opens or create.
    /// </summary>

    private void OpenOrCreate()
    {
        string? version = null;
        try
        {
            version = Connection.ExecuteScalar<string>("Select Version from DbConfig");
        }
        catch (Exception sqEx)
        {
            System.Diagnostics.Debug.Print(sqEx.Message);
        }

        if (string.IsNullOrEmpty(version))
        {
            Db.DbUpgrader upgrader = new();
            upgrader.RunUpgradeScripts(Connection);
            var versionObj = Connection.ExecuteScalar("Select Version from DbConfig");
            if (int.TryParse(versionObj?.ToString(), out int versionNumber))
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
