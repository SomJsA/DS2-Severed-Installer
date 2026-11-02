# 更新检查兼容性检查

使用 .NET Framework 的 x86 csc 编译 UpdateCompatibilityCheck.cs。运行：

    UpdateCompatibilityCheck.exe <installer.exe> releases-fixture.json <expected embedded release>

已分发基线使用 expected release 2026.11.2；上个发布版使用 2026.11.2.1；当前新版使用 2026.11.3。分别启动进程，避免同名程序集的加载上下文混淆。每份 EXE 检查新版本识别、新旧日志合并、草稿/预发布过滤、无重复通知以及无二进制下载方法，共 7 项。JSON 为模拟发布记录，不会创建或下载这些发布。2026.11.1 历史 EXE 尚无此更新检查器。


2026.11.3.1 已移除安装前强制功能页与倒计时。主页面简短说明预设和兼容性，详细设置按需打开。原首装审阅检查对应历史版，保存在 v2026.11.3 源码中。
