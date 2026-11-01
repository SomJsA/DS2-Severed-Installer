# 死亡空间贴吧汉化组移植&汉化 R9

完整解压分发 ZIP，把 `DS2SeveredOneClick.exe` 与 `Severed.marker` 放在一起后运行。程序会寻找附近的游戏和数据包，也能识别 Steam、EA 安装目录；有多个游戏时，点“自动识别”选择本次要安装的版本。

主页面可多选汉化补丁、Severed DLC、MarkerPatch，也可分别单装。字幕默认 **1.2 倍**，可直接调整。“其他由 MarkerPatch 提供的功能改善”中包含 59 项开关及 13 项数值设置，列明默认状态、功能说明和默认值。

安装顺序固定为 MarkerPatch → 原始英文 DLC → 汉化。只装 DLC 会保持英文，并自动安装所需的最小运行组件；在全新安装上只开启 LoadASIPlugins、EnableSeveredDLC，其他增强和字幕缩放关闭。已有用户配置保留。只装汉化不会额外安装加载器。

“卸载”使用首次覆盖前的备份恢复原文件。安装失败会恢复本次操作前的状态。关闭游戏后安装；不要直接在压缩软件预览目录中运行。

## 数据与历史版本

当前 `Severed.marker` 是原始英文 DLC 基包，含英文 `DS2DAT2.DAT`、海报 `Severed.poster.jpg` 和 `DLC.version.json`。汉化资源单独嵌入安装器，勾选汉化时最后应用。218 条 DLC 译文保持用户校对版本，字体为狮尾快剪黑 SC Medium。

R9 根据包内协议、最低安装器版本及解压文件长度/SHA-256 检查兼容性，不固定整包哈希。相同协议的新版数据可继续识别；不匹配现有 DLC 汉化资源时保留英文并提示。SHA-256 检查完整性，不提供来源认证。R8 及更早属于历史版本，不支持本协议的后续兼容。

安装器、游戏插件未签名。若安全软件报告具体威胁，请停止运行并核对来源。发行检查记录保留实际扫描结果。

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
- [死亡空间百度贴吧](https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page)
- [Wemino/MarkerPatch](https://github.com/Wemino/MarkerPatch)
- [Tubzbuster/DS2SeveredPC](https://github.com/Tubzbuster/DS2SeveredPC)
- [本项目源码](https://github.com/SomJsA/DS2-Severed-Installer)

源码仓库只发布程序代码、配置、构建说明与许可证，不含游戏数据、汉化资源包、海报/图标或外部 EXE/DLL/ASI。构建方法见 [BUILDING.md](BUILDING.md)，依赖记录见 [DEPENDENCIES.json](DEPENDENCIES.json)。
