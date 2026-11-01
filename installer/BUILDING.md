# 2026.11.1 构建

Windows PowerShell 5.1+，.NET Framework 4.x C# 编译器。源码仓库不含游戏/汉化数据、美术资源或第三方二进制；构建时显式提供，冻结依赖长度与 SHA-256 见 DEPENDENCIES.json。

## 安装器

UI 资源目录需包含 `app.ico`（遗产图标）、`ChinesePatch.logo.jpg`（用户提供的 Titan Station 标志）、`button.png`、`Severed.poster.jpg`、`MarkerPatch_Logo.png`（上游 Logo）。`Chinese.assets.R8.7z` 是沿用并校验的汉化资源文件名，2026.11.1 没有更改译文。

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

本地维护树保留 XLSX/218 行导出、字体及 `tools/build_localization.py`、`tools/font_build/`，这些输入不发布到纯代码仓库。源码中的处理脚本可在提供私有输入后使用。修改译文需要重建资源、校验输出并同步程序常量与 DEPENDENCIES.json；当前 2026.11.1 仍使用已验证的 R8 汉化资源。

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
