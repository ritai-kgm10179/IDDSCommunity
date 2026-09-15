using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dapper;
using Microsoft.Data.Sqlite;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 定義防火牆異動的種類。
/// </summary>
public enum FirewallChangeOperation
{
    /// <summary>
    /// 新增或更新封鎖。
    /// </summary>
    Upsert = 1,

    /// <summary>
    /// 移除封鎖。
    /// </summary>
    Remove = 2,

    /// <summary>
    /// 修復外部漂移。
    /// </summary>
    Repair = 3
}

/// <summary>
/// 定義防火牆異動的處理狀態。
/// </summary>
public enum FirewallChangeProcessingState
{
    /// <summary>
    /// 尚待處理。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已成功完成。
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 已成功完成。
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 處理失敗且可供後續重試或診斷。
    /// </summary>
    Failed = 3
}

/// <summary>
/// 表示一筆來源擁有的防火牆期望位址。
/// </summary>
public sealed record FirewallDesiredAddress
{
    /// <summary>
    /// 取得或設定 位址或前綴字串。
    /// </summary>
    public string AddressKey { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 位址家族（IPv4 或 IPv6）。
    /// </summary>
    public int AddressFamily { get; init; }

    /// <summary>
    /// 取得或設定 網路位元組陣列。
    /// </summary>
    public byte[] NetworkBytes { get; init; } = [];

    /// <summary>
    /// 取得或設定 前綴長度。
    /// </summary>
    public int PrefixLength { get; init; }

    /// <summary>
    /// 取得或設定 期望狀態。
    /// </summary>
    public int DesiredState { get; init; }

    /// <summary>
    /// 取得或設定 來源識別碼。
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 期望版本號。
    /// </summary>
    public long Revision { get; init; }

    /// <summary>
    /// 取得或設定 穩定分桶代碼。
    /// </summary>
    public int StableBucket { get; init; }

    /// <summary>
    /// 取得或設定 指派的分片識別碼。
    /// </summary>
    public string? AssignedShard { get; init; }

    /// <summary>
    /// 取得或設定 最後變更之 UTC 時間字串。
    /// </summary>
    public string LastChangedUtc { get; init; } = string.Empty;

    /// <summary>
    /// 初始化 <see cref="FirewallDesiredAddress"/> 類別的新執行個體。
    /// </summary>
    public FirewallDesiredAddress() { }

    /// <summary>
    /// 初始化具備完整屬性之 <see cref="FirewallDesiredAddress"/> 類別的新執行個體。
    /// </summary>
    /// <param name="addressKey">位址鍵值。</param>
    /// <param name="addressFamily">位址家族代碼。</param>
    /// <param name="networkBytes">網路位元組陣列。</param>
    /// <param name="prefixLength">前綴遮罩長度。</param>
    /// <param name="desiredState">期望狀態代碼。</param>
    /// <param name="source">來源識別碼。</param>
    /// <param name="revision">版本序號。</param>
    /// <param name="stableBucket">分桶代碼。</param>
    /// <param name="assignedShard">所指派分片識別碼。</param>
    /// <param name="lastChangedUtc">最後異動時間戳記字串。</param>
    public FirewallDesiredAddress(
        string addressKey,
        int addressFamily,
        byte[] networkBytes,
        int prefixLength,
        int desiredState,
        string source,
        long revision,
        int stableBucket,
        string? assignedShard,
        string lastChangedUtc)
    {
        AddressKey = addressKey;
        AddressFamily = addressFamily;
        NetworkBytes = networkBytes;
        PrefixLength = prefixLength;
        DesiredState = desiredState;
        Source = source;
        Revision = revision;
        StableBucket = stableBucket;
        AssignedShard = assignedShard;
        LastChangedUtc = lastChangedUtc;
    }
}

/// <summary>
/// 表示防火牆期望位址的複合識別鍵。
/// </summary>
public sealed record FirewallDesiredAddressKey(string AddressKey, string Source);

/// <summary>
/// 表示一筆待寫入的防火牆異動。
/// </summary>
public sealed record FirewallChangeRequest(
    long? LockId,
    string AddressKey,
    FirewallChangeOperation Operation,
    long DesiredRevision,
    DateTimeOffset ChangedUtc);

/// <summary>
/// 表示一筆已持久化的防火牆異動。
/// </summary>
public sealed record FirewallChangeJournalEntry
{
    /// <summary>
    /// 取得或設定 異動序號。
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// 取得或設定 關聯的鎖定記錄識別碼。
    /// </summary>
    public long? LockId { get; init; }

    /// <summary>
    /// 取得或設定 位址鍵值。
    /// </summary>
    public string AddressKey { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 異動操作種類。
    /// </summary>
    public FirewallChangeOperation Operation { get; init; }

    /// <summary>
    /// 取得或設定 期望版本號。
    /// </summary>
    public long DesiredRevision { get; init; }

    /// <summary>
    /// 取得或設定 處理狀態。
    /// </summary>
    public FirewallChangeProcessingState ProcessingState { get; init; }

    /// <summary>
    /// 取得或設定 建立之 UTC 時間字串。
    /// </summary>
    public string CreatedUtc { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 更新之 UTC 時間字串。
    /// </summary>
    public string UpdatedUtc { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 失敗詳細資訊。
    /// </summary>
    public string? FailureDetails { get; init; }

    /// <summary>
    /// 取得或設定 租約擁有者。
    /// </summary>
    public string? ClaimOwner { get; init; }

    /// <summary>
    /// 取得或設定 租約有效期限字串。
    /// </summary>
    public string? LeaseUntilUtc { get; init; }

    /// <summary>
    /// 取得或設定 嘗試次數。
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// 取得或設定 下次可重試之 UTC 時間字串。
    /// </summary>
    public string? NextAttemptUtc { get; init; }

    /// <summary>
    /// 初始化 <see cref="FirewallChangeJournalEntry"/> 類別的新執行個體。
    /// </summary>
    public FirewallChangeJournalEntry() { }

    /// <summary>
    /// 初始化具備完整屬性之 <see cref="FirewallChangeJournalEntry"/> 類別的新執行個體。
    /// </summary>
    /// <param name="sequence">序號。</param>
    /// <param name="lockId">鎖定識別碼。</param>
    /// <param name="addressKey">位址鍵值。</param>
    /// <param name="operation">操作種類。</param>
    /// <param name="desiredRevision">期望版本號。</param>
    /// <param name="processingState">處理狀態。</param>
    /// <param name="createdUtc">建立時間。</param>
    /// <param name="updatedUtc">更新時間。</param>
    /// <param name="failureDetails">失敗詳情。</param>
    /// <param name="claimOwner">領取者名稱。</param>
    /// <param name="leaseUntilUtc">租約到期時間。</param>
    /// <param name="attemptCount">重試嘗試次數。</param>
    /// <param name="nextAttemptUtc">下次嘗試時間。</param>
    public FirewallChangeJournalEntry(
        long sequence,
        long? lockId,
        string addressKey,
        FirewallChangeOperation operation,
        long desiredRevision,
        FirewallChangeProcessingState processingState,
        string createdUtc,
        string updatedUtc,
        string? failureDetails,
        string? claimOwner,
        string? leaseUntilUtc,
        int attemptCount,
        string? nextAttemptUtc)
    {
        Sequence = sequence;
        LockId = lockId;
        AddressKey = addressKey;
        Operation = operation;
        DesiredRevision = desiredRevision;
        ProcessingState = processingState;
        CreatedUtc = createdUtc;
        UpdatedUtc = updatedUtc;
        FailureDetails = failureDetails;
        ClaimOwner = claimOwner;
        LeaseUntilUtc = leaseUntilUtc;
        AttemptCount = attemptCount;
        NextAttemptUtc = nextAttemptUtc;
    }
}

/// <summary>
/// 表示合併所有來源後仍要求封鎖的有效期望位址。
/// </summary>
public sealed record EffectiveFirewallDesiredAddress
{
    /// <summary>
    /// 取得或設定 位址鍵值。
    /// </summary>
    public string AddressKey { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 位址家族代碼。
    /// </summary>
    public int AddressFamily { get; init; }

    /// <summary>
    /// 取得或設定 網路位元組陣列。
    /// </summary>
    public byte[] NetworkBytes { get; init; } = [];

    /// <summary>
    /// 取得或設定 前綴遮罩長度。
    /// </summary>
    public int PrefixLength { get; init; }

    /// <summary>
    /// 取得或設定 期望版本號。
    /// </summary>
    public long Revision { get; init; }

    /// <summary>
    /// 取得或設定 穩定分桶代碼。
    /// </summary>
    public int StableBucket { get; init; }

    /// <summary>
    /// 取得或設定 指派的分片識別碼。
    /// </summary>
    public string? AssignedShard { get; init; }

    /// <summary>
    /// 取得或設定 參照之來源數量。
    /// </summary>
    public int SourceCount { get; init; }

    /// <summary>
    /// 初始化 <see cref="EffectiveFirewallDesiredAddress"/> 類別的新執行個體。
    /// </summary>
    public EffectiveFirewallDesiredAddress() { }

    /// <summary>
    /// 初始化具備完整屬性之 <see cref="EffectiveFirewallDesiredAddress"/> 類別的新執行個體。
    /// </summary>
    /// <param name="addressKey">位址鍵值。</param>
    /// <param name="addressFamily">位址家族代碼。</param>
    /// <param name="networkBytes">網路位元組陣列。</param>
    /// <param name="prefixLength">前綴遮罩長度。</param>
    /// <param name="revision">版本序號。</param>
    /// <param name="stableBucket">分桶代碼。</param>
    /// <param name="assignedShard">指派分片識別碼。</param>
    /// <param name="sourceCount">來源數。</param>
    public EffectiveFirewallDesiredAddress(
        string addressKey,
        int addressFamily,
        byte[] networkBytes,
        int prefixLength,
        long revision,
        int stableBucket,
        string? assignedShard,
        int sourceCount)
    {
        AddressKey = addressKey;
        AddressFamily = addressFamily;
        NetworkBytes = networkBytes;
        PrefixLength = prefixLength;
        Revision = revision;
        StableBucket = stableBucket;
        AssignedShard = assignedShard;
        SourceCount = sourceCount;
    }
}

/// <summary>
/// 表示一筆已套用的防火牆分片資訊。
/// </summary>
public sealed record FirewallAppliedShard
{
    /// <summary>
    /// 取得或設定 規則方向。
    /// </summary>
    public int Direction { get; init; }

    /// <summary>
    /// 取得或設定 分片識別碼。
    /// </summary>
    public string ShardId { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 內容雜湊值。
    /// </summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>
    /// 取得或設定 包含的位址數量。
    /// </summary>
    public int AddressCount { get; init; }

    /// <summary>
    /// 取得或設定 序列化字元數。
    /// </summary>
    public long SerializedBytes { get; init; }

    /// <summary>
    /// 取得或設定 套用版本號。
    /// </summary>
    public long AppliedRevision { get; init; }

    /// <summary>
    /// 取得或設定 底層防火牆規則名稱。
    /// </summary>
    public string? BackendRuleId { get; init; }

    /// <summary>
    /// 取得或設定 最後驗證之 UTC 時間戳記字串。
    /// </summary>
    public string? LastVerifiedUtc { get; init; }

    /// <summary>
    /// 初始化 <see cref="FirewallAppliedShard"/> 類別的新執行個體。
    /// </summary>
    public FirewallAppliedShard() { }

    /// <summary>
    /// 初始化具備完整屬性之 <see cref="FirewallAppliedShard"/> 類別的新執行個體。
    /// </summary>
    /// <param name="direction">規則方向。</param>
    /// <param name="shardId">分片識別碼。</param>
    /// <param name="contentHash">內容雜湊值。</param>
    /// <param name="addressCount">位址數量。</param>
    /// <param name="serializedBytes">序列化大小。</param>
    /// <param name="appliedRevision">套用版本號。</param>
    /// <param name="backendRuleId">後端規則識別名稱。</param>
    /// <param name="lastVerifiedUtc">最後驗證時間字串。</param>
    public FirewallAppliedShard(
        int direction,
        string shardId,
        string contentHash,
        int addressCount,
        long serializedBytes,
        long appliedRevision,
        string? backendRuleId,
        string? lastVerifiedUtc)
    {
        Direction = direction;
        ShardId = shardId;
        ContentHash = contentHash;
        AddressCount = addressCount;
        SerializedBytes = serializedBytes;
        AppliedRevision = appliedRevision;
        BackendRuleId = backendRuleId;
        LastVerifiedUtc = lastVerifiedUtc;
    }
}

/// <summary>
/// 提供可分頁、可續跑的防火牆期望狀態、異動日誌及已套用分片持久層。
/// </summary>
public sealed class FirewallStateStore
{
    /// <summary>
    /// 單次寫入或領取所允許的最大項目數。
    /// </summary>
    public const int MaximumBatchSize = 10000;

    private readonly Database database;

    /// <summary>
    /// 初始化防火牆狀態持久層。
    /// </summary>
    /// <param name="database">已設定完成的資料庫執行個體。</param>
    public FirewallStateStore(Database database) => this.database = database ?? throw new ArgumentNullException(nameof(database));

    /// <summary>
    /// 在單一交易內新增或更新一批來源擁有的期望位址。
    /// </summary>
    /// <param name="addresses">期望位址串流。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>實際寫入的輸入項目數。</returns>
    public int UpsertDesiredAddresses(IEnumerable<FirewallDesiredAddress> addresses, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        int count = 0;
        database.ExecuteInTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO FirewallDesiredAddress(AddressKey,AddressFamily,NetworkBytes,PrefixLength,DesiredState,Source,Revision,StableBucket,AssignedShard,LastChangedUtc) VALUES($key,$family,$bytes,$prefix,$state,$source,$revision,$bucket,$shard,$changed) ON CONFLICT(AddressKey,Source) DO UPDATE SET AddressFamily=excluded.AddressFamily,NetworkBytes=excluded.NetworkBytes,PrefixLength=excluded.PrefixLength,DesiredState=excluded.DesiredState,Revision=excluded.Revision,StableBucket=excluded.StableBucket,AssignedShard=excluded.AssignedShard,LastChangedUtc=excluded.LastChangedUtc WHERE excluded.Revision>=FirewallDesiredAddress.Revision";
            SqliteParameter key = command.Parameters.Add("$key", SqliteType.Text);
            SqliteParameter family = command.Parameters.Add("$family", SqliteType.Integer);
            SqliteParameter bytes = command.Parameters.Add("$bytes", SqliteType.Blob);
            SqliteParameter prefix = command.Parameters.Add("$prefix", SqliteType.Integer);
            SqliteParameter state = command.Parameters.Add("$state", SqliteType.Integer);
            SqliteParameter source = command.Parameters.Add("$source", SqliteType.Text);
            SqliteParameter revision = command.Parameters.Add("$revision", SqliteType.Integer);
            SqliteParameter bucket = command.Parameters.Add("$bucket", SqliteType.Integer);
            SqliteParameter shard = command.Parameters.Add("$shard", SqliteType.Text);
            SqliteParameter changed = command.Parameters.Add("$changed", SqliteType.Text);
            command.Prepare();
            foreach (FirewallDesiredAddress address in addresses)
            {
                EnsureBatchLimit(count);
                cancellationToken.ThrowIfCancellationRequested();
                ValidateDesiredAddress(address);
                key.Value = address.AddressKey;
                family.Value = address.AddressFamily;
                bytes.Value = address.NetworkBytes;
                prefix.Value = address.PrefixLength;
                state.Value = address.DesiredState;
                source.Value = address.Source;
                revision.Value = address.Revision;
                bucket.Value = address.StableBucket;
                shard.Value = (object?)address.AssignedShard ?? DBNull.Value;
                changed.Value = address.LastChangedUtc;
                command.ExecuteNonQuery();
                count++;
            }
        });
        return count;
    }

    /// <summary>
    /// 以複合游標讀取固定大小的期望位址頁面。
    /// </summary>
    /// <param name="afterAddressKey">上一頁最後一筆的位址鍵；第一頁使用空字串。</param>
    /// <param name="afterSource">上一頁最後一筆的來源；第一頁使用空字串。</param>
    /// <param name="maximumRows">頁面上限。</param>
    /// <returns>依位址鍵及來源排序的頁面。</returns>
    public IReadOnlyList<FirewallDesiredAddress> ReadDesiredAddressPage(string afterAddressKey, string afterSource, int maximumRows)
    {
        ValidatePageSize(maximumRows);
        return database.Query<FirewallDesiredAddress>(
            "SELECT AddressKey,AddressFamily,NetworkBytes,PrefixLength,DesiredState,Source,Revision,StableBucket,AssignedShard,LastChangedUtc FROM FirewallDesiredAddress WHERE AddressKey>@AfterKey OR (AddressKey=@AfterKey AND Source>@AfterSource) ORDER BY AddressKey,Source LIMIT @MaximumRows",
            new { AfterKey = afterAddressKey ?? string.Empty, AfterSource = afterSource ?? string.Empty, MaximumRows = maximumRows }).AsList();
    }

    /// <summary>
    /// 讀取跨來源合併後仍為有效狀態的期望位址頁面。
    /// </summary>
    /// <param name="afterAddressKey">上一頁最後一筆位址鍵；第一頁使用空字串。</param>
    /// <param name="maximumRows">頁面上限。</param>
    /// <returns>每個位址鍵最多一筆的有效期望狀態。</returns>
    public IReadOnlyList<EffectiveFirewallDesiredAddress> ReadEffectiveDesiredAddressPage(string afterAddressKey, int maximumRows)
    {
        ValidatePageSize(maximumRows);
        return database.Query<EffectiveFirewallDesiredAddress>(
            "SELECT AddressKey,MAX(AddressFamily) AddressFamily,MAX(NetworkBytes) NetworkBytes,MAX(PrefixLength) PrefixLength,MAX(Revision) Revision,MAX(StableBucket) StableBucket,MAX(AssignedShard) AssignedShard,COUNT(*) SourceCount FROM FirewallDesiredAddress WHERE DesiredState<>0 AND AddressKey>@AfterKey GROUP BY AddressKey ORDER BY AddressKey LIMIT @MaximumRows",
            new { AfterKey = afterAddressKey ?? string.Empty, MaximumRows = maximumRows }).AsList();
    }

    /// <summary>
    /// 在單一交易內移除指定來源所擁有的期望位址。
    /// </summary>
    /// <param name="keys">要移除的複合識別鍵串流。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>實際移除的資料列數。</returns>
    public int DeleteDesiredAddresses(IEnumerable<FirewallDesiredAddressKey> keys, CancellationToken cancellationToken = default)
        => ExecutePreparedKeys(keys, "DELETE FROM FirewallDesiredAddress WHERE AddressKey=$key AND Source=$source", cancellationToken);

    /// <summary>
    /// 在單一交易內附加一批防火牆異動日誌。
    /// </summary>
    /// <param name="changes">異動要求串流。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>依輸入順序排列的新日誌序號。</returns>
    public IReadOnlyList<long> AppendChanges(IEnumerable<FirewallChangeRequest> changes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);
        List<long> sequences = [];
        database.ExecuteInTransaction((connection, transaction) =>
        {
            int count = 0;
            using SqliteCommand supersede = connection.CreateCommand();
            supersede.Transaction = transaction;
            supersede.CommandText = "UPDATE FirewallChangeJournal SET ProcessingState=$completed,UpdatedUtc=$updated,FailureDetails='Superseded',ClaimOwner=NULL,LeaseUntilUtc=NULL,NextAttemptUtc=NULL WHERE AddressKey=$key AND ProcessingState IN (0,3) AND DesiredRevision<=$revision";
            supersede.Parameters.AddWithValue("$completed", (int)FirewallChangeProcessingState.Completed);
            SqliteParameter supersededUpdated = supersede.Parameters.Add("$updated", SqliteType.Text);
            SqliteParameter supersededKey = supersede.Parameters.Add("$key", SqliteType.Text);
            SqliteParameter supersededRevision = supersede.Parameters.Add("$revision", SqliteType.Integer);
            supersede.Prepare();
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO FirewallChangeJournal(LockId,AddressKey,Operation,DesiredRevision,ProcessingState,CreatedUtc,UpdatedUtc) SELECT $lockId,$key,$operation,$revision,0,$created,$created WHERE NOT EXISTS (SELECT 1 FROM FirewallChangeJournal WHERE AddressKey=$key AND ProcessingState IN (0,3) AND DesiredRevision>$revision) RETURNING Sequence";
            SqliteParameter lockId = command.Parameters.Add("$lockId", SqliteType.Integer);
            SqliteParameter key = command.Parameters.Add("$key", SqliteType.Text);
            SqliteParameter operation = command.Parameters.Add("$operation", SqliteType.Integer);
            SqliteParameter revision = command.Parameters.Add("$revision", SqliteType.Integer);
            SqliteParameter created = command.Parameters.Add("$created", SqliteType.Text);
            command.Prepare();
            foreach (FirewallChangeRequest change in changes)
            {
                EnsureBatchLimit(count);
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(change.AddressKey) || change.DesiredRevision < 0)
                    throw new ArgumentException(null, nameof(changes));
                lockId.Value = (object?)change.LockId ?? DBNull.Value;
                key.Value = change.AddressKey;
                operation.Value = (int)change.Operation;
                revision.Value = change.DesiredRevision;
                created.Value = change.ChangedUtc.ToUniversalTime().ToString("O");
                supersededUpdated.Value = created.Value;
                supersededKey.Value = change.AddressKey;
                supersededRevision.Value = change.DesiredRevision;
                supersede.ExecuteNonQuery();
                object? insertedSequence = command.ExecuteScalar();
                if (insertedSequence is not null && insertedSequence is not DBNull)
                    sequences.Add(Convert.ToInt64(insertedSequence));
                count++;
            }
        });
        return sequences;
    }

    /// <summary>
    /// 使用序號游標讀取待處理的異動日誌頁面。
    /// </summary>
    /// <param name="afterSequence">上一頁最後序號；第一頁使用零。</param>
    /// <param name="maximumRows">頁面上限。</param>
    /// <returns>依序號遞增的待處理日誌頁面。</returns>
    public IReadOnlyList<FirewallChangeJournalEntry> ReadPendingChangesPage(long afterSequence, int maximumRows)
    {
        if (afterSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(afterSequence));
        ValidatePageSize(maximumRows);
        return database.Query<FirewallChangeJournalEntry>(
            "SELECT Sequence,LockId,AddressKey,Operation,DesiredRevision,ProcessingState,CreatedUtc,UpdatedUtc,FailureDetails,ClaimOwner,LeaseUntilUtc,AttemptCount,NextAttemptUtc FROM FirewallChangeJournal WHERE ProcessingState=0 AND Sequence>@AfterSequence ORDER BY Sequence LIMIT @MaximumRows",
            new { AfterSequence = afterSequence, MaximumRows = maximumRows }).AsList();
    }

    /// <summary>
    /// 原子領取一批可處理、可重試或租約已過期的異動。
    /// </summary>
    /// <param name="claimOwner">本次 worker 的穩定識別碼。</param>
    /// <param name="maximumRows">領取上限。</param>
    /// <param name="nowUtc">判斷重試與租約到期的目前時間。</param>
    /// <param name="leaseDuration">避免其他 worker 重複處理的租約期限。</param>
    /// <param name="cancellationToken">取消領取交易的權杖。</param>
    /// <returns>已原子轉為處理中的異動頁面。</returns>
    public IReadOnlyList<FirewallChangeJournalEntry> ClaimChanges(string claimOwner, int maximumRows, DateTimeOffset nowUtc, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(claimOwner))
            throw new ArgumentException(null, nameof(claimOwner));
        ValidatePageSize(maximumRows);
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        List<FirewallChangeJournalEntry> claimed = [];
        database.ExecuteInTransaction((connection, transaction) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            claimed.AddRange(connection.Query<FirewallChangeJournalEntry>(
                "WITH candidates AS (SELECT Sequence FROM FirewallChangeJournal WHERE ProcessingState=0 OR (ProcessingState=$failed AND (NextAttemptUtc IS NULL OR NextAttemptUtc<=$now)) OR (ProcessingState=$processing AND LeaseUntilUtc<=$now) ORDER BY Sequence LIMIT $limit) UPDATE FirewallChangeJournal SET ProcessingState=$processing,ClaimOwner=$owner,LeaseUntilUtc=$lease,AttemptCount=AttemptCount+1,UpdatedUtc=$now,FailureDetails=NULL WHERE Sequence IN (SELECT Sequence FROM candidates) RETURNING Sequence,LockId,AddressKey,Operation,DesiredRevision,ProcessingState,CreatedUtc,UpdatedUtc,FailureDetails,ClaimOwner,LeaseUntilUtc,AttemptCount,NextAttemptUtc",
                new
                {
                    failed = (int)FirewallChangeProcessingState.Failed,
                    processing = (int)FirewallChangeProcessingState.Processing,
                    now = nowUtc.ToUniversalTime().ToString("O"),
                    lease = nowUtc.Add(leaseDuration).ToUniversalTime().ToString("O"),
                    owner = claimOwner,
                    limit = maximumRows
                }, transaction));
        });
        claimed.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        return claimed;
    }

    /// <summary>
    /// 在單一交易內將指定日誌標記為完成或失敗。
    /// </summary>
    /// <param name="sequences">日誌序號串流。</param>
    /// <param name="state">完成或失敗狀態。</param>
    /// <param name="failureDetails">失敗摘要；成功時應省略。</param>
    /// <param name="updatedUtc">狀態更新時間。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>實際變更的待處理日誌數。</returns>
    public int MarkChanges(IEnumerable<long> sequences, FirewallChangeProcessingState state, string? failureDetails, DateTimeOffset updatedUtc, CancellationToken cancellationToken = default)
    {
        if (state is FirewallChangeProcessingState.Pending or FirewallChangeProcessingState.Processing)
            throw new ArgumentOutOfRangeException(nameof(state));
        return ExecuteSequenceSet(sequences,
            "UPDATE FirewallChangeJournal SET ProcessingState=$state,UpdatedUtc=$updated,FailureDetails=$failure,ClaimOwner=NULL,LeaseUntilUtc=NULL,NextAttemptUtc=NULL WHERE ProcessingState IN (0,1) AND Sequence IN (SELECT Sequence FROM FirewallStateSequenceIds)",
            state, failureDetails, updatedUtc, cancellationToken);
    }

    /// <summary>
    /// 將已領取的異動排入有期限的重試佇列。
    /// </summary>
    /// <param name="sequences">要重試的日誌序號。</param>
    /// <param name="failureDetails">失敗摘要。</param>
    /// <param name="nextAttemptUtc">最早可再次領取的時間。</param>
    /// <param name="updatedUtc">狀態更新時間。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>實際排入重試的項目數。</returns>
    public int ScheduleRetry(IEnumerable<long> sequences, string failureDetails, DateTimeOffset nextAttemptUtc, DateTimeOffset updatedUtc, CancellationToken cancellationToken = default)
        => ExecuteSequenceSet(sequences,
            "UPDATE FirewallChangeJournal SET ProcessingState=$state,UpdatedUtc=$updated,FailureDetails=$failure,ClaimOwner=NULL,LeaseUntilUtc=NULL,NextAttemptUtc=$nextAttempt WHERE ProcessingState=1 AND Sequence IN (SELECT Sequence FROM FirewallStateSequenceIds)",
            FirewallChangeProcessingState.Failed, failureDetails, updatedUtc, cancellationToken, nextAttemptUtc);

    /// <summary>
    /// 在單一交易內新增或更新已套用分片資訊。
    /// </summary>
    /// <param name="shards">分片資訊串流。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>處理的輸入項目數。</returns>
    public int UpsertAppliedShards(IEnumerable<FirewallAppliedShard> shards, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shards);
        int count = 0;
        database.ExecuteInTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO FirewallAppliedShard(Direction,ShardId,ContentHash,AddressCount,SerializedBytes,AppliedRevision,BackendRuleId,LastVerifiedUtc) VALUES($direction,$id,$hash,$count,$bytes,$revision,$rule,$verified) ON CONFLICT(Direction,ShardId) DO UPDATE SET ContentHash=excluded.ContentHash,AddressCount=excluded.AddressCount,SerializedBytes=excluded.SerializedBytes,AppliedRevision=excluded.AppliedRevision,BackendRuleId=excluded.BackendRuleId,LastVerifiedUtc=excluded.LastVerifiedUtc WHERE excluded.AppliedRevision>=FirewallAppliedShard.AppliedRevision";
            SqliteParameter direction = command.Parameters.Add("$direction", SqliteType.Integer);
            SqliteParameter id = command.Parameters.Add("$id", SqliteType.Text);
            SqliteParameter hash = command.Parameters.Add("$hash", SqliteType.Text);
            SqliteParameter addressCount = command.Parameters.Add("$count", SqliteType.Integer);
            SqliteParameter bytes = command.Parameters.Add("$bytes", SqliteType.Integer);
            SqliteParameter revision = command.Parameters.Add("$revision", SqliteType.Integer);
            SqliteParameter rule = command.Parameters.Add("$rule", SqliteType.Text);
            SqliteParameter verified = command.Parameters.Add("$verified", SqliteType.Text);
            command.Prepare();
            foreach (FirewallAppliedShard shard in shards)
            {
                EnsureBatchLimit(count);
                cancellationToken.ThrowIfCancellationRequested();
                direction.Value = shard.Direction;
                id.Value = shard.ShardId;
                hash.Value = shard.ContentHash;
                addressCount.Value = shard.AddressCount;
                bytes.Value = shard.SerializedBytes;
                revision.Value = shard.AppliedRevision;
                rule.Value = (object?)shard.BackendRuleId ?? DBNull.Value;
                verified.Value = (object?)shard.LastVerifiedUtc ?? DBNull.Value;
                command.ExecuteNonQuery();
                count++;
            }
        });
        return count;
    }

    /// <summary>
    /// 以方向及分片識別碼複合游標讀取已套用分片頁面。
    /// </summary>
    /// <param name="afterDirection">上一頁最後一筆方向；第一頁使用最小整數。</param>
    /// <param name="afterShardId">上一頁最後一筆分片識別碼。</param>
    /// <param name="maximumRows">頁面上限。</param>
    /// <returns>依方向及分片識別碼排序的頁面。</returns>
    public IReadOnlyList<FirewallAppliedShard> ReadAppliedShardPage(int afterDirection, string afterShardId, int maximumRows)
    {
        ValidatePageSize(maximumRows);
        return database.Query<FirewallAppliedShard>(
            "SELECT Direction,ShardId,ContentHash,AddressCount,SerializedBytes,AppliedRevision,BackendRuleId,LastVerifiedUtc FROM FirewallAppliedShard WHERE Direction>@AfterDirection OR (Direction=@AfterDirection AND ShardId>@AfterShardId) ORDER BY Direction,ShardId LIMIT @MaximumRows",
            new { AfterDirection = afterDirection, AfterShardId = afterShardId ?? string.Empty, MaximumRows = maximumRows }).AsList();
    }

    /// <summary>
    /// 在單一交易內移除指定的已套用分片。
    /// </summary>
    /// <param name="shards">只使用方向及分片識別碼的分片串流。</param>
    /// <param name="cancellationToken">取消交易的權杖。</param>
    /// <returns>實際移除的分片數。</returns>
    public int DeleteAppliedShards(IEnumerable<FirewallAppliedShard> shards, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shards);
        int affected = 0;
        int count = 0;
        database.ExecuteInTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM FirewallAppliedShard WHERE Direction=$direction AND ShardId=$id";
            SqliteParameter direction = command.Parameters.Add("$direction", SqliteType.Integer);
            SqliteParameter id = command.Parameters.Add("$id", SqliteType.Text);
            command.Prepare();
            foreach (FirewallAppliedShard shard in shards)
            {
                EnsureBatchLimit(count);
                cancellationToken.ThrowIfCancellationRequested();
                direction.Value = shard.Direction;
                id.Value = shard.ShardId;
                affected += command.ExecuteNonQuery();
                count++;
            }
        });
        return affected;
    }

    private int ExecutePreparedKeys(IEnumerable<FirewallDesiredAddressKey> keys, string sql, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        int affected = 0;
        int count = 0;
        database.ExecuteInTransaction((connection, transaction) =>
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            SqliteParameter key = command.Parameters.Add("$key", SqliteType.Text);
            SqliteParameter source = command.Parameters.Add("$source", SqliteType.Text);
            command.Prepare();
            foreach (FirewallDesiredAddressKey item in keys)
            {
                EnsureBatchLimit(count);
                cancellationToken.ThrowIfCancellationRequested();
                key.Value = item.AddressKey;
                source.Value = item.Source;
                affected += command.ExecuteNonQuery();
                count++;
            }
        });
        return affected;
    }

    private int ExecuteSequenceSet(IEnumerable<long> sequences, string updateSql, FirewallChangeProcessingState state, string? failureDetails, DateTimeOffset updatedUtc, CancellationToken cancellationToken, DateTimeOffset? nextAttemptUtc = null)
    {
        ArgumentNullException.ThrowIfNull(sequences);
        int affected = 0;
        database.ExecuteInTransaction((connection, transaction) =>
        {
            using SqliteCommand create = connection.CreateCommand();
            create.Transaction = transaction;
            create.CommandText = "CREATE TEMP TABLE IF NOT EXISTS FirewallStateSequenceIds(Sequence INTEGER PRIMARY KEY) WITHOUT ROWID; DELETE FROM FirewallStateSequenceIds";
            create.ExecuteNonQuery();
            using SqliteCommand insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = "INSERT OR IGNORE INTO FirewallStateSequenceIds(Sequence) VALUES($sequence)";
            SqliteParameter sequence = insert.Parameters.Add("$sequence", SqliteType.Integer);
            insert.Prepare();
            int count = 0;
            foreach (long value in sequences)
            {
                EnsureBatchLimit(count);
                cancellationToken.ThrowIfCancellationRequested();
                if (value <= 0)
                    continue;
                sequence.Value = value;
                insert.ExecuteNonQuery();
                count++;
            }
            using SqliteCommand update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = updateSql;
            update.Parameters.AddWithValue("$state", (int)state);
            update.Parameters.AddWithValue("$updated", updatedUtc.ToUniversalTime().ToString("O"));
            update.Parameters.AddWithValue("$failure", (object?)failureDetails ?? DBNull.Value);
            update.Parameters.AddWithValue("$nextAttempt", nextAttemptUtc?.ToUniversalTime().ToString("O") ?? (object)DBNull.Value);
            update.Prepare();
            cancellationToken.ThrowIfCancellationRequested();
            affected = update.ExecuteNonQuery();
        });
        return affected;
    }

    private static void ValidateDesiredAddress(FirewallDesiredAddress address)
    {
        if (string.IsNullOrWhiteSpace(address.AddressKey) || string.IsNullOrWhiteSpace(address.Source) || address.NetworkBytes.Length == 0 || address.PrefixLength < 0 || address.Revision < 0 || address.StableBucket < 0)
            throw new ArgumentException(null, nameof(address));
    }

    private static void ValidatePageSize(int maximumRows)
    {
        if (maximumRows is < 1 or > MaximumBatchSize)
            throw new ArgumentOutOfRangeException(nameof(maximumRows));
    }

    private static void EnsureBatchLimit(int processedCount)
    {
        if (processedCount >= MaximumBatchSize)
            throw new ArgumentOutOfRangeException(nameof(processedCount), $"單次批次不得超過 {MaximumBatchSize} 筆。請使用 keyset 分批處理。");
    }
}
