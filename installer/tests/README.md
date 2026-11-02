# 更新检查兼容性检查

使用 .NET Framework 的 x86 csc 编译 UpdateCompatibilityCheck.cs。运行：

    UpdateCompatibilityCheck.exe <installer.exe> releases-fixture.json <expected embedded release>

已分发基线使用 expected release 2026.11.2；新版使用 2026.11.2.1。分别启动进程，避免同名程序集的加载上下文混淆。每份 EXE 检查四段版本、新旧日志合并、草稿/预发布过滤、无重复通知以及无二进制下载方法，共 7 项。JSON 为模拟发布记录，不会创建或下载这些发布。2026.11.1 历史 EXE 尚无此更新检查器。
