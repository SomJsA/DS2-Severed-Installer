using System;using System.IO;using System.Reflection;using System.Security.Cryptography;
class TitleFontSourceChecks {
 static Type engine;static int n;static BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
 static void Validate(string game){try{engine.GetMethod("ValidateTitleFontSource",flags).Invoke(null,new object[]{game});}catch(TargetInvocationException e){throw e.InnerException;}}
 static string Hash(string p){using(var f=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
 static bool Refuses(string p){try{Validate(p);return false;}catch{return true;}}
 static uint Be32(BinaryReader r){var b=r.ReadBytes(4);return ((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3];}
 static long FrontendOffset(string path){using(var f=File.OpenRead(path))using(var r=new BinaryReader(f)){f.Position=8;uint count=Be32(r);Be32(r);for(uint i=0;i<count;i++){uint offset=Be32(r);Be32(r);if(Be32(r)==0x15C7731F)return offset;}throw new Exception("fixture has no frontend stream");}}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);n++;}
 static int Main(string[] args){try{
  string root=Path.GetFullPath(args[3]);if(!root.StartsWith(@"E:\DS2-Severed-Patch\work\")||Directory.Exists(root))throw new Exception("fresh project fixture required");Directory.CreateDirectory(root);
  var asm=Assembly.LoadFrom(args[0]);engine=asm.GetType("InstallerEngine");asm.GetType("L").GetMethod("Initialize",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,new object[]{"en"});
  string dat6=Path.Combine(root,"DS2DAT6.DAT"),dat3=Path.Combine(root,"DS2DAT3.DAT");File.Copy(args[1],dat6);string sourceHash=Hash(dat6);
  Validate(root);Check(Hash(dat6)==sourceHash,"supported frontend passes without changing resources");
  File.WriteAllText(dat3,"FOREIGN MOD DO NOT TOUCH");Check(Refuses(root),"foreign DAT3 rejected before title overlay");Check(File.ReadAllText(dat3)=="FOREIGN MOD DO NOT TOUCH","foreign archive preserved byte-for-byte");File.Move(dat3,dat3+".foreign-preserved");
  File.Copy(args[2],dat3);string known=Hash(dat3);Validate(root);Check(Hash(dat3)==known,"newly installed verified icons pass before ownership is committed");File.WriteAllText(Path.Combine(root,"DS2SeveredOneClick.install.txt"),"owns_save_icons=True\r\nsave_icons_sha256="+known+"\r\n");Validate(root);Check(Hash(dat3)==known,"owned verified save-icon archive remains compatible");
  using(var f=File.Open(dat3,FileMode.Open,FileAccess.ReadWrite)){f.Position=2048;int b=f.ReadByte();f.Position=2048;f.WriteByte((byte)(b^1));}Check(Refuses(root),"modified owned DAT3 rejected");
  File.Move(dat3,dat3+".modified-preserved");long at=FrontendOffset(dat6)+2;using(var f=File.Open(dat6,FileMode.Open,FileAccess.ReadWrite)){f.Position=at;int b=f.ReadByte();f.Position=at;f.WriteByte((byte)(b^1));}Check(Refuses(root),"modified frontend rejected instead of being silently masked");
  Console.WriteLine("ALL TITLE FONT SOURCE CHECKS PASSED "+n);return 0;
 }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}}
}
