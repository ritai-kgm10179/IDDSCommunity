using System;
using System.Text;

namespace Cyberarms.Agents.MailServer;

/// <summary>
/// Correlates IMAP authentication commands with tagged server responses without retaining credentials.
/// </summary>
public sealed class ImapSessionInspector
{
    private const int MaximumBufferedCharacters = 8192;
    private readonly StringBuilder clientBuffer = new();
    private readonly StringBuilder serverBuffer = new();
    private string? authenticationTag;
    private string? startTlsTag;

    /// <summary>
    /// Gets a value indicating whether the session has upgraded to TLS and must no longer be parsed.
    /// </summary>
    public bool IsEncrypted { get; private set; }

    /// <summary>
    /// Processes bytes sent by an IMAP client.
    /// </summary>
    /// <param name="data">The TCP application payload.</param>
    public void ProcessClientData(ReadOnlySpan<byte> data)
    {
        if (IsEncrypted) return;
        AppendAscii(clientBuffer, data);
        while (TryReadLine(clientBuffer, out string line))
        {
            string[] parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            if (parts[1].Equals("LOGIN", StringComparison.OrdinalIgnoreCase) ||
                parts[1].Equals("AUTHENTICATE", StringComparison.OrdinalIgnoreCase))
            {
                authenticationTag = parts[0];
            }
            else if (parts[1].Equals("STARTTLS", StringComparison.OrdinalIgnoreCase))
            {
                startTlsTag = parts[0];
            }
        }
    }

    /// <summary>
    /// Processes bytes sent by an IMAP server.
    /// </summary>
    /// <param name="data">The TCP application payload.</param>
    /// <returns><see langword="true"/> when a pending LOGIN or AUTHENTICATE command receives a tagged NO response.</returns>
    public bool ProcessServerData(ReadOnlySpan<byte> data)
    {
        if (IsEncrypted) return false;
        AppendAscii(serverBuffer, data);
        bool authenticationFailed = false;
        while (TryReadLine(serverBuffer, out string line))
        {
            string[] parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            if (startTlsTag is not null && parts[0].Equals(startTlsTag, StringComparison.Ordinal) &&
                parts[1].Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                IsEncrypted = true;
                authenticationTag = null;
                startTlsTag = null;
                clientBuffer.Clear();
                serverBuffer.Clear();
                break;
            }
            if (authenticationTag is not null && parts[0].Equals(authenticationTag, StringComparison.Ordinal))
            {
                authenticationFailed |= parts[1].Equals("NO", StringComparison.OrdinalIgnoreCase);
                if (parts[1].Equals("NO", StringComparison.OrdinalIgnoreCase) ||
                    parts[1].Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                    parts[1].Equals("BAD", StringComparison.OrdinalIgnoreCase))
                {
                    authenticationTag = null;
                }
            }
        }
        return authenticationFailed;
    }

    private static void AppendAscii(StringBuilder buffer, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;
        buffer.Append(Encoding.ASCII.GetString(data));
        if (buffer.Length > MaximumBufferedCharacters)
            buffer.Remove(0, buffer.Length - MaximumBufferedCharacters);
    }

    private static bool TryReadLine(StringBuilder buffer, out string line)
    {
        for (int index = 0; index < buffer.Length - 1; index++)
        {
            if (buffer[index] != '\r' || buffer[index + 1] != '\n') continue;
            line = buffer.ToString(0, index);
            buffer.Remove(0, index + 2);
            return true;
        }
        line = string.Empty;
        return false;
    }
}
