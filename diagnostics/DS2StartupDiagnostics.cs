using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Management;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.IO.Compression;
using Microsoft.Win32;

internal static class StartupDiagnostics
{
    const string ToolVersion="20261103-v1";
    static readonly Encoding Utf8=new UTF8Encoding(false);
    static readonly List<string> Notes=new List<string>();
    static readonly JavaScriptSerializer Json=new JavaScriptSerializer { MaxJsonLength=8*1024*1024 };
    static string Game,Output;
    static bool FullData;
    static Dictionary<string,string> State=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string,object> Item(params object[] pairs) { var d=new Dictionary<string,object>();for(int i=0;i<pairs.Length;i+=2)d[(string)pairs[i]]=pairs[i+1];return d; }
    static void Save(string name,object data) { File.WriteAllText(Path.Combine(Output,name),Json.Serialize(data),Utf8); }
    static string Arg(string[] a,string key) { for(int i=0;i+1<a.Length;i++)if(a[i]==key)return a[i+1];return null; }
    static string Value(string key) { string v;return State.TryGetValue(key,out v)?v:null; }
    static void Attempt(string label,Action action) { try {action();}catch(Exception e){Notes.Add(label+": "+e.GetType().Name+": "+e.Message);} }
    static FileStream ReadShared(string path) { return new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete); }
    static string Hash(string path) { using(var s=ReadShared(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant(); }
    static string Tail(string path,int maxBytes) { using(var s=ReadShared(path)){long start=Math.Max(0,s.Length-maxBytes);s.Position=start;using(var reader=new StreamReader(s,Encoding.UTF8,true))return (start>0?"[TAIL ONLY; earlier bytes omitted]\r\n":"")+reader.ReadToEnd();} }
    static Dictionary<string,string> ReadPairs(string path) { var d=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);if(!File.Exists(path))return d;foreach(string line in Tail(path,256*1024).Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)){int x=line.IndexOf('=');if(x>0)d[line.Substring(0,x).Trim()]=line.Substring(x+1).Trim();}return d; }
    static string Reg(string key,string name) { try{return Convert.ToString(Registry.GetValue(key,name,null));}catch{return "";} }
    static void AddGame(List<string> list,string path) { if(string.IsNullOrWhiteSpace(path))return;Attempt("discovery",()=>{path=path.Trim().Trim('"');if(File.Exists(path))path=Path.GetDirectoryName(Path.GetFullPath(path));path=Path.GetFullPath(path);if(File.Exists(Path.Combine(path,"deadspace2.exe"))&&!list.Contains(path,StringComparer.OrdinalIgnoreCase))list.Add(path);}); }
    static string Discover(string[] args)
    {
        string explicitPath=Arg(args,"--game-dir");if(explicitPath==null&&args.Length>0&&!args[0].StartsWith("--"))explicitPath=args[0];
        if(explicitPath!=null){var one=new List<string>();AddGame(one,explicitPath);if(one.Count!=1)throw new DirectoryNotFoundException("指定目录中没有 deadspace2.exe / No deadspace2.exe in the specified folder.");return one[0];}
        var games=new List<string>();AddGame(games,Environment.CurrentDirectory);AddGame(games,AppDomain.CurrentDomain.BaseDirectory);var nearbyParent=Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'));if(nearbyParent!=null)AddGame(games,nearbyParent.FullName);
        string steam=Reg(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath");if(string.IsNullOrEmpty(steam))steam=Reg(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam","InstallPath");
        var libraries=new List<string>();if(!string.IsNullOrEmpty(steam)){libraries.Add(steam);string vdf=Path.Combine(steam,@"steamapps\libraryfolders.vdf");if(File.Exists(vdf))foreach(Match match in Regex.Matches(Tail(vdf,256*1024),"\"path\"\\s+\"([^\"]+)\""))libraries.Add(match.Groups[1].Value.Replace("\\\\","\\"));}
        foreach(string library in libraries)AddGame(games,Path.Combine(library,@"steamapps\common\Dead Space 2"));
        AddGame(games,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"EA Games\Dead Space 2"));AddGame(games,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),@"Origin Games\Dead Space 2"));
        foreach(string branch in new[]{@"HKEY_LOCAL_MACHINE\SOFTWARE\EA Games\Dead Space 2",@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 2"})foreach(string key in new[]{"Install Dir","InstallDir","InstallPath"})AddGame(games,Reg(branch,key));
        if(games.Count==1)return games[0];
        if(games.Count>1){Console.WriteLine("发现多个版本，请选编号 / Multiple installations; choose a number:");for(int i=0;i<games.Count;i++)Console.WriteLine((i+1)+". "+games[i]);int index;if(int.TryParse(Console.ReadLine(),out index)&&index>=1&&index<=games.Count)return games[index-1];}
        Console.WriteLine("粘贴游戏目录（含 deadspace2.exe）/ Paste the game folder:");var manual=new List<string>();AddGame(manual,Console.ReadLine());if(manual.Count!=1)throw new DirectoryNotFoundException("未找到游戏 / Game not found.");return manual[0];
    }
    static List<Dictionary<string,object>> Query(string wql,string[] fields)
    {
        var rows=new List<Dictionary<string,object>>();using(var search=new ManagementObjectSearcher(wql)){search.Options.Timeout=TimeSpan.FromSeconds(5);using(var found=search.Get())foreach(ManagementObject obj in found){using(obj){var row=new Dictionary<string,object>();foreach(string field in fields)row[field]=obj[field];rows.Add(row);if(rows.Count>=16)break;}}}return rows;
    }
    static uint Rva(byte[] b,int section,int count,uint rva)
    {
        for(int i=0;i<count;i++){int p=section+40*i;uint virtualSize=BitConverter.ToUInt32(b,p+8),virtualAddress=BitConverter.ToUInt32(b,p+12),rawSize=BitConverter.ToUInt32(b,p+16),raw=BitConverter.ToUInt32(b,p+20);if(rva>=virtualAddress&&(ulong)rva<(ulong)virtualAddress+Math.Max(virtualSize,rawSize))return raw+rva-virtualAddress;}return rva;
    }
    static string AsciiZ(byte[] b,uint offset) { if(offset>=b.Length)throw new InvalidDataException("PE string outside file");int end=(int)offset;while(end<b.Length&&end<(long)offset+512&&b[end]!=0)end++;return Encoding.ASCII.GetString(b,(int)offset,end-(int)offset); }
    static Dictionary<string,object> Pe(string path)
    {
        byte[] b=File.ReadAllBytes(path);if(b.Length<64||b[0]!=77||b[1]!=90)throw new InvalidDataException("Not a PE image");int p=BitConverter.ToInt32(b,60);if(p<0||p>b.Length-24||BitConverter.ToUInt32(b,p)!=0x4550)throw new InvalidDataException("Invalid PE header");
        int count=BitConverter.ToUInt16(b,p+6),optional=p+24,section=optional+BitConverter.ToUInt16(b,p+20);ushort machine=BitConverter.ToUInt16(b,p+4),magic=BitConverter.ToUInt16(b,optional);if(count>96||section+40L*count>b.Length||(magic!=0x10b&&magic!=0x20b))throw new InvalidDataException("Unsupported PE layout");
        uint imports=BitConverter.ToUInt32(b,optional+(magic==0x10b?96:112)+8);var modules=new List<Dictionary<string,object>>();
        if(imports!=0){uint off=Rva(b,section,count,imports);for(int i=0;i<256;i++){long pos=(long)off+20*i;if(pos<0||pos+20>b.Length)throw new InvalidDataException("Import descriptor outside file");uint name=BitConverter.ToUInt32(b,(int)pos+12);if(name==0)break;string module=AsciiZ(b,Rva(b,section,count,name));bool contract=module.StartsWith("api-ms-",StringComparison.OrdinalIgnoreCase)||module.StartsWith("ext-ms-",StringComparison.OrdinalIgnoreCase);var search=new[]{Path.Combine(Path.GetDirectoryName(path),module),Path.Combine(Game,module),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),Environment.Is64BitOperatingSystem?"SysWOW64":"System32",module)};string located=search.FirstOrDefault(File.Exists);modules.Add(Item("name",module,"api_set_contract",contract,"located_candidate",located,"note",contract?"API set contracts may resolve without a physical DLL of this name.":"Presence is not a loader compatibility test."));}}
        return Item("machine",machine==0x14c?"x86":machine==0x8664?"x64":"0x"+machine.ToString("x"),"imports",modules);
    }
    static Dictionary<string,object> FileDetails(string path,bool hash,bool pe)
    {
        var result=Item("exists",File.Exists(path));if(!File.Exists(path))return result;
        Attempt("file "+path,()=>{var f=new FileInfo(path);result["bytes"]=f.Length;result["last_write_utc"]=f.LastWriteTimeUtc.ToString("o");result["attributes"]=f.Attributes.ToString();if(hash)result["sha256"]=Hash(path);if(pe&&f.Length<=64*1024*1024){Attempt("PE "+path,()=>result["pe"]=Pe(path));Attempt("version "+path,()=>result["file_version"]=FileVersionInfo.GetVersionInfo(path).FileVersion);}});return result;
    }
    static List<Dictionary<string,object>> Events(string log,string ids)
    {
        var list=new List<Dictionary<string,object>>();string query="*[System[("+ids+") and TimeCreated[timediff(@SystemTime) <= 604800000]]]";
        using(var reader=new EventLogReader(new EventLogQuery(log,PathType.LogName,query){ReverseDirection=true})){int scanned=0;EventRecord ev;while((ev=reader.ReadEvent())!=null){using(ev){if(++scanned>2000||list.Count>=30)break;string xml=ev.ToXml(),message="";try{message=ev.FormatDescription();}catch{}if(!Regex.IsMatch(xml+message,@"deadspace2|dead space 2|DS2Severed|SeveredPC|SeveredExtras|MarkerPatch",RegexOptions.IgnoreCase))continue;list.Add(Item("utc",ev.TimeCreated.HasValue?ev.TimeCreated.Value.ToUniversalTime().ToString("o"):"","event_id",ev.Id,"provider",ev.ProviderName,"message",message,"xml",xml));}}}return list;
    }
    static void Collect()
    {
        State=ReadPairs(Path.Combine(Game,"DS2SeveredOneClick.install.txt"));
        var system=Item("tool_version",ToolVersion,"time_local",DateTimeOffset.Now.ToString("o"),"time_utc",DateTime.UtcNow.ToString("o"),"game_folder",Game,"os_version",Environment.OSVersion.ToString(),"os_64_bit",Environment.Is64BitOperatingSystem,"process_64_bit",Environment.Is64BitProcess,"logical_processors",Environment.ProcessorCount,"clr",Environment.Version.ToString(),"culture",System.Globalization.CultureInfo.CurrentUICulture.Name);
        system["windows_registry"]=Item("product",Reg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion","ProductName"),"display_version",Reg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion","DisplayVersion"),"build",Reg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion","CurrentBuildNumber"));
        var cpu=new List<Dictionary<string,object>>();Attempt("CPU WMI",()=>cpu=Query("SELECT Name,NumberOfCores,NumberOfLogicalProcessors FROM Win32_Processor",new[]{"Name","NumberOfCores","NumberOfLogicalProcessors"}));system["cpu"]=cpu;
        Attempt("GPU WMI",()=>system["gpu"]=Query("SELECT Name,DriverVersion FROM Win32_VideoController",new[]{"Name","DriverVersion"}));
        Attempt("game drive",()=>{var drive=new DriveInfo(Path.GetPathRoot(Game));system["game_drive"]=Item("drive_type",drive.DriveType.ToString(),"format",drive.DriveFormat,"available_bytes",drive.AvailableFreeSpace,"total_bytes",drive.TotalSize,"note","DriveType does not reliably identify USB-attached fixed disks.");});
        var runtime=new Dictionary<string,object>();string win=Environment.GetFolderPath(Environment.SpecialFolder.Windows),x86=Path.Combine(win,Environment.Is64BitOperatingSystem?"SysWOW64":"System32");foreach(string dll in new[]{"msvcp140.dll","vcruntime140.dll","vcruntime140_1.dll","ucrtbase.dll","msvcr100.dll","msvcp100.dll","msvcr90.dll","d3dx9_43.dll","XINPUT1_3.dll"})runtime[dll]=FileDetails(Path.Combine(x86,dll),false,false);runtime["x86_folder"]=x86;runtime["vc14_x86_registry"]=Item("installed",Reg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86","Installed"),"version",Reg(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86","Version"));system["x86_runtimes"]=runtime;Save("system.json",system);
        var files=new Dictionary<string,object>();var targets=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"deadspace2.exe","dinput8.dll","dinput8.dll.ds2oneclick-off","DS2PatchManager.exe"};
        foreach(string directory in new[]{Game,Path.Combine(Game,"plugins")})if(Directory.Exists(directory))foreach(string path in Directory.GetFiles(directory)){string extension=Path.GetExtension(path);if(extension.Equals(".dll",StringComparison.OrdinalIgnoreCase)||extension.Equals(".asi",StringComparison.OrdinalIgnoreCase))targets.Add(path.Substring(Game.Length+1));}
        int examined=0;foreach(string name in targets){if(++examined>80){Notes.Add("Native file listing capped at 80 entries.");break;}string path=Path.Combine(Game,name);bool small=File.Exists(path)&&new FileInfo(path).Length<=64*1024*1024;files[name]=FileDetails(path,small,small);}
        foreach(string name in new[]{"DS2DAT0.DAT","DS2DAT1.DAT","DS2DAT2.DAT","DS2DAT3.DAT","DS2DAT6.DAT","DS2DAT7.DAT","DS2DAT8.DAT","DS2DAT9.DAT"})files[name]=FileDetails(Path.Combine(Game,name),FullData&&(name=="DS2DAT0.DAT"||name=="DS2DAT2.DAT"||name=="DS2DAT3.DAT"),false);
        Save("files.json",files);Save("installation-record.json",State);
        var logs=new List<string>();foreach(string name in new[]{"DS2SeveredOneClick.install.txt","DS2PatchManager.version.json","MarkerPatch.ini","MarkerPatch.log","severed_probe.log","SeveredExtras.log","SeveredRuntimeFixes.log",@"plugins\SeveredRuntimeFixes.ini",@"plugins\SeveredExtras.ini",@"plugins\SeveredExtras.log",@"plugins\SeveredRuntimeFixes.log",@"plugins\SeveredPowerNodeFix.ini",@"plugins\SeveredPowerNodeFix.log",@"plugins\SeveredSaveDisplay.ini",@"plugins\SeveredSaveDisplay.log",@"plugins\MarkerPatch.ini",@"plugins\MarkerPatch.log"}){string path=Path.Combine(Game,name);if(!File.Exists(path))continue;Attempt("log "+name,()=>{string target=Path.Combine(Output,"logs",name);Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllText(target,Tail(path,1024*1024),Utf8);logs.Add(name);});}Save("collected-logs.json",logs);
        string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),@"EA Games\Dead Space 2\settings.txt");if(File.Exists(settings))Attempt("graphics settings",()=>File.WriteAllText(Path.Combine(Output,"graphics-settings.txt"),Tail(settings,64*1024),Utf8));
        Attempt("Application events",()=>Save("application-events.json",Events("Application","EventID=1000 or EventID=1001 or EventID=1002 or EventID=1026 or EventID=33 or EventID=59")));
        Attempt("Defender events (read only)",()=>Save("defender-events.json",Events("Microsoft-Windows-Windows Defender/Operational","EventID=1116 or EventID=1117 or EventID=1121 or EventID=1123")));
        var wer=new List<object>();foreach(string folder in new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),@"Microsoft\Windows\WER\ReportArchive"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),@"Microsoft\Windows\WER\ReportQueue")})Attempt("WER text reports",()=>{if(!Directory.Exists(folder))return;foreach(string sub in Directory.GetDirectories(folder).Where(p=>p.IndexOf("deadspace2",StringComparison.OrdinalIgnoreCase)>=0).OrderByDescending(Directory.GetLastWriteTimeUtc).Take(5)){string path=Path.Combine(sub,"Report.wer");if(File.Exists(path))wer.Add(Item("folder_name",Path.GetFileName(sub),"text",Tail(path,128*1024)));}});Save("wer-text-reports.json",wer);
        var dumps=new List<object>();string dumpFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CrashDumps");Attempt("dump metadata",()=>{if(Directory.Exists(dumpFolder))foreach(string path in Directory.GetFiles(dumpFolder,"deadspace2*.dmp").OrderByDescending(File.GetLastWriteTimeUtc).Take(8)){var info=new FileInfo(path);dumps.Add(Item("path",path,"bytes",info.Length,"utc",info.LastWriteTimeUtc.ToString("o")));}});Save("dump-file-inventory.json",dumps);
        var hints=new List<string>();hints.Add("报告只提供证据，不自动定因。No automatic root-cause verdict.");hints.Add("Installer release: "+(Value("installer_release")??"unknown / legacy")+"; record schema: "+(Value("version")??"none"));
        if(File.Exists(Path.Combine(Game,"dinput8.dll"))&&(!File.Exists(Path.Combine(x86,"msvcp140.dll"))||!File.Exists(Path.Combine(x86,"vcruntime140.dll"))))hints.Add("系统32位目录缺少部分VC14运行库；也可能存在游戏目录本地副本，请检查files.json中的PE导入候选路径。Missing x86 system VC14 candidates; check app-local imports before concluding.");
        var ini=ReadPairs(Path.Combine(Game,"MarkerPatch.ini"));int cores=0;foreach(var row in cpu)if(row["NumberOfCores"]!=null)cores+=Convert.ToInt32(row["NumberOfCores"]);string highCore;ini.TryGetValue("HighCoreCPUFix",out highCore);if(cores>10&&highCore!="1")hints.Add("CPU物理核心超过10，HighCoreCPUFix未确认启用；属于候选原因，不能据此定因。More than 10 physical cores; HighCoreCPUFix not confirmed enabled.");
        if(File.Exists(Path.Combine(Game,"plugins","MarkerPatch.asi"))&&File.Exists(Path.Combine(Game,"dinput8.dll")))hints.Add("检测到根目录加载器及plugins/MarkerPatch.asi，请人工核对重复/外置补丁。Check duplicate/external MarkerPatch installation.");
        hints.Add(FullData?"已请求DAT0/DAT2/DAT3完整SHA-256，结果见files.json。":"未完整校验大型DAT；文件大小不能证明完整性。Large DAT hashes skipped; size alone does not establish integrity.");
        hints.Add("未收集存档内容，未启动/关闭游戏，未加载任何ASI，未修改注册表/安全设置/游戏文件。No save contents, game launch/termination, ASI loading, registry/security/game changes.");
        File.WriteAllText(Path.Combine(Output,"SUMMARY.txt"),string.Join(Environment.NewLine,hints),Utf8);Save("collection-errors.json",Notes);
    }
    static int Main(string[] args)
    {
        Console.OutputEncoding=Utf8;int exit=0;try{Console.WriteLine("DS2 启动诊断 / Startup diagnostics "+ToolVersion);Console.WriteLine("只读收集；不改游戏或存档 / Read-only collection; no game/save changes.");if(args.Contains("--help")){Console.WriteLine("DS2StartupDiagnostics.exe [--game-dir folder] [--out report-parent] [--full-data] [--no-pause]");return 0;}
            FullData=args.Contains("--full-data");Game=Discover(args);string parent=Path.GetFullPath(Arg(args,"--out")??AppDomain.CurrentDomain.BaseDirectory);Directory.CreateDirectory(parent);Output=Path.Combine(parent,"DS2-StartupDiag-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(Output);
            Console.WriteLine("游戏 / Game: "+Game);Console.WriteLine("收集中，请稍候 / Collecting...");Collect();string zip=Output+".zip";ZipFile.CreateFromDirectory(Output,zip,CompressionLevel.Optimal,true);Console.WriteLine("完成，把这个ZIP发回 / Done; send this ZIP:\r\n"+zip);Console.WriteLine("报告含游戏路径、系统信息和相关错误记录，发送前可查看。Contains paths, system details and related errors; review before sharing.");
        }catch(Exception e){exit=1;Console.WriteLine("收集失败 / Collection failed: "+e.Message);if(Output!=null)Attempt("error report",()=>File.WriteAllText(Path.Combine(Output,"collector-failure.txt"),e.ToString(),Utf8));}
        if(!args.Contains("--no-pause")){Console.WriteLine("按回车退出 / Press Enter to close.");Console.ReadLine();}return exit;
    }
}
