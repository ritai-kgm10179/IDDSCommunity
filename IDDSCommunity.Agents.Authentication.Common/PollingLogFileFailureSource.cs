using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed class PollingLogFileFailureSource : IAuthenticationEventSource
{
    private const int AnchorBytes = 32;
    private readonly Func<IEnumerable<string>> paths;
    private readonly Func<string, string, AuthenticationFailureEvent?> parser;
    private readonly Action<string>? resetParser;
    private readonly Dictionary<string, LogFileState> states = new(StringComparer.OrdinalIgnoreCase);
    private Timer? timer;
    private int reading;

    public PollingLogFileFailureSource(Func<IEnumerable<string>> paths, Func<string, AuthenticationFailureEvent?> parser)
        : this(paths, (_, line) => parser(line), null)
    {
    }

    public PollingLogFileFailureSource(
        Func<IEnumerable<string>> paths,
        Func<string, string, AuthenticationFailureEvent?> parser,
        Action<string>? resetParser = null)
    {
        this.paths = paths;
        this.parser = parser;
        this.resetParser = resetParser;
    }

    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    public event Action<Exception>? Error;
    public void Start() => timer ??= new Timer(_ => ReadAvailable(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    public void Pause() => timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    public void Resume() => timer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));
    public void Stop() { timer?.Dispose(); timer = null; }
    public void Dispose() => Stop();
    internal void ReadAvailableForTest() => ReadAvailable();

    private void ReadAvailable()
    {
        if (Interlocked.Exchange(ref reading, 1) != 0) return;
        try
        {
            foreach (string path in paths())
            {
                try { ReadFile(path); }
                catch (Exception exception) { Error?.Invoke(new IOException($"Could not process authentication log '{path}'.", exception)); }
            }
        }
        catch (Exception exception) { Error?.Invoke(exception); }
        finally { Volatile.Write(ref reading, 0); }
    }

    private void ReadFile(string path)
    {
        if (!File.Exists(path)) return;
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        DateTime creationTimeUtc = File.GetCreationTimeUtc(path);
        if (!states.TryGetValue(path, out LogFileState? state))
        {
            resetParser?.Invoke(path);
            PrimeMetadata(stream, path);
            long initialOffset = FindLastCompleteLineOffset(stream);
            states[path] = new LogFileState(initialOffset, creationTimeUtc, ReadAnchor(stream, initialOffset));
            return;
        }
        bool sameFile = state.CreationTimeUtc == creationTimeUtc && state.Offset <= stream.Length && AnchorMatches(stream, state);
        long offset = sameFile ? state.Offset : 0;
        if (!sameFile)
        {
            resetParser?.Invoke(path);
            PrimeMetadata(stream, path);
        }
        stream.Position = offset;
        long committedOffset = offset;
        byte[] readBuffer = new byte[4096];
        using MemoryStream lineBuffer = new();
        int bytesRead;
        while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            long bufferStart = stream.Position - bytesRead;
            for (int index = 0; index < bytesRead; index++)
            {
                if (readBuffer[index] != (byte)'\n')
                {
                    lineBuffer.WriteByte(readBuffer[index]);
                    continue;
                }
                ProcessCompleteLine(path, lineBuffer);
                lineBuffer.SetLength(0);
                committedOffset = bufferStart + index + 1;
            }
        }
        state.Offset = committedOffset;
        state.CreationTimeUtc = creationTimeUtc;
        state.Anchor = ReadAnchor(stream, committedOffset);
    }

    private void ProcessCompleteLine(string path, MemoryStream buffer)
    {
        byte[] bytes = buffer.ToArray();
        int start = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
        int length = bytes.Length - start;
        if (length > 0 && bytes[start + length - 1] == (byte)'\r') length--;
        string line = new UTF8Encoding(false, true).GetString(bytes, start, length);
        AuthenticationFailureEvent? failure = parser(path, line);
        if (failure is not null) EventReceived?.Invoke(this, failure);
    }

    private void PrimeMetadata(FileStream stream, string path)
    {
        stream.Position = 0;
        using StreamReader reader = new(stream, new UTF8Encoding(false, true), true, 1024, true);
        while (reader.ReadLine() is string line)
        {
            if (!line.StartsWith('#')) break;
            _ = parser(path, line);
        }
        stream.Position = 0;
    }

    private static bool AnchorMatches(FileStream stream, LogFileState state) =>
        ReadAnchor(stream, state.Offset).AsSpan().SequenceEqual(state.Anchor);

    private static long FindLastCompleteLineOffset(FileStream stream)
    {
        byte[] buffer = new byte[4096];
        long end = stream.Length;
        while (end > 0)
        {
            int length = (int)Math.Min(end, buffer.Length);
            long start = end - length;
            stream.Position = start;
            stream.ReadExactly(buffer.AsSpan(0, length));
            for (int index = length - 1; index >= 0; index--)
                if (buffer[index] == (byte)'\n') return start + index + 1;
            end = start;
        }
        return 0;
    }

    private static byte[] ReadAnchor(FileStream stream, long offset)
    {
        int length = (int)Math.Min(offset, AnchorBytes);
        if (length == 0) return [];
        byte[] anchor = new byte[length];
        long originalPosition = stream.Position;
        stream.Position = offset - length;
        stream.ReadExactly(anchor);
        stream.Position = originalPosition;
        return anchor;
    }

    private sealed class LogFileState(long offset, DateTime creationTimeUtc, byte[] anchor)
    {
        internal long Offset { get; set; } = offset;
        internal DateTime CreationTimeUtc { get; set; } = creationTimeUtc;
        internal byte[] Anchor { get; set; } = anchor;
    }
}
