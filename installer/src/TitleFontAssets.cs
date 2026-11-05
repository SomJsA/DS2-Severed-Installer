using System;
using System.IO;

internal static partial class InstallerEngine
{
    // The APX frontend must explicitly select a complete FFN resource name.
    // RussellSquare is shared with native subtitles; changing its atlas alone
    // changes both roles. This is a data overlay and requires no runtime plugin.
    private static void ValidateTitleFontSource(string game)
    {
        byte[] stream=EffectiveIconEntry(game,@"frontend\frontend.str");
        byte[] table=EffectiveIconEntry(game,@"frontend\frontend.toc");
        if(stream==null || table==null || IconBytesHash(stream)!=IconStreamSha || IconBytesHash(table)!=IconTocSha)
            throw new InvalidDataException(L.T("前端界面资源版本不匹配，无法安全应用标题字体。已停止汉化安装。"));
        string other=Path.Combine(game,SaveIconsArchive);
        // A verified icon archive may have just been installed in this transaction;
        // ownership is committed only after localization finishes. Compatibility
        // depends on its exact contents here; this check never changes that file.
        if(File.Exists(other) && !KnownSaveIconsSha(Sha256(other)))
            throw new InvalidDataException(L.T("DS2DAT3.DAT 属于其他模组或已被修改，无法安全合并标题字体。已保留现有文件。"));
    }
}
