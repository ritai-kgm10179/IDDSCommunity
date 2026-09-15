using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Service;

/// <summary>
/// 提供具備容量約束與工作者並行池之 HTTP 請求非同步分派器。
/// </summary>
internal sealed class BoundedHttpDispatcher : IDisposable
{
    private readonly Channel<HttpListenerContext> requests;
    private readonly CancellationTokenSource stopping = new();
    private readonly Task[] workers;
    private int disposed;

    /// <summary>
    /// 初始化 <see cref="BoundedHttpDispatcher"/> 類別之新執行個體。
    /// </summary>
    /// <param name="handler">處理個別 HTTP 請求之非同步委派。</param>
    /// <param name="capacity">有界通道容量上限（預設 2,048）。</param>
    /// <param name="workerCount">背景工作者工作數量；若為 null 則依處理器核心數動態配置。</param>
    internal BoundedHttpDispatcher(Func<HttpListenerContext, Task> handler, int capacity = 2048, int? workerCount = null)
    {
        int effectiveCapacity = capacity > 0 ? capacity : 2048;
        requests = Channel.CreateBounded<HttpListenerContext>(new BoundedChannelOptions(effectiveCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false
        });

        int effectiveWorkers = workerCount ?? Math.Clamp(Environment.ProcessorCount * 4, 16, 128);
        workers = new Task[effectiveWorkers];
        for (int i = 0; i < workers.Length; i++) workers[i] = Task.Run(async () =>
        {
            await foreach (HttpListenerContext context in requests.Reader.ReadAllAsync().ConfigureAwait(false))
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

    /// <summary>
    /// 將 HTTP 請求上下文提交至分派佇列。
    /// </summary>
    /// <param name="context">傳入之 HTTP 監聽器上下文。</param>
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

    /// <summary>
    /// 讀取 HTTP 請求內文並限制其最大位元組數。
    /// </summary>
    /// <param name="request">HTTP 請求執行個體。</param>
    /// <param name="maximumBytes">允許之最大位元組數限制。</param>
    /// <returns>以 UTF-8 解碼之字串內容。</returns>
    /// <exception cref="RequestBodyTooLargeException">當請求內文大小超過上限時擲出。</exception>
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

    /// <summary>
    /// 釋放分派器所佔用之資源並等待工作者完成。
    /// </summary>
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