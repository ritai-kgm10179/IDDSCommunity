using System;
using System.Text;

namespace IDDSCommunity.Agents.MailServer;
/// <summary>
/// 將 IMAP 驗證命令與標記的伺服器回應進行關聯，且不保留憑證。
/// </summary>
public sealed class ImapSessionInspector
{
    private const int MaximumBufferedCharacters = 8192;
    private readonly StringBuilder clientBuffer = new();
    private readonly StringBuilder serverBuffer = new();
    private string? authenticationTag;
    private string? startTlsTag;
    /// <summary>
    /// 取得一個值，指出該會話是否已升級至 TLS 且不得再進行解析。
    /// </summary>
    public bool IsEncrypted { get; private set; }
    /// <summary>
    /// 取得最後互動時間。
    /// </summary>
    public DateTime LastInteraction { get; private set; } = DateTime.UtcNow;
    /// <summary>
    /// 處理 IMAP 用戶端傳送的位元組。
    /// </summary>
    /// <param name="data">TCP 應用程式負載資料。</param>
    public void ProcessClientData(ReadOnlySpan<byte> data)
    {
        LastInteraction = DateTime.UtcNow;
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
    /// 處理 IMAP 伺服器傳送的位元組。
    /// </summary>
    /// <param name="data">TCP 應用程式負載資料。</param>
    /// <returns>當未處理的 LOGIN 或 AUTHENTICATE 命令收到標記的 NO 回應時傳回 <see langword="true"/>。</returns>
    public bool ProcessServerData(ReadOnlySpan<byte> data)
    {
        LastInteraction = DateTime.UtcNow;
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
