using System;

namespace IDDSCommunity.Agents.MailServer;


public enum Pop3Message
{
    None,
    APOP,
    DELE,
    LIST,
    NOOP,
    PASS,
    QUIT,
    RETR,
    RSET,
    STAT,
    TOP,
    UIDL,
    USER
}

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
