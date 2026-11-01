# 死亡空间贴吧汉化组移植&汉化 2026.11.1

完整解压分发 ZIP，把 `DS2SeveredOneClick.exe` 与 `Severed.marker` 放在一起后运行。程序会寻找附近的游戏和数据包，也能识别 Steam、EA 安装目录；有多个游戏时，点“自动识别”选择本次要安装的版本。

主页面可多选汉化补丁、Severed DLC、MarkerPatch，也可分别单装。字幕默认 **1.0 倍**，可直接调整。“其他由 MarkerPatch 提供的功能改善”中包含 59 项开关及 13 项数值设置，列明默认状态、功能说明和默认值。

安装顺序固定为 MarkerPatch → 原始英文 DLC → 汉化。只装 DLC 会保持英文，并自动安装所需的最小运行组件；在全新安装上只开启 LoadASIPlugins、EnableSeveredDLC，其他增强和字幕缩放关闭。已有用户配置保留。只装汉化不会额外安装加载器。

“卸载”使用首次覆盖前的备份恢复原文件。安装失败会恢复本次操作前的状态。关闭游戏后安装；不要直接在压缩软件预览目录中运行。

## 数据与历史版本

当前 `Severed.marker` 是原始英文 DLC 基包，含英文 `DS2DAT2.DAT`、海报 `Severed.poster.jpg` 和 `DLC.version.json`。汉化资源单独嵌入安装器，勾选汉化时最后应用。218 条 DLC 译文保持用户校对版本，字体为狮尾快剪黑 SC Medium。

2026.11.1 根据包内协议、最低安装器版本及解压文件长度/SHA-256 检查兼容性，不固定整包哈希。相同协议的新版数据可继续识别；不匹配现有 DLC 汉化资源时保留英文并提示。SHA-256 检查完整性，不提供来源认证。R8 及更早属于历史版本，不支持本协议的后续兼容。

安装器、游戏插件未签名。若安全软件报告具体威胁，请停止运行并核对来源。发行检查记录保留实际扫描结果。

## 卸载与修复

卸载先检查安装记录、原版 DAT0 备份、相关文件的哈希及可访问状态。缺少记录、备份损坏、文件被其他工具改动或占用时停止，并保留文件及安装记录。复制恢复备份时验证 SHA-256，再替换目标。

卸载恢复本工具保存的备份；安装前已有的 DLC/插件可能仍会保留。不能把“卸载完成”等同于 Steam/EA 已验证为纯净原版。意外 I/O 错误仍可能导致部分操作完成，失败提示会要求保留备份、记录并检查日志。

仍无法卸载时，先备份存档并关闭游戏，再按平台修复：

- Steam：库 → 死亡空间 2 → 属性 → 已安装文件 → 验证游戏文件完整性。[Steam 官方说明](https://help.steampowered.com/en/faqs/view/0C48-FCBD-DA71-93EB)
- EA app：库 → 死亡空间 2 → 管理 → 修复。[EA 官方说明](https://help.ea.com/en/articles/platforms/fix-crashing-and-freezing/)

平台修复可能保留非官方插件和 DLC 文件；结合日志检查残留，保留原备份和安装记录，不删除存档。

## 已知问题

中文冒号、括号等标点的字距偏窄，用户决定暂不修复。本版沿用已验证的汉化数据、字体与 218 条译文。
尚未测试全部 MarkerPatch 功能组合及与其他模组的兼容性；详细设置内也显示此说明。

## 人员与联系

当前版本基石：Wemino/MarkerPatch、Tubzbuster/DS2SeveredPC<br>
simgroup异版简单修复和整合：SomJs<br>
超级校对：lxyun_2（腹黑小喵）、Charlotte<br>
超级测试：待补充<br>
提供部分Token：天雷本雷

历史版本人员名单：

汉化总负责：lxyun_2（腹黑小喵）<br>
汉化技术负责：Erwin Qins<br>
翻译、校对、游戏测试：特洛克·洛克莱斯<br>
翻译、校对：attprtt、Charlotte、零之境、上白石泽雨<br>
校对：SomJs

- [交流群 339639634](https://qm.qq.com/q/AsZY1d8WxG)
- [死亡空间贴吧](https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page)
- [Wemino/MarkerPatch](https://github.com/Wemino/MarkerPatch)
- [Tubzbuster/DS2SeveredPC](https://github.com/Tubzbuster/DS2SeveredPC)
- [本项目源码](https://github.com/SomJsA/DS2-Severed-Installer)

源码仓库只发布程序代码、配置、构建说明与许可证，不含游戏数据、汉化资源包、海报/图标或外部 EXE/DLL/ASI。构建方法见 [BUILDING.md](BUILDING.md)，依赖记录见 [DEPENDENCIES.json](DEPENDENCIES.json)。
