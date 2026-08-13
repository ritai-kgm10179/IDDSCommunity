using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Service.Test;

[TestClass]
public sealed class RuntimeLogIsolationTest
{
    /// <summary>
    /// Verifies that the runtime logger forwards structured events through the injectable platform boundary.
    /// </summary>
    [TestMethod]
    public void WriteEntry_ForwardsToWindowsEventLogAbstraction()
    {
        FakeWindowsEventLog platformLog = new();
        global::IDDSCommunity.IntrusionDetection.Service.WindowsLogManager runtimeLog = new(platformLog);

        runtimeLog.WriteEntry("message", EventLogEntryType.Warning, 42, 7);

        Assert.AreEqual("message", platformLog.Text);
        Assert.AreEqual(EventLogEntryType.Warning, platformLog.Type);
        Assert.AreEqual(42, platformLog.EventId);
        Assert.AreEqual((short)7, platformLog.Category);
    }

    private sealed class FakeWindowsEventLog : IWindowsEventLog
    {
        internal string? Text { get; private set; }

        internal EventLogEntryType Type { get; private set; }

        internal int EventId { get; private set; }

        internal short Category { get; private set; }
        /// <summary>
        /// Records an event without accessing Windows.
        /// </summary>
        /// <param name="text">The event message.</param>
        /// <param name="type">The event severity.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="category">The event category.</param>
        public void WriteEntry(string text, EventLogEntryType type, int eventId, short category)
        {
            Text = text;
            Type = type;
            EventId = eventId;
            Category = category;
        }
        /// <summary>
        /// Releases no resources in the test double.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
