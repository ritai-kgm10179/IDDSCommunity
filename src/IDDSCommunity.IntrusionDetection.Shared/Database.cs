using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Polly;
using Polly.Retry;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供 SQLite 頁面加密資料庫連線管理、結構升級、交易執行與查詢封裝之主要資料存取類別。
/// </summary>
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
        /// <summary>
    /// 取得或設定 IsConfigured。
    /// </summary>
public bool IsConfigured => _isConfigured;

    private readonly SqliteConnectionStringBuilder connBuilder = [];
    private SqliteConnection? _connection;
    private string databasePassword = string.Empty;
    private static int sqliteInitialized;
    /// <summary>
    /// 取得 absolute path of the configured SQLite database.
    /// </summary>
    public string DataSource => connBuilder.DataSource;
    /// <summary>
    /// Closes the active database connection and releases its file handle.
    /// </summary>
    public void Close()
    {
        if (_connection is null)
            return;
        _connection.StateChange -= _connection_StateChange;
        _connection.Dispose();
        _connection = null;
        SqliteConnection.ClearAllPools();
        connBuilder.Password = string.Empty;
        databasePassword = string.Empty;
        _isConfigured = false;
    }
    /// <summary>
    /// Configures requested operation.
    /// </summary>
    /// <param name="directory">The directory containing the database.</param>
    /// <param name="fileName">The database file name without directory components.</param>
    public void Configure(string directory, string fileName = "iddscommunity.dbf")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        _instance = this;
        if (!string.Equals(System.IO.Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            throw new ArgumentException(Localization.Strings.Get("The database file name cannot contain a directory path."), nameof(fileName));

        if (_connection is not null)
        {
            _connection.StateChange -= _connection_StateChange;
            _connection.Dispose();
        }

        EnsureSqliteInitialized();
        connBuilder.DataSource = System.IO.Path.Combine(directory, fileName);
        connBuilder.Mode = SqliteOpenMode.ReadWriteCreate;
        connBuilder.Cache = SqliteCacheMode.Private;
        connBuilder.Pooling = true;
        connBuilder.DefaultTimeout = 5;

        string? dbDir = System.IO.Path.GetDirectoryName(connBuilder.DataSource);
        if (!string.IsNullOrEmpty(dbDir) && !System.IO.Directory.Exists(dbDir))
            System.IO.Directory.CreateDirectory(dbDir);

        CleanupStrayMigrationArtifacts(connBuilder.DataSource);
        bool databaseExists = File.Exists(connBuilder.DataSource);
        bool isPlaintext = databaseExists && HasPlaintextHeader(connBuilder.DataSource);
        databasePassword = DatabaseEncryptionKeyStore.GetPassword(
            connBuilder.DataSource,
            allowCreate: !databaseExists || isPlaintext || File.Exists(DatabaseEncryptionKeyStore.GetKeyPath(connBuilder.DataSource)));
        if (isPlaintext)
            MigratePlaintextDatabase(connBuilder.DataSource, databasePassword);
        connBuilder.Password = databasePassword;

        try
        {
            _connection = new SqliteConnection(connBuilder.ConnectionString);
            _connection.Open();
            ConfigureConnection(_connection);
            _connection.StateChange += _connection_StateChange;
            _isConfigured = true;

            OpenOrCreate();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 8) // SQLITE_READONLY
        {
            _connection?.Dispose();
            connBuilder.Mode = SqliteOpenMode.ReadOnly;
            _connection = new SqliteConnection(connBuilder.ConnectionString);
            _connection.Open();
            ConfigureConnection(_connection);
            _connection.StateChange += _connection_StateChange;
            _isConfigured = true;
        }
    }

    /// <summary>
    /// 處理 state change 事件。
    /// </summary>
    /// <param name="sender">事件來源物件。</param>
    /// <param name="e">事件資料。</param>
    private void _connection_StateChange(object sender, StateChangeEventArgs e) => System.Diagnostics.Debug.Print("Db state {0} --> {1}", e.OriginalState, e.CurrentState);

        /// <summary>
    /// 取得或設定 Connection。
    /// </summary>
public SqliteConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Sorry, cannot return requested connection object. Please run Configure first to set database path."));
            }
            if (_connection.State == ConnectionState.Broken || _connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
            return _connection;
        }
    }

    private static Database? _instance;
        /// <summary>
    /// 取得或設定 全域共用單例執行個體。
    /// </summary>
public static Database Instance
    {
        get
        {
            _instance ??= new Database();
            return _instance;
        }
    }
    /// <summary>
    /// 初始化 <see cref="Database"/> class的新執行個體。
    /// </summary>
    public Database() => _instance = this;
    /// <summary>
    /// Executes reader.
    /// </summary>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="parameters">parameters參數。</param>
    /// <returns>傳回execute reader結果。</returns>
    public IDataReader ExecuteReader(string sqlString, params object[] parameters) => ExecuteReader(sqlString, null, parameters);
    /// <summary>
    /// Executes reader.
    /// </summary>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="transaction">transaction參數。</param>
    /// <param name="parameters">parameters參數。</param>
    /// <returns>傳回execute reader結果。</returns>
    public IDataReader ExecuteReader(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        DynamicParameters? paramObj = BuildDynamicParameters(parameters);
        if (transaction is not null)
            return transaction.Connection!.ExecuteReader(sqlString, paramObj, transaction);
        return SqlitePipeline.Execute(() =>
        {
            using SqliteConnection connection = OpenConnection();
            using IDataReader reader = connection.ExecuteReader(sqlString, paramObj);
            DataTable table = new();
            table.Load(reader);
            return table.CreateDataReader();
        });
    }
    /// <summary>
    /// Executes non query.
    /// </summary>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="parameters">parameters參數。</param>
    public void ExecuteNonQuery(string sqlString, params object[] parameters) => ExecuteNonQuery(sqlString, null, parameters);
    /// <summary>
    /// Executes non query.
    /// </summary>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="transaction">transaction參數。</param>
    /// <param name="parameters">parameters參數。</param>
    public void ExecuteNonQuery(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        DynamicParameters? paramObj = BuildDynamicParameters(parameters);
        if (transaction is not null)
            transaction.Connection!.Execute(sqlString, paramObj, transaction);
        else
            SqlitePipeline.Execute(() =>
            {
                using SqliteConnection connection = OpenConnection();
                connection.Execute(sqlString, paramObj);
            });
    }
    /// <summary>
    /// Executes scalar.
    /// </summary>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="parameters">parameters參數。</param>
    /// <returns>傳回execute scalar結果。</returns>
    public object? ExecuteScalar(string sqlString, params object[] parameters) => ExecuteScalar(sqlString, null, parameters);
    /// <summary>
    /// Executes scalar.
    /// </summary>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="transaction">transaction參數。</param>
    /// <param name="parameters">parameters參數。</param>
    /// <returns>傳回execute scalar結果。</returns>
    public object? ExecuteScalar(string sqlString, IDbTransaction? transaction, params object[] parameters)
    {
        DynamicParameters? paramObj = BuildDynamicParameters(parameters);
        if (transaction is not null)
            return transaction.Connection!.ExecuteScalar(sqlString, paramObj, transaction);
        return SqlitePipeline.Execute(() =>
        {
            using SqliteConnection connection = OpenConnection();
            return connection.ExecuteScalar(sqlString, paramObj);
        });
    }
    /// <summary>
    /// Asynchronously executes a command using an independently owned database connection.
    /// </summary>
    /// <param name="sqlString">The parameterized SQL command to execute.</param>
    /// <param name="parameters">The named parameter values, or <see langword="null"/> when none are required.</param>
    /// <param name="cancellationToken">A token that cancels opening the connection, retry delays, and command execution.</param>
    /// <returns>包含受影響資料列數量的非同步 Task。</returns>
    public async Task<int> ExecuteNonQueryAsync(string sqlString, object? parameters = null, CancellationToken cancellationToken = default) =>
        await SqlitePipeline.ExecuteAsync(async token =>
        {
            await using SqliteConnection connection = await OpenConnectionAsync(token).ConfigureAwait(false);
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
    /// <returns>包含轉換後純量值的非同步 Task。</returns>
    public async Task<T?> ExecuteScalarAsync<T>(string sqlString, object? parameters = null, CancellationToken cancellationToken = default) =>
        await SqlitePipeline.ExecuteAsync(async token =>
        {
            await using SqliteConnection connection = await OpenConnectionAsync(token).ConfigureAwait(false);
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
    /// <returns>包含實體化資料列集合的非同步 Task。</returns>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sqlString, object? parameters = null, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        CommandDefinition command = new(sqlString, parameters, cancellationToken: cancellationToken);
        return await connection.QueryAsync<T>(command).ConfigureAwait(false);
    }
    /// <summary>
    /// Executes an asynchronous unit of work inside an independently owned SQLite transaction.
    /// </summary>
    /// <param name="operation">The transaction operation.</param>
    /// <param name="cancellationToken">Cancels connection opening, the operation, or commit.</param>
    /// <returns>表示非同步工作完成的 Task。</returns>
    public async Task ExecuteInTransactionAsync(
        Func<SqliteConnection, SqliteTransaction, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using SqliteConnection connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation(connection, transaction, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
    /// <summary>
    /// Executes a synchronous unit of work inside an independently owned SQLite transaction.
    /// </summary>
    /// <param name="operation">The transaction operation.</param>
    public void ExecuteInTransaction(Action<SqliteConnection, SqliteTransaction> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        SqlitePipeline.Execute(() =>
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                operation(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        });
    }
    /// <summary>
    /// Opens and configures an independently owned pooled SQLite connection.
    /// </summary>
    /// <returns>傳回 open connection 的結果。</returns>
    private SqliteConnection OpenConnection()
    {
        if (!_isConfigured)
            throw new InvalidOperationException(Localization.Strings.Get("Database is not configured yet. Please configure database and re-try this operation!"));
        SqliteConnection connection = new(connBuilder.ConnectionString);
        try
        {
            connection.Open();
            ConfigureConnection(connection);
            return connection;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 8) // SQLITE_READONLY
        {
            connection.Dispose();
            SqliteConnectionStringBuilder roBuilder = new(connBuilder.ConnectionString)
            {
                Mode = SqliteOpenMode.ReadOnly
            };
            SqliteConnection roConn = new(roBuilder.ConnectionString);
            roConn.Open();
            ConfigureConnection(roConn);
            return roConn;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
    /// <summary>
    /// Opens and configures an independently owned pooled SQLite connection.
    /// </summary>
    /// <param name="cancellationToken">Cancels opening the connection.</param>
    /// <returns>傳回 open connection 的結果。</returns>
    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (!_isConfigured)
            throw new InvalidOperationException(Localization.Strings.Get("Database is not configured yet. Please configure database and re-try this operation!"));
        SqliteConnection connection = new(connBuilder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            ConfigureConnection(connection);
            return connection;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 8) // SQLITE_READONLY
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            SqliteConnectionStringBuilder roBuilder = new(connBuilder.ConnectionString)
            {
                Mode = SqliteOpenMode.ReadOnly
            };
            SqliteConnection roConn = new(roBuilder.ConnectionString);
            await roConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            ConfigureConnection(roConn);
            return roConn;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
    /// <summary>
    /// Applies connection-local integrity and contention settings.
    /// </summary>
    /// <param name="connection">已開啟的 SQLite 資料庫連線。</param>
    private static void ConfigureConnection(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000; PRAGMA memory_security=ON;";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 建立使用目前資料庫密鑰且不共用連線集區的 SQLite 連線。
    /// </summary>
    /// <param name="path">欲開啟的 SQLite 資料庫路徑。</param>
    /// <param name="mode">資料庫開啟模式。</param>
    /// <returns>尚未開啟的加密 SQLite 連線。</returns>
    internal SqliteConnection CreateEncryptedConnection(string path, SqliteOpenMode mode) => new(new SqliteConnectionStringBuilder
    {
        DataSource = Path.GetFullPath(path),
        Mode = mode,
        Pooling = false,
        Password = databasePassword
    }.ConnectionString);

    private static void EnsureSqliteInitialized()
    {
        if (Interlocked.Exchange(ref sqliteInitialized, 1) == 0)
            SQLitePCL.Batteries_V2.Init();
    }

    private static bool HasPlaintextHeader(string path)
    {
        ReadOnlySpan<byte> expected = "SQLite format 3\0"u8;
        Span<byte> actual = stackalloc byte[16];
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return stream.Read(actual) == actual.Length && actual.SequenceEqual(expected);
    }

    private static void MigratePlaintextDatabase(string path, string password)
    {
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException(Localization.Strings.Get("The database directory is unavailable."));
        string candidate = Path.Combine(directory, $".{Path.GetFileName(path)}.encrypted-{Guid.NewGuid():N}");
        string rollback = Path.Combine(directory, $".{Path.GetFileName(path)}.plaintext-rollback-{Guid.NewGuid():N}");
        string lockPath = path + ".migration.lock";
        FileStream migrationLock = AcquireMigrationLock(lockPath);
        try
        {
            if (!HasPlaintextHeader(path))
                return;

            try
            {
                using (SqliteConnection source = new(new SqliteConnectionStringBuilder
                {
                    DataSource = path,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ConnectionString))
                {
                    source.Open();
                    using SqliteCommand snapshot = source.CreateCommand();
                    snapshot.CommandText = "VACUUM INTO $path";
                    snapshot.Parameters.AddWithValue("$path", candidate);
                    snapshot.ExecuteNonQuery();
                }
                using (SqliteConnection destination = new(new SqliteConnectionStringBuilder
                {
                    DataSource = candidate,
                    Mode = SqliteOpenMode.ReadWrite,
                    Pooling = false
                }.ConnectionString))
                {
                    destination.Open();
                    using (SqliteCommand encrypt = destination.CreateCommand())
                    {
                        encrypt.CommandText = $"PRAGMA rekey = '{password.Replace("'", "''", StringComparison.Ordinal)}'";
                        encrypt.ExecuteNonQuery();
                    }
                    ConfigureConnection(destination);
                    using SqliteCommand check = destination.CreateCommand();
                    check.CommandText = "PRAGMA integrity_check";
                    if (!string.Equals(Convert.ToString(check.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException(Localization.Strings.Get("The encrypted database migration failed its integrity check."));
                }

                SqliteConnection.ClearAllPools();
                File.Replace(candidate, path, rollback, true);
                DeleteIfExists(path + "-wal");
                DeleteIfExists(path + "-shm");
                using SqliteConnection verification = new(new SqliteConnectionStringBuilder
                {
                    DataSource = path,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false,
                    Password = password
                }.ConnectionString);
                verification.Open();
                using SqliteCommand verificationCommand = verification.CreateCommand();
                verificationCommand.CommandText = "PRAGMA integrity_check";
                if (!string.Equals(Convert.ToString(verificationCommand.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(Localization.Strings.Get("The migrated encrypted database failed verification."));
                SecureDeleteIfExists(rollback);
            }
            catch
            {
                if (File.Exists(rollback))
                    File.Replace(rollback, path, null, true);
                throw;
            }
            finally
            {
                DeleteIfExists(candidate);
            }
        }
        finally
        {
            migrationLock.Dispose();
            DeleteIfExists(lockPath);
        }
    }

    /// <summary>
    /// 取得遷移鎖，若目前被另一個處理程序持有（例如服務與管理主控台同時啟動並同時偵測到明文資料庫），
    /// 會以短暫延遲重試數次，而非立即失敗，以提高並行啟動情境下的可靠性。
    /// </summary>
    /// <param name="lockPath">遷移鎖檔案路徑。</param>
    /// <returns>已取得獨佔存取權的檔案串流。</returns>
    private static FileStream AcquireMigrationLock(string lockPath)
    {
        const int maxAttempts = 10;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(200);
            }
        }
    }

    /// <summary>
    /// 清除前次明文資料庫遷移意外中斷（例如處理程序遭終止或電源中斷）後可能殘留的暫存明文回滾檔案，
    /// 避免其違反「不得留下明文備份或回滾副本」的保證而永久留存於磁碟。
    /// 會先嘗試取得與 <see cref="MigratePlaintextDatabase"/> 相同的遷移鎖，若鎖定失敗代表另一個
    /// 處理程序正在進行中的遷移，此時不觸碰任何暫存檔案。
    /// </summary>
    /// <param name="path">SQLite 資料庫的完整路徑。</param>
    private static void CleanupStrayMigrationArtifacts(string path)
    {
        string? directory = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return;

        string lockPath = path + ".migration.lock";
        FileStream migrationLock;
        try
        {
            migrationLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            // 另一個處理程序正持有遷移鎖，代表遷移正在進行中，不得干擾其暫存檔案。
            return;
        }

        try
        {
            string fileName = System.IO.Path.GetFileName(path);
            foreach (string stray in Directory.EnumerateFiles(directory, $".{fileName}.plaintext-rollback-*"))
                SecureDeleteIfExists(stray);
            foreach (string stray in Directory.EnumerateFiles(directory, $".{fileName}.encrypted-*"))
                DeleteIfExists(stray);
        }
        finally
        {
            migrationLock.Dispose();
            DeleteIfExists(lockPath);
        }
    }

    /// <summary>
    /// 以零位元組覆寫檔案內容後再刪除，降低殘留明文資料庫副本可被復原的風險。
    /// </summary>
    /// <param name="path">欲安全刪除的檔案路徑。</param>
    private static void SecureDeleteIfExists(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            long length = new FileInfo(path).Length;
            if (length > 0)
            {
                using FileStream stream = new(path, FileMode.Open, FileAccess.Write, FileShare.None);
                byte[] zeros = new byte[(int)Math.Min(length, 1024 * 1024)];
                long remaining = length;
                while (remaining > 0)
                {
                    int chunk = (int)Math.Min(remaining, zeros.Length);
                    stream.Write(zeros, 0, chunk);
                    remaining -= chunk;
                }
                stream.Flush(true);
            }
        }
        catch (IOException)
        {
            // 覆寫失敗仍嘗試刪除，避免殘留檔案永久留存；覆寫僅為縱深防禦，非唯一保護機制。
        }
        File.Delete(path);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
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
    /// <returns>傳回 delay before the next attempt 的結果。</returns>
    private static TimeSpan GetRetryDelay(int attempt) => TimeSpan.FromMilliseconds(50 * (1 << Math.Min(attempt, 4)));
    /// <summary>
    /// 執行query作業。
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="param">param參數。</param>
    /// <param name="transaction">transaction參數。</param>
    /// <returns>傳回query結果。</returns>
    public IEnumerable<T> Query<T>(string sqlString, object? param = null, IDbTransaction? transaction = null)
    {
        if (transaction is not null)
            return transaction.Connection!.Query<T>(sqlString, param, transaction).AsList();
        return SqlitePipeline.Execute(() =>
        {
            using SqliteConnection connection = OpenConnection();
            return connection.Query<T>(sqlString, param).AsList();
        });
    }
    /// <summary>
    /// 執行query first or default作業。
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sqlString">sql string參數。</param>
    /// <param name="param">param參數。</param>
    /// <param name="transaction">transaction參數。</param>
    /// <returns>傳回query first or default結果。</returns>
    public T? QueryFirstOrDefault<T>(string sqlString, object? param = null, IDbTransaction? transaction = null)
    {
        if (transaction is not null)
            return transaction.Connection!.QueryFirstOrDefault<T>(sqlString, param, transaction);
        return SqlitePipeline.Execute(() =>
        {
            using SqliteConnection connection = OpenConnection();
            return connection.QueryFirstOrDefault<T>(sqlString, param);
        });
    }
    /// <summary>
    /// Builds dynamic parameters.
    /// </summary>
    /// <param name="parameters">parameters參數。</param>
    /// <returns>傳回build dynamic parameters結果。</returns>
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

        /// <summary>
    /// 取得或設定 DatabaseVersion。
    /// </summary>
public int DatabaseVersion { get; set; }
    /// <summary>
    /// Opens or create.
    /// </summary>
    private void OpenOrCreate()
    {
        Connection.Execute("PRAGMA journal_mode=WAL");
        Db.SchemaMigrationRunner.Migrate(Connection);
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
            var versionObj = Connection.ExecuteScalar("Select Version from DbConfig");
            if (int.TryParse(versionObj?.ToString(), out int versionNumber))
            {
                DatabaseVersion = versionNumber;
            }
            else
            {
                throw new ApplicationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Error while accessing or creating the database"));
            }
        }
        else
        {
            int.TryParse(version, out int versionNumber);
            DatabaseVersion = versionNumber;
        }
    }
}
