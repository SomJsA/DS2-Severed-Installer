using System;using System.IO;
internal static partial class InstallerEngine
{
    private static readonly string ExtrasRelative=Path.Combine("plugins","SeveredExtras.asi");
    private const string ExtrasSha="e9adf81e28a3010ed7755927a5e41f56a6ac30e445f7525a5bebf2e8aa42b0a3";
    private static bool OwnsExtras(string game){return ReadState(game,"owns_extras_runtime")=="True";}
    private static string ExtrasHashForUninstall(string game)
    {
        string recorded=ReadState(game,"extras_runtime_sha256");
        if(recorded!=ExtrasSha)throw new InvalidDataException(L.T("独立修复插件的安装记录不受支持，已保留现有文件。"));
        return recorded;
    }
}
