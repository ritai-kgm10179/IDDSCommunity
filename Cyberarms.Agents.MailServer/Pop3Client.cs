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
    public Pop3Message LastMessage { get; set; }
    public DateTime LastInteraction { get; set; }
}
