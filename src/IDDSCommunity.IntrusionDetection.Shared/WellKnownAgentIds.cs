using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared;

/// <summary>
/// 提供 IDDS Community 內建安全性代理程式（Security Agents）之確定性不可變識別碼（Invariant GUID）與歷史別名解析對照表。
/// </summary>
public static class WellKnownAgentIds
{
    /// <summary>
    /// Windows 基礎安全性代理程式（Windows Base Security Agent）。
    /// </summary>
    public static readonly Guid WindowsBase = new("{CC03AE88-51B4-426C-BA68-50875D70409F}");

    /// <summary>
    /// Windows 網路登入安全性代理程式（Windows Network Logon Security Agent）。
    /// </summary>
    public static readonly Guid WindowsNetworkLogon = new("{61F99E76-4C53-4D88-8C4A-1AF5D1A0C219}");

    /// <summary>
    /// 遠端桌面安全性代理程式（TLS/SSL Security Agent）。
    /// </summary>
    public static readonly Guid TerminalServer = new("{A682433B-852F-4150-ADF4-FB7F75090015}");

    /// <summary>
    /// 遠端桌面閘道安全性代理程式（Remote Desktop Gateway Security Agent）。
    /// </summary>
    public static readonly Guid RemoteDesktopGateway = new("{B8E49A12-87C2-4FE5-912A-18F7D034E59C}");

    /// <summary>
    /// Windows 遠端管理安全性代理程式（WinRM / WAC Security Agent）。
    /// </summary>
    public static readonly Guid WinRm = new("{47F3B59B-C578-4D05-8E1E-2B6B6D197F4A}");

    /// <summary>
    /// Windows OpenSSH 安全性代理程式（Windows OpenSSH Security Agent）。
    /// </summary>
    public static readonly Guid OpenSsh = new("{FA68919B-6D0B-4508-9659-3CD1E160235C}");

    /// <summary>
    /// Active Directory 認證驗證安全性代理程式（AD Credential Validation Security Agent）。
    /// </summary>
    public static readonly Guid AdCredentialValidation = new("{D67852B4-DBEF-4831-877C-E37DAB764952}");

    /// <summary>
    /// Kerberos 預先驗證安全性代理程式（Kerberos Pre-authentication Security Agent）。
    /// </summary>
    public static readonly Guid Kerberos = new("{880435D7-AB31-4498-B872-1512E7D723F0}");

    /// <summary>
    /// RRAS 安全性代理程式（RRAS Security Agent）。
    /// </summary>
    public static readonly Guid Rras = new("{FDA41145-2E75-400E-882C-E06EC4790EBE}");

    /// <summary>
    /// NPS RADIUS 安全性代理程式（NPS RADIUS Security Agent）。
    /// </summary>
    public static readonly Guid Radius = new("{981D2895-B343-477B-A2BD-21832FDD1305}");

    /// <summary>
    /// Web 安全性代理程式（Web Security Agent）。
    /// </summary>
    public static readonly Guid WebSecurity = new("{63F5567C-7A75-4870-A842-E981855DA3E9}");

    /// <summary>
    /// IIS 驗證安全性代理程式（IIS Authentication Security Agent）。
    /// </summary>
    public static readonly Guid IisAuthentication = new("{6B87C539-1585-41E5-A12F-D5073EF6D631}");

    /// <summary>
    /// Windows DNS 安全性代理程式（Windows DNS Security Agent）。
    /// </summary>
    public static readonly Guid WindowsDns = new("{0E5C35B5-7B2E-4DD5-970D-89A33C935A51}");

    /// <summary>
    /// Technitium DNS 安全性代理程式（Technitium DNS Security Agent）。
    /// </summary>
    public static readonly Guid TechnitiumDns = new("{99C71D22-830E-4E76-A83B-7D831C2442FE}");

    /// <summary>
    /// SQL Server 安全性代理程式（SQL Server Security Agent）。
    /// </summary>
    public static readonly Guid SqlServer = new("{0F470A49-594D-4895-ADE1-46B48B9B8A58}");

    /// <summary>
    /// MySQL 與 MariaDB 安全性代理程式（MySQL &amp; MariaDB Security Agent）。
    /// </summary>
    public static readonly Guid MySql = new("{EE4906AD-7242-4940-A3B0-81B4E3F16B71}");

    /// <summary>
    /// PostgreSQL 安全性代理程式（PostgreSQL Security Agent）。
    /// </summary>
    public static readonly Guid PostgreSql = new("{E4D503EE-33D9-4A79-A2E3-B19597D49D58}");

    /// <summary>
    /// FileMaker 安全性代理程式（FileMaker Security Agent）。
    /// </summary>
    public static readonly Guid FileMaker = new("{F0F28CC4-8103-4781-927E-CFD4C5991092}");

    /// <summary>
    /// 郵件伺服器 SMTP 安全性代理程式（SMTP Security Agent）。
    /// </summary>
    public static readonly Guid Smtp = new("{EB69BF23-939C-4F89-97D0-50274306D018}");

    /// <summary>
    /// 郵件伺服器 POP3 安全性代理程式（POP3 Security Agent）。
    /// </summary>
    public static readonly Guid Pop3 = new("{1F917251-2661-473A-970B-B2BB62EA6E1A}");

    /// <summary>
    /// 郵件伺服器 IMAP 安全性代理程式（IMAP Security Agent）。
    /// </summary>
    public static readonly Guid Imap = new("{3F8B715C-4A2D-4C98-9C6E-7F89B219E022}");

    /// <summary>
    /// FTP 伺服器安全性代理程式（FTP Security Agent）。
    /// </summary>
    public static readonly Guid Ftp = new("{F040A37F-8A53-428E-85A3-EDC858144742}");

    /// <summary>
    /// FileZilla 伺服器安全性代理程式（FileZilla Security Agent）。
    /// </summary>
    public static readonly Guid FileZilla = new("{88B67B54-9E7D-4F7B-8A5F-4E90B0F33A11}");

    private sealed record AgentDescriptor(Guid Id, string PrimaryName, string AssemblyName, string DisplayName, string[] Aliases);

    private static readonly AgentDescriptor[] KnownAgents =
    [
        new(WindowsBase, "WindowsSecurityBase", "IDDSCommunity.IntrusionDetection.Base.dll", "Windows 基礎安全性代理程式",
            ["Windows Base Security Agent", "WindowsBase", "Windows 基礎", "基礎安全性", "IDDSCommunity.IntrusionDetection.Base.WindowsSecurityBase"]),
        new(WindowsNetworkLogon, "WindowsNetworkLogonSecurityAgent", "IDDSCommunity.Agents.WindowsNetworkLogon.dll", "Windows 網路登入安全性代理程式",
            ["Windows Network Logon Security Agent", "WindowsNetworkLogon", "網路登入", "IDDSCommunity.Agents.WindowsNetworkLogon"]),
        new(TerminalServer, "TlsSslAgent", "IDDSCommunity.Agents.TerminalServer.dll", "遠端桌面安全性代理程式",
            ["TLS/SSL Security Agent", "Remote Desktop Security Agent", "TlsSsl", "遠端桌面", "IDDSCommunity.Agents.TerminalServer"]),
        new(RemoteDesktopGateway, "RdGatewaySecurityAgent", "IDDSCommunity.Agents.RemoteDesktopGateway.dll", "遠端桌面閘道安全性代理程式",
            ["Remote Desktop Gateway Security Agent", "RdGateway", "遠端桌面閘道", "IDDSCommunity.Agents.RemoteDesktopGateway"]),
        new(WinRm, "WinRmSecurityAgent", "IDDSCommunity.Agents.WinRm.dll", "Windows 遠端管理 (WinRM / WAC) 安全性代理程式",
            ["WinRM Security Agent", "Windows Remote Management", "WinRm", "Windows 遠端管理", "IDDSCommunity.Agents.WinRm"]),
        new(OpenSsh, "OpenSshSecurityAgent", "IDDSCommunity.Agents.OpenSsh.dll", "Windows OpenSSH 安全性代理程式",
            ["Windows OpenSSH Security Agent", "OpenSSH", "OpenSsh", "IDDSCommunity.Agents.OpenSsh"]),
        new(AdCredentialValidation, "AdCredentialValidationSecurityAgent", "IDDSCommunity.IntrusionDetection.Base.dll", "AD 認證驗證安全性代理程式",
            ["AD Credential Validation Security Agent", "AD Credential", "AD 認證", "IDDSCommunity.IntrusionDetection.Base.AdCredentialValidationSecurityAgent"]),
        new(Kerberos, "KerberosSecurityAgent", "IDDSCommunity.IntrusionDetection.Base.dll", "Kerberos 預先驗證安全性代理程式",
            ["Kerberos pre-authentication Security Agent", "Kerberos", "IDDSCommunity.IntrusionDetection.Base.KerberosSecurityAgent"]),
        new(Rras, "RrasSecurityAgent", "IDDSCommunity.IntrusionDetection.Base.dll", "RRAS 安全性代理程式",
            ["RRAS Security Agent", "RRAS", "IDDSCommunity.IntrusionDetection.Base.RrasSecurityAgent"]),
        new(Radius, "RadiusSecurityAgent", "IDDSCommunity.Agents.Radius.dll", "NPS RADIUS 安全性代理程式",
            ["RADIUS / NPS Security Agent", "RADIUS Security Agent", "NPS Security Agent", "Radius", "NPS", "IDDSCommunity.Agents.Radius"]),
        new(WebSecurity, "WebSecurityAgent", "IDDSCommunity.Agents.WebSecurity.dll", "Web 安全性代理程式",
            ["Web Security Agent", "SecurityMonitor", "Web 安全", "IDDSCommunity.Agents.WebSecurity"]),
        new(IisAuthentication, "IisAuthenticationSecurityAgent", "IDDSCommunity.Agents.IisAuthentication.dll", "IIS 驗證安全性代理程式",
            ["IIS Authentication Security Agent", "IIS Authentication", "IIS 驗證", "IDDSCommunity.Agents.IisAuthentication"]),
        new(WindowsDns, "WindowsDnsSecurityAgent", "IDDSCommunity.Agents.WindowsDns.dll", "Windows DNS 安全性代理程式",
            ["Windows DNS Security Agent", "Windows DNS", "IDDSCommunity.Agents.WindowsDns"]),
        new(TechnitiumDns, "TechnitiumDnsSecurityAgent", "IDDSCommunity.Agents.TechnitiumDns.dll", "Technitium DNS 安全性代理程式",
            ["Technitium DNS Security Agent", "Technitium", "IDDSCommunity.Agents.TechnitiumDns"]),
        new(SqlServer, "SqlFailedLoginWatcher", "IDDSCommunity.Agents.SqlServer.dll", "SQL Server 安全性代理程式",
            ["SQL Server Security Agent", "SQL Server", "SqlServer", "IDDSCommunity.Agents.SqlServer.SqlFailedLoginWatcher", "IDDSCommunity.Agents.SqlServer"]),
        new(MySql, "MySqlFailedLoginWatcher", "IDDSCommunity.Agents.MySql.dll", "MySQL／MariaDB 安全性代理程式",
            ["MySQL and MariaDB Security Agent", "MySQL", "MariaDB", "MySql", "IDDSCommunity.Agents.MySql.MySqlFailedLoginWatcher", "IDDSCommunity.Agents.MySql"]),
        new(PostgreSql, "PostgreSqlSecurityAgent", "IDDSCommunity.Agents.PostgreSql.dll", "PostgreSQL 安全性代理程式",
            ["PostgreSQL Security Agent", "PostgreSQL", "Postgres", "IDDSCommunity.Agents.PostgreSql"]),
        new(FileMaker, "FileMakerSecurityAgent", "IDDSCommunity.Agents.FileMaker.dll", "FileMaker 安全性代理程式",
            ["FileMaker Security Agent", "FileMaker", "IDDSCommunity.Agents.FileMaker"]),
        new(Smtp, "IDDSCommunity.Agents.MailServer.SmtpAgent", "IDDSCommunity.Agents.MailServer.dll", "郵件伺服器 SMTP 安全性代理程式",
            ["SMTP Security Agent", "SmtpAgent", "SMTP", "Legacy.Mail.SmtpAgent", "IDDSCommunity.Agents.MailServer.SmtpAgent"]),
        new(Pop3, "IDDSCommunity.Agents.MailServer.Pop3Agent", "IDDSCommunity.Agents.MailServer.dll", "POP3 安全性代理程式",
            ["POP3 Security Agent", "Pop3Agent", "POP3", "Legacy.Mail.Pop3Agent", "IDDSCommunity.Agents.MailServer.Pop3Agent"]),
        new(Imap, "IDDSCommunity.Agents.MailServer.ImapAgent", "IDDSCommunity.Agents.MailServer.dll", "IMAP 安全性代理程式",
            ["IMAP Security Agent", "ImapAgent", "IMAP", "Legacy.Mail.ImapAgent", "IDDSCommunity.Agents.MailServer.ImapAgent"]),
        new(Ftp, "FtpAgent", "IDDSCommunity.Agents.FtpServer.dll", "FTP 安全性代理程式",
            ["FTP Security Agent", "FTP Server", "FtpAgent", "FTP", "IDDSCommunity.Agents.FtpServer"]),
        new(FileZilla, "FileZillaSecurityAgent", "IDDSCommunity.Agents.FileZilla.dll", "FileZilla 安全性代理程式",
            ["FileZilla Security Agent", "FileZilla", "IDDSCommunity.Agents.FileZilla"])
    ];

    /// <summary>
    /// 嘗試自原始字串（GUID、型別名稱、組件名稱、舊版英文名稱或多語系顯示名稱）解析出確定性 Invariant GUID。
    /// </summary>
    /// <param name="rawIdentifier">待解析的識別字串。</param>
    /// <param name="canonicalGuid">成功解析時傳回的確定性 GUID，失敗時傳回 <see cref="Guid.Empty"/>。</param>
    /// <returns>若成功解析則傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public static bool TryResolveCanonicalGuid(string? rawIdentifier, out Guid canonicalGuid)
    {
        if (string.IsNullOrWhiteSpace(rawIdentifier))
        {
            canonicalGuid = Guid.Empty;
            return false;
        }

        if (Guid.TryParse(rawIdentifier, out canonicalGuid))
            return true;

        string trimmed = rawIdentifier.Trim();
        string shortName = GetShortName(trimmed);

        // 1. 精確比對 PrimaryName, AssemblyName, DisplayName
        foreach (AgentDescriptor descriptor in KnownAgents)
        {
            if (descriptor.PrimaryName.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                descriptor.DisplayName.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                descriptor.AssemblyName.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                canonicalGuid = descriptor.Id;
                return true;
            }
        }

        // 2. 短名稱比對
        if (!string.IsNullOrEmpty(shortName))
        {
            foreach (AgentDescriptor descriptor in KnownAgents)
            {
                if (GetShortName(descriptor.PrimaryName).Equals(shortName, StringComparison.OrdinalIgnoreCase) ||
                    GetShortName(descriptor.AssemblyName).Equals(shortName, StringComparison.OrdinalIgnoreCase))
                {
                    canonicalGuid = descriptor.Id;
                    return true;
                }
            }
        }

        // 3. 別名陣列比對
        foreach (AgentDescriptor descriptor in KnownAgents)
        {
            foreach (string alias in descriptor.Aliases)
            {
                if (alias.Equals(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    GetShortName(alias).Equals(shortName, StringComparison.OrdinalIgnoreCase))
                {
                    canonicalGuid = descriptor.Id;
                    return true;
                }
            }
        }

        // 4. 關鍵字模糊比對
        foreach (AgentDescriptor descriptor in KnownAgents)
        {
            foreach (string alias in descriptor.Aliases)
            {
                if (trimmed.Contains(alias, StringComparison.OrdinalIgnoreCase))
                {
                    canonicalGuid = descriptor.Id;
                    return true;
                }
            }
        }

        canonicalGuid = Guid.Empty;
        return false;
    }

    private static string GetShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return string.Empty;
        string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fullName);
        int idx = nameWithoutExt.LastIndexOf('.');
        return idx >= 0 ? nameWithoutExt[(idx + 1)..] : nameWithoutExt;
    }
}
