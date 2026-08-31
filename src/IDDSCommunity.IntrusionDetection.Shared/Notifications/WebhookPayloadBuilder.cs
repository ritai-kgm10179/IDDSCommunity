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
    /// <returns>傳回建構之 JSON 字串。</returns>
    public static string BuildPayload(
        WebhookPlatform platform,
        string eventTitle,
        string ipAddress,
        string statusName,
        string agentName,
        string details,
        DateTime timestamp,
        string? telegramChatId = null)
    {
        return platform switch
        {
            WebhookPlatform.MicrosoftTeams => BuildTeamsPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp),
            WebhookPlatform.Slack => BuildSlackPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp),
            WebhookPlatform.Discord => BuildDiscordPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp),
            WebhookPlatform.Telegram => BuildTelegramPayload(telegramChatId ?? string.Empty, eventTitle, ipAddress, statusName, agentName, details, timestamp),
            _ => BuildGenericJsonPayload(eventTitle, ipAddress, statusName, agentName, details, timestamp)
        };
    }

    /// <summary>
    /// 建構 Microsoft Teams Adaptive Card 1.6 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildTeamsPayload(string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp)
    {
        var card = new Dictionary<string, object>
        {
            ["type"] = "message",
            ["attachments"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["contentType"] = "application/vnd.microsoft.card.adaptive",
                    ["content"] = new Dictionary<string, object>
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
                                ["text"] = details,
                                ["wrap"] = true
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(card, JsonOptions);
    }

    /// <summary>
    /// 建構 Slack Block Kit 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildSlackPayload(string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp)
    {
        var slackMessage = new Dictionary<string, object>
        {
            ["text"] = $"🛡️ {eventTitle}: {ipAddress}",
            ["blocks"] = new List<object>
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
                        new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*狀態:*\n{statusName}" },
                        new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*代理程式:*\n{agentName}" },
                        new Dictionary<string, string> { ["type"] = "mrkdwn", ["text"] = $"*時間 (UTC):*\n{timestamp:u}" }
                    }
                },
                new Dictionary<string, object>
                {
                    ["type"] = "section",
                    ["text"] = new Dictionary<string, string>
                    {
                        ["type"] = "mrkdwn",
                        ["text"] = $"*詳細資訊:*\n{details}"
                    }
                }
            }
        };

        return JsonSerializer.Serialize(slackMessage, JsonOptions);
    }

    /// <summary>
    /// 建構 Discord Rich Embed 格式之 Webhook 酬載。
    /// </summary>
    public static string BuildDiscordPayload(string eventTitle, string ipAddress, string statusName, string agentName, string details, DateTime timestamp, int colorHex = 0xDC2626)
    {
        var discordMessage = new Dictionary<string, object>
        {
            ["username"] = "IDDS Community",
            ["embeds"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["title"] = $"🛡️ IDDS Community: {eventTitle}",
                    ["description"] = details,
                    ["color"] = colorHex,
                    ["fields"] = new List<object>
                    {
                        new Dictionary<string, object> { ["name"] = "IP 位址", ["value"] = ipAddress, ["inline"] = true },
                        new Dictionary<string, object> { ["name"] = "狀態", ["value"] = statusName, ["inline"] = true },
                        new Dictionary<string, object> { ["name"] = "代理程式", ["value"] = agentName, ["inline"] = true }
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
        string text = $"<b>🛡️ IDDS Community 警報</b>\n\n" +
                      $"<b>事件:</b> {WebUtility.HtmlEncode(eventTitle)}\n" +
                      $"<b>IP 位址:</b> <code>{WebUtility.HtmlEncode(ipAddress)}</code>\n" +
                      $"<b>狀態:</b> {WebUtility.HtmlEncode(statusName)}\n" +
                      $"<b>代理程式:</b> {WebUtility.HtmlEncode(agentName)}\n" +
                      $"<b>時間 (UTC):</b> {timestamp:u}\n\n" +
                      $"<b>詳細資訊:</b>\n{WebUtility.HtmlEncode(details)}";

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
}
