using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

internal static class PaintSkin
{
    public static readonly Color Background = Color.FromArgb(10, 17, 23);
    public static readonly Color Panel = Color.FromArgb(19, 30, 39);
    public static readonly Color Accent = Color.FromArgb(145, 221, 236);
    public static GraphicsPath Round(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); float d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90); p.CloseFigure(); return p;
    }
}
internal sealed class ImageChoice : CheckBox
{
    public Image Artwork { get; set; }
    public string Caption { get; set; }
    public bool ContainArtwork { get; set; }
    private float hover, targetHover, selectPulse, selection, targetSelection, press, targetPress;
    private Point lastClick;
    private readonly Timer animation = new Timer { Interval = 16 };
    public ImageChoice()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = false; Cursor = Cursors.Hand; BackColor = PaintSkin.Background; ForeColor = Color.White;
        AccessibleRole = AccessibleRole.CheckButton;
        animation.Tick += (sender, e) => {
            hover += (targetHover-hover)*0.20f;
            selection += (targetSelection-selection)*0.19f;
            press += (targetPress-press)*0.30f;
            selectPulse *= 0.89f; Invalidate();
            if (Math.Abs(targetHover-hover)<0.01f && Math.Abs(targetSelection-selection)<0.01f && Math.Abs(targetPress-press)<0.01f && selectPulse<0.01f)
            { hover=targetHover; selection=targetSelection; press=targetPress; animation.Stop(); }
        };
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); targetHover=1; animation.Start(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); targetHover=0; targetPress=0; animation.Start(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); targetSelection=Checked?1:0; selectPulse=1; animation.Start(); }
    protected override void OnMouseDown(MouseEventArgs e) { lastClick=e.Location; targetPress=1; animation.Start(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { targetPress=0; animation.Start(); base.OnMouseUp(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics; g.SmoothingMode=SmoothingMode.AntiAlias; g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        g.Clear(BackColor); float unit=Math.Max(1f,g.DpiX/96f), lift=5*unit*hover-2*unit*press;
        var box=new RectangleF(2*unit,7*unit-lift,Width-4*unit,Height-9*unit);
        using(var path=PaintSkin.Round(box,9*unit))
        {
            using(var brush=new SolidBrush(PaintSkin.Panel)) g.FillPath(brush,path);
            var save=g.Save(); g.SetClip(path);
            var art=new RectangleF(box.X+unit,box.Y+unit,box.Width-2*unit,box.Height-76*unit);
            var artState=g.Save(); g.SetClip(art,CombineMode.Intersect);
            if(Artwork!=null)
            {
                float zoom=ContainArtwork ? Math.Min(art.Width/Artwork.Width,art.Height/Artwork.Height) : Math.Max(art.Width/Artwork.Width,art.Height/Artwork.Height);
                zoom*=1+hover*0.03f-press*0.012f;
                var dest=new RectangleF(art.X+(art.Width-Artwork.Width*zoom)/2,art.Y+(art.Height-Artwork.Height*zoom)/2,Artwork.Width*zoom,Artwork.Height*zoom);
                using(var attrib=new ImageAttributes())
                {
                    float saturation=.2f+.8f*selection;
                    var matrix=new ColorMatrix(new float[][] {
                        new float[]{.213f+.787f*saturation,.213f-.213f*saturation,.213f-.213f*saturation,0,0},
                        new float[]{.715f-.715f*saturation,.715f+.285f*saturation,.715f-.715f*saturation,0,0},
                        new float[]{.072f-.072f*saturation,.072f-.072f*saturation,.072f+.928f*saturation,0,0},
                        new float[]{0,0,0,Enabled ? (.62f+.38f*selection) : .4f,0},new float[]{0,0,0,0,1}});
                    attrib.SetColorMatrix(matrix);
                    g.DrawImage(Artwork,Rectangle.Round(dest),0,0,Artwork.Width,Artwork.Height,GraphicsUnit.Pixel,attrib);
                }
            }
            g.Restore(artState);
            using(var fade=new LinearGradientBrush(new PointF(art.X,art.Bottom-52*unit),new PointF(art.X,art.Bottom+unit),Color.Transparent,PaintSkin.Panel))
                g.FillRectangle(fade,art.X,art.Bottom-52*unit,art.Width,54*unit);
            if(selectPulse>.01f)
            {
                float radius=(1-selectPulse)*Width*1.15f;
                using(var flash=new SolidBrush(Color.FromArgb((int)(32*selectPulse),PaintSkin.Accent)))
                    g.FillEllipse(flash,lastClick.X-radius,lastClick.Y-radius,radius*2,radius*2);
            }
            float labelY=box.Bottom-66*unit;
            using(var title=new Font("Microsoft YaHei UI",13F,FontStyle.Bold))
            using(var textBrush=new SolidBrush(Checked ? Color.FromArgb(240,246,249) : Color.FromArgb(146,164,174)))
                g.DrawString(Text,title,textBrush,new PointF(box.X+16*unit,labelY));
            using(var caption=new Font("Microsoft YaHei UI",8.5F))
            using(var textBrush=new SolidBrush(Color.FromArgb(137,161,174)))
                g.DrawString(Caption ?? "",caption,textBrush,new RectangleF(box.X+16*unit,labelY+32*unit,box.Width-28*unit,26*unit));
            g.Restore(save);
            Color border=Color.FromArgb((int)(48+97*selection+10*hover),(int)(66+155*selection+10*hover),(int)(77+159*selection+10*hover));
            using(var pen=new Pen(border,(Focused ? 2f : Checked ? 1.3f : 1f)*unit)) g.DrawPath(pen,path);
            float checkSize=21*unit*(.9f+.1f*selection+.08f*selectPulse);
            var check=new RectangleF(box.Right-25.5f*unit-checkSize/2,box.Y+25.5f*unit-checkSize/2,checkSize,checkSize);
            using(var fill=new SolidBrush(Checked ? PaintSkin.Accent : Color.FromArgb(160,9,17,22))) g.FillEllipse(fill,check);
            using(var pen=new Pen(Checked ? PaintSkin.Background : Color.FromArgb(139,162,174),1.5f*unit))
            {
                if(Checked) g.DrawLines(pen,new[]{new PointF(check.X+5*unit,check.Y+10*unit),new PointF(check.X+9*unit,check.Y+14*unit),new PointF(check.X+16*unit,check.Y+7*unit)});
                else g.DrawEllipse(pen,check);
            }
        }
    }
    protected override void Dispose(bool disposing) { if(disposing){animation.Dispose(); if(Artwork!=null)Artwork.Dispose();} base.Dispose(disposing); }
}
internal sealed class SkinButton : Button
{
    public bool Primary { get; set; }
    private bool hover;
    private bool flowGlow;
    private float flowPhase;
    private readonly Timer glowTimer=new Timer{Interval=30};
    public bool FlowGlow
    {
        get{return flowGlow;}
        set{if(flowGlow==value)return;flowGlow=value;UpdateGlowTimer();Invalidate();}
    }
    private void UpdateGlowTimer(){if(flowGlow&&Visible&&Enabled)glowTimer.Start();else glowTimer.Stop();}
    public SkinButton()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;Cursor=Cursors.Hand;
        glowTimer.Tick+=(sender,e)=>{flowPhase=(flowPhase+.012f)%1f;Invalidate();};
    }
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);UpdateGlowTimer();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);UpdateGlowTimer();Invalidate();}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
        using(var path=PaintSkin.Round(new RectangleF(0.5f,0.5f,Width-1,Height-1),6))
        {
            Color fill=Primary ? (hover ? Color.FromArgb(185,240,248) : PaintSkin.Accent) : (hover ? Color.FromArgb(35,53,65) : PaintSkin.Panel);
            if(!Enabled)fill=Color.FromArgb(42,55,64);
            using(var brush=new SolidBrush(fill))g.FillPath(brush,path);
            using(var pen=new Pen(Primary ? fill : Color.FromArgb(52,71,83)))g.DrawPath(pen,path);
            if(flowGlow&&Enabled)
            {
                using(var pen=new Pen(Color.FromArgb(100,PaintSkin.Accent),1.4f))g.DrawPath(pen,path);
                float span=Math.Max(96f,Width*.38f),x=flowPhase*(Width+span)-span;
                var beam=new RectangleF(x,0,span,Height);
                var saved=g.Save();g.SetClip(path);
                using(var light=new LinearGradientBrush(beam,Color.Transparent,Color.Transparent,LinearGradientMode.Horizontal))
                {
                    light.InterpolationColors=new ColorBlend{Positions=new[]{0f,.5f,1f},Colors=new[]{Color.FromArgb(0,PaintSkin.Accent),Color.FromArgb(36,PaintSkin.Accent),Color.FromArgb(0,PaintSkin.Accent)}};
                    g.FillRectangle(light,beam);
                    g.SetClip(beam,CombineMode.Intersect);
                    light.InterpolationColors=new ColorBlend{Positions=new[]{0f,.5f,1f},Colors=new[]{Color.FromArgb(0,PaintSkin.Accent),Color.FromArgb(230,PaintSkin.Accent),Color.FromArgb(0,PaintSkin.Accent)}};
                    using(var pen=new Pen(light,2.6f))g.DrawPath(pen,path);
                }
                g.Restore(saved);
            }
        }
        TextRenderer.DrawText(g,Text,Font,ClientRectangle,Primary ? PaintSkin.Background : ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,Width-8,Height-8));
    }
    protected override void Dispose(bool disposing){if(disposing)glowTimer.Dispose();base.Dispose(disposing);}
}
internal sealed class SlimProgress : ProgressBar
{
    private float position;
    private readonly Timer timer=new Timer{Interval=30};
    public SlimProgress()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        timer.Tick+=(s,e)=>{if(Style==ProgressBarStyle.Marquee){position=(position+0.025f)%1;Invalidate();}};timer.Start();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.FromArgb(30,46,58));
        float width=Style==ProgressBarStyle.Marquee ? Width*.22f : Width*(float)Value/Math.Max(1,Maximum);
        float x=Style==ProgressBarStyle.Marquee ? position*(Width+width)-width : 0;
        using(var brush=new SolidBrush(PaintSkin.Accent))e.Graphics.FillRectangle(brush,x,0,width,Height);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
