using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cyberarms.Agents.MailServer.Test;

/// <summary>
/// Verifies IMAP authentication correlation and TLS transition behavior.
/// </summary>
[TestClass]
public sealed class ImapSessionInspectorTest
{
    /// <summary>
    /// Verifies that only a tagged NO for the pending LOGIN command is reported.
    /// </summary>
    [TestMethod]
    public void LoginTaggedNoIsReportedAsAuthenticationFailure()
    {
        ImapSessionInspector inspector = new();
        inspector.ProcessClientData(Encoding.ASCII.GetBytes("A001 LOGIN user secret\r\n"));

        bool failed = inspector.ProcessServerData(Encoding.ASCII.GetBytes("A001 NO [AUTHENTICATIONFAILED] rejected\r\n"));

        Assert.IsTrue(failed);
    }

    /// <summary>
    /// Verifies that protocol errors are not misclassified as rejected credentials.
    /// </summary>
    [TestMethod]
    public void TaggedBadIsNotReportedAsAuthenticationFailure()
    {
        ImapSessionInspector inspector = new();
        inspector.ProcessClientData(Encoding.ASCII.GetBytes("A002 LOGIN\r\n"));

        bool failed = inspector.ProcessServerData(Encoding.ASCII.GetBytes("A002 BAD invalid command\r\n"));

        Assert.IsFalse(failed);
    }

    /// <summary>
    /// Verifies that successful STARTTLS permanently disables application-data parsing for the session.
    /// </summary>
    [TestMethod]
    public void StartTlsSuccessStopsFurtherParsing()
    {
        ImapSessionInspector inspector = new();
        inspector.ProcessClientData(Encoding.ASCII.GetBytes("A003 STARTTLS\r\n"));
        Assert.IsFalse(inspector.ProcessServerData(Encoding.ASCII.GetBytes("A003 OK Begin TLS\r\n")));
        Assert.IsTrue(inspector.IsEncrypted);

        inspector.ProcessClientData(Encoding.ASCII.GetBytes("A004 LOGIN user secret\r\n"));
        Assert.IsFalse(inspector.ProcessServerData(Encoding.ASCII.GetBytes("A004 NO rejected\r\n")));
    }

    /// <summary>
    /// Verifies that fragmented TCP payloads are reassembled before command correlation.
    /// </summary>
    [TestMethod]
    public void FragmentedAuthenticateResponseIsCorrelated()
    {
        ImapSessionInspector inspector = new();
        inspector.ProcessClientData(Encoding.ASCII.GetBytes("B001 AUTH"));
        inspector.ProcessClientData(Encoding.ASCII.GetBytes("ENTICATE PLAIN\r\n"));

        Assert.IsFalse(inspector.ProcessServerData(Encoding.ASCII.GetBytes("B001 N")));
        Assert.IsTrue(inspector.ProcessServerData(Encoding.ASCII.GetBytes("O rejected\r\n")));
    }
}
