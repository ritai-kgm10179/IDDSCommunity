using System;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using IDDSCommunity.IntrusionDetection.Shared;
using IDDSCommunity.IntrusionDetection.Api.Plugin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class SecurityEventPipelineTest
{
    private string testDirectory = null!;
    private Database database = null!;

    /// <summary>
    /// Creates an isolated durable inbox for each pipeline test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), "IDDSCommunity.SecurityEventPipelineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        database = new Database();
        database.Configure(testDirectory, "pipeline.db");
    }

    /// <summary>
    /// Releases the isolated inbox database.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        database.Close();
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);
    }
    /// <summary>
    /// Verifies accepted events are processed sequentially and drained during completion.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task Complete_AcceptedEvents_DrainsInOrderAsync()
    {
        ConcurrentQueue<string> processed = new();
        SecurityEventPipeline pipeline = CreatePipeline(8, (_, args) => processed.Enqueue(args.IpAddress), _ => Assert.Fail("Unexpected failure."));
        NotificationEventArgs first = CreateEvent("192.0.2.1");

        Assert.IsTrue(pipeline.TryPublish(this, first));
        first.IpAddress = "203.0.113.99";
        Assert.IsTrue(pipeline.TryPublish(this, CreateEvent("192.0.2.2")));
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        CollectionAssert.AreEqual(new[] { "192.0.2.1", "192.0.2.2" }, processed.ToArray());
        Assert.AreEqual(0, pipeline.QueueDepth);
        Assert.IsFalse(pipeline.TryPublish(this, CreateEvent("192.0.2.3")));
    }

    /// <summary>
    /// Verifies saturation rejects producers without blocking and one consumer failure does not stop later work.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task TryPublish_SaturationAndConsumerFailure_AreIsolatedAsync()
    {
        using ManualResetEventSlim entered = new(false);
        using ManualResetEventSlim release = new(false);
        int processed = 0;
        int failures = 0;
        SecurityEventPipeline pipeline = CreatePipeline(1, (_, args) =>
        {
            Interlocked.Increment(ref processed);
            if (args.IpAddress == "192.0.2.1")
            {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5));
                throw new InvalidOperationException("expected");
            }
        }, _ => Interlocked.Increment(ref failures));

        Assert.IsTrue(pipeline.TryPublish(this, CreateEvent("192.0.2.1")));
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(pipeline.TryPublish(this, CreateEvent("192.0.2.2")));
        Assert.IsFalse(pipeline.TryPublish(this, CreateEvent("192.0.2.3")));
        release.Set();
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(2, processed);
        Assert.AreEqual(1, failures);
    }

    /// <summary>
    /// Verifies the lossless publishing path applies backpressure until bounded capacity becomes available.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task Publish_SaturatedQueue_BackpressuresProducerAsync()
    {
        using ManualResetEventSlim entered = new(false);
        using ManualResetEventSlim release = new(false);
        SecurityEventPipeline pipeline = CreatePipeline(1, (_, args) =>
        {
            if (args.IpAddress == "192.0.2.1")
            {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5));
            }
        }, _ => Assert.Fail("Unexpected failure."));

        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.1")));
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(pipeline.Publish(this, CreateEvent("192.0.2.2")));
        System.Threading.Tasks.Task<bool> blockedProducer = System.Threading.Tasks.Task.Run(() => pipeline.Publish(this, CreateEvent("192.0.2.3")));
        await System.Threading.Tasks.Task.Delay(100);
        Assert.IsFalse(blockedProducer.IsCompleted);

        release.Set();
        Assert.IsTrue(await blockedProducer.WaitAsync(TimeSpan.FromSeconds(5)));
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies an event persisted before interruption is replayed and marked completed by a new pipeline.
    /// </summary>
    /// <returns>表示非同步工作完成的 Task。</returns>
    [TestMethod]
    public async System.Threading.Tasks.Task RecoverPending_PersistedEvent_ReplaysAfterRestartAsync()
    {
        SecurityEventInbox inbox = new(database, TimeProvider.System);
        inbox.Add("RecoveredAgent", CreateEvent("192.0.2.44"));
        string? processedAddress = null;
        SecurityEventPipeline pipeline = new(
            4,
            (_, args) => processedAddress = args.IpAddress,
            _ => Assert.Fail("Unexpected failure."),
            inbox,
            agentName => agentName == "RecoveredAgent" ? this : null);

        pipeline.RecoverPending(10);
        pipeline.Complete();
        await pipeline.Completion.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual("192.0.2.44", processedAddress);
        Assert.AreEqual(0, inbox.CountUnfinished());
    }

    /// <summary>
    /// Creates one test detection event.
    /// </summary>
    /// <param name="address">The source address.</param>
    /// <returns>傳回 mutable test event 的結果。</returns>
    private static NotificationEventArgs CreateEvent(string address) => new()
    {
        CreateDate = DateTime.UtcNow,
        EventId = 1,
        EventMessage = "test",
        IpAddress = address
    };

    private SecurityEventPipeline CreatePipeline(int capacity, Action<object, INotificationEventArgs> process, Action<Exception> reportFailure) =>
        new(capacity, process, reportFailure, new SecurityEventInbox(database, TimeProvider.System), _ => this);
}
