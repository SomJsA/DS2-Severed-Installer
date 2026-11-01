using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

internal static class ProductInfo
{
    public const string GroupTitle = "死亡空间贴吧汉化组移植&汉化";
    public const string Contact = "交流群339639634 && 死亡空间百度贴吧";
    public const string GroupUrl = "https://qm.qq.com/q/AsZY1d8WxG";
    public const string TiebaUrl = "https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page";
    public const string MarkerRepository = "https://github.com/Wemino/MarkerPatch";
    public const string DlcRepository = "https://github.com/Tubzbuster/DS2SeveredPC";
    public const string OwnRepository = "https://github.com/SomJsA/DS2-Severed-Installer";
    public const string CurrentCredits = "当前版本基石：Wemino/MarkerPatch、Tubzbuster/DS2SeveredPC\r\nsimgroup异版简单修复和整合：SomJs\r\n超级校对：lxyun_2（腹黑小喵）、Charlotte\r\n超级测试：待补充\r\n提供部分Token：天雷本雷";
    public const string LegacyCredits = "汉化总负责：lxyun_2（腹黑小喵）\r\n汉化技术负责：Erwin Qins\r\n翻译、校对、游戏测试：特洛克·洛克莱斯\r\n翻译、校对：attprtt、Charlotte、零之境、上白石泽雨\r\n校对：SomJs";
    public static readonly string StartupNotice = GroupTitle + "\r\n" + Contact +
        "\r\n\r\n使用提示\r\n本程序未签名，包含游戏插件，可能被安全软件提示风险。如检测到具体威胁，请先停止运行并核对来源。" +
        "\r\n\r\n项目引用\r\n" + MarkerRepository + "\r\n" + DlcRepository +
        "\r\n\r\n本项目源码（不含游戏数据）\r\n" + OwnRepository +
        "\r\n\r\n历史版本人员名单（原署名沿用）\r\n" + LegacyCredits;
    public static readonly string Details = GroupTitle + "\r\n" + Contact +
        "\r\n\r\n版本：R9" +
        "\r\n\r\n" + CurrentCredits +
        "\r\n\r\n历史版本人员名单\r\n" + LegacyCredits +
        "\r\n\r\n开源项目\r\n" + MarkerRepository + "\r\n" + DlcRepository +
        "\r\n\r\n本项目仓库\r\n" + OwnRepository;
    public static Image LoadImage(string name)
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (stream == null) throw new InvalidDataException("Missing UI image: " + name);
            using (Image image = Image.FromStream(stream)) return new Bitmap(image);
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
        return MessageBox.Show(StartupNotice, "使用须知", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
    }
}
