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
    private SkinButton reloadSettingsButton, startupControlButton, updateCheckButton;
    private readonly Label dependencyHint = new Label(), installedHint = new Label();
    private readonly LinkLabel markerIssuesLink = new LinkLabel();
    private readonly Dictionary<string, bool> dependencyOriginal = new Dictionary<string, bool>();
    private readonly Dictionary<string, bool> inactivePreferences = new Dictionary<string, bool>();
    private InstalledSelection installedSelection;
    private bool synchronizing, checkingUpdates;

    private void InitializeMaintenanceUi()
    {
        ClientSize = new Size(920, 798);
        ConfigureCheck(nodeFixBox, L.T("修正 Severed 商店能量节点"), new Point(26, 582), true);
        ConfigureCheck(keepManagerBox, L.T("安装 / 更新游戏根目录管理器"), new Point(26, 616), true);
        nodeFixBox.ForeColor = keepManagerBox.ForeColor = Color.FromArgb(192, 215, 226);
        Controls.Add(nodeFixBox); Controls.Add(keepManagerBox);
        reloadSettingsButton = MakeButton(L.T("读取当前安装"), 484, 579, 192, 30, false);
        startupControlButton = MakeButton(L.T("切换 MarkerPatch 启动"), 484, 613, 410, 30, false);
        updateCheckButton = MakeButton(L.T("检查更新"), 690, 579, 204, 30, false);
        Controls.Add(reloadSettingsButton); Controls.Add(startupControlButton); Controls.Add(updateCheckButton);
        reloadSettingsButton.Click += (s, e) => RefreshInstalledState();
        startupControlButton.Click += (s, e) => ToggleMarkerStartup();
        updateCheckButton.Click += (s, e) => CheckUpdates(false);
        dependencyHint.Location = new Point(26, 650); dependencyHint.Size = new Size(868, 36);
        dependencyHint.Font = new Font(Font.FontFamily, 8.5F); dependencyHint.ForeColor = Color.FromArgb(201, 190, 151);
        installedHint.Location = new Point(26, 691); installedHint.Size = new Size(690, 24); installedHint.ForeColor = Color.FromArgb(146, 178, 193);
        markerIssuesLink.Text = "MarkerPatch Issues"; L.KeepLiteral(markerIssuesLink);
        markerIssuesLink.Location = new Point(731, 691); markerIssuesLink.Size = new Size(163, 24); markerIssuesLink.LinkColor = PaintSkin.Accent;
        markerIssuesLink.LinkClicked += (s, e) => OpenPage("https://github.com/Wemino/MarkerPatch/issues");
        Controls.Add(dependencyHint); Controls.Add(installedHint); Controls.Add(markerIssuesLink);
        progressBar.Top = 724; statusLabel.Top = 741;
        uninstallButton.Top = installButton.Top = 740;
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
        installedHint.Text = managed ? L.T("当前安装：") + (installedSelection.Version == ProductInfo.ReleaseVersion ? installedSelection.Version : L.T("旧版记录：") + installedSelection.Version) : L.T("未发现本工具的安装记录");
        if (installedSelection != null && installedSelection.ExternalMarker) installedHint.Text = L.T("外置加载器 / MarkerPatch：兼容性未验证，可向对应作者提 Issue。");
        if (managed && installedSelection.MarkerPresent && !installedSelection.ExternalMarker) installedHint.Text += " | MarkerPatch " + (installedSelection.MarkerIsCurrent ? "3.0.7" : L.T("已识别旧版"));
        installButton.Text = managed ? L.T("升级 / 更改安装") : L.T("安装所选组件");
        startupControlButton.Enabled = !busy && managed && installedSelection.MarkerPresent && !installedSelection.ExternalMarker;
        startupControlButton.Text = installedSelection != null && installedSelection.MarkerEnabled ? L.T("停用随包 MarkerPatch") : L.T("启用随包 MarkerPatch");
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
        string prompt = enable ? L.T("启用随包 MarkerPatch？重新启动游戏后生效。") : L.T("停用随包 MarkerPatch 后，DLC 入口与依赖它的运行修正将不再生效。继续？");
        if (MessageBox.Show(this, prompt, L.T("MarkerPatch 启动控制"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        SetBusy(true);
        try { InstallerEngine.SetMarkerEnabled(gameBox.Text, enable, AppendLog); RefreshInstalledState(); }
        catch (Exception ex) { MessageBox.Show(this, InstallerEngine.MaintenanceFailure(ex)); }
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
            (installedSelection != null && installedSelection.ExternalMarker ? "\r\n\r\n" + L.T("检测到外置加载器或其他版本，可能不兼容；请核对来源，必要时向对应作者提 Issue。") : "") + L.T("\r\n\r\n继续安装？");
    }
    public void SelectGameDirectory(string path) { gameBox.Text = path; RefreshNearbyPackage(); RefreshInstalledState(); }
    private static void OpenPage(string url)
    {
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https" || uri.Host != "github.com") throw new InvalidDataException("Invalid GitHub link");
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
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
    private void ShowUpdateReport(UpdateReport report)
    {
        using (var dialog = MakeDialog(L.T("更新与累计日志"), new Size(790, 590)))
        {
            bool available = report.Installer != null || report.MarkerUpdate;
            string body = report.Installer == null ? L.T("本程序没有发现更新版本。") : L.T("发现新版：") + report.Installer.Tag + "\r\n\r\n" + report.CombinedNotes;
            body += "\r\n\r\nMarkerPatch " + L.T("随包版本：") + UpdateChecker.BundledMarkerVersion + "\r\n" +
                (report.MarkerUpdate ? L.T("上游有更新；新版本与当前节点修正的兼容性尚需验证。") + "\r\n" + report.MarkerNotes : L.T("未发现较新的上游发行版本。"));
            if (!string.IsNullOrWhiteSpace(report.Error)) body += "\r\n\r\n" + report.Error;
            if (available) body += "\r\n\r\n" + L.T("是否打开 GitHub 发布网页？选择否则继续正常使用。");
            var text = new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = body, Location = new Point(16,16), Size = new Size(758,490), BackColor = PaintSkin.Panel, ForeColor = Color.White };
            L.KeepLiteral(text); dialog.Controls.Add(text);
            var yes = MakeButton(L.T("是，打开网页"),16,530,240,40,true); yes.Enabled=available; yes.DialogResult=DialogResult.Yes; dialog.Controls.Add(yes);
            var no = MakeButton(L.T("否，继续使用"),534,530,240,40,false); no.DialogResult=DialogResult.No; dialog.Controls.Add(no);
            dialog.CancelButton=no; L.ApplyTree(dialog);
            if (dialog.ShowDialog(this) == DialogResult.Yes)
            {
                try { OpenPage(report.Installer != null ? report.Installer.Page : report.MarkerPage); }
                catch (Exception ex) { MessageBox.Show(this,L.T("无法打开更新网页：")+ex.Message); }
            }
        }
    }
}
