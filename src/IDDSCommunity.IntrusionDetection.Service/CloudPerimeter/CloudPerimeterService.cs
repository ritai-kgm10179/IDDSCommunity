using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.CloudPerimeter;

namespace IDDSCommunity.IntrusionDetection.Service.CloudPerimeter;

/// <summary>
/// 以持久化待送匣、有序單一工作者及失敗退避管理雲端邊界處置。
/// </summary>
public sealed class CloudPerimeterService : IDisposable
{
    private readonly CloudPerimeterSettings settings;
    private readonly Database database;
    private readonly CancellationTokenSource stopping = new();
    private readonly SemaphoreSlim providerGate = new(1, 1);
    private readonly Task worker;
    private ICloudPerimeterProvider? activeProvider;
    private bool isDisposed;

    /// <summary>
    /// 取得目前是否已啟用並具有提供者。
    /// </summary>
    public bool IsEnabled => settings.EnableCloudPerimeter && activeProvider is not null && !isDisposed;
    /// <summary>
    /// 取得目前的提供者。
    /// </summary>
    public ICloudPerimeterProvider? ActiveProvider => activeProvider;

    /// <summary>
    /// 建立待送匣服務。
    /// </summary>
    /// <param name="settings">雲端設定。</param>
    /// <param name="database">已設定的加密資料庫；省略時使用共用執行個體。</param>
    public CloudPerimeterService(CloudPerimeterSettings settings, Database? database = null) : this(settings, database ?? Database.Instance, null) { }

    internal CloudPerimeterService(CloudPerimeterSettings settings, Database database, ICloudPerimeterProvider? provider)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.database = database ?? Database.Instance;
        activeProvider = provider ?? CloudPerimeterProviderFactory.Create(settings);
        worker = Task.Run(RunAsync);
    }

    /// <summary>
    /// 等候目前處置結束後重新建立提供者。
    /// </summary>
    public void RefreshProvider()
    {
        providerGate.Wait();
        try
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            (activeProvider as IDisposable)?.Dispose();
            activeProvider = CloudPerimeterProviderFactory.Create(settings);
        }
        finally { providerGate.Release(); }
    }

    /// <summary>
    /// 將封鎖要求持久化，同一 IP 的較新要求會取代待送要求。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="reason">封鎖原因。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>是否成功排入待送匣；不代表遠端已完成封鎖。</returns>
    public Task<bool> NotifyBlockAsync(string ipAddress, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(Enqueue(ipAddress, true, reason, cancellationToken));

    /// <summary>
    /// 將解鎖要求持久化，依序覆蓋較舊的封鎖要求。
    /// </summary>
    /// <param name="ipAddress">來源 IP。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>是否成功排入待送匣；不代表遠端已完成解鎖。</returns>
    public Task<bool> NotifyUnblockAsync(string ipAddress, CancellationToken cancellationToken = default) =>
        Task.FromResult(Enqueue(ipAddress, false, string.Empty, cancellationToken));

    private bool Enqueue(string ipAddress, bool block, string reason, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!IsEnabled) return false;
        if (!database.IsConfigured) throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cloud perimeter outbox requires a configured database."));
        string ip = IpAddressCanonicalizer.Canonicalize(ipAddress);
        if (!System.Net.IPAddress.TryParse(ip, out _)) throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Invalid IP address."), nameof(ipAddress));
        database.ExecuteInTransaction((connection, transaction) =>
        {
            if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM CloudPerimeterOutbox", transaction: transaction) >= 100000
                && connection.ExecuteScalar<int>("SELECT COUNT(*) FROM CloudPerimeterOutbox WHERE IpAddress=@ip", new { ip }, transaction) == 0)
                throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cloud perimeter outbox capacity has been reached."));
            connection.Execute("INSERT INTO CloudPerimeterOutbox(IpAddress,Version,ShouldBlock,Reason) VALUES(@ip,@version,@block,@reason) ON CONFLICT(IpAddress) DO UPDATE SET Version=excluded.Version,ShouldBlock=excluded.ShouldBlock,Reason=excluded.Reason,Attempts=0,DueTicks=0",
                new { ip, version = Guid.NewGuid().ToString("N"), block, reason = reason.Length > 512 ? reason[..512] : reason }, transaction);
        });
        return true;
    }

    private async Task RunAsync()
    {
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                bool processedAny = false;
                try
                {
                    if (IsEnabled && database.IsConfigured)
                    {
                        var rows = database.Query<Pending>(
                            "SELECT * FROM CloudPerimeterOutbox WHERE DueTicks<=@now ORDER BY DueTicks,IpAddress LIMIT 20",
                            new { now = DateTime.UtcNow.Ticks }).ToList();

                        if (rows.Count > 0)
                        {
                            processedAny = true;
                            foreach (var row in rows)
                            {
                                if (stopping.IsCancellationRequested) break;
                                await providerGate.WaitAsync(stopping.Token).ConfigureAwait(false);
                                bool success;
                                try
                                {
                                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                                    deadline.CancelAfter(TimeSpan.FromSeconds(30));
                                    success = activeProvider is not null && (row.ShouldBlock
                                        ? await activeProvider.BlockIpAsync(row.IpAddress, row.Reason, deadline.Token).ConfigureAwait(false)
                                        : await activeProvider.UnblockIpAsync(row.IpAddress, deadline.Token).ConfigureAwait(false));
                                }
                                catch (Exception) when (!stopping.IsCancellationRequested) { success = false; }
                                finally { providerGate.Release(); }

                                if (success)
                                {
                                    database.ExecuteNonQuery("DELETE FROM CloudPerimeterOutbox WHERE IpAddress=@p0 AND Version=@p1", row.IpAddress, row.Version);
                                }
                                else
                                {
                                    int nextAttempts = row.Attempts + 1;
                                    if (nextAttempts >= 30)
                                    {
                                        database.ExecuteNonQuery("DELETE FROM CloudPerimeterOutbox WHERE IpAddress=@p0 AND Version=@p1", row.IpAddress, row.Version);
                                        System.Diagnostics.Trace.TraceError("Cloud perimeter delivery failed permanently for {0} after 30 attempts. Discarded.", row.IpAddress);
                                    }
                                    else
                                    {
                                        long due = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(nextAttempts, 8)))).Ticks;
                                        database.ExecuteNonQuery("UPDATE CloudPerimeterOutbox SET Attempts=@p0,DueTicks=@p1 WHERE IpAddress=@p2 AND Version=@p3", nextAttempts, due, row.IpAddress, row.Version);
                                        System.Diagnostics.Trace.TraceWarning("Cloud perimeter delivery pending for {0} (attempt {1}/30)", row.IpAddress, nextAttempts);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (stopping.IsCancellationRequested) { break; }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError("Cloud perimeter outbox: {0}", ex.GetType().Name); }

                if (!processedAny)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stopping.Token).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50), stopping.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    /// <summary>
    /// 取消並等待工作者結束；未完成要求保留於資料庫供下次啟動重試。
    /// </summary>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        stopping.Cancel();
        worker.GetAwaiter().GetResult();
        (activeProvider as IDisposable)?.Dispose();
        activeProvider = null;
        stopping.Dispose();
        providerGate.Dispose();
    }

    private sealed class Pending
    {
        public string IpAddress { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool ShouldBlock { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Attempts { get; set; }
    }
}