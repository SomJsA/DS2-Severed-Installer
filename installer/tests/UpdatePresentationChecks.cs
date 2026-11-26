using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
class UpdatePresentationChecks
{
    static BindingFlags instance=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,stat=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
    static void Set(object value,string field,object data){value.GetType().GetField(field,instance).SetValue(value,data);}
    static IEnumerable<Control> All(Control root){foreach(Control child in root.Controls){yield return child;foreach(Control item in All(child))yield return item;}}
    static Rectangle InForm(Control c,Form form){Point p=c.Location;for(Control parent=c.Parent;parent!=form;parent=parent.Parent)p.Offset(parent.Location);return new Rectangle(p,c.Size);}
    [STAThread]static int Main(string[]args)
    {
        Console.OutputEncoding=new UTF8Encoding(false);
        try{
            Application.EnableVisualStyles();Assembly asm=Assembly.LoadFrom(args[0]);Type language=asm.GetType("L"),type=asm.GetType("InstallerForm");
            foreach(string locale in new[]{"zh-CN","en"}){
                bool en=locale=="en";language.GetMethod("Initialize",stat).Invoke(null,new object[]{locale});
                using(Form owner=(Form)Activator.CreateInstance(type,true)){
                    type.GetProperty("PreviewOnly").SetValue(owner,true,null);
                    object installed=Activator.CreateInstance(asm.GetType("InstalledSelection"),true);
                    Set(installed,"HasRecord",true);Set(installed,"Version","2026.11.11.20");Set(installed,"MarkerPresent",true);Set(installed,"MarkerIsCurrent",false);Set(installed,"MarkerVersion","3.0.7");
                    Set(owner,"installedSelection",installed);type.GetMethod("UpdateMaintenanceSummary",instance).Invoke(owner,null);
                    string hint=((Label)type.GetField("installedHint",instance).GetValue(owner)).Text;
                    Check(hint.Contains("2026.11.11.20")&&hint.Contains("3.0.7"),"record and bundled component versions retained");
                    Check(!hint.Contains("旧版")&&!hint.Contains("Legacy"),"record timestamp does not imply outdated installer");
                    foreach(string scenario in new[]{"marker","installer","both","none","failed","marker-partial"}){
                        bool marker=scenario=="marker"||scenario=="both"||scenario=="marker-partial";
                        bool app=scenario=="installer"||scenario=="both";
                        bool error=scenario=="failed"||scenario=="marker-partial";
                        object report=Activator.CreateInstance(asm.GetType("UpdateReport"),true);Set(report,"MarkerUpdate",marker);Set(report,"MarkerNotes","3.3.1 fixture");
                        Set(report,"MarkerPage","https://github.com/Wemino/MarkerPatch/releases/tag/3.3.1");
                        if(error)Set(report,"Error","network fixture");
                        if(app){object release=Activator.CreateInstance(asm.GetType("ReleaseUpdate"),true);Set(release,"Tag","v2099.1.1");Set(release,"Page","https://github.com/SomJsA/DS2-Severed-Installer/releases/tag/v2099.1.1");Set(report,"Installer",release);}
                        using(Form dialog=(Form)type.GetMethod("CreateUpdateDialog",instance).Invoke(owner,new[]{report})){
                            string body=All(dialog).OfType<TextBox>().First(b=>b.Multiline).Text;
                            string address=All(dialog).OfType<TextBox>().First(b=>!b.Multiline).Text;
                            if(marker&&!app){Check(dialog.Text==(en?"MarkerPatch update":"MarkerPatch 更新"),"component-only title");Check(!body.Contains(en?"Installer update available":"安装器有可用更新"),"component update does not offer installer upgrade");Check(address.Contains("Wemino/MarkerPatch"),"component-only link goes to upstream");}
                            if(app){Check(dialog.Text==(en?(marker?"Installer and MarkerPatch updates":"Installer update"):(marker?"安装器与 MarkerPatch 更新":"安装器更新")),"installer/both title");Check(body.Contains("v2099.1.1"),"real installer update remains visible");Check(address.Contains("SomJsA/DS2-Severed-Installer"),"installer update link stays with installer");}
                            if(error)Check(!body.Contains(en?"installer is up to date":"安装器已是最新版本"),"failed query never claims current installer");
                            if(scenario=="none")Check(body.Contains(en?"installer is up to date":"安装器已是最新版本"),"successful empty result is current");
                            Check(!owner.Visible&&!dialog.Visible,"dialog construction leaves windows hidden");
                            foreach(Size area in new[]{new Size(1280,680),new Size(800,560),new Size(640,440)}){
                                type.GetMethod("FitWindowToWorkingArea",stat).Invoke(null,new object[]{dialog,new Rectangle(Point.Empty,area)});dialog.PerformLayout();
                                Check(dialog.Width<=area.Width&&dialog.Height<=area.Height,"dialog stays within work area");
                                foreach(Button button in All(dialog).OfType<Button>())Check(dialog.ClientRectangle.Contains(InForm(button,dialog)),"dialog action stays reachable");
                            }
                            if(scenario=="marker"){
                                dialog.Opacity=0;dialog.ShowInTaskbar=false;dialog.Show();Application.DoEvents();
                                type.GetMethod("FitWindowToWorkingArea",stat).Invoke(null,new object[]{dialog,new Rectangle(0,0,640,440)});
                                using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(args[1],"marker-only-"+locale+".png"),ImageFormat.Png);}
                            }
                        }
                    }
                }
                Console.WriteLine("PASS "+locale+" update scenarios");
            }
            Console.WriteLine("UPDATE PRESENTATION CHECKS PASSED "+checks);return 0;
        }catch(Exception error){Console.WriteLine("FAIL "+error);return 1;}
    }
}
