using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;

internal sealed class ReleaseUpdate
{
    public string Tag, Notes, Page;
    public Version Version;
}
internal sealed class UpdateReport
{
    public ReleaseUpdate Installer;
    public string CombinedNotes = "", MarkerNotes = "", Error = "";
    public bool MarkerUpdate;
    public bool UsedFeedFallback;
    public string MarkerPage = "https://github.com/Wemino/MarkerPatch/releases";
}

internal static class UpdateChecker
{
    internal const string Repository = "SomJsA/DS2-Severed-Installer";
    internal const string BundledMarkerVersion = "3.0.7";
    private const int MaxJson = 2 * 1024 * 1024;

    internal static Version ParseVersion(string tag)
    {
        Version value;
        return Version.TryParse((tag ?? "").TrimStart('v', 'V'), out value) ? value : null;
    }
    private static string Text(IDictionary<string, object> item, string key)
    {
        object value;
        return item.TryGetValue(key, out value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : "";
    }
    private static bool Flag(IDictionary<string, object> item, string key)
    {
        object value;
        return item.TryGetValue(key, out value) && value is bool && (bool)value;
    }
    private static HttpWebRequest Request(string url)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.UserAgent = "DS2-Severed-Installer/" + ProductInfo.ReleaseVersion;
        request.Accept = request.RequestUri.Host == "api.github.com" ? "application/vnd.github+json" : "*/*";
        request.Timeout = 8000; request.ReadWriteTimeout = 8000;
        request.AllowAutoRedirect = true;
        return request;
    }
    private static byte[] ReadLimited(Stream stream, int limit)
    {
        using (var memory = new MemoryStream())
        {
            byte[] buffer = new byte[16384]; int n;
            while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (memory.Length + n > limit) throw new InvalidDataException(L.T("更新响应超过限制，已停止。"));
                memory.Write(buffer, 0, n);
            }
            return memory.ToArray();
        }
    }
    private static string Fetch(string url)
    {
        using (var response = (HttpWebResponse)Request(url).GetResponse())
        using (var stream = response.GetResponseStream()) return Encoding.UTF8.GetString(ReadLimited(stream, MaxJson));
    }
    private static IEnumerable<IDictionary<string, object>> Items(object value)
    {
        var items = value as IEnumerable;
        if (items == null || value is string) yield break;
        foreach (object item in items)
        {
            var map = item as IDictionary<string, object>;
            if (map != null) yield return map;
        }
    }
    private static IEnumerable<ReleaseUpdate> ParseReleases(string json)
    {
        object data = new JavaScriptSerializer { MaxJsonLength = MaxJson }.DeserializeObject(json);
        foreach (var item in Items(data))
        {
            if (Flag(item, "draft") || Flag(item, "prerelease")) continue;
            string tag = Text(item, "tag_name"); Version version = ParseVersion(tag);
            if (version == null) continue;
            var result = new ReleaseUpdate { Tag = tag, Version = version, Page = Text(item, "html_url"), Notes = Text(item, "body") };
            yield return result;
        }
    }
    internal static UpdateReport ParseInstallerUpdates(string json, string current)
    {
        var report = new UpdateReport();
        Version installed = ParseVersion(current);
        if (installed == null) throw new InvalidDataException("Invalid current version");
        var newer = ParseReleases(json).Where(r => r.Version > installed).OrderByDescending(r => r.Version).ToList();
        report.Installer = newer.FirstOrDefault();
        report.CombinedNotes = string.Join("\r\n\r\n", newer.Select(r => r.Tag + "\r\n" + (r.Notes.Length > 24000 ? r.Notes.Substring(0, 24000) : r.Notes)));
        return report;
    }
    private static IEnumerable<ReleaseUpdate> ReadReleases(string repository, out bool fallback)
    {
        fallback = false;
        try { return ParseReleases(Fetch("https://api.github.com/repos/" + repository + "/releases?per_page=30")).ToList(); }
        catch (WebException) { fallback = true; }
        // GitHub's shared anonymous API quota can be exhausted. The public Atom
        // feed needs no token; use /latest to bound the offered version to the
        // current stable release rather than offering a future preview tag.
        Version latest;
        using (var response = (HttpWebResponse)Request("https://github.com/" + repository + "/releases/latest").GetResponse())
        {
            string prefix = "/" + repository + "/releases/tag/";
            string path = response.ResponseUri.AbsolutePath;
            if (response.ResponseUri.Host != "github.com" || !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid latest-release response");
            latest = ParseVersion(Uri.UnescapeDataString(path.Substring(prefix.Length)));
            if (latest == null) throw new InvalidDataException("Invalid latest-release tag");
        }
        string feed = Fetch("https://github.com/" + repository + "/releases.atom");
        var document = new XmlDocument { XmlResolver = null };
        using (var reader = XmlReader.Create(new StringReader(feed), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) document.Load(reader);
        var list = new List<ReleaseUpdate>();
        foreach (XmlNode entry in document.SelectNodes("/*[local-name()='feed']/*[local-name()='entry']"))
        {
            XmlNode link = entry.SelectSingleNode("*[local-name()='link' and @rel='alternate']");
            string href = link == null || link.Attributes["href"] == null ? "" : link.Attributes["href"].Value;
            Uri uri; string prefix = "/" + repository + "/releases/tag/";
            if (!Uri.TryCreate(href, UriKind.Absolute, out uri) || uri.Host != "github.com" || !uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            string tag = Uri.UnescapeDataString(uri.AbsolutePath.Substring(prefix.Length)); Version version = ParseVersion(tag);
            if (version == null || version > latest) continue;
            XmlNode content = entry.SelectSingleNode("*[local-name()='content']");
            string body = content == null ? "" : content.InnerText;
            body = WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(body, @"(?i)<br\s*/?>|</p>|</li>", "\r\n"), "<[^>]+>", ""));
            list.Add(new ReleaseUpdate { Tag = tag, Version = version, Page = href, Notes = body });
        }
        return list;
    }
    internal static string BrowserPage(UpdateReport report)
    {
        string fallback=report.Installer!=null || !report.MarkerUpdate ? ProductInfo.OwnReleasesPage : "https://github.com/Wemino/MarkerPatch/releases";
        string offered=report.Installer!=null ? report.Installer.Page : report.MarkerUpdate ? report.MarkerPage : fallback;
        Uri candidate;
        if(!Uri.TryCreate(offered,UriKind.Absolute,out candidate) || candidate.Host!="github.com")offered=fallback;
        return WebLinks.Normalize(offered,fallback);
    }
    public static UpdateReport Check()
    {
        var result = new UpdateReport();
        try
        {
            bool fallback; var newer = ReadReleases(Repository, out fallback).Where(r => r.Version > ParseVersion(ProductInfo.ReleaseVersion)).OrderByDescending(r => r.Version).ToList();
            result.UsedFeedFallback = fallback;
            result.Installer = newer.FirstOrDefault();
            result.CombinedNotes = string.Join("\r\n\r\n", newer.Select(r => r.Tag + "\r\n" + r.Notes));
        }
        catch (Exception ex) { result.Error = L.T("本程序更新检查失败：") + ex.Message; }
        try
        {
            bool fallback; var parsed = ReadReleases("Wemino/MarkerPatch", out fallback);
            result.UsedFeedFallback |= fallback;
            Version bundled = ParseVersion(BundledMarkerVersion);
            var newer = parsed.Where(r => r.Version > bundled).OrderByDescending(r => r.Version).ToList();
            result.MarkerUpdate = newer.Count != 0;
            if (newer.Count != 0)
            {
                result.MarkerPage = newer[0].Page;
                result.MarkerNotes = string.Join("\r\n\r\n", newer.Select(r => r.Tag + "\r\n" + r.Notes));
            }
        }
        catch (Exception ex) { result.Error += "\r\n" + L.T("MarkerPatch 更新检查失败：") + ex.Message; }
        return result;
    }

}
