using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IDDSCommunity.IntrusionDetection.Shared;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供 Windows 防火牆 COM 介面之非同步批次聚合與調步佇列調度器（Paced Queue Dispatcher）。
/// 藉由非同步通道、批次合併與速率調步機制，徹底消除多安全代理程式併發請求時底層 COM 全域鎖（_firewallLock）之爭奪瓶頸。
/// </summary>
internal sealed class FirewallPacedQueueDispatcher : IFirewallPolicy, IDisposable, IAsyncDisposable
{
    private readonly IFirewallPolicy innerPolicy;
    private readonly IRuntimeLog logManager;
    private readonly FirewallPacingOptions options;
    private readonly Channel<FirewallOperationItem> channel;
    private readonly Task workerTask;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly FirewallLockedAddressTracker tracker;
    private DateTime lastCommitTimeUtc = DateTime.MinValue;
    private bool disposed;

    private long totalBlocksQueued;
    private long totalBlocksCommitted;
    private long totalRemovesQueued;
    private long totalRemovesCommitted;
    private long totalBatchesDispatched;
    private long coalescedDuplicatesCount;

    /// <summary>
    /// 取得底層實際封裝之 Windows 防火牆策略執行個體。
    /// </summary>
    public IFirewallPolicy InnerPolicy => innerPolicy;

    /// <summary>
    /// 取得目前套用之調步佇列組態設定。
    /// </summary>
    public FirewallPacingOptions Options => options;

    /// <summary>
    /// 取得已累計入列之封鎖 IP 請求總數。
    /// </summary>
    public long TotalBlocksQueued => Interlocked.Read(ref totalBlocksQueued);

    /// <summary>
    /// 取得已成功提交至底層 COM 防火牆之封鎖 IP 總數。
    /// </summary>
    public long TotalBlocksCommitted => Interlocked.Read(ref totalBlocksCommitted);

    /// <summary>
    /// 取得已累計入列之解除封鎖 IP 請求總數。
    /// </summary>
    public long TotalRemovesQueued => Interlocked.Read(ref totalRemovesQueued);

    /// <summary>
    /// 取得已成功提交至底層 COM 防火牆之解除封鎖 IP 總數。
    /// </summary>
    public long TotalRemovesCommitted => Interlocked.Read(ref totalRemovesCommitted);

    /// <summary>
    /// 取得已派發至底層 COM 之批次執行次數。
    /// </summary>
    public long TotalBatchesDispatched => Interlocked.Read(ref totalBatchesDispatched);

    /// <summary>
    /// 取得因重複請求而在佇列內被合流去重消除之請求次數。
    /// </summary>
    public long CoalescedDuplicatesCount => Interlocked.Read(ref coalescedDuplicatesCount);

    /// <summary>
    /// 初始化 <see cref="FirewallPacedQueueDispatcher"/> 類別的新執行個體。
    /// </summary>
    /// <param name="innerPolicy">底層實際執行 COM 呼叫之防火牆策略執行個體。</param>
    /// <param name="logManager">執行階段日誌管理器。</param>
    /// <param name="options">選擇性的調步佇列組態設定。</param>
    public FirewallPacedQueueDispatcher(
        IFirewallPolicy innerPolicy,
        IRuntimeLog logManager,
        FirewallPacingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(innerPolicy);
        ArgumentNullException.ThrowIfNull(logManager);

        this.innerPolicy = innerPolicy;
        this.logManager = logManager;
        this.options = options ?? FirewallPacingOptions.Default;
        this.tracker = new FirewallLockedAddressTracker(innerPolicy);

        BoundedChannelOptions channelOptions = new(this.options.MaxQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };
        this.channel = Channel.CreateBounded<FirewallOperationItem>(channelOptions);
        this.workerTask = Task.Run(() => ProcessQueueAsync(cancellationTokenSource.Token));
    }

    /// <summary>
    /// 將指定之 IP 位址加入 Windows 防火牆阻擋規則（支援調步佇列與記憶體快取加速）。
    /// </summary>
    /// <param name="ipAddress">要阻擋之遠端 IP 位址。</param>
    public void Block(string ipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        string trimmed = ipAddress.Trim();
        ValidateIpAddress(trimmed);

        if (IsLocked(trimmed))
        {
            Interlocked.Increment(ref coalescedDuplicatesCount);
            return;
        }

        tracker.Add(trimmed);
        Interlocked.Increment(ref totalBlocksQueued);

        if (options.WaitForCommitOnSync)
        {
            TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Block, [trimmed], tcs));
            tcs.Task.GetAwaiter().GetResult();
        }
        else
        {
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Block, [trimmed], null));
        }
    }

    /// <summary>
    /// 非同步將指定之 IP 位址加入 Windows 防火牆阻擋規則。
    /// </summary>
    /// <param name="ipAddress">要阻擋之遠端 IP 位址。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    public async ValueTask BlockAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        string trimmed = ipAddress.Trim();
        ValidateIpAddress(trimmed);

        if (IsLocked(trimmed))
        {
            Interlocked.Increment(ref coalescedDuplicatesCount);
            return;
        }

        tracker.Add(trimmed);
        Interlocked.Increment(ref totalBlocksQueued);

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await WriteToChannelAsync(new FirewallOperationItem(FirewallOperationType.Block, [trimmed], tcs), cancellationToken).ConfigureAwait(false);
        await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批次將多個 IP 位址加入 Windows 防火牆阻擋規則（支援 CIDR 聚合與切片批次寫入）。
    /// </summary>
    /// <param name="ipAddresses">要批次阻擋之 IP 位址清單。</param>
    public void BatchBlock(IReadOnlyCollection<string> ipAddresses)
    {
        if (ipAddresses == null || ipAddresses.Count == 0) return;

        List<string> validIps = ExtractValidAddresses(ipAddresses);
        if (validIps.Count == 0) return;

        List<string> newBlocks = new(validIps.Count);
        HashSet<string> seenInBatch = new(StringComparer.OrdinalIgnoreCase);
        int duplicatesInBatch = 0;
        foreach (string ip in validIps)
        {
            if (seenInBatch.Add(ip))
            {
                if (tracker.IsLocked(ip))
                {
                    duplicatesInBatch++;
                }
                else
                {
                    newBlocks.Add(ip);
                }
            }
            else
            {
                duplicatesInBatch++;
            }
        }

        if (duplicatesInBatch > 0)
        {
            Interlocked.Add(ref coalescedDuplicatesCount, duplicatesInBatch);
        }

        if (newBlocks.Count == 0) return;

        tracker.AddRange(newBlocks);
        Interlocked.Add(ref totalBlocksQueued, newBlocks.Count);

        if (options.WaitForCommitOnSync)
        {
            TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Block, newBlocks, tcs));
            tcs.Task.GetAwaiter().GetResult();
        }
        else
        {
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Block, newBlocks, null));
        }
    }

    /// <summary>
    /// 非同步批次將多個 IP 位址加入 Windows 防火牆阻擋規則。
    /// </summary>
    /// <param name="ipAddresses">要批次阻擋之 IP 位址清單。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    public async ValueTask BatchBlockAsync(IReadOnlyCollection<string> ipAddresses, CancellationToken cancellationToken = default)
    {
        if (ipAddresses == null || ipAddresses.Count == 0) return;

        List<string> validIps = ExtractValidAddresses(ipAddresses);
        if (validIps.Count == 0) return;

        List<string> newBlocks = new(validIps.Count);
        HashSet<string> seenInBatch = new(StringComparer.OrdinalIgnoreCase);
        int duplicatesInBatch = 0;
        foreach (string ip in validIps)
        {
            if (seenInBatch.Add(ip))
            {
                if (tracker.IsLocked(ip))
                {
                    duplicatesInBatch++;
                }
                else
                {
                    newBlocks.Add(ip);
                }
            }
            else
            {
                duplicatesInBatch++;
            }
        }

        if (duplicatesInBatch > 0)
        {
            Interlocked.Add(ref coalescedDuplicatesCount, duplicatesInBatch);
        }

        if (newBlocks.Count == 0) return;

        tracker.AddRange(newBlocks);
        Interlocked.Add(ref totalBlocksQueued, newBlocks.Count);

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await WriteToChannelAsync(new FirewallOperationItem(FirewallOperationType.Block, newBlocks, tcs), cancellationToken).ConfigureAwait(false);
        await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 判斷指定之 IP 位址是否已被 Windows 防火牆規則阻擋（使用極速無鎖記憶體快取）。
    /// </summary>
    /// <param name="ipAddress">欲檢查之 IP 位址。</param>
    /// <returns>若已被阻擋則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsLocked(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return false;
        return tracker.IsLocked(ipAddress);
    }

    /// <summary>
    /// 批次篩選給定之 IP 位址清單中，哪些位址已處於 Windows 防火牆阻擋規則中（完全於記憶體中完成比對）。
    /// </summary>
    /// <param name="ipAddresses">欲檢驗之 IP 位址清單。</param>
    /// <returns>已被防火牆阻擋之 IP 位址集合。</returns>
    public HashSet<string> FilterLockedIps(IEnumerable<string> ipAddresses)
    {
        if (ipAddresses == null) return [];
        return tracker.FilterLockedIps(ipAddresses);
    }

    /// <summary>
    /// 取得目前受 Windows 防火牆規則阻擋之所有有效 IP 位址清單（排空待寫入佇列後自底層取得完整一致狀態）。
    /// </summary>
    /// <returns>已被防火牆阻擋之 IP 位址唯讀集合。</returns>
    public IReadOnlyCollection<string> GetBlockedAddresses()
    {
        Flush();
        return innerPolicy.GetBlockedAddresses();
    }

    /// <summary>
    /// 取得目前 Windows 防火牆阻擋規則之完整狀態快照（排空待寫入佇列後自底層取得完整一致狀態）。
    /// </summary>
    /// <returns>傳回包含有效位址與各方向位址之快照物件。</returns>
    public FirewallBlockState GetBlockState()
    {
        Flush();
        return innerPolicy.GetBlockState();
    }

    /// <summary>
    /// 自 Windows 防火牆阻擋規則中移除指定之 IP 位址。
    /// </summary>
    /// <param name="ipAddress">要移除之 IP 位址。</param>
    public void RemoveIpAddressFromBlockList(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return;
        string trimmed = ipAddress.Trim();
        tracker.Remove(trimmed);
        Interlocked.Increment(ref totalRemovesQueued);

        if (options.WaitForCommitOnSync)
        {
            TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Remove, [trimmed], tcs));
            tcs.Task.GetAwaiter().GetResult();
        }
        else
        {
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Remove, [trimmed], null));
        }
    }

    /// <summary>
    /// 非同步自 Windows 防火牆阻擋規則中移除指定之 IP 位址。
    /// </summary>
    /// <param name="ipAddress">要移除之 IP 位址。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    public async ValueTask RemoveIpAddressFromBlockListAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return;
        string trimmed = ipAddress.Trim();
        tracker.Remove(trimmed);
        Interlocked.Increment(ref totalRemovesQueued);

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await WriteToChannelAsync(new FirewallOperationItem(FirewallOperationType.Remove, [trimmed], tcs), cancellationToken).ConfigureAwait(false);
        await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 批次自 Windows 防火牆阻擋規則中移除多個 IP 位址，以單一 Pass 更新分片規則以杜絕 COM 昂貴耗時。
    /// </summary>
    /// <param name="ipAddresses">要批次移除之 IP 位址清單。</param>
    public void BatchRemove(IReadOnlyCollection<string> ipAddresses)
    {
        if (ipAddresses == null || ipAddresses.Count == 0) return;
        List<string> list = ipAddresses.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (list.Count == 0) return;

        tracker.RemoveRange(list);
        Interlocked.Add(ref totalRemovesQueued, list.Count);

        if (options.WaitForCommitOnSync)
        {
            TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Remove, list, tcs));
            tcs.Task.GetAwaiter().GetResult();
        }
        else
        {
            WriteToChannel(new FirewallOperationItem(FirewallOperationType.Remove, list, null));
        }
    }

    /// <summary>
    /// 非同步批次自 Windows 防火牆阻擋規則中移除多個 IP 位址。
    /// </summary>
    /// <param name="ipAddresses">要批次移除之 IP 位址清單。</param>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步操作的 ValueTask。</returns>
    public async ValueTask BatchRemoveAsync(IReadOnlyCollection<string> ipAddresses, CancellationToken cancellationToken = default)
    {
        if (ipAddresses == null || ipAddresses.Count == 0) return;
        List<string> list = ipAddresses.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (list.Count == 0) return;

        tracker.RemoveRange(list);
        Interlocked.Add(ref totalRemovesQueued, list.Count);

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await WriteToChannelAsync(new FirewallOperationItem(FirewallOperationType.Remove, list, tcs), cancellationToken).ConfigureAwait(false);
        await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 重整並壓縮既有 Windows 防火牆分片規則，重新進行 CIDR 網段聚合並清除破碎或空洞分片。
    /// </summary>
    /// <param name="safeNetworks">選擇性的安全網路全域白名單集合。</param>
    public void CompactBlockRules(IEnumerable<string>? safeNetworks = null)
    {
        Flush();
        innerPolicy.CompactBlockRules(safeNetworks);
        tracker.Reset(innerPolicy.GetBlockedAddresses());
    }

    /// <summary>
    /// 宣告式比對並對齊 Windows 防火牆傳入放行規則，自動新增缺漏項目並移除過期舊規則。
    /// </summary>
    /// <param name="targetRules">目標期望開放之通訊埠規則規格清單。</param>
    /// <param name="auditRecorder">選擇性的稽核日誌紀錄委派。</param>
    public void ReconcileInboundAllowRules(
        IReadOnlyCollection<FirewallInboundRuleDefinition> targetRules,
        Action<string, string, string, string?>? auditRecorder = null)
    {
        Flush();
        innerPolicy.ReconcileInboundAllowRules(targetRules, auditRecorder);
    }

    /// <summary>
    /// 於服務停止或解除安裝時，清除所有由 IDDS 社群版所建立之傳入放行規則。
    /// </summary>
    /// <param name="auditRecorder">選擇性的稽核日誌紀錄委派。</param>
    public void RemoveAllInboundAllowRules(Action<string, string, string, string?>? auditRecorder = null)
    {
        Flush();
        innerPolicy.RemoveAllInboundAllowRules(auditRecorder);
    }

    /// <summary>
    /// 強制將調步佇列中所有待處理的防火牆規則立即排空並寫入底層 Windows 防火牆 COM 介面。
    /// </summary>
    public void Flush()
    {
        if (disposed) return;
        FlushAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 非同步強制將調步佇列中所有待處理的防火牆規則立即排空並寫入底層 Windows 防火牆 COM 介面。
    /// </summary>
    /// <param name="cancellationToken">取消語彙基元。</param>
    /// <returns>表示非同步排空作業的任務。</returns>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (disposed) return;
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FirewallOperationItem flushItem = new(FirewallOperationType.Flush, Array.Empty<string>(), tcs);
        try
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
            await channel.Writer.WriteAsync(flushItem, linkedCts.Token).ConfigureAwait(false);
            await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            // 佇列已關閉，已完成排空
        }
    }

    /// <summary>
    /// 釋放由 <see cref="FirewallPacedQueueDispatcher"/> 所使用之所有資源並停止背景調步工作。
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        channel.Writer.TryComplete();
        try
        {
            workerTask.GetAwaiter().GetResult();
        }
        catch
        {
            // 忽略結束時的工作取消例外狀況
        }

        while (channel.Reader.TryRead(out FirewallOperationItem? item))
        {
            item.CompletionSource?.TrySetException(new ObjectDisposedException(nameof(FirewallPacedQueueDispatcher)));
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        tracker.Dispose();

        if (innerPolicy is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// 非同步釋放由 <see cref="FirewallPacedQueueDispatcher"/> 所使用之所有資源。
    /// </summary>
    /// <returns>表示非同步釋放作業的 ValueTask。</returns>
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;

        channel.Writer.TryComplete();
        try
        {
            await workerTask.ConfigureAwait(false);
        }
        catch
        {
            // 忽略結束時的工作取消例外狀況
        }

        while (channel.Reader.TryRead(out FirewallOperationItem? item))
        {
            item.CompletionSource?.TrySetException(new ObjectDisposedException(nameof(FirewallPacedQueueDispatcher)));
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        tracker.Dispose();

        if (innerPolicy is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else if (innerPolicy is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void WriteToChannel(FirewallOperationItem item)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!channel.Writer.TryWrite(item))
        {
            ValueTask vt = channel.Writer.WriteAsync(item, cancellationTokenSource.Token);
            if (!vt.IsCompletedSuccessfully)
            {
                vt.AsTask().GetAwaiter().GetResult();
            }
        }
    }

    private async ValueTask WriteToChannelAsync(FirewallOperationItem item, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!channel.Writer.TryWrite(item))
        {
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
            await channel.Writer.WriteAsync(item, linkedCts.Token).ConfigureAwait(false);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        ChannelReader<FirewallOperationItem> reader = channel.Reader;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    break;

                List<FirewallOperationItem> batchItems = [];
                DateTime windowStart = DateTime.UtcNow;

                while (batchItems.Count < options.MaxBatchSize)
                {
                    while (reader.TryRead(out FirewallOperationItem? item))
                    {
                        batchItems.Add(item);
                        if (item.Type == FirewallOperationType.Flush || batchItems.Count >= options.MaxBatchSize)
                            break;
                    }

                    if (batchItems.Count >= options.MaxBatchSize
                        || (batchItems.Count > 0 && batchItems[^1].Type == FirewallOperationType.Flush))
                    {
                        break;
                    }

                    if (options.CoalescingWindowMs <= 0)
                        break;

                    TimeSpan elapsed = DateTime.UtcNow - windowStart;
                    if (elapsed >= TimeSpan.FromMilliseconds(options.CoalescingWindowMs))
                        break;

                    TimeSpan remaining = TimeSpan.FromMilliseconds(options.CoalescingWindowMs) - elapsed;
                    using CancellationTokenSource windowCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    windowCts.CancelAfter(remaining);

                    try
                    {
                        if (!await reader.WaitToReadAsync(windowCts.Token).ConfigureAwait(false))
                            break;
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                if (batchItems.Count == 0)
                    continue;

                await DispatchBatchAsync(batchItems, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logManager.WriteEntry("FirewallPacedQueueDispatcher worker encountered an error: " + ex.Message,
                    System.Diagnostics.EventLogEntryType.Error,
                    Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR,
                    Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
            }
        }
    }

    private async Task DispatchBatchAsync(List<FirewallOperationItem> batchItems, CancellationToken cancellationToken)
    {
        HashSet<string> blocksToApply = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> removesToApply = new(StringComparer.OrdinalIgnoreCase);

        foreach (FirewallOperationItem item in batchItems)
        {
            if (item.Type == FirewallOperationType.Block)
            {
                foreach (string addr in item.Addresses)
                {
                    removesToApply.Remove(addr);
                    if (!blocksToApply.Add(addr))
                    {
                        Interlocked.Increment(ref coalescedDuplicatesCount);
                    }
                }
            }
            else if (item.Type == FirewallOperationType.Remove)
            {
                foreach (string addr in item.Addresses)
                {
                    blocksToApply.Remove(addr);
                    if (!removesToApply.Add(addr))
                    {
                        Interlocked.Increment(ref coalescedDuplicatesCount);
                    }
                }
            }
        }

        bool hasMutations = removesToApply.Count > 0 || blocksToApply.Count > 0;

        // 調步間隔控制（僅在實際呼叫 COM 寫入時套用 MinPacingIntervalMs）
        if (hasMutations && options.MinPacingIntervalMs > 0 && lastCommitTimeUtc != DateTime.MinValue)
        {
            TimeSpan elapsedSinceLastCommit = DateTime.UtcNow - lastCommitTimeUtc;
            if (elapsedSinceLastCommit < TimeSpan.FromMilliseconds(options.MinPacingIntervalMs))
            {
                TimeSpan delay = TimeSpan.FromMilliseconds(options.MinPacingIntervalMs) - elapsedSinceLastCommit;
                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                }
            }
        }

        Exception? dispatchException = null;
        try
        {
            if (removesToApply.Count > 0)
            {
                innerPolicy.BatchRemove(removesToApply);
                Interlocked.Add(ref totalRemovesCommitted, removesToApply.Count);
            }

            if (blocksToApply.Count > 0)
            {
                innerPolicy.BatchBlock(blocksToApply);
                Interlocked.Add(ref totalBlocksCommitted, blocksToApply.Count);
            }

            if (hasMutations)
            {
                lastCommitTimeUtc = DateTime.UtcNow;
                Interlocked.Increment(ref totalBatchesDispatched);
            }
        }
        catch (Exception ex)
        {
            dispatchException = ex;
            tracker.RemoveRange(blocksToApply);
            tracker.AddRange(removesToApply);
            logManager.WriteEntry("Firewall batch dispatch failed: " + ex.Message,
                System.Diagnostics.EventLogEntryType.Error,
                Globals.IDDSCOMMUNITY_EVENT_ID_CONFIGURATION_ERROR,
                Globals.IDDSCOMMUNITY_LOG_CATEGORY_RUNTIME);
        }

        foreach (FirewallOperationItem item in batchItems)
        {
            if (item.CompletionSource != null)
            {
                if (dispatchException != null)
                    item.CompletionSource.TrySetException(dispatchException);
                else
                    item.CompletionSource.TrySetResult();
            }
        }
    }

    private static void ValidateIpAddress(string candidate)
    {
        if (candidate == "*") return;
        if (!IddsConfig.IsValidIpAddress(candidate) && !IPNetwork.TryParse(candidate, out _))
        {
            throw new ArgumentOutOfRangeException(nameof(candidate),
                global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IP address must be given in IP version 4 or IP version 6 format!"));
        }
    }

    private static List<string> ExtractValidAddresses(IEnumerable<string> addresses)
    {
        List<string> valid = [];
        foreach (string raw in addresses)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string trimmed = raw.Trim();
            if (trimmed == "*" || IddsConfig.IsValidIpAddress(trimmed) || IPNetwork.TryParse(trimmed, out _))
            {
                valid.Add(trimmed);
            }
        }
        return valid;
    }

    private enum FirewallOperationType
    {
        Block,
        Remove,
        Flush
    }

    private sealed class FirewallOperationItem
    {
        public FirewallOperationType Type { get; }
        public IReadOnlyList<string> Addresses { get; }
        public TaskCompletionSource? CompletionSource { get; }

        public FirewallOperationItem(FirewallOperationType type, IReadOnlyList<string> addresses, TaskCompletionSource? completionSource)
        {
            Type = type;
            Addresses = addresses;
            CompletionSource = completionSource;
        }
    }

    private sealed class FirewallLockedAddressTracker : IDisposable
    {
        private readonly ReaderWriterLockSlim lockSlim = new();
        private readonly HashSet<string> exactAddresses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<IPAddress> exactIpObjects = [];
        private readonly HashSet<(IPAddress Network, int PrefixLength)> subnets = [];
        private readonly HashSet<IPAddress> excludedIpObjects = [];
        private readonly IFirewallPolicy fallbackPolicy;
        private bool wildcard;
        private bool initialized;

        public FirewallLockedAddressTracker(IFirewallPolicy fallbackPolicy)
        {
            this.fallbackPolicy = fallbackPolicy;
            EnsureInitialized();
        }

        public bool IsLocked(string ipAddress)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            string trimmed = ipAddress.Trim();

            lockSlim.EnterReadLock();
            try
            {
                if (IPAddress.TryParse(trimmed, out IPAddress? parsedIp))
                {
                    if (excludedIpObjects.Contains(parsedIp)) return false;
                    if (wildcard) return true;
                    if (exactIpObjects.Contains(parsedIp)) return true;
                    if (exactAddresses.Contains(trimmed) || exactAddresses.Contains(parsedIp.ToString())) return true;
                    return IsInAnySubnet(parsedIp);
                }

                if (trimmed == "*") return wildcard;
                if (exactAddresses.Contains(trimmed)) return true;

                string[] parts = trimmed.Split('/', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? net))
                {
                    if (int.TryParse(parts[1], out int prefixLength)
                        || FirewallPolicyManager.TryConvertSubnetMaskToPrefixLength(parts[1], out prefixLength))
                    {
                        return subnets.Contains((net, prefixLength));
                    }
                }

                return false;
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        public HashSet<string> FilterLockedIps(IEnumerable<string> ipAddresses)
        {
            EnsureInitialized();
            HashSet<string> locked = new(StringComparer.OrdinalIgnoreCase);

            lockSlim.EnterReadLock();
            try
            {
                foreach (string raw in ipAddresses)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    string trimmed = raw.Trim();

                    if (IPAddress.TryParse(trimmed, out IPAddress? parsedIp))
                    {
                        if (excludedIpObjects.Contains(parsedIp)) continue;
                        if (wildcard || exactIpObjects.Contains(parsedIp) || exactAddresses.Contains(trimmed) || exactAddresses.Contains(parsedIp.ToString()) || IsInAnySubnet(parsedIp))
                        {
                            locked.Add(trimmed);
                        }
                        continue;
                    }

                    if (trimmed == "*" && wildcard)
                    {
                        locked.Add(trimmed);
                        continue;
                    }

                    if (exactAddresses.Contains(trimmed))
                    {
                        locked.Add(trimmed);
                        continue;
                    }

                    string[] parts = trimmed.Split('/', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? net))
                    {
                        if ((int.TryParse(parts[1], out int prefixLength)
                            || FirewallPolicyManager.TryConvertSubnetMaskToPrefixLength(parts[1], out prefixLength))
                            && subnets.Contains((net, prefixLength)))
                        {
                            locked.Add(trimmed);
                        }
                    }
                }
                return locked;
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        public void Add(string address)
        {
            EnsureInitialized();
            lockSlim.EnterWriteLock();
            try
            {
                AddInternal(address);
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public void AddRange(IEnumerable<string> addresses)
        {
            EnsureInitialized();
            lockSlim.EnterWriteLock();
            try
            {
                foreach (string addr in addresses)
                {
                    AddInternal(addr);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public void Remove(string address)
        {
            EnsureInitialized();
            lockSlim.EnterWriteLock();
            try
            {
                RemoveInternal(address);
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public void RemoveRange(IEnumerable<string> addresses)
        {
            EnsureInitialized();
            lockSlim.EnterWriteLock();
            try
            {
                foreach (string addr in addresses)
                {
                    RemoveInternal(addr);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public void Reset(IEnumerable<string> addresses)
        {
            lockSlim.EnterWriteLock();
            try
            {
                exactAddresses.Clear();
                exactIpObjects.Clear();
                subnets.Clear();
                excludedIpObjects.Clear();
                wildcard = false;
                initialized = true;
                foreach (string addr in addresses)
                {
                    AddInternal(addr);
                }
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            lockSlim.Dispose();
        }

        private bool IsInAnySubnet(IPAddress ip)
        {
            foreach (var (network, prefixLength) in subnets)
            {
                if (network.AddressFamily == ip.AddressFamily
                    && FirewallPolicyManager.IsInSubnet(ip, network, prefixLength))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddInternal(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string trimmed = raw.Trim();
            if (trimmed == "*")
            {
                wildcard = true;
                excludedIpObjects.Clear();
                return;
            }

            string[] parts = trimmed.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                exactAddresses.Add(trimmed);
                if (IPAddress.TryParse(trimmed, out IPAddress? ip))
                {
                    exactIpObjects.Add(ip);
                    excludedIpObjects.Remove(ip);
                    exactAddresses.Add(ip.ToString());
                }
            }
            else if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? network))
            {
                if (int.TryParse(parts[1], out int prefixLength)
                    || FirewallPolicyManager.TryConvertSubnetMaskToPrefixLength(parts[1], out prefixLength))
                {
                    subnets.Add((network, prefixLength));
                    exactAddresses.Add($"{network}/{prefixLength}");
                    exactAddresses.Add(trimmed);
                    excludedIpObjects.RemoveWhere(ip => ip.AddressFamily == network.AddressFamily && FirewallPolicyManager.IsInSubnet(ip, network, prefixLength));
                }
            }
        }

        private void RemoveInternal(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string trimmed = raw.Trim();
            if (trimmed == "*")
            {
                wildcard = false;
                exactAddresses.Clear();
                exactIpObjects.Clear();
                subnets.Clear();
                excludedIpObjects.Clear();
                return;
            }

            string[] parts = trimmed.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                exactAddresses.Remove(trimmed);
                if (IPAddress.TryParse(trimmed, out IPAddress? ip))
                {
                    exactAddresses.Remove(ip.ToString());
                    exactIpObjects.Remove(ip);
                    if (wildcard || IsInAnySubnet(ip))
                    {
                        excludedIpObjects.Add(ip);
                    }
                }
            }
            else if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? network))
            {
                if (int.TryParse(parts[1], out int prefixLength)
                    || FirewallPolicyManager.TryConvertSubnetMaskToPrefixLength(parts[1], out prefixLength))
                {
                    subnets.Remove((network, prefixLength));
                    exactAddresses.Remove($"{network}/{prefixLength}");
                    exactAddresses.Remove(trimmed);
                }
            }
        }

        private void EnsureInitialized()
        {
            if (initialized) return;

            lockSlim.EnterWriteLock();
            try
            {
                if (initialized) return;
                try
                {
                    var blocked = fallbackPolicy.GetBlockedAddresses();
                    if (blocked != null)
                    {
                        foreach (string addr in blocked)
                        {
                            AddInternal(addr);
                        }
                    }
                }
                catch
                {
                    // 若底層策略尚未就緒或處於單元測試環境，則繼續進行
                }
                initialized = true;
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
    }
}
