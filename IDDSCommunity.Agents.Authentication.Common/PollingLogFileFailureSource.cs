using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace IDDSCommunity.Agents.Authentication.Common;

public sealed class PollingLogFileFailureSource : IAuthenticationEventSource
{
    private readonly Func<IEnumerable<string>> paths;
    private readonly Func<string, AuthenticationFailureEvent?> parser;
    private readonly Dictionary<string, long> offsets = new(StringComparer.OrdinalIgnoreCase);
    private Timer? timer;
    private int reading;

    public PollingLogFileFailureSource(Func<IEnumerable<string>> paths, Func<string, AuthenticationFailureEvent?> parser)
    {
        this.paths = paths;
        this.parser = parser;
    }

    public event EventHandler<AuthenticationFailureEvent>? EventReceived;
    public event Action<Exception>? Error;
    public void Start() => timer ??= new Timer(_ => ReadAvailable(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    public void Pause() => timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    public void Resume() => timer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));
    public void Stop() { timer?.Dispose(); timer = null; }
    public void Dispose() => Stop();

    private void ReadAvailable()
    {
        if (Interlocked.Exchange(ref reading, 1) != 0) return;
        try
        {
            foreach (string path in paths()) ReadFile(path);
        }
        catch (Exception exception) { Error?.Invoke(exception); }
        finally { Volatile.Write(ref reading, 0); }
    }

    private void ReadFile(string path)
    {
        if (!File.Exists(path)) return;
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        bool known = offsets.TryGetValue(path, out long saved);
        long offset = !known ? stream.Length : saved <= stream.Length ? saved : 0;
        stream.Position = offset;
        using StreamReader reader = new(stream, new UTF8Encoding(false, true), true, leaveOpen: true);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            AuthenticationFailureEvent? failure = parser(line);
            if (failure is not null) EventReceived?.Invoke(this, failure);
        }
        offsets[path] = stream.Position;
    }
}
