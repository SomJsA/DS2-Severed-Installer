using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class InstallerForm
{
    private sealed class LayoutElement
    {
        public Control Control;
        public Rectangle Bounds;
        public Font OriginalFont, ScaledFont;
    }
    private Panel contentViewport, contentCanvas, actionFooter;
    private readonly List<LayoutElement> layoutElements = new List<LayoutElement>();
    private bool arrangingLayout;
    private float layoutScale = 1f;
    private int compactLevel;

    private void InitializeAdaptiveLayout()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.None;
        contentViewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, TabIndex = 0 };
        contentCanvas = new Panel { Size = new Size(920, 740), TabIndex = 0 };
        actionFooter = new Panel { Dock = DockStyle.Bottom, Width = ClientSize.Width, Height = 82, TabIndex = 1 };
        var actions = new Control[] { progressBar, statusLabel, uninstallButton, installButton };
        foreach (Control control in Controls.Cast<Control>().ToArray())
        {
            if (actions.Contains(control)) { control.Top -= 740; actionFooter.Controls.Add(control); }
            else contentCanvas.Controls.Add(control);
            layoutElements.Add(new LayoutElement { Control = control, Bounds = control.Bounds, OriginalFont = control.Font });
        }
        contentViewport.Controls.Add(contentCanvas);
        Controls.Add(contentViewport); Controls.Add(actionFooter);
        ResumeLayout(true);
        ApplyAdaptiveLayout();
    }

    private static int CompactLevelFor(Size size)
    {
        if (size.Width < 850 || size.Height < 560) return 2;
        return size.Width < 920 || size.Height < 822 ? 1 : 0;
    }
    private static int CardHeight(int level) { return level == 0 ? 278 : level == 1 ? 90 : 54; }
    private static int LayoutHeight(int level) { return 822 - (278 - CardHeight(level)); }
    private int LayoutX(int value) { return (int)Math.Round(value * layoutScale); }

    private void ApplyAdaptiveLayout()
    {
        if (contentViewport == null || arrangingLayout) return;
        arrangingLayout = true;
        SuspendLayout(); contentCanvas.SuspendLayout(); actionFooter.SuspendLayout();
        try
        {
            compactLevel = CompactLevelFor(ClientSize);
            int savedHeight = 278 - CardHeight(compactLevel);
            // Always scale the original geometry; repeated resizing must not
            // progressively shrink fonts or controls.
            float fitScale = Math.Min(1f, Math.Min(ClientSize.Width / 920f,
                ClientSize.Height / (float)LayoutHeight(compactLevel)));
            layoutScale = Math.Max(.72f, fitScale);
            actionFooter.Height = LayoutX(82);
            contentCanvas.Size = new Size(fitScale >= .72f ? Math.Min(LayoutX(920), ClientSize.Width) : LayoutX(920),
                fitScale >= .72f ? Math.Min(LayoutX(740 - savedHeight), ClientSize.Height - actionFooter.Height) : LayoutX(740 - savedHeight));
            foreach (var element in layoutElements)
            {
                Control control = element.Control;
                Rectangle bounds = element.Bounds;
                if (control.Parent == contentCanvas)
                {
                    if (control is ImageChoice) bounds.Height = CardHeight(compactLevel);
                    else if (bounds.Top >= 413) bounds.Y -= savedHeight;
                    if (control == selectionHint) bounds.X = L.IsEnglish ? 770 : 844;
                    if (control == subtitleScaleBox) bounds.X = L.IsEnglish ? 130 : 108;
                    if (control == subtitleUnit) bounds.X = L.IsEnglish ? 217 : 194;
                }
                float fontSize = element.OriginalFont.SizeInPoints * layoutScale;
                if (element.ScaledFont == null || Math.Abs(element.ScaledFont.SizeInPoints - fontSize) > .01f)
                {
                    Font previous = element.ScaledFont;
                    element.ScaledFont = new Font(element.OriginalFont.FontFamily, fontSize, element.OriginalFont.Style);
                    control.Font = element.ScaledFont;
                    if (previous != null) previous.Dispose();
                }
                control.Bounds = new Rectangle(LayoutX(bounds.X), LayoutX(bounds.Y), LayoutX(bounds.Width), LayoutX(bounds.Height));
                var choice = control as ImageChoice;
                if (choice != null) { choice.CompactLevel = compactLevel; choice.VisualScale = layoutScale; choice.Invalidate(); }
            }
            // Actions stay reachable even in the extreme-size scroll fallback.
            installButton.Left = ClientSize.Width - LayoutX(26 + 222);
            uninstallButton.Left = installButton.Left - LayoutX(16 + 112);
            statusLabel.Width = Math.Max(0, uninstallButton.Left - LayoutX(16) - statusLabel.Left);
            progressBar.Width = Math.Max(1, ClientSize.Width - LayoutX(52));
            contentViewport.AutoScrollPosition = Point.Empty;
        }
        finally
        {
            actionFooter.ResumeLayout(true); contentCanvas.ResumeLayout(true); ResumeLayout(true);
            arrangingLayout = false;
        }
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); ApplyAdaptiveLayout(); }
    protected override void OnLoad(EventArgs e) { base.OnLoad(e); FitToWorkingArea(Screen.FromControl(this).WorkingArea); }
    protected override void OnShown(EventArgs e) { base.OnShown(e); contentViewport.AutoScrollPosition = Point.Empty; }

    private void FitToWorkingArea(Rectangle area)
    {
        MinimumSize = Size.Empty;
        Size frame = new Size(Width - ClientSize.Width, Height - ClientSize.Height);
        Size available = new Size(Math.Max(1, area.Width - 16 - frame.Width), Math.Max(1, area.Height - 16 - frame.Height));
        int level = CompactLevelFor(available);
        float scale = Math.Max(.72f, Math.Min(1f, Math.Min(available.Width / 920f, available.Height / (float)LayoutHeight(level))));
        ClientSize = new Size(Math.Min(available.Width, (int)Math.Round(920 * scale)),
            Math.Min(available.Height, (int)Math.Round(LayoutHeight(level) * scale)));
        ApplyAdaptiveLayout();
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        MinimumSize = new Size(Math.Min(560, Width), Math.Min(360, Height));
    }

    internal static void FitWindowToWorkingArea(Form form, Rectangle area)
    {
        var available = Rectangle.Inflate(area, -8, -8);
        int width = form.Width;
        if (form.Height > available.Height) width += SystemInformation.VerticalScrollBarWidth;
        form.Size = new Size(Math.Min(width, available.Width), Math.Min(form.Height, available.Height));
        form.Location = new Point(available.Left + (available.Width - form.Width) / 2,
            available.Top + (available.Height - form.Height) / 2);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) foreach (var element in layoutElements) if (element.ScaledFont != null) element.ScaledFont.Dispose();
    }
}
