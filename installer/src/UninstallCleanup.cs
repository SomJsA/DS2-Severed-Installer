using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

internal static partial class InstallerEngine
{
    private static readonly string[] GeneratedOutputPaths = { "severed_probe.log", "plugins\\SeveredRuntimeFixes.log", "plugins\\SeveredExtras.log", ManagerInfo, ManagerName + ".ds2oneclick.bak" };
    private static bool KnownGeneratedOutput(string game, string relative)
    {
        AssertPlain(game, relative);
        string path = Path.Combine(game, relative);
        if (!File.Exists(path)) return false;
        if (relative == ManagerName + ".ds2oneclick.bak")
            return FileVersionInfo.GetVersionInfo(path).ProductName == "DS2SeveredOneClick";
        if (relative == ManagerInfo)
        {
            string manager = Path.Combine(game, ManagerName); AssertPlain(game, ManagerName);
            if (!File.Exists(manager) || new FileInfo(path).Length > 4096 || FileVersionInfo.GetVersionInfo(manager).ProductName != "DS2SeveredOneClick") return false;
            try
            {
                var info = new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(path));
                string hash; return info != null && info.TryGetValue("sha256", out hash) && hash == Sha256(manager);
            }
            catch { return false; }
        }
        using (var reader = new StreamReader(path))
        {
            char[] buffer = new char[4096]; string head = new string(buffer, 0, reader.Read(buffer, 0, buffer.Length));
            return relative == "severed_probe.log" ? Regex.IsMatch(head, @"\bsevered hook (?:[0-9]+|[0-9]+\+SomJs-shop-(?:test1|candidate2)|[0-9]+\+SomJs-core-split1)\b") :
                relative == "plugins\\SeveredExtras.log" ? head.Contains("SeveredExtras standalone-1;") :
                head.Contains("SomJs shop-test1 profile=") || head.Contains("SomJs shop-candidate2 profile=");
        }
    }
    private static IEnumerable<string> OwnedEmptyDirectoryCandidates(string game)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "plugins" };
        foreach (string relative in MarkerAssetPaths(game))
            for (string dir = Path.GetDirectoryName(relative); !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir)) directories.Add(dir);
        return directories.OrderByDescending(p => p.Length);
    }
    public static bool HasKnownResidue(string game)
    {
        try
        {
            if (!File.Exists(Path.Combine(game, "deadspace2.exe"))) return false;
            return GeneratedOutputPaths.Any(p => KnownGeneratedOutput(game,p)) || OwnedEmptyDirectoryCandidates(game).Any(p =>
            { AssertPlain(game,p); string path=Path.Combine(game,p); return Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any(); });
        }
        catch { return false; }
    }
    private static void CleanupKnownResidue(string game, Action<string> log)
    {
        foreach (string relative in GeneratedOutputPaths)
        {
            if (KnownGeneratedOutput(game,relative)) { File.Delete(Path.Combine(game,relative)); log(L.T("已清理本工具残留：") + relative); }
        }
        foreach (string relative in OwnedEmptyDirectoryCandidates(game))
        {
            AssertPlain(game,relative); string path=Path.Combine(game,relative);
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any()) Directory.Delete(path);
        }
    }
}
