using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Polly;
using Polly.Retry;

namespace Cyberarms.IntrusionDetection.Shared;

public class Database
{
    private const int MaximumRetryCount = 5;
    private static readonly ResiliencePipeline SqlitePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<SqliteException>(IsTransient),
            MaxRetryAttempts = MaximumRetryCount,
            Delay = TimeSpan.FromMilliseconds(100),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .Build();
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
                throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Sorry, cannot return requested connection object. Please run Configure first to set database path."));
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
        if (transaction is not null)
            return Connection.ExecuteReader(sqlString, paramObj, transaction);
        return SqlitePipeline.Execute(() => Connection.ExecuteReader(sqlString, paramObj));
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
        if (transaction is not null)
            Connection.Execute(sqlString, paramObj, transaction);
        else
            SqlitePipeline.Execute(() => Connection.Execute(sqlString, paramObj));
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
        if (transaction is not null)
            return Connection.ExecuteScalar(sqlString, paramObj, transaction);
        return SqlitePipeline.Execute(() => Connection.ExecuteScalar(sqlString, paramObj));
    }

    /// <summary>
    /// Asynchronously executes a command using an independently owned database connection.
    /// </summary>
    /// <param name="sqlString">The parameterized SQL command to execute.</param>
    /// <param name="parameters">The named parameter values, or <see langword="null"/> when none are required.</param>
    /// <param name="cancellationToken">A token that cancels opening the connection, retry delays, and command execution.</param>
    /// <returns>A task whose result is the number of affected rows.</returns>
    public async Task<int> ExecuteNonQueryAsync(string sqlString, object? parameters = null, CancellationToken cancellationToken = default) =>
        await SqlitePipeline.ExecuteAsync(async token =>
        {
            await using SqliteConnection connection = new(connBuilder.ConnectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            CommandDefinition command = new(sqlString, parameters, cancellationToken: token);
            return await connection.ExecuteAsync(command).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously returns the first column of the first row produced by a query.
    /// </summary>
    /// <typeparam name="T">The expected scalar result type.</typeparam>
    /// <param name="sqlString">The parameterized SQL query to execute.</param>
    /// <param name="parameters">The named parameter values, or <see langword="null"/> when none are required.</param>
    /// <param name="cancellationToken">A token that cancels opening the connection, retry delays, and query execution.</param>
    /// <returns>A task whose result is the converted scalar value, or the default value when no value is returned.</returns>
    public async Task<T?> ExecuteScalarAsync<T>(string sqlString, object? parameters = null, CancellationToken cancellationToken = default) =>
        await SqlitePipeline.ExecuteAsync(async token =>
        {
            await using SqliteConnection connection = new(connBuilder.ConnectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            CommandDefinition command = new(sqlString, parameters, cancellationToken: token);
            return await connection.ExecuteScalarAsync<T>(command).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Asynchronously executes a parameterized query and materializes its rows.
    /// </summary>
    /// <typeparam name="T">The row type to materialize.</typeparam>
    /// <param name="sqlString">The parameterized SQL query to execute.</param>
    /// <param name="parameters">The named parameter values, or <see langword="null"/> when none are required.</param>
    /// <param name="cancellationToken">A token that cancels opening the connection and query execution.</param>
    /// <returns>A task whose result contains the materialized rows.</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sqlString, object? parameters = null, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = new(connBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        CommandDefinition command = new(sqlString, parameters, cancellationToken: cancellationToken);
        return await connection.QueryAsync<T>(command).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether SQLite reported a transient busy or locked condition.
    /// </summary>
    /// <param name="exception">The SQLite exception.</param>
    /// <returns><see langword="true"/> when retrying may succeed.</returns>
    private static bool IsTransient(SqliteException exception) => exception.SqliteErrorCode is 5 or 6;

    /// <summary>
    /// Calculates the bounded exponential retry delay.
    /// </summary>
    /// <param name="attempt">The zero-based retry attempt.</param>
    /// <returns>The delay before the next attempt.</returns>
    private static TimeSpan GetRetryDelay(int attempt) => TimeSpan.FromMilliseconds(50 * (1 << Math.Min(attempt, 4)));

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
                throw new ApplicationException(global::Cyberarms.IntrusionDetection.Shared.Localization.Strings.Get("Error while accessing or creating the database"));
            }
        }
        else
        {
            int.TryParse(version, out int versionNumber);
            DatabaseVersion = versionNumber;
        }
    }
}
