using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
class DialogLayoutChecks
{
    static BindingFlags inst=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,stat=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static Type type; static Size area; static int checks; static Exception failure; static string output,locale;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
    static System.Collections.Generic.IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(Control nested in All(c))yield return nested;}}
    static Rectangle InDialog(Control c,Form dialog){Point p=c.Location;for(Control parent=c.Parent;parent!=dialog;parent=parent.Parent)p.Offset(parent.Location);return new Rectangle(p,c.Size);}
    static void Inspect(Form dialog,string name)
    {
        dialog.Opacity=0; dialog.ShowInTaskbar=false;
        type.GetMethod("FitWindowToWorkingArea",stat).Invoke(null,new object[]{dialog,new Rectangle(Point.Empty,area)});
        dialog.PerformLayout();
        Check(dialog.Width<=area.Width&&dialog.Height<=area.Height,name+" exceeds work area");
        Check(!dialog.AutoScroll,name+" must not use outer scrolling");
        var actions=All(dialog).OfType<Button>().Where(b=>b.DialogResult!=DialogResult.None).ToArray();
        foreach(Button button in actions)
        {
            Check(dialog.ClientRectangle.Contains(InDialog(button,dialog)),name+" action unreachable: "+button.Text);
        }
        for(int x=0;x<actions.Length;x++)for(int y=x+1;y<actions.Length;y++)Check(!InDialog(actions[x],dialog).IntersectsWith(InDialog(actions[y],dialog)),name+" actions overlap");
        foreach(LinkLabel link in All(dialog).OfType<LinkLabel>())Check(dialog.ClientRectangle.Contains(InDialog(link,dialog)),name+" link clipped");
        var tabs=dialog.Controls.OfType<TabControl>().FirstOrDefault();
        if(tabs!=null)
        {
            Check(tabs.TabPages.Count==2,"both settings tabs present");
            tabs.SelectedIndex=1;dialog.ScrollControlIntoView(tabs);
            var parameters=tabs.TabPages[1];
            var last=parameters.Controls.OfType<NumericUpDown>().OrderBy(c=>c.Top).Last();
            parameters.ScrollControlIntoView(last);
            Check(parameters.ClientRectangle.Contains(last.Bounds),"last parameter unreachable");
            Check(dialog.ClientRectangle.Contains(InDialog(last,dialog)),"last parameter clipped by dialog");
            Check(parameters.VerticalScroll.Visible,"parameter list must scroll");
            Check(!parameters.HorizontalScroll.Visible,"parameters require horizontal scrolling");
            Check(!tabs.Bounds.IntersectsWith(InDialog(actions[0],dialog)),"settings tabs overlap action footer");
        }
        using(var image=new Bitmap(dialog.Width,dialog.Height))
        {dialog.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(output,name+"-"+area.Width+"-"+locale+".png"),ImageFormat.Png);}
    }
    static void Modal(Form owner,string method,string name)
    {
        using(var timer=new Timer{Interval=30})
        {
            timer.Tick+=(s,e)=>{
                Form dialog=Application.OpenForms.Cast<Form>().LastOrDefault(f=>f!=owner);
                if(dialog==null)return;
                timer.Stop();
                try{Inspect(dialog,name);}catch(Exception ex){failure=ex;}
                finally{dialog.DialogResult=DialogResult.Cancel;dialog.Close();}
            };
            timer.Start();
            type.GetMethod(method,inst).Invoke(owner,null);
            if(failure!=null)throw failure;
        }
    }
    [STAThread]static int Main(string[]args)
    {
        try
        {
            Application.EnableVisualStyles();var asm=Assembly.LoadFrom(args[0]);type=asm.GetType("InstallerForm");output=args[1];
            System.IO.Directory.CreateDirectory(output);
            foreach(var size in new[]{new Size(1920,1040),new Size(1280,680),new Size(800,560),new Size(640,440)})
            foreach(string language in new[]{"zh-CN","en"})
            {
                area=size;locale=language;
                asm.GetType("L").GetMethod("Initialize",stat).Invoke(null,new object[]{language});
                using(Form owner=(Form)Activator.CreateInstance(type,true))
                {
                    type.GetProperty("PreviewOnly").SetValue(owner,true,null);
                    owner.Opacity=0;owner.ShowInTaskbar=false;owner.Show();
                    Modal(owner,"ShowMarkerSettingsDialog","settings");
                    Modal(owner,"ShowDetails","details");
                    Modal(owner,"ShowLog","log");
                    var report=Activator.CreateInstance(asm.GetType("UpdateReport"),true);
                    using(Form update=(Form)type.GetMethod("CreateUpdateDialog",inst).Invoke(owner,new[]{report}))
                    {update.Opacity=0;update.ShowInTaskbar=false;update.Show();Inspect(update,"update");update.Close();}
                    var result=Activator.CreateInstance(asm.GetType("WebOpenResult"),true);result.GetType().GetField("Url",inst).SetValue(result,"https://github.com/SomJsA/DS2-Severed-Installer/releases");
                    using(Form manual=(Form)asm.GetType("WebLinks").GetMethod("ManualDialog",stat).Invoke(null,new[]{result}))
                    {manual.Opacity=0;manual.ShowInTaskbar=false;manual.Show();Inspect(manual,"browser-fallback");manual.Close();}
                    Check(!((Control)type.GetField("markerOptionsList",inst).GetValue(owner)).IsDisposed,"shared settings control preserved after close");
                    Check(!((Control)type.GetField("detailsBox",inst).GetValue(owner)).IsDisposed,"shared details control preserved after close");
                    Modal(owner,"ShowMarkerSettingsDialog","settings-reopen");
                    Console.WriteLine("PASS "+area+" "+locale);
                }
            }
            Console.WriteLine("DIALOG LAYOUT CHECKS PASSED "+checks);return 0;
        }
        catch(Exception error){Console.WriteLine("FAIL "+error);return 1;}
    }
}
