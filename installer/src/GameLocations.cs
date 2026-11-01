using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

internal sealed class GameLocation
{
    public string Path;
    public bool Nearby;
    public override string ToString()
    {
        string type=Path.IndexOf("steamapps",StringComparison.OrdinalIgnoreCase)>=0?"Steam 版":
            Path.IndexOf("EA Games",StringComparison.OrdinalIgnoreCase)>=0?"EA 版":Nearby?"附近的游戏":"游戏";
        return type+"  ·  "+Path;
    }
}
internal static class GameLocations
{
    private static bool IsGame(string path)
    {
        try{return !string.IsNullOrWhiteSpace(path)&&File.Exists(System.IO.Path.Combine(path,"deadspace2.exe"));}
        catch{return false;}
    }
    public static List<GameLocation> Find(string applicationDirectory)
    {
        var results=new List<GameLocation>();
        Action<string,bool> add=(path,nearby)=>
        {
            if(!IsGame(path))return;
            string full=System.IO.Path.GetFullPath(path).TrimEnd('\\','/');
            if(!results.Any(x=>string.Equals(x.Path,full,StringComparison.OrdinalIgnoreCase)))
                results.Add(new GameLocation{Path=full,Nearby=nearby});
        };
        string parent=null;
        try{parent=Directory.GetParent(applicationDirectory.TrimEnd('\\','/')).FullName;}catch{}
        foreach(string folder in new[]{applicationDirectory,parent}.Where(x=>!string.IsNullOrEmpty(x)))
        {
            add(folder,true);
            foreach(string name in new[]{"Dead Space 2","DeadSpace2","Game"})add(System.IO.Path.Combine(folder,name),true);
        }
        var steamRoots=new List<string>();
        foreach(string keyPath in new[]{@"Software\Valve\Steam",@"SOFTWARE\WOW6432Node\Valve\Steam"})
        foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine})
        {
            try{using(var key=hive.OpenSubKey(keyPath)){if(key!=null){object value=key.GetValue("SteamPath")??key.GetValue("InstallPath");if(value!=null)steamRoots.Add(value.ToString());}}}catch{}
        }
        steamRoots.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam"));
        foreach(string drive in Environment.GetLogicalDrives())steamRoots.Add(System.IO.Path.Combine(drive,"SteamLibrary"));
        foreach(string root in steamRoots.ToArray())
        foreach(string file in new[]{System.IO.Path.Combine(root,"steamapps","libraryfolders.vdf"),System.IO.Path.Combine(root,"config","libraryfolders.vdf")})
        {
            try{if(File.Exists(file))foreach(Match match in Regex.Matches(File.ReadAllText(file),"\"path\"\\s+\"([^\"]+)\""))steamRoots.Add(match.Groups[1].Value.Replace(@"\\",@"\"));}catch{}
        }
        foreach(string root in steamRoots.Distinct(StringComparer.OrdinalIgnoreCase))add(System.IO.Path.Combine(root,"steamapps","common","Dead Space 2"),false);
        foreach(string keyPath in new[]{@"SOFTWARE\EA Games\Dead Space 2",@"SOFTWARE\WOW6432Node\EA Games\Dead Space 2"})
        foreach(var hive in new[]{Registry.LocalMachine,Registry.CurrentUser})
        {
            try{using(var key=hive.OpenSubKey(keyPath)){if(key!=null)foreach(string name in new[]{"Install Dir","InstallDir","InstallLocation"}){object value=key.GetValue(name);if(value!=null)add(value.ToString(),false);}}}catch{}
        }
        foreach(string keyPath in new[]{@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"})
        foreach(var hive in new[]{Registry.LocalMachine,Registry.CurrentUser})
        {
            try
            {
                using(var uninstall=hive.OpenSubKey(keyPath))
                {
                    if(uninstall==null)continue;
                    foreach(string child in uninstall.GetSubKeyNames())using(var key=uninstall.OpenSubKey(child))
                    {
                        if(key==null)continue;
                        string name=Convert.ToString(key.GetValue("DisplayName")).Replace("™","").Trim();
                        if(!name.Equals("Dead Space 2",StringComparison.OrdinalIgnoreCase))continue;
                        add(Convert.ToString(key.GetValue("InstallLocation")),false);
                        string icon=Convert.ToString(key.GetValue("DisplayIcon"));
                        if(!string.IsNullOrWhiteSpace(icon))try{add(System.IO.Path.GetDirectoryName(icon.Split(',')[0].Trim('"')),false);}catch{}
                    }
                }
            }
            catch{}
        }
        foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)})
            if(!string.IsNullOrEmpty(root))add(System.IO.Path.Combine(root,"EA Games","Dead Space 2"),false);
        return results;
    }
    public static string FindPackage(string applicationDirectory,string gameDirectory)
    {
        string parent=null;try{parent=Directory.GetParent(applicationDirectory.TrimEnd('\\','/')).FullName;}catch{}
        foreach(string folder in new[]{applicationDirectory,parent,gameDirectory}.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        foreach(string name in new[]{"Severed.marker","marker.DLC","DLC.marker"})
        {
            string path=System.IO.Path.Combine(folder,name);if(File.Exists(path))return path;
        }
        return System.IO.Path.Combine(applicationDirectory,"Severed.marker");
    }
}
internal static class LaunchGuard
{
    public static string BlockReason(string executablePath)
    {
        string full;
        try{full=System.IO.Path.GetFullPath(executablePath);}catch{return "程序路径无效，请完整解压后运行。";}
        string temp=System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()).TrimEnd('\\','/')+System.IO.Path.DirectorySeparatorChar;
        if(full.StartsWith(temp,StringComparison.OrdinalIgnoreCase)||Regex.IsMatch(full,@"[\\/](Rar\$[^\\/]*|7z[OE][A-Za-z0-9._-]*|Temp\d*_[^\\/]*\.(zip|rar|7z))([\\/]|$)",RegexOptions.IgnoreCase))
            return "请先将整个分发包完整解压到固定文件夹，再运行安装程序。不要在压缩软件内直接打开 EXE。";
        return null;
    }
}
