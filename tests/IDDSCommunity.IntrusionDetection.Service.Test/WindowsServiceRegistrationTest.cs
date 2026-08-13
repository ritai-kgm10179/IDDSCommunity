using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

/// <summary>
/// 驗證 Windows 服務註冊名稱與安裝器使用的 SCM 鍵一致。
/// </summary>
[TestClass]
public sealed class WindowsServiceRegistrationTest
{
    /// <summary>
    /// 驗證服務存留期不會誤用僅供顯示的名稱。
    /// </summary>
    [TestMethod]
    public void WindowsServiceLifetime_UsesScmServiceName()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddIDDSCommunityWindowsService();
        using ServiceProvider provider = services.BuildServiceProvider();

        WindowsServiceLifetimeOptions options = provider.GetRequiredService<IOptions<WindowsServiceLifetimeOptions>>().Value;

        Assert.AreEqual(Globals.WINDOWS_SERVICE_NAME, options.ServiceName);
        Assert.AreNotEqual(Globals.WINDOWS_SERVICE_DISPLAY_NAME, options.ServiceName);
    }
}
