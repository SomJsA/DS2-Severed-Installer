# SeveredExtras — independent ASI / 独立修复插件

Load plugins/SeveredExtras.asi with the official MarkerPatch ASI loader. It has no imports from SeveredPC.asi. Existing Power Node C2 and APX save-display behavior is retained, controlled by plugins/SeveredRuntimeFixes.ini. Both supported game builds and the known MarkerPatch shop hooks remain required for the Node fix. EnableHackerDLC still opens base-game Hacker equipment; it does not enable Ignition rooms.

The level observer is outside the DLC core entry signature and supports either loading order. Known previous combined local plugins cause this module to stay inactive, preventing duplicate fixes; use the matching split core from the installer or the original upstream core. Actual DLC play still needs the DLC data and runtime. Images use the existing managed DS2DAT3.DAT, not the save file. Missing/changed image data keeps the original image.

汉化仍由安装器修改原有数据资源；没有改为 ASI。此插件只保留原有节点与存档显示逻辑，不写入、转换或修复存档正文，也不解决矿区互动 BUG。现有安装请保留 SeveredRuntimeFixes.ini。示例 INI 不应覆盖用户已有配置。

Chinese save labels follow the successfully installed game localization flag, not the installer UI language. Without Chinese localization, labels remain Severed, chapter 1 / Severed, chapter 2. Removing the DLC does not make its saves loadable in the original game.

AI参与 / AI assisted. Thanks to Wemino/MarkerPatch, Tubzbuster/DS2SeveredPC and all credited contributors. No claim of upstream endorsement.
