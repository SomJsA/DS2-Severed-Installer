using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class InstallerForm
{
    private void ShowMarkerSettingsDialog()
    {
        using(var dialog=MakeDialog(L.T("其他由 MarkerPatch 提供的功能改善"),new Size(820,650)))
        {
            dialog.Name="markerSettingsDialog";
            var tabs=new TabControl{Location=new Point(14,14),Size=new Size(792,573)};
            var switches=new TabPage(L.T("功能开关")){BackColor=PaintSkin.Background,ForeColor=Color.White};var parameters=new TabPage(L.T("数值参数")){BackColor=PaintSkin.Background,ForeColor=Color.White,AutoScroll=true};tabs.TabPages.Add(switches);tabs.TabPages.Add(parameters);dialog.Controls.Add(tabs);
            AddLabel(switches,(markerBox.Checked ? string.Format(CultureInfo.InvariantCulture,L.T("预设启用 {0} 项；DLC 所需选项按当前选择自动启用。"),InstallerEngine.RecommendedMarkerValues().Count(p=>p.Value)) : L.T("未勾选 MarkerPatch：只启用 DLC 依赖，其他功能需在主页面勾选。")),9F,FontStyle.Regular,14,14,Color.FromArgb(170,196,210));
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

}
