using System;

namespace Cyberarms.Agents.MailServer;


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
    /// Gets or sets the most recent POP3 command observed for this connection.
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
    /// Gets or sets the timestamp of the most recent packet observed for this connection.
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
