using System;
using System.IO;
using System.Security.Cryptography;
using IDDSCommunity.IntrusionDetection.Admin;
using IDDSCommunity.IntrusionDetection.Shared.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class PanelConfigurationTransferTest
{
    /// <summary>
    /// 驗證匯出位置無法寫入時會提供可操作的錯誤訊息。
    /// </summary>
    [TestMethod]
    public void GetFailureMessage_UnauthorizedLocation_ProvidesActionableGuidance()
    {
        string message = PanelConfigurationTransfer.GetFailureMessage(new UnauthorizedAccessException(), false);

        Assert.AreEqual(
            Strings.Get("The selected export location is not writable. Choose a folder owned by your user account."),
            message);
    }

    /// <summary>
    /// 驗證 SMTP 密碼無法解密時會指示重新儲存密碼或排除機密。
    /// </summary>
    [TestMethod]
    public void GetFailureMessage_SecretDecryptionFailure_ProvidesRecoveryOptions()
    {
        string message = PanelConfigurationTransfer.GetFailureMessage(new CryptographicException(), true);

        Assert.AreEqual(
            Strings.Get("The stored SMTP password cannot be decrypted. Save the SMTP password again, or export without including secrets."),
            message);
    }

    /// <summary>
    /// 驗證一般例外狀況會保留例外型別與原始診斷訊息。
    /// </summary>
    [TestMethod]
    public void GetFailureMessage_UnexpectedFailure_PreservesDiagnosticMessage()
    {
        string message = PanelConfigurationTransfer.GetFailureMessage(new InvalidOperationException("diagnostic-detail"), false);

        StringAssert.Contains(message, nameof(InvalidOperationException));
        StringAssert.Contains(message, "diagnostic-detail");
    }

    /// <summary>
    /// 驗證設定驗證失敗時不會再次隱藏原始欄位診斷。
    /// </summary>
    [TestMethod]
    public void GetFailureMessage_InvalidData_PreservesFieldDiagnostic()
    {
        string message = PanelConfigurationTransfer.GetFailureMessage(
            new InvalidDataException("SmtpPort must be between 1 and 65535; actual value: 0."), false);

        StringAssert.Contains(message, "SmtpPort");
        StringAssert.Contains(message, "actual value: 0");
    }
}
