# 更新检查兼容性检查

使用 .NET Framework 的 x86 csc 编译 UpdateCompatibilityCheck.cs。运行：

    UpdateCompatibilityCheck.exe <installer.exe> releases-fixture.json <expected embedded release>

已分发基线使用 expected release 2026.11.2；上个发布版使用 2026.11.2.1；当前新版使用 2026.11.3。分别启动进程，避免同名程序集的加载上下文混淆。每份 EXE 检查新版本识别、新旧日志合并、草稿/预发布过滤、无重复通知以及无二进制下载方法，共 7 项。JSON 为模拟发布记录，不会创建或下载这些发布。2026.11.1 历史 EXE 尚无此更新检查器。

MarkerReviewChecks.cs 为首装功能页检查：编译时引用 System.Windows.Forms.dll、System.Drawing.dll、System.Core.dll，使用 x86。运行 `MarkerReviewChecks.exe <installer.exe> <新的空测试输出路径> zh-CN` 或 en。只用虚拟游戏目录检查三秒确认、取消/重试、同目标缓存、选项保留及 INI 预设，不执行实际安装。每种语言 48 项，还验证全部可选项取消后能继续，输出截图用于布局核对。
