using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

internal static partial class InstallerEngine
{
    private const string SaveIconsArchive="DS2DAT3.DAT";
    private const string SaveIconsSha="adc7195c9a32a14fa8087f07d870d271e14aa039ba947db141d17fdebcaaf1b6";
    private const string IconStreamSha="7dc56fe9483c09a4645cfa3172a48298c0525b8b851e7adb753c47186eeec114";
    private const string IconTocSha="852cb55463f539c9e635d2dac40f403ed09302a9577e384d528c180371eae7d8";
    private static uint IconNameHash(string value)
    { uint h=0;foreach(byte b in Encoding.ASCII.GetBytes(value.ToLowerInvariant().Replace('/','\\')))h=unchecked(h*65599+b);return h; }
    private static string IconBytesHash(byte[] value)
    { using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","").ToLowerInvariant(); }
    private static byte[] EffectiveIconEntry(string game,string name)
    {
        uint key=IconNameHash(name);
        var archives=Directory.GetFiles(game,"*",SearchOption.TopDirectoryOnly).Select(path=>new {Path=path, Match=System.Text.RegularExpressions.Regex.Match(Path.GetFileName(path),@"^DS2DAT(\d+)\.DAT$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)})
            .Where(x=>x.Match.Success).OrderBy(x=>int.Parse(x.Match.Groups[1].Value));
        foreach(var archive in archives)
        {
            if(string.Equals(Path.GetFileName(archive.Path),SaveIconsArchive,StringComparison.OrdinalIgnoreCase))continue;
            using(var input=File.OpenRead(archive.Path))
            {
                byte[] h=ReadExact(input,16);if(Encoding.ASCII.GetString(h,0,4)!="BIGH")throw new InvalidDataException("Invalid BIGH icon source");
                uint count=ReadBe32(h,8),header=ReadBe32(h,12);
                if(count>1000000||header<16L+12L*count||header>input.Length)throw new InvalidDataException("Invalid icon source index");
                for(uint n=0;n<count;++n)
                {
                    byte[] row=ReadExact(input,12);if(ReadBe32(row,8)!=key)continue;
                    uint offset=ReadBe32(row,0),size=ReadBe32(row,4);
                    if(offset<header||size>64*1024*1024||offset+(long)size>input.Length)throw new InvalidDataException("Invalid icon source bounds");
                    input.Position=offset;return ReadExact(input,checked((int)size));
                }
            }
        }
        return null;
    }
    private static string PrepareSaveIcons(string game,string temp,Action<string> log)
    {
        AssertPlain(game,SaveIconsArchive);
        string destination=Path.Combine(game,SaveIconsArchive);
        bool own=ReadState(game,"owns_save_icons")=="True";
        if(own&&!KnownSaveIconsSha(ReadState(game,"save_icons_sha256")))throw new InvalidDataException(L.T("存档图标安装记录不受支持，已保留现有文件。"));
        if(File.Exists(destination)&&(!own||Sha256(destination)!=ReadState(game,"save_icons_sha256")))throw new IOException(L.T("DS2DAT3.DAT 属于其他模组或已被修改，已停止覆盖。请保留该文件并核对兼容性。"));
        byte[] original=EffectiveIconEntry(game,@"frontend\frontend.str"),table=EffectiveIconEntry(game,@"frontend\frontend.toc");
        if(original==null||table==null||IconBytesHash(original)!=IconStreamSha||IconBytesHash(table)!=IconTocSha)
        {
            string note=L.T("基础图片资源版本未匹配，已保留原图片；本次只启用存档名称候选。读取 DLC 存档仍需要启用 DLC。");
            LastInstallNote+=(string.IsNullOrEmpty(LastInstallNote)?"":"\r\n")+note;log(note);return null;
        }
        byte[] tail=ReadResource("DS2_SAVE_ICON_TAIL"),toc=ReadResource("DS2_SAVE_ICON_TOC");
        byte[] native=EffectiveIconEntry(game,@"global_assets\global_assets.str"),nativeToc=EffectiveIconEntry(game,@"global_assets\global_assets.toc");
        if(native==null||nativeToc==null||IconBytesHash(native)!="5645b0a0358966b1966bb066ba90a337fbeb539be4fbaf4450cc5d47171982dd"||IconBytesHash(nativeToc)!="fc4b37b2abda391c47c1835cd303cf19f521ead40f8368d3d122386aa24d3afc")
        {log(L.T("基础图片资源版本未匹配，已保留原图片；本次只启用存档名称候选。读取 DLC 存档仍需要启用 DLC。"));return null;}
        string stream=Path.Combine(temp,"SaveIcons.str"),tocPath=Path.Combine(temp,"SaveIcons.toc"),empty=Path.Combine(temp,"SaveIcons.empty.dat"),target=Path.Combine(temp,"SaveIcons.dat");
        File.WriteAllBytes(stream, ApplySavePreviewDelta(original,tail));
        File.WriteAllBytes(tocPath,toc);
        string nativeStream=Path.Combine(temp,"NativeSaveIcons.str"),nativeTable=Path.Combine(temp,"NativeSaveIcons.toc");
        File.WriteAllBytes(nativeStream,ApplyIconDelta(native,ReadResource("DS2_NATIVE_ICON_TAIL"),"5c3ab219ab76d0a22f404dc0782eaca41493effd60ea8556acf6f57681c06e30"));
        File.WriteAllBytes(nativeTable,ReadResource("DS2_NATIVE_ICON_TOC"));
        byte[] head=new byte[24];Encoding.ASCII.GetBytes("BIGH").CopyTo(head,0);WriteLe32(head,4,2048);WriteBe32(head,12,24);File.WriteAllBytes(empty,head);
        RebuildBigh(empty,target,new Dictionary<uint,string>{{IconNameHash(@"frontend\frontend.str"),stream},{IconNameHash(@"frontend\frontend.toc"),tocPath},{IconNameHash(@"global_assets\global_assets.str"),nativeStream},{IconNameHash(@"global_assets\global_assets.toc"),nativeTable}});
        if(Sha256(target)!=SaveIconsSha)throw new InvalidDataException(L.T("存档图标资源校验失败，已停止安装。"));
        return target;
    }
    private static bool KnownSaveIconsSha(string hash)
    { return hash==SaveIconsSha || hash=="c796ae4b87664fb4064c845fcf32cdec59cd8bd7221913400e70f0fcc70052f9" || hash=="5f68655c4c0e5e091c535a134a3d28366710048c58971f4b430a6858b9fc299b" || hash=="e6dd41cd413f962bb35376e4f4387fd562746847a1b32274f11052edda8698a1" || hash=="3e237c3d9650c39920881b6717939e47c75f0cacc99f60b1390b3769399e6db8" || hash=="b9900b89f2a2e9b18ba900f47c6aa4d78ca473ff10eb088883b1ed6c17cf1b93"; }
    private static byte[] ApplySavePreviewDelta(byte[] original,byte[] recipe)
    {return ApplyIconDelta(original,recipe,"dc3f5e03dd753142d2a63e1f983bfbb1f0f73c57c62aee998f9ed085ad8ee562");}
    private static byte[] ApplyIconDelta(byte[] original,byte[] recipe,string expectedHash)
    {
        using(var input=new BinaryReader(new MemoryStream(recipe,false)))
        using(var output=new MemoryStream())
        {
            if(Encoding.ASCII.GetString(input.ReadBytes(8))!="DS2APX1\0" || input.ReadUInt32()!=original.Length)
                throw new InvalidDataException("Invalid save preview patch header");
            uint expected=input.ReadUInt32();if(expected>64*1024*1024)throw new InvalidDataException("Save preview size exceeds limit");
            bool complete=false;
            for(int instruction=0;instruction<100000 && input.BaseStream.Position<input.BaseStream.Length;++instruction)
            {
                byte tag=input.ReadByte();if(tag==255){complete=true;break;}
                if(tag==0)
                {
                    uint offset=input.ReadUInt32(),length=input.ReadUInt32();
                    if(offset+(long)length>original.Length || output.Length+length>expected)throw new InvalidDataException("Save preview copy out of bounds");
                    output.Write(original,checked((int)offset),checked((int)length));
                }
                else if(tag==1)
                {
                    uint length=input.ReadUInt32();if(length>input.BaseStream.Length-input.BaseStream.Position || output.Length+length>expected)throw new InvalidDataException("Save preview insert out of bounds");
                    byte[] bytes=input.ReadBytes(checked((int)length));output.Write(bytes,0,bytes.Length);
                }
                else throw new InvalidDataException("Unknown save preview instruction");
            }
            byte[] result=output.ToArray();
            if(!complete || input.BaseStream.Position!=input.BaseStream.Length || result.Length!=expected || IconBytesHash(result)!=expectedHash)
                throw new InvalidDataException("Save preview patch verification failed");
            return result;
        }
    }
    private static bool OwnsSaveIcons(string game)
    {return ReadState(game,"owns_save_icons")=="True";}
}
