using System;using System.IO;using System.Linq;using System.Collections.Generic;
internal static partial class InstallerEngine
{
    private const string NodePluginSha="b4433be75687bef192914bc39e8a2c0ac31e3115d098a4c153f25ae722dfc55a";
    private const string SavePluginSha="add55bbb72932bf70ff177fb71e9bba8358790f5e2c0b8208a1f50cf97014f35";
    private const string MiningPluginSha="114042713c525ad46485d5e02b3e52b705f7c5e0dc08b7e94159c0043e62e944";
    private static readonly string[] BugPluginNames={"SeveredPowerNodeFix","SeveredSaveDisplay","SeveredMiningCampFix"};
    private static readonly string[] BugConfigNames={"SeveredNodeFix","SeveredSaveDisplay","SeveredMiningCampFix"};
    private static readonly string[] BugPluginHashes={NodePluginSha,SavePluginSha,MiningPluginSha};
    private static readonly string[] BugResourceNames={"DS2_NODE_ASI","DS2_SAVE_ASI","DS2_MINING_ASI"};
    private static bool OwnsBugPlugins(string game){return ReadState(game,"owns_bug_plugins")=="True";}
    private static IEnumerable<string> BugPluginPaths()
    {return BugPluginNames.Select(n=>Path.Combine("plugins",n+".asi")).Concat(BugConfigNames.Select(n=>Path.Combine("plugins",n+".ini")));}
    private static string BugPluginRecordedHash(string game,int index)
    {
        string value=ReadState(game,"bug_plugin_"+index+"_sha256");
        if(value!=BugPluginHashes[index] && !(index==1 && (value=="af89f4216bbb1df0c277c2e22abc040107303a91ed42cc294372148ab92eaf16" || value=="3bb7abe1c8ebf4dce38030899251dc5fa6ab2a9a4f200e8511ca033530d3aae0" || value=="906840c9edeb139dfaea8b7d5d460700281e75ac8d5f9805dc7056667a934d66")))throw new InvalidDataException(L.T("独立修复插件的安装记录不受支持，已保留现有文件。"));
        return value;
    }
    private static bool OwnsBugPath(string game,string relative,string actual)
    {
        if(!OwnsBugPlugins(game))return false;
        for(int n=0;n<3;++n)
        {
            if(relative==Path.Combine("plugins",BugConfigNames[n]+".ini"))return true;
            if(relative==Path.Combine("plugins",BugPluginNames[n]+".asi"))return actual==BugPluginRecordedHash(game,n);
        }
        return false;
    }
    private static bool BugPluginsReady(string game)
    {
        return OwnsBugPlugins(game)&&Enumerable.Range(0,3).All(n=>File.Exists(Path.Combine(game,"plugins",BugPluginNames[n]+".asi"))&&Sha256(Path.Combine(game,"plugins",BugPluginNames[n]+".asi"))==BugPluginHashes[n]);
    }
    private static void InstallBugPlugins(string game,List<Action> undo,Action<string> log)
    {
        if(File.Exists(Path.Combine(game,ExtrasRelative)))throw new InvalidOperationException(L.T("检测到旧合并修复插件，请使用更改安装完成迁移。"));
        for(int n=0;n<3;++n)InstallOwnedBytes(game,Path.Combine("plugins",BugPluginNames[n]+".asi"),ReadResource(BugResourceNames[n]),BugPluginHashes[n],undo,log);
    }
    private static void WriteBugConfiguration(string game,bool node,bool chinese,List<Action> undo,Action<string> log)
    {
        string[] texts={"[Shop]\r\nEnablePowerNodeShopFix="+(node?"1":"0")+"\r\n",
            "[SaveDisplay]\r\nTitleFix=1\r\nIconFix=1\r\nTraceRows=0\r\n[Localization]\r\nSimplifiedChinese="+(chinese?"1":"0")+"\r\n",
            "[MiningCamp]\r\nInitializationFix=1\r\n"};
        for(int n=0;n<3;++n)InstallOwnedText(game,Path.Combine("plugins",BugConfigNames[n]+".ini"),texts[n],null,undo,log);
    }
    private static string BugPluginState(string game,bool installing)
    {
        bool own=installing||OwnsBugPlugins(game);
        return "owns_bug_plugins="+own+"\r\n"+string.Join("",Enumerable.Range(0,3).Select(n=>"bug_plugin_"+n+"_sha256="+(own?BugPluginHashes[n]:"")+"\r\n"));
    }
    private static void ValidateBugPlugins(string game,Dictionary<string,string> owned)
    {
        if(!OwnsBugPlugins(game))return;
        for(int n=0;n<3;++n){owned[Path.Combine("plugins",BugPluginNames[n]+".asi")]=BugPluginRecordedHash(game,n);owned[Path.Combine("plugins",BugConfigNames[n]+".ini")]=null;}
    }
    private static void RemoveBugPlugins(string game,Action<string> log)
    {
        if(!OwnsBugPlugins(game))return;
        for(int n=0;n<3;++n){RestoreOrRemoveOwned(game,Path.Combine("plugins",BugPluginNames[n]+".asi"),BugPluginRecordedHash(game,n),log);RestoreOrRemoveOwned(game,Path.Combine("plugins",BugConfigNames[n]+".ini"),null,log);}
    }
}
