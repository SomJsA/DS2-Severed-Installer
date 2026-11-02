using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

internal static partial class InstallerEngine
{
    private static readonly Lazy<Dictionary<string, string>> MarkerSections = new Lazy<Dictionary<string, string>>(() =>
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string current = "General";
        foreach (string line in ReadResourceText("DS2_MARKER_INI").Split('\n'))
        {
            string trimmed = line.Trim().TrimStart('\ufeff');
            Match section = Regex.Match(trimmed, @"^\[([^\]]+)\]");
            if (section.Success) { current = section.Groups[1].Value; continue; }
            Match key = Regex.Match(trimmed, @"^(\w+)\s*=");
            if (key.Success) sections[key.Groups[1].Value] = current;
        }
        return sections;
    });

    private static Match MarkerSection(string text, string section)
    {
        return Regex.Match(text, @"(?ims)^[ \t\uFEFF]*\[" + Regex.Escape(section) + @"\][^\r\n]*(?:\r?\n|\z).*?(?=^[ \t]*\[|\z)");
    }
    private static string ReadMarkerIniValue(string text, string key)
    {
        string section;
        if (MarkerSections.Value.TryGetValue(key, out section))
        {
            Match block = MarkerSection(text, section);
            if (!block.Success) return null;
            text = block.Value;
        }
        Match value = Regex.Match(text, @"(?im)^[ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*([^;\r\n]*)(?:;[^\r\n]*)?\r?$");
        return value.Success ? value.Groups[1].Value.Trim() : null;
    }
    private static string SetMarkerIniValue(string text, string key, string value)
    {
        string section;
        if (!MarkerSections.Value.TryGetValue(key, out section)) section = "General";
        string newline = text.Contains("\r\n") ? "\r\n" : "\n";
        Match block = MarkerSection(text, section);
        if (!block.Success)
            return text + (text.EndsWith("\n") || text.Length == 0 ? "" : newline) + "[" + section + "]" + newline + key + " = " + value + newline;
        string pattern = @"(?im)^([ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*)([^;\r\n]*)(;[^\r\n]*)?(\r?)$";
        string updated;
        if (Regex.IsMatch(block.Value, pattern))
            updated = Regex.Replace(block.Value, pattern, m => m.Groups[1].Value + value +
                (m.Groups[3].Success ? " " + m.Groups[3].Value : "") + m.Groups[4].Value);
        else
            updated = block.Value + (block.Value.EndsWith("\n") ? "" : newline) + key + " = " + value + newline;
        return text.Substring(0, block.Index) + updated + text.Substring(block.Index + block.Length);
    }
}
