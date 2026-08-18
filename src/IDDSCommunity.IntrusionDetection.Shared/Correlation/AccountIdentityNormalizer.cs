namespace IDDSCommunity.IntrusionDetection.Shared.Correlation;

using System;
using System.Text;

/// <summary>
/// 提供 Windows 與跨通訊協定帳號表示法的確定性正規化功能。
/// </summary>
public static class AccountIdentityNormalizer
{
    /// <summary>
    /// 正規化觀察事件中的帳號、網域與安全性識別碼欄位。
    /// </summary>
    /// <param name="observation">欲正規化的安全性觀察事件。</param>
    public static void Normalize(SecurityObservationEvent observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        (string account, string domain) = Split(observation.NormalizedAccount, observation.NormalizedDomain);
        observation.NormalizedAccount = account;
        observation.NormalizedDomain = domain;
        observation.AccountSid = Canonicalize(observation.AccountSid);
    }

    /// <summary>
    /// 建立優先採用安全性識別碼的穩定帳號關聯鍵值。
    /// </summary>
    /// <param name="accountName">帳號名稱。</param>
    /// <param name="domainName">網域或本機電腦名稱。</param>
    /// <param name="accountSid">Windows 安全性識別碼。</param>
    /// <returns>可供跨來源比較的帳號關聯鍵值。</returns>
    public static string BuildKey(string? accountName, string? domainName, string? accountSid)
    {
        string sid = Canonicalize(accountSid);
        if (!string.IsNullOrEmpty(sid))
        {
            return $"SID:{sid}";
        }

        (string account, string domain) = Split(accountName, domainName);
        return $"NAME:{domain}|{account}";
    }

    private static (string Account, string Domain) Split(string? accountName, string? domainName)
    {
        string account = accountName?.Trim() ?? string.Empty;
        string domain = domainName?.Trim() ?? string.Empty;
        int slash = account.IndexOf('\\', StringComparison.Ordinal);
        if (slash > 0 && slash < account.Length - 1)
        {
            if (string.IsNullOrWhiteSpace(domain))
            {
                domain = account[..slash];
            }

            account = account[(slash + 1)..];
        }
        else
        {
            int at = account.LastIndexOf('@');
            if (at > 0 && at < account.Length - 1)
            {
                if (string.IsNullOrWhiteSpace(domain))
                {
                    domain = account[(at + 1)..];
                }

                account = account[..at];
            }
        }

        return (Canonicalize(account), Canonicalize(domain));
    }

    private static string Canonicalize(string? value) =>
        (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
}
