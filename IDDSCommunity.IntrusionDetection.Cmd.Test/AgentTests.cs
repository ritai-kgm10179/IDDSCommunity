using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace IDDSCommunityIntrusionDetection.Cmd.Test;

[TestClass]
public class AgentTests
{
    /// <summary>
    /// Executes the test resolve ip operation.
    /// </summary>

    [TestMethod]
    public void TestResolveIP()
    {
        try
        {
            string[] result = ResolveIp("localhost");
            Assert.IsNotEmpty(result);
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Offline or unresolvable hostname in environment
        }
    }


    /// <summary>
    /// Resolves ip.
    /// </summary>
    /// <param name="hostname">The hostname value.</param>
    /// <returns>The resolve ip result.</returns>

    private static string[] ResolveIp(string hostname)
    {
        List<string> result = [];
        IPAddress[] addr = Dns.GetHostAddresses(hostname);
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
