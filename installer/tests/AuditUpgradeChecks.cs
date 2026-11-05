using System;using System.IO;using System.Reflection;using System.Security.Cryptography;using System.Text;
class AuditUpgradeChecks {
 static Type engine;static int checks;static BindingFlags f=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 static object Call(string name,params object[] args){try{return engine.GetMethod(name,f).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
 static string Hash(string path){using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);checks++;}
 static int Main(string[]a){Console.OutputEncoding=new UTF8Encoding(false);string root=Path.GetFullPath(a[1]);if(!root.StartsWith(@"E:\DS2-Severed-Patch\work")||Directory.Exists(root))throw new Exception("fresh project test root required");Directory.CreateDirectory(root);string[] names={"DS2DAT1.DAT","DS2DAT6.DAT","DS2DAT7.DAT","DS2DAT8.DAT","DS2DAT9.DAT"};try{
 var asm=Assembly.LoadFrom(a[0]);engine=asm.GetType("InstallerEngine");asm.GetType("L").GetMethod("Initialize",f).Invoke(null,new object[]{"en"});
 File.WriteAllText(Path.Combine(root,"deadspace2.exe"),"NONEXECUTABLE isolated fixture");File.WriteAllText(Path.Combine(root,"unrelated.txt"),"preserve unrelated file");
 string dat0=Path.Combine(root,"DS2DAT0.DAT"),bak=dat0+".ds2oneclick.bak",state=Path.Combine(root,"DS2SeveredOneClick.install.txt");
 // Only independent full copies are used. Never create links to live or backup files.
 File.Copy(a[2],dat0,false);File.Copy(a[4],bak,false);foreach(string name in names)File.Copy(Path.Combine(a[3],name),Path.Combine(root,name),false);
 File.WriteAllText(state,"version=14\r\ninstaller_release=2026.11.4.17\r\nchinese=True\r\nselected_chinese=True\r\nselected_dlc=False\r\nselected_marker=False\r\ndlc=False\r\nowns_dat2=False\r\nmarker_patch=False\r\nowns_dlc_runtime=False\r\nowns_extras_runtime=False\r\nowns_runtime_config=False\r\n");
 Check(Hash(dat0)=="3311f5c137f390b6b01435403d48ba5b5faa2d2d893a54069322b55dea9a67d8","fixture begins with independently copied 3.17 punctuation candidate DAT0");
 var options=Activator.CreateInstance(asm.GetType("InstallOptions"),true);foreach(string key in new[]{"Dlc","MarkerPatch","NodeFix","DeployManager"})options.GetType().GetField(key).SetValue(options,false);options.GetType().GetField("Chinese").SetValue(options,true);
 Action<string> log=(text)=>File.AppendAllText(Path.Combine(root,"engine.log"),text+Environment.NewLine,Encoding.UTF8);
 Call("ApplySelection",root,options,log);
 Check(Hash(dat0)=="1e2363ed8613f910ca4bb0345d5a5eb3157fad5f51f935c53661bda9a803d803","public upgrade applies exact accepted fonts, short labels and save APX archive");
 Check(Hash(bak)=="fd7d1bdbb5ec6b92324cd877465a029942c78396468695b4551068f4c3562e23","verified pristine backup retained during upgrade");
 Check(File.ReadAllText(state).Contains("version=15")&&File.ReadAllText(state).Contains("installer_release=2026.11.6.19"),"successful data upgrade stamps new schema and release");
 Check(!File.Exists(Path.Combine(root,"DS2DAT2.DAT"))&&!File.Exists(Path.Combine(root,"dinput8.dll")),"Chinese-only upgrade adds no DLC or MarkerPatch component");
 string report=(string)asm.GetType("InstallAudit").GetField("LastReport",f).GetValue(null);Check(File.Exists(report),"full-data upgrade has retrievable report ZIP");
 using(var zip=System.IO.Compression.ZipFile.OpenRead(report)){
 using(var rd=new StreamReader(zip.GetEntry("prerequisite-runtime.json").Open()))Check(rd.ReadToEnd().Contains("\"required\":false"),"Chinese-only install skips runtime prerequisite");
 using(var rd=new StreamReader(zip.GetEntry("snapshot-before.csv").Open()))Check(rd.ReadToEnd().Contains("1bb7beeda1b6a42365ef64a69c3822b0"),"before CSV records exact candidate MD5");
 using(var rd=new StreamReader(zip.GetEntry("snapshot-after.csv").Open()))Check(rd.ReadToEnd().Contains("d4c9b80096c2ef9ab35030da99bb9d39"),"after CSV matches independent stable MD5 reference");
 using(var rd=new StreamReader(zip.GetEntry("verification.json").Open()))Check(rd.ReadToEnd().Contains("\"passed\":true"),"all full-data expected comparisons pass");}
 Call("Uninstall",root,log);
 Check(Hash(dat0)=="fd7d1bdbb5ec6b92324cd877465a029942c78396468695b4551068f4c3562e23","candidate uninstaller restores exact original DAT0");
 Check(!File.Exists(bak)&&!File.Exists(state),"uninstall removes only consumed managed state and backup");
 Check(File.ReadAllText(Path.Combine(root,"unrelated.txt"))=="preserve unrelated file"&&File.ReadAllText(Path.Combine(root,"deadspace2.exe"))=="NONEXECUTABLE isolated fixture","unrelated file and fixture executable preserved");
 Check(Hash(a[2])=="3311f5c137f390b6b01435403d48ba5b5faa2d2d893a54069322b55dea9a67d8"&&Hash(a[4])=="fd7d1bdbb5ec6b92324cd877465a029942c78396468695b4551068f4c3562e23","read-only source archives remain unchanged");
 Console.WriteLine("ALL AUDITED DATA UPGRADE CHECKS PASSED "+checks);return 0;
 }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}
 }
}
