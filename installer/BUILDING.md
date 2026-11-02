# 2026.11.2 构建

2026.11.2 集成 SomJs 编写的 Severed 能量节点修正，沿用用户已在 Steam 确认恢复显示的候选 1 二进制。仅在 Severed 构建商店列表时对能量节点临时绕过章节/轮次门槛，保留原价格，调用后恢复字段；默认 Profile=1，消息追踪默认关闭。

**当前依赖与影响：**安装 DLC 会确保 LoadASIPlugins、EnableSeveredDLC、EnableHackerDLC 三项开启。节点修正逻辑限定在 DLC；依赖的 EnableHackerDLC 同时会在本体开放黑客套装与黑客接触光束。这不是完全不影响本体的配置。仅汉化、仅 MarkerPatch 安装不新增该插件。

尚未修复：DLC 存档沿用本体第一章名称/图片、游戏内 Severed 名称和说明的汉化、矿区死亡后互动面板不可用，以及上游隐藏的 10 个套装。不能把本次能量节点修正当作这些问题已经解决。

源码 `runtime-fixes` 随本仓库发布（与 installer 同级）。安装/升级/卸载记录支持新旧已知插件哈希；未知后续改动会停止卸载并保留备份。Severed.marker 沿用原包，游戏数据、218 条译文和字体未改。

Windows PowerShell 5.1+，.NET Framework 4.x C# 编译器。源码仓库不含游戏/汉化数据、美术资源或第三方二进制；构建时显式提供，冻结依赖长度与 SHA-256 见 DEPENDENCIES.json。

## 安装器

UI 资源目录需包含 `app.ico`（遗产图标）、`ChinesePatch.logo.jpg`（用户提供的 Titan Station 标志）、`button.png`、`Severed.poster.jpg`、`MarkerPatch_Logo.png`（上游 Logo）。`Chinese.assets.R8.7z` 是沿用并校验的汉化资源文件名，2026.11.2 没有更改译文。

```powershell
.\tools\build_installer.ps1 `
  -MarkerPatchDll "E:\Dependencies\dinput8.dll" `
  -SeveredAsi "E:\Dependencies\SeveredPC.asi" `
  -SevenZipExe "E:\Dependencies\7z.exe" `
  -SevenZipDll "E:\Dependencies\7z.dll" `
  -ChineseAssets "E:\PrivateAssets\Chinese.assets.R8.7z" `
  -UiAssetsDir "E:\PrivateAssets\ui" `
  -MarkerIni ".\assets\MarkerPatch.ini" `
  -OutputExe "E:\Release\DS2SeveredOneClick.exe"
```

构建器校验外部依赖，不读取或绑定某个 DLC 整包哈希。ICO 同时用于 EXE 的 Win32 图标及各窗口图标。`--preview` 仅展示界面，禁止安装/卸载操作。

## 英文 DLC 包

```powershell
.\tools\make_dlc_marker.ps1 `
  -Dat2 "E:\Verified\DS2DAT2.DAT" `
  -Poster "E:\PrivateAssets\ui\Severed.poster.jpg" `
  -SevenZipExe "E:\Dependencies\7z.exe" `
  -Output "E:\NewRelease\Severed.marker"
```

默认要求已验证的 base-en-v1（3,187,759,104 字节，SHA-256 `81a145da647660d34a6fcb15ea3a8709c74855cf1beef23fc668a3ae42eb6ea0`）。生成包内 JSON 协议版本 1、包版本 1.1.0、最低安装器 1.0.9.0，并记录数据和海报长度及哈希。标准 7z 容器，无自解压程序。

更新已独立核验的英文基座时，显式提供 `-PackageVersion`、`-Variant`、`-ExpectedPayloadBytes`、`-ExpectedPayloadSha256`；必要时提高 `-MinimumInstallerVersion`。不能把中文 DAT2 标成英文。构建器使用副本固定时间戳，拒绝覆盖现有输出，保留明确的暂存路径供后续审阅清理。不同 7-Zip 版本不保证产生同一个整包哈希。

元数据结构见 `assets/DLC.version.json`（示例）。安装器验证结构和最低版本后再验证解压文件。不匹配已知汉化基座时跳过 DLC 汉化、保留英文并提示。

## 汉化重建

本地维护树保留 XLSX/218 行导出、字体及 `tools/build_localization.py`、`tools/font_build/`，这些输入不发布到纯代码仓库。源码中的处理脚本可在提供私有输入后使用。修改译文需要重建资源、校验输出并同步程序常量与 DEPENDENCIES.json；当前 2026.11.2 仍使用已验证的 R8 汉化资源。

## 命令行

```powershell
.\DS2SeveredOneClick.exe --install "E:\Games\Dead Space 2" --log "E:\Logs\full.log"
.\DS2SeveredOneClick.exe --install "E:\Games\Dead Space 2" --no-marker --no-chinese --dlc-package "E:\Data\Severed.marker"
.\DS2SeveredOneClick.exe --install "E:\Games\Dead Space 2" --no-dlc --no-chinese --subtitle-scale 1.2
.\DS2SeveredOneClick.exe --install "E:\Games\Dead Space 2" --no-marker --no-dlc
.\DS2SeveredOneClick.exe --uninstall "E:\Games\Dead Space 2" --log "E:\Logs\uninstall.log"
```

`--ignore-errors` 只能放宽基础游戏版本识别，不会绕过 DLC 元数据/解压文件的完整性校验。程序必须在完整解压后运行；临时解压目录会被阻止。

## 安装器语言

源语言为简体中文，English 文案集中在 `src/Locales/en.json`，构建脚本将其嵌入 EXE。`src/I18n.cs` 按 Windows `GetUserPreferredUILanguages` 返回顺序选择中文或英语，其他语言跳过，无法获取时回退到 UI culture 和英语。主页面 Language 按钮即时切换，不重新创建安装选项。

`--language zh-CN` / `--language en` 可显式覆盖本次启动语言，也可用于命令行安装/卸载。`--preview --language en` 仅预览英语界面。
语言扩展需要补全主界面、59 项功能/13 项参数、提示、日志和错误文案，并做版面与选项保留检查；不能只翻译按钮。

## 2026.11.2 维护与 MarkerPatch 3.0.7

构建时增加 `-MarkerAssets "E:\Dependencies\MarkerPatch.3.0.7.zip"`，输入官方 3.0.7 原 ZIP（1,191,931 字节、SHA-256 95781f153738dbc7a67d014476fea730195933812e36e746051279a0218b62e9）。安装器只从中安装清单指定的 64 个成就图标／文本，记录逐文件归属，保留安装前同版本文件与外置素材备份。文件被外部修改时拒绝覆盖／卸载。

MarkerPatch DLL 使用官方 3.0.7 的 1,718,784 字节、SHA-256 30af0b5ccf50a34d9303d99c04d899c6f6368e522f509ca9d077586542622b55。此前随包的 c6923b5e… 是干净 3.0.6 源码的本地构建；已知旧 DLL 可直接迁移，不能将它的哈希描述为官方 3.0.6 发布附件。

新增维护源码 InstallMaintenance.cs、MarkerIni.cs、MarkerAssets.cs、MaintenanceForm.cs、UpdateChecker.cs，仍由构建器自动收集 src/*.cs。新增资源清单 assets/MarkerAssets.version.json；资源 ZIP、PE 和图片不提交到仓库。

CLI `--change` 与 `--install` 按目标组件调整安装，`--node-fix`／`--no-node-fix` 明确控制节点选项，`--keep-manager` 安装／更新根目录管理器。未显式指定节点选项时保留已有偏好；GUI 支持直接读取、保存配置与启动控制。`--game-dir` 为 GUI 预选目录。更新检查只读取 GitHub API／公开订阅和版本日志，用户同意后打开 HTTPS 发布网页，不下载程序。

安装记录 schema 11 区分选中的可选组件与物理运行依赖，保存用户开关、参数、节点偏好及可恢复的 INI。大组件变更先生成校验恢复点，失败时恢复原文件；设置保存使用小范围事务。版本迁移、未知文件拒绝覆盖与卸载检查不能移除，也不能把隔离测试写成实机验收。

本地候选 2 使用 runtime-fixes 对应源码构建的 ASI（711680 字节，SHA256 cbc5d2a6a0eb28353e2af8c833469cd238de74a3535906501e77f235eafea475）。UninstallCleanup.cs 清理有签名内容的运行日志、管理器版本文件及空目录；未知文件保留。原生检查 71 项，游戏内验证仍待完成。
