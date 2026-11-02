using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class InstallerForm
{
    private readonly HashSet<string> reviewedMarkerTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool EnsureMarkerReviewBeforeInstall()
    {
        string target = gameBox.Text.Trim();
        try { target = Path.GetFullPath(target).TrimEnd('\\', '/'); } catch { }
        if (reviewedMarkerTargets.Contains(target)) return true;
        if (!ShowMarkerSettingsDialog(true)) return false;
        reviewedMarkerTargets.Add(target);
        return true;
    }
    private bool ShowMarkerSettingsDialog(bool installationReview)
    {
        using (var dialog = MakeDialog(L.T("其他由 MarkerPatch 提供的功能改善"), new Size(820,650)))
        using (var delay = new System.Windows.Forms.Timer { Interval = 100 })
        {
            dialog.Name = installationReview ? "markerInstallReviewDialog" : "markerSettingsDialog";
            int offset = installationReview ? 128 : 0;
            if (installationReview)
            {
                var notice = new Label { Name="markerReviewNotice", Text=L.T("本程序仅按随包 MarkerPatch 版本提供安装与管理，不维护与其他版本的兼容性。\r\n已预选部分功能，请查看并按需勾选；仅安装 DLC 时保留必需依赖。\r\n以后可回到本程序读取配置、保存设置或更改安装。确认了解后继续。"), Location=new Point(14,14), Size=new Size(792,112), Padding=new Padding(10,8,10,8), BackColor=PaintSkin.Panel, ForeColor=Color.FromArgb(229,204,145), Font=new Font(Font.FontFamily,9F) };
                dialog.Controls.Add(notice);
            }
            var tabs = new TabControl { Location=new Point(14,14+offset), Size=new Size(792,573-offset) };
            var switches = new TabPage(L.T("功能开关")) { BackColor=PaintSkin.Background,ForeColor=Color.White };
            var parameters = new TabPage(L.T("数值参数")) { BackColor=PaintSkin.Background,ForeColor=Color.White,AutoScroll=true };
            tabs.TabPages.Add(switches);tabs.TabPages.Add(parameters);dialog.Controls.Add(tabs);
            var hint = AddLabel(switches,markerBox.Checked ? string.Format(CultureInfo.InvariantCulture,L.T("预设启用 {0} 项；DLC 所需选项按当前选择自动启用。"),InstallerEngine.RecommendedMarkerValues().Count(p=>p.Value)) : L.T("未勾选 MarkerPatch：只启用 DLC 依赖，其他功能需在主页面勾选。"),9F,FontStyle.Regular,14,14,Color.FromArgb(170,196,210));
            L.KeepLiteral(hint);
            AddLabel(switches,L.T("尚未测试全部功能组合及与其他模组的兼容性。"),9F,FontStyle.Regular,14,37,Color.FromArgb(219,193,139));
            markerOptionsList.Location=new Point(14,66);markerOptionsList.Size=new Size(432,456-offset);switches.Controls.Add(markerOptionsList);
            var help=new TextBox { Location=new Point(460,66),Size=new Size(302,456-offset),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=PaintSkin.Panel,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle };
            switches.Controls.Add(help);
            EventHandler updateHelp=(sender,e)=>{var option=markerOptionsList.SelectedItem as MarkerOption;if(option==null)return;var info=MarkerCatalog.Find(option.Key);help.Text=L.T(info.Title)+"\r\n\r\n"+L.T(info.Description)+"\r\n\r\n"+option.StatusText+L.T("\r\n\r\n配置项：")+option.Key;};
            markerOptionsList.SelectedIndexChanged+=updateHelp;
            if(markerOptionsList.Items.Count>0) markerOptionsList.SelectedIndex=installationReview ? Math.Max(0,markerOptionsList.Items.Cast<MarkerOption>().ToList().FindIndex(p=>p.Key=="HighCoreCPUFix")) : 0;
            updateHelp(null,EventArgs.Empty);
            AddLabel(parameters,L.T("尚未测试全部功能组合及与其他模组的兼容性。"),9F,FontStyle.Regular,16,14,Color.FromArgb(219,193,139));
            var edited=new Dictionary<string,Control>();int y=48;
            foreach(var param in MarkerCatalog.Parameters)
            {
                AddLabel(parameters,param.Title,10F,FontStyle.Bold,16,y,Color.White);Control input;
                if(!string.IsNullOrEmpty(param.Choices))
                {
                    var combo=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(552,y),Size=new Size(190,28) };
                    combo.Items.AddRange(param.Choices.Split('|').Select(L.T).ToArray());decimal val=decimal.Parse(parameterValues[param.Key],CultureInfo.InvariantCulture);combo.SelectedIndex=param.Key=="MaxAnisotropy"?Array.IndexOf(new[]{0,2,4,8,16},(int)val):(int)val;input=combo;
                }
                else input=new NumericUpDown { Minimum=param.Min,Maximum=param.Max,Increment=param.Step,DecimalPlaces=param.Decimals,Value=decimal.Parse(parameterValues[param.Key],CultureInfo.InvariantCulture),Location=new Point(552,y),Size=new Size(190,28) };
                input.Enabled=markerBox.Checked;parameters.Controls.Add(input);edited[param.Key]=input;
                AddLabel(parameters,L.T("默认值：")+param.Default.ToString(CultureInfo.InvariantCulture),8.5F,FontStyle.Regular,16,y+30,Color.FromArgb(125,163,182));
                parameters.Controls.Add(new Label { Text=L.T(param.Description),Location=new Point(16,y+51),Size=new Size(726,32),ForeColor=Color.FromArgb(161,183,197) });y+=94;
            }
            AddLabel(parameters,L.T("字幕倍率在主页面调整；当前为 ")+subtitleScaleBox.Value.ToString("0.0",CultureInfo.InvariantCulture)+L.T("倍，默认 1.0倍。"),9F,FontStyle.Regular,16,y,PaintSkin.Accent);parameters.AutoScrollMinSize=new Size(0,y+50);
            if(installationReview)
            {
                hint.Visible=false;
                var optional=new CheckBox { Name="markerReviewFeatures",Text=L.T("启用 MarkerPatch 自选功能（可逐项调整）"),Location=new Point(14,14),AutoSize=true,ForeColor=Color.FromArgb(170,196,210),Checked=markerBox.Checked };
                optional.CheckedChanged+=(sender,e)=>{markerBox.Checked=optional.Checked;foreach(var control in edited.Values)control.Enabled=markerBox.Checked;UpdateSelectionSummary();};
                switches.Controls.Add(optional);
            }
            ignoreErrorsBox.Location=new Point(18,600);dialog.Controls.Add(ignoreErrorsBox);
            var watch=new Stopwatch();
            SkinButton accept,decline;
            if(installationReview)
            {
                accept=MakeButton(L.T("是，继续"),628,594,172,36,true);accept.Name="markerReviewAccept";accept.DialogResult=DialogResult.Yes;accept.Enabled=false;L.KeepLiteral(accept);
                decline=MakeButton(L.T("取消并返回"),480,594,136,36,false);decline.Name="markerReviewDecline";decline.DialogResult=DialogResult.No;
                Action updateDelay=()=>{int remaining=Math.Max(0,(int)Math.Ceiling(3.0-watch.Elapsed.TotalSeconds));accept.Text=remaining>0?string.Format(CultureInfo.InvariantCulture,L.T("是，继续（{0}秒）"),remaining):L.T("是，继续");accept.Enabled=remaining==0;if(remaining==0)delay.Stop();};
                delay.Tick+=(sender,e)=>updateDelay();dialog.Shown+=(sender,e)=>{watch.Restart();updateDelay();delay.Start();};
                dialog.FormClosing+=(sender,e)=>{if(dialog.DialogResult==DialogResult.Yes && watch.Elapsed.TotalSeconds<3.0){e.Cancel=true;dialog.DialogResult=DialogResult.None;}};
            }
            else
            {
                accept=MakeButton(L.T("完成"),690,594,110,36,true);accept.DialogResult=DialogResult.OK;
                decline=MakeButton(L.T("保存至游戏"),510,594,164,36,false);decline.DialogResult=DialogResult.Yes;
            }
            dialog.Controls.Add(accept);dialog.Controls.Add(decline);dialog.AcceptButton=accept;
            if(installationReview)dialog.CancelButton=decline;
            L.ApplyTree(dialog);
            try
            {
                var answer=dialog.ShowDialog(this);
                foreach(var param in MarkerCatalog.Parameters)
                {
                    var input=edited[param.Key];
                    if(input is NumericUpDown)parameterValues[param.Key]=((NumericUpDown)input).Value.ToString(CultureInfo.InvariantCulture);
                    else {int index=((ComboBox)input).SelectedIndex;parameterValues[param.Key]=(param.Key=="MaxAnisotropy"?new[]{0,2,4,8,16}[index]:index).ToString(CultureInfo.InvariantCulture);}
                }
                if(!installationReview && answer==DialogResult.Yes)SaveCurrentConfiguration();
                return installationReview && answer==DialogResult.Yes && watch.Elapsed.TotalSeconds>=3.0;
            }
            finally
            {
                delay.Stop();markerOptionsList.SelectedIndexChanged-=updateHelp;switches.Controls.Remove(markerOptionsList);dialog.Controls.Remove(ignoreErrorsBox);
            }
        }
    }
}
