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

    /// <summary>
    /// 驗證 Microsoft Teams 酬載在提供 Management API 設定時，正確包含一鍵處置按鈕 (actions)。
    /// </summary>
    [TestMethod]
    public void BuildTeamsPayload_WithManagementApi_IncludesActionButtons()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.MicrosoftTeams,
            "硬封鎖已套用",
            "198.51.100.42",
            "Hard lock",
            "Windows RDP Agent",
            "Test alert",
            TestTimestamp,
            managementApiBaseUrl: "https://idds.local:8443",
            managementApiKey: "test-secret-key-12345");

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("attachments")[0].GetProperty("content");

        Assert.IsTrue(content.TryGetProperty("actions", out var actions));
        Assert.AreEqual(2, actions.GetArrayLength());
        Assert.AreEqual("Action.OpenUrl", actions[0].GetProperty("type").GetString());
        Assert.IsTrue(actions[0].GetProperty("url").GetString()!.Contains("/api/v1/actions/unblock"));
        Assert.IsTrue(actions[1].GetProperty("url").GetString()!.Contains("/api/v1/actions/block"));
    }

    /// <summary>
    /// 驗證 Slack 酬載在提供 Management API 設定時，正確包含一鍵處置按鈕 (actions block)。
    /// </summary>
    [TestMethod]
    public void BuildSlackPayload_WithManagementApi_IncludesActionButtons()
    {
        string json = WebhookPayloadBuilder.BuildPayload(
            WebhookPlatform.Slack,
            "硬封鎖已套用",
            "198.51.100.42",
            "Hard lock",
            "Windows RDP Agent",
            "Test alert",
            TestTimestamp,
            managementApiBaseUrl: "https://idds.local:8443",
            managementApiKey: "test-secret-key-12345");

        Assert.IsNotNull(json);
        using var doc = JsonDocument.Parse(json);
        var blocks = doc.RootElement.GetProperty("blocks");

        bool foundActionBlock = false;
        foreach (var block in blocks.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var typeElem) && typeElem.GetString() == "actions")
            {
                foundActionBlock = true;
                var elements = block.GetProperty("elements");
                Assert.AreEqual(2, elements.GetArrayLength());
                Assert.AreEqual("button", elements[0].GetProperty("type").GetString());
                Assert.IsTrue(elements[0].GetProperty("url").GetString()!.Contains("/api/v1/actions/unblock"));
                Assert.IsTrue(elements[1].GetProperty("url").GetString()!.Contains("/api/v1/actions/block"));
            }
        }
        Assert.IsTrue(foundActionBlock);
    }

    /// <summary>
    /// 驗證 Slack mrkdwn 區塊確實轉義 &、< 與 > 字元，防範 Markdown 注入與釣魚連結 (CWE-116)。
    /// </summary>
    [TestMethod]
    public void BuildSlackPayload_EscapesMrkdwnSpecialChars()
    {
        string maliciousDetails = "Attacker payload: <http://evil.corp|Click here> & <!channel>";
        string maliciousAgent = "Agent <admin>";

        string json = WebhookPayloadBuilder.BuildSlackPayload(
            "AttackDetected",
            "198.51.100.22",
            "Hard lock",
            maliciousAgent,
            maliciousDetails,
            TestTimestamp);

        Assert.IsNotNull(json);
        Assert.IsFalse(json.Contains("<http://evil.corp", StringComparison.Ordinal));
        using var doc = JsonDocument.Parse(json);
        string detailsText = doc.RootElement.GetProperty("blocks")[2].GetProperty("text").GetProperty("text").GetString()!;
        Assert.IsTrue(detailsText.Contains("&lt;http://evil.corp|Click here&gt; &amp; &lt;!channel&gt;", StringComparison.Ordinal));
        var fields = doc.RootElement.GetProperty("blocks")[1].GetProperty("fields");
        string agentField = fields[2].GetProperty("text").GetString()!;
        Assert.IsTrue(agentField.Contains("Agent &lt;admin&gt;", StringComparison.Ordinal));
    }

    /// <summary>
    /// 驗證過長之事件詳細內容在 Discord 與 Teams 酬載中安全截斷，防範第三方端點拒絕請求。
    /// </summary>
    [TestMethod]
    public void BuildPayload_TruncatesExcessivelyLongDetails()
    {
        string extremelyLongDetails = new('A', 10000);

        string discordJson = WebhookPayloadBuilder.BuildDiscordPayload(
            "Long Alert", "198.51.100.22", "Hard lock", "TestAgent", extremelyLongDetails, TestTimestamp);
        using (var doc = JsonDocument.Parse(discordJson))
        {
            string desc = doc.RootElement.GetProperty("embeds")[0].GetProperty("description").GetString()!;
            Assert.IsTrue(desc.Length <= 4005);
            Assert.IsTrue(desc.EndsWith("...", StringComparison.Ordinal));
        }

        string teamsJson = WebhookPayloadBuilder.BuildTeamsPayload(
            "Long Alert", "198.51.100.22", "Hard lock", "TestAgent", extremelyLongDetails, TestTimestamp);
        using (var doc = JsonDocument.Parse(teamsJson))
        {
            var body = doc.RootElement.GetProperty("attachments")[0].GetProperty("content").GetProperty("body");
            string text = body[3].GetProperty("text").GetString()!;
            Assert.IsTrue(text.Length <= 4005);
            Assert.IsTrue(text.EndsWith("...", StringComparison.Ordinal));
        }
    }
}
