using System;
using System.Collections.Generic;

namespace IDDSCommunity.IntrusionDetection.Shared.Deception;

/// <summary>
/// 提供誘餌帳號 (Honey-Accounts / Canary Accounts) 與欺敵陷阱偵測引擎。
/// </summary>
public sealed class HoneyAccountDetector
{
    private readonly HashSet<string> honeyAccounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object syncLock = new();

    /// <summary>
    /// 當偵測到針對誘餌帳號發動之未授權登入嘗試時觸發之委派事件。
    /// </summary>
    public event Action<string, string, string>? HoneyAccountBreached;

    /// <summary>
    /// 初始化 <see cref="HoneyAccountDetector"/> 類別的新執行個體。
    /// </summary>
    /// <param name="initialAccounts">選擇性初始誘餌帳號清單字串（以逗號或分號分隔）。</param>
    public HoneyAccountDetector(string? initialAccounts = null)
    {
        if (!string.IsNullOrWhiteSpace(initialAccounts))
        {
            UpdateHoneyAccounts(initialAccounts);
        }
    }

    /// <summary>
    /// 更新誘餌帳號清單。
    /// </summary>
    /// <param name="accountsRaw">以逗號、分號或換行分隔之帳號清單字串。</param>
    public void UpdateHoneyAccounts(string accountsRaw)
    {
        lock (syncLock)
        {
            honeyAccounts.Clear();
            if (string.IsNullOrWhiteSpace(accountsRaw)) return;

            string[] items = accountsRaw.Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    honeyAccounts.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// 取得目前配置之誘餌帳號數量。
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return honeyAccounts.Count;
            }
        }
    }

    /// <summary>
    /// 檢查指定的目標帳號名稱是否為誘餌帳號。
    /// </summary>
    /// <param name="targetAccount">嘗試登入之目標使用者名稱。</param>
    /// <returns>若為誘餌帳號傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool IsHoneyAccount(string? targetAccount)
    {
        if (string.IsNullOrWhiteSpace(targetAccount)) return false;

        string normalized = targetAccount.Trim();
        // 處理包含網域前綴的情況 (如 DOMAIN\admin_backup 或 admin_backup@corp.local)
        int slashIdx = normalized.LastIndexOf('\\');
        if (slashIdx >= 0 && slashIdx < normalized.Length - 1)
        {
            normalized = normalized[(slashIdx + 1)..];
        }
        int atIdx = normalized.IndexOf('@');
        if (atIdx > 0)
        {
            normalized = normalized[..atIdx];
        }

        lock (syncLock)
        {
            return honeyAccounts.Contains(normalized);
        }
    }

    /// <summary>
    /// 檢查並記錄登入事件。若目標帳號為誘餌帳號，自動觸發 <see cref="HoneyAccountBreached"/> 事件。
    /// </summary>
    /// <param name="sourceIp">來源 IP 位址。</param>
    /// <param name="targetAccount">嘗試登入之目標帳號。</param>
    /// <param name="agentName">回報之安全代理程式名稱。</param>
    /// <returns>若觸發誘餌陷阱傳回 <see langword="true"/>；否則傳回 <see langword="false"/>。</returns>
    public bool CheckAndReport(string sourceIp, string? targetAccount, string agentName)
    {
        if (IsHoneyAccount(targetAccount))
        {
            HoneyAccountBreached?.Invoke(sourceIp, targetAccount ?? "Unknown", agentName);
            return true;
        }
        return false;
    }
}
