using System;using System.IO;using System.Reflection;using System.Security.Cryptography;using System.Collections.Generic;
class Resource19Checks {
 static Type e;static int n;static BindingFlags f=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;static Action<string> log=x=>{};
 static object Call(string name,params object[] a){try{return e.GetMethod(name,f).Invoke(null,a);}catch(TargetInvocationException ex){throw ex.InnerException;}}
 static string Hash(string p){using(var s=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static void Check(bool b,string text){if(!b)throw new Exception(text);n++;Console.WriteLine("PASS "+text);}
 static int Main(string[] a){try{
  string g=Path.GetFullPath(a[1]);if(!g.StartsWith(@"E:\DS2-Severed-Patch\work\"))throw new Exception("isolated fixture required");e=Assembly.LoadFrom(a[0]).GetType("InstallerEngine");string temp=Path.Combine(g,"temp");
  string icon=(string)Call("PrepareSaveIcons",g,temp,log);Check(icon!=null,"managed builder generates both menu and native save resources");
  Check(Hash(icon)=="adc7195c9a32a14fa8087f07d870d271e14aa039ba947db141d17fdebcaaf1b6","managed reconstruction matches independent combined archive exactly");
  File.Copy(icon,Path.Combine(g,"DS2DAT3.DAT"));Call("ValidateTitleFontSource",g);Check(true,"combined icon resource remains compatible with title font validation");
  var undo=new List<Action>();Check((bool)Call("InstallChinese",g,temp,a[2],log,undo,false),"real localization pipeline completes in isolated original DAT0");
  Check(Hash(Path.Combine(g,"DS2DAT0.DAT"))=="1e2363ed8613f910ca4bb0345d5a5eb3157fad5f51f935c53661bda9a803d803","complete rebuilt Chinese DAT0 matches independent full-file SHA256");
  Check(Hash(Path.Combine(g,"DS2DAT0.DAT.ds2oneclick.bak"))=="fd7d1bdbb5ec6b92324cd877465a029942c78396468695b4551068f4c3562e23","original DAT0 backup verified intact");
  for(int i=undo.Count-1;i>=0;i--)undo[i]();Check(Hash(Path.Combine(g,"DS2DAT0.DAT"))=="fd7d1bdbb5ec6b92324cd877465a029942c78396468695b4551068f4c3562e23","failed later step can restore exact original DAT0");
  Console.WriteLine("ALL RELEASE RESOURCE CHECKS PASSED "+n);return 0;
 }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}}
}
