# DS2 Severed Installer

当前版本：**2026.11.3.9** · [下载 / Releases](https://github.com/SomJsA/DS2-Severed-Installer/releases/tag/v2026.11.3.9)

死亡空间贴吧汉化组移植&汉化。支持 Steam / EA，安装器界面支持简体中文 / English。

- 三项选择：简体中文汉化、Severed DLC、MarkerPatch。安装顺序为 MarkerPatch → 原始英文 DLC → 汉化；未选步骤跳过。仅 DLC 保持英文／启动器语言，选择汉化后本体与 DLC 使用简体中文。
- MarkerPatch 的位置、勾选、默认值和依赖不变；字幕默认 1.0 倍。仅 DLC 自动安装最小必需组件，其余增强仍按选项启用。
- 能量节点修正与 DLC 存档显示修复现由独立的 `plugins/SeveredExtras.asi` 提供，MarkerPatch 官方 3.0.7 保持原文件。汉化仍使用原有数据补丁方式。
- DLC 存档按章节显示名称与两张图片；中文仅随游戏汉化启用，未安装汉化时使用 `Severed, chapter 1 / chapter 2`。这只调整显示，不转换存档内容。
- 游戏内 DLC 名称和说明可随汉化显示为“死亡空间2：阻断”“盖伯·维勒的故事。”。
- 支持已知旧版直接升级、更改安装、保存设置和根目录管理器；已有自定义值与原始备份保留。若升级异常，请先备份存档，再尝试全新安装或平台修复。
- 更新检查显示累计日志。确认后打开发布网页，不自动下载／替换程序；打不开时可复制地址手动打开。
- 安装完成后，在“新游戏”中选择 Severed 进入 DLC。

继续使用原 **Severed.marker**，本版不重发数据包。本仓库只公开程序源码与许可证，不包含 DAT、汉化资源、字体、美术素材或第三方二进制。

能量节点修正仍依赖 `EnableHackerDLC`，会开放本体黑客装备；不自动开启 Ignition 房间。独立 ASI 沿用已有修正行为，不扩大开放范围。图标使用安装器管理的 `DS2DAT3.DAT`；已有用户建议通过新版安装器更改安装，避免与旧合并插件重复加载。

矿区死亡／读档后面板无法互动的问题仍未修复。DLC 存档需要 DLC 与运行组件；停用、卸载或回到原版后，请勿读取这些存档，备份后重新启用 DLC 继续。卸载按文件归属恢复可验证备份，未知或被修改文件会保留并提示。

## English

A bilingual installer for Steam and EA: Simplified Chinese localization, original Severed DLC, and MarkerPatch. Installation order is MarkerPatch → English DLC → selected Chinese localization. DLC-only installs retain the original English/launcher language.

2026.11.3.9 separates the existing Power Node and DLC save-display fixes into `SeveredExtras.asi`, without moving controls or changing defaults/dependencies. Chinese localization remains data-based. The DLC core retains its existing episode/menu/gameplay handling. Upgrades preserve settings; updates open a webpage rather than downloading binaries. Reuse the existing `Severed.marker`.

DLC saves receive chapter-specific titles and pictures; English labels remain when Chinese localization is not selected. Display changes do not convert saves. DLC saves still require the DLC/runtime. The mine interaction issue after death/reload remains unresolved.

## 致谢 / Credits

当前版本基石：[Wemino/MarkerPatch](https://github.com/Wemino/MarkerPatch)、[Tubzbuster/DS2SeveredPC](https://github.com/Tubzbuster/DS2SeveredPC)。
simgroup 异版简单修复和整合：SomJs。超级校对：lxyun_2（腹黑小喵）、MisakaCharlotte（是的呀啊）。提供部分 Token：天雷本雷。AI 参与 / AI assisted.

[交流群 339639634](https://qm.qq.com/q/AsZY1d8WxG) · [死亡空间贴吧](https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page) · [构建方法](installer/BUILDING.md) · [独立 ASI 源码](extras/README.md) · [保留的上游说明](README.upstream.md)
