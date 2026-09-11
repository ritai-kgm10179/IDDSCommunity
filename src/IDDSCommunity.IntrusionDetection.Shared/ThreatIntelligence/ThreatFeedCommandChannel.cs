using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;

/// <summary>
/// 提供管理主控台與背景 Windows 服務之間安全、零通訊埠依賴之本機非同步情資更新命令通道。
/// </summary>
public static class ThreatFeedCommandChannel
{
    private static readonly string CommandsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "IDDS Community",
        "Commands");

    private static readonly string RequestFilePath = Path.Combine(CommandsDirectory, "refresh_threat_feeds.req");
    private static readonly string ResponseFilePath = Path.Combine(CommandsDirectory, "refresh_threat_feeds.ack");

    /// <summary>
    /// 代表命令通道之回應資料模型。
    /// </summary>
    private sealed class CommandResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int IngestedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 於管理端發送即時更新請求，並非同步等待背景服務執行完成與回傳結果。
    /// </summary>
    /// <param name="timeout">等候回應之逾時時間。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>傳回包含成功狀態、匯入筆數與錯誤訊息之結果 Tuple。</returns>
    public static async Task<(bool Success, int IngestedCount, string ErrorMessage)> SendRefreshRequestAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(CommandsDirectory);

            string requestId = Guid.NewGuid().ToString("N");

            // 清理可能殘留的舊回應
            if (File.Exists(ResponseFilePath))
            {
                try { File.Delete(ResponseFilePath); } catch { }
            }

            // 寫入請求檔案
            await File.WriteAllTextAsync(RequestFilePath, requestId, cancellationToken).ConfigureAwait(false);

            // 輪詢等待回應檔案
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                if (File.Exists(ResponseFilePath))
                {
                    string json = string.Empty;
                    try
                    {
                        json = await File.ReadAllTextAsync(ResponseFilePath, cancellationToken).ConfigureAwait(false);
                    }
                    catch (IOException)
                    {
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        try
                        {
                            CommandResponse? resp = JsonSerializer.Deserialize<CommandResponse>(json);
                            if (resp != null && resp.RequestId == requestId)
                            {
                                try { File.Delete(ResponseFilePath); } catch { }
                                return (resp.Success, resp.IngestedCount, resp.ErrorMessage ?? string.Empty);
                            }
                        }
                        catch (JsonException)
                        {
                            // 等待寫入完成
                        }
                    }
                }

                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }

            return (false, 0, "Request timed out waiting for background service response.");
        }
        catch (OperationCanceledException)
        {
            return (false, 0, "Operation was cancelled.");
        }
        catch (Exception ex)
        {
            return (false, 0, ex.Message);
        }
        finally
        {
            if (File.Exists(RequestFilePath))
            {
                try { File.Delete(RequestFilePath); } catch { }
            }
        }
    }

    /// <summary>
    /// 啟動背景命令監聽器，當偵測到前台發出情資更新請求時調用回呼執行並回報結果。
    /// </summary>
    /// <param name="refreshAction">執行情資下載與同步之委派，其結果為匯入之威脅數量。</param>
    /// <param name="cancellationToken">取消權杖。</param>
    /// <returns>表示監聽迴圈作業之 Task。</returns>
    public static async Task StartCommandListenerAsync(
        Func<Task<int>> refreshAction,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(refreshAction);

        try
        {
            Directory.CreateDirectory(CommandsDirectory);
        }
        catch
        {
            // 目錄建立失敗時由後續處理
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(RequestFilePath))
                {
                    string requestId = string.Empty;
                    try
                    {
                        requestId = (await File.ReadAllTextAsync(RequestFilePath, cancellationToken).ConfigureAwait(false)).Trim();
                    }
                    catch
                    {
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(requestId))
                    {
                        try { File.Delete(RequestFilePath); } catch { }

                        CommandResponse response = new() { RequestId = requestId };
                        try
                        {
                            int count = await refreshAction().ConfigureAwait(false);
                            response.Success = true;
                            response.IngestedCount = count;
                        }
                        catch (Exception ex)
                        {
                            response.Success = false;
                            response.ErrorMessage = ex.Message;
                        }

                        try
                        {
                            string json = JsonSerializer.Serialize(response);
                            await File.WriteAllTextAsync(ResponseFilePath, json, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            // 寫入失敗忽略
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 忽略非嚴重錯誤
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}