using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class LogSanitizerTest
{
    /// <summary>
    /// Verifies that control characters cannot forge additional Event Log lines.
    /// </summary>
    [TestMethod]
    public void Sanitize_RemovesControlCharacters()
    {
        Assert.AreEqual("first  second third", LogSanitizer.Sanitize("first\r\nsecond\0third"));
    }
    /// <summary>
    /// Verifies that common credential fields are redacted before logging.
    /// </summary>
    [TestMethod]
    public void Sanitize_RedactsSecrets()
    {
        string result = LogSanitizer.Sanitize("password=hunter2 token:abc123 status=failed");

        Assert.IsFalse(result.Contains("hunter2", System.StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("abc123", System.StringComparison.Ordinal));
        StringAssert.Contains(result, "password=[REDACTED]");
        StringAssert.Contains(result, "token=[REDACTED]");
    }
    /// <summary>
    /// Verifies that attacker-controlled diagnostic messages have a fixed maximum size.
    /// </summary>
    [TestMethod]
    public void Sanitize_TruncatesOversizedMessage()
    {
        Assert.AreEqual(4096, LogSanitizer.Sanitize(new string('x', 5000)).Length);
    }
}
