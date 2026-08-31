using System;
using System.Text.Json;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Shared.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 驗證 WebhookPayloadBuilder 在不同平台下之 JSON 酬載建構格式正確性。
/// </summary>
[TestClass]
public sealed class WebhookPayloadBuilderTest
{
    private static readonly DateTime TestTimestamp = new(2026, 8, 31, 4, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// 驗證 Microsoft Teams Adaptive Card 1.6 酬載結構。
    /// </summary>
    [TestMethod]
    public void BuildTeamsPayload_ContainsAdaptiveCardStructure()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.MicrosoftTeams,
            "硬封鎖已套用",
            "198.51.100.42",
            "Hard lock",
            "Windows RDP Agent",
            "Multiple failed login attempts detected.",
            TestTimestamp);

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("message", root.GetProperty("type").GetString());
        var attachments = root.GetProperty("attachments");
        Assert.IsTrue(attachments.GetArrayLength() > 0);

        var content = attachments[0].GetProperty("content");
        Assert.AreEqual("AdaptiveCard", content.GetProperty("type").GetString());
        Assert.AreEqual("1.6", content.GetProperty("version").GetString());
        Assert.IsTrue(json.Contains("198.51.100.42", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 Slack Block Kit 酬載結構。
    /// </summary>
    [TestMethod]
    public void BuildSlackPayload_ContainsBlockKitStructure()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.Slack,
            "軟封鎖已套用",
            "203.0.113.88",
            "Soft lock",
            "OpenSSH Agent",
            "SSH authentication failures exceeded threshold.",
            TestTimestamp);

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.IsTrue(root.TryGetProperty("blocks", out var blocks));
        Assert.IsTrue(blocks.GetArrayLength() > 0);
        Assert.IsTrue(json.Contains("203.0.113.88", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 Discord Rich Embed 酬載結構。
    /// </summary>
    [TestMethod]
    public void BuildDiscordPayload_ContainsEmbedStructure()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.Discord,
            "攻擊偵測",
            "192.0.2.1",
            "Hard lock",
            "SQL Server Agent",
            "SQL injection attack pattern detected.",
            TestTimestamp);

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.IsTrue(root.TryGetProperty("embeds", out var embeds));
        Assert.AreEqual(1, embeds.GetArrayLength());
        Assert.IsTrue(json.Contains("192.0.2.1", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 Telegram Bot API sendMessage 酬載結構。
    /// </summary>
    [TestMethod]
    public void BuildTelegramPayload_ContainsChatIdAndText()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.Telegram,
            "IP 已解除鎖定",
            "198.51.100.99",
            "Unlocked",
            "IDDS Community",
            "Lock period expired naturally.",
            TestTimestamp,
            telegramChatId: "-1001234567890");

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("-1001234567890", root.GetProperty("chat_id").GetString());
        Assert.AreEqual("HTML", root.GetProperty("parse_mode").GetString());
        Assert.IsTrue(root.GetProperty("text").GetString()!.Contains("198.51.100.99", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證 Generic JSON 通用酬載結構。
    /// </summary>
    [TestMethod]
    public void BuildGenericJsonPayload_ContainsStandardFields()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.GenericJson,
            "系統告警",
            "198.51.100.1",
            "New",
            "Core",
            "Service started.",
            TestTimestamp);

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.AreEqual("IDDS Community", root.GetProperty("system").GetString());
        Assert.AreEqual("198.51.100.1", root.GetProperty("ip_address").GetString());
    }
}
