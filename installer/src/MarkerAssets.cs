using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

internal sealed class MarkerAssetInfo { public string Path, Sha256; public long Bytes; public string Relative { get { return Path.Replace('/', System.IO.Path.DirectorySeparatorChar); } } }
internal static partial class InstallerEngine
{
    private static readonly Lazy<List<MarkerAssetInfo>> MarkerAssetList = new Lazy<List<MarkerAssetInfo>>(() => ParseMarkerAssets(ReadResourceText("DS2_MARKER_ASSET_MANIFEST")));
    private static List<MarkerAssetInfo> ParseMarkerAssets(string json)
    {
        var root = new JavaScriptSerializer { MaxJsonLength = 128 * 1024 }.Deserialize<Dictionary<string, object>>(json);
        var rows = root["files"] as System.Collections.IEnumerable;
        var result = new List<MarkerAssetInfo>();
        if (rows == null) throw new InvalidDataException("Invalid MarkerPatch asset manifest");
        foreach (object row in rows)
        {
            var data = row as Dictionary<string, object>;
            if (data == null || result.Count >= 256) throw new InvalidDataException("Invalid MarkerPatch asset entry");
            var item = new MarkerAssetInfo { Path = Convert.ToString(data["path"]), Bytes = Convert.ToInt64(data["bytes"]), Sha256 = Convert.ToString(data["sha256"]) };
            if (!Regex.IsMatch(item.Path, @"^achievements/(img/[0-9]{1,4}\.png|txt/[a-z]{2}\.txt)$") ||
                !Regex.IsMatch(item.Sha256, "^[0-9a-f]{64}$") || item.Bytes < 0 || item.Bytes > 1024 * 1024 || result.Any(r => r.Path == item.Path))
                throw new InvalidDataException("Invalid MarkerPatch asset path or hash");
            result.Add(item);
        }
        return result;
    }
    private static string MarkerAssetManifestBase64()
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(ReadResourceText("DS2_MARKER_ASSET_MANIFEST")));
    }
    private static List<MarkerAssetInfo> OwnedMarkerAssets(string game)
    {
        if (StateValue(game, "owns_marker_assets") != "True") return new List<MarkerAssetInfo>();
        string json = StateValue(game, "marker_assets_manifest_b64");
        if (string.IsNullOrEmpty(json) || json.Length > 180 * 1024) throw new InvalidDataException(L.T("MarkerPatch 资源归属清单缺失或无效，已保留文件。"));
        return ParseMarkerAssets(Encoding.UTF8.GetString(Convert.FromBase64String(json)));
    }
    private static IEnumerable<string> MarkerAssetPaths(string game)
    {
        return MarkerAssetList.Value.Concat(OwnedMarkerAssets(game)).SelectMany(a => new[] { a.Relative, a.Relative + ".ds2oneclick.bak" }).Distinct(StringComparer.OrdinalIgnoreCase);
    }
    private static bool IsOwnedMarkerAsset(string game, string relative, string hash)
    {
        if (hash == null || !relative.StartsWith("achievements" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;
        var owned = OwnedMarkerAssets(game).FirstOrDefault(a => a.Relative.Equals(relative, StringComparison.OrdinalIgnoreCase));
        return owned != null && (owned.Sha256 == hash || MarkerAssetList.Value.Any(a => a.Relative.Equals(relative, StringComparison.OrdinalIgnoreCase) && a.Sha256 == hash));
    }
    private static string HashBytes(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
    private static void InstallMarkerAssets(string game, List<Action> undo, Action<string> log)
    {
        foreach (var old in OwnedMarkerAssets(game))
        {
            string path = Path.Combine(game, old.Relative);
            if (File.Exists(path) && !IsOwnedMarkerAsset(game, old.Relative, Sha256(path))) throw new IOException(L.T("MarkerPatch 资源已被外部修改，请先核对：") + old.Relative);
        }
        byte[] package = ReadResource("DS2_MARKER_ASSETS");
        if (HashBytes(package) != "95781f153738dbc7a67d014476fea730195933812e36e746051279a0218b62e9") throw new InvalidDataException(L.T("内置 MarkerPatch 资源包校验失败。"));
        using (var memory = new MemoryStream(package))
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Read))
        {
            foreach (var item in MarkerAssetList.Value)
            {
                var entry = archive.GetEntry(item.Path);
                if (entry == null || entry.Length != item.Bytes) throw new InvalidDataException(item.Path);
                byte[] bytes;
                using (var source = entry.Open()) using (var payload = new MemoryStream()) { source.CopyTo(payload); bytes = payload.ToArray(); }
                if (HashBytes(bytes) != item.Sha256) throw new InvalidDataException(item.Path);
                InstallOwnedBytes(game, item.Relative, bytes, item.Sha256, undo, log);
            }
        }
    }
}
