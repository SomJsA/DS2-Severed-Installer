using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static class L
{
    private static string language = "zh-CN";
    private static readonly Dictionary<string, string> english = LoadEnglish();
    private static readonly Dictionary<string, string> sources = Reverse();
    private sealed class BoundText { public string Source; public BoundText(string source){Source=source;} }
    private sealed class LiteralText { }
    public static string Language { get { return language; } }
    public static bool IsEnglish { get { return language == "en"; } }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)]
    private static extern bool GetUserPreferredUILanguages(uint flags,out uint languages,[Out] char[] buffer,ref uint characters);
    public static string[] UserPreferredLanguages()
    {
        try
        {
            uint count,size=0;
            if(!GetUserPreferredUILanguages(8,out count,null,ref size)||size==0||size>16384)return new string[0];
            var buffer=new char[size];
            if(!GetUserPreferredUILanguages(8,out count,buffer,ref size))return new string[0];
            return new string(buffer).Split(new[]{'\0'},StringSplitOptions.RemoveEmptyEntries);
        }
        catch(DllNotFoundException){return new string[0];}
        catch(EntryPointNotFoundException){return new string[0];}
    }
    public static string ChooseSupported(string[] preferences)
    {
        foreach(string value in preferences)
        {
            if(value.StartsWith("zh",StringComparison.OrdinalIgnoreCase))return "zh-CN";
            if(value.StartsWith("en",StringComparison.OrdinalIgnoreCase))return "en";
        }
        return null;
    }
    private static Dictionary<string,string> LoadEnglish()
    {
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("DS2_LANG_EN"))
        {
            if(stream==null)throw new InvalidDataException("Missing installer language catalog.");
            using(var reader=new StreamReader(stream))
                return new JavaScriptSerializer{MaxJsonLength=1024*1024}.Deserialize<Dictionary<string,string>>(reader.ReadToEnd());
        }
    }
    private static Dictionary<string,string> Reverse()
    {
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var item in english)if(!result.ContainsKey(item.Value))result[item.Value]=item.Key;
        return result;
    }
    public static void Initialize(string requested)
    {
        string detected=ChooseSupported(UserPreferredLanguages())??ChooseSupported(new[]{CultureInfo.CurrentUICulture.Name,CultureInfo.InstalledUICulture.Name})??"en";
        SetLanguage(string.IsNullOrEmpty(requested)?detected:requested);
    }
    public static void SetLanguage(string requested)
    {
        language=requested!=null&&requested.StartsWith("zh",StringComparison.OrdinalIgnoreCase)?"zh-CN":"en";
        Thread.CurrentThread.CurrentUICulture=CultureInfo.GetCultureInfo(IsEnglish?"en-US":"zh-CN");
    }
    public static string T(string source)
    {
        if(source==null)return "";
        string translated;
        return IsEnglish&&english.TryGetValue(source,out translated)?translated:source;
    }
    public static void KeepLiteral(Control control){control.Tag=new LiteralText();}
    public static void ApplyTree(Control control)
    {
        if(!(control.Tag is LiteralText))
        {
            var binding=control.Tag as BoundText;
            if(binding==null)
            {
                string source=control.Text,original;
                if(sources.TryGetValue(source,out original))source=original;
                if(english.ContainsKey(source)){binding=new BoundText(source);control.Tag=binding;}
            }
            if(binding!=null)control.Text=T(binding.Source);
        }
        foreach(Control child in control.Controls)ApplyTree(child);
        control.Invalidate();
    }
}
