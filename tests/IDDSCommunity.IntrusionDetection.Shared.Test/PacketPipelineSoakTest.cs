using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class PacketPipelineSoakTest
{
    /// <summary>
    /// Verifies sustained bounded dispatch without packet reordering or unbounded queue growth.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async Task Dispatcher_SustainedLoad_RemainsBoundedAndCompletes()
    {
        const int packetCount = 10000;
        int lastValue = -1;
        bool ordered = true;
        BoundedPacketDispatcher dispatcher = new(256, packet =>
        {
            int value = BitConverter.ToInt32(packet.Packet);
            if (value <= lastValue)
                ordered = false;
            lastValue = value;
        });
        int accepted = 0;
        for (int value = 0; value < packetCount; value++)
        {
            if (dispatcher.TryEnqueue(BitConverter.GetBytes(value)))
                accepted++;
        }
        dispatcher.Complete();
        await dispatcher.Completion.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

        Assert.AreEqual(packetCount, dispatcher.ReceivedCount);
        Assert.AreEqual(accepted, dispatcher.DispatchedCount);
        Assert.AreEqual(packetCount - accepted, dispatcher.DroppedCount);
        Assert.IsTrue(ordered);
        Assert.IsGreaterThanOrEqualTo(0, lastValue);
    }
}
