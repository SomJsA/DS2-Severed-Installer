# DS2 Severed Installer

当前维护版本：**2026.11.3**。 [GitHub 发布页](https://github.com/SomJsA/DS2-Severed-Installer/releases)。已分发的 2026.11.2／2026.11.2.1 可检查到此递增版本，确认后打开网页；节点 C2 实机仍待验证。

死亡空间贴吧汉化组移植&汉化。支持 Steam / EA 游戏目录，安装器界面支持简体中文 / English。

- 三张卡片选择简体中文汉化、Severed DLC、MarkerPatch。安装顺序为 MarkerPatch → 原始英文 DLC → 汉化；未选步骤跳过。只装 DLC 保持英文／启动器语言，勾选汉化后本体与 DLC 使用简体中文。
- DLC 自动带必需 Patch，无需另勾 MarkerPatch。节点修正为独立选项；启用时额外需要 EnableHackerDLC，会开放黑客装备。Ignition 房间是另一个独立开关。
- MarkerPatch 更新为官方 3.0.7，提供 61 项功能和 13 项数值设置，包括陀螺仪横向／纵向反转。标注“默认启用”“默认不启用”“随 DLC 自动启用”；节点依赖单独说明。字幕默认 1.0 倍。
- 首次点击安装先打开 MarkerPatch 功能页，阅读 3 秒后可继续；可按需修改或取消全部可选项。“取消并返回”不安装、不写配置，保留界面选择，再点安装仍会提示。新增六项默认修复，预设共 18 项；已有自定义值保留。
- 读取所选游戏的实际安装与配置，支持更改组件组合。保存设置只写配置，不重写 DAT；未知配置项、注释与原始备份保留。已识别旧版可以直接升级，异常时提示先备份存档，再尝试全新安装或平台修复。
- 可在游戏根目录安装／更新 DS2PatchManager.exe，调整设置或切换随包 MarkerPatch 启动状态。停用后 DLC 入口与相关运行修正不可用。外置／未知版本会提示兼容性与作者 Issue 链接。
- GitHub 更新检查合并显示版本日志。选择“是”打开发布网页，选择“否”继续使用。本程序不下载或自动替换更新。
- 安装完成后，从游戏“新游戏”中选择 Severed 进入 DLC。开场后显示简短累计日志：初版 Playable；新版新增节点购买修正与提示优化。

DLC 数据包仍使用原 Severed.marker，无需重新制作。此仓库不含 DAT、汉化数据、字体、美术素材或第三方 PE；构建所需资源另行提供。EXE 分发与源码提交按各自范围处理。

能量节点当前采用候选 2：旧候选 C1 在 EA + 官方 MarkerPatch 3.0.7 中出现商店钩子失败。候选 2 增加错误诊断及已校验 MarkerPatch 回调回退，71 项原生检查通过，仍待实机验证。此前 Steam 节点恢复显示的反馈对应旧 C1 组合，不能替代本候选验收。矿区面板、存档名称／图标、游戏内 Severed 菜单汉化仍待单独修复。

卸载会检查归属与改动，恢复可验证备份；不会覆盖存档。异常保留记录、文件和恢复点。管理器保留在根目录，关闭后可手动移除。平台验证／修复可能保留第三方文件，不保证恢复所有未知模组组合。

当前版本基石：[Wemino/MarkerPatch](https://github.com/Wemino/MarkerPatch)、[Tubzbuster/DS2SeveredPC](https://github.com/Tubzbuster/DS2SeveredPC)。
simgroup 异版简单修复和整合：SomJs。超级校对：lxyun_2（腹黑小喵）、MisakaCharlotte（是的呀啊）。提供部分 Token：天雷本雷。

[交流群 339639634](https://qm.qq.com/q/AsZY1d8WxG) · [死亡空间贴吧](https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page)

[安装器构建方法](installer/BUILDING.md) · [节点插件源码](runtime-fixes/README.md) · [保留的上游说明](README.upstream.md)

勾选或重新勾选 DLC 时默认勾选能量节点修正，仍可手动取消；读取已有安装时保留已保存的选择。

卸载默认清理本工具安装的全部组件、已确认日志、管理器版本文件和空目录；保留管理器 EXE 与 Severed.marker，恢复原先文件。不依据当前卡片勾选来缩小卸载范围。无记录时仅清理可确认的旧残留，不删除未知文件。

右上交流群旁新增 GitHub 发布页按钮；安装成功后显示 DLC 的“新游戏 → Severed”入口提示，确认后关闭安装器主窗口。取消或安装失败时保留窗口。
