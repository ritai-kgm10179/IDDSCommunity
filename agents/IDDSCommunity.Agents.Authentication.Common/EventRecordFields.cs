using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;

namespace IDDSCommunity.Agents.Authentication.Common;

public static class EventRecordFields
{
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

    public static string Get(IReadOnlyDictionary<string, string> fields, params string[] names)
    {
        foreach (string name in names)
            if (fields.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)) return value;
        return string.Empty;
    }
}
