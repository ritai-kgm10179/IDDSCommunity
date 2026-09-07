using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Service;

internal sealed class BoundedHttpDispatcher : IDisposable
{
    private readonly Channel<HttpListenerContext> requests = Channel.CreateBounded<HttpListenerContext>(new BoundedChannelOptions(32)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleWriter = false
    });
    private readonly CancellationTokenSource stopping = new();
    private readonly Task[] workers;
    private int disposed;

    internal BoundedHttpDispatcher(Func<HttpListenerContext, Task> handler)
    {
        workers = new Task[4];
        for (int i = 0; i < workers.Length; i++) workers[i] = Task.Run(async () =>
        {
            await foreach (HttpListenerContext context in requests.Reader.ReadAllAsync())
            {
                if (stopping.IsCancellationRequested) { Abort(context); continue; }
                using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                using CancellationTokenRegistration registration = deadline.Token.Register(() =>
                {
                    try { context.Request.InputStream.Close(); Abort(context); } catch (Exception) { }
                });
                try { await handler(context).ConfigureAwait(false); }
                catch (Exception) { Abort(context); }
            }
        });
    }

    internal void Submit(HttpListenerContext context)
    {
        if (requests.Writer.TryWrite(context)) return;
        try
        {
            context.Response.Headers["Retry-After"] = "5";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.Close();
        }
        catch { }
    }

    internal static async Task<string> ReadBodyAsync(HttpListenerRequest request, int maximumBytes = 65536)
    {
        if (request.ContentLength64 > maximumBytes) throw new RequestBodyTooLargeException();
        using MemoryStream body = new();
        byte[] buffer = new byte[4096];
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(10));
        int count;
        while ((count = await request.InputStream.ReadAsync(buffer, deadline.Token).ConfigureAwait(false)) != 0)
        {
            if (body.Length + count > maximumBytes) throw new RequestBodyTooLargeException();
            body.Write(buffer, 0, count);
        }
        return Encoding.UTF8.GetString(body.GetBuffer(), 0, (int)body.Length);
    }

    private static void Abort(HttpListenerContext context)
    {
        try { context.Response.Abort(); } catch (Exception) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        requests.Writer.TryComplete();
        stopping.Cancel();
        Task completion = Task.WhenAll(workers);
        try
        {
            if (!completion.Wait(TimeSpan.FromSeconds(20)))
            {
                System.Diagnostics.Trace.TraceWarning("HTTP handlers have not finished during shutdown.");
                _ = completion.ContinueWith(_ => stopping.Dispose(), TaskScheduler.Default);
                return;
            }
        }
        catch (AggregateException ex) { System.Diagnostics.Trace.TraceWarning("HTTP shutdown: {0}", ex.GetType().Name); }
        stopping.Dispose();
    }
}

internal sealed class RequestBodyTooLargeException : IOException;