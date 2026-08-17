using System;

namespace IDDSCommunity.Agents.MailServer;


/// <summary>
/// 表示連線目前觀察到的最新 POP3 命令。
/// </summary>
public enum Pop3Message
{
    /// <summary>
    /// 尚未觀察到任何命令。
    /// </summary>
    None,
    /// <summary>
    /// APOP 命令。
    /// </summary>
    APOP,
    /// <summary>
    /// DELE 命令。
    /// </summary>
    DELE,
    /// <summary>
    /// LIST 命令。
    /// </summary>
    LIST,
    /// <summary>
    /// NOOP 命令。
    /// </summary>
    NOOP,
    /// <summary>
    /// PASS 命令。
    /// </summary>
    PASS,
    /// <summary>
    /// QUIT 命令。
    /// </summary>
    QUIT,
    /// <summary>
    /// RETR 命令。
    /// </summary>
    RETR,
    /// <summary>
    /// RSET 命令。
    /// </summary>
    RSET,
    /// <summary>
    /// STAT 命令。
    /// </summary>
    STAT,
    /// <summary>
    /// TOP 命令。
    /// </summary>
    TOP,
    /// <summary>
    /// UIDL 命令。
    /// </summary>
    UIDL,
    /// <summary>
    /// USER 命令。
    /// </summary>
    USER
}

/// <summary>
/// 追蹤單一 POP3 用戶端連線的最新命令與互動時間，執行緒安全。
/// </summary>
public class Pop3Client
{
    private readonly object sync = new();
    private Pop3Message lastMessage;
    private DateTime lastInteraction;
    /// <summary>
    /// 取得或設定此連線觀察到的最新 POP3 命令。
    /// </summary>
    public Pop3Message LastMessage
    {
        get
        {
            lock (sync)
                return lastMessage;
        }
        set
        {
            lock (sync)
                lastMessage = value;
        }
    }
    /// <summary>
    /// 取得或設定此連線觀察到的最新封包時間戳記。
    /// </summary>
    public DateTime LastInteraction
    {
        get
        {
            lock (sync)
                return lastInteraction;
        }
        set
        {
            lock (sync)
                lastInteraction = value;
        }
    }
}
