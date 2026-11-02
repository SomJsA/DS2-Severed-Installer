using System.Globalization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Dead Space 2 Severed 一站式补丁")]
[assembly: AssemblyDescription("DLC data loader, optional workbook-based Chinese patch, and MarkerPatch installer")]
[assembly: AssemblyProduct("DS2SeveredOneClick")]
[assembly: AssemblyVersion("2026.11.2.1")]
[assembly: AssemblyFileVersion("2026.11.2.1")]
[assembly: AssemblyInformationalVersion("2026.11.2.1")]

internal sealed class InstallOptions
{
    public bool Chinese = true;
    public bool Dlc = true;
    public bool MarkerPatch = true;
    public bool IgnoreCompatibilityErrors = false;
    public bool NodeFix = true;
    public bool DeployManager = false;
    public decimal SubtitleScale = 1.0m;
    public string DlcArchivePath;
    public Dictionary<string,string> MarkerParameters = MarkerCatalog.DefaultParameters();
    public Dictionary<string, bool> MarkerValues = InstallerEngine.RecommendedMarkerValues();
}

internal sealed class MarkerOption
{
    public string Key { get; set; }
    public string Display { get; set; }
    public bool Recommended { get; set; }
    public bool RequiredForDlc, RequiredForNode;
    public string StatusText { get { return RequiredForNode ? L.T("节点修正所需，自动启用") : RequiredForDlc ? L.T("随 DLC 自动启用") : Recommended ? L.T("默认启用") : L.T("默认不启用"); } }

    public override string ToString()
    {
        return Display + "  [" + StatusText + "]";
    }
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        L.Initialize(ArgValue(args,"--language"));
        string launchProblem=LaunchGuard.BlockReason(Assembly.GetExecutingAssembly().Location);
        if(launchProblem!=null)
        {
            string log=ArgValue(args,"--log");
            if(log!=null)try{File.AppendAllText(log,launchProblem+Environment.NewLine,Encoding.UTF8);}catch{}
            if(args.Length==0 || (args[0]!="--install" && args[0]!="--uninstall"))
                MessageBox.Show(launchProblem,L.T("请完整解压后运行"),MessageBoxButtons.OK,MessageBoxIcon.Warning);
            return 2;
        }
        if (args.Length > 0 && (args[0] == "--install" || args[0] == "--change"))
        {
            return RunSilentInstall(args);
        }
        if (args.Length > 0 && args[0] == "--uninstall")
        {
            return RunSilentUninstall(args);
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        bool preview = args.Contains("--preview");
        if (!preview && ProductInfo.ShowStartupNotice() != DialogResult.OK) return 0;
        using (var form = new InstallerForm { PreviewOnly = preview })
        {
            string selectedGame = ArgValue(args, "--game-dir");
            if (!string.IsNullOrWhiteSpace(selectedGame)) form.SelectGameDirectory(selectedGame);
            Application.Run(form);
        }
        return 0;
    }

    private static string ArgValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }

    private static int RunSilentInstall(string[] args)
    {
        string game = ArgValue(args, "--install") ?? ArgValue(args, "--change");
        string logPath = ArgValue(args, "--log");
        var options = new InstallOptions
        {
            Chinese = !args.Contains("--no-chinese"),
            Dlc = !args.Contains("--no-dlc"),
            MarkerPatch = !args.Contains("--no-marker"),
            NodeFix = !args.Contains("--no-node-fix"),
            DeployManager = args.Contains("--keep-manager"),
            IgnoreCompatibilityErrors = args.Contains("--ignore-errors")
        };
        try
        {
            if (File.Exists(Path.Combine(game, "DS2SeveredOneClick.install.txt")))
            {
                var installed = InstallerEngine.InspectInstallation(game).Options;
                options.MarkerValues = installed.MarkerValues;
                options.MarkerParameters = installed.MarkerParameters;
                options.SubtitleScale = installed.SubtitleScale;
                if (!args.Contains("--node-fix") && !args.Contains("--no-node-fix") && File.ReadAllLines(Path.Combine(game, "DS2SeveredOneClick.install.txt")).Any(line => line.StartsWith("node_fix_preference="))) options.NodeFix = installed.NodeFix;
            }
            options.DlcArchivePath = ArgValue(args, "--dlc-package");
            string scaleValue = ArgValue(args, "--subtitle-scale");
            if (scaleValue != null)
                options.SubtitleScale = decimal.Parse(scaleValue, NumberStyles.Number, CultureInfo.InvariantCulture);
            InstallerEngine.ApplySelection(game, options, message =>
            {
                if (logPath != null) File.AppendAllText(logPath, message + Environment.NewLine, Encoding.UTF8);
            });
            return 0;
        }
        catch (Exception ex)
        {
            if (logPath != null) File.AppendAllText(logPath, "ERROR: " + InstallerEngine.MaintenanceFailure(ex) + Environment.NewLine, Encoding.UTF8);
            return 1;
        }
    }

    private static int RunSilentUninstall(string[] args)
    {
        string game = ArgValue(args, "--uninstall");
        string logPath = ArgValue(args, "--log");
        try
        {
            InstallerEngine.Uninstall(game, message =>
            {
                if (logPath != null) File.AppendAllText(logPath, message + Environment.NewLine, Encoding.UTF8);
            });
            return 0;
        }
        catch (Exception ex)
        {
            if (logPath != null) File.AppendAllText(logPath, "ERROR: " + ex + Environment.NewLine + UninstallSupport.FailureMessage(ex) + Environment.NewLine, Encoding.UTF8);
            return 1;
        }
    }
}

internal sealed partial class InstallerForm : Form
{
    private readonly TextBox gameBox = new TextBox();
    private readonly TextBox logBox = new TextBox();
    private readonly ImageChoice chineseBox = new ImageChoice();
    private readonly ImageChoice dlcBox = new ImageChoice();
    private readonly ImageChoice markerBox = new ImageChoice();
    private readonly CheckBox ignoreErrorsBox = new CheckBox();
    private readonly CheckedListBox markerOptionsList = new CheckedListBox();
    private readonly SlimProgress progressBar = new SlimProgress();
    private readonly SkinButton installButton = new SkinButton { Primary = true };
    private readonly SkinButton uninstallButton = new SkinButton();
    private readonly SkinButton markerSettingsButton = new SkinButton();
    private readonly SkinButton chineseLanguageButton = new SkinButton();
    private readonly SkinButton englishLanguageButton = new SkinButton();
    private Label selectionHint;
    private Label subtitleUnit;
    private readonly Button closeButton = new Button();
    private readonly Label statusLabel = new Label();

    private readonly NumericUpDown subtitleScaleBox = new NumericUpDown();
    private readonly TextBox detailsBox = new TextBox();
    private readonly TextBox dlcPathBox = new TextBox();
    private readonly List<Control> pathEditors = new List<Control>();
    public bool PreviewOnly { get; set; }
    private bool busy;
    private readonly Dictionary<string,string> parameterValues = MarkerCatalog.DefaultParameters();

    public InstallerForm()
    {
        Text = ProductInfo.GroupTitle;
        Icon = ProductInfo.LoadIcon();
        ClientSize = new Size(920, 716); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Microsoft YaHei UI",9.5F);
        BackColor = PaintSkin.Background; ForeColor = Color.FromArgb(230,239,243); AutoScaleMode = AutoScaleMode.Dpi;
        AddLabel(this,ProductInfo.GroupTitle,18F,FontStyle.Bold,24,25,Color.FromArgb(239,247,250));
        AddLabel(this,ProductInfo.ReleaseVersion,10F,FontStyle.Regular,798,28,PaintSkin.Accent);
        var groupLink = new LinkLabel { Text=L.T("交流群 339639634"), AutoSize=false, TextAlign=ContentAlignment.MiddleRight, Location=new Point(472,15), Size=new Size(302,33), Font=new Font("Microsoft YaHei UI",14F,FontStyle.Bold), LinkColor=Color.FromArgb(212,238,247), ActiveLinkColor=Color.White, VisitedLinkColor=Color.FromArgb(212,238,247), LinkBehavior=LinkBehavior.HoverUnderline };
        groupLink.LinkClicked += (sender,e)=>Process.Start(new ProcessStartInfo(ProductInfo.GroupUrl){UseShellExecute=true});Controls.Add(groupLink);
        var tiebaLink = new LinkLabel { Text=L.T("死亡空间贴吧"), AutoSize=false, TextAlign=ContentAlignment.MiddleRight, Location=new Point(614,49), Size=new Size(160,29), Font=new Font("Microsoft YaHei UI",12F,FontStyle.Bold), LinkColor=Color.FromArgb(165,209,229), ActiveLinkColor=Color.FromArgb(212,238,247), LinkBehavior=LinkBehavior.HoverUnderline };
        tiebaLink.LinkClicked += (sender,e)=>Process.Start(new ProcessStartInfo(ProductInfo.TiebaUrl){UseShellExecute=true});Controls.Add(tiebaLink);
        var releasesButton = MakeButton(L.T("GitHub 发布页"),472,50,128,28,false); releasesButton.Name="githubReleasesButton";
        releasesButton.Click += (sender,e)=>OpenPage(ProductInfo.OwnReleasesPage); Controls.Add(releasesButton);
        AddLabel(this,L.T("选择安装内容"),10F,FontStyle.Regular,26,96,Color.FromArgb(177,199,210));
        selectionHint=AddLabel(this,L.T("可多选"),8.5F,FontStyle.Regular,844,98,Color.FromArgb(112,137,151));
        var languageLabel=AddLabel(this,"Language",8.5F,FontStyle.Regular,510,98,Color.FromArgb(157,185,201));L.KeepLiteral(languageLabel);
        ConfigureLanguageButton(chineseLanguageButton,"中文",586,66,"zh-CN");
        ConfigureLanguageButton(englishLanguageButton,"English",660,85,"en");
        ConfigureChoice(markerBox,"MarkerPatch",L.T("功能改善与个性化"),"DS2_MARKER_LOGO",616,true);
        ConfigureChoice(dlcBox,"Severed DLC",L.T("英文 / 启动器所选语言"),"DS2_DLC_POSTER",320,true);
        ConfigureChoice(chineseBox,L.T("简体中文汉化"),L.T("基础游戏 + Severed DLC"),"DS2_CHINESE_LOGO",24,true);
        AddLabel(this,L.T("游戏目录"),8.5F,FontStyle.Regular,26,413,Color.FromArgb(132,157,171));
        gameBox.Location = new Point(26,436); gameBox.Size = new Size(658,28); gameBox.BorderStyle = BorderStyle.FixedSingle;
        gameBox.BackColor = Color.FromArgb(20,32,42); gameBox.ForeColor = Color.FromArgb(211,227,235);
        gameBox.Text = InstallerEngine.FindGameDir() ?? ""; Controls.Add(gameBox);
        var detect = MakeButton(L.T("自动识别"),700,432,94,34,false); detect.Click += (sender,e)=>AutoDetectGame(); Controls.Add(detect);pathEditors.Add(detect);
        var browse = MakeButton(L.T("浏览"),806,432,88,34,false); browse.Click += (sender,e)=>BrowseGame(); Controls.Add(browse);pathEditors.Add(browse);
        AddLabel(this,L.T("Severed.marker 路径"),8.5F,FontStyle.Regular,26,477,Color.FromArgb(132,157,171));
        dlcPathBox.Location=new Point(26,500);dlcPathBox.Size=new Size(744,28);dlcPathBox.BackColor=Color.FromArgb(20,32,42);dlcPathBox.ForeColor=Color.FromArgb(211,227,235);dlcPathBox.BorderStyle=BorderStyle.FixedSingle;
        dlcPathBox.Text=InstallerEngine.FindDlcArchive();Controls.Add(dlcPathBox);
        var packageBrowse=MakeButton(L.T("浏览"),786,496,108,34,false);packageBrowse.Click+=(sender,e)=>BrowseDlcArchive();Controls.Add(packageBrowse);pathEditors.Add(packageBrowse);
        AddLabel(this,L.T("字幕倍率"),9F,FontStyle.Regular,26,552,Color.FromArgb(165,190,202));
        subtitleScaleBox.Location=new Point(108,547); subtitleScaleBox.Size=new Size(77,28);
        subtitleScaleBox.DecimalPlaces=1; subtitleScaleBox.Minimum=.5m; subtitleScaleBox.Maximum=3m; subtitleScaleBox.Increment=.1m;
        subtitleScaleBox.Value=1.0m; subtitleScaleBox.BackColor=Color.FromArgb(20,32,42); subtitleScaleBox.ForeColor=Color.White; Controls.Add(subtitleScaleBox);
        subtitleUnit=AddLabel(this,L.T("倍"),10F,FontStyle.Regular,194,551,Color.FromArgb(132,157,171));
        markerSettingsButton.Text=L.T("其他由 MarkerPatch 提供的功能改善");markerSettingsButton.Location=new Point(246,541);markerSettingsButton.Size=new Size(330,36);markerSettingsButton.BackColor=BackColor;markerSettingsButton.ForeColor=Color.FromArgb(192,211,221);markerSettingsButton.Font=Font;markerSettingsButton.Click+=(sender,e)=>ShowMarkerSettings();Controls.Add(markerSettingsButton);
        var details = MakeButton(L.T("详情"),718,541,80,36,false); details.Click+=(sender,e)=>ShowDetails(); Controls.Add(details);
        var logs = MakeButton(L.T("日志"),814,541,80,36,false); logs.Click+=(sender,e)=>ShowLog(); Controls.Add(logs);
        progressBar.Location=new Point(26,607); progressBar.Size=new Size(868,4); progressBar.Style=ProgressBarStyle.Continuous; Controls.Add(progressBar);
        statusLabel.Location=new Point(26,630); statusLabel.Size=new Size(505,48); statusLabel.ForeColor=Color.FromArgb(147,178,194); Controls.Add(statusLabel);
        uninstallButton.Text=L.T("卸载"); uninstallButton.Location=new Point(544,632); uninstallButton.Size=new Size(112,44); uninstallButton.BackColor=BackColor; uninstallButton.ForeColor=Color.FromArgb(192,211,221); uninstallButton.Click+=(sender,e)=>RunUninstall(); Controls.Add(uninstallButton);
        installButton.Text=L.T("安装所选组件"); installButton.Location=new Point(672,632); installButton.Size=new Size(222,44); installButton.BackColor=BackColor; installButton.Font=new Font(Font.FontFamily,11F,FontStyle.Bold); installButton.Click+=(sender,e)=>RunInstall(); Controls.Add(installButton);
        logBox.Multiline=true; logBox.ReadOnly=true; logBox.ScrollBars=ScrollBars.Vertical; logBox.BackColor=PaintSkin.Background; logBox.ForeColor=Color.FromArgb(201,223,233);
        ConfigureCheck(ignoreErrorsBox,L.T("允许跳过基础版本识别（建议使用原版游戏）"),new Point(16,570),false);
        markerOptionsList.CheckOnClick=true; markerOptionsList.IntegralHeight=false; markerOptionsList.BackColor=PaintSkin.Panel; markerOptionsList.ForeColor=Color.White; markerOptionsList.HorizontalScrollbar=true;
        foreach(var option in InstallerEngine.GetMarkerPatchOptions()){int i=markerOptionsList.Items.Add(option);markerOptionsList.SetItemChecked(i,option.Recommended);}
        markerBox.CheckedChanged+=(sender,e)=>UpdateSelectionSummary(); dlcBox.CheckedChanged+=(sender,e)=>OnDlcSelectionChanged(); chineseBox.CheckedChanged+=(sender,e)=>UpdateSelectionSummary();
        InitializeMaintenanceUi(); ApplyLanguage(); FormClosing+=(sender,e)=>{if(busy)e.Cancel=true;};
    }
    private static Label AddLabel(Control parent,string text,float size,FontStyle style,int x,int y,Color color)
    {
        var label=new Label{Text=text,UseMnemonic=false,AutoSize=true,Location=new Point(x,y),ForeColor=color,BackColor=Color.Transparent,Font=new Font("Microsoft YaHei UI",size,style)};
        parent.Controls.Add(label);return label;
    }
    private void ConfigureLanguageButton(SkinButton button,string text,int x,int width,string language)
    {
        button.Text=text;button.Location=new Point(x,91);button.Size=new Size(width,28);button.BackColor=BackColor;button.ForeColor=Color.FromArgb(210,230,238);button.Font=Font;
        L.KeepLiteral(button);button.Click+=(sender,e)=>{L.SetLanguage(language);ApplyLanguage();};Controls.Add(button);pathEditors.Add(button);
    }
    private void ApplyLanguage()
    {
        var selected=new Dictionary<string,bool>();
        for(int i=0;i<markerOptionsList.Items.Count;i++){var option=(MarkerOption)markerOptionsList.Items[i];selected[option.Key]=markerOptionsList.GetItemChecked(i);}
        int selectedIndex=markerOptionsList.SelectedIndex;
        L.ApplyTree(this);Text=ProductInfo.GroupTitle;
        markerOptionsList.Items.Clear();
        foreach(var option in InstallerEngine.GetMarkerPatchOptions())
        {
            bool check;int index=markerOptionsList.Items.Add(option);markerOptionsList.SetItemChecked(index,selected.TryGetValue(option.Key,out check)?check:option.Recommended);
        }
        if(selectedIndex>=0&&selectedIndex<markerOptionsList.Items.Count)markerOptionsList.SelectedIndex=selectedIndex;
        chineseLanguageButton.Primary=!L.IsEnglish;englishLanguageButton.Primary=L.IsEnglish;
        chineseLanguageButton.Invalidate();englishLanguageButton.Invalidate();
        chineseBox.Caption=L.T("基础游戏 + Severed DLC");markerBox.Caption=L.T("功能改善与个性化");
        foreach(var card in new[]{chineseBox,dlcBox,markerBox}){card.AccessibleName=card.Text;card.Invalidate();}
        selectionHint.Left=L.IsEnglish?770:844;
        subtitleScaleBox.Left=L.IsEnglish?130:108;subtitleUnit.Left=L.IsEnglish?217:194;
        UpdateSelectionSummary();
        ApplyMaintenanceLanguage();
    }
    private SkinButton MakeButton(string text,int x,int y,int width,int height,bool primary)
    {
        return new SkinButton{Text=text,Primary=primary,Location=new Point(x,y),Size=new Size(width,height),BackColor=BackColor,ForeColor=Color.FromArgb(192,211,221),Font=Font};
    }
    private void ConfigureChoice(ImageChoice choice,string title,string caption,string image,int x,bool contain)
    {
        choice.Text=title;choice.Caption=caption;choice.Artwork=ProductInfo.LoadImage(image);choice.ContainArtwork=contain;
        choice.Location=new Point(x,120);choice.Size=new Size(280,278);choice.Checked=true;choice.AccessibleName=title;Controls.Add(choice);
    }
    private void UpdateSelectionSummary()
    {
        markerSettingsButton.FlowGlow=markerBox.Checked;
        dlcBox.Caption=chineseBox.Checked?L.T("随汉化补丁变为简体中文"):L.T("英文 / 启动器所选语言");dlcBox.Invalidate();
        if(busy)return;
        int count=(markerBox.Checked?1:0)+(dlcBox.Checked?1:0)+(chineseBox.Checked?1:0);
        statusLabel.Text=count==0?L.T("请选择需要安装的内容"):L.T("已选择 ")+count+L.T(" 个组件");

        installButton.Enabled=count>0 || installedSelection != null && installedSelection.HasRecord;
        UpdateMaintenanceSummary();
    }
    private Form MakeDialog(string title,Size size)
    {
        return new Form{Text=title,Icon=Icon,ClientSize=size,StartPosition=FormStartPosition.CenterParent,BackColor=PaintSkin.Background,ForeColor=Color.White,Font=Font,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
    }
    private void AutoDetectGame()
    {
        var games=InstallerEngine.FindGameDirs();
        if(games.Count==0){MessageBox.Show(this,L.T("没有识别到游戏，请手动选择目录。"),L.T("游戏目录"));return;}
        string selected=null;
        if(games.Count==1)selected=games[0].Path;
        else using(var dialog=MakeDialog(L.T("选择游戏版本"),new Size(780,250)))
        {
            AddLabel(dialog,L.T("检测到多个游戏目录，请选择本次安装的版本。"),11F,FontStyle.Bold,18,16,Color.White);
            var choices=new ListBox{Location=new Point(18,54),Size=new Size(744,126),BackColor=PaintSkin.Panel,ForeColor=Color.White,HorizontalScrollbar=true};
            foreach(var game in games)choices.Items.Add(game);choices.SelectedIndex=0;dialog.Controls.Add(choices);
            var use=MakeButton(L.T("使用所选目录"),598,198,164,34,true);use.DialogResult=DialogResult.OK;dialog.Controls.Add(use);dialog.AcceptButton=use;
            L.ApplyTree(dialog);if(dialog.ShowDialog(this)==DialogResult.OK)selected=((GameLocation)choices.SelectedItem).Path;
        }
        if(selected!=null){gameBox.Text=selected;RefreshNearbyPackage();}
    }
    private void RefreshNearbyPackage()
    {
        if(string.IsNullOrWhiteSpace(dlcPathBox.Text)||!System.IO.File.Exists(dlcPathBox.Text))
            dlcPathBox.Text=GameLocations.FindPackage(AppDomain.CurrentDomain.BaseDirectory,gameBox.Text);
        RefreshInstalledState();
    }
    private void ShowLog()
    {
        using(var dialog=MakeDialog(L.T("安装日志"),new Size(800,480)))
        {
            logBox.Dock=DockStyle.Fill;dialog.Controls.Add(logBox);
            try{dialog.ShowDialog(this);}finally{dialog.Controls.Remove(logBox);logBox.Dock=DockStyle.None;}
        }
    }
    private void ShowDetails()
    {
        using(var dialog=MakeDialog(L.T("详情"),new Size(680,650)))
        {
            detailsBox.Text=ProductInfo.Details;detailsBox.Multiline=true;detailsBox.ReadOnly=true;detailsBox.ScrollBars=ScrollBars.Vertical;detailsBox.TabStop=false;
            detailsBox.Location=new Point(18,18);detailsBox.Size=new Size(644,410);detailsBox.BackColor=PaintSkin.Panel;detailsBox.ForeColor=Color.White;dialog.Controls.Add(detailsBox);
            string[] links={ProductInfo.GroupUrl,ProductInfo.TiebaUrl,ProductInfo.MarkerRepository,ProductInfo.DlcRepository,ProductInfo.OwnRepository};
            string[] labels={L.T("交流群 339639634"),L.T("死亡空间贴吧"),"Wemino/MarkerPatch","Tubzbuster/DS2SeveredPC","SomJsA/DS2-Severed-Installer"};
            for(int i=0;i<links.Length;i++){string url=links[i];var link=new LinkLabel{Text=labels[i],AutoSize=true,LinkColor=PaintSkin.Accent,Location=new Point(20,448+i*32)};link.LinkClicked+=(sender,e)=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true});dialog.Controls.Add(link);}
            L.ApplyTree(dialog);try{dialog.ShowDialog(this);}finally{dialog.Controls.Remove(detailsBox);}
        }
    }
    private void ShowMarkerSettings()
    {
        using(var dialog=MakeDialog(L.T("其他由 MarkerPatch 提供的功能改善"),new Size(820,650)))
        {
            var tabs=new TabControl{Location=new Point(14,14),Size=new Size(792,573)};
            var switches=new TabPage(L.T("功能开关")){BackColor=PaintSkin.Background,ForeColor=Color.White};var parameters=new TabPage(L.T("数值参数")){BackColor=PaintSkin.Background,ForeColor=Color.White,AutoScroll=true};tabs.TabPages.Add(switches);tabs.TabPages.Add(parameters);dialog.Controls.Add(tabs);
            AddLabel(switches,(markerBox.Checked ? L.T("预设启用 12 项；DLC 所需选项按当前选择自动启用。") : L.T("未勾选 MarkerPatch：只启用 DLC 依赖，其他功能需在主页面勾选。")),9F,FontStyle.Regular,14,14,Color.FromArgb(170,196,210));
            AddLabel(switches,L.T("尚未测试全部功能组合及与其他模组的兼容性。"),9F,FontStyle.Regular,14,37,Color.FromArgb(219,193,139));
            markerOptionsList.Location=new Point(14,66);markerOptionsList.Size=new Size(432,456);switches.Controls.Add(markerOptionsList);
            var help=new TextBox{Location=new Point(460,66),Size=new Size(302,456),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=PaintSkin.Panel,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};switches.Controls.Add(help);
            EventHandler updateHelp=(sender,e)=>{var option=markerOptionsList.SelectedItem as MarkerOption;if(option==null)return;var info=MarkerCatalog.Find(option.Key);help.Text=L.T(info.Title)+"\r\n\r\n"+L.T(info.Description)+"\r\n\r\n"+option.StatusText+L.T("\r\n\r\n配置项：")+option.Key;};
            markerOptionsList.SelectedIndexChanged+=updateHelp;if(markerOptionsList.Items.Count>0)markerOptionsList.SelectedIndex=0;updateHelp(null,EventArgs.Empty);
            AddLabel(parameters,L.T("尚未测试全部功能组合及与其他模组的兼容性。"),9F,FontStyle.Regular,16,14,Color.FromArgb(219,193,139));
            var edited=new Dictionary<string,Control>();int y=48;
            foreach(var param in MarkerCatalog.Parameters)
            {
                AddLabel(parameters,param.Title,10F,FontStyle.Bold,16,y,Color.White);Control input;
                if(!string.IsNullOrEmpty(param.Choices))
                {
                    var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(552,y),Size=new Size(190,28)};combo.Items.AddRange(param.Choices.Split('|').Select(L.T).ToArray());decimal val=decimal.Parse(parameterValues[param.Key],CultureInfo.InvariantCulture);combo.SelectedIndex=param.Key=="MaxAnisotropy"?Array.IndexOf(new[]{0,2,4,8,16},(int)val):(int)val;input=combo;
                }
                else
                {
                    var number=new NumericUpDown{Minimum=param.Min,Maximum=param.Max,Increment=param.Step,DecimalPlaces=param.Decimals,Value=decimal.Parse(parameterValues[param.Key],CultureInfo.InvariantCulture),Location=new Point(552,y),Size=new Size(190,28)};input=number;
                }
                input.Enabled=markerBox.Checked;parameters.Controls.Add(input);edited[param.Key]=input;
                AddLabel(parameters,L.T("默认值：")+param.Default.ToString(CultureInfo.InvariantCulture),8.5F,FontStyle.Regular,16,y+30,Color.FromArgb(125,163,182));
                var description=new Label{Text=L.T(param.Description),Location=new Point(16,y+51),Size=new Size(726,32),ForeColor=Color.FromArgb(161,183,197)};parameters.Controls.Add(description);y+=94;
            }
            AddLabel(parameters,L.T("字幕倍率在主页面调整；当前为 ")+subtitleScaleBox.Value.ToString("0.0",CultureInfo.InvariantCulture)+L.T("倍，默认 1.0倍。"),9F,FontStyle.Regular,16,y,PaintSkin.Accent);parameters.AutoScrollMinSize=new Size(0,y+50);
            ignoreErrorsBox.Location=new Point(18,600);dialog.Controls.Add(ignoreErrorsBox);var done=MakeButton(L.T("完成"),690,594,110,36,true);done.DialogResult=DialogResult.OK;dialog.Controls.Add(done);
            var save=MakeButton(L.T("保存至游戏"),510,594,164,36,false);save.DialogResult=DialogResult.Yes;dialog.Controls.Add(save);
            L.ApplyTree(dialog);try{var answer=dialog.ShowDialog(this);foreach(var param in MarkerCatalog.Parameters){var input=edited[param.Key];if(input is NumericUpDown)parameterValues[param.Key]=((NumericUpDown)input).Value.ToString(CultureInfo.InvariantCulture);else {int index=((ComboBox)input).SelectedIndex;parameterValues[param.Key]=(param.Key=="MaxAnisotropy"?new[]{0,2,4,8,16}[index]:index).ToString(CultureInfo.InvariantCulture);}}if(answer==DialogResult.Yes)SaveCurrentConfiguration();}
            finally{markerOptionsList.SelectedIndexChanged-=updateHelp;switches.Controls.Remove(markerOptionsList);dialog.Controls.Remove(ignoreErrorsBox);}
        }
    }

    private static void ConfigureCheck(CheckBox box, string text, Point location, bool value)
    {
        box.Text = text;
        box.Location = location;
        box.AutoSize = true;
        box.Checked = value;
    }

    private void BrowseGame()
    {
        using (var dialog = new FolderBrowserDialog())
        {
            dialog.Description = L.T("选择包含 deadspace2.exe 的目录");
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                gameBox.Text = dialog.SelectedPath;
                RefreshNearbyPackage();
            }
        }
    }

    private void BrowseDlcArchive()
    {
        using(var dialog=new OpenFileDialog())
        {
            dialog.Title=L.T("选择 Severed DLC 数据包");
            dialog.Filter=L.T("DLC 数据包|Severed.marker;*.marker;*.DLC|所有文件|*.*");
            if(System.IO.File.Exists(dlcPathBox.Text))dialog.FileName=dlcPathBox.Text;
            if(dialog.ShowDialog(this)==DialogResult.OK)dlcPathBox.Text=dialog.FileName;
        }
    }

    private void AppendLog(string text)
    {
        logBox.AppendText(text + Environment.NewLine);
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
        if(text.StartsWith(L.T("步骤")) || text.StartsWith(L.T("安装完成")) || text.StartsWith(L.T("卸载完成")) || text.StartsWith(L.T("安装失败")) || text.StartsWith(L.T("卸载失败")) || (text.Contains(L.T("校验")) && text.Length<60)) statusLabel.Text=text;
        progressBar.Invalidate();
        Application.DoEvents();
    }

    private void SetBusy(bool busy)
    {
        this.busy = busy;
        markerBox.Enabled = dlcBox.Enabled = chineseBox.Enabled = ignoreErrorsBox.Enabled = !busy;
        markerOptionsList.Enabled = subtitleScaleBox.Enabled = markerSettingsButton.Enabled = !busy;
        installButton.Enabled = !busy;
        uninstallButton.Enabled = !busy;
        closeButton.Enabled = !busy;
        gameBox.Enabled = dlcPathBox.Enabled = !busy;
        SetMaintenanceBusy(busy);
        foreach(var editor in pathEditors)editor.Enabled=!busy;
        progressBar.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        progressBar.MarqueeAnimationSpeed = busy ? 30 : 0;
        if (busy) progressBar.Value = 0;
    }

    private InstallOptions ReadOptions()
    {
        var values = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < markerOptionsList.Items.Count; i++)
        {
            var option = markerOptionsList.Items[i] as MarkerOption;
            if (option != null) values[option.Key] = markerOptionsList.GetItemChecked(i);
        }
        return new InstallOptions
        {
            Chinese = chineseBox.Checked,
            Dlc = dlcBox.Checked,
            MarkerPatch = markerBox.Checked,
            IgnoreCompatibilityErrors = ignoreErrorsBox.Checked,
            SubtitleScale = subtitleScaleBox.Value,
            DlcArchivePath = dlcPathBox.Text.Trim(),
            MarkerParameters = new Dictionary<string,string>(parameterValues),
            MarkerValues = RequestedMarkerValues(values),
            NodeFix = nodeFixBox.Checked,
            DeployManager = keepManagerBox.Checked
        };
    }

    private void RunInstall()
    {
        if (PreviewOnly) { MessageBox.Show(this,L.T("界面预览，不执行安装。"),L.T("预览")); return; }
        if (MessageBox.Show(this, MaintenanceConfirmationText(),
            L.T("确认安装"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }
        bool installationCompleted = false;
        SetBusy(true);
        logBox.Clear();
        try
        {
            InstallerEngine.ApplySelection(gameBox.Text, ReadOptions(), AppendLog);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;
            AppendLog(L.T("安装完成。"));
            RefreshInstalledState();
            MessageBox.Show(this, ProductInfo.InstallationComplete(dlcBox.Checked) + (dlcBox.Checked && installedSelection != null && !installedSelection.MarkerEnabled ? "\r\n" + L.T("当前随包 MarkerPatch 已停用，启用后才能使用 DLC 入口和运行修正。") : "") + (string.IsNullOrEmpty(InstallerEngine.LastInstallNote) ? "" : "\r\n" + InstallerEngine.LastInstallNote), L.T("完成"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            installationCompleted = true;
        }
        catch (Exception ex)
        {
            AppendLog(L.T("安装失败：") + ex.Message);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            MessageBox.Show(this, InstallerEngine.MaintenanceFailure(ex), L.T("安装失败"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
        if (installationCompleted) Close();
    }

    private void RunUninstall()
    {
        if (PreviewOnly) { MessageBox.Show(this,L.T("界面预览，不执行卸载。"),L.T("预览")); return; }
        if (MessageBox.Show(this,
            L.T("按安装记录移除本工具的改动，并恢复安装前备份。\r\n如文件已被改动或原备份不可用，会停止卸载并提示处理方法。\r\n\r\n继续卸载？"),
            L.T("确认卸载"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }
        SetBusy(true);
        logBox.Clear();
        try
        {
            InstallerEngine.Uninstall(gameBox.Text, AppendLog);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;
            AppendLog(L.T("卸载完成。"));
            RefreshInstalledState();
            MessageBox.Show(this, L.T("已卸载本工具安装的全部组件并清理已识别残留，原文件已按备份还原。程序自身与 Severed.marker 保留。"), L.T("完成"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            string message = UninstallSupport.FailureMessage(ex);
            AppendLog(message);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            MessageBox.Show(this, message, L.T("卸载未完成"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }
}

internal static partial class InstallerEngine
{
    private const string OriginalDat0Sha =
        "fd7d1bdbb5ec6b92324cd877465a029942c78396468695b4551068f4c3562e23";
    private const string PatchedDat0Sha =
        "e9a5a0d212c1848009543ace7876a745d39f498b4c514673bd02df123c182583";
    private const string OriginalDat2Sha =
        "81a145da647660d34a6fcb15ea3a8709c74855cf1beef23fc668a3ae42eb6ea0";
    private const string ChineseDat2Sha =
        "90301421956be9de44c2dbc89d64f3a928bc8cc468ad82927ef059f48b454876";
    private const string ChineseAssetsSha =
        "8c035b6ba2b0ad9e96967866281607b55db14a5a1261b413ec3faee93bd7105a";
    private const string MarkerDllSha =
        "30af0b5ccf50a34d9303d99c04d899c6f6368e522f509ca9d077586542622b55";
    private const string LegacyMarkerDllSha = "c6923b5ecd4c941a9894c21f145a51931efea9ef4ed384a16ee53680563232e5";
    private const string SeveredAsiSha =
        "cbc5d2a6a0eb28353e2af8c833469cd238de74a3535906501e77f235eafea475";
    private const string Candidate1SeveredAsiSha = "62e35b4d9187b5e4a96ee1290db2f79d50c39998a2563eb2b5bab0080ac1a66c";
    private const string LegacySeveredAsiSha = "9a4542eccdbf88ab3ca0c02c52aca34eff9d0ccf3d2fdf71fd1a6314cb561a46";
    private static readonly string RuntimeConfig = Path.Combine("plugins", "SeveredRuntimeFixes.ini");
    private const string MarkerSha =
        "becb3529597bb84d8eb216a5d4ded7efd0191ea4493a24e7ec956078947de097";
    private const long ChineseAssetsBytes = 6981480;
    private const long ChineseDat2Bytes = 3187759104;
    private const long PatchedDat0Bytes = 2163148800;
    private const int BigArchiveAlign = 0x800;

    private static readonly Dictionary<string, string> BaseHashes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "DS2DAT1.DAT", "3537308b17237d733d6b70607dd3c84397c2b65cd53ba648d725fa7ca2584e51" },
            { "DS2DAT6.DAT", "fed960612373e1ab42cef096812f069646cf649420fb8c6d808ecdb1e957e99f" },
            { "DS2DAT7.DAT", "6f933272717800543794723f0395e9697e81ab3fa5820790538a50075519dd4a" },
            { "DS2DAT8.DAT", "b9242f262963b6fd0cef755a6086e41b063a103dce8b42f34e3c0f3b3e0baa7d" },
            { "DS2DAT9.DAT", "21d507b08ef085f9b0cf9ddf0a3f8c5b86992dfd7df40f4f8ef8163c69918d3d" }
        };

    private static readonly HashSet<string> RecommendedKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LoadASIPlugins", "FontScaling",
            "EnableHazardPack", "EnableMartialLawPack", "EnableSupernovaPack",
            "EnableSeveredDLC", "EnableHackerDLC", "EnableZealotDLC",
            "EnableRivetGunDLC", "EnableIgnitionRooms", "EnableOriginalPlasmaCutter",
            "FixFlareArtifacts"
        };

    public static Dictionary<string, bool> RecommendedMarkerValues()
    {
        var values = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in GetMarkerPatchOptions())
            values[option.Key] = option.Recommended;
        return values;
    }

    public static List<MarkerOption> GetMarkerPatchOptions()
    {
        return MarkerCatalog.Features.Select(info=>new MarkerOption{Key=info.Key,Display="["+L.T(info.Category)+"] "+L.T(info.Title),Recommended=RecommendedKeys.Contains(info.Key)}).ToList();
    }

    public static List<GameLocation> FindGameDirs()
    {
        return GameLocations.Find(AppDomain.CurrentDomain.BaseDirectory);
    }
    public static string FindGameDir()
    {
        var games=FindGameDirs();var nearby=games.Where(game=>game.Nearby).ToList();
        if(nearby.Count==1)return nearby[0].Path;
        return games.Count==1?games[0].Path:null;
    }

    public static void Install(string gamePath, InstallOptions options, Action<string> log)
    {
        LastInstallNote = "";
        if (!options.MarkerPatch && !options.Dlc && !options.Chinese)
            throw new InvalidOperationException(L.T("请至少选择一个安装组件。"));
        if (options.SubtitleScale < 0.5m || options.SubtitleScale > 3.0m)
            throw new InvalidDataException(L.T("字幕倍率应在 0.5 到 3.0 之间。"));
        MarkerCatalog.Validate(options.MarkerParameters);
        if(options.MarkerValues.ContainsKey("GyroEnabled") && options.MarkerValues["GyroEnabled"] &&
           (!options.MarkerValues.ContainsKey("RawMouseInput") || !options.MarkerValues["RawMouseInput"]))
            throw new InvalidDataException(L.T("陀螺仪瞄准需要同时开启原始鼠标输入。"));
        if(options.MarkerValues.Keys.Any(key=>MarkerCatalog.Find(key)==null)) throw new InvalidDataException(L.T("MarkerPatch 功能项无效。"));
        string game = ValidateGame(gamePath);
        EnsureGameStopped();
        if (options.Chinese || options.Dlc)
        {
            log(L.T("校验基础游戏 DAT..."));
            ValidateBase(game, options.IgnoreCompatibilityErrors, log);
        }

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string dlcArchive = options.Dlc ? (string.IsNullOrWhiteSpace(options.DlcArchivePath) ? GameLocations.FindPackage(baseDir,game) : Path.GetFullPath(options.DlcArchivePath)) : null;
        bool wasManagedDat2 = IsManagedDat2(game);
        bool chineseApplied = false;
        bool dlcLocalized = false;
        if (options.Dlc && !File.Exists(dlcArchive))
            throw new FileNotFoundException(L.T("找不到 DLC 数据包，请检查 Severed.marker 路径。"), dlcArchive);
        bool previousChinese = ReadState(game, "chinese") == "True";
        bool previousRuntime = ReadState(game, "owns_dlc_runtime") == "True" || ReadState(game, "dlc") == "True";
        bool previousMarker = ReadState(game, "marker_patch") == "True";
        bool previousMarkerAssets = ReadState(game, "owns_marker_assets") == "True";
        string previousAssetManifest = ReadState(game, "marker_assets_manifest_b64");
        bool previousRuntimeConfig = ReadState(game, "owns_runtime_config") == "True";
        string previousBaseHash = ReadState(game, "english_base_sha256");
        DlcPackage package = null;

        long required = 2L * 1024 * 1024 * 1024;
        if (options.Chinese) required += 5L * 1024 * 1024 * 1024;
        if (options.Dlc) required += 10L * 1024 * 1024 * 1024;
        CheckFreeSpace(game, required);

        string temp = Path.Combine(game, ".ds2oneclick-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var undo = new List<Action>();
        try
        {
            string sevenZip = Path.Combine(temp, "7z.exe");
            string sevenZipDll = Path.Combine(temp, "7z.dll");
            ExtractResource("DS2_7Z_EXE", sevenZip);
            ExtractResource("DS2_7Z_DLL", sevenZipDll);

            if (options.Dlc)
            {
                package = ReadDlcPackage(dlcArchive, temp, sevenZip);
                LastDlcDescription = package.Summary;
                log(L.T("数据包：") + package.Summary);
            }
            if (options.MarkerPatch)
            {
                log(L.T("步骤 1/3：安装 MarkerPatch 核心与所选设置..."));
                InstallMarkerPatch(game, temp, options, log, undo);
                InstallMarkerAssets(game, undo, log);
            }
            else if (options.Dlc)
            {
                log(L.T("步骤 1/3：仅安装 DLC 所需的最小 MarkerPatch 运行组件..."));
                InstallMinimalDlcRuntime(game, temp, options, log, undo);
            }
            if (options.Dlc)
            {
                log(L.T("步骤 2/3：安装原始英文 DLC..."));
                InstallDlc(game, dlcArchive, temp, sevenZip, package, log, undo, !wasManagedDat2);
            }
            if (options.Dlc)
            {
                InstallOwnedBytes(
                    game, Path.Combine("plugins", "SeveredPC.asi"),
                    ReadResource("DS2_SEVERED_ASI"), SeveredAsiSha, undo, log);
                InstallOwnedText(game, RuntimeConfig, RuntimeConfiguration(options.NodeFix), null, undo, log);
                log(options.NodeFix ? L.T("已启用 Severed 能量节点修正（仅节点；保留原价格）。依赖 EnableHackerDLC，也会开放本体黑客装备。") : L.T("能量节点修正已关闭，沿用原版商店判断。"));
                InstallOwnedText(
                    game, "DS2DAT2.DAT.severed",
                    "DS2DAT2.DAT was created by DS2SeveredPC (Dead Space 2: Severed for PC). Keep this file: it lets the converter update or remove that archive. Remove both with Remove-SeveredTest.bat.",
                    MarkerSha, undo, log);
            }
            if (options.Chinese)
            {
                log(L.T("步骤 3/3：安装汉化（基础游戏 → DLC 译文）..."));
                chineseApplied = InstallChinese(game, temp, sevenZip, log, undo, options.IgnoreCompatibilityErrors);
                if (chineseApplied)
                    dlcLocalized = InstallDlcChinese(game, temp, log, undo, !options.Dlc && !wasManagedDat2,
                        package != null ? package.PayloadSha256 : previousBaseHash);
            }
            if (options.Dlc && !options.Chinese)
                log(L.T("未选择汉化：DLC 保持原始英文。"));
            File.WriteAllText(
                Path.Combine(game, "DS2SeveredOneClick.install.txt"),
                "version=10\r\n" +
                "chinese=" + (chineseApplied || previousChinese) + "\r\n" +
                "dlc_variant=" + (dlcLocalized ? "zh-workbook-v1" : package != null ? package.Variant : InstalledDlcVariant(game)) + "\r\n" +
                "owns_dat2=" + (options.Dlc || dlcLocalized || wasManagedDat2) + "\r\n" +
                "dlc=" + (options.Dlc || previousRuntime) + "\r\n" +
                "owns_dlc_runtime=" + (options.Dlc || previousRuntime) + "\r\n" +
                "runtime_asi_sha256=" + (options.Dlc ? SeveredAsiSha : previousRuntime ? RuntimeHashForUninstall(game) : "") + "\r\n" +
                "owns_runtime_config=" + (options.Dlc || previousRuntimeConfig) + "\r\n" +
                "marker_patch=" + (options.MarkerPatch || options.Dlc || previousMarker) + "\r\n" +
                "owns_marker_assets=" + (options.MarkerPatch || previousMarkerAssets) + "\r\n" +
                "marker_assets_manifest_b64=" + (options.MarkerPatch ? MarkerAssetManifestBase64() : previousAssetManifest) + "\r\n" +
                "marker_profile=" + (options.MarkerPatch ? "selected" : options.Dlc ? "minimal-or-preserved" : "unchanged") + "\r\n" +
                "subtitle_scale=" + options.SubtitleScale.ToString("0.0#", CultureInfo.InvariantCulture) + "\r\n" +
                "english_base_sha256=" + (package != null ? package.PayloadSha256 : previousBaseHash) + "\r\n" +
                "installed_dat2_sha256=" + ((options.Dlc || dlcLocalized || wasManagedDat2) && File.Exists(Path.Combine(game, "DS2DAT2.DAT")) ? Sha256(Path.Combine(game, "DS2DAT2.DAT")) : "") + "\r\n" +
                "package_version=" + (package != null ? package.Version : ReadState(game, "package_version")) + "\r\n" +
                "ignore_compatibility_errors=" + options.IgnoreCompatibilityErrors + "\r\n" +
                "marker_values=" + (File.Exists(Path.Combine(game, "MarkerPatch.ini")) ?
                    string.Join(";", Regex.Matches(File.ReadAllText(Path.Combine(game, "MarkerPatch.ini")), @"(?m)^\s*(\w+)\s*=\s*([01])\s*$")
                        .Cast<Match>().Where(match=>MarkerCatalog.Find(match.Groups[1].Value)!=null).Select(match => match.Groups[1].Value + "=" + match.Groups[2].Value)) : "") + "\r\n",
                Encoding.UTF8);
            WriteSelectionState(game, options);
            log(L.T("所选步骤已完成；校验结果及跳过原因见上方日志。"));
        }
        catch
        {
            log(L.T("安装失败，正在恢复安装前状态..."));
            for (int i = undo.Count - 1; i >= 0; i--)
            {
                try { undo[i](); } catch (Exception rollbackError) { log(L.T("回滚失败：") + rollbackError.Message); }
            }
            throw;
        }
        finally
        {
            DeleteTemp(temp, game);
        }
    }

    private static void UninstallCore(string gamePath, Action<string> log)
    {
        string game = ValidateGame(gamePath);
        EnsureGameStopped();

        bool stateExists = File.Exists(Path.Combine(game, "DS2SeveredOneClick.install.txt"));
        if (!stateExists)
            throw new InvalidOperationException(L.T("找不到本工具的安装记录，无法确认文件归属，已停止卸载并保留现有文件。"));
        bool ownsChinese = !stateExists || ReadState(game, "chinese") == "True";
        bool ownsRuntime = !stateExists || ReadState(game, "owns_dlc_runtime") == "True" || ReadState(game, "dlc") == "True";
        bool ownsMarker = !stateExists || ReadState(game, "marker_patch") == "True";
        bool ownsDat2 = !stateExists || ReadState(game, "owns_dat2") == "True" || ReadState(game, "dlc") == "True";
        string recordedDat2Hash = ReadState(game, "installed_dat2_sha256");
        string runtimeHash = ownsRuntime ? RuntimeHashForUninstall(game) : null;
        string markerHash = ownsMarker ? MarkerHashForUninstall(game) : null;
        bool ownsRuntimeConfig = ReadState(game, "owns_runtime_config") == "True";
        PreflightUninstall(game, ownsChinese, ownsRuntime, ownsMarker, ownsDat2, recordedDat2Hash, runtimeHash, ownsRuntimeConfig, markerHash);
        string dat0 = Path.Combine(game, "DS2DAT0.DAT");
        string dat0Backup = dat0 + ".ds2oneclick.bak";
        if (ownsChinese && (!File.Exists(dat0) || Sha256(dat0) == PatchedDat0Sha))
        {
            if (!File.Exists(dat0Backup) || Sha256(dat0Backup) != OriginalDat0Sha)
                throw new InvalidOperationException(L.T("找不到可验证的 DS2DAT0.DAT 原始备份，拒绝卸载汉化。"));
            RestoreOrRemoveOwned(game, "DS2DAT0.DAT", File.Exists(dat0) ? PatchedDat0Sha : null, log);
        }
        else if (ownsChinese && File.Exists(dat0) && Sha256(dat0) != OriginalDat0Sha)
        {
            throw new InvalidOperationException(L.T("DS2DAT0.DAT 已被其他工具修改，拒绝覆盖。"));
        }

        string dat2 = Path.Combine(game, "DS2DAT2.DAT");
        if (ownsDat2 && !File.Exists(dat2) && File.Exists(dat2 + ".ds2oneclick.bak"))
            RestoreOrRemoveOwned(game, "DS2DAT2.DAT", null, log);
        else if (ownsDat2 && File.Exists(dat2))
        {
            string dat2Hash = Sha256(dat2);
            if (dat2Hash == recordedDat2Hash || (string.IsNullOrEmpty(recordedDat2Hash) && (dat2Hash == OriginalDat2Sha || dat2Hash == ChineseDat2Sha)))
                RestoreOrRemoveOwned(game, "DS2DAT2.DAT", dat2Hash, log);
            else throw new InvalidOperationException(L.T("DS2DAT2.DAT 已被其他工具修改，已保留文件及卸载记录。"));
        }
        if (ownsRuntime)
        {
            RestoreOrRemoveOwned(game, "DS2DAT2.DAT.severed", MarkerSha, log);
            RestoreOrRemoveOwned(game, Path.Combine("plugins", "SeveredPC.asi"), runtimeHash, log);
            if (ownsRuntimeConfig) RestoreOrRemoveOwned(game, RuntimeConfig, null, log);
        }
        if (ownsMarker)
        {
            RemoveOwned(game, DisabledMarker, markerHash, log);
            RestoreOrRemoveOwned(game, "dinput8.dll", markerHash, log);
            RestoreOrRemoveOwned(game, "MarkerPatch.ini", null, log);
            RestoreLegacyMarkerPlugin(game, log);
        }
        foreach (var asset in OwnedMarkerAssets(game)) RestoreOrRemoveOwned(game, asset.Relative, asset.Sha256, log);
        CleanupKnownResidue(game, log);
        string plugins = Path.Combine(game, "plugins");
        if (Directory.Exists(plugins) && !Directory.EnumerateFileSystemEntries(plugins).Any())
            Directory.Delete(plugins);
        string state = Path.Combine(game, "DS2SeveredOneClick.install.txt");
        if (File.Exists(state)) File.Delete(state);
    }

    private static string RuntimeHashForUninstall(string game)
    {
        string recorded = ReadState(game, "runtime_asi_sha256");
        if (!string.IsNullOrEmpty(recorded))
        {
            if (recorded != SeveredAsiSha && recorded != LegacySeveredAsiSha && recorded != Candidate1SeveredAsiSha)
                throw new InvalidDataException(L.T("安装记录中的 DLC 插件版本不受支持；保留文件和备份。"));
            return recorded;
        }
        string path = Path.Combine(game, "plugins", "SeveredPC.asi");
        // Legacy v9 records did not carry a runtime hash. Recognize only these
        // two frozen binaries; unknown changes still fail uninstall preflight.
        return File.Exists(path) && Sha256(path) == SeveredAsiSha ? SeveredAsiSha : File.Exists(path) && Sha256(path) == Candidate1SeveredAsiSha ? Candidate1SeveredAsiSha : LegacySeveredAsiSha;
    }

    private static void PreflightUninstall(string game, bool chinese, bool runtime, bool marker, bool dat2, string dat2Hash, string runtimeHash, bool runtimeConfig, string markerHash)
    {
        if (!chinese && !runtime && !marker && !dat2)
            throw new InvalidDataException(L.T("安装记录缺少组件信息，无法安全卸载。"));
        var problems = new List<string>();
        var owned = new Dictionary<string, string>();
        string dat0 = Path.Combine(game, "DS2DAT0.DAT");
        if (chinese)
        {
            string current = File.Exists(dat0) ? Sha256(dat0) : null;
            if (current == PatchedDat0Sha || current == null)
            {
                string backup = dat0 + ".ds2oneclick.bak";
                if (!File.Exists(backup) || Sha256(backup) != OriginalDat0Sha)
                    problems.Add(L.T("缺少可验证的 DS2DAT0.DAT 原版备份，无法恢复汉化。"));
            }
            else if (current != OriginalDat0Sha)
                problems.Add(L.T("DS2DAT0.DAT 已被其他工具修改，不能确认恢复方式。"));
            owned["DS2DAT0.DAT"] = null;
        }
        if (dat2 && File.Exists(Path.Combine(game, "DS2DAT2.DAT")))
        {
            string actual = Sha256(Path.Combine(game, "DS2DAT2.DAT"));
            bool matches = !string.IsNullOrEmpty(dat2Hash) ? actual == dat2Hash : actual == OriginalDat2Sha || actual == ChineseDat2Sha;
            if (!matches) problems.Add(L.T("DS2DAT2.DAT 已被其他工具修改，将保留该文件。"));
            owned["DS2DAT2.DAT"] = actual;
        }
        else if (dat2) owned["DS2DAT2.DAT"] = null;
        if (runtime)
        {
            owned["DS2DAT2.DAT.severed"] = MarkerSha;
            owned[Path.Combine("plugins", "SeveredPC.asi")] = runtimeHash;
            if (runtimeConfig) owned[RuntimeConfig] = null;
        }
        if (marker)
        {
            owned["dinput8.dll"] = markerHash;
            owned[DisabledMarker] = markerHash;
            owned["MarkerPatch.ini"] = null;
            foreach (string name in new[] { "MarkerPatch.asi", "MarkerPatch.ini" })
            {
                string original = Path.Combine(game, "plugins", name), disabled = original + ".ds2oneclick-disabled";
                if (File.Exists(original) && File.Exists(disabled)) problems.Add(L.T("旧插件恢复位置被占用：plugins\\") + name);
                if (File.Exists(disabled)) owned[Path.Combine("plugins", name + ".ds2oneclick-disabled")] = null;
            }
        }
        foreach (var asset in OwnedMarkerAssets(game)) owned[asset.Relative] = asset.Sha256;
        owned["DS2SeveredOneClick.install.txt"] = null;
        foreach (var item in owned)
        {
            string path = Path.Combine(game, item.Key);
            if (File.Exists(path) && item.Value != null && Sha256(path) != item.Value)
                problems.Add(item.Key + L.T(" 已被改动，将保留该文件。"));
            foreach (string candidate in new[] { path, path + ".ds2oneclick.bak" })
            {
                if (!File.Exists(candidate)) continue;
                if ((File.GetAttributes(candidate) & FileAttributes.ReadOnly) != 0)
                    problems.Add(Path.GetFileName(candidate) + L.T(" 为只读文件，请检查权限。"));
                else using (var check = new FileStream(candidate, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            }
        }
        if (problems.Count != 0)
            throw new InvalidOperationException(L.T("卸载前检查未通过，尚未修改任何文件：\r\n") + string.Join("\r\n", problems.Distinct()));
    }

    private static bool InstallChinese(string game, string temp, string sevenZip, Action<string> log, List<Action> undo, bool ignoreErrors)
    {
        string assets = Path.Combine(temp, "Chinese.assets.7z");
        ExtractResource("DS2_CHINESE_ASSETS", assets);
        if (new FileInfo(assets).Length != ChineseAssetsBytes || Sha256(assets) != ChineseAssetsSha)
            throw new InvalidDataException(L.T("内置汉化资源包校验失败。"));
        string root = Path.Combine(temp, "cn");
        Directory.CreateDirectory(root);
        Run7Zip(sevenZip, "x -y -o\"" + root + "\" \"" + assets + "\"");
        string font = Path.Combine(root, "4564F3D7.str");
        string text = Path.Combine(root, "D22D5B5A.str");
        if (Sha256(font) != "3b70cff50748d8d931a04bb6ba1286bdef97a0a5f1e2acabaef916043e843dcb" ||
            Sha256(text) != "0a75e1bc191e2127e2cab7f01c2f3ba6dc45e4c3452803aeac97aa403a0546e7")
            throw new InvalidDataException(L.T("内置字体或译文资源校验失败。"));

        string dat0 = Path.Combine(game, "DS2DAT0.DAT");
        string hash = Sha256(dat0);
        if (hash == PatchedDat0Sha)
        {
            log(L.T("DS2DAT0.DAT 已包含本汉化，跳过。"));
            return true;
        }
        if (hash != OriginalDat0Sha)
        {
            if (!ignoreErrors)
                throw new InvalidOperationException(L.T("DS2DAT0.DAT 不是支持的原版版本。"));
            log(L.T("警告：DS2DAT0.DAT 哈希不受支持，跳过基础及 DLC 汉化。"));
            return false;
        }

        string backup = dat0 + ".ds2oneclick.bak";
        if (File.Exists(backup) && Sha256(backup) != OriginalDat0Sha)
            throw new InvalidOperationException(L.T("已有 DAT0 备份哈希不匹配，拒绝覆盖。"));
        string rebuilt = Path.Combine(temp, "DS2DAT0.zh.DAT");
        RebuildBigh(dat0, rebuilt, new Dictionary<uint, string>
        {
            { 0x4564F3D7, font },
            { 0xD22D5B5A, text }
        });
        if (new FileInfo(rebuilt).Length != PatchedDat0Bytes || Sha256(rebuilt) != PatchedDat0Sha)
            throw new InvalidDataException(L.T("重建后的 DS2DAT0.DAT 校验失败。"));
        ReplaceDat(dat0, rebuilt, temp, true, undo, log);
        log(L.T("DS2DAT0.DAT 汉化写入完成。"));
        return true;
    }

    public static string FindDlcArchive()
    {
        return ResolveDlcArchive(AppDomain.CurrentDomain.BaseDirectory);
    }
    private static string ResolveDlcArchive(string baseDir)
    {
        return GameLocations.FindPackage(baseDir,null);
    }

    public static string LastDlcDescription = L.T("尚未读取数据包");
    public static string LastInstallNote = "";
    private static string ReadState(string game, string key)
    {
        string state = Path.Combine(game, "DS2SeveredOneClick.install.txt");
        if (!File.Exists(state)) return "";
        string prefix = key + "=";
        return File.ReadAllLines(state).Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Substring(prefix.Length)).FirstOrDefault() ?? "";
    }
    private static bool IsManagedDat2(string game)
    {
        string target = Path.Combine(game, "DS2DAT2.DAT");
        if (!File.Exists(target) || (ReadState(game, "dlc") != "True" && ReadState(game, "owns_dat2") != "True")) return false;
        string hash = Sha256(target), expected = ReadState(game, "installed_dat2_sha256");
        return string.IsNullOrEmpty(expected) ? hash == OriginalDat2Sha || hash == ChineseDat2Sha : hash == expected;
    }
    private static DlcPackage ReadDlcPackage(string archive, string temp, string sevenZip)
    {
        string folder = Path.Combine(temp, "dlc"); Directory.CreateDirectory(folder);
        Run7Zip(sevenZip, "x -y -o\"" + folder + "\" \"" + archive + "\" DLC.version.json");
        string manifest = Path.Combine(folder, "DLC.version.json");
        if (!File.Exists(manifest) || new FileInfo(manifest).Length > 65536)
            throw new InvalidDataException(L.T("DLC 包缺少有效的版本与校验清单。"));
        return DlcPackage.Parse(File.ReadAllText(manifest, Encoding.UTF8));
    }

    private static string InstalledDlcVariant(string game)
    {
        string path = Path.Combine(game, "DS2DAT2.DAT");
        if (!File.Exists(path)) return "none";
        string hash = Sha256(path);
        return hash == OriginalDat2Sha ? "base-en-v1" : hash == ChineseDat2Sha ? "zh-workbook-v1" : "unknown";
    }

    private static void ReplaceDat(string target, string staged, string temp, bool keepOriginalBackup, List<Action> undo, Action<string> log)
    {
        bool existed = File.Exists(target);
        string previous = Path.Combine(temp, Guid.NewGuid().ToString("N") + ".rollback");
        if (existed) File.Copy(target, previous);
        string backup = target + ".ds2oneclick.bak";
        bool createdBackup = existed && keepOriginalBackup && !File.Exists(backup);
        // Roll back to the state immediately before this step, not an older .bak.
        undo.Add(() =>
        {
            if (existed) File.Copy(previous, target, true);
            else if (File.Exists(target)) File.Delete(target);
            if (createdBackup && File.Exists(backup)) File.Delete(backup);
        });
        if (createdBackup)
        {
            File.Copy(target, backup);
            if (Sha256(backup) != Sha256(previous)) throw new InvalidDataException(L.T("原文件备份校验失败。"));
            log(L.T("已保留原文件备份 ") + Path.GetFileName(backup) + L.T("。"));
        }
        if (existed) File.Replace(staged, target, null);
        else File.Move(staged, target);
    }

    private static void InstallDlc(string game, string archive, string temp, string sevenZip, DlcPackage package, Action<string> log, List<Action> undo, bool keepOriginalBackup)
    {
        string root = Path.Combine(temp, "dlc");
        log(L.T("包内版本与协议通过：") + package.Summary);
        if (package.PosterSha256 != null)
        {
            Run7Zip(sevenZip, "x -y -o\"" + root + "\" \"" + archive + "\" Severed.poster.jpg");
            string poster = Path.Combine(root, "Severed.poster.jpg");
            if (!File.Exists(poster) || new FileInfo(poster).Length != package.PosterBytes || Sha256(poster) != package.PosterSha256)
                throw new InvalidDataException(L.T("数据包海报大小或 SHA-256 校验失败。"));
            log(L.T("DLC 海报完整性校验通过。"));
        }
        else log(L.T("历史数据包没有海报，英文数据仍可按原校验安装。"));
        Run7Zip(sevenZip, "x -y -o\"" + root + "\" \"" + archive + "\" DS2DAT2.DAT");
        string extracted = Path.Combine(root, "DS2DAT2.DAT");
        if (!File.Exists(extracted) || new FileInfo(extracted).Length != package.PayloadBytes || Sha256(extracted) != package.PayloadSha256)
            throw new InvalidDataException(L.T("原始英文 DS2DAT2.DAT 大小或 SHA-256 校验失败。"));
        using (var input = File.OpenRead(extracted))
        {
            byte[] header = ReadExact(input, 16);
            if (Encoding.ASCII.GetString(header, 0, 4) != "BIGH" || ReadLe32(header, 4) != package.PayloadBytes)
                throw new InvalidDataException(L.T("DLC 数据不是支持的完整 BIGH 归档。"));
        }
        string target = Path.Combine(game, "DS2DAT2.DAT");
        if (File.Exists(target) && Sha256(target) == package.PayloadSha256)
        {
            if (keepOriginalBackup)
            {
                string backup = target + ".ds2oneclick.bak";
                if (File.Exists(backup)) throw new IOException(L.T("已有 DLC 备份但缺少可确认的安装归属，请保留文件并核对旧安装。"));
                File.Copy(target, backup);
                if (Sha256(backup) != package.PayloadSha256) throw new IOException(L.T("原有 DLC 备份校验失败。"));
                undo.Add(() => { if (File.Exists(backup)) File.Delete(backup); });
            }
            log(L.T("DS2DAT2.DAT 已是此包的英文数据，跳过替换。"));
            return;
        }
        ReplaceDat(target, extracted, temp, keepOriginalBackup, undo, log);
        if (Sha256(target) != package.PayloadSha256) throw new InvalidDataException(L.T("原始 DLC 落盘校验失败。"));
        log(L.T("原始英文 DLC 安装完成（SHA-256 ") + package.PayloadSha256 + L.T("）。"));
    }

    private static bool InstallDlcChinese(string game, string temp, Action<string> log, List<Action> undo, bool keepOriginalBackup, string acceptedEnglishHash)
    {
        string dat2 = Path.Combine(game, "DS2DAT2.DAT");
        if (!File.Exists(dat2))
        {
            log(L.T("未发现 DLC，跳过 DLC 汉化；基础游戏汉化已完成。"));
            return false;
        }
        string hash = Sha256(dat2);
        if (hash == ChineseDat2Sha)
        {
            log(L.T("DLC 已是 XLSX 汉化版本 zh-workbook-v1，跳过。"));
            return true;
        }
        if (hash != OriginalDat2Sha)
        {
            if (!string.IsNullOrEmpty(acceptedEnglishHash) && hash == acceptedEnglishHash)
            {
                LastInstallNote = L.T("新版 DLC 暂无匹配汉化，已保留英文。");
                log(L.T("此英文 DLC 版本较新，当前汉化不兼容其数据布局；保留英文 DLC，未应用旧译文。"));
                return false;
            }
            throw new InvalidDataException(L.T("DLC 汉化仅支持已知英文基座；请先安装受支持的原始 DLC。"));
        }
        string cn = Path.Combine(temp, "cn");
        string stream = Path.Combine(cn, "DLC.4F21D71F.str");
        string toc = Path.Combine(cn, "DLC.4F9AE556.toc");
        if (Sha256(stream) != "678cd673364ed40e96d73d4de02ff0941399be08354be252e3f340134d76e718" ||
            Sha256(toc) != "4771a3b58d03a6216dc5e802ca5e04676ee638ae8e1f0f49485f9f88abb6bd35")
            throw new InvalidDataException(L.T("内置 DLC 汉化资源校验失败。"));
        string rebuilt = Path.Combine(temp, "DS2DAT2.zh.DAT");
        RebuildBigh(dat2, rebuilt, new Dictionary<uint, string>
        {
            { 0x4F21D71F, stream },
            { 0x4F9AE556, toc }
        });
        if (new FileInfo(rebuilt).Length != ChineseDat2Bytes || Sha256(rebuilt) != ChineseDat2Sha)
            throw new InvalidDataException(L.T("DLC 汉化输出校验失败。"));
        ReplaceDat(dat2, rebuilt, temp, keepOriginalBackup, undo, log);
        if (Sha256(dat2) != ChineseDat2Sha) throw new InvalidDataException(L.T("DLC 汉化落盘校验失败。"));
        log(L.T("DLC 218 条 XLSX 译文安装完成（zh-workbook-v1，90301421...）。"));
        return true;
    }

    private static void InstallMarkerPatch(string game, string temp, InstallOptions options, Action<string> log, List<Action> undo)
    {
        QuarantineLegacyMarkerPlugin(game, undo, log);
        string dll = Path.Combine(temp, "dinput8.dll");
        ExtractResource("DS2_MARKER_DLL", dll);
        if (Sha256(dll) != MarkerDllSha) throw new InvalidDataException(L.T("内置 MarkerPatch DLL 校验失败。"));
        InstallBinary(game, "dinput8.dll", File.ReadAllBytes(dll), MarkerDllSha, undo, log);

        string ini = ConfigureMarkerPatch(ReadResourceText("DS2_MARKER_INI"), options);
        InstallText(game, "MarkerPatch.ini", ini, null, undo, log);
    }

    private static void InstallMinimalDlcRuntime(string game, string temp, InstallOptions options, Action<string> log, List<Action> undo)
    {
        QuarantineLegacyMarkerPlugin(game, undo, log);
        byte[] loader = ReadResource("DS2_MARKER_DLL");
        InstallBinary(game, "dinput8.dll", loader, MarkerDllSha, undo, log);
        string path = Path.Combine(game, "MarkerPatch.ini");
        string ini;
        if (File.Exists(path))
        {
            ini = File.ReadAllText(path);
            ini = SetIni(ini, "LoadASIPlugins", "1");
            ini = SetIni(ini, "EnableSeveredDLC", "1");
            if (options.NodeFix) ini = SetIni(ini, "EnableHackerDLC", "1");
            log(DependencyDescription(options));
        }
        else
        {
            var minimal = new InstallOptions { SubtitleScale = options.SubtitleScale, NodeFix = options.NodeFix };
            minimal.MarkerValues = RecommendedMarkerValues().Keys.ToDictionary(key => key,
                key => key == "LoadASIPlugins" || key == "EnableSeveredDLC" || (options.NodeFix && key == "EnableHackerDLC"), StringComparer.OrdinalIgnoreCase);
            ini = ConfigureMarkerPatch(ReadResourceText("DS2_MARKER_INI"), minimal);
            log(DependencyDescription(options));
        }
        InstallText(game, "MarkerPatch.ini", ini, null, undo, log);
    }

    private static void QuarantineLegacyMarkerPlugin(string game, List<Action> undo, Action<string> log)
    {
        string pluginDir = Path.Combine(game, "plugins");
        Directory.CreateDirectory(pluginDir);
        foreach (string name in new[] { "MarkerPatch.asi", "MarkerPatch.ini" })
        {
            string source = Path.Combine(pluginDir, name);
            if (!File.Exists(source)) continue;
            string disabled = source + ".ds2oneclick-disabled";
            if (File.Exists(disabled))
                throw new InvalidOperationException(L.T("旧 MarkerPatch 隔离文件已存在：") + disabled);
            File.Move(source, disabled);
            undo.Add(() =>
            {
                if (File.Exists(disabled) && !File.Exists(source))
                    File.Move(disabled, source);
            });
            log(L.T("已隔离冲突插件 plugins\\") + name + L.T("。"));
        }
    }

    private static void RestoreLegacyMarkerPlugin(string game, Action<string> log)
    {
        string pluginDir = Path.Combine(game, "plugins");
        foreach (string name in new[] { "MarkerPatch.asi", "MarkerPatch.ini" })
        {
            string original = Path.Combine(pluginDir, name);
            string disabled = original + ".ds2oneclick-disabled";
            if (File.Exists(disabled) && File.Exists(original))
                throw new IOException(L.T("旧插件恢复位置被占用，已保留备份：") + original);
            if (File.Exists(disabled) && !File.Exists(original))
            {
                File.Move(disabled, original);
                log(L.T("已恢复旧插件 plugins\\") + name + L.T("。"));
            }
        }
    }

    private static string ConfigureMarkerPatch(string text, InstallOptions options)
    {
        foreach (var pair in options.MarkerValues)
        {
            text = SetIni(text, pair.Key, options.MarkerPatch && pair.Value ? "1" : "0");
        }
        text = SetIni(text, "FontScalingFactor", options.SubtitleScale.ToString("0.0#", CultureInfo.InvariantCulture));
        foreach(var pair in options.MarkerParameters) text=SetIni(text,pair.Key,pair.Value);
        if(options.Dlc)
        {
            text=SetIni(text,"LoadASIPlugins","1");
            text=SetIni(text,"EnableSeveredDLC","1");
            if (options.NodeFix) text=SetIni(text,"EnableHackerDLC","1");
        }
        return text;
    }

    private static string SetIni(string text, string key, string value)
    {
        return SetMarkerIniValue(text, key, value);
    }

    private static void ValidateBase(string game, bool ignoreErrors, Action<string> log)
    {
        string dat0 = Path.Combine(game, "DS2DAT0.DAT");
        if (!File.Exists(dat0)) throw new FileNotFoundException(L.T("缺少 DS2DAT0.DAT。"), dat0);
        string dat0Hash = Sha256(dat0);
        if (dat0Hash != OriginalDat0Sha && dat0Hash != PatchedDat0Sha)
        {
            if (!ignoreErrors)
                throw new InvalidDataException(L.T("DS2DAT0.DAT 不属于 Steam/EA pristine 或本补丁版本。"));
            log(L.T("警告：DS2DAT0.DAT 哈希不受支持。建议恢复原版后再安装汉化。"));
        }
        foreach (var pair in BaseHashes)
        {
            string path = Path.Combine(game, pair.Key);
            if (!File.Exists(path))
                throw new FileNotFoundException(L.T("缺少基础 DAT。"), path);
            if (Sha256(path) != pair.Value)
            {
                if (!ignoreErrors)
                    throw new InvalidDataException(pair.Key + L.T(" 哈希不匹配。"));
                log(L.T("警告：") + pair.Key + L.T(" 不是 pristine 哈希，按忽略兼容性错误继续。"));
            }
        }
    }

    private static string ValidateGame(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
            throw new DirectoryNotFoundException(L.T("游戏目录不存在。"));
        string game = Path.GetFullPath(gamePath);
        if (!File.Exists(Path.Combine(game, "deadspace2.exe")))
            throw new FileNotFoundException(L.T("目录中没有 deadspace2.exe。"), game);
        return game;
    }

    private static void EnsureGameStopped()
    {
#if !TEST_BUILD
        if (Process.GetProcessesByName("deadspace2").Length != 0)
            throw new InvalidOperationException(L.T("请先关闭 deadspace2.exe。"));
#endif
    }

    private static bool IsGameDir(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
            File.Exists(Path.Combine(path, "deadspace2.exe"));
    }

    private static void CheckFreeSpace(string game, long requiredBytes)
    {
        string root = Path.GetPathRoot(Path.GetFullPath(game));
        long free = new DriveInfo(root).AvailableFreeSpace;
        if (free < requiredBytes)
            throw new IOException(L.T("磁盘空间不足。需要至少 ") + (requiredBytes >> 20) + L.T(" MiB，当前可用 ") + (free >> 20) + L.T(" MiB。"));
    }

    private static void Run7Zip(string exe, string arguments)
    {
        var info = new ProcessStartInfo(exe, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using (var process = Process.Start(info))
        {
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new IOException(L.T("7-Zip 解压失败：") + (stderr + stdout).Trim());
        }
    }

    private static byte[] ReadResource(string name)
    {
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (stream == null) throw new InvalidDataException("Missing embedded resource: " + name);
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }
    }

    private static string ReadResourceText(string name)
    {
        return Encoding.UTF8.GetString(ReadResource(name));
    }

    private static void ExtractResource(string name, string path)
    {
        File.WriteAllBytes(path, ReadResource(name));
    }

    private static void InstallOwnedBytes(string game, string relative, byte[] data, string hash, List<Action> undo, Action<string> log)
    {
        string path = Path.Combine(game, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        bool sameBytes = hash != null && File.Exists(path) && Sha256(path) == hash;
        string backup = path + ".ds2oneclick.bak";
        bool existed = File.Exists(path);
        byte[] previous = existed ? File.ReadAllBytes(path) : null;
        string statePath = Path.Combine(game, "DS2SeveredOneClick.install.txt");
        bool ownConfig = relative == "MarkerPatch.ini" && File.Exists(statePath) &&
            File.ReadAllLines(statePath).Any(line => line == "marker_patch=True");
        bool ownRuntime = relative == Path.Combine("plugins", "SeveredPC.asi") &&
            (ReadState(game, "owns_dlc_runtime") == "True" || ReadState(game, "dlc") == "True") &&
            existed && (Sha256(path) == LegacySeveredAsiSha || Sha256(path) == SeveredAsiSha);
        bool ownMarkerBinary = relative == "dinput8.dll" && ReadState(game, "marker_patch") == "True" && existed && IsKnownMarkerHash(Sha256(path));
        bool ownDlcMarker = relative == "DS2DAT2.DAT.severed" && (ReadState(game, "owns_dlc_runtime") == "True" || ReadState(game, "dlc") == "True");
        bool ownRuntimeConfig = relative == RuntimeConfig && ReadState(game, "owns_runtime_config") == "True";
        bool ownMarkerAsset = IsOwnedMarkerAsset(game, relative, existed ? Sha256(path) : null);
        bool ownManager = relative == ManagerName && File.Exists(Path.Combine(game, ManagerInfo)) &&
            Regex.IsMatch(File.ReadAllText(Path.Combine(game, ManagerInfo)), "\"sha256\"\\s*:\"" + (existed ? Sha256(path) : "missing") + "\"");
        if (existed && File.Exists(backup) && !ownConfig && !ownRuntime && !ownRuntimeConfig && !ownManager && !ownMarkerBinary && !ownDlcMarker && !ownMarkerAsset)
            throw new IOException(relative + L.T(" 已有原始备份且当前文件又被改动，已停止覆盖。"));
        bool createdBackup = existed && !File.Exists(backup) && !ownConfig && !ownRuntime && !ownRuntimeConfig && !ownManager && !ownMarkerBinary && !ownDlcMarker && !ownMarkerAsset;
        undo.Add(() =>
        {
            if (existed) { if (!sameBytes) File.WriteAllBytes(path, previous); }
            else if (File.Exists(path)) File.Delete(path);
            if (createdBackup && File.Exists(backup)) File.Delete(backup);
        });
        if (createdBackup) File.Copy(path, backup);
        if (sameBytes) return;
        File.WriteAllBytes(path, data);
        if (hash != null && Sha256(path) != hash) throw new InvalidDataException(relative + L.T(" 写入校验失败。"));
        log(L.T("已安装 ") + relative + L.T("。"));
    }

    private static void InstallOwnedText(string game, string relative, string text, string expectedHash, List<Action> undo, Action<string> log)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        InstallOwnedBytes(game, relative, bytes, expectedHash, undo, log);
    }

    private static void InstallBinary(string game, string name, byte[] data, string hash, List<Action> undo, Action<string> log)
    {
        InstallOwnedBytes(game, name, data, hash, undo, log);
    }

    private static void InstallText(string game, string name, string text, string expectedHash, List<Action> undo, Action<string> log)
    {
        InstallOwnedBytes(game, name, Encoding.UTF8.GetBytes(text), expectedHash, undo, log);
    }

    private static void RemoveOwned(string game, string relative, string expectedHash, Action<string> log)
    {
        string path = Path.Combine(game, relative);
        if (File.Exists(path) && Sha256(path) != expectedHash)
            throw new IOException(relative + L.T(" 在卸载期间发生变化，已保留文件及安装记录。"));
        if (File.Exists(path) && Sha256(path) == expectedHash)
        {
            File.Delete(path);
            log(L.T("已移除 ") + relative + L.T("。"));
        }
    }

    private static void RestoreOrRemoveOwned(string game, string relative, string expectedHash, Action<string> log)
    {
        string path = Path.Combine(game, relative);
        string backup = path + ".ds2oneclick.bak";
        if (!File.Exists(path) && !File.Exists(backup)) return;
        if (expectedHash != null && File.Exists(path) && Sha256(path) != expectedHash)
            throw new IOException(relative + L.T(" 在卸载期间发生变化，已保留文件及安装记录。"));
        if (File.Exists(backup))
        {
            string restore = path + ".ds2oneclick-restore-" + Guid.NewGuid().ToString("N");
            string expectedBackup = Sha256(backup);
            try
            {
                File.Copy(backup, restore, false);
                if (Sha256(restore) != expectedBackup) throw new InvalidDataException(relative + L.T(" 备份复制校验失败。"));
                if (File.Exists(path)) File.Replace(restore, path, null);
                else File.Move(restore, path);
                if (Sha256(path) != expectedBackup) throw new InvalidDataException(relative + L.T(" 还原校验失败。"));
                File.Delete(backup);
            }
            finally { if (File.Exists(restore)) File.Delete(restore); }
            log(L.T("已恢复 ") + relative + L.T("。"));
        }
        else
        {
            File.Delete(path);
            log(L.T("已移除 ") + relative + L.T("。"));
        }
    }

    private static void RebuildBigh(string source, string target, Dictionary<uint, string> replacements)
    {
        var entries = new Dictionary<uint, Tuple<uint, uint>>();
        byte[] trailer;
        using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
        {
            byte[] header = ReadExact(stream, 16);
            if (Encoding.ASCII.GetString(header, 0, 4) != "BIGH") throw new InvalidDataException(L.T("DAT 不是 BIGH。"));
            uint count = ReadBe32(header, 8);
            uint headerSize = ReadBe32(header, 12);
            byte[] table = ReadExact(stream, checked((int)count * 12));
            for (int i = 0; i < count; i++)
            {
                uint offset = ReadBe32(table, i * 12);
                uint size = ReadBe32(table, i * 12 + 4);
                uint hash = ReadBe32(table, i * 12 + 8);
                entries[hash] = Tuple.Create(offset, size);
            }
            int trailerLength = checked((int)headerSize - 16 - checked((int)count * 12));
            trailer = ReadExact(stream, trailerLength);
        }
        foreach (var pair in replacements)
        {
            entries[pair.Key] = Tuple.Create((uint)0, checked((uint)new FileInfo(pair.Value).Length));
        }
        var ordered = entries.OrderBy(pair => pair.Key).ToList();
        int newHeaderSize = 16 + ordered.Count * 12 + trailer.Length;
        long position = Align(newHeaderSize, BigArchiveAlign);
        var layout = new List<Tuple<uint, long, long>>();
        foreach (var pair in ordered)
        {
            layout.Add(Tuple.Create(pair.Key, position, (long)pair.Value.Item2));
            position = Align(position + pair.Value.Item2, BigArchiveAlign);
        }
        if (position > uint.MaxValue) throw new InvalidDataException(L.T("重建后的 DAT 超过 4 GiB。"));

        using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
        using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20))
        {
            byte[] head = new byte[16];
            Encoding.ASCII.GetBytes("BIGH").CopyTo(head, 0);
            WriteLe32(head, 4, (uint)position);
            WriteBe32(head, 8, (uint)ordered.Count);
            WriteBe32(head, 12, (uint)newHeaderSize);
            output.Write(head, 0, head.Length);
            byte[] row = new byte[12];
            foreach (var item in layout)
            {
                WriteBe32(row, 0, (uint)item.Item2);
                WriteBe32(row, 4, (uint)item.Item3);
                WriteBe32(row, 8, item.Item1);
                output.Write(row, 0, row.Length);
            }
            output.Write(trailer, 0, trailer.Length);
            foreach (var item in layout)
            {
                WriteZerosTo(output, item.Item2);
                if (replacements.ContainsKey(item.Item1))
                {
                    using (var replacement = File.OpenRead(replacements[item.Item1]))
                        replacement.CopyTo(output, 1 << 20);
                }
                else
                {
                    var original = entries[item.Item1];
                    input.Position = original.Item1;
                    CopyBytes(input, output, original.Item2);
                }
            }
            WriteZerosTo(output, position);
        }
    }

    private static void WriteZerosTo(Stream stream, long position)
    {
        byte[] zeros = new byte[1 << 16];
        while (stream.Position < position)
        {
            int count = (int)Math.Min(zeros.Length, position - stream.Position);
            stream.Write(zeros, 0, count);
        }
    }

    private static void CopyBytes(Stream input, Stream output, long count)
    {
        byte[] buffer = new byte[1 << 20];
        while (count > 0)
        {
            int wanted = (int)Math.Min(buffer.Length, count);
            int read = input.Read(buffer, 0, wanted);
            if (read <= 0) throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            count -= read;
        }
    }

    private static byte[] ReadExact(Stream stream, int count)
    {
        byte[] data = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(data, offset, count - offset);
            if (read <= 0) throw new EndOfStreamException();
            offset += read;
        }
        return data;
    }

    private static long Align(long value, int alignment)
    {
        return (value + alignment - 1) / alignment * alignment;
    }

    private static uint ReadBe32(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) |
               ((uint)data[offset + 2] << 8) | data[offset + 3];
    }

    private static uint ReadLe32(byte[] data, int offset)
    {
        return data[offset] | ((uint)data[offset + 1] << 8) |
               ((uint)data[offset + 2] << 16) | ((uint)data[offset + 3] << 24);
    }

    private static void WriteBe32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteLe32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    private static string Sha256(string path)
    {
        using (var algorithm = SHA256.Create())
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
        {
            byte[] hash = algorithm.ComputeHash(stream);
            var text = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash) text.Append(value.ToString("x2"));
            return text.ToString();
        }
    }

    private static void DeleteTemp(string temp, string game)
    {
        try
        {
            string fullTemp = Path.GetFullPath(temp);
            string fullGame = Path.GetFullPath(game).TrimEnd('\\') + "\\";
            string leaf = Path.GetFileName(fullTemp);
            if (fullTemp.StartsWith(fullGame, StringComparison.OrdinalIgnoreCase) &&
                leaf.StartsWith(".ds2oneclick-", StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(fullTemp))
            {
                Directory.Delete(fullTemp, true);
            }
        }
        catch
        {
        }
    }
}
