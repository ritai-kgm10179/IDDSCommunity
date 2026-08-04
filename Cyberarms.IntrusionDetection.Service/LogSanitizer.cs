using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Cyberarms.IntrusionDetection.Service;

internal static partial class LogSanitizer
{
    private const int MaximumMessageLength = 4096;

    /// <summary>
    /// Removes log-forging characters, redacts common secrets, and bounds Event Log message size.
    /// </summary>
    /// <param name="message">The untrusted diagnostic message.</param>
    /// <returns>The safe bounded message.</returns>
    internal static string Sanitize(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;
        StringBuilder builder = new(Math.Min(message.Length, MaximumMessageLength));
        foreach (char value in message)
        {
            if (builder.Length >= MaximumMessageLength)
                break;
            builder.Append(char.IsControl(value) ? ' ' : value);
        }
        return SecretPattern().Replace(builder.ToString(), "$1=[REDACTED]");
    }

    [GeneratedRegex("(?i)\\b(password|token|secret|authorization)\\s*[:=]\\s*[^\\s,;]+", RegexOptions.CultureInvariant)]
    private static partial Regex SecretPattern();
}
