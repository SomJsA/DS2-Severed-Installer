using System;

internal static class UninstallSupport
{
    public const string RepairAdvice =
        "请先关闭游戏并检查文件权限。仍无法完成时，建议备份存档后修复游戏：\r\n" +
        "Steam：库 → 死亡空间 2 → 属性 → 已安装文件 → 验证游戏文件完整性。\r\n" +
        "EA app：库 → 死亡空间 2 → 管理 → 修复。\r\n\r\n" +
        "平台修复可能保留第三方插件和 DLC 文件；请根据日志检查残留，并保留原备份与安装记录。不要删除存档。";

    public static string FailureMessage(Exception error)
    {
        return "卸载未完成：" + error.Message + "\r\n\r\n" + RepairAdvice;
    }
}
