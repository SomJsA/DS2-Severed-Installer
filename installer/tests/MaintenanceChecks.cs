using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

class MaintenanceChecks
{
    static Assembly asm; static Type engine, optionType; static string root, exe;
    static BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static int checks;
    static object Invoke(Type type, string method, params object[] args)
    {
        try { return type.GetMethod(method, flags).Invoke(null, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
    }
    static object Call(string name, params object[] args) { return Invoke(engine, name, args); }
    static object Get(object value, string name) { return value.GetType().GetField(name).GetValue(value); }
    static void Set(object value, string name, object data) { value.GetType().GetField(name).SetValue(value, data); }
    static string Hash(string file) { using (var s=File.OpenRead(file)) using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant(); }
    static Action<string> Log(string name) { return text => File.AppendAllText(Path.Combine(root, name+".log"),text+Environment.NewLine,Encoding.UTF8); }
    static void Check(bool pass, string name) { if(!pass)throw new Exception("FAIL "+name); ++checks; Console.WriteLine("PASS "+name); }
    static void Refused(Action action, string name) { bool threw=false;try{action();}catch{threw=true;}Check(threw,name); }
    static object Options(bool chinese, bool dlc, bool marker, bool node=false)
    {
        var o=Activator.CreateInstance(optionType,true);Set(o,"Chinese",chinese);Set(o,"Dlc",dlc);Set(o,"MarkerPatch",marker);Set(o,"NodeFix",node);return o;
    }
    static object Current(string game) { return Get(Call("InspectInstallation",game),"Options"); }
    static string Fixture(string name)
    {
        string game=Path.Combine(root,name);Directory.CreateDirectory(game);
        File.WriteAllText(Path.Combine(game,"deadspace2.exe"),"MZ: isolated non-running test fixture");
        File.WriteAllText(Path.Combine(game,"DS2DAT0.DAT"),"foreign base data: preserve exactly");
        File.WriteAllText(Path.Combine(game,"DS2DAT2.DAT"),"foreign DLC data: preserve exactly");
        return game;
    }
    static Dictionary<string,string> Snapshot(string game) { return Directory.GetFiles(game,"*",SearchOption.AllDirectories).ToDictionary(p=>p.Substring(game.Length),p=>Hash(p)); }
    static bool Equal(Dictionary<string,string>a, Dictionary<string,string>b) { return a.Count==b.Count && a.All(p=>b.ContainsKey(p.Key)&&b[p.Key]==p.Value); }
    static bool Ini(string game,string key,string value) { return System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(Path.Combine(game,"MarkerPatch.ini")),@"(?m)^\s*"+key+@"\s*=\s*"+value+@"\s*$"); }
    static void Small()
    {
        string broken="[General]\r\nLoadASIPlugins = 0 ; user note\r\n[Display]\r\nFONTSCALINGFACTOR = 1.7 ; display note\r\n[Unknown]\r\nKeep=unchanged\r\n";
        string fixedIni=(string)Invoke(engine,"SetIni",broken,"EnableHackerDLC","1");
        Check(fixedIni.Contains("[DLC]") && (string)Invoke(engine,"ReadIniValue",fixedIni,"EnableHackerDLC")=="1","missing INI key inserted into its correct section");
        Check((string)Invoke(engine,"ReadIniValue",fixedIni,"FontScalingFactor")=="1.7","INI reads honor case, CRLF and inline comments");
        fixedIni=(string)Invoke(engine,"SetIni",fixedIni,"FontScalingFactor","1.4");
        Check(fixedIni.Contains("display note") && fixedIni.Contains("Keep=unchanged") && (string)Invoke(engine,"ReadIniValue",fixedIni,"FontScalingFactor")=="1.4","INI changes preserve inline comments and unknown sections");
        string template=(string)Invoke(engine,"ReadResourceText","DS2_MARKER_INI");
        string minimal=(string)Invoke(engine,"ConfigureMarkerPatch",template,Options(false,true,false,false));
        Check(System.Text.RegularExpressions.Regex.IsMatch(minimal,@"(?m)^EnableHackerDLC\s*=\s*0\s*$") && System.Text.RegularExpressions.Regex.IsMatch(minimal,@"(?m)^EnableIgnitionRooms\s*=\s*0\s*$"),"DLC-only configuration ignores inactive optional preferences");
        string game=Fixture("settings");var original=Snapshot(game);
        Call("ApplySelection",game,Options(false,false,true),Log("settings-install"));
        string dat0=Hash(Path.Combine(game,"DS2DAT0.DAT")),dat2=Hash(Path.Combine(game,"DS2DAT2.DAT"));
        File.WriteAllText(Path.Combine(game,"MarkerPatch.ini"),File.ReadAllText(Path.Combine(game,"MarkerPatch.ini")).Replace("\r\n","\n").Replace("\n","\r\n"));
        File.AppendAllText(Path.Combine(game,"MarkerPatch.ini"),"\r\n; custom retained comment\r\n[ExternalPreferences]\r\nPreserveUnknown=some-value\r\n");
        var o=Current(game);Set(o,"SubtitleScale",1.2m);
        ((Dictionary<string,bool>)Get(o,"MarkerValues"))["RawMouseInput"]=false;
        ((Dictionary<string,string>)Get(o,"MarkerParameters"))["MaxAnisotropy"]="16";
        Call("SaveConfiguration",game,o,Log("settings-save"));
        var read=Current(game);
        Check((decimal)Get(read,"SubtitleScale")==1.2m,"load actual custom subtitle scale");
        Check(((Dictionary<string,string>)Get(read,"MarkerParameters"))["MaxAnisotropy"]=="16","persist and reload numeric parameter");
        Check(!((Dictionary<string,bool>)Get(read,"MarkerValues"))["RawMouseInput"],"persist and reload feature switch");
        Check(File.ReadAllText(Path.Combine(game,"MarkerPatch.ini")).Contains("PreserveUnknown=some-value"),"unknown INI sections and comments retained");
        Check(dat0==Hash(Path.Combine(game,"DS2DAT0.DAT")) && dat2==Hash(Path.Combine(game,"DS2DAT2.DAT")),"settings save leaves both DAT files unchanged");
        Call("SetMarkerEnabled",game,false,Log("startup-off"));
        Check(!File.Exists(Path.Combine(game,"dinput8.dll")) && File.Exists(Path.Combine(game,"dinput8.dll.ds2oneclick-off")),"disable owned MarkerPatch startup");
        Call("SaveConfiguration",game,Current(game),Log("save-while-off"));
        Check(!File.Exists(Path.Combine(game,"dinput8.dll")),"saving settings does not re-enable disabled loader");
        Call("SetMarkerEnabled",game,true,Log("startup-on"));
        Check(File.Exists(Path.Combine(game,"dinput8.dll")),"enable owned MarkerPatch startup");
        File.WriteAllText(Path.Combine(game,"dinput8.dll"),"external loader: preserve");var before=Snapshot(game);
        Check((bool)Get(Call("InspectInstallation",game),"ExternalMarker"),"external loader detected");
        Refused(()=>Call("SetMarkerEnabled",game,false,Log("external-refusal")),"external loader startup toggle refused");
        Check(Equal(before,Snapshot(game)),"external loader and all files preserved after refusal");
        string managed=Path.Combine(root,"resource-marker.dll");Invoke(engine,"ExtractResource","DS2_MARKER_DLL",managed);File.Copy(managed,Path.Combine(game,"dinput8.dll"),true);
        Call("SetMarkerEnabled",game,false,Log("off-before-uninstall"));
        Call("Uninstall",game,Log("disabled-uninstall"));
        Check(Equal(original,Snapshot(game)),"uninstall from disabled state restores original fixture");

        foreach(int version in new[]{1,5,8,9})
        {
            game=Fixture("legacy-"+version);original=Snapshot(game);
            File.WriteAllText(Path.Combine(game,"dinput8.dll"),"pre-existing third-party loader");
            File.WriteAllText(Path.Combine(game,"MarkerPatch.ini"),"[Original]\r\noriginal=1\r\n");
            var preExisting=Snapshot(game);
            Call("ApplySelection",game,Options(false,false,true),Log("legacy-seed-"+version));
            File.AppendAllText(Path.Combine(game,"MarkerPatch.ini"),"\r\n; keep custom\r\nunknown_extra=retained\r\n");
            var options=Current(game);Set(options,"SubtitleScale",1.3m);Call("SaveConfiguration",game,options,Log("legacy-custom-"+version));
            string backup=Hash(Path.Combine(game,"dinput8.dll.ds2oneclick.bak"));
            if(version==9) { File.Copy(@"E:\DS2-Severed-Patch\maintenance\installer-maintenance-20261102\before-marker-3.0.7\build-deps\dinput8.dll",Path.Combine(game,"dinput8.dll"),true); Call("SetMarkerEnabled",game,false,Log("legacy-disable-9")); }
            string state=Path.Combine(game,"DS2SeveredOneClick.install.txt");
            var lines=File.ReadAllLines(state).Where(l=>!l.StartsWith("installer_release=")&&!l.StartsWith("selected_")&&!l.StartsWith("requested_")&&!l.StartsWith("node_fix_preference=")&&!l.StartsWith("saved_marker_ini_b64=")&&!l.StartsWith("marker_dll_sha256="));
            File.WriteAllLines(state,lines.Select(l=>l.StartsWith("version=")?"version="+version:l).ToArray(),Encoding.UTF8);
            Call("ApplySelection",game,Current(game),Log("upgrade-"+version));
            if(version==9)Check(!File.Exists(Path.Combine(game,"dinput8.dll")) && File.Exists(Path.Combine(game,"dinput8.dll.ds2oneclick-off")),"upgrade preserves disabled MarkerPatch state");
            Check(File.ReadAllText(state).Contains("version=15"),"migrate recognized legacy record "+version);
            Check((decimal)Get(Current(game),"SubtitleScale")==1.3m,"legacy custom scale retained "+version);
            Check(File.ReadAllText(Path.Combine(game,"MarkerPatch.ini")).Contains("unknown_extra=retained"),"legacy unknown INI entry retained "+version);
            Check(Hash(Path.Combine(game,"dinput8.dll.ds2oneclick.bak"))==backup,"original third-party backup preserved "+version);
            Call("Uninstall",game,Log("legacy-remove-"+version));
            Check(Equal(preExisting,Snapshot(game)),"legacy upgrade/uninstall restores pre-existing files "+version);
        }
        game=Fixture("same-external-module");
        Invoke(engine,"ExtractResource","DS2_MARKER_DLL",Path.Combine(game,"dinput8.dll"));
        File.WriteAllText(Path.Combine(game,"MarkerPatch.ini"),template);
        var externalOriginal=Snapshot(game);
        var sameUndo=new List<Action>();string sameTarget=Path.Combine(game,"dinput8.dll");
        Invoke(engine,"InstallOwnedBytes",game,"dinput8.dll",File.ReadAllBytes(sameTarget),Hash(sameTarget),sameUndo,(Action<string>)(s0=>{}));
        for(int i=sameUndo.Count-1;i>=0;--i)sameUndo[i]();
        Check(Equal(externalOriginal,Snapshot(game)),"rollback of identical pre-existing file preserves original and removes only its new backup");
        Call("ApplySelection",game,Options(false,false,true),Log("same-external-install"));
        Check(File.Exists(Path.Combine(game,"dinput8.dll.ds2oneclick.bak")),"identical pre-existing loader receives an original backup");
        Call("Uninstall",game,Log("same-external-remove"));
        Check(Equal(externalOriginal,Snapshot(game)),"identical external loader and configuration restored after uninstall");

        game=Fixture("external-assets");Directory.CreateDirectory(Path.Combine(game,"achievements","img"));File.WriteAllText(Path.Combine(game,"achievements","img","0.png"),"external custom icon: restore exactly");
        var assetBefore=Snapshot(game);Call("ApplySelection",game,Options(false,false,true),Log("external-assets-install"));
        Check(File.Exists(Path.Combine(game,"achievements","img","0.png.ds2oneclick.bak")),"external achievement asset backed up");
        Call("Uninstall",game,Log("external-assets-remove"));Check(Equal(assetBefore,Snapshot(game)),"external achievement asset restored");
        game=Fixture("changed-assets");Call("ApplySelection",game,Options(false,false,true),Log("changed-assets-seed"));File.WriteAllText(Path.Combine(game,"achievements","img","0.png"),"changed after install");
        var changedAsset=Snapshot(game);Refused(()=>Call("Uninstall",game,Log("changed-assets-refused")),"externally modified managed asset blocks uninstall");Check(Equal(changedAsset,Snapshot(game)),"modified asset refusal preserves all files");
        Check(((System.Collections.ICollection)Call("GetMarkerPatchOptions")).Count==61,"MarkerPatch 3.0.7 exposes all 61 feature switches");

        game=Fixture("remembered-settings");Call("ApplySelection",game,Options(false,false,true),Log("remembered-seed"));
        var remembered=Current(game);Set(remembered,"SubtitleScale",1.7m);Set(remembered,"NodeFix",false);
        ((Dictionary<string,bool>)Get(remembered,"MarkerValues"))["RawMouseInput"]=false;
        ((Dictionary<string,string>)Get(remembered,"MarkerParameters"))["MaxAnisotropy"]="16";
        string rememberedIni=File.ReadAllText(Path.Combine(game,"MarkerPatch.ini"))+"\r\n[CustomPreserved]\r\nRetain=forever\r\n";
        File.Delete(Path.Combine(game,"MarkerPatch.ini"));Set(remembered,"MarkerPatch",false);
        Invoke(engine,"WriteSelectionState",game,remembered,rememberedIni);
        var recalled=Current(game);
        Check((decimal)Get(recalled,"SubtitleScale")==1.7m && !((Dictionary<string,bool>)Get(recalled,"MarkerValues"))["RawMouseInput"] && ((Dictionary<string,string>)Get(recalled,"MarkerParameters"))["MaxAnisotropy"]=="16","remember options when optional MarkerPatch INI is absent");
        Check(!(bool)Get(recalled,"NodeFix"),"node preference survives an absent DLC component");
        Set(recalled,"MarkerPatch",true);Call("ApplySelection",game,recalled,Log("remembered-repair"));
        Check(File.ReadAllText(Path.Combine(game,"MarkerPatch.ini")).Contains("Retain=forever"),"reinstall restores saved unknown INI settings");

        game=Fixture("manager");Call("ApplySelection",game,Options(false,false,true),Log("manager-seed"));
        Call("DeployManager",game,Log("deploy-manager"));
        Check(Hash(Path.Combine(game,"DS2PatchManager.exe"))==Hash(exe),"root manager is exact current EXE");
        Call("Uninstall",game,Log("manager-uninstall"));
        Check(File.Exists(Path.Combine(game,"DS2PatchManager.exe")) && !File.Exists(Path.Combine(game,"DS2SeveredOneClick.install.txt")),"manager remains available after patch removal");

        game=Fixture("node-switch");Call("ApplySelection",game,Options(false,false,true),Log("node-seed"));
        string native=Path.Combine(game,"plugins","SeveredPC.asi");Directory.CreateDirectory(Path.GetDirectoryName(native));Invoke(engine,"ExtractResource","DS2_SEVERED_ASI",native);
        File.WriteAllText(Path.Combine(game,"plugins","SeveredRuntimeFixes.ini"),"[Shop]\r\nProfile=0\r\n[Diagnostics]\r\nTraceMessages=0\r\n");
        string nodeState=Path.Combine(game,"DS2SeveredOneClick.install.txt");string nodeText=File.ReadAllText(nodeState).Replace("selected_dlc=False","selected_dlc=True").Replace("dlc=False","dlc=True").Replace("owns_dlc_runtime=False","owns_dlc_runtime=True");
        Call("InstallBugPlugins",game,new List<Action>(),Log("node-standalone-seed"));
        nodeText=string.Join("\r\n",nodeText.Split(new[]{"\r\n","\n"},StringSplitOptions.None).Where(l=>!l.StartsWith("owns_bug_plugins=")&&!l.StartsWith("bug_plugin_")))+"\r\n"+(string)Call("BugPluginState",game,true);
        File.WriteAllText(nodeState,nodeText,Encoding.UTF8);
        var nodeOptions=Current(game);Set(nodeOptions,"NodeFix",true);var switches=(Dictionary<string,bool>)Get(nodeOptions,"MarkerValues");foreach(var key in switches.Keys.ToArray())switches[key]=false;
        var dataBefore=new[]{Hash(Path.Combine(game,"DS2DAT0.DAT")),Hash(Path.Combine(game,"DS2DAT2.DAT"))};
        Call("SaveConfiguration",game,nodeOptions,Log("node-enable"));
        Check(Ini(game,"LoadASIPlugins","1") && Ini(game,"EnableSeveredDLC","1") && Ini(game,"EnableHackerDLC","1"),"node option enables required three switches");
        Check(Ini(game,"EnableIgnitionRooms","0"),"node option does not enable Ignition rooms");
        nodeOptions=Current(game);Check(!((Dictionary<string,bool>)Get(nodeOptions,"MarkerValues"))["EnableHackerDLC"],"original optional Hacker choice retained separately from dependency");
        Set(nodeOptions,"NodeFix",false);Call("SaveConfiguration",game,nodeOptions,Log("node-disable"));
        Check(Ini(game,"EnableHackerDLC","0") && Ini(game,"LoadASIPlugins","1") && Ini(game,"EnableSeveredDLC","1"),"disabling node option releases Hacker dependency while retaining DLC dependencies");
        Check(File.ReadAllText(Path.Combine(game,"plugins","SeveredRuntimeFixes.ini")).Contains("Profile=0"),"disabled node option reaches runtime configuration");
        Check(dataBefore[0]==Hash(Path.Combine(game,"DS2DAT0.DAT"))&&dataBefore[1]==Hash(Path.Combine(game,"DS2DAT2.DAT")),"node switching never rewrites DAT");
        File.WriteAllText(native,"unknown future runtime");before=Snapshot(game);nodeOptions=Current(game);Set(nodeOptions,"SubtitleScale",1.5m);
        Refused(()=>Call("SaveConfiguration",game,nodeOptions,Log("late-rollback")),"late runtime validation failure rejected");
        Check(Equal(before,Snapshot(game)),"late save failure rolls back INI and record exactly");

        Type updater=asm.GetType("UpdateChecker");
        string json="[{\"tag_name\":\"v2026.11.10\",\"body\":\"LATEST_NOTE\",\"assets\":[]},{\"tag_name\":\"v2026.11.3\",\"body\":\"MIDDLE_NOTE\",\"assets\":[]},{\"tag_name\":\"v2026.11.2\",\"body\":\"OLD_NOTE\",\"assets\":[]},{\"tag_name\":\"v2026.11.99\",\"body\":\"PREVIEW_NOTE\",\"prerelease\":true,\"assets\":[]}]";
        var report=Invoke(updater,"ParseInstallerUpdates",json,"2026.11.2");string notes=(string)Get(report,"CombinedNotes");
        Check(notes.IndexOf("LATEST_NOTE")<notes.IndexOf("MIDDLE_NOTE") && !notes.Contains("OLD_NOTE") && !notes.Contains("PREVIEW_NOTE"),"merged changelog includes only newer stable releases in version order");
        Check(updater.GetMethod("DownloadVerified",flags)==null,"update checker has no program download or self-update method");

        Type product=asm.GetType("ProductInfo");
        foreach(string language in new[]{"zh-CN","en"})
        {
            Invoke(asm.GetType("L"),"Initialize",language);
            string history=(string)product.GetProperty("VersionHistory",flags).GetValue(null,null);
            string completed=(string)Invoke(product,"InstallationComplete",true);
            Check(history.Contains("2026.11.1")&&history.Contains("2026.11.2")&&history.Contains("Playable"),"cumulative startup log "+language);
            Check(completed.Contains("Severed") && completed.Contains(language=="en"?"New Game":"新游戏"),"DLC completion entry instruction "+language);
        }
    }
    [STAThread] static int Main(string[] args)
    {
        exe=Path.GetFullPath(args[0]);root=Path.GetFullPath(args[1]);
        if(!root.StartsWith(@"E:\DS2-Severed-Patch\work\",StringComparison.OrdinalIgnoreCase))throw new Exception("Test root outside project");
        Directory.CreateDirectory(root);asm=Assembly.LoadFrom(exe);engine=asm.GetType("InstallerEngine");optionType=asm.GetType("InstallOptions");
        Invoke(asm.GetType("L"),"Initialize","en");
        Small();File.WriteAllText(Path.Combine(root,"checks-passed.txt"),checks.ToString());Console.WriteLine("ALL SMALL CHECKS PASSED: "+checks);return 0;
    }
}

