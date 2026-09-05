using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.Network;

/// <summary>
/// 限制 HTTP 內容讀取大小，避免未知長度或不可信來源造成無界記憶體配置。
/// </summary>
public static class BoundedHttpContent
{
    /// <summary>
    /// 以串流讀取有限長度的 UTF-8 文字。
    /// </summary>
    /// <param name="content">HTTP 回應內容。</param>
    /// <param name="maximumBytes">允許的最大位元組數。</param>
    /// <param name="cancellationToken">包含下載期限的取消權杖。</param>
    /// <returns>完整的 UTF-8 文字。</returns>
    /// <exception cref="InvalidDataException">內容超出允許長度。</exception>
    public static async Task<string> ReadAsync(HttpContent content, int maximumBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (content.Headers.ContentLength > maximumBytes) throw new InvalidDataException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("HTTP content exceeds the size limit."));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        using Stream source = await content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
        using MemoryStream destination = new();
        byte[] buffer = new byte[16384];
        int count;
        while ((count = await source.ReadAsync(buffer, deadline.Token).ConfigureAwait(false)) != 0)
        {
            if (destination.Length + count > maximumBytes) throw new InvalidDataException(global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("HTTP content exceeds the size limit."));
            destination.Write(buffer, 0, count);
        }
        return Encoding.UTF8.GetString(destination.GetBuffer(), 0, (int)destination.Length);
    }
}