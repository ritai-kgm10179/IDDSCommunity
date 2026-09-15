using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 管理可設定 Webhook 的 HTTP 處理常式與連線池，並保留逐次要求的目的地檢查。
/// </summary>
internal sealed class WebhookHttpClientFactory : IDisposable
{
    private const string PublicClientName = "IDDS.Webhook.Public";
    private const string PrivateClientName = "IDDS.Webhook.Private";
    private static readonly HttpRequestOptionsKey<WebhookRequestPolicy> PolicyKey = new("IDDS.Webhook.RequestPolicy");
    private readonly ServiceProvider provider;
    private readonly IHttpClientFactory clients;

    internal static WebhookHttpClientFactory Shared { get; } = new(WebhookConnectionPolicy.ResolveAddressesAsync, WebhookConnectionPolicy.ConnectAddressAsync);

    internal WebhookHttpClientFactory(
        Func<string, AddressFamily, CancellationToken, Task<IPAddress[]>> resolver,
        Func<IPAddress, int, CancellationToken, Task<Stream>> connector)
    {
        ServiceCollection services = new();
        services.AddHttpClient(PublicClientName)
            .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(false, resolver, connector))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        services.AddHttpClient(PrivateClientName)
            .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(true, resolver, connector))
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        provider = services.BuildServiceProvider();
        clients = provider.GetRequiredService<IHttpClientFactory>();
    }

    internal async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string destinationUrl,
        string? privateDestinations, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        Uri destination = WebhookConnectionPolicy.ParseDestination(destinationUrl);
        if (request.RequestUri is not Uri requested || requested.Scheme != destination.Scheme
            || !string.Equals(requested.IdnHost, destination.IdnHost, StringComparison.OrdinalIgnoreCase)
            || requested.Port != destination.Port)
            throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook destination changed."));

        bool allowPrivate = WebhookConnectionPolicy.HasPrivateAllowanceFor(destination, privateDestinations);
        request.Options.Set(PolicyKey, new WebhookRequestPolicy(destination, allowPrivate ? privateDestinations : null));
        using HttpClient client = clients.CreateClient(allowPrivate ? PrivateClientName : PublicClientName);
        client.Timeout = timeout ?? TimeSpan.FromSeconds(10);
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    internal HttpMessageHandler GetHandlerForTests(bool allowPrivate)
        => provider.GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(allowPrivate ? PrivateClientName : PublicClientName);

    private static SocketsHttpHandler CreateHandler(bool allowPrivate,
        Func<string, AddressFamily, CancellationToken, Task<IPAddress[]>> resolver,
        Func<IPAddress, int, CancellationToken, Task<Stream>> connector)
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            PooledConnectionLifetime = allowPrivate ? TimeSpan.Zero : TimeSpan.FromMinutes(2),
            ConnectCallback = async (context, cancellationToken) =>
            {
                if (!context.InitialRequestMessage.Options.TryGetValue(PolicyKey, out WebhookRequestPolicy policy))
                    throw new InvalidOperationException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Webhook destination changed."));
                return await WebhookConnectionPolicy.ConnectCheckedAsync(context, policy.Destination,
                    allowPrivate ? policy.PrivateDestinations : null, resolver, connector, cancellationToken).ConfigureAwait(false);
            }
        };
    }

    public void Dispose() => provider.Dispose();

    private readonly record struct WebhookRequestPolicy(Uri Destination, string? PrivateDestinations);
}
