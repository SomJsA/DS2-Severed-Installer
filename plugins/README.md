# Independent runtime fixes / 独立问题修复

Each plugin has one responsibility, its own INI and a default-off upstream configuration. The installer enables the fixes selected by its managed profile. They use the original MarkerPatch ASI loader and do not import or modify the DLC core.

| Plugin | Scope | Upstream proposal |
| --- | --- | --- |
| SeveredPowerNodeFix.asi | Existing Power Node shop fix; requires EnableHackerDLC | [Draft #9](https://github.com/Tubzbuster/DS2SeveredPC/pull/9) |
| SeveredSaveDisplay.asi | DLC names and pictures in menu/native save lists; read-only save metadata | [Draft #11](https://github.com/Tubzbuster/DS2SeveredPC/pull/11) |
| SeveredMiningCampFix.asi | Exact mining-camp shared initializer Options 1→0 in memory | [Draft #13](https://github.com/Tubzbuster/DS2SeveredPC/pull/13) |

Build each with its build_msvc.cmd on x86 MSVC. Shared hook support and third-party source licenses are under common. Resource builders require privately supplied original game files and chapter artwork; those inputs/outputs are excluded from source distributions. See each plugin README for policy and validation limits.

原核心、官方加载器和三个问题修复分别保留。升级通过安装器移除旧合并 Extras；不要同时手动加载旧合并实现。矿区插件不修改 DAT／存档、不广播解锁，存档插件不转换存档内容。上游默认关闭，按各自验证范围保留 Draft。
