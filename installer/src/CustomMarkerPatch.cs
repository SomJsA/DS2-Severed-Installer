using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

internal sealed class CustomMarkerPackage
{
    public byte[] Loader;
    public string Ini;
    public readonly Dictionary<string, byte[]> Assets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<MarkerAssetInfo> Manifest()
    {
        foreach (var pair in Assets)
            yield return new MarkerAssetInfo { Path = pair.Key, Bytes = pair.Value.Length, Sha256 = InstallerEngine.HashBytes(pair.Value) };
    }
}

internal static partial class InstallerEngine
{
    private const string CustomMarkerStateKey = "marker_custom_sha256";
    private const string MarkerLoaderName = "dinput8.dll";
    private const string MarkerIniName = "MarkerPatch.ini";
    private const int MaxMarkerZipBytes = 64 * 1024 * 1024;

    private static string CustomMarkerHash(string game)
    {
        string hash = StateValue(game, CustomMarkerStateKey);
        if (hash != null && !Regex.IsMatch(hash, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException(L.T("自定义 MarkerPatch 安装记录无效。"));
        return hash;
    }

    private static bool IsManagedMarkerHash(string game, string hash)
    {
        string custom = CustomMarkerHash(game);
        return custom == null ? IsKnownMarkerHash(hash) : hash == custom;
    }

    private static InvalidDataException UnsupportedMarkerZip()
    {
        return new InvalidDataException(L.T("ZIP 不是受支持的 MarkerPatch 发布包。请选择根目录包含 32 位 dinput8.dll 和 MarkerPatch.ini 的完整发布 ZIP。"));
    }

    internal static CustomMarkerPackage ReadCustomMarkerZip(string zipPath)
    {
        var package = new CustomMarkerPackage();
        using (var stream = File.OpenRead(zipPath))
        {
            if (stream.Length > MaxMarkerZipBytes) throw UnsupportedMarkerZip();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                if (archive.Entries.Count > 270) throw UnsupportedMarkerZip();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                long total = 0;
                foreach (var entry in archive.Entries)
                {
                    string name = entry.FullName.ToLowerInvariant();
                    if (!seen.Add(name)) throw UnsupportedMarkerZip();
                    if (name == "achievements/" || name == "achievements/img/" || name == "achievements/txt/")
                    {
                        if (entry.Length != 0) throw UnsupportedMarkerZip();
                        continue;
                    }
                    bool loader = name == MarkerLoaderName;
                    bool ini = name == "markerpatch.ini";
                    bool asset = IsMarkerAssetPath(name);
                    bool doc = name == "readme.md" || name == "readme.txt" || name == "license" || name == "license.txt";
                    if ((!loader && !ini && !asset && !doc) || entry.Length > (loader ? 32 * 1024 * 1024 : ini ? 256 * 1024 : 1024 * 1024))
                        throw UnsupportedMarkerZip();
                    total += entry.Length;
                    if (total > MaxMarkerZipBytes) throw UnsupportedMarkerZip();
                    if (doc) continue;
                    byte[] bytes = new byte[(int)entry.Length];
                    using (var data = entry.Open())
                    {
                        for (int offset = 0; offset < bytes.Length;)
                        {
                            int read = data.Read(bytes, offset, bytes.Length - offset);
                            if (read == 0) throw UnsupportedMarkerZip();
                            offset += read;
                        }
                        if (data.ReadByte() >= 0) throw UnsupportedMarkerZip();
                    }
                    if (loader) package.Loader = bytes;
                    else if (ini) package.Ini = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\ufeff');
                    else package.Assets.Add(name, bytes);
                }
            }
        }
        byte[] dll = package.Loader;
        int pe = dll != null && dll.Length >= 64 ? BitConverter.ToInt32(dll, 60) : -1;
        if (pe < 64 || pe > dll.Length - 26 || BitConverter.ToUInt16(dll, 0) != 0x5a4d ||
            BitConverter.ToUInt32(dll, pe) != 0x4550 || BitConverter.ToUInt16(dll, pe + 4) != 0x14c ||
            BitConverter.ToUInt16(dll, pe + 24) != 0x10b || (BitConverter.ToUInt16(dll, pe + 22) & 0x2000) == 0 ||
            package.Ini == null || package.Ini.Contains("\0")) throw UnsupportedMarkerZip();
        RequireCustomMarkerDependencies(package.Ini);
        return package;
    }

    private static void RequireCustomMarkerDependencies(string ini)
    {
        foreach (string key in new[] { "LoadASIPlugins", "EnableSeveredDLC", "EnableHackerDLC" })
            if (ReadMarkerIniValue(ini, key) == null)
                throw new InvalidDataException(L.T("导入包缺少标准 DLC 配置项：") + key);
    }

    private static CustomMarkerPackage CaptureCustomMarker(string game, string loaderName)
    {
        var package = new CustomMarkerPackage
        {
            Loader = File.ReadAllBytes(Path.Combine(game, loaderName)),
            Ini = File.ReadAllText(Path.Combine(game, MarkerIniName))
        };
        if (HashBytes(package.Loader) != CustomMarkerHash(game)) throw UnsupportedMarkerZip();
        RequireCustomMarkerDependencies(package.Ini);
        foreach (var item in OwnedMarkerAssets(game))
        {
            byte[] data = File.ReadAllBytes(Path.Combine(game, item.Relative));
            if (HashBytes(data) != item.Sha256) throw new IOException(item.Relative);
            package.Assets.Add(item.Path, data);
        }
        return package;
    }

    private static void InstallCustomMarker(string game, CustomMarkerPackage package, InstallOptions options, List<Action> undo, Action<string> log)
    {
        InstallOwnedBytes(game, MarkerLoaderName, package.Loader, HashBytes(package.Loader), undo, log);
        InstallText(game, MarkerIniName, ConfigureMarkerPatchTemplate(package.Ini, options, true), null, undo, log);
        foreach (var item in package.Manifest()) InstallOwnedBytes(game, item.Relative, package.Assets[item.Path], item.Sha256, undo, log);
    }

    public static void SwitchMarkerVersion(string gamePath, string zipPath, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        var package = zipPath == null ? null : ReadCustomMarkerZip(zipPath);
        var newAssets = package == null ? MarkerAssetList.Value : package.Manifest().ToList();
        var paths = new[] { MarkerLoaderName, DisabledMarker, MarkerIniName, "DS2SeveredOneClick.install.txt", ManagerName, ManagerInfo };
        Transaction(game, paths.Concat(paths.Select(p => p + ".ds2oneclick.bak")).Concat(MarkerAssetPaths(game))
            .Concat(newAssets.SelectMany(a => new[] { a.Relative, a.Relative + ".ds2oneclick.bak" })), () =>
        {
            var current = InspectInstallation(game);
            ValidateMaintenanceVersion(game);
            if (!current.HasRecord || !current.MarkerPresent || current.ExternalMarker)
                throw new InvalidOperationException(L.T("请先安装或修复本工具管理的 MarkerPatch，再切换版本。"));

            var oldAssets = OwnedMarkerAssets(game);
            foreach (var old in oldAssets)
            {
                string path = Path.Combine(game, old.Relative);
                if (File.Exists(path) && Sha256(path) != old.Sha256) throw new IOException(L.T("MarkerPatch 资源已被外部修改，请先核对：") + old.Relative);
            }
            var undo = new List<Action>();
            string slot = current.MarkerEnabled ? MarkerLoaderName : DisabledMarker;
            byte[] loader = package == null ? ReadResource("DS2_MARKER_DLL") : package.Loader;
            string hash = package == null ? MarkerDllSha : HashBytes(loader);
            InstallOwnedBytes(game, slot, loader, hash, undo, log);
            string ini = package == null ? ReadResourceText("DS2_MARKER_INI") : package.Ini;
            ini = ConfigureMarkerPatchTemplate(ini, current.Options, package != null);
            InstallText(game, MarkerIniName, ini, null, undo, log);
            if (package == null) InstallMarkerAssets(game, undo, log);
            else foreach (var item in newAssets) InstallOwnedBytes(game, item.Relative, package.Assets[item.Path], item.Sha256, undo, log);
            foreach (var old in oldAssets)
                if (!newAssets.Any(a => a.Path == old.Path)) RestoreOrRemoveOwned(game, old.Relative, old.Sha256, log);

            string statePath = Path.Combine(game, "DS2SeveredOneClick.install.txt");
            string state = File.ReadAllText(statePath);
            var fields = new Dictionary<string, string> {
                { CustomMarkerStateKey, package == null ? "" : hash }, { "marker_dll_sha256", hash },
                { "owns_marker_assets", "True" }, { "marker_assets_manifest_b64", MarkerAssetManifestBase64(newAssets) },
                { "saved_marker_ini_b64", Convert.ToBase64String(Encoding.UTF8.GetBytes(ini)) }
            };
            File.WriteAllText(statePath, UpdateStateFields(state, fields), Encoding.UTF8);
            if (current.Options.DeployManager) DeployManagerCore(game, log);
            log((package == null ? L.T("已恢复随包 MarkerPatch。") : L.T("已导入自定义 MarkerPatch。")) + " SHA-256 " + hash);
        }, log);
    }
}
