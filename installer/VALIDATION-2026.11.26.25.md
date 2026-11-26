# 2026.11.26.25 validation

- Steam gameplay with the local test archive confirmed normal standing primary charge and distant hits under classic laser aiming. The released Simplified Chinese DLC archive has the same full SHA-256 as that tested archive. The English archive has the equivalent eleven one-byte changes in its eleven corresponding suit streams.
- Isolated installations covered a clean English DLC install and a Simplified Chinese upgrade from 2026.11.26.24. The independent installer audit matched the full archive digests and the bundled official MarkerPatch 3.3.1. The upgrade retained a custom INI entry. Both uninstall paths restored the pristine DAT0, removed the installed DAT2, and preserved the save fixture byte for byte.
- The Chinese global language table comparison found one changed label, `AE3D74E6` (OK), out of 7,403 entries. The patched value is `确定`. The asset comparison verifies the text; it does not cover every screen that displays the label.
- The user-provided MEGA link resolved to `Severed.marker` with the expected size of 1,170,665,452 bytes. Installation verifies the package manifest and the extracted data.

## 中文

Steam 实测确认经典激光瞄准下聚能枪站立蓄力姿势和远射命中恢复，发布版汉化 DLC 与该测试档 SHA-256 一致。英文版对同样的 11 个套装流各改 1 字节。隔离回归覆盖英文全新安装、汉化旧版升级、卸载及完整归档校验。已确认「确定」对应的文本资源连写，具体弹窗显示待逐页复核。
