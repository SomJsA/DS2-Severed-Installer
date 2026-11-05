using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using Microsoft.Win32;

internal sealed class VcRuntimeState
{
    public bool MsvcpX86, VcruntimeX86, UcrtX86;
    public string MsvcpVersion, VcruntimeVersion, RegistryVersion, Error;
    public bool Ready { get {return MsvcpX86 && VcruntimeX86 && UcrtX86 && RuntimePrerequisite.VersionReady(MsvcpVersion) && RuntimePrerequisite.VersionReady(VcruntimeVersion);} }
}
internal static class RuntimePrerequisite
{
    // This is the version validated and bundled for this release, not a claim
    // about the earliest ABI-compatible version of every Microsoft component.
    internal const string ValidatedVersion="14.51.36247.0";
    internal const string RedistSha="f0bab33a302b3cdb2e11113760d016f54fd3d2632c65ba7834fac4f0abd7f1a3";
    internal const long RedistBytes=6941536;
    internal const string OfficialDownload="https://aka.ms/vc14/vc_redist.x86.exe";
    internal static bool AllowInteractive=true;
    internal static bool Selected(InstallOptions options){return options.MarkerPatch || options.Dlc;}
    internal static bool VersionReady(string text){Version v;return Version.TryParse(text,out v)&&v.CompareTo(new Version(ValidatedVersion))>=0;}
    static bool X86Image(string path)
    {
        if(!File.Exists(path))return false;
        using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new BinaryReader(file))
        {
            if(file.Length<64||reader.ReadUInt16()!=0x5a4d)return false;file.Position=60;int pe=reader.ReadInt32();if(pe<0||pe>file.Length-6)return false;file.Position=pe;return reader.ReadUInt32()==0x4550&&reader.ReadUInt16()==0x14c;
        }
    }
    internal static VcRuntimeState Probe()
    {
        var state=new VcRuntimeState();string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),Environment.Is64BitOperatingSystem?"SysWOW64":"System32");
        try
        {
            string msvcp=Path.Combine(folder,"msvcp140.dll"),vcr=Path.Combine(folder,"vcruntime140.dll"),ucrt=Path.Combine(folder,"ucrtbase.dll");
            state.MsvcpX86=X86Image(msvcp);state.VcruntimeX86=X86Image(vcr);state.UcrtX86=X86Image(ucrt);
            if(state.MsvcpX86)state.MsvcpVersion=NumericVersion(msvcp);if(state.VcruntimeX86)state.VcruntimeVersion=NumericVersion(vcr);
            using(var hive=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry32))using(var key=hive.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86"))if(key!=null)state.RegistryVersion=Convert.ToString(key.GetValue("Version"));
        }
        catch(Exception error){state.Error=error.GetType().Name+": "+error.Message;}
        return state;
    }
    static string NumericVersion(string path){var v=FileVersionInfo.GetVersionInfo(path);return new Version(v.FileMajorPart,v.FileMinorPart,v.FileBuildPart,v.FilePrivatePart).ToString();}
    internal static int CanonicalExitCode(int exit){return ((uint)exit&0xffff0000)==0x80070000 ? exit&0xffff : exit;}
    internal static VcRuntimeState RunFlow(InstallOptions options,bool interactive,VcRuntimeState before,Func<bool> confirm,Func<int> install,Func<VcRuntimeState> refresh,Action<string> log)
    {
        if(!Selected(options)){log("VC_RUNTIME skipped: localization only");return before;}
        if(before!=null&&before.Ready){log("VC_RUNTIME ready; setup skipped");return before;}
        if(!interactive)throw new InvalidOperationException(L.T("需要更新 Microsoft Visual C++ v14 x86 运行库；请使用图形界面安装或先安装微软官方运行库。")+" "+OfficialDownload);
        if(!confirm())throw new OperationCanceledException(L.T("已取消运行库安装，游戏文件尚未更改。"));
        int raw=install(),exit=CanonicalExitCode(raw);log("VC_RUNTIME setup_exit="+raw+" canonical="+exit);
        if(exit==3010||exit==1641)throw new OperationCanceledException(L.T("运行库安装完成，需要重新启动 Windows。重启后请重新运行安装器；此次尚未更改游戏文件。"));
        if(exit==1602)throw new OperationCanceledException(L.T("已取消运行库安装，游戏文件尚未更改。"));
        if(exit!=0&&exit!=1638)throw new InvalidOperationException(L.T("微软运行库安装未成功，游戏文件尚未更改。")+" ("+raw+")");
        VcRuntimeState after=refresh();if(after==null||!after.Ready)throw new InvalidOperationException(L.T("运行库仍未就绪。请使用微软官方 x86 安装程序安装或修复后重试。"));
        log("VC_RUNTIME verified after setup");return after;
    }
    internal static void Ensure(InstallOptions options,InstallAudit audit)
    {
        if(!Selected(options)){audit.Save("prerequisite-runtime.json",new { required=false,reason="localization only" });return;}
        var before=Probe();audit.Save("prerequisite-runtime.json",new { required=true,validated_version=ValidatedVersion,before=before,ready_before=before.Ready });
        audit.Write("VC_RUNTIME required by MarkerPatch loader / DLC; validated="+ValidatedVersion+" MSVCP="+before.MsvcpVersion+" VCRUNTIME="+before.VcruntimeVersion+" ready="+before.Ready);
        var after=RunFlow(options,AllowInteractive,before,
            ()=>MessageBox.Show(Form.ActiveForm,L.T("MarkerPatch 和 Severed DLC 共用必需的 Microsoft Visual C++ v14（x86／32 位）运行库。\r\n检测到未安装、版本较旧或文件不完整。是否打开微软官方安装程序？完成后将重新检查，再继续安装。"),L.T("安装必需运行库"),MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes,
            ()=>RunBundledInstaller(audit),Probe,audit.Write);
        audit.Save("prerequisite-runtime.json",new { required=true,validated_version=ValidatedVersion,before=before,after=after,ready=after.Ready });
    }
    internal static bool ValidateInstaller(string path)
    {
        var actual=InstallAudit.Digest(path,null);if(actual.status!="ok"||actual.bytes!=RedistBytes||actual.sha256!=RedistSha)return false;
        if(VerifyTrust(path)!=0)return false;
        using(var certificate=new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))return certificate.Subject.IndexOf("O=Microsoft Corporation",StringComparison.OrdinalIgnoreCase)>=0;
    }
    static int RunBundledInstaller(InstallAudit audit)
    {
        string folder=Path.Combine(audit.Folder,"prerequisite"),target=Path.Combine(folder,"vc_redist.x86.exe");Directory.CreateDirectory(folder);
        try
        {
            using(var resource=typeof(InstallerEngine).Assembly.GetManifestResourceStream("DS2_VC_REDIST_X86"))using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None))resource.CopyTo(output);
            if(!ValidateInstaller(target))throw new SecurityException(L.T("无法验证微软运行库安装包，请从微软官网下载 x86 运行库安装后重试。")+" "+OfficialDownload);
            var start=new ProcessStartInfo(target,"/install /norestart /log \""+Path.Combine(audit.Folder,"vc-redist-install.log")+"\"") { UseShellExecute=false,WindowStyle=ProcessWindowStyle.Normal };
            audit.Write("VC_RUNTIME opening signed Microsoft setup UI; no automatic restart");
            using(var process=Process.Start(start)){while(!process.WaitForExit(200)){Application.DoEvents();}return process.ExitCode;}
        }
        finally
        {
            // Delete only our exact temporary copy after setup exits. No shared
            // runtime, registry key, game file or other directory is removed.
            try{if(File.Exists(target))File.Delete(target);if(Directory.Exists(folder)&&Directory.GetFileSystemEntries(folder).Length==0)Directory.Delete(folder);}catch(Exception error){audit.Problems.Add("prerequisite temp cleanup: "+error.Message);}
        }
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct TrustFile { public uint Size;[MarshalAs(UnmanagedType.LPWStr)]public string Path;public IntPtr File,Subject; }
    [StructLayout(LayoutKind.Sequential)]struct TrustData { public uint Size;public IntPtr Policy,Sip;public uint Ui,Revocation,Choice;public IntPtr File;public uint State;public IntPtr StateHandle,Url;public uint Flags,Context;public IntPtr SignatureSettings; }
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern int WinVerifyTrust(IntPtr window,ref Guid action,ref TrustData data);
    internal static int VerifyTrust(string path)
    {
        var file=new TrustFile { Size=(uint)Marshal.SizeOf(typeof(TrustFile)),Path=Path.GetFullPath(path) };IntPtr pointer=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrustFile)));Marshal.StructureToPtr(file,pointer,false);
        var data=new TrustData { Size=(uint)Marshal.SizeOf(typeof(TrustData)),Ui=2,Choice=1,File=pointer,State=1,Flags=0x1000 };var action=new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
        try{return WinVerifyTrust(new IntPtr(-1),ref action,ref data);}finally{data.State=2;WinVerifyTrust(new IntPtr(-1),ref action,ref data);Marshal.DestroyStructure(pointer,typeof(TrustFile));Marshal.FreeHGlobal(pointer);}
    }
}
