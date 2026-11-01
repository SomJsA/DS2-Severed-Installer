# DS2 Severed Installer

死亡空间贴吧汉化组移植&汉化。一站式安装器 2026.11.1 的纯代码仓库，支持 Steam / EA 版游戏。

**自有内容：稍后补充自有内容。**

- [安装器源码与说明](installer/README.md)
- [构建方法](installer/BUILDING.md)
- [MarkerPatch 功能中文说明、默认开关与参数](installer/MARKER_FEATURES_ZH.md)
- [原上游转换器说明](README.upstream.md)

程序采用遗产版图标和汉化图，主页面可分别选择汉化、DLC、MarkerPatch，字幕默认 1.0 倍。
游戏与 Severed.marker 优先从附近发现；自动识别遇到多个 Steam / EA 目录时可选择。

新上传的安装器目录仅含代码、配置、元数据示例、文档和许可证。
不上传游戏 DAT、DLC 大包、汉化资源、字体、美术素材或第三方二进制。
构建这些资源需由使用者另行提供。R9、R8 及以前为历史版本。

当前版本基石：[Wemino/MarkerPatch](https://github.com/Wemino/MarkerPatch)、
[Tubzbuster/DS2SeveredPC](https://github.com/Tubzbuster/DS2SeveredPC)。本仓库保留上游转换器代码及原始署名。

[交流群 339639634](https://qm.qq.com/q/AsZY1d8WxG) ·
[死亡空间贴吧](https://tieba.baidu.com/f?kw=%E6%AD%BB%E4%BA%A1%E7%A9%BA%E9%97%B4&fr=personalize_page)

已知：中文标点间距偏窄，暂不修复；未测试全部 MarkerPatch 功能组合和其他模组的兼容性。
卸载遇到异常会保留文件及安装记录，并提示通过 Steam/EA 修复游戏。
