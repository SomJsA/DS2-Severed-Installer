# 2026.11.28.26 验收范围 / Validation

- 使用隔离目录验证英文 DLC 首装、从 2026.11.26.25 带加载器冲突的汉化升级，以及按两个旧安装器的资源和重打包逻辑重建的贴吧 β0.1、β0.11 汉化升级。分别核对 DAT、随包模块、安装记录、卸载还原和测试存档。
- 使用官方 3.3.1 ZIP 和带额外配置项与资源的试验 ZIP 测试首装、导入、保存设置、升级、停用、恢复随包版和卸载。检查了加载器备份、陌生 ZIP 被拒绝，以及 DLC 所需的 INI 开关。
- 验证中英文主界面、常见高缩放和小工作区、MarkerPatch 独立更新提示，以及每个组件最多三条更新摘要。
- 本版验证针对安装器和文件。自定义 MarkerPatch 与游戏内 DLC、运行修正和其他模组的兼容性仍需要实机验证。聚能枪的实机记录见 [2026.11.26.25 验收范围](VALIDATION-2026.11.26.25.md)。

## English

- Isolated tests cover a fresh English DLC installation, a Chinese v2026.11.26.25 upgrade with loader replacement, and migrations from fixtures reconstructed from the two legacy Tieba localization installers. Checks cover installed files, exact restoration on uninstall, and save preservation.
- Custom MarkerPatch tests use the official 3.3.1 ZIP and a fixture with an altered loader, an extra setting, and an extra asset. They cover fresh installation, switching builds, saving settings, keeping the selected build through an upgrade, restoring the bundled build, and uninstalling.
- The bilingual layout and update preview checks cover smaller work areas and the separate MarkerPatch-only update state. In-game compatibility with imported versions requires separate testing.
