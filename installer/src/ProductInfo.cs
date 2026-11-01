using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class ProductInfo
{
    public const string ReleaseVersion = "2026.11.1";
    public static string GroupTitle { get { return L.T("死亡空间贴吧汉化组移植&汉化"); } }
    public static string Contact { get { return L.T("交流群339639634 && 死亡空间贴吧"); } }
    public const string GroupUrl = "https://qm.qq.com/q/AsZY1d8WxG";
    public const string TiebaUrl = "https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page";
    public const string MarkerRepository = "https://github.com/Wemino/MarkerPatch";
    public const string DlcRepository = "https://github.com/Tubzbuster/DS2SeveredPC";
    public const string OwnRepository = "https://github.com/SomJsA/DS2-Severed-Installer";
    public static string CurrentCredits
    {
        get { return L.T("当前版本基石：")+"Wemino/MarkerPatch、Tubzbuster/DS2SeveredPC\r\n"+
            L.T("simgroup异版简单修复和整合：")+"SomJs\r\n"+
            L.T("超级校对：")+"lxyun_2（腹黑小喵）、MisakaCharlotte（是的呀啊）\r\n"+
            L.T("提供部分Token：")+"天雷本雷"; }
    }
    public static string LegacyCredits
    {
        get { return L.T("汉化总负责：")+"lxyun_2（腹黑小喵）\r\n"+
            L.T("汉化技术负责：")+"Erwin Qins\r\n"+
            L.T("翻译、校对、游戏测试：")+"特洛克·洛克莱斯\r\n"+
            L.T("翻译、校对：")+"attprtt、MisakaCharlotte（是的呀啊）、零之境、上白石泽雨\r\n"+
            L.T("校对：")+"SomJs"; }
    }
    private static string Credits { get { return CurrentCredits+"\r\n\r\n"+L.T("历史版本人员名单")+"\r\n"+LegacyCredits; } }
    private static string Heading { get { return GroupTitle+"\r\n"+Contact+"\r\n"+L.T("版本：")+ReleaseVersion; } }
    private static string Links { get { return L.T("项目引用")+"\r\n"+MarkerRepository+"\r\n"+DlcRepository+"\r\n\r\n"+L.T("本项目源码（不含游戏数据）")+"\r\n"+OwnRepository; } }
    public static string StartupNotice
    {
        get { return Heading+"\r\n\r\n"+L.T("使用提示")+"\r\n"+
            L.T("本程序未签名，包含游戏插件，可能被安全软件提示风险。如检测到具体威胁，请先停止运行并核对来源。")+
            "\r\n\r\n"+Credits+"\r\n\r\n"+Links; }
    }
    public static string Details { get { return Heading+"\r\n\r\n"+Credits+"\r\n\r\n"+Links; } }
    public static Image LoadImage(string name)
    {
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if(stream==null)throw new InvalidDataException("Missing UI image: "+name);
            using(Image image=Image.FromStream(stream))return new Bitmap(image);
        }
    }
    public static Icon LoadIcon()
    {
        using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("DS2_APP_ICON"))
        {
            if(stream==null)throw new InvalidDataException("Missing legacy application icon.");
            using(var icon=new Icon(stream))return (Icon)icon.Clone();
        }
    }
    public static DialogResult ShowStartupNotice()
    {
        return MessageBox.Show(StartupNotice,L.T("使用须知"),MessageBoxButtons.OKCancel,MessageBoxIcon.Information);
    }
}
