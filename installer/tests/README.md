# 更新检查兼容性检查

使用 .NET Framework 的 x86 csc 编译 UpdateCompatibilityCheck.cs。运行：

    UpdateCompatibilityCheck.exe <installer.exe> releases-fixture.json <expected embedded release>

已分发基线使用 expected release 2026.11.2；上个发布版使用 2026.11.2.1；当前新版使用 2026.11.3。分别启动进程，避免同名程序集的加载上下文混淆。每份 EXE 检查新版本识别、新旧日志合并、草稿/预发布过滤、无重复通知以及无二进制下载方法，共 7 项。JSON 为模拟发布记录，不会创建或下载这些发布。2026.11.1 历史 EXE 尚无此更新检查器。


2026.11.3.1 已移除安装前强制功能页与倒计时。主页面简短说明预设和兼容性，详细设置按需打开。原首装审阅检查对应历史版，保存在 v2026.11.3 源码中。

MenuLocalizationChecks.cs 使用 x86 C# 编译器。参数：<候选安装器.exe> <runtime-fixes/build/menu_abi_tests.exe> <E:/DS2-Severed-Patch/work/下新的测试目录> <旧 C2 SeveredPC.asi>。验证配置跨语言传入实际 x86 AddEpisode，以及旧 C2 的升级、卸载和原始备份恢复。52 项，不操作真实游戏。

候选 2 的 MenuLocalizationChecks.cs 增加第 5 个参数：旧菜单候选 1 的 ASI 路径；共 55 项。原生 base_label_tests.cpp 验证目录精确匹配、重排、只改标题、原调用及异常指针／边界，共 20 项。

存档名称候选：MenuLocalizationChecks.cs 共 66 项，参数依次为候选 EXE、原生 menu_abi_tests.exe、新的项目 work 测试目录、旧 C2 ASI、菜单候选 1 ASI、菜单候选 2 ASI。HeadlessUiChecks.cs 的 10 项不显示窗口，适合用户正使用游戏时运行。
