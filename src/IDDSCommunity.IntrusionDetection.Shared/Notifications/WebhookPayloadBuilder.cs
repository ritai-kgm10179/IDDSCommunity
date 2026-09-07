using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace IDDSCommunity.IntrusionDetection.Shared.Notifications;

/// <summary>
/// 提供多平台 Webhook（Microsoft Teams、Slack、Discord、Telegram、通用 JSON）警報酬載建構器。
/// </summary>
public static class WebhookPayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// 依據指定平台類型與事件參數建構對應之 Webhook JSON 酬載字串。
    /// </summary>
    /// <param name="platform">目標平台類型。</param>
    /// <param name="eventTitle">事件標題。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="statusName">鎖定狀態名稱。</param>
    /// <param name="agentName">觸發代理程式名稱。</param>
    /// <param name="details">事件詳細資訊。</param>
    /// <param name="timestamp">事件發生時間（UTC）。</param>
    /// <param name="telegramChatId">Telegram Chat ID（僅 Telegram 平台需要）。</param>
    /// <param name="managementApiBaseUrl">管理 API 對外基底 URL（選擇性，用於產生 ChatOps 處置按鈕）。</param>
    /// <param name="managementApiKey">管理 API 簽署金鑰（選擇性，用於簽署處置權杖）。</param>
    /// <returns>傳回建構之 JSON 字串。</returns>
    public static string BuildPayload(
        WebhookPlatform platform,
        string eventTitle,
        string ipAddress,
        string statusName,
        string agentName,
        string details,
        DateTime timestamp,
        string? telegramChatId = null,
        string? managementApiBaseUrl = null,
        string? managementApiKey = null)
    {
        return platform switch
        {
            WebhookPlatform.MicrosoftTeams => BuildTeamsPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp, managementApiBaseUrl, managementApiKey),
            WebhookPlatform.Slack => BuildSlackPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp, managementApiBaseUrl, managementApiKey),
            WebhookPlatform.Discord => BuildDiscordPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp),
            WebhookPlatform.Telegram => BuildTelegramPayload(telegramChatId ?? string.Empty, eventTitle, ipAddress, statusName, agentName, details, timestamp),
            WebhookPlatform.LineMessagingApi => BuildLineMessagingPayload(telegramChatId ?? string.Empty, eventTitle, ipAddress, statusName, agentName, details, timestamp),
            _ => BuildGenericJsonPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp)
        };
    }

    private static string EscapeSlackMrkdwn(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string SanitizeDiscordMarkdown(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("@everyone", "@\u200beveryone")
            .Replace("@here", "@\u200bhere")
            .Replace("[", "\\[")
            .Replace("]", "\\]");
    }

    private static string TruncateText(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input ?? string.Empty;
        return input[..maxLength] + "...";
    }

    /// <summary>
    /// 建構 Microsoft Teams Adaptive Card 1.6 格式之 Webhook 酬載。
    /// </summary>
    /// <param name="eventTitle">事件標題。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="statusName">鎖定狀態名稱。</param>
    /// <param name="agentName">觸發代理程式名稱。</param>
    /// <param name="details">事件詳細資訊。</param>
    /// <param name="timestamp">事件發生時間（UTC）。</param>
    /// <param name="managementApiBaseUrl">管理 API 對外基底 URL（選擇性，用於產生 ChatOps 處置按鈕）。</param>
    /// <param name="managementApiKey">管理 API 簽署金鑰（選擇性，用於簽署處置權杖）。</param>
    /// <returns>傳回 Microsoft Teams JSON 酬載字串。</returns>
    public static string BuildTeamsPayload(
        string eventTitle,
        string ipAddress,
        string statusName,
        string agentName,
        string details,
        DateTime timestamp,
        string? managementApiBaseUrl = null,
        string? managementApiKey = null)
    {
        string safeDetails = TruncateText(details, 4000);
        var cardContent = new Dictionary<string, object>
        {
            ["type"] = "AdaptiveCard",
            ["version"] = "1.6",
            ["body"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["type"] = "TextBlock",
                    ["text"] = "🛡️ IDDS Community 警報",
                    ["weight"] = "Bolder",
                    ["size"] = "Medium",
                    ["color"] = "Attention"
                },
                new Dictionary<string, object>
                {
                    ["type"] = "TextBlock",
                    ["text"] = eventTitle,
                    ["weight"] = "Bolder",
                    ["size"] = "Large"
                },
                new Dictionary<string, object>
                {
                    ["type"] = "FactSet",
                    ["facts"] = new List<object>
                    {
                        new Dictionary<string, string> { ["title"] = "IP 位址", ["value"] = ipAddress },
                        new Dictionary<string, string> { ["title"] = "狀態", ["value"] = statusName },
                        new Dictionary<string, string> { ["title"] = "代理程式", ["value"] = agentName },
                        new Dictionary<string, string> { ["title"] = "時間 (UTC)", ["value"] = timestamp.ToString("u") }
                    }
                },
                new Dictionary<string, object>
                {
                    ["type"] = "TextBlock",
                    ["text"] = safeDetails,
                    ["wrap"] = true
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(managementApiBaseUrl) && !string.IsNullOrWhiteSpace(managementApiKey) && IPAddress.TryParse(ipAddress, out _))
        {
            try
            {
                string unblockToken = Security.ActionTokenService.GenerateToken("unblock", ipAddress, 15, managementApiKey);
                string blockToken = Security.ActionTokenService.GenerateToken("block", ipAddress, 15, managementApiKey);
                string unblockUrl = $"{managementApiBaseUrl.TrimEnd('/')}/api/v1/actions/unblock?token={Uri.EscapeDataString(unblockToken)}";
                string blockUrl = $"{managementApiBaseUrl.TrimEnd('/')}/api/v1/actions/block?token={Uri.EscapeDataString(blockToken)}";

                cardContent["actions"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "Action.OpenUrl",
                        ["title"] = "🔓 一鍵解除封鎖 (Unblock)",
                        ["url"] = unblockUrl,
                        ["style"] = "positive"
                    },
                    new Dictionary<string, object>
                    {
                        ["type"] = "Action.OpenUrl",
                        ["title"] = "🚫 永久封鎖 (Block)",
                        ["url"] = blockUrl,
                        ["style"] = "destructive"
                    }
                };
            }
            catch
            {
                // 若密鑰無效或權杖生成失敗，安全略過處置按鈕
            }
        }

        var card = new Dictionary<string, object>
        {
            ["type"] = "message",
            ["attachments"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["contentType"] = "application/vnd.microsoft.card.adaptive",
                    ["content"] = cardContent
                }
            }
        };

        return JsonSerializer.Serialize(card, JsonOptions);
    }

    /// <summary>
    /// 建構 Slack Block Kit 格式之 Webhook 酬載。
    /// </summary>
    /// <param name="eventTitle">事件標題。</param>
    /// <param name="ipAddress">來源 IP 位址。</param>
    /// <param name="statusName">鎖定狀態名稱。</param>
    /// <param name="agentName">觸發代理程式名稱。</param>
    /// <param name="details">事件詳細資訊。</param>
    /// <param name="timestamp">事件發生時間（UTC）。</param>
    /// <param name="managementApiBaseUrl">管理 API 對外基底 URL（選擇性，用於產生 ChatOps 處置按鈕）。</param>
    /// <param name="managementApiKey">管理 API 簽署金鑰（選擇性，用於簽署處置權杖）。</param>
    /// <returns>傳回 Slack JSON 酬載字串。</returns>
    public static string BuildSlackPayload(
        string eventTitle,
        string ipAddress,
        string statusName,
        string agentName,
        string details,
        DateTime timestamp,
        string? managementApiBaseUrl = null,
        string? managementApiKey = null)
    {
        string cleanTitle = EscapeSlackMrkdwn(eventTitle);
        string cleanStatus = EscapeSlackMrkdwn(statusName);
        string cleanAgent = EscapeSlackMrkdwn(agentName);
        string cleanDetails = EscapeSlackMrkdwn(TruncateText(details, 3000));

        var blocks = new List<object>
        {
            new Dictionary<string, object>
            {
                ["type"] = "header",
                ["text"] = new Dictionary<string, object>
                {
                    ["type"] = "plain_text",
                    ["text"] = $"🛡️ IDDS Community: {eventTitle}",
                    ["emoji"] = true
                }
            },
            new Dictionary<string, object>
            {
                ["type"] = "section",
                ["fields"] = new List<object>
                {
                    new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*IP 位址:*\n`{ipAddress}`" },
                    new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*狀態:*\n{cleanStatus}" },
                    new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*代理程式:*\n{cleanAgent}" },
                    new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*時間 (UTC):*\n{timestamp:u}" }
                }
            },
            new Dictionary<string, object>
            {
                ["type"] = "section",
                ["text"] = new Dictionary<string, string>
                {
                    ["type"] = "mrkdwn",
                    ["text"] = $"*詳細資訊:*\n{cleanDetails}"
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(managementApiBaseUrl) && !string.IsNullOrWhiteSpace(managementApiKey) && IPAddress.TryParse(ipAddress, out _))
        {
            try
            {
                string unblockToken = Security.ActionTokenService.GenerateToken("unblock", ipAddress, 15, managementApiKey);
                string blockToken = Security.ActionTokenService.GenerateToken("block", ipAddress, 15, managementApiKey);
                string unblockUrl = $"{managementApiBaseUrl.TrimEnd('/')}/api/v1/actions/unblock?token={Uri.EscapeDataString(unblockToken)}";
                string blockUrl = $"{managementApiBaseUrl.TrimEnd('/')}/api/v1/actions/block?token={Uri.EscapeDataString(blockToken)}";

                blocks.Add(new Dictionary<string, object>
                {
                    ["type"] = "actions",
                    ["elements"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "button",
                            ["text"] = new Dictionary<string, object>
                            {
                                ["type"] = "plain_text",
                                ["text"] = "🔓 一鍵解除封鎖",
                                ["emoji"] = true
                            },
                            ["style"] = "primary",
                            ["url"] = unblockUrl
                        },
                        new Dictionary<string, object>
                        {
                            ["type"] = "button",
                            ["text"] = new Dictionary<string, object>
                            {
                                ["type"] = "plain_text",
                                ["text"] = "🚫 永久封鎖",
                                ["emoji"] = true
                            },
                            ["style"] = "danger",
                            ["url"] = blockUrl
                        }
                    }
                });
            }
            catch
            {
                // 若密鑰無效或權杖生成失敗，安全略過處置按鈕
            }
        }

        var slackMessage = new Dictionary<string, object>
        {
            ["text"] = $"🛡️ {eventTitle}: {ipAddress}",
            ["blocks"] = blocks
        };

        return JsonSerializer.Serialize(slackMessage, JsonOptions);
    }

    /// <summary>
    /// 建構 Discord Rich Embed 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildDiscordPayload(string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp, int colorHex = 0xDC2626)
    {
        string safeTitle = SanitizeDiscordMarkdown(TruncateText(eventTitle, 250));
        string safeDetails = SanitizeDiscordMarkdown(TruncateText(details, 4000));
        string safeStatus = SanitizeDiscordMarkdown(TruncateText(statusName, 100));
        string safeAgent = SanitizeDiscordMarkdown(TruncateText(agentName, 100));
        var discordMessage = new Dictionary<string, object>
        {
            ["username"] = "IDDS Community",
            ["allowed_mentions"] = new Dictionary<string, object> { ["parse"] = Array.Empty<string>() },
            ["embeds"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["title"] = $"🛡️ IDDS Community: {safeTitle}",
                    ["description"] = safeDetails,
                    ["color"] = colorHex,
                    ["fields"] = new List<object>
                    {
                        new Dictionary<string, object> { ["name"] = "IP 位址", ["value"] = ipAddress, ["inline"] = true },
                        new Dictionary<string, object> { ["name"] = "狀態", ["value"] = safeStatus, ["inline"] = true },
                        new Dictionary<string, object> { ["name"] = "代理程式", ["value"] = safeAgent, ["inline"] = true }
                    },
                    ["timestamp"] = timestamp.ToString("o")
                }
            }
        };

        return JsonSerializer.Serialize(discordMessage, JsonOptions);
    }

    /// <summary>
    /// 建構 Telegram Bot API sendMessage 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildTelegramPayload(string chatId, string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp)
    {
        string safeDetails = TruncateText(details, 4000);
        string text = $"<b>🛡️ IDDS Community 警報</b>\n\n" +
                      $"<b>事件:</b> {WebUtility.HtmlEncode(eventTitle)}\n" +
                      $"<b>IP 位址:</b> <code>{WebUtility.HtmlEncode(ipAddress)}</code>\n" +
                      $"<b>狀態:</b> {WebUtility.HtmlEncode(statusName)}\n" +
                      $"<b>代理程式:</b> {WebUtility.HtmlEncode(agentName)}\n" +
                      $"<b>時間 (UTC):</b> {timestamp:u}\n\n" +
                      $"<b>詳細資訊:</b>\n{WebUtility.HtmlEncode(safeDetails)}";

        var telegramMessage = new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            ["parse_mode"] = "HTML",
            ["text"] = text
        };

        return JsonSerializer.Serialize(telegramMessage, JsonOptions);
    }

    /// <summary>
    /// 建構標準通用 JSON 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildGenericJsonPayload(string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp)
    {
        var genericMessage = new Dictionary<string, object>
        {
            ["system"] = "IDDS Community",
            ["event_title"] = eventTitle,
            ["ip_address"] = ipAddress,
            ["status"] = statusName,
            ["agent"] = agentName,
            ["details"] = details,
            ["timestamp_utc"] = timestamp.ToString("o")
        };

        return JsonSerializer.Serialize(genericMessage, JsonOptions);
    }

    /// <summary>
    /// 建構 LINE Messaging API Push Message 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildLineMessagingPayload(string toUserIdOrGroupId, string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp)
    {
        string safeDetails = TruncateText(details, 4500);
        string text = $"🛡️【IDDS Community 警報】\n" +
                      $"• 事件：{eventTitle}\n" +
                      $"• 來源 IP：{ipAddress}\n" +
                      $"• 狀態：{statusName}\n" +
                      $"• 代理：{agentName}\n" +
                      $"• 時間：{timestamp:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                      $"詳細說明：\n{safeDetails}";

        var lineMessage = new Dictionary<string, object>
        {
            ["to"] = toUserIdOrGroupId,
            ["messages"] = new List<object>
            {
                new Dictionary<string, string>
                {
                    ["type"] = "text",
                    ["text"] = text
                }
            }
        };

        return JsonSerializer.Serialize(lineMessage, JsonOptions);
    }
}
