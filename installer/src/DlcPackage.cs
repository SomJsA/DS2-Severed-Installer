using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

internal sealed class DlcPackage
{
    public const int SupportedProtocol = 1;
    public string Version;
    public string Variant;
    public string PayloadSha256;
    public long PayloadBytes;
    public string PosterSha256;
    public long PosterBytes;
    public bool Historical;
    public string MinimumInstaller;
    public string Summary { get { return (Historical ? "历史版本 " : "") + "DLC " + Version + " / " + Variant + " / 英文 / 协议 1"; } }

    private static object Value(Dictionary<string, object> map, string key, bool optional)
    {
        object value;
        if (!map.TryGetValue(key, out value) || value == null)
        {
            if (optional) return null;
            throw new InvalidDataException("DLC.version.json 缺少 " + key + "。");
        }
        return value;
    }
    private static string Text(Dictionary<string, object> map, string key, bool optional = false)
    {
        object value = Value(map, key, optional);
        if (value == null) return null;
        string result = value as string;
        if (result == null || result.Length == 0 || result.Length > 512)
            throw new InvalidDataException("数据包字段无效：" + key);
        return result;
    }
    private static long Number(Dictionary<string, object> map, string key)
    {
        object value = Value(map, key, false);
        if (!(value is int) && !(value is long) && !(value is decimal))
            throw new InvalidDataException("数据包数值字段无效：" + key);
        if (value is decimal && (decimal)value != decimal.Truncate((decimal)value))
            throw new InvalidDataException("数据包数值字段必须是整数：" + key);
        try { return Convert.ToInt64(value, CultureInfo.InvariantCulture); }
        catch (Exception) { throw new InvalidDataException("数据包数值越界：" + key); }
    }
    private static string Hash(Dictionary<string, object> map, string key)
    {
        string value = Text(map, key).ToLowerInvariant();
        if (!Regex.IsMatch(value, "^[0-9a-f]{64}$")) throw new InvalidDataException("数据包 SHA-256 格式无效：" + key);
        return value;
    }
    public static DlcPackage Parse(string json)
    {
        if (json == null || json.Length > 65536) throw new InvalidDataException("数据包元数据过大。");
        var parser = new JavaScriptSerializer { MaxJsonLength = 65536, RecursionLimit = 16 };
        Dictionary<string, object> data;
        try { data = parser.DeserializeObject(json) as Dictionary<string, object>; }
        catch (Exception ex) { throw new InvalidDataException("DLC.version.json 无法解析。", ex); }
        if (data == null || Number(data, "schema_version") != 1)
            throw new InvalidDataException("数据包元数据结构版本不受支持。");
        if (Text(data, "package_id") != "ds2-severed-base-en" || Text(data, "language") != "en" ||
            !(Value(data, "localized", false) is bool) || (bool)Value(data, "localized", false) ||
            Text(data, "payload_file") != "DS2DAT2.DAT")
            throw new InvalidDataException("需要原始英文 DLC 包，不能把汉化包当基座。");
        bool historical = !data.ContainsKey("protocol_version");
        if (!historical && Number(data, "protocol_version") != SupportedProtocol)
            throw new InvalidDataException("此 DLC 数据包需要更新的安装器协议。");
        string minimum = Text(data, "minimum_installer_version", true) ?? "1.0.0.0";
        System.Version minVersion;
        if (!System.Version.TryParse(minimum, out minVersion) || minVersion > Assembly.GetExecutingAssembly().GetName().Version)
            throw new InvalidDataException("DLC 数据包要求安装器版本至少为 " + minimum + "，请更新安装器。");
        string packageVersion = Text(data, "package_version");
        System.Version numericVersion;
        if (!System.Version.TryParse(packageVersion, out numericVersion))
            throw new InvalidDataException("DLC 数据包版本号无效。");
        var info = new DlcPackage { Version = packageVersion, Variant = Text(data, "variant"),
            PayloadSha256 = Hash(data, "payload_sha256"), PayloadBytes = Number(data, "payload_bytes"),
            Historical = historical, MinimumInstaller = minimum };
        if (!Regex.IsMatch(info.Variant, "^[a-z0-9][a-z0-9._-]{0,63}$"))
            throw new InvalidDataException("DLC 内容版本标记无效。");
        if (info.PayloadBytes < 16 || info.PayloadBytes > uint.MaxValue)
            throw new InvalidDataException("DLC 数据长度不受支持。");
        if (historical && info.PayloadSha256 != "81a145da647660d34a6fcb15ea3a8709c74855cf1beef23fc668a3ae42eb6ea0")
            throw new InvalidDataException("未知的历史 DLC 数据，缺少可兼容协议。");
        object posterValue = Value(data, "poster", true);
        if (posterValue != null)
        {
            var poster = posterValue as Dictionary<string, object>;
            if (poster == null || Text(poster, "file") != "Severed.poster.jpg")
                throw new InvalidDataException("DLC 海报文件名无效。");
            info.PosterBytes = Number(poster, "bytes"); info.PosterSha256 = Hash(poster, "sha256");
            if (info.PosterBytes < 1 || info.PosterBytes > 8 * 1024 * 1024)
                throw new InvalidDataException("DLC 海报大小无效。");
        }
        else if (!historical) throw new InvalidDataException("此协议数据包缺少海报及其校验信息。");
        return info;
    }
}
