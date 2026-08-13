using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.Agents.MailServer.Test;

[TestClass]
public sealed class SmtpTest
{
    [TestMethod]
    public void AuthenticationFailure_UsesRfc4954ReplyCode()
    {
        byte[] invalidCredentials = Encoding.ASCII.GetBytes("535 5.7.8 Authentication credentials invalid\r\n");
        byte[] unsupportedParameter = Encoding.ASCII.GetBytes("504 5.5.4 Unrecognized authentication type\r\n");

        AppLayerSmtp failure = new(invalidCredentials, invalidCredentials.Length);
        AppLayerSmtp notFailure = new(unsupportedParameter, unsupportedParameter.Length);

        Assert.AreEqual(AppLayerSmtp.SMTP_REPLY_CODE_LOGIN_DENIED, failure.SmtpReplyCode);
        Assert.AreNotEqual(AppLayerSmtp.SMTP_REPLY_CODE_LOGIN_DENIED, notFailure.SmtpReplyCode);
    }
}
