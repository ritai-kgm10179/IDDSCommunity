using System;

namespace IDDSCommunity.IntrusionDetection.Shared.Storage;

/// <summary>
/// 定義 IDDS Community 支援之資料庫後端儲存引擎型別。
/// </summary>
public enum DatabaseBackendType
{
    /// <summary>
    /// 本機 ChaCha20-Poly1305 + Windows DPAPI 透明加密 SQLite 資料庫（預設核心機制）。
    /// </summary>
    SQLite = 0,

    /// <summary>
    /// 企業集中式 PostgreSQL 資料庫。
    /// </summary>
    PostgreSQL = 1,

    /// <summary>
    /// 企業集中式 Microsoft SQL Server / Azure SQL 資料庫。
    /// </summary>
    SqlServer = 2
}
