using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

internal sealed class AuditDigest
{
    public string status, md5, sha256, error, known_reference;
    public long bytes;
    public string last_write_utc;
}
internal sealed class AuditReferenceFile { public Dictionary<string,AuditDigest> references; }
internal sealed class InstallAudit
{
    internal static string OutputRootOverride;
    internal static string LastReport;
    internal readonly string Folder;
    readonly string logFile;
    readonly Action<string> sink;
    readonly JavaScriptSerializer json=new JavaScriptSerializer { MaxJsonLength=16*1024*1024 };
    readonly Dictionary<string,AuditDigest> references;
    readonly Stopwatch elapsed=Stopwatch.StartNew();
    readonly HashSet<string> observed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    bool finished;
    internal bool AfterCaptured;
    internal bool GamePhaseEntered;
    internal bool WasCancelled;
    internal Dictionary<string,AuditDigest> After;
    internal AuditDigest Self;
    internal readonly List<string> Problems=new List<string>();
    internal InstallAudit(string game,InstallOptions options,Action<string> output)
    {
        LastReport=null;sink=output;
        string parent=OutputRootOverride;
        if(string.IsNullOrWhiteSpace(parent))parent=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"DS2-InstallLogs");
        Folder=Path.Combine(Path.GetFullPath(parent),DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
        Directory.CreateDirectory(Folder);logFile=Path.Combine(Folder,"installation.log");
        using(var stream=typeof(InstallerEngine).Assembly.GetManifestResourceStream("DS2_AUDIT_REFERENCES"))
        using(var reader=new StreamReader(stream,Encoding.UTF8))references=json.Deserialize<AuditReferenceFile>(reader.ReadToEnd()).references;
        Write("START diagnostic installer "+ProductInfo.ReleaseVersion+"; local="+DateTimeOffset.Now.ToString("o")+"; game="+game);
        Write(L.T("诊断报告：")+Folder);
        Save("expected-references.json",new { schema=1,baseline="2026.11.6.19 fonts, short labels and independent runtime fixes",references=references });
        Save("selection.json",new { version=ProductInfo.ReleaseVersion,game=game,Chinese=options.Chinese,Dlc=options.Dlc,MarkerPatch=options.MarkerPatch,NodeFix=options.NodeFix,DeployManager=options.DeployManager,SubtitleScale=options.SubtitleScale,IgnoreCompatibilityErrors=options.IgnoreCompatibilityErrors,MarkerValues=options.MarkerValues,MarkerParameters=options.MarkerParameters });
        Self=Digest(typeof(InstallerEngine).Assembly.Location,null);
        var runtimes=new Dictionary<string,object>();string windows=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string system32=Path.Combine(windows,Environment.Is64BitOperatingSystem?"SysWOW64":"System32");
        foreach(string name in new[]{"msvcp140.dll","vcruntime140.dll","ucrtbase.dll"})
        {
            string path=Path.Combine(system32,name);string version=null;
            if(File.Exists(path))try{version=FileVersionInfo.GetVersionInfo(path).FileVersion;}catch(Exception ex){Problems.Add("runtime version: "+ex.Message);}
            runtimes[name]=new { path=path,exists=File.Exists(path),version=version };
            Write("X86_RUNTIME "+name+" "+(version??"missing/unreadable"));
        }
        Save("environment.json",new { local_time=DateTimeOffset.Now.ToString("o"),utc=DateTime.UtcNow.ToString("o"),os=Environment.OSVersion.ToString(),os_64_bit=Environment.Is64BitOperatingSystem,logical_processors=Environment.ProcessorCount,culture=CultureInfo.CurrentUICulture.Name,x86_runtime_files=runtimes,installer=Self });
    }
    internal void Write(string text)
    {
        File.AppendAllText(logFile,DateTime.UtcNow.ToString("o")+" "+text+Environment.NewLine,new UTF8Encoding(false));
        if(sink!=null)sink(text);
    }
    internal void Save(string name,object data) { File.WriteAllText(Path.Combine(Folder,name),json.Serialize(data),new UTF8Encoding(false)); }
    internal static AuditDigest Digest(string path,Action<long,long> progress)
    {
        var row=new AuditDigest();if(!File.Exists(path)){row.status="missing";return row;}
        try
        {
            var initial=new FileInfo(path);long length=initial.Length;DateTime time=initial.LastWriteTimeUtc;
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,1024*1024,FileOptions.SequentialScan))
            using(var md5=MD5.Create())using(var sha=SHA256.Create())
            {
                byte[] buffer=new byte[1024*1024];int read;long count=0,next=256L*1024*1024;
                while((read=stream.Read(buffer,0,buffer.Length))>0)
                {
                    md5.TransformBlock(buffer,0,read,buffer,0);sha.TransformBlock(buffer,0,read,buffer,0);count+=read;
                    if(progress!=null&&count>=next){progress(count,length);next=count+256L*1024*1024;}
                }
                md5.TransformFinalBlock(new byte[0],0,0);sha.TransformFinalBlock(new byte[0],0,0);
                row.bytes=count;row.md5=BitConverter.ToString(md5.Hash).Replace("-","").ToLowerInvariant();row.sha256=BitConverter.ToString(sha.Hash).Replace("-","").ToLowerInvariant();
            }
            var end=new FileInfo(path);row.last_write_utc=end.LastWriteTimeUtc.ToString("o");
            row.status=end.Length==length&&row.bytes==length&&end.LastWriteTimeUtc==time?"ok":"changed_while_reading";
        }
        catch(Exception e){row.status="unreadable";row.error=e.GetType().Name+": "+e.Message;}
        return row;
    }
    internal Dictionary<string,AuditDigest> Capture(string phase,string game,IEnumerable<string> names)
    {
        Write(L.T("正在计算完整 MD5 / SHA-256：")+phase);
        var paths=new HashSet<string>(names,StringComparer.OrdinalIgnoreCase);
        if(Directory.Exists(game))foreach(string p in Directory.GetFiles(game,"DS2DAT*.DAT"))paths.Add(Path.GetFileName(p));
        paths.Add("deadspace2.exe");foreach(string p in observed)paths.Add(p);
        var rows=new Dictionary<string,AuditDigest>(StringComparer.OrdinalIgnoreCase);
        foreach(string name in paths.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
        {
            if(Path.IsPathRooted(name)||name.Split('\\','/').Any(x=>x==".."))throw new InvalidDataException("Unsafe audit relative path");
            observed.Add(name);string path=Path.Combine(game,name);
            var row=Digest(path,(count,total)=>Write("HASH_PROGRESS "+phase+" "+name+" "+count+"/"+total));
            foreach(var known in references)if(row.status=="ok"&&row.bytes==known.Value.bytes&&row.sha256==known.Value.sha256&&row.md5==known.Value.md5){row.known_reference=known.Key;break;}
            rows[name]=row;Write("HASH "+phase+" "+name+" bytes="+row.bytes+" MD5="+row.md5+" SHA256="+row.sha256+" status="+row.status);
        }
        Save("snapshot-"+phase+".json",rows);
        var csv=new StringBuilder("path,bytes,MD5,SHA256,status,known_reference\r\n");
        foreach(var pair in rows)csv.Append('"').Append(pair.Key.Replace("\"","\"\"")).Append("\",").Append(pair.Value.bytes).Append(',').Append(pair.Value.md5).Append(',').Append(pair.Value.sha256).Append(',').Append(pair.Value.status).Append(',').Append(pair.Value.known_reference).Append("\r\n");
        File.WriteAllText(Path.Combine(Folder,"snapshot-"+phase+".csv"),csv.ToString(),new UTF8Encoding(true));
        foreach(string name in new[]{"DS2SeveredOneClick.install.txt","MarkerPatch.ini",@"plugins\SeveredRuntimeFixes.ini","DS2PatchManager.version.json"})
        {
            string source=Path.Combine(game,name);if(!File.Exists(source))continue;
            try{if(new FileInfo(source).Length<=512*1024)File.Copy(source,Path.Combine(Folder,phase+"-"+name.Replace('\\','_').Replace('/','_')),false);}
            catch(Exception e){Problems.Add("capture config "+name+": "+e.Message);}
        }
        if(phase=="after"){After=rows;AfterCaptured=true;}return rows;
    }
    internal void CapturePackage(string path)
    {
        Write("INPUT_PACKAGE "+path);var row=Digest(path,(count,total)=>Write("HASH_PROGRESS input Severed.marker "+count+"/"+total));Save("input-package.json",new { path=path,digest=row });
        Write("INPUT MD5="+row.md5+" SHA256="+row.sha256+" status="+row.status);
    }
    static Dictionary<string,string> State(string game)
    {
        var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);string path=Path.Combine(game,"DS2SeveredOneClick.install.txt");
        if(File.Exists(path))foreach(string line in File.ReadAllLines(path)){int p=line.IndexOf('=');if(p>0)values[line.Substring(0,p)]=line.Substring(p+1);}
        return values;
    }
    internal void Verify(string game,InstallOptions options)
    {
        var state=State(game);Func<string,string> value=k=>state.ContainsKey(k)?state[k]:null;
        var checks=new List<object>();var failures=new List<string>();
        Action<string,string> check=(name,role)=>
        {
            AuditDigest expected=references[role],actual;bool found=After.TryGetValue(name,out actual);
            bool match=found&&actual.status=="ok"&&actual.bytes==expected.bytes&&actual.md5==expected.md5&&actual.sha256==expected.sha256;
            checks.Add(new { path=name,expected_reference=role,expected=expected,actual=found?actual:null,status=match?"MATCH":"MISMATCH_OR_UNREADABLE" });
            Write("VERIFY "+name+" expected="+role+" "+(match?"MATCH":"FAILED"));if(!match)failures.Add(name);
        };
        if(value("chinese")=="True")check("DS2DAT0.DAT","dat0.chinese.release19");
        if((options.Chinese||options.Dlc)&&!options.IgnoreCompatibilityErrors)foreach(string name in new[]{"DS2DAT1.DAT","DS2DAT6.DAT","DS2DAT7.DAT","DS2DAT8.DAT","DS2DAT9.DAT"})check(name,"base."+name);
        if(value("marker_patch")=="True")check(File.Exists(Path.Combine(game,"dinput8.dll"))?"dinput8.dll":"dinput8.dll.ds2oneclick-off","marker.3.0.7");
        if(value("owns_dlc_runtime")=="True"||value("dlc")=="True")check(@"plugins\SeveredPC.asi","core.3.9");
        if(value("owns_extras_runtime")=="True")check(@"plugins\SeveredExtras.asi","extras.3.9");
        if(value("owns_bug_plugins")=="True"){check(@"plugins\SeveredPowerNodeFix.asi","node.release19");check(@"plugins\SeveredSaveDisplay.asi","save.release19");check(@"plugins\SeveredMiningCampFix.asi","mining.release19");}
        if(value("owns_save_icons")=="True")check("DS2DAT3.DAT","dat3.save-icons");
        if(value("owns_dat2")=="True"||value("dlc")=="True")
        {
            string expectedHash=value("installed_dat2_sha256");var known=references.FirstOrDefault(p=>p.Key.StartsWith("dat2.")&&p.Value.sha256==expectedHash);
            if(known.Key!=null)check("DS2DAT2.DAT",known.Key);
            else
            {
                AuditDigest actual;bool match=After.TryGetValue("DS2DAT2.DAT",out actual)&&actual.status=="ok"&&!string.IsNullOrEmpty(expectedHash)&&actual.sha256==expectedHash;
                checks.Add(new { path="DS2DAT2.DAT",expected_sha256=expectedHash,actual=actual,status=match?"SHA256_MATCH_NEW_PACKAGE_MD5_RECORDED":"MISMATCH_OR_UNREADABLE" });
                Write("VERIFY DS2DAT2.DAT newer package SHA256 "+(match?"MATCH; full MD5 recorded, no pinned MD5 reference":"FAILED"));if(!match)failures.Add("DS2DAT2.DAT");
            }
        }
        if(options.DeployManager)
        {
            AuditDigest actual;bool match=After.TryGetValue("DS2PatchManager.exe",out actual)&&actual.status=="ok"&&actual.sha256==Self.sha256&&actual.md5==Self.md5;
            checks.Add(new { path="DS2PatchManager.exe",expected=Self,actual=actual,status=match?"MATCH_INSTALLER":"MISMATCH" });if(!match)failures.Add("DS2PatchManager.exe");
        }
        Save("verification.json",new { passed=failures.Count==0,checks=checks,failures=failures });
        if(failures.Count>0)throw new InvalidDataException(L.T("安装后的文件校验失败，请保留诊断报告。")+" "+string.Join(", ",failures));
    }
    internal void Failure(Exception error)
    {
        WasCancelled=error is OperationCanceledException;
        Save("failure.json",new { type=error.GetType().FullName,message=error.Message,hresult=error.HResult,exception=error.ToString() });
        Write("OPERATION_FAILED "+error);Problems.Add(error.GetType().Name+": "+error.Message);
    }
    internal void Complete(bool success)
    {
        if(finished)return;finished=true;
        try
        {
            Save("result.json",new { success=success,cancelled=WasCancelled,game_installation_started=GamePhaseEntered,before_snapshot_present=File.Exists(Path.Combine(Folder,"snapshot-before.json")),installer=ProductInfo.ReleaseVersion,elapsed_seconds=elapsed.Elapsed.TotalSeconds,report_folder=Folder,issues=Problems });
            string summary="诊断安装器 "+ProductInfo.ReleaseVersion+" / Diagnostic installer\r\n结果 / Result: "+(success?"SUCCESS":"FAILED; read failure.json")+"\r\n安装日志: installation.log\r\n安装前后完整MD5和SHA-256: snapshot-before/after.json及.csv\r\n预期值与匹配情况: expected-references.json / verification.json\r\n\r\n同一原版DAT、安装器、数据包和组件选择下，生成的数据及随包插件应一致。配置、自定义设置、路径、安装记录、管理器版本和日志不作为跨机器统一MD5。\r\nIdentical source DATs, installer, data package and component choices should produce identical data and bundled modules. Configurations, custom settings, paths, installation records, manager versions and logs can differ across machines.\r\n\r\n这里的MD5便于对照，现有SHA-256完整性校验继续保留。报告不含游戏DAT内容或存档；可能含本机路径和设置，发送前可查看。No save/DAT contents. Paths/settings may appear; review before sharing.\r\n";
            File.WriteAllText(Path.Combine(Folder,"SUMMARY.zh-en.txt"),summary,new UTF8Encoding(true));Write("FINISH success="+success+" elapsed="+elapsed.Elapsed);
            string zip=Folder+".zip";
            using(var output=new FileStream(zip,FileMode.CreateNew,FileAccess.Write))using(var archive=new ZipArchive(output,ZipArchiveMode.Create))foreach(string path in Directory.GetFiles(Folder))
            {
                var entry=archive.CreateEntry(Path.GetFileName(path),CompressionLevel.Optimal);using(var input=File.OpenRead(path))using(var stream=entry.Open())input.CopyTo(stream);
            }
            LastReport=zip;
        }
        catch(Exception e){Problems.Add("Report packaging: "+e);LastReport=Folder;try{File.AppendAllText(logFile,"REPORT_PACKAGING_ERROR "+e+Environment.NewLine);}catch{}}
        if(sink!=null)try{sink(L.T("诊断报告：")+LastReport);}catch{}
    }
}
internal static partial class InstallerEngine
{
    internal static string DiagnosticReportText { get {return string.IsNullOrEmpty(InstallAudit.LastReport)?"":"\r\n\r\n"+L.T("诊断报告：")+InstallAudit.LastReport;} }
    public static void ApplySelection(string gamePath,InstallOptions options,Action<string> log)
    {
        InstallAudit audit=null;string game=null;bool entered=false,success=false;
        try
        {
            audit=new InstallAudit(gamePath,options,log);game=ValidateGame(gamePath);EnsureGameStopped();
            RuntimePrerequisite.Ensure(options,audit);
            audit.Capture("before",game,ManagedPaths(game));
            if(options.Dlc)
            {
                string package=string.IsNullOrWhiteSpace(options.DlcArchivePath)?GameLocations.FindPackage(AppDomain.CurrentDomain.BaseDirectory,game):options.DlcArchivePath;
                if(!string.IsNullOrEmpty(package))audit.CapturePackage(package);
            }
            entered=true;audit.GamePhaseEntered=true;ApplySelectionCore(game,options,audit.Write);audit.Capture("after",game,ManagedPaths(game));audit.Verify(game,options);success=true;
        }
        catch(Exception error)
        {
            if(audit!=null)
            {
                try{audit.Failure(error);}catch{}
                if(entered&&!audit.AfterCaptured&&game!=null)try{audit.Capture("after-failure",game,ManagedPaths(game));}catch(Exception capture){audit.Problems.Add("after-failure snapshot: "+capture);}
            }
            throw;
        }
        finally{if(audit!=null)audit.Complete(success);}
    }
}
