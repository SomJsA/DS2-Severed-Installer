using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Windows.Forms;

internal sealed class WebOpenResult
{
    public string Url, Error;
    public bool Requested;
}
internal static class WebLinks
{
    private static bool Allowed(string value,out Uri uri)
    {
        uri=null;
        if(string.IsNullOrWhiteSpace(value))return false;
        foreach(char c in value)if(char.IsControl(c)||c=='"')return false;
        return Uri.TryCreate(value,UriKind.Absolute,out uri) && uri.Scheme==Uri.UriSchemeHttps && uri.Port==443 && uri.UserInfo.Length==0 &&
            (uri.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase)||uri.Host.Equals("qm.qq.com",StringComparison.OrdinalIgnoreCase)||uri.Host.Equals("tieba.baidu.com",StringComparison.OrdinalIgnoreCase));
    }
    internal static string Normalize(string url,string fallback)
    {
        Uri uri;
        if(Allowed(url,out uri))return uri.AbsoluteUri;
        if(Allowed(fallback,out uri))return uri.AbsoluteUri;
        throw new ArgumentException("Invalid project web address.");
    }
    private static bool ExpectedFailure(Exception ex)
    {
        return ex is Win32Exception || ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is SecurityException || ex is System.Runtime.InteropServices.COMException;
    }
    private static void Launch(ProcessStartInfo info)
    {
        using(Process process=Process.Start(info)) { }
    }
    internal static WebOpenResult OpenUsing(string url,Action<ProcessStartInfo> launch)
    {
        var result=new WebOpenResult { Url=Normalize(url,ProductInfo.OwnReleasesPage) };
        try
        {
            launch(new ProcessStartInfo(result.Url) { UseShellExecute=true,Verb="open",ErrorDialog=false });
            result.Requested=true;return result;
        }
        catch(Exception ex) { if(!ExpectedFailure(ex))throw;result.Error=ex.Message; }
        // Use Explorer only as a second request path; never change browser
        // associations, invoke a command shell, or assume a browser was shown.
        try
        {
            string explorer=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe");
            launch(new ProcessStartInfo(explorer,"\""+result.Url+"\"") { UseShellExecute=false,CreateNoWindow=true,ErrorDialog=false });
            result.Requested=true;
        }
        catch(Exception ex) { if(!ExpectedFailure(ex))throw;result.Error+="\r\n"+ex.Message; }
        return result;
    }
    internal static void CopyAddress(TextBox address,Label status)
    {
        try { Clipboard.SetText(address.Text);status.Text=L.T("链接已复制。"); }
        catch(System.Runtime.InteropServices.ExternalException) { address.Focus();address.SelectAll();status.Text=L.T("复制失败，请选中地址后手动复制。"); }
    }
    internal static Form ManualDialog(WebOpenResult result)
    {
        var form=new Form { Text=L.T("手动打开网页"),ClientSize=new Size(660,245),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false };
        var note=new Label { Text=L.T("系统未能打开浏览器。请复制下面的地址，粘贴到浏览器中打开；关闭此窗口后可继续使用。"),Location=new Point(18,16),Size=new Size(624,64) };form.Controls.Add(note);
        var address=new TextBox { ReadOnly=true,Multiline=true,Text=result.Url,Location=new Point(18,90),Size=new Size(624,66),ScrollBars=ScrollBars.Vertical };L.KeepLiteral(address);form.Controls.Add(address);
        var status=new Label { Location=new Point(18,168),Size=new Size(624,24) };form.Controls.Add(status);
        var copy=new Button { Text=L.T("复制链接"),Location=new Point(18,202),Size=new Size(170,30) };copy.Click+=(s,e)=>CopyAddress(address,status);form.Controls.Add(copy);
        var close=new Button { Text=L.T("继续使用"),Location=new Point(472,202),Size=new Size(170,30),DialogResult=DialogResult.Cancel };form.Controls.Add(close);form.CancelButton=close;
        L.ApplyTree(form);return form;
    }
    public static void Show(IWin32Window owner,string url)
    {
        var result=OpenUsing(url,Launch);
        if(!result.Requested)using(var form=ManualDialog(result))form.ShowDialog(owner);
    }
}
