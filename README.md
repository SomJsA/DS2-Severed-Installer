# DS2 Severed Installer

当前版本：**2026.11.6.19** · [下载 / Releases](https://github.com/SomJsA/DS2-Severed-Installer/releases/tag/v2026.11.6.19)

死亡空间贴吧汉化组移植&汉化。支持 Steam / EA，安装器界面支持简体中文 / English。

- 可分别选择简体中文汉化、Severed DLC、MarkerPatch。安装顺序为 MarkerPatch → 原始英文 DLC → 所选汉化。只装 DLC 时保留英文／启动器语言。
- 标题使用 MStiffHei，剧情使用 Swei Shear Sans CJK SC Regular；保留已确认的字形、中文零额外字距、空格步进 3、中英数字距和标点修正。移除 21 处短操作提示的内部空格，修正“返回、确认、继续、取消、选择、启动”等提示换行。
- 保留原 DLC 核心 `SeveredPC.asi` 与官方 MarkerPatch 3.0.7。能量节点、存档显示、矿区初始化分别使用三个独立 ASI；升级会迁移旧合并 Extras，避免重复加载。
- DLC 存档按章节显示名称和图片，覆盖主菜单与局内全息保存页。中文随汉化启用，英文为 `Severed, chapter 1 / chapter 2`。只改显示，不转换或写入存档。
- 矿区修正针对已复现的“操作一个面板后死亡读档，三个面板全锁”。仅修正指定共用初始化对象，仍需分别完成三个面板，不能重复操作同一面板刷进度。
- 保留既有设置、节点偏好、原始备份与存档。安装记录 schema 15 支持独立插件的校验、迁移、回滚和卸载；未知或已修改文件会保留并提示。
- 更新检查显示累计日志并打开发布网页，不自动下载／替换程序。安装完成后，在“新游戏”中选择 Severed 进入 DLC。

继续使用原 **Severed.marker**，本版不重发完整 DLC 数据包。程序包、纯源码和许可证分别提供；仓库及源码包不含游戏 DAT、汉化资源、私有字体、美术素材、存档或第三方二进制。

能量节点修正仍依赖 `EnableHackerDLC`，会开放本体黑客装备，不自动开启 Ignition 房间。MarkerPatch 选项位置、默认值和依赖保持；字幕默认 1.0 倍。存档图片资源由安装器管理的 `DS2DAT3.DAT` 提供。

DLC 存档需要 DLC 及运行组件；停用、卸载或回到原版后，请重新启用 DLC 再读取这些存档。请用新版安装器执行升级或更改安装，保留原始备份和自定义配置。

已通过 472 项覆盖原生钩子、存档识别、资源重建、安装审计、升级、回滚、卸载、界面及更新兼容的定向检查；Steam 实机确认主菜单与局内保存页的两章名称／图片、字体角色和多页短提示。正常退出后四份存档与游戏设置逐字节保持。详见 [验收范围](installer/VALIDATION-2026.11.6.19.md)。Steam 实机与隔离测试不能替代 EA 平台、所有检查点和连续多次死亡的完整回归。上游提案保留 Draft：[节点 #9](https://github.com/Tubzbuster/DS2SeveredPC/pull/9)、[存档显示 #11](https://github.com/Tubzbuster/DS2SeveredPC/pull/11)、[矿区 #13](https://github.com/Tubzbuster/DS2SeveredPC/pull/13)。

## English

A bilingual installer for Steam and EA: Simplified Chinese localization, original Severed DLC and MarkerPatch. DLC-only installations retain English/launcher language. Reuse the existing `Severed.marker`.

2026.11.6.19 retains the accepted title/dialogue fonts, spacing and punctuation, and removes internal spaces from 21 short action labels. The original DLC core and official MarkerPatch remain separate. Power Node, save display and mining-camp initialization each have their own optional runtime plugin; known combined Extras installations are migrated.

DLC saves receive chapter-specific titles and previews in the menu and native in-game list. Only display buffers change; saves, chapter metadata, difficulty, time and progress remain unchanged. The mining fix corrects only the exact shared initializer responsible for the reproduced post-death/reload panel lock, preserving the three distinct puzzle steps.

Settings, Power Node preferences and original backups are preserved. Ownership schema 15 covers plugin checks, migration, rollback and uninstall. Updates open the release webpage. The Power Node fix retains its `EnableHackerDLC` dependency and does not unlock Ignition rooms. Broader platform/gameplay combinations remain outside the targeted acceptance scope.

Source excludes game data, private fonts/art, saved games and compiled dependencies. See [build instructions](installer/BUILDING.md) and [independent plugin sources](plugins/README.md).

## 致谢 / Credits

当前版本基石：[Wemino/MarkerPatch](https://github.com/Wemino/MarkerPatch)、[Tubzbuster/DS2SeveredPC](https://github.com/Tubzbuster/DS2SeveredPC)。

simgroup 异版简单修复和整合：SomJs。超级校对：lxyun_2（腹黑小喵）、MisakaCharlotte（是的呀啊）。提供部分 Token：天雷本雷。AI 参与 / AI participated.

[交流群 339639634](https://qm.qq.com/q/AsZY1d8WxG) · [死亡空间贴吧](https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page)
