using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class ThreatFeedParserTest
{
    [TestMethod]
    public void ParseFeed_PlainTextWithCommentsAndCidr_ExtractsValidPublicIps()
    {
        string rawContent = @"
# Spamhaus DROP Advisory List
; Format: Network/Mask ; SBL Ref
198.51.100.0/24 ; Test-Net (Bogon, should be filtered)
140.112.1.1 # Valid Public IP
10.0.0.1 ; RFC 1918 (Bogon, should be filtered)
// Another comment
1.1.1.1
";

        List<string> result = ThreatFeedParser.ParseFeed(rawContent, ThreatFeedFormat.PlainTextLines);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains("140.112.1.1"));
        Assert.IsTrue(result.Contains("1.1.1.1"));
        Assert.IsFalse(result.Contains("10.0.0.1"));
    }

    [TestMethod]
    public void ParseFeed_IPsumTabDelimited_FiltersByMinimumLevelAndBogon()
    {
        string rawContent = @"
# IPsum list generated 2026-08-31
# IP	Level
1.1.1.1	2
8.8.8.8	3
140.112.8.8	5
192.168.1.1	8
";

        // minLevel = 3
        List<string> result = ThreatFeedParser.ParseFeed(rawContent, ThreatFeedFormat.IPsumTabDelimited, minConfidenceOrLevel: 3);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains("8.8.8.8"));
        Assert.IsTrue(result.Contains("140.112.8.8"));
        Assert.IsFalse(result.Contains("1.1.1.1")); // Level 2 < 3
        Assert.IsFalse(result.Contains("192.168.1.1")); // RFC 1918 Bogon
    }

    [TestMethod]
    public void ParseFeed_AbuseIpDbJson_FiltersByConfidenceAndBogon()
    {
        string json = @"
{
  ""data"": [
    {
      ""ipAddress"": ""140.112.2.2"",
      ""abuseConfidenceScore"": 100
    },
    {
      ""ipAddress"": ""8.8.4.4"",
      ""abuseConfidenceScore"": 50
    },
    {
      ""ipAddress"": ""10.1.2.3"",
      ""abuseConfidenceScore"": 99
    }
  ]
}";

        // minConfidence = 90
        List<string> result = ThreatFeedParser.ParseFeed(json, ThreatFeedFormat.AbuseIpDbJson, minConfidenceOrLevel: 90);

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.Contains("140.112.2.2"));
        Assert.IsFalse(result.Contains("8.8.4.4")); // score 50 < 90
        Assert.IsFalse(result.Contains("10.1.2.3")); // RFC 1918 Bogon
    }
}
