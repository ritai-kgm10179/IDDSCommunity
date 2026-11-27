using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 以定時輪詢方式監看一組文字記錄檔，並針對每一完整換行紀錄嘗試解析出驗證失敗事件；
/// 以位元組位置與檔案錨點正確處理半行、截斷與日誌輪替情境。
/// </summary>
public sealed class PollingLogFileFailureSource : IAuthenticationEventSource
{
    private const int AnchorBytes = 32;
    private const int MaxLineLength = 65536;
    private static readonly Encoding SafeUtf8 = new UTF8Encoding(false, false);
    private readonly Func<IEnumerable<string>> paths;
    private readonly Func<string, string, AuthenticationFailureEvent?> parser;
    private readonly Action<string>? resetParser;
    private readonly Dictionary<string, LogFileState> states = new(StringComparer.OrdinalIgnoreCase);
    private Timer? timer;
    private int reading;

    /// <summary>
    /// 初始化 <see cref="PollingLogFileFailureSource"/> 類別的新執行個體。
    /// </summary>
    /// <param name="paths">用於列舉欲監看之記錄檔路徑的委派。</param>
    /// <param name="parser">將單行文字解析為驗證失敗事件的委派。</param>
    public PollingLogFileFailureSource(Func<IEnumerable<string>> paths, Func<string, AuthenticationFailureEvent?> parser)
        : this(paths, (_, line) => parser(line), null)
    {
    }

    /// <summary>
    /// 初始化 <see cref="PollingLogFileFailureSource"/> 類別的新執行個體。
    /// </summary>
    /// <param name="paths">用於列舉欲監看之記錄檔路徑的委派。</param>
    /// <param name="parser">將檔案路徑與單行文字解析為驗證失敗事件的委派。</param>
    /// <param name="resetParser">偵測到檔案為新檔或已輪替時呼叫之選用重設委派。</param>
    public PollingLogFileFailureSource(
        Func<IEnumerable<string>> paths,
        Func<string, string, AuthenticationFailureEvent?> parser,
        Action<string>? resetParser = null)
    {
        this.paths = paths;
        this.parser = parser;
        this.resetParser = resetParser;
    }

    /// <summary>
    /// 當解析出驗證失敗事件時引發。
    /// </summary>
    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    /// <summary>
    /// 當讀取或解析記錄檔發生例外狀況時引發。
    /// </summary>
    public event Action<Exception>? Error;
    /// <summary>
    /// 啟動定時輪詢計時器。
    /// </summary>
    public void Start() => timer ??= new Timer(_ => ReadAvailable(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    /// <summary>
    /// 暫停定時輪詢。
    /// </summary>
    public void Pause() => timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    /// <summary>
    /// 從暫停狀態恢復定時輪詢。
    /// </summary>
    public void Resume() => timer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));
    /// <summary>
    /// 停止並釋放定時輪詢計時器。
    /// </summary>
    public void Stop() { timer?.Dispose(); timer = null; }
    /// <summary>
    /// 停止定時輪詢並釋放相關資源。
    /// </summary>
    public void Dispose() => Stop();
    internal void ReadAvailableForTest() => ReadAvailable();

    private void ReadAvailable()
    {
        if (Interlocked.Exchange(ref reading, 1) != 0) return;
        try
        {
            List<string> activePaths = [];
            foreach (string path in paths())
            {
                activePaths.Add(path);
                try { ReadFile(path); }
                catch (Exception exception) { Error?.Invoke(new IOException($"Could not process authentication log '{path}'.", exception)); }
            }

            if (states.Count > activePaths.Count)
            {
                HashSet<string> activeSet = new(activePaths, StringComparer.OrdinalIgnoreCase);
                List<string>? stalePaths = null;
                foreach (string tracked in states.Keys)
                {
                    if (!activeSet.Contains(tracked))
                    {
                        stalePaths ??= [];
                        stalePaths.Add(tracked);
                    }
                }
                if (stalePaths is not null)
                {
                    foreach (string stale in stalePaths)
                    {
                        states.Remove(stale);
                        resetParser?.Invoke(stale);
                    }
                }
            }
        }
        catch (Exception exception) { Error?.Invoke(exception); }
        finally { Volatile.Write(ref reading, 0); }
    }

    private void ReadFile(string path)
    {
        FileInfo fileInfo = new(path);
        if (!fileInfo.Exists) return;
        DateTime creationTimeUtc = fileInfo.CreationTimeUtc;
        DateTime lastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
        long currentLength = fileInfo.Length;

        if (states.TryGetValue(path, out LogFileState? state) &&
            state.Offset == currentLength &&
            state.CreationTimeUtc == creationTimeUtc &&
            state.LastWriteTimeUtc == lastWriteTimeUtc &&
            state.LastObservedLength == currentLength)
        {
            return;
        }

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        creationTimeUtc = File.GetCreationTimeUtc(path);
        if (state is null)
        {
            resetParser?.Invoke(path);
            PrimeMetadata(stream, path);
            long initialOffset = FindLastCompleteLineOffset(stream);
            states[path] = new LogFileState(initialOffset, creationTimeUtc, ReadAnchor(stream, initialOffset), stream.Length, lastWriteTimeUtc);
            return;
        }
        if (state.Offset > stream.Length)
        {
            state.Offset = 0;
            state.Anchor = [];
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
        byte[] readBuffer = ArrayPool<byte>.Shared.Rent(4096);
        using MemoryStream lineBuffer = new();
        try
        {
            int bytesRead;
            while ((bytesRead = stream.Read(readBuffer, 0, 4096)) > 0)
            {
                long bufferStart = stream.Position - bytesRead;
                int cursor = 0;
                while (cursor < bytesRead)
                {
                    ReadOnlySpan<byte> remaining = readBuffer.AsSpan(cursor, bytesRead - cursor);
                    int newlineOffset = remaining.IndexOf((byte)'\n');
                    if (newlineOffset < 0)
                    {
                        int available = MaxLineLength - (int)lineBuffer.Length;
                        if (available > 0)
                        {
                            lineBuffer.Write(remaining[..Math.Min(available, remaining.Length)]);
                        }
                        break;
                    }

                    if (newlineOffset > 0)
                    {
                        int available = MaxLineLength - (int)lineBuffer.Length;
                        if (available > 0)
                        {
                            lineBuffer.Write(remaining[..Math.Min(available, newlineOffset)]);
                        }
                    }

                    ProcessCompleteLine(path, lineBuffer);
                    lineBuffer.SetLength(0);
                    cursor += newlineOffset + 1;
                    committedOffset = bufferStart + cursor;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }

        if (!sameFile || committedOffset != state.Offset)
        {
            state.Anchor = ReadAnchor(stream, committedOffset);
        }
        state.Offset = committedOffset;
        state.CreationTimeUtc = creationTimeUtc;
        state.LastObservedLength = stream.Length;
        state.LastWriteTimeUtc = lastWriteTimeUtc;
    }

    private void ProcessCompleteLine(string path, MemoryStream buffer)
    {
        ReadOnlySpan<byte> bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        int start = bytes.StartsWith(Encoding.UTF8.Preamble) ? Encoding.UTF8.Preamble.Length : 0;
        int length = bytes.Length - start;
        if (length > 0 && bytes[start + length - 1] == (byte)'\r') length--;
        string line = SafeUtf8.GetString(bytes.Slice(start, length));
        AuthenticationFailureEvent? failure = parser(path, line);
        if (failure is not null) EventReceived?.Invoke(this, failure);
    }

    private void PrimeMetadata(FileStream stream, string path)
    {
        stream.Position = 0;
        using StreamReader reader = new(stream, SafeUtf8, true, 1024, true);
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
        byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            long end = stream.Length;
            while (end > 0)
            {
                int length = (int)Math.Min(end, 4096);
                long start = end - length;
                stream.Position = start;
                stream.ReadExactly(buffer.AsSpan(0, length));
                int index = buffer.AsSpan(0, length).LastIndexOf((byte)'\n');
                if (index >= 0) return start + index + 1;
                end = start;
            }
            return 0;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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

    private sealed class LogFileState(long offset, DateTime creationTimeUtc, byte[] anchor, long lastObservedLength, DateTime lastWriteTimeUtc)
    {
        internal long Offset { get; set; } = offset;
        internal DateTime CreationTimeUtc { get; set; } = creationTimeUtc;
        internal byte[] Anchor { get; set; } = anchor;
        internal long LastObservedLength { get; set; } = lastObservedLength;
        internal DateTime LastWriteTimeUtc { get; set; } = lastWriteTimeUtc;
    }
}
