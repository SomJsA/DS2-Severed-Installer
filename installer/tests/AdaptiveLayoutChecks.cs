using System;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

class AdaptiveLayoutChecks
{
    static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    static Control Field(Form form, string name) { return (Control)form.GetType().GetField(name, Instance).GetValue(form); }
    static Rectangle InForm(Control control, Control form)
    {
        Point p = control.Location;
        for (Control parent = control.Parent; parent != form; parent = parent.Parent) p.Offset(parent.Location);
        return new Rectangle(p, control.Size);
    }
    [STAThread] static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        try
        {
            Application.EnableVisualStyles();
            Assembly asm = Assembly.LoadFrom(args[0]);
            Type language = asm.GetType("L"), type = asm.GetType("InstallerForm");
            language.GetMethod("Initialize", Static).Invoke(null, new object[] { "zh-CN" });
            // Logical work areas: physical resolution divided by Windows scaling,
            // less taskbar space. No display or registry settings are changed.
            var areas = new[] {
                new Size(1280,680), new Size(1920,1040), new Size(1536,824),
                new Size(960,500), new Size(1366,728), new Size(1280,680),
                new Size(1024,728), new Size(800,560), new Size(640,440) };
            foreach (Size area in areas)
            foreach (string locale in new[] { "zh-CN", "en" })
            using (Form form = (Form)Activator.CreateInstance(type, true))
            {
                type.GetProperty("PreviewOnly", Instance).SetValue(form, true, null);
                // WinForms only computes real scroll ranges for visible windows.
                // Exercise that path in a transparent, taskbar-hidden preview.
                form.Opacity = 0;
                form.ShowInTaskbar = false;
                form.Show();
                Application.DoEvents();
                language.GetMethod("SetLanguage", Static).Invoke(null, new object[] { locale });
                type.GetMethod("ApplyLanguage", Instance).Invoke(form, null);
                MethodInfo fit = type.GetMethod("FitToWorkingArea", Instance);
                if (fit == null) form.Size = new Size(Math.Min(form.Width,area.Width),Math.Min(form.Height,area.Height));
                else fit.Invoke(form, new object[] { new Rectangle(100,50,area.Width,area.Height) });
                form.PerformLayout();
                Rectangle install = InForm(Field(form,"installButton"),form);
                Rectangle uninstall = InForm(Field(form,"uninstallButton"),form);
                Check(form.ClientRectangle.Contains(install), "install button clipped at " + area + " " + locale);
                Check(form.ClientRectangle.Contains(uninstall), "uninstall button clipped at " + area + " " + locale);
                Check(!install.IntersectsWith(uninstall), "action buttons overlap");
                Check(form.Width <= area.Width && form.Height <= area.Height, "window exceeds work area");
                Check(form.Opacity == 0 && !form.ShowInTaskbar,"test must remain invisible");
                Panel viewport = Field(form,"contentViewport") as Panel;
                Check(viewport != null && viewport.AutoScroll,"content must be scrollable");
                IntPtr viewportHandle = viewport.Handle;
                Check(!viewport.Bounds.IntersectsWith(install),"footer overlaps scrolling content");
                if(area.Width >= 800)
                {
                    Check(!viewport.HorizontalScroll.Visible && !viewport.VerticalScroll.Visible,"ordinary small desktop must fit without scrollbars: " + area);
                    Check(viewport.Bounds.Contains(InForm(Field(form,"chineseBox"),form)),"component card clipped before scrolling");
                    Check(viewport.Bounds.Contains(InForm(Field(form,"installedHint"),form)),"installation state clipped before scrolling");
                }
                // Both the last settings row and the top of the content remain reachable.
                viewport.AutoScrollPosition = new Point(920,740);
                Check(viewport.Bounds.Contains(InForm(Field(form,"markerIssuesLink"),form)),"last row not reachable: viewport=" + viewport.Bounds + " row=" + InForm(Field(form,"markerIssuesLink"),form) + " scroll=" + viewport.AutoScrollPosition + " display=" + viewport.DisplayRectangle);
                viewport.AutoScrollPosition = Point.Empty;
                viewport.ScrollControlIntoView(Field(form,"chineseBox"));
                Check(viewport.Bounds.Contains(InForm(Field(form,"chineseBox"),form)),"component selection not reachable");
                Check(form.ClientRectangle.Contains(InForm(Field(form,"installButton"),form)),"scrolling moves install button");
                language.GetMethod("SetLanguage", Static).Invoke(null, new object[] { locale == "en" ? "zh-CN" : "en" });
                type.GetMethod("ApplyLanguage", Instance).Invoke(form, null);
                Check(form.ClientRectangle.Contains(InForm(Field(form,"installButton"),form)),"language switch clips install button");
                if(area.Width >= 800) Check(!viewport.HorizontalScroll.Visible && !viewport.VerticalScroll.Visible,"language switch adds scrollbars");
                if (area.Width <= 800)
                {
                    object report = Activator.CreateInstance(asm.GetType("UpdateReport"),true);
                    using(Form dialog = (Form)type.GetMethod("CreateUpdateDialog",Instance).Invoke(form,new[]{report}))
                    {
                        dialog.Opacity = 0; dialog.ShowInTaskbar = false; dialog.Show();
                        type.GetMethod("FitWindowToWorkingArea",Static).Invoke(null,new object[]{dialog,new Rectangle(Point.Empty,area)});
                        dialog.PerformLayout();
                        Check(dialog.AutoScroll,"secondary dialog must scroll");
                        Check(dialog.Width <= area.Width && dialog.Height <= area.Height,"secondary dialog exceeds work area");
                        dialog.AutoScrollPosition = new Point(1000,1000);
                        Check(dialog.ClientRectangle.Contains(InForm((Control)dialog.CancelButton,dialog)),"secondary dialog cancel button unreachable");
                    }
                }
                Console.WriteLine("PASS " + area.Width + "x" + area.Height + " " + locale);
            }
            Console.WriteLine("ADAPTIVE LAYOUT CHECKS PASSED " + checks);
            return 0;
        }
        catch (Exception error) { Console.WriteLine("FAIL " + error); return 1; }
    }
}
