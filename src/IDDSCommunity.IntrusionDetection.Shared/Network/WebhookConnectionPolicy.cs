using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 將可設定 Webhook 的目的地限制落實於 Socket 建立前，並禁止重新導向與代理。
/// </summary>
public static class WebhookConnectionPolicy
{
    private static readonly IPNetwork[] RestrictedNetworks =
    [
        IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"), IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("127.0.0.0/8"), IPNetwork.Parse("0.0.0.0/8"),
        IPNetwork.Parse("169.254.0.0/16"), IPNetwork.Parse("224.0.0.0/4"),
        IPNetwork.Parse("240.0.0.0/4"), IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("fc00::/7"), IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("ff00::/8")
    ];

    /// <summary>
    /// 建立與指定 Webhook URL 綁定之 HTTP 用戶端，連線僅使用通過檢查的位址。
    /// </summary>
    /// <param name="destinationUrl">可設定的 Webhook 目的地網址。</param>
    /// <param name="privateDestinations">每行一筆「HTTPS 網址|IP 或 CIDR」的專用內網目的地允許清單。</param>
    /// <param name="timeout">要求逾時時間。</param>
    /// <returns>已限制連線目的地的 HTTP 用戶端。</returns>
    public static HttpClient CreateClient(string destinationUrl, string? privateDestinations = null, TimeSpan? timeout = null)
        => CreateClientForTests(destinationUrl, privateDestinations, timeout, ResolveAddressesAsync, ConnectAddressAsync);

    /// <summary>
    /// 透過共用 HTTP 用戶端工廠派送 Webhook 要求，並於實際連線前檢查目的地。
    /// </summary>
    /// <param name="request">要派送的 HTTP 要求。</param>
    /// <param name="destinationUrl">已設定的 Webhook 目的地網址。</param>
    /// <param name="privateDestinations">專用內網目的地允許清單。</param>
    /// <param name="timeout">要求逾時時間。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>HTTP 回應；呼叫端須釋放此物件。</returns>
    public static Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string destinationUrl,
        string? privateDestinations = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => WebhookHttpClientFactory.Shared.SendAsync(request, destinationUrl, privateDestinations, timeout, cancellationToken);

    internal static Uri ParseDestination(string destinationUrl)
    {
        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri? destination)
            || destination.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(destination.UserInfo))
            throw new ArgumentException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook URL must be an absolute HTTP or HTTPS URL."), nameof(destinationUrl));
        return destination;
    }

    internal static HttpClient CreateClientForTests(string destinationUrl, string? privateDestinations, TimeSpan? timeout,
        Func<string, AddressFamily, CancellationToken, Task<IPAddress[]>> resolver,
        Func<IPAddress, int, CancellationToken, Task<Stream>> connector)
    {
        Uri destination = ParseDestination(destinationUrl);
        SocketsHttpHandler handler = new()
        {
            AllowAutoRedirect = false, UseProxy = false, PooledConnectionLifetime = TimeSpan.Zero,
            ConnectCallback = async (context, cancellationToken) =>
            {
                return await ConnectCheckedAsync(context, destination, privateDestinations, resolver, connector, cancellationToken).ConfigureAwait(false);
            }
        };
        return new HttpClient(handler, disposeHandler: true) { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
    }

    internal static async Task<Stream> ConnectCheckedAsync(SocketsHttpConnectionContext context, Uri destination,
        string? privateDestinations, Func<string, AddressFamily, CancellationToken, Task<IPAddress[]>> resolver,
        Func<IPAddress, int, CancellationToken, Task<Stream>> connector, CancellationToken cancellationToken)
    {
        Uri? requestUri = context.InitialRequestMessage.RequestUri;
        if (requestUri is null || requestUri.Scheme != destination.Scheme
            || !string.Equals(requestUri.IdnHost, destination.IdnHost, StringComparison.OrdinalIgnoreCase)
            || requestUri.Port != destination.Port
            || !string.Equals(context.DnsEndPoint.Host, destination.IdnHost, StringComparison.OrdinalIgnoreCase)
            || context.DnsEndPoint.Port != destination.Port)
            throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook destination changed."));
        IPAddress[] addresses = await resolver(context.DnsEndPoint.Host, context.DnsEndPoint.AddressFamily, cancellationToken).ConfigureAwait(false);
        if (addresses.Length == 0 || addresses.Any(address => !IsAllowed(destination, address, privateDestinations)))
            throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook destination resolves to a restricted address."));
        Exception? lastError = null;
        foreach (IPAddress address in addresses)
        {
            try { return await connector(address, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                lastError = ex;
                if (cancellationToken.IsCancellationRequested) throw;
            }
        }
        throw lastError ?? new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook connection failed."));
    }

    internal static Task<IPAddress[]> ResolveAddressesAsync(string host, AddressFamily family, CancellationToken cancellationToken)
        => Dns.GetHostAddressesAsync(host, family, cancellationToken);

    internal static async Task<Stream> ConnectAddressAsync(IPAddress address, int port, CancellationToken cancellationToken)
    {
        Socket socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch { socket.Dispose(); throw; }
    }

    /// <summary>
    /// 判斷解析後的位址是否可用於指定 Webhook 目的地。
    /// </summary>
    /// <param name="destination">Webhook 目的地網址。</param>
    /// <param name="address">DNS 解析後的位址。</param>
    /// <param name="privateDestinations">專用內網目的地允許清單。</param>
    /// <returns>若位址可連線則傳回 true。</returns>
    public static bool IsAllowed(Uri destination, IPAddress address, string? privateDestinations)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (NetworkEndpointValidator.IsBlockedImdsOrLinkLocalAddress(address)) return false;
        if (!RestrictedNetworks.Any(network => network.Contains(address))) return true;
        if (destination.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(privateDestinations)) return false;
        foreach (string line in privateDestinations.Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !Uri.TryCreate(parts[0], UriKind.Absolute, out Uri? origin)
                || origin.Scheme != Uri.UriSchemeHttps || !string.Equals(origin.IdnHost, destination.IdnHost, StringComparison.OrdinalIgnoreCase)
                || origin.Port != destination.Port || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.UserInfo)) continue;
            if (IPAddress.TryParse(parts[1], out IPAddress? allowedIp) && allowedIp.Equals(address)) return true;
            if (IPNetwork.TryParse(parts[1], out IPNetwork network) && network.Contains(address)) return true;
        }
        return false;
    }

    internal static bool HasPrivateAllowanceFor(Uri destination, string? privateDestinations)
    {
        if (destination.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(privateDestinations)) return false;
        foreach (string line in privateDestinations.Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = line.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !Uri.TryCreate(parts[0], UriKind.Absolute, out Uri? origin)
                || origin.Scheme != Uri.UriSchemeHttps || !string.Equals(origin.IdnHost, destination.IdnHost, StringComparison.OrdinalIgnoreCase)
                || origin.Port != destination.Port || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query)
                || !string.IsNullOrEmpty(origin.UserInfo)) continue;
            if (IPAddress.TryParse(parts[1], out _) || IPNetwork.TryParse(parts[1], out _)) return true;
        }
        return false;
    }
}
