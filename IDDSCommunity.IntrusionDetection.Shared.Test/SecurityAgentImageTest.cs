using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class SecurityAgentImageTest
{
    /// <summary>
    /// 驗證每次讀取自訂 Agent 圖示時，都會取得可獨立釋放的圖片執行個體。
    /// </summary>
    [TestMethod]
    public void Icon_CustomImage_ReturnsIndependentOwnedCopies()
    {
        using Bitmap source = new(4, 4);
        source.SetPixel(0, 0, Color.Teal);
        SecurityAgent agent = new() { Icon = source };

        using Image first = agent.Icon;
        using Image second = agent.Icon;

        Assert.AreNotSame(first, second);
        first.Dispose();
        Assert.AreEqual(4, second.Width);
        Assert.AreEqual(Color.Teal.ToArgb(), ((Bitmap)second).GetPixel(0, 0).ToArgb());
    }

    /// <summary>
    /// 驗證預設 Agent 圖示不會直接公開共用資源執行個體。
    /// </summary>
    [TestMethod]
    public void Icon_DefaultImage_ReturnsIndependentOwnedCopies()
    {
        SecurityAgent agent = new();

        using Image first = agent.Icon;
        using Image second = agent.Icon;

        Assert.AreNotSame(first, second);
        Assert.AreEqual(first.Size, second.Size);
    }
}
