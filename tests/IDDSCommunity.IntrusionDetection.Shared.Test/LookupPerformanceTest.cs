using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using IDDSCommunity.IntrusionDetection.Shared.ThreatIntelligence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

/// <summary>
/// 以相同資料比較線性參考實作與快照索引；時間只供診斷，不作不穩定門檻斷言。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class LookupPerformanceTest
{
    /// <summary>
    /// 取得或設定測試輸出環境。
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// 驗證索引與線性參考結果一致並記錄配置量、總時間及尾端延遲。
    /// </summary>
    [TestMethod]
    [TestCategory("Performance")]
    public void CompareLinearAndIndexedLookups()
    {
        IPNetwork[] networks = Enumerable.Range(0, 8192).Select(i => IPNetwork.Parse($"11.{i / 256}.{i % 256}.0/24")).ToArray();
        IPAddress[] addresses = Enumerable.Range(0, 2048).Select(i => new IPAddress(new byte[] { (byte)(i % 2 == 0 ? 11 : 12), (byte)((i * 17 % 8192) / 256), (byte)(i * 17 % 256), 123 })).ToArray();
        StringBuilder csv = new();
        foreach (IPNetwork network in networks) csv.Append(network).Append(",US,United States\n");
        try
        {
            Stopwatch construction = Stopwatch.StartNew();
            GeoIpLookupService.LoadFromCsv(csv.ToString());
            BogonIpFilter.UpdateDynamicBogons(networks);
            construction.Stop();
            Func<IPAddress, bool> linear = ip => { foreach (IPNetwork network in networks) if (network.Contains(ip)) return true; return false; };
            Func<IPAddress, bool> geo = ip => GeoIpLookupService.TryLookup(ip, out _, out _);
            Func<IPAddress, bool> bogon = BogonIpFilter.IsBogonOrReserved;
            foreach (IPAddress ip in addresses)
            {
                Assert.AreEqual(linear(ip), geo(ip));
                Assert.AreEqual(linear(ip), bogon(ip));
            }
            TestContext.WriteLine($"Index construction: {construction.Elapsed.TotalMilliseconds:F3} ms; records=8192; queries=2048");
            Measure("Linear reference", linear, addresses);
            Measure("GeoIP index", geo, addresses);
            Measure("Bogon index", bogon, addresses);
        }
        finally { GeoIpLookupService.Clear(); BogonIpFilter.ClearDynamicBogons(); }
    }

    private void Measure(string name, Func<IPAddress, bool> lookup, IPAddress[] addresses)
    {
        long[] samples = new long[addresses.Length];
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long totalStart = Stopwatch.GetTimestamp();
        int hits = 0;
        for (int i = 0; i < addresses.Length; i++)
        {
            long start = Stopwatch.GetTimestamp();
            if (lookup(addresses[i])) hits++;
            samples[i] = Stopwatch.GetTimestamp() - start;
        }
        double milliseconds = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
        long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Array.Sort(samples);
        double p95 = samples[(int)(samples.Length * .95)] * 1000000.0 / Stopwatch.Frequency;
        double p99 = samples[(int)(samples.Length * .99)] * 1000000.0 / Stopwatch.Frequency;
        TestContext.WriteLine($"{name}: total={milliseconds:F3} ms; p95={p95:F3} us; p99={p99:F3} us; allocated={bytes} B; hits={hits}");
        Assert.AreEqual(1024, hits);
    }
}