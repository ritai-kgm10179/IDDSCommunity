using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.Storage;

/// <summary>
/// 定義 IDDS Community 統一資料庫存取後端介面。
/// </summary>
public interface IDatabaseBackend
{
    /// <summary>
    /// 取得資料庫後端型別。
    /// </summary>
    DatabaseBackendType BackendType { get; }

    /// <summary>
    /// 取得資料庫是否已完成初始化與設定。
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// 取得目前的資料庫連線字串或資料來源路徑。
    /// </summary>
    string DataSource { get; }

    /// <summary>
    /// 開啟或初始化資料庫連線。
    /// </summary>
    void Open();

    /// <summary>
    /// 關閉目前作用中之資料庫連線並釋放檔案或通訊控制代碼。
    /// </summary>
    void Close();

    /// <summary>
    /// 執行不傳回結果集的 SQL 命令。
    /// </summary>
    /// <param name="sqlString">SQL 命令字串。</param>
    /// <param name="parameters">命令參數陣列。</param>
    void ExecuteNonQuery(string sqlString, params object[] parameters);

    /// <summary>
    /// 於指定交易中執行不傳回結果集的 SQL 命令。
    /// </summary>
    /// <param name="sqlString">SQL 命令字串。</param>
    /// <param name="transaction">作用中的交易執行個體。</param>
    /// <param name="parameters">命令參數陣列。</param>
    void ExecuteNonQuery(string sqlString, IDbTransaction? transaction, params object[] parameters);

    /// <summary>
    /// 執行 SQL 查詢並傳回結果集中第一筆記錄的第一個資料行。
    /// </summary>
    /// <param name="sqlString">SQL 查詢字串。</param>
    /// <param name="parameters">命令參數陣列。</param>
    /// <returns>查詢結果物件；若無結果則傳回 <see langword="null"/>。</returns>
    object? ExecuteScalar(string sqlString, params object[] parameters);

    /// <summary>
    /// 執行 SQL 查詢並傳回 <see cref="IDataReader"/> 讀取器。
    /// </summary>
    /// <param name="sqlString">SQL 查詢字串。</param>
    /// <param name="parameters">命令參數陣列。</param>
    /// <returns>資料讀取器執行個體。</returns>
    IDataReader ExecuteReader(string sqlString, params object[] parameters);

    /// <summary>
    /// 非同步執行不傳回結果集的 SQL 命令。
    /// </summary>
    /// <param name="sqlString">SQL 命令字串。</param>
    /// <param name="parameters">選擇性具名參數物件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>受影響的資料列數。</returns>
    Task<int> ExecuteNonQueryAsync(string sqlString, object? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 非同步執行 SQL 查詢並傳回單一純量值。
    /// </summary>
    /// <typeparam name="T">純量型別。</typeparam>
    /// <param name="sqlString">SQL 查詢字串。</param>
    /// <param name="parameters">選擇性具名參數物件。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>查詢結果純量值。</returns>
    Task<T?> ExecuteScalarAsync<T>(string sqlString, object? parameters = null, CancellationToken cancellationToken = default);
}
