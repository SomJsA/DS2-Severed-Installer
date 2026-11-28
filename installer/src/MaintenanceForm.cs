using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

internal sealed partial class InstallerForm
{
    private readonly CheckBox nodeFixBox = new CheckBox();
    private readonly CheckBox keepManagerBox = new CheckBox();
    private SkinButton reloadSettingsButton, startupControlButton, updateCheckButton, customMarkerButton;
    private CustomMarkerPackage pendingMarkerPackage;
    private string pendingMarkerName;
    private readonly Label dependencyHint = new Label(), installedHint = new Label();
    private readonly LinkLabel markerIssuesLink = new LinkLabel();
    private readonly Dictionary<string, bool> dependencyOriginal = new Dictionary<string, bool>();
    private readonly Dictionary<string, bool> inactivePreferences = new Dictionary<string, bool>();
    private InstalledSelection installedSelection;
    private bool synchronizing, checkingUpdates;

    private void InitializeMaintenanceUi()
    {
        ClientSize = new Size(920, 822);
        ConfigureCheck(nodeFixBox, L.T("修正 Severed 商店能量节点"), new Point(26, 582), true);
        ConfigureCheck(keepManagerBox, L.T("安装 / 更新游戏根目录管理器"), new Point(26, 616), true);
        nodeFixBox.ForeColor = keepManagerBox.ForeColor = Color.FromArgb(192, 215, 226);
        Controls.Add(nodeFixBox); Controls.Add(keepManagerBox);
        reloadSettingsButton = MakeButton(L.T("读取当前安装"), 484, 579, 192, 30, false);
        startupControlButton = MakeButton(L.T("切换 MarkerPatch 启动"), 484, 613, 410, 30, false);
        updateCheckButton = MakeButton(L.T("检查更新"), 690, 579, 204, 30, false);
        Controls.Add(reloadSettingsButton); Controls.Add(startupControlButton); Controls.Add(updateCheckButton);
        customMarkerButton = MakeButton(L.T("自定义"), 806, 365, 78, 25, false);
        markerBox.TrailingActionWidth = 74;
        customMarkerButton.Name = "customMarkerButton";
        Controls.Add(customMarkerButton);
        customMarkerButton.BringToFront();
        customMarkerButton.Click += (s, e) => ShowCustomMarkerMenu();
        reloadSettingsButton.Click += (s, e) => RefreshInstalledState();
        startupControlButton.Click += (s, e) => ToggleMarkerStartup();
        updateCheckButton.Click += (s, e) => CheckUpdates(false);
        var markerNotice = AddLabel(this,L.T("MarkerPatch 已预设常用修复，可在上方调整。"),8.5F,FontStyle.Regular,26,650,Color.FromArgb(161,183,197));
        markerNotice.Name="markerPresetNotice";
        dependencyHint.Location = new Point(26, 674); dependencyHint.Size = new Size(868, 36);
        dependencyHint.Font = new Font(Font.FontFamily, 8.5F); dependencyHint.ForeColor = Color.FromArgb(201, 190, 151);
        installedHint.Location = new Point(26, 715); installedHint.Size = new Size(690, 24); installedHint.ForeColor = Color.FromArgb(146, 178, 193);
        markerIssuesLink.Text = "MarkerPatch Issues"; L.KeepLiteral(markerIssuesLink);
        markerIssuesLink.Location = new Point(731, 715); markerIssuesLink.Size = new Size(163, 24); markerIssuesLink.LinkColor = PaintSkin.Accent;
        markerIssuesLink.LinkClicked += (s, e) => OpenPage("https://github.com/Wemino/MarkerPatch/issues");
        Controls.Add(dependencyHint); Controls.Add(installedHint); Controls.Add(markerIssuesLink);
        progressBar.Top = 748; statusLabel.Top = 765;
        uninstallButton.Top = installButton.Top = 764;
        nodeFixBox.CheckedChanged += (s, e) => UpdateSelectionSummary();
        gameBox.Leave += (s, e) => RefreshInstalledState();
        markerOptionsList.ItemCheck += (s, e) =>
        {
            if (synchronizing) return;
            var option = markerOptionsList.Items[e.Index] as MarkerOption;
            if (option != null && RequiredDependency(option.Key)) e.NewValue = CheckState.Checked;
            else if (!markerBox.Checked) e.NewValue = e.CurrentValue;
        };
        Shown += (s, e) => { RefreshInstalledState(); if (!PreviewOnly) CheckUpdates(true); };
        RefreshInstalledState();
    }

    private void OnDlcSelectionChanged()
    {
        if (!synchronizing && dlcBox.Checked) nodeFixBox.Checked = true;
        UpdateSelectionSummary();
    }

    private bool RequiredDependency(string key)
    {
        return dlcBox.Checked && (key == "LoadASIPlugins" || key == "EnableSeveredDLC" || (nodeFixBox.Checked && key == "EnableHackerDLC"));
    }
    private void SynchronizeDependencies()
    {
        if (synchronizing) return;
        synchronizing = true;
        try
        {
            for (int i = 0; i < markerOptionsList.Items.Count; i++)
            {
                var option = (MarkerOption)markerOptionsList.Items[i]; bool previous;
                bool nodeRequired = dlcBox.Checked && nodeFixBox.Checked && option.Key == "EnableHackerDLC";
                bool dlcRequired = dlcBox.Checked && (option.Key == "LoadASIPlugins" || option.Key == "EnableSeveredDLC");
                if (option.RequiredForNode != nodeRequired || option.RequiredForDlc != dlcRequired)
                {
                    bool check = markerOptionsList.GetItemChecked(i);
                    option.RequiredForNode = nodeRequired; option.RequiredForDlc = dlcRequired;
                    markerOptionsList.Items[i] = option; markerOptionsList.SetItemChecked(i, check);
                }
                if (!markerBox.Checked)
                {
                    if (!inactivePreferences.ContainsKey(option.Key))
                        inactivePreferences[option.Key] = dependencyOriginal.ContainsKey(option.Key) ? dependencyOriginal[option.Key] : markerOptionsList.GetItemChecked(i);
                    if (!RequiredDependency(option.Key)) markerOptionsList.SetItemChecked(i, false);
                }
                else if (inactivePreferences.TryGetValue(option.Key, out previous))
                {
                    markerOptionsList.SetItemChecked(i, previous); inactivePreferences.Remove(option.Key);
                }
                if (RequiredDependency(option.Key))
                {
                    if (!dependencyOriginal.ContainsKey(option.Key)) dependencyOriginal[option.Key] = markerOptionsList.GetItemChecked(i);
                    markerOptionsList.SetItemChecked(i, true);
                }
                else if (dependencyOriginal.TryGetValue(option.Key, out previous))
                {
                    markerOptionsList.SetItemChecked(i, markerBox.Checked && previous); dependencyOriginal.Remove(option.Key);
                }
            }
        }
        finally { synchronizing = false; }
    }
    private Dictionary<string, bool> RequestedMarkerValues(Dictionary<string, bool> values)
    {
        foreach (var pair in dependencyOriginal) values[pair.Key] = pair.Value;
        foreach (var pair in inactivePreferences) values[pair.Key] = pair.Value;
        return values;
    }
    private void ApplyMaintenanceLanguage()
    {
        if (reloadSettingsButton == null) return;
        nodeFixBox.Text = L.T("修正 Severed 商店能量节点"); keepManagerBox.Text = L.T("安装 / 更新游戏根目录管理器");
        reloadSettingsButton.Text = L.T("读取当前安装"); updateCheckButton.Text = L.T("检查更新");
        customMarkerButton.Text = L.T("自定义");
        UpdateMaintenanceSummary();
    }
    private void UpdateMaintenanceSummary()
    {
        if (reloadSettingsButton == null) return;
        SynchronizeDependencies();
        nodeFixBox.Enabled = !busy && dlcBox.Checked;
        subtitleScaleBox.Enabled = !busy && markerBox.Checked;
        dependencyHint.Text = InstallerEngine.DependencyDescription(new InstallOptions { Dlc = dlcBox.Checked, NodeFix = nodeFixBox.Checked });
        if (markerBox.Checked && !nodeFixBox.Checked && markerOptionsList.Items.Cast<MarkerOption>().Select((o,i) => new { Option=o, Index=i }).Any(x => x.Option.Key == "EnableHackerDLC" && markerOptionsList.GetItemChecked(x.Index)))
            dependencyHint.Text += " " + L.T("黑客装备独立开关仍为开启，可在详细设置中调整。");
        if (installedSelection != null && installedSelection.ExternalMarker)
        {

            markerIssuesLink.Visible = true;
        }
        else markerIssuesLink.Visible = false;
        bool managed = installedSelection != null && installedSelection.HasRecord;
        installedHint.Text = managed ? L.T("安装记录：") + installedSelection.Version : L.T("未发现本工具的安装记录");
        if (installedSelection != null && installedSelection.ExternalMarker) installedHint.Text = L.T("外置加载器 / MarkerPatch：兼容性未验证，可向对应作者提 Issue。");
        if (managed && installedSelection.MarkerPresent && !installedSelection.ExternalMarker)
            installedHint.Text += " | MarkerPatch " + (installedSelection.CustomMarker ? L.T("自定义版本") : installedSelection.MarkerVersion ?? L.T("已识别版本"));
        installButton.Text = managed ? L.T("升级 / 更改安装") : L.T("安装所选组件");
        startupControlButton.Enabled = !busy && managed && installedSelection.MarkerPresent && !installedSelection.ExternalMarker;
        startupControlButton.Text = installedSelection != null && installedSelection.MarkerEnabled ? L.T("停用 MarkerPatch") : L.T("启用 MarkerPatch");
        customMarkerButton.Enabled = !busy;
        customMarkerButton.FlowGlow = pendingMarkerPackage != null || installedSelection != null && installedSelection.CustomMarker;
        uninstallButton.Enabled = !busy && (managed || InstallerEngine.HasKnownResidue(gameBox.Text));
    }

    private void RefreshInstalledState()
    {
        if (reloadSettingsButton == null) return;
        try
        {
            if (!File.Exists(Path.Combine(gameBox.Text, "deadspace2.exe"))) { installedSelection = null; UpdateSelectionSummary(); return; }
            var selection = InstallerEngine.InspectInstallation(gameBox.Text);
            installedSelection = selection;
            if (selection.HasRecord)
            {
                synchronizing = true;
                try
                {
                    dependencyOriginal.Clear(); inactivePreferences.Clear();
                    chineseBox.Checked = selection.Options.Chinese; dlcBox.Checked = selection.Options.Dlc; markerBox.Checked = selection.Options.MarkerPatch;
                    nodeFixBox.Checked = selection.Options.NodeFix; subtitleScaleBox.Value = selection.Options.SubtitleScale;
                    if (!File.Exists(dlcPathBox.Text) && File.Exists(selection.Options.DlcArchivePath)) dlcPathBox.Text = selection.Options.DlcArchivePath;
                    parameterValues.Clear(); foreach (var pair in selection.Options.MarkerParameters) parameterValues[pair.Key] = pair.Value;
                    for (int i = 0; i < markerOptionsList.Items.Count; i++)
                    {
                        var option = (MarkerOption)markerOptionsList.Items[i]; bool value;
                        if (selection.Options.MarkerValues.TryGetValue(option.Key, out value)) markerOptionsList.SetItemChecked(i, value);
                    }
                }
                finally { synchronizing = false; }
            }
            UpdateSelectionSummary();
        }
        catch (Exception ex) { installedSelection = null; installedHint.Text = L.T("读取当前安装失败：") + ex.Message; }
    }

    private void SetMaintenanceBusy(bool value)
    {
        if (reloadSettingsButton == null) return;
        nodeFixBox.Enabled = keepManagerBox.Enabled = reloadSettingsButton.Enabled = !value;
        updateCheckButton.Enabled = !value && !checkingUpdates;
        startupControlButton.Enabled = !value;
        customMarkerButton.Enabled = !value;
        if (!value) UpdateSelectionSummary();
    }
    private void SaveCurrentConfiguration()
    {
        if (PreviewOnly) { MessageBox.Show(this, L.T("界面预览，不执行安装。")); return; }
        SetBusy(true);
        try { InstallerEngine.SaveConfiguration(gameBox.Text, ReadOptions(), AppendLog); RefreshInstalledState(); MessageBox.Show(this, L.T("设置已保存，重新启动游戏后生效；没有改写 DAT。")); }
        catch (Exception ex) { MessageBox.Show(this, InstallerEngine.MaintenanceFailure(ex), L.T("保存设置失败"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { SetBusy(false); }
    }
    private void ToggleMarkerStartup()
    {
        if (PreviewOnly || installedSelection == null) return;
        bool enable = !installedSelection.MarkerEnabled;
        string prompt = enable ? L.T("启用 MarkerPatch？重新启动游戏后生效。") : L.T("停用 MarkerPatch 后，DLC 入口与依赖它的运行修正将不再生效。继续？");
        if (!enable) prompt += "\r\n\r\n" + ProductInfo.SaveCompatibility;
        if (MessageBox.Show(this, prompt, L.T("MarkerPatch 启动控制"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        SetBusy(true);
        try { InstallerEngine.SetMarkerEnabled(gameBox.Text, enable, AppendLog); RefreshInstalledState(); }
        catch (Exception ex) { MessageBox.Show(this, InstallerEngine.MaintenanceFailure(ex)); }
        finally { SetBusy(false); }
    }
    private void ShowCustomMarkerMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(L.T("导入 MarkerPatch ZIP…"), null, (s, e) => ImportMarkerZip());
        var restore = menu.Items.Add(L.T("恢复随包版"), null, (s, e) =>
        {
            if (pendingMarkerPackage != null) { pendingMarkerPackage = null; pendingMarkerName = null; UpdateSelectionSummary(); }
            else ChangeInstalledMarker(null);
        });
        restore.Enabled = pendingMarkerPackage != null || installedSelection != null && installedSelection.CustomMarker && !installedSelection.ExternalMarker;
        string ini = Path.Combine(gameBox.Text, "MarkerPatch.ini");
        var open = menu.Items.Add(L.T("打开 MarkerPatch.ini"), null, (s, e) =>
        {
            if (PreviewOnly) return;
            try { Process.Start(new ProcessStartInfo("notepad.exe") { Arguments = "\"" + Path.GetFullPath(ini) + "\"", UseShellExecute = false }); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message); }
        });
        open.Enabled = File.Exists(ini);
        menu.Closed += (s, e) => menu.Dispose();
        menu.Show(customMarkerButton, new Point(0, customMarkerButton.Height));
    }

    private void ImportMarkerZip()
    {
        if (PreviewOnly) return;
        using (var picker = new OpenFileDialog { Filter = L.T("MarkerPatch 发布包 (*.zip)|*.zip"), Title = L.T("导入 MarkerPatch ZIP…"), CheckFileExists = true })
        {
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            string warning = L.T("只导入可信来源的 MarkerPatch 发布包。自定义版本与 DLC 和运行修正的兼容性需要实测，可从这里恢复随包版。");
            if (MessageBox.Show(this, warning + "\r\n\r\n" + Path.GetFileName(picker.FileName), L.T("导入 MarkerPatch ZIP…"),
                MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
            if (installedSelection != null && installedSelection.HasRecord && installedSelection.MarkerPresent && !installedSelection.ExternalMarker)
                ChangeInstalledMarker(picker.FileName);
            else
            {
                try
                {
                    pendingMarkerPackage = InstallerEngine.ReadCustomMarkerZip(picker.FileName);
                    pendingMarkerName = Path.GetFileName(picker.FileName);
                    markerBox.Checked = true;
                    UpdateSelectionSummary();
                    MessageBox.Show(this, L.T("已选好自定义发布包，将在安装所选组件时使用。"));
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("导入失败"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    private void ChangeInstalledMarker(string zip)
    {
        if (PreviewOnly) return;
        SetBusy(true);
        try
        {
            InstallerEngine.SwitchMarkerVersion(gameBox.Text, zip, AppendLog);
            pendingMarkerPackage = null; pendingMarkerName = null;
            RefreshInstalledState();
            MessageBox.Show(this, zip == null ? L.T("已恢复随包 MarkerPatch。") : L.T("已导入自定义 MarkerPatch。"));
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("切换 MarkerPatch 失败"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
    }
    private string MaintenanceConfirmationText()
    {
        var options = ReadOptions();
        string yes = L.T("安装 / 保留"), no = L.T("移除本工具安装的此组件");
        return L.T("将把安装调整为以下组合：") + "\r\n\r\n" +
            L.T("简体中文汉化") + "：" + (options.Chinese ? yes : no) + "\r\n" +
            "Severed DLC：" + (options.Dlc ? yes : no) + "\r\nMarkerPatch：" + (options.MarkerPatch ? yes : L.T("只保留 DLC 必需依赖；没有 DLC 时移除")) +
            "\r\n\r\n" + InstallerEngine.DependencyDescription(options) + "\r\n\r\n" +
            L.T("识别到旧版时直接迁移，保留自定义值和原始备份。异常时请备份存档并尝试全新安装。") +
            (pendingMarkerPackage != null && (options.MarkerPatch || options.Dlc) ? "\r\n\r\n" + L.T("本次使用自定义发布包 ") + pendingMarkerName : "") +
            (!options.Dlc && installedSelection != null && installedSelection.Options.Dlc ? "\r\n\r\n" + ProductInfo.SaveCompatibility : "") +
            (installedSelection != null && installedSelection.ExternalMarker ? "\r\n\r\n" + L.T("检测到外置加载器或其他版本，可能不兼容；请核对来源，必要时向对应作者提 Issue。") : "") + L.T("\r\n\r\n继续安装？");
    }
    public void SelectGameDirectory(string path) { gameBox.Text = path; RefreshNearbyPackage(); RefreshInstalledState(); }
    private void OpenPage(string url) { WebLinks.Show(this,url); }
    private void CheckUpdates(bool automatic)
    {
        if (checkingUpdates || busy || PreviewOnly) return;
        checkingUpdates = true; updateCheckButton.Enabled = false;
        ThreadPool.QueueUserWorkItem(s =>
        {
            UpdateReport report = UpdateChecker.Check();
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try { BeginInvoke((Action)(() => { checkingUpdates = false; updateCheckButton.Enabled = !busy; if (!busy && (!automatic || report.Installer != null || report.MarkerUpdate)) ShowUpdateReport(report); })); }
            catch (InvalidOperationException) { }
        });
    }
    private Form CreateUpdateDialog(UpdateReport report)
    {
            string title = report.Installer != null ? (report.MarkerUpdate ? L.T("安装器与 MarkerPatch 更新") : L.T("安装器更新")) : (report.MarkerUpdate ? L.T("MarkerPatch 更新") : L.T("更新检查"));
            var dialog = MakeDialog(title, new Size(740,435));
            bool available = report.Installer != null || report.MarkerUpdate;
            string body = report.Installer != null ? L.T("安装器有可用更新：") + report.Installer.Tag + "\r\n" + UpdateChecker.BriefNotes(report.Installer.Notes, true) : (report.MarkerUpdate ? L.T("MarkerPatch 有可选更新。") : (string.IsNullOrWhiteSpace(report.Error) ? L.T("安装器已是最新版本。") : L.T("更新检查未完成，可打开发布页手动查看。")));
            body += "\r\n\r\nMarkerPatch " + L.T("随包版本：") + UpdateChecker.BundledMarkerVersion + "\r\n" +
                (report.MarkerUpdate ? (string.IsNullOrEmpty(report.MarkerTag) ? "" : L.T("可导入版本 ") + report.MarkerTag + "\r\n") +
                    UpdateChecker.BriefNotes(report.MarkerNotes, false) + "\r\n" + L.T("下载 ZIP 后可在 MarkerPatch 卡片的「自定义」中试用。") :
                    (string.IsNullOrWhiteSpace(report.Error) ? L.T("未发现较新的上游发行版本。") : L.T("部分更新信息未能获取，可打开发布页查看。")));
            if (!string.IsNullOrWhiteSpace(report.Error)) body += "\r\n\r\n" + report.Error;
            bool canOpen = available || !string.IsNullOrWhiteSpace(report.Error);
            string page = UpdateChecker.BrowserPage(report);
            if (canOpen) body += "\r\n\r\n" + L.T("是否打开 GitHub 发布网页？选择否则继续正常使用。");
            dialog.AutoScroll=false;dialog.Padding=new Padding(16);
            var text = new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = body, Dock=DockStyle.Fill, BackColor = PaintSkin.Panel, ForeColor = Color.White };
            L.KeepLiteral(text); dialog.Controls.Add(text);
            var footer=new Panel{Dock=DockStyle.Bottom,Height=124};dialog.Controls.Add(footer);
            var addressLabel = new Label { Text=L.T("发布页地址（也可复制到浏览器）："),AutoSize=true,Location=new Point(0,8),ForeColor=Color.White }; footer.Controls.Add(addressLabel);
            var address = new TextBox { ReadOnly=true,Text=page,Location=new Point(0,36),Size=new Size(footer.Width,28),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right }; L.KeepLiteral(address); footer.Controls.Add(address);
            var buttons=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=44,ColumnCount=3,RowCount=1};
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,37.5f));buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,37.5f));footer.Controls.Add(buttons);
            var copy=MakeButton(L.T("复制链接"),0,0,180,40,false); copy.Click+=(s,e)=>WebLinks.CopyAddress(address,addressLabel); buttons.Controls.Add(copy,0,0);
            var yes = MakeButton(L.T("是，打开网页"),0,0,280,40,true); yes.Enabled=canOpen; yes.DialogResult=DialogResult.Yes; buttons.Controls.Add(yes,1,0);
            var no = MakeButton(L.T("否，继续使用"),0,0,240,40,false); no.DialogResult=DialogResult.No; buttons.Controls.Add(no,2,0);
            foreach(var button in new[]{copy,yes,no}){button.Dock=DockStyle.Fill;button.Margin=new Padding(0,2,8,2);}no.Margin=new Padding(0,2,0,2);
            dialog.CancelButton=no; L.ApplyTree(dialog);return dialog;
    }
    private void ShowUpdateReport(UpdateReport report)
    {
        using(var dialog=CreateUpdateDialog(report))
            if(dialog.ShowDialog(this)==DialogResult.Yes)OpenPage(UpdateChecker.BrowserPage(report));
    }
}
