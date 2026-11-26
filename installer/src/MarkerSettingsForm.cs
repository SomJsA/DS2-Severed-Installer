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
            dialog.Name="markerSettingsDialog"; dialog.AutoScroll=false; dialog.Padding=new Padding(14);
            var tabs=new TabControl{Dock=DockStyle.Fill};
            var switches=new TabPage(L.T("功能开关")){BackColor=PaintSkin.Background,ForeColor=Color.White};
            var parameters=new TabPage(L.T("数值参数")){BackColor=PaintSkin.Background,ForeColor=Color.White,AutoScroll=true};
            tabs.TabPages.Add(switches);tabs.TabPages.Add(parameters);dialog.Controls.Add(tabs);
            var footer=new Panel{Dock=DockStyle.Bottom,Height=82};dialog.Controls.Add(footer);
            ignoreErrorsBox.Location=new Point(2,3);footer.Controls.Add(ignoreErrorsBox);
            var done=MakeButton(L.T("完成"),0,40,110,36,true);done.DialogResult=DialogResult.OK;footer.Controls.Add(done);
            var save=MakeButton(L.T("保存至游戏"),0,40,164,36,false);save.DialogResult=DialogResult.Yes;footer.Controls.Add(save);
            EventHandler layoutFooter=(s,e)=>{done.Left=footer.ClientSize.Width-done.Width;save.Left=done.Left-16-save.Width;};footer.Resize+=layoutFooter;layoutFooter(null,EventArgs.Empty);
            var preset=AddLabel(switches,(markerBox.Checked?string.Format(CultureInfo.InvariantCulture,L.T("预设启用 {0} 项；DLC 所需选项按当前选择自动启用。"),InstallerEngine.RecommendedMarkerValues().Count(p=>p.Value)):L.T("未勾选 MarkerPatch：只启用 DLC 依赖，其他功能需在主页面勾选。")),9F,FontStyle.Regular,14,8,Color.FromArgb(170,196,210));
            var warning=AddLabel(switches,L.T("尚未测试全部功能组合及与其他模组的兼容性。"),9F,FontStyle.Regular,14,40,Color.FromArgb(219,193,139));
            preset.AutoSize=warning.AutoSize=false; switches.Controls.Add(markerOptionsList);
            var help=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=PaintSkin.Panel,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};switches.Controls.Add(help);
            EventHandler layoutSwitches=(s,e)=>{
                int width=Math.Max(1,switches.ClientSize.Width-28),height=Math.Max(40,switches.ClientSize.Height-86);
                preset.Size=new Size(width,30);warning.Size=new Size(width,32);
                int listWidth=(int)((width-14)*.56f);
                markerOptionsList.Bounds=new Rectangle(14,74,listWidth,height);
                help.Bounds=new Rectangle(28+listWidth,74,Math.Max(1,width-listWidth-14),height);
            };
            switches.Resize+=layoutSwitches;
            EventHandler updateHelp=(s,e)=>{var option=markerOptionsList.SelectedItem as MarkerOption;if(option==null)return;var info=MarkerCatalog.Find(option.Key);help.Text=L.T(info.Title)+"\r\n\r\n"+L.T(info.Description)+"\r\n\r\n"+option.StatusText+L.T("\r\n\r\n配置项：")+option.Key;};
            markerOptionsList.SelectedIndexChanged+=updateHelp;if(markerOptionsList.Items.Count>0)markerOptionsList.SelectedIndex=0;updateHelp(null,EventArgs.Empty);
            var parameterWarning=AddLabel(parameters,L.T("尚未测试全部功能组合及与其他模组的兼容性。"),9F,FontStyle.Regular,16,8,Color.FromArgb(219,193,139));parameterWarning.AutoSize=false;
            var edited=new Dictionary<string,Control>();var rows=new List<Control[]>();
            foreach(var param in MarkerCatalog.Parameters)
            {
                var title=AddLabel(parameters,param.Title,10F,FontStyle.Bold,16,0,Color.White);title.AutoSize=false;Control input;
                if(!string.IsNullOrEmpty(param.Choices))
                {
                    var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Size=new Size(190,28)};combo.Items.AddRange(param.Choices.Split('|').Select(L.T).ToArray());decimal val=decimal.Parse(parameterValues[param.Key],CultureInfo.InvariantCulture);combo.SelectedIndex=param.Key=="MaxAnisotropy"?Array.IndexOf(new[]{0,2,4,8,16},(int)val):(int)val;input=combo;
                }
                else input=new NumericUpDown{Minimum=param.Min,Maximum=param.Max,Increment=param.Step,DecimalPlaces=param.Decimals,Value=decimal.Parse(parameterValues[param.Key],CultureInfo.InvariantCulture),Size=new Size(190,28)};
                input.Enabled=markerBox.Checked;parameters.Controls.Add(input);edited[param.Key]=input;
                var defaultValue=AddLabel(parameters,L.T("默认值：")+param.Default.ToString(CultureInfo.InvariantCulture),8.5F,FontStyle.Regular,16,0,Color.FromArgb(125,163,182));defaultValue.AutoSize=false;
                var description=new Label{Text=L.T(param.Description),ForeColor=Color.FromArgb(161,183,197)};parameters.Controls.Add(description);
                rows.Add(new Control[]{title,input,defaultValue,description});
            }
            var scaleHint=AddLabel(parameters,L.T("字幕倍率在主页面调整；当前为 ")+subtitleScaleBox.Value.ToString("0.0",CultureInfo.InvariantCulture)+L.T("倍，默认 1.0倍。"),9F,FontStyle.Regular,16,0,PaintSkin.Accent);scaleHint.AutoSize=false;
            bool arranging=false;
            EventHandler layoutParameters=(s,e)=>{
                if(arranging)return;arranging=true;parameters.SuspendLayout();
                try{
                    Point scroll=parameters.AutoScrollPosition;parameters.AutoScrollPosition=Point.Empty;
                    int width=Math.Max(1,parameters.ClientSize.Width-32),inputWidth=Math.Min(190,Math.Max(130,width/3)),y=48;
                    parameterWarning.Bounds=new Rectangle(16,8,width,36);
                    foreach(var row in rows){
                        row[0].Bounds=new Rectangle(16,y,Math.Max(1,width-inputWidth-16),28);
                        row[1].Location=new Point(16+width-inputWidth,y);row[1].Width=inputWidth;
                        row[2].Bounds=new Rectangle(16,y+30,width,20);
                        int descriptionHeight=Math.Max(32,TextRenderer.MeasureText(row[3].Text,row[3].Font,new Size(width,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding).Height+4);
                        row[3].Bounds=new Rectangle(16,y+52,width,descriptionHeight);y+=64+descriptionHeight;
                    }
                    scaleHint.Bounds=new Rectangle(16,y,width,40);parameters.AutoScrollMinSize=new Size(0,y+52);
                    parameters.AutoScrollPosition=new Point(-scroll.X,-scroll.Y);
                }finally{parameters.ResumeLayout(true);arranging=false;}
            };
            parameters.Resize+=layoutParameters;
            L.ApplyTree(dialog);layoutSwitches(null,EventArgs.Empty);layoutParameters(null,EventArgs.Empty);
            try{
                var answer=dialog.ShowDialog(this);
                foreach(var param in MarkerCatalog.Parameters){var input=edited[param.Key];if(input is NumericUpDown)parameterValues[param.Key]=((NumericUpDown)input).Value.ToString(CultureInfo.InvariantCulture);else{int index=((ComboBox)input).SelectedIndex;parameterValues[param.Key]=(param.Key=="MaxAnisotropy"?new[]{0,2,4,8,16}[index]:index).ToString(CultureInfo.InvariantCulture);}}
                if(answer==DialogResult.Yes)SaveCurrentConfiguration();
            }
            finally{markerOptionsList.SelectedIndexChanged-=updateHelp;switches.Controls.Remove(markerOptionsList);footer.Controls.Remove(ignoreErrorsBox);}
        }
    }
}
