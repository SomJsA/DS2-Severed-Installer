using System;using System.IO;using System.Reflection;using System.Security.Cryptography;using System.Collections.Generic;using System.Linq;
class BugPluginChecks {
 static Type e;static BindingFlags flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;static int n,id;static string root;static Action<string> log=x=>{};
 static object Call(string name,params object[] a){try{return e.GetMethod(name,flags).Invoke(null,a);}catch(TargetInvocationException ex){throw ex.InnerException;}}
 static void Check(bool b,string text){if(!b)throw new Exception(text);n++;Console.WriteLine("PASS "+text);}
 static string Fresh(){string p=Path.Combine(root,"case-"+(++id));Directory.CreateDirectory(Path.Combine(p,"plugins"));File.WriteAllText(Path.Combine(p,"deadspace2.exe"),"fixture - never execute");return p;}
 static string Hash(string p){using(var s=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static void Record(string p){File.WriteAllBytes(Path.Combine(p,@"plugins\SeveredPC.asi"),(byte[])Call("ReadResource","DS2_SEVERED_ASI"));File.WriteAllText(Path.Combine(p,"DS2SeveredOneClick.install.txt"),"version=15\r\nchinese=False\r\ndlc=False\r\nmarker_patch=False\r\nowns_dat2=False\r\nowns_dlc_runtime=True\r\n"+(string)Call("BugPluginState",p,true));}
 static bool Refused(Action a){try{a();return false;}catch(IOException){return true;}catch(InvalidDataException){return true;}catch(InvalidOperationException){return true;}}
 static void Preflight(string g){Call("PreflightUninstall",g,false,true,false,false,null,(string)e.GetField("SeveredAsiSha",flags).GetRawConstantValue(),false,null);}
 static int Main(string[] args){try{
  root=Path.GetFullPath(args[1]);if(!root.StartsWith(@"E:\DS2-Severed-Patch\work\")||Directory.Exists(root))throw new Exception("fresh isolated work root required");Directory.CreateDirectory(root);e=Assembly.LoadFrom(args[0]).GetType("InstallerEngine");
  var names=new[]{"SeveredPowerNodeFix","SeveredSaveDisplay","SeveredMiningCampFix"};var keys=new[]{"DS2_NODE_ASI","DS2_SAVE_ASI","DS2_MINING_ASI"};
  for(int i=0;i<3;i++){var b=(byte[])Call("ReadResource",keys[i]);Check(b.Length>0&&b[0]=='M'&&b[1]=='Z',"embedded independent PE "+names[i]);}
  string g=Fresh();var undo=new List<Action>();Call("InstallBugPlugins",g,undo,log);Call("WriteBugConfiguration",g,true,false,undo,log);Record(g);
  Check((bool)Call("BugPluginsReady",g),"all three independent ASIs verified against ownership");
  Check(!File.Exists(Path.Combine(g,@"plugins\SeveredExtras.asi")),"new installation does not recreate combined Extras");
  Check(File.ReadAllText(Path.Combine(g,@"plugins\SeveredNodeFix.ini")).Contains("EnablePowerNodeShopFix=1"),"node preference reaches independent INI");
  Check(File.ReadAllText(Path.Combine(g,@"plugins\SeveredMiningCampFix.ini")).Contains("InitializationFix=1"),"mining fix enabled only through its own INI");
  Call("WriteBugConfiguration",g,false,true,new List<Action>(),log);
  Check(File.ReadAllText(Path.Combine(g,@"plugins\SeveredNodeFix.ini")).Contains("EnablePowerNodeShopFix=0"),"explicit node disable preserved");
  Check(File.ReadAllText(Path.Combine(g,@"plugins\SeveredSaveDisplay.ini")).Contains("SimplifiedChinese=1"),"game localization reaches save display independently");
  string save=Path.Combine(g,@"plugins\SeveredSaveDisplay.asi"),node=Path.Combine(g,@"plugins\SeveredPowerNodeFix.asi");string nodeBefore=Hash(node);File.WriteAllText(save,"changed by user");
  Check(Refused(()=>Preflight(g)),"changed second ASI stops removal before any mutation");Check(Hash(node)==nodeBefore&&File.ReadAllText(save)=="changed by user","all current files retained after refused preflight");
  g=Fresh();string external=Path.Combine(g,@"plugins\SeveredSaveDisplay.asi"),ini=Path.Combine(g,@"plugins\SeveredSaveDisplay.ini");File.WriteAllText(external,"external plugin");File.WriteAllText(ini,"external configuration");Call("InstallBugPlugins",g,new List<Action>(),log);Call("WriteBugConfiguration",g,true,false,new List<Action>(),log);Record(g);Preflight(g);Call("RemoveBugPlugins",g,log);
  Check(File.ReadAllText(external)=="external plugin"&&File.ReadAllText(ini)=="external configuration","uninstall restores external same-name plugin and configuration");
  Check(!File.Exists(Path.Combine(g,@"plugins\SeveredPowerNodeFix.asi"))&&!File.Exists(Path.Combine(g,@"plugins\SeveredMiningCampFix.ini")),"newly owned files removed without leaving active fixes");
  g=Fresh();undo=new List<Action>();Call("InstallBugPlugins",g,undo,log);Call("WriteBugConfiguration",g,true,true,undo,log);for(int i=undo.Count-1;i>=0;i--)undo[i]();Check(!Directory.GetFiles(Path.Combine(g,"plugins")).Any(),"installation failure undo restores a clean plugin directory");
  g=Fresh();File.WriteAllText(Path.Combine(g,@"plugins\SeveredExtras.asi"),"external combined plugin");Check(Refused(()=>Call("InstallBugPlugins",g,new List<Action>(),log)),"combined plugin collision refused before installing duplicate hooks");
  g=Fresh();Call("InstallBugPlugins",g,new List<Action>(),log);Record(g);string record=Path.Combine(g,"DS2SeveredOneClick.install.txt");File.WriteAllText(record,File.ReadAllText(record).Replace("bug_plugin_1_sha256=","bug_plugin_1_sha256=unknown"));Check(Refused(()=>Preflight(g)),"unknown binary ownership record refused");
  Console.WriteLine("ALL BUG PLUGIN CHECKS PASSED "+n);return 0;
 }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}}
}
