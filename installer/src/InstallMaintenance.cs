using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

internal sealed class InstalledSelection
{
    public bool HasRecord, MarkerPresent, MarkerEnabled, MarkerIsCurrent, ExternalMarker, HasComponents;
    public string Version;
    public InstallOptions Options = new InstallOptions();
}

internal static partial class InstallerEngine
{
    private const string DisabledMarker = "dinput8.dll.ds2oneclick-off";
    private const string ManagerName = "DS2PatchManager.exe";
    private const string ManagerInfo = "DS2PatchManager.version.json";

    public static InstalledSelection InspectInstallation(string gamePath)
    {
        string game = ValidateGame(gamePath);
        var result = new InstalledSelection();
        result.HasRecord = File.Exists(Path.Combine(game, "DS2SeveredOneClick.install.txt"));
        result.Version = StateValue(game, "installer_release") ?? StateValue(game, "version") ?? "";
        bool markerOwned = StateValue(game, "marker_patch") == "True";
        string selected = StateValue(game, "selected_marker");
        result.Options.MarkerPatch = selected == null ? markerOwned && StateValue(game, "marker_profile") != "minimal-or-preserved" : selected == "True";
        result.Options.Chinese = (StateValue(game, "selected_chinese") ?? StateValue(game, "chinese")) == "True";
        result.Options.Dlc = (StateValue(game, "selected_dlc") ?? StateValue(game, "dlc")) == "True";
        result.Options.NodeFix = StateValue(game, "node_fix") == "True";
        string config = Path.Combine(game, RuntimeConfig);
        if (StateValue(game, "node_fix") == null && File.Exists(config))
            result.Options.NodeFix = Regex.IsMatch(File.ReadAllText(config), @"(?m)^\s*Profile\s*=\s*[12]\s*$");
        string preference = StateValue(game, "node_fix_preference");
        if (preference != null) result.Options.NodeFix = preference == "True";
        else if (!result.Options.Dlc || (StateValue(game, "node_fix") == null && !File.Exists(config))) result.Options.NodeFix = true;
        result.Options.DeployManager = File.Exists(Path.Combine(game, ManagerName));
        result.Options.DlcArchivePath = StateValue(game, "dlc_package_path");
        result.HasComponents = result.Options.Chinese || result.Options.Dlc || result.Options.MarkerPatch;
        string active = Path.Combine(game, "dinput8.dll"), disabled = Path.Combine(game, DisabledMarker);
        result.MarkerPresent = File.Exists(active) || File.Exists(disabled);
        result.MarkerEnabled = File.Exists(active);
        result.MarkerIsCurrent = File.Exists(active) ? Sha256(active) == MarkerDllSha : File.Exists(disabled) && Sha256(disabled) == MarkerDllSha;
        result.ExternalMarker = (result.MarkerPresent && !markerOwned) ||
            (File.Exists(active) && !IsKnownMarkerHash(Sha256(active))) ||
            (File.Exists(disabled) && !IsKnownMarkerHash(Sha256(disabled))) || (File.Exists(active) && File.Exists(disabled));
        result.ExternalMarker |= File.Exists(Path.Combine(game, "plugins", "MarkerPatch.asi")) || File.Exists(Path.Combine(game, "plugins", "MarkerPatch.ini"));
        string ini = Path.Combine(game, "MarkerPatch.ini");
        if (File.Exists(ini))
        {
            string text = File.ReadAllText(ini);
            foreach (var item in MarkerCatalog.Features)
            {
                string value = ReadIniValue(text, item.Key);
                if (value == "0" || value == "1") result.Options.MarkerValues[item.Key] = value == "1";
            }
            foreach (var item in MarkerCatalog.Parameters)
            {
                decimal value;
                if (decimal.TryParse(ReadIniValue(text, item.Key), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= item.Min && value <= item.Max)
                    result.Options.MarkerParameters[item.Key] = value.ToString(CultureInfo.InvariantCulture);
            }
            decimal scale;
            if (decimal.TryParse(ReadIniValue(text, "FontScalingFactor"), NumberStyles.Float, CultureInfo.InvariantCulture, out scale) && scale >= .5m && scale <= 3m)
                result.Options.SubtitleScale = scale;
            // Recover choices before automatic dependencies were imposed. Keep
            // every other value from the actual INI, including manual edits.
            var requested = (StateValue(game, "requested_marker_values") ?? "").Split(';');
            foreach (string entry in requested)
            {
                string[] pair = entry.Split('=');
                if (pair.Length == 2 && result.Options.MarkerValues.ContainsKey(pair[0]) &&
                    ((!result.Options.MarkerPatch) || (IsDependency(pair[0], result.Options) && ReadIniValue(text, pair[0]) == "1")))
                    result.Options.MarkerValues[pair[0]] = pair[1] == "1";
            }
        }
        else
        {
            foreach (string entry in (StateValue(game, "requested_marker_values") ?? "").Split(';'))
            {
                string[] pair = entry.Split('=');
                if (pair.Length == 2 && result.Options.MarkerValues.ContainsKey(pair[0])) result.Options.MarkerValues[pair[0]] = pair[1] == "1";
            }
            foreach (string entry in (StateValue(game, "requested_marker_parameters") ?? "").Split(';'))
            {
                string[] pair = entry.Split('='); decimal value;
                if (pair.Length != 2) continue;
                var parameter = MarkerCatalog.Parameters.FirstOrDefault(p => p.Key == pair[0]);
                if (parameter != null && decimal.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= parameter.Min && value <= parameter.Max)
                    result.Options.MarkerParameters[pair[0]] = pair[1];
            }
            decimal scale;
            if (decimal.TryParse(StateValue(game, "subtitle_scale"), NumberStyles.Float, CultureInfo.InvariantCulture, out scale) && scale >= .5m && scale <= 3m) result.Options.SubtitleScale = scale;
        }
        return result;
    }

    private static string StateValue(string game, string key)
    {
        string value = ReadState(game, key);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string MaintenanceMutex(string game)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
            return "Local\\SomJsDS2-" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFullPath(game).ToLowerInvariant()))).Replace("-", "").Substring(0, 24);
    }

    private static string ReadIniValue(string text, string key)
    {
        return ReadMarkerIniValue(text, key);
    }

    public static bool IsDependency(string key, InstallOptions options)
    {
        return options.Dlc && (key == "LoadASIPlugins" || key == "EnableSeveredDLC" || (options.NodeFix && key == "EnableHackerDLC"));
    }

    public static string DependencyDescription(InstallOptions options)
    {
        if (!options.Dlc) return "";
        return L.T("DLC 必需：LoadASIPlugins、EnableSeveredDLC。") +
            (options.NodeFix ? L.T(" 节点修正另需 EnableHackerDLC（开放黑客装备，不开放房间）。") : "");
    }

    private static void WriteSelectionState(string game, InstallOptions options, string fallbackIni = null)
    {
        string path = Path.Combine(game, "DS2SeveredOneClick.install.txt");
        string text = File.Exists(path) ? File.ReadAllText(path) : "version=11\r\n";
        var changes = new Dictionary<string, string> {
            {"version", "11"}, {"installer_release", ProductInfo.ReleaseVersion},
            {"selected_chinese", options.Chinese.ToString()}, {"selected_dlc", options.Dlc.ToString()},
            {"selected_marker", options.MarkerPatch.ToString()}, {"node_fix", (options.Dlc && options.NodeFix).ToString()},
            {"node_fix_preference", options.NodeFix.ToString()},
            {"requested_marker_parameters", string.Join(";", options.MarkerParameters.Select(p => p.Key + "=" + p.Value))},
            {"requested_marker_values", string.Join(";", options.MarkerValues.Select(p => p.Key + "=" + (p.Value ? "1" : "0")))},
            {"subtitle_scale", options.SubtitleScale.ToString("0.0#", CultureInfo.InvariantCulture)}
        };
        if (!string.IsNullOrWhiteSpace(options.DlcArchivePath)) changes["dlc_package_path"] = Path.GetFullPath(options.DlcArchivePath);
        string runtime = Path.Combine(game, "plugins", "SeveredPC.asi");
        if (options.Dlc && File.Exists(runtime)) changes["runtime_asi_sha256"] = Sha256(runtime);
        if (options.Dlc && File.Exists(Path.Combine(game, RuntimeConfig))) changes["owns_runtime_config"] = "True";
        string activeMarker = Path.Combine(game, "dinput8.dll"), offMarker = Path.Combine(game, DisabledMarker);
        if (StateValue(game, "marker_patch") == "True" && (File.Exists(activeMarker) || File.Exists(offMarker))) changes["marker_dll_sha256"] = Sha256(File.Exists(activeMarker) ? activeMarker : offMarker);
        string ini = Path.Combine(game, "MarkerPatch.ini");
        string remembered = File.Exists(ini) ? File.ReadAllText(ini) : fallbackIni;
        if (options.MarkerPatch) { changes["owns_marker_assets"] = "True"; changes["marker_assets_manifest_b64"] = MarkerAssetManifestBase64(); }
        if (remembered != null) changes["saved_marker_ini_b64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(remembered));
        if (File.Exists(ini)) changes["marker_values"] = string.Join(";", Regex.Matches(File.ReadAllText(ini), @"(?m)^\s*(\w+)\s*=\s*([01])\s*$")
            .Cast<Match>().Where(m => MarkerCatalog.Find(m.Groups[1].Value) != null).Select(m => m.Groups[1].Value + "=" + m.Groups[2].Value));
        foreach (var pair in changes)
        {
            string pattern = @"(?m)^" + Regex.Escape(pair.Key) + @"=[^\r\n]*";
            text = Regex.IsMatch(text, pattern) ? Regex.Replace(text, pattern, m => pair.Key + "=" + pair.Value) : text.TrimEnd() + "\r\n" + pair.Key + "=" + pair.Value + "\r\n";
        }
        File.WriteAllText(path, text, Encoding.UTF8);
    }

    private static string[] ManagedPaths(string game)
    {
        var names = new List<string> { "DS2DAT0.DAT", "DS2DAT2.DAT", "dinput8.dll", "MarkerPatch.ini", "DS2DAT2.DAT.severed",
            Path.Combine("plugins", "SeveredPC.asi"), RuntimeConfig,
            Path.Combine("plugins", "MarkerPatch.asi"), Path.Combine("plugins", "MarkerPatch.ini"), ManagerName, ManagerInfo };
        var paths = names.SelectMany(n => new[] { n, n + ".ds2oneclick.bak" }).ToList();
        paths.AddRange(new[] { "DS2SeveredOneClick.install.txt", DisabledMarker,
            Path.Combine("plugins", "MarkerPatch.asi.ds2oneclick-disabled"), Path.Combine("plugins", "MarkerPatch.ini.ds2oneclick-disabled") });
        paths.AddRange(MarkerAssetPaths(game));
        paths.AddRange(GeneratedOutputPaths);
        return paths.ToArray();
    }

    private static void AssertPlain(string game, string relative)
    {
        string root = Path.GetFullPath(game).TrimEnd('\\');
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (path != root && !path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(L.T("维护路径越界，已停止。"));
        for (string p = path; p != null && (p == root || p.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)); p = Path.GetDirectoryName(p))
            if ((File.Exists(p) || Directory.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(L.T("维护操作不跟随目录链接：") + p);
    }

    private static void Transaction(string game, IEnumerable<string> names, Action action, Action<string> log)
    {
        EnsureGameStopped();
        string mutexName = MaintenanceMutex(game);
        using (var mutex = new Mutex(false, mutexName))
        {
            bool acquired;
            try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new IOException(L.T("另一个安装器正在维护这个游戏目录，请稍后重试。"));
            try
            {
                string[] paths = names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                foreach (string name in paths) AssertPlain(game, name);
                long bytes = paths.Where(n => File.Exists(Path.Combine(game, n))).Sum(n => new FileInfo(Path.Combine(game, n)).Length);
                CheckFreeSpace(game, bytes + 512L * 1024 * 1024);
                string temp = Path.Combine(game, ".ds2oneclick-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temp);
                log(L.T("正在建立维护恢复点：") + (bytes / (1024.0 * 1024 * 1024)).ToString("0.0", CultureInfo.InvariantCulture) + " GiB");
                var saved = new Dictionary<string, string>();
                bool prepared = false, keep = false;
                try
                {
                    foreach (string name in paths)
                    {
                        string path = Path.Combine(game, name), copy = Path.Combine(temp, name);
                        if (!File.Exists(path)) { saved[name] = null; continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(copy));
                        File.Copy(path, copy);
                        if (Sha256(path) != Sha256(copy)) throw new IOException(L.T("维护备份校验失败：") + name);
                        saved[name] = copy;
                    }
                    prepared = true;
                    log(L.T("维护恢复点已校验，开始应用更改。"));
                    try { action(); }
                    catch
                    {
                        foreach (var pair in saved.Reverse())
                        {
                            try
                            {
                                string path = Path.Combine(game, pair.Key);
                                AssertPlain(game, pair.Key);
                                if (pair.Value == null) { if (File.Exists(path)) File.Delete(path); }
                                else
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                                    if (File.Exists(path) && Sha256(path) == Sha256(pair.Value)) continue;
                                    File.Copy(pair.Value, path, true);
                                    if (Sha256(path) != Sha256(pair.Value)) throw new IOException(pair.Key);
                                }
                            }
                            catch (Exception ex) { keep = true; log(L.T("维护回退未完成：") + pair.Key + " — " + ex.Message); }
                        }
                        if (keep) log(L.T("请保留维护恢复目录：") + temp);
                        else log(L.T("已恢复本次更改前的文件和安装记录。"));
                        throw;
                    }
                }
                finally
                {
                    if (!keep) DeleteTemp(temp, game);
                    if (prepared)
                    {
                        string plugins = Path.Combine(game, "plugins");
                        if (Directory.Exists(plugins) && !Directory.EnumerateFileSystemEntries(plugins).Any()) Directory.Delete(plugins);
                    }
                }
            }
            finally { mutex.ReleaseMutex(); }
        }
    }

    private static void ValidateMaintenanceVersion(string game)
    {
        int version;
        if (!int.TryParse(StateValue(game, "version"), out version) || version < 1 || version > 11)
            throw new InvalidDataException(L.T("无法识别此安装记录的版本，请保留文件并尝试全新安装。"));
    }
    private static bool IsKnownMarkerHash(string hash) { return hash == MarkerDllSha || hash == LegacyMarkerDllSha; }
    private static string MarkerHashForUninstall(string game)
    {
        string recorded = StateValue(game, "marker_dll_sha256");
        if (recorded != null && !IsKnownMarkerHash(recorded)) throw new InvalidDataException(L.T("安装记录中的 MarkerPatch 版本不受支持，请保留文件与备份。"));
        foreach (string name in new[] { "dinput8.dll", DisabledMarker })
        {
            string path = Path.Combine(game, name);
            if (File.Exists(path)) { string hash = Sha256(path); if (IsKnownMarkerHash(hash)) return hash; }
        }
        return recorded ?? LegacyMarkerDllSha;
    }
    private static void PreflightManaged(string game)
    {
        ValidateMaintenanceVersion(game);
        bool runtime = StateValue(game, "owns_dlc_runtime") == "True" || StateValue(game, "dlc") == "True";
        PreflightUninstall(game, StateValue(game, "chinese") == "True", runtime, StateValue(game, "marker_patch") == "True",
            StateValue(game, "owns_dat2") == "True" || StateValue(game, "dlc") == "True", StateValue(game, "installed_dat2_sha256"),
            runtime ? RuntimeHashForUninstall(game) : null, StateValue(game, "owns_runtime_config") == "True", MarkerHashForUninstall(game));
    }

    public static void ApplySelection(string gamePath, InstallOptions options, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        EnsureGameStopped();
        InstalledSelection current = InspectInstallation(game);
        if (current.HasRecord) ValidateMaintenanceVersion(game);
        bool ready = current.MarkerIsCurrent && File.Exists(Path.Combine(game, "MarkerPatch.ini")) && (!current.Options.Dlc ||
            (File.Exists(Path.Combine(game, "DS2DAT2.DAT")) && File.Exists(Path.Combine(game, RuntimeConfig)) && File.Exists(Path.Combine(game, "plugins", "SeveredPC.asi")) && Sha256(Path.Combine(game, "plugins", "SeveredPC.asi")) == SeveredAsiSha));
        bool same = ready && current.HasRecord && current.Version == ProductInfo.ReleaseVersion &&
            current.Options.Chinese == options.Chinese && current.Options.Dlc == options.Dlc && (current.Options.MarkerPatch == options.MarkerPatch || options.Dlc);
        if (same && current.MarkerPresent && !current.ExternalMarker)
        {
            SaveConfiguration(game, options, log);
            return;
        }
        string previousIni = current.HasRecord && !current.ExternalMarker && File.Exists(Path.Combine(game, "MarkerPatch.ini")) ? File.ReadAllText(Path.Combine(game, "MarkerPatch.ini")) : null;
        if (previousIni == null && current.HasRecord)
        {
            string remembered = StateValue(game, "saved_marker_ini_b64");
            if (!string.IsNullOrEmpty(remembered) && remembered.Length < 256 * 1024)
                try { previousIni = Encoding.UTF8.GetString(Convert.FromBase64String(remembered)); } catch (FormatException) { }
        }
        bool any = options.Chinese || options.Dlc || options.MarkerPatch;
        if (options.Dlc)
        {
            string archive = string.IsNullOrWhiteSpace(options.DlcArchivePath) ? GameLocations.FindPackage(AppDomain.CurrentDomain.BaseDirectory, game) : options.DlcArchivePath;
            if (string.IsNullOrEmpty(archive) || !File.Exists(archive)) throw new FileNotFoundException(L.T("更改或升级 DLC 需要 Severed.marker，请先选择数据包。"));
            // Validate the package contract before changes. InstallDlc verifies the payload within the outer recovery transaction.
            string verify = Path.Combine(game, ".ds2oneclick-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(verify);
            try { string seven = Path.Combine(verify, "7z.exe"); ExtractResource("DS2_7Z_EXE", seven); ExtractResource("DS2_7Z_DLL", Path.Combine(verify, "7z.dll")); ReadDlcPackage(archive, verify, seven); }
            finally { DeleteTemp(verify, game); }
            options.DlcArchivePath = archive;
        }
        if (current.HasRecord) PreflightManaged(game);
        Transaction(game, ManagedPaths(game), () =>
        {
            if (current.HasRecord)
            {
                log(L.T("正在迁移旧安装并更改组件；保留原始备份与当前自定义设置。"));
                UninstallCore(game, log);
            }
            if (any)
            {
                Install(game, options, log);
                if (options.MarkerPatch && previousIni != null)
                    File.WriteAllText(Path.Combine(game, "MarkerPatch.ini"), ConfigureMarkerPatch(previousIni, options), Encoding.UTF8);
                WriteSelectionState(game, options, previousIni);
                if (current.MarkerPresent && !current.MarkerEnabled && !current.ExternalMarker && (options.Dlc || options.MarkerPatch)) SetMarkerEnabledCore(game, false, log);
                if (options.DeployManager) DeployManagerCore(game, log);
            }
            log(L.T("更改安装已完成。"));
        }, log);
    }

    public static void SaveConfiguration(string gamePath, InstallOptions options, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        InstalledSelection current = InspectInstallation(game);
        if (!current.HasRecord || StateValue(game, "marker_patch") != "True" || !current.MarkerPresent || current.ExternalMarker)
            throw new InvalidOperationException(L.T("只允许直接保存本安装器管理的 MarkerPatch；外置版本请先核对兼容性。"));
        if (current.Options.Chinese != options.Chinese || current.Options.Dlc != options.Dlc || (current.Options.MarkerPatch != options.MarkerPatch && !current.Options.Dlc))
            throw new InvalidOperationException(L.T("组件选择发生变化，请使用“更改安装”；仅保存设置不会增删组件。"));
        ValidateMaintenanceVersion(game);
        if (!current.MarkerIsCurrent) throw new InvalidOperationException(L.T("请先使用更改安装升级随包 MarkerPatch，再保存新版设置。"));
        Transaction(game, new[] { "MarkerPatch.ini", "DS2SeveredOneClick.install.txt", RuntimeConfig, RuntimeConfig + ".ds2oneclick.bak", ManagerName, ManagerName + ".ds2oneclick.bak", ManagerInfo }.Concat(MarkerAssetPaths(game)), () =>
        {
            string ini = Path.Combine(game, "MarkerPatch.ini");
            if (!File.Exists(ini)) throw new FileNotFoundException(L.T("MarkerPatch.ini 缺失，请使用更改安装修复。"));
            File.WriteAllText(ini, ConfigureMarkerPatch(File.ReadAllText(ini), options), Encoding.UTF8);
            if (current.Options.Dlc)
            {
                if (RuntimeHashForUninstall(game) != SeveredAsiSha || !File.Exists(Path.Combine(game, "plugins", "SeveredPC.asi")) || Sha256(Path.Combine(game, "plugins", "SeveredPC.asi")) != SeveredAsiSha)
                    throw new InvalidOperationException(L.T("旧 DLC 插件需要先通过更改安装升级，再保存节点选项。"));
                InstallOwnedText(game, RuntimeConfig, RuntimeConfiguration(options.NodeFix), null, new List<Action>(), log);
            }
            if (options.MarkerPatch) InstallMarkerAssets(game, new List<Action>(), log);
            WriteSelectionState(game, options);
            if (options.DeployManager) DeployManagerCore(game, log);
            log(L.T("设置已保存，重新启动游戏后生效；没有改写 DAT。"));
        }, log);
    }

    private static string RuntimeConfiguration(bool enabled)
    {
        return "[Shop]\r\nProfile=" + (enabled ? "1" : "0") + "\r\n[Diagnostics]\r\nTraceMessages=0\r\n";
    }

    public static void SetMarkerEnabled(string gamePath, bool enabled, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        Transaction(game, new[] { "dinput8.dll", DisabledMarker }, () => SetMarkerEnabledCore(game, enabled, log), log);
    }

    private static void SetMarkerEnabledCore(string game, bool enabled, Action<string> log)
    {
        ValidateMaintenanceVersion(game);
        if (StateValue(game, "marker_patch") != "True") throw new InvalidOperationException(L.T("此 MarkerPatch 不属于本安装器，不能自动切换启动状态。"));
        string active = Path.Combine(game, "dinput8.dll"), off = Path.Combine(game, DisabledMarker);
        if (File.Exists(active) && File.Exists(off)) throw new IOException(L.T("MarkerPatch 启动文件存在冲突，已保留两个文件。"));
        foreach (string file in new[] { active, off }) if (File.Exists(file) && !IsKnownMarkerHash(Sha256(file)))
            throw new IOException(L.T("检测到外置或未知 dinput8.dll，已保留，不切换它的启动状态。"));
        string from = enabled ? off : active, to = enabled ? active : off;
        if (File.Exists(from)) File.Move(from, to);
        if (!File.Exists(to)) throw new FileNotFoundException(L.T("随包 MarkerPatch 文件缺失，请使用更改安装修复。"));
        log(enabled ? L.T("已启用随包 MarkerPatch，重新启动游戏后生效。") : L.T("已停用随包 MarkerPatch；DLC 入口与依赖它的运行修正将不再生效。"));
    }

    public static void DeployManager(string gamePath, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        Transaction(game, new[] { ManagerName, ManagerName + ".ds2oneclick.bak", ManagerInfo }, () => DeployManagerCore(game, log), log);
    }

    private static void DeployManagerCore(string game, Action<string> log)
    {
        string source = Assembly.GetExecutingAssembly().Location, target = Path.Combine(game, ManagerName);
        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            InstallOwnedBytes(game, ManagerName, File.ReadAllBytes(source), Sha256(source), new List<Action>(), log);
        File.WriteAllText(Path.Combine(game, ManagerInfo), "{\"version\":\"" + ProductInfo.ReleaseVersion + "\",\"sha256\":\"" + Sha256(target) + "\"}", Encoding.UTF8);
        log(L.T("根目录管理器已就绪：") + target);
    }

    public static void Uninstall(string gamePath, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        if (!File.Exists(Path.Combine(game, "DS2SeveredOneClick.install.txt")))
        {
            if (!HasKnownResidue(game)) throw new InvalidOperationException(L.T("找不到本工具的安装记录，无法确认文件归属，已停止卸载并保留现有文件。"));
            Transaction(game, GeneratedOutputPaths, () => CleanupKnownResidue(game, log), log);
            log(L.T("已清理可确认的历史卸载残留。")); return;
        }
        PreflightManaged(game);
        Transaction(game, ManagedPaths(game), () => UninstallCore(game, log), log);
        if (File.Exists(Path.Combine(game, ManagerName))) log(L.T("补丁已卸载；根目录管理器保留，可关闭程序后手动删除。"));
    }

    public static string MaintenanceFailure(Exception ex)
    {
        return ex.Message + "\r\n\r\n" + L.T("如直接升级或更改安装异常，请先备份存档，再尝试卸载后全新安装。仍有异常时，使用 Steam 验证完整性或 EA app 修复；平台修复可能保留第三方插件，请核对后再安装。不要删除原始备份。 ");
    }
}
