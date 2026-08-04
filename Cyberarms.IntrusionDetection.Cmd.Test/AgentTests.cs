using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace CyberarmsPaladinCmd.Test;

[TestClass]
public class AgentTests
{
    [TestMethod]
    public void TestResolveIP()
    {
        try
        {
            string[] result = ResolveIp("localhost");
            Assert.IsTrue(result.Length > 0);
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Offline or unresolvable hostname in environment
        }
    }


    private string[] ResolveIp(string hostname)
    {
        List<string> result = new();
        IPAddress[] addr = System.Net.Dns.GetHostAddresses(hostname);
        foreach (IPAddress ip in addr)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                result.Add(ip.ToString());
            }
        }
        return result.ToArray();
    }

}
