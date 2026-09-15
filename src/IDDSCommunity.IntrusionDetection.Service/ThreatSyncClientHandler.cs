using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using IDDSCommunity.IntrusionDetection.Service.Protos;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Network;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供邊緣節點與威脅情資中繼中心通訊之 gRPC 同步串流處理器，具備向後相容之自動 REST 容錯移轉能力。
/// </summary>
internal sealed class ThreatSyncClientHandler : IDisposable
{
    private readonly ThreatHubClient restClient;
    private readonly bool disposeRestClient;
    private readonly IddsConfig config;
    private readonly Action<string> logInformation;
    private readonly Action<string, Exception> logWarning;
    private readonly ConcurrentDictionary<string, GrpcChannel> channelCache = new(StringComparer.OrdinalIgnoreCase);
    private bool disposed;

    /// <summary>
    /// 初始化 <see cref="ThreatSyncClientHandler"/> 類別之新執行個體。
    /// </summary>
    /// <param name="config">系統組態設定執行個體。</param>
    /// <param name="restClient">可選之既有 REST 客戶端；若為 null 則自動建立。</param>
    /// <param name="logInformation">資訊日誌委派。</param>
    /// <param name="logWarning">警告日誌委派。</param>
    internal ThreatSyncClientHandler(
        IddsConfig config,
        ThreatHubClient? restClient = null,
        Action<string>? logInformation = null,
        Action<string, Exception>? logWarning = null)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        if (restClient != null)
        {
            this.restClient = restClient;
            disposeRestClient = false;
        }
        else
        {
            this.restClient = new ThreatHubClient();
            disposeRestClient = true;
        }
        this.logInformation = logInformation ?? (msg => System.Diagnostics.Trace.TraceInformation(msg));
        this.logWarning = logWarning ?? ((msg, ex) => System.Diagnostics.Trace.TraceWarning("{0}: {1}", msg, ex.Message));
    }

    private GrpcChannel GetOrCreateChannel(string endpoint)
    {
        return channelCache.GetOrAdd(endpoint, ep =>
        {
            string grpcUrl = ResolveGrpcEndpoint(ep);
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                EnableMultipleHttp2Connections = true,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    DnsEndPoint dnsEndPoint = context.DnsEndPoint;
                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(dnsEndPoint.Host, dnsEndPoint.AddressFamily, cancellationToken).ConfigureAwait(false);
                    foreach (IPAddress address in addresses)
                    {
                        if (NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(address))
                        {
                            throw new InvalidOperationException(string.Format(
                                global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Connection to IMDS or link-local address '{0}' is blocked."),
                                address));
                        }
                    }
                    Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(addresses, dnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            return GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
            {
                HttpHandler = handler,
                DisposeHttpClient = true
            });
        });
    }

    private string ResolveGrpcEndpoint(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
        {
            int grpcPort = config.ThreatHubGrpcPort > 0 ? config.ThreatHubGrpcPort : (uri.Port == 8443 ? 8445 : (uri.Port > 0 ? uri.Port + 2 : 8445));
            UriBuilder builder = new(uri)
            {
                Port = grpcPort
            };
            return builder.Uri.ToString().TrimEnd('/');
        }
        return endpoint;
    }

    /// <summary>
    /// 優先透過 gRPC 執行威脅情資雙向同步，若失敗則自動容錯移轉至 REST HTTP API。
    /// </summary>
    /// <param name="endpoint">中繼中心伺服器端點位址。</param>
    /// <param name="apiKey">叢集授權 API 金鑰。</param>
    /// <param name="payload">本次同步請求載體。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>表示非同步作業之工作，其結果為同步回應物件。</returns>
    public async Task<ThreatHubSyncResponse> SynchronizeAsync(
        string endpoint,
        string apiKey,
        ThreatHubSyncPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(payload);

        if (config.EnableThreatHubGrpc)
        {
            try
            {
                GrpcChannel channel = GetOrCreateChannel(endpoint);
                var grpcClient = new ThreatSyncService.ThreatSyncServiceClient(channel);

                var syncRequest = new SyncRequest
                {
                    NodeId = payload.NodeId ?? string.Empty,
                    NodeName = payload.NodeName ?? string.Empty,
                    ApiKey = apiKey,
                    Cursor = payload.Cursor,
                    Generation = payload.Generation ?? string.Empty
                };

                if (payload.NewThreats != null)
                {
                    foreach (var threat in payload.NewThreats)
                    {
                        syncRequest.NewThreats.Add(new ThreatRecord
                        {
                            SourceIp = threat.SourceIp,
                            ThreatCategory = threat.ThreatCategory,
                            ConfidenceScore = threat.ConfidenceScore,
                            ReportedTicks = threat.ReportedUtc.Ticks,
                            ExpiresTicks = threat.ExpiresUtc.Ticks,
                            ReporterNodeId = threat.ReporterNodeId,
                            ReporterNodeName = threat.ReporterNodeName,
                            Reason = threat.Notes ?? string.Empty
                        });
                    }
                }

                DateTime deadline = DateTime.UtcNow.AddSeconds(15);
                var callOptions = new CallOptions(deadline: deadline, cancellationToken: cancellationToken);
                SyncResponse grpcResponse = await grpcClient.SyncAsync(syncRequest, callOptions).ResponseAsync.ConfigureAwait(false);

                if (grpcResponse.Success)
                {
                    List<ThreatIntelligenceItem> activeThreats = [];
                    foreach (var record in grpcResponse.ActiveThreats)
                    {
                        activeThreats.Add(new ThreatIntelligenceItem
                        {
                            SourceIp = record.SourceIp,
                            ThreatCategory = record.ThreatCategory,
                            ConfidenceScore = record.ConfidenceScore,
                            ReportedUtc = new DateTime(record.ReportedTicks, DateTimeKind.Utc),
                            ExpiresUtc = new DateTime(record.ExpiresTicks, DateTimeKind.Utc),
                            ReporterNodeId = record.ReporterNodeId,
                            ReporterNodeName = record.ReporterNodeName,
                            Notes = record.Reason ?? string.Empty
                        });
                    }

                    List<ThreatHubJournalEntry> deltaEvents = [];
                    if (grpcResponse.DeltaBatch?.Events != null)
                    {
                        foreach (var ev in grpcResponse.DeltaBatch.Events)
                        {
                            deltaEvents.Add(new ThreatHubJournalEntry
                            {
                                Sequence = ev.Sequence,
                                EventType = (int)ev.EventType,
                                SourceIp = ev.SourceIp,
                                Payload = ev.Payload,
                                CreatedTicks = ev.CreatedTicks
                            });
                        }
                    }

                    return new ThreatHubSyncResponse
                    {
                        Success = true,
                        Generation = grpcResponse.Generation,
                        NextCursor = grpcResponse.NextCursor,
                        HasMore = grpcResponse.HasMore,
                        ActiveThreats = activeThreats,
                        DeltaEvents = deltaEvents
                    };
                }
                else
                {
                    logWarning($"Threat Hub gRPC sync returned failure: {grpcResponse.ErrorMessage}; falling back to REST.", new InvalidOperationException(grpcResponse.ErrorMessage));
                }
            }
            catch (Exception ex) when (ex is RpcException or HttpRequestException or TimeoutException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logInformation($"Threat Hub gRPC synchronization failed ({ex.GetType().Name}: {ex.Message}); automatically falling back to REST API.");
            }
        }

        // 容錯降級使用 REST API 進行同步
        return await restClient.SynchronizeAsync(endpoint, apiKey, payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 透過 gRPC 伺服器串流拉取威脅情資增量日誌批次。
    /// </summary>
    /// <param name="endpoint">中繼中心伺服器端點位址。</param>
    /// <param name="apiKey">叢集授權 API 金鑰。</param>
    /// <param name="cursor">起始序號游標。</param>
    /// <param name="generation">中繼中心世代識別碼。</param>
    /// <param name="onBatchReceived">接收到批次時執行之回呼委派。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>表示非同步串流拉取作業之工作。</returns>
    public async Task<bool> PullDeltasStreamAsync(
        string endpoint,
        string apiKey,
        long cursor,
        string generation,
        Func<ThreatDeltaBatch, Task> onBatchReceived,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(onBatchReceived);

        try
        {
            GrpcChannel channel = GetOrCreateChannel(endpoint);
            var grpcClient = new ThreatSyncService.ThreatSyncServiceClient(channel);
            var request = new StreamDeltasRequest
            {
                ApiKey = apiKey,
                Cursor = cursor,
                Generation = generation
            };

            using var call = grpcClient.PullDeltas(request, cancellationToken: cancellationToken);
            while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                ThreatDeltaBatch batch = call.ResponseStream.Current;
                await onBatchReceived(batch).ConfigureAwait(false);
            }
            return true;
        }
        catch (Exception ex)
        {
            logWarning("Failed to stream threat deltas via gRPC", ex);
            return false;
        }
    }

    /// <summary>
    /// 釋放通道與客戶端所配置之非受控資源。
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        foreach (var channel in channelCache.Values)
        {
            try { channel.Dispose(); } catch { }
        }
        channelCache.Clear();

        if (disposeRestClient)
        {
            restClient.Dispose();
        }
    }
}
