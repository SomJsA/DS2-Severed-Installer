using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class ProductInfo
{
    public const string ReleaseVersion = "2026.11.3.9";
    public static string GroupTitle { get { return L.T("死亡空间贴吧汉化组移植&汉化"); } }
    public static string Contact { get { return L.T("交流群339639634 && 死亡空间贴吧"); } }
    public const string GroupUrl = "https://qm.qq.com/q/AsZY1d8WxG";
    public const string TiebaUrl = "https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page";
    public const string MarkerRepository = "https://github.com/Wemino/MarkerPatch";
    public const string DlcRepository = "https://github.com/Tubzbuster/DS2SeveredPC";
    public const string OwnRepository = "https://github.com/SomJsA/DS2-Severed-Installer";
    public const string OwnReleasesPage = OwnRepository + "/releases";
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
    public static string Details { get { return Heading+"\r\n\r\n"+Credits+"\r\n\r\n"+Links+"\r\n\r\n"+SaveCompatibility; } }
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
    public static string VersionHistory
    {
        get { return "2026.11.1 — " + L.T("Playable：Severed 已可游玩。") + "\r\n" +
            "2026.11.2 — " + L.T("新增能量节点购买修正与提示优化。") + "\r\n" +
            "2026.11.2.1 — " + L.T("完善更新提示与安装流程。") + "\r\n" +
            "2026.11.3 — " + L.T("新增安装前 MarkerPatch 功能确认与推荐勾选。") + "\r\n" +
            "2026.11.3.1 — " + L.T("功能提示移至主页面，按需打开详细设置。") + "\r\n" +
            "2026.11.3.2 — " + L.T("测试候选：DLC 菜单名称与说明汉化，游戏内显示待验证。") + "\r\n" +
            "2026.11.3.3 — " + L.T("测试候选：章节选择中的本体名称改为“死亡空间2本体”。") + "\r\n" +
            "2026.11.3.4 — " + L.T("存档名称测试候选：只在确认 DLC 条目时改显示；图标尚未替换。") + "\r\n" +
            "2026.11.3.5 — " + L.T("存档图标测试候选：新增两章原始图片与 DLC 存档兼容性提示，实机待验证。") + "\r\n" +
            "2026.11.3.7 — " + L.T("存档显示候选：修正读档列表的 DLC 章节名称与两章图片。") + "\r\n" +
            "2026.11.3.8 — " + L.T("改进网页打开兼容性，打不开时可复制发布页地址。") + "\r\n" +
            "2026.11.3.9 — " + L.T("能量节点与 DLC 存档修复改为独立 ASI，原有选项与汉化方式不变。") + "\r\n" +
            L.T("支持更改安装、保存设置、根目录管理器与更新检查。"); }
    }
    public static string SaveCompatibility { get { return L.T("DLC 存档需要 Severed DLC 及运行组件。卸载、停用或回到原版后，请勿读取这些存档；原版读档失败的具体表现尚未验证。请备份存档，重新安装并启用 DLC 后继续。名称和图标修复只改显示，不转换存档。"); } }
    public static string InstallationComplete(bool dlc)
    {
        return L.T("安装完成。") + (dlc ? "\r\n\r\n" + L.T("进入游戏后，在“新游戏”中选择 Severed，即可进入 DLC。") + "\r\n\r\n" + SaveCompatibility : "") + "\r\n\r\n" + L.T("点击确定后退出安装器。");
    }
    public static DialogResult ShowStartupNotice()
    {
        DialogResult result = MessageBox.Show(StartupNotice,L.T("使用须知"),MessageBoxButtons.OKCancel,MessageBoxIcon.Information);
        if (result == DialogResult.OK) MessageBox.Show(VersionHistory,L.T("累计更新"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        return result;
    }
}
