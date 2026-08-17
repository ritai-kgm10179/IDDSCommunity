using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;

namespace IDDSCommunity.Agents.Authentication.Common;

/// <summary>
/// 提供將 Windows 事件記錄的具名資料欄位讀取為字典，並依名稱優先順序查詢的輔助方法。
/// </summary>
public static class EventRecordFields
{
    /// <summary>
    /// 將事件記錄的 XML 表示中所有具名 <c>Data</c> 欄位讀取為字典。
    /// </summary>
    /// <param name="record">待讀取之事件記錄。</param>
    /// <returns>以欄位名稱（不分大小寫）為索引鍵的欄位值字典。</returns>
    public static IReadOnlyDictionary<string, string> Read(EventRecord record)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        XDocument document = XDocument.Parse(record.ToXml(), LoadOptions.None);
        XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
        foreach (XElement element in document.Descendants(ns + "Data"))
        {
            string? name = element.Attribute("Name")?.Value;
            if (!string.IsNullOrWhiteSpace(name)) values[name] = element.Value;
        }
        return values;
    }

    /// <summary>
    /// 依指定名稱優先順序查詢欄位字典，傳回第一個存在且非空白的值。
    /// </summary>
    /// <param name="fields">由 <see cref="Read(EventRecord)"/> 取得之欄位字典。</param>
    /// <param name="names">依優先順序排列之候選欄位名稱。</param>
    /// <returns>第一個符合的欄位值；皆未找到時傳回空字串。</returns>
    public static string Get(IReadOnlyDictionary<string, string> fields, params string[] names)
    {
        foreach (string name in names)
            if (fields.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)) return value;
        return string.Empty;
    }
}
