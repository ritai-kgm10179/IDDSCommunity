using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.MailServer.Test;
/// <summary>
/// 驗證 IMAP 驗證關聯與 TLS 轉移行為。
/// </summary>
[TestClass]
public sealed class ImapSessionInspectorTest
{
    /// <summary>
    /// 驗證僅傳回未處理 LOGIN 命令之標記 NO 狀態。
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
    /// 驗證協定錯誤不會被誤分類為拒絕憑證。
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
    /// 驗證成功的 STARTTLS 會永久停用該會話的應用程式資料解析。
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
    /// 驗證分割的 TCP 負載會在命令關聯之前重組。
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
