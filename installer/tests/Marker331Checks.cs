using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
class Marker331Checks
{
    static BindingFlags stat=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,inst=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static int checks;static Type engine;static Assembly asm;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static object Call(Type type,string name,params object[] args){try{return type.GetMethod(name,stat).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
    static object Field(object obj,string name){return obj.GetType().GetField(name,inst).GetValue(obj);}
    static void Set(object obj,string name,object value){obj.GetType().GetField(name,inst).SetValue(obj,value);}
    static string Hash(string path){using(var file=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
    static string ResourceHash(string name){using(var file=asm.GetManifestResourceStream(name))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
    static object Options(Assembly assembly){var value=Activator.CreateInstance(assembly.GetType("InstallOptions"),true);Set(value,"Chinese",false);Set(value,"Dlc",false);Set(value,"MarkerPatch",true);Set(value,"NodeFix",false);return value;}
    static Dictionary<string,string> Snapshot(string root){return Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(p=>p.Substring(root.Length),Hash);}
    static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b){return a.Count==b.Count&&a.All(p=>b.ContainsKey(p.Key)&&b[p.Key]==p.Value);}
    static string Ini(string text,string key){return (string)Call(engine,"ReadIniValue",text,key);}
    [STAThread]static int Main(string[]args)
    {
        Console.OutputEncoding=new UTF8Encoding(false);
        try{
            string root=Path.GetFullPath(args[2]);
            if(!root.StartsWith(@"D:\DS2-Marker331-Candidate-20261126\checks\",StringComparison.OrdinalIgnoreCase)||Directory.Exists(root))throw new Exception("Fresh isolated candidate test root required");
            Directory.CreateDirectory(root);asm=Assembly.LoadFile(Path.GetFullPath(args[0]));engine=asm.GetType("InstallerEngine");
            Call(asm.GetType("L"),"Initialize","en");
            Check(ResourceHash("DS2_MARKER_DLL")=="d37df6d429c6c1671d93b1b84c03c2eb516957de735ee3abb4d3d93200b34745","official 3.3.1 DLL embedded unchanged");
            Check(ResourceHash("DS2_MARKER_ASSETS")=="f3f14bcafaa4f327bd2251af7486d1b70aa315e42670ee4e34a867693ae1face","official 3.3.1 archive embedded unchanged");
            Check((string)asm.GetType("UpdateChecker").GetField("BundledMarkerVersion",stat).GetRawConstantValue()=="3.3.1","updater compares against bundled 3.3.1");
            var catalog=asm.GetType("MarkerCatalog");var features=((IEnumerable)catalog.GetField("Features",stat).GetValue(null)).Cast<object>().ToList();
            var parameters=((IEnumerable)catalog.GetField("Parameters",stat).GetValue(null)).Cast<object>().ToList();
            Check(features.Count==66&&parameters.Count==15,"all 66 switches and 15 parameters exposed");
            string template=(string)Call(engine,"ReadResourceText","DS2_MARKER_INI");
            var keys=features.Concat(parameters).Select(p=>(string)Field(p,"Key")).Concat(new[]{"FontScalingFactor"}).OrderBy(p=>p).ToArray();
            var iniKeys=Regex.Matches(template,@"(?m)^\s*(\w+)\s*=").Cast<Match>().Select(m=>m.Groups[1].Value).OrderBy(p=>p).ToArray();
            Check(keys.SequenceEqual(iniKeys),"every official INI key represented once");
            var fresh=Options(asm);var values=(Dictionary<string,bool>)Field(fresh,"MarkerValues");var numbers=(Dictionary<string,string>)Field(fresh,"MarkerParameters");
            Check(values["MarkerPatchMenu"]&&values["FixStuckChaseCamera"],"new settings menu and camera fix default on");
            Check(!values["AlwaysSucceedQTE"]&&!values["SkipHackingMinigame"]&&!values["DisableControllerAimAcceleration"],"gameplay and aiming helpers default off");
            Check(numbers["PlayStationButtonIcons"]=="2"&&numbers["MultiplayerSuit"]=="0"&&numbers["IncreasedEntityPersistenceLimbs"]=="36","new defaults match upstream");
            foreach(string key in new[]{"PlayStationButtonIcons","MultiplayerSuit","ImprovedAntiAliasingMode"}){
                var item=parameters.Single(p=>(string)Field(p,"Key")==key);string choices=(string)Field(item,"Choices");
                Check(choices.Split('|').Length==1+(int)(decimal)Field(item,"Max"),key+" choices retain numeric mapping");
            }
            var suits=(string)Field(parameters.Single(p=>(string)Field(p,"Key")=="MultiplayerSuit"),"Choices");
            Check(suits.Split('|').Skip(11).Take(4).SequenceEqual(new[]{"虎纹蓝","虎纹军绿","虎纹红","虎纹军用"}),"tiger camo choices keep the upstream IDs 11–14");
            numbers["PlayStationButtonIcons"]="2";numbers["MultiplayerSuit"]="36";numbers["ImprovedAntiAliasingMode"]="4";Call(catalog,"Validate",numbers);
            string configured=(string)Call(engine,"ConfigureMarkerPatch",template,fresh);
            Check(Ini(configured,"PlayStationButtonIcons")=="2"&&Ini(configured,"MultiplayerSuit")=="36"&&Ini(configured,"ImprovedAntiAliasingMode")=="4","numeric choices survive INI configuration");
            foreach(var invalid in new[]{new[]{"PlayStationButtonIcons","3"},new[]{"MultiplayerSuit","37"},new[]{"ImprovedAntiAliasingMode","5"}}){
                var bad=new Dictionary<string,string>(numbers);bad[invalid[0]]=invalid[1];bool refused=false;try{Call(catalog,"Validate",bad);}catch{refused=true;}Check(refused,"reject invalid "+invalid[0]);
            }
            Call(asm.GetType("L"),"SetLanguage","en");
            foreach(object item in features.Concat(parameters)){
                foreach(string name in new[]{"Title","Description"}){
                    string translated=(string)Call(asm.GetType("L"),"T",Field(item,name));Check(!Regex.IsMatch(translated,@"[\u4e00-\u9fff]"),"English "+Field(item,"Key")+" "+name);
                }
            }
            Check((bool)Call(engine,"IsKnownMarkerHash","30af0b5ccf50a34d9303d99c04d899c6f6368e522f509ca9d077586542622b55"),"3.0.7 remains a supported migration source");
            Check(!(bool)Call(engine,"IsKnownMarkerHash",new string('0',64)),"unknown DLL is not trusted");

            string game=Path.Combine(root,"upgrade");Directory.CreateDirectory(game);File.WriteAllText(Path.Combine(game,"deadspace2.exe"),"fixture never execute");File.WriteAllText(Path.Combine(game,"DS2DAT0.DAT"),"foreign base preserve");File.WriteAllText(Path.Combine(game,"DS2DAT2.DAT"),"foreign DLC preserve");File.WriteAllText(Path.Combine(game,"player.deadspace2saved"),"fixture save preserve");
            var original=Snapshot(game);Assembly previous=Assembly.LoadFile(Path.GetFullPath(args[1]));Type oldEngine=previous.GetType("InstallerEngine");Call(previous.GetType("L"),"Initialize","en");
            Action<string> log=s=>File.AppendAllText(Path.Combine(root,"installation.log"),s+Environment.NewLine);
            Call(oldEngine,"ApplySelection",game,Options(previous),log);
            Check(Hash(Path.Combine(game,"dinput8.dll"))=="30af0b5ccf50a34d9303d99c04d899c6f6368e522f509ca9d077586542622b55","fixture starts with actual old bundled DLL");
            string path=Path.Combine(game,"MarkerPatch.ini"),oldIni=File.ReadAllText(path);
            oldIni=Regex.Replace(oldIni,@"(?m)^IncreasedEntityPersistenceLimbs\s*=.*$","IncreasedEntityPersistenceLimbs = 147");
            oldIni=Regex.Replace(oldIni,@"(?m)^FontScalingFactor\s*=.*$","FontScalingFactor = 1.7");
            File.WriteAllText(path,oldIni+"\r\n[KeepCustom]\r\nPreserve=exact\r\n");
            object selected=Call(engine,"InspectInstallation",game);Check((string)Field(selected,"MarkerVersion")=="3.0.7"&&!(bool)Field(selected,"ExternalMarker"),"old DLL recognized by actual version");
            object upgrade=Field(selected,"Options");var custom=(Dictionary<string,string>)Field(upgrade,"MarkerParameters");
            Check(custom["IncreasedEntityPersistenceLimbs"]=="147"&&(decimal)Field(upgrade,"SubtitleScale")==1.7m,"existing custom settings read before upgrade");
            Call(engine,"ApplySelection",game,upgrade,log);
            Check(Hash(Path.Combine(game,"dinput8.dll"))=="d37df6d429c6c1671d93b1b84c03c2eb516957de735ee3abb4d3d93200b34745","upgrade installs official 3.3.1 DLL");
            string current=File.ReadAllText(path);
            Check(Ini(current,"IncreasedEntityPersistenceLimbs")=="147"&&Ini(current,"FontScalingFactor")=="1.7"&&current.Contains("Preserve=exact"),"upgrade retains custom values and unknown sections");
            Check(Ini(current,"PlayStationButtonIcons")=="2"&&Ini(current,"MultiplayerSuit")=="0"&&Ini(current,"MarkerPatchMenu")=="1","upgrade inserts typed new options");
            selected=Call(engine,"InspectInstallation",game);Check((string)Field(selected,"MarkerVersion")=="3.3.1"&&(bool)Field(selected,"MarkerIsCurrent"),"new version correctly recognized");
            Call(engine,"Uninstall",game,log);Check(Equal(original,Snapshot(game)),"managed upgrade and uninstall restore original fixture byte for byte");
            Console.WriteLine("MARKER 3.3.1 CHECKS PASSED "+checks);return 0;
        }catch(Exception error){Console.WriteLine("FAIL "+error);return 1;}
    }
}
