using System;
using System.Collections.Generic;
using IDDSCommunity.IntrusionDetection.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public class SecurityAgentsSortTest
{
    [TestMethod]
    public void SecurityAgent_SortOrder_GroupsAgentsCorrectly()
    {
        // 1. System & Remote Access
        Assert.AreEqual(10, SecurityAgent.GetSortOrder("Windows Base Security Agent", "Windows 基礎安全性代理程式"));
        Assert.AreEqual(11, SecurityAgent.GetSortOrder("Windows Network Logon Security Agent", "Windows 網路登入安全性代理程式"));
        Assert.AreEqual(12, SecurityAgent.GetSortOrder("TLS/SSL Security Agent", "遠端桌面安全性代理程式"));
        Assert.AreEqual(13, SecurityAgent.GetSortOrder("Windows OpenSSH Security Agent", "Windows OpenSSH 安全性代理程式"));
        Assert.AreEqual(14, SecurityAgent.GetSortOrder("AD Credential Validation Security Agent", "AD 認證驗證安全性代理程式"));
        Assert.AreEqual(15, SecurityAgent.GetSortOrder("Kerberos pre-authentication Security Agent", "Kerberos 預先驗證安全性代理程式"));
        Assert.AreEqual(16, SecurityAgent.GetSortOrder("RRAS Security Agent", "RRAS 安全性代理程式"));

        // 2. Web & Domain Services
        Assert.AreEqual(20, SecurityAgent.GetSortOrder("Web Security Agent", "Web 安全性代理程式"));
        Assert.AreEqual(21, SecurityAgent.GetSortOrder("IIS Authentication Security Agent", "IIS 驗證安全性代理程式"));
        Assert.AreEqual(22, SecurityAgent.GetSortOrder("Windows DNS Security Agent", "Windows DNS 安全性代理程式"));

        // 3. Database Services
        Assert.AreEqual(30, SecurityAgent.GetSortOrder("SQL Server Security Agent", "SQL Server 安全性代理程式"));
        Assert.AreEqual(31, SecurityAgent.GetSortOrder("MySQL and MariaDB Security Agent", "MySQL／MariaDB 安全性代理程式"));
        Assert.AreEqual(32, SecurityAgent.GetSortOrder("PostgreSQL Security Agent", "PostgreSQL 安全性代理程式"));
        Assert.AreEqual(33, SecurityAgent.GetSortOrder("FileMaker Security Agent", "FileMaker 安全性代理程式"));

        // 4. Mail & Network Protocols
        Assert.AreEqual(40, SecurityAgent.GetSortOrder("IDDSCommunity.Agents.MailServer.SmtpAgent", "郵件伺服器 SMTP 安全性代理程式"));
        Assert.AreEqual(41, SecurityAgent.GetSortOrder("IDDSCommunity.Agents.MailServer.Pop3Agent", "POP3 安全性代理程式"));
        Assert.AreEqual(42, SecurityAgent.GetSortOrder("IDDSCommunity.Agents.MailServer.ImapAgent", "IMAP 安全性代理程式"));
        Assert.AreEqual(43, SecurityAgent.GetSortOrder("FTP Security Agent", "FTP 安全性代理程式"));
        Assert.AreEqual(44, SecurityAgent.GetSortOrder("RADIUS / NPS Security Agent", "RADIUS 安全性代理程式"));
    }

    [TestMethod]
    public void SecurityAgents_SortAgents_SortsListByCategoryAndDisplayName()
    {
        List<SecurityAgent> agents = new()
        {
            new SecurityAgent("FTP Security Agent") { DisplayName = "FTP 安全性代理程式" },
            new SecurityAgent("Windows Base Security Agent") { DisplayName = "Windows 基礎安全性代理程式" },
            new SecurityAgent("SQL Server Security Agent") { DisplayName = "SQL Server 安全性代理程式" },
            new SecurityAgent("Web Security Agent") { DisplayName = "Web 安全性代理程式" }
        };

        agents.Sort((a, b) =>
        {
            int cmp = a.SortOrder.CompareTo(b.SortOrder);
            if (cmp != 0) return cmp;
            string nameA = !string.IsNullOrEmpty(a.DisplayName) ? a.DisplayName : a.Name ?? string.Empty;
            string nameB = !string.IsNullOrEmpty(b.DisplayName) ? b.DisplayName : b.Name ?? string.Empty;
            return string.Compare(nameA, nameB, StringComparison.CurrentCultureIgnoreCase);
        });

        Assert.AreEqual("Windows Base Security Agent", agents[0].Name);
        Assert.AreEqual("Web Security Agent", agents[1].Name);
        Assert.AreEqual("SQL Server Security Agent", agents[2].Name);
        Assert.AreEqual("FTP Security Agent", agents[3].Name);
    }
}
