2026.11.4.16 — 中英数字距修正候选5 / Readable spacing candidate 5

采用B方案：空格5→3；英文字母和数字恢复各自源TTF的步进与左侧留白。保留3.15的字形、标点及标题/剧情字体分工。详见 installer/SPACING-3.16.md。

Restore the source-font Latin/digit advance widths and side bearings, and tighten ASCII spaces from 5 to 3. The established MStiffHei title / Swei Regular dialogue split and all glyph atlases remain exact. No new ASI or option dependency.

以下为历史说明 / Historical notes follow.

2026.11.4.15 — 字体角色修正候选4 / Font role correction candidate 4

标题 MStiffHei、剧情 Swei Regular。52 个前端界面显式选择标题字体；中文额外字距为 0，保留英数/标点修正。详情见 installer/FONT-ROLES-3.15.md。实机样本字形分别匹配；此候选未发布 GitHub。

MStiffHei frontend titles and Swei Regular dialogue. A data-only DAT0 overlay separates the shared font resource and preserves existing save-preview art. No new ASI. See installer/FONT-ROLES-3.15.md for input requirements and verification scope.

以下为历史说明 / Historical notes follow.

2026.11.4.14 — 字距候选3 / font spacing candidate 3
中文字距恢复原始值，不再额外加宽；剧情字形使用用户提供的 Swei Shear Sans CJK SC Regular，标题保持 MStiffHei。英文数字+1与符号留白沿用候选2。标题图集/UV沿用3.13，剧情图集按新字体重建，字形数量与行距不变。DAT0的2188个成员中仅4564F3D7字体变化。
Native CJK tracking is restored. Story text uses the supplied Swei Shear Sans CJK SC Regular face and titles retain MStiffHei. Candidate 2 Latin/digit and punctuation spacing is retained. The title atlas/UVs remain from 3.13; story atlases are rebuilt from the supplied face. Glyph count and line height are unchanged.

运行库前置 / Required runtime
勾选 MarkerPatch 或 DLC 时检查32位VC++运行库，两项共用同一依赖；仅安装汉化不要求安装运行库。已安装本包验证版本或较新版时跳过提示。缺失、较旧或不完整时，会说明这是MarkerPatch和DLC的运行依赖，询问是否打开微软原版安装程序。点否返回，尚未更改游戏文件。
Selecting MarkerPatch or DLC checks the shared x86 VC++ runtime. Localization alone skips the requirement. The validated version or newer skips setup. If missing/older/incomplete, consent is requested before opening Microsoft's unmodified setup UI. Declining stops before game changes.

本包随附Microsoft Visual C++ v14 x86 14.51.36247.0（Microsoft数字签名已验证）。安装时再核对大小、SHA-256、Windows签名和Microsoft发布者。保留原版安装/许可界面，不静默代为接受许可。使用/norestart；若需要重启，提示重启后再运行本程序，不开始改游戏。安装失败、取消或结束后检查仍不合格时停止并保留诊断报告。命令行无人值守模式遇缺失依赖会返回错误并给出官方下载地址，不意外弹出系统安装。
Bundled Microsoft VC++ v14 x86 14.51.36247.0 is unchanged and signature-verified. Size/hash/Windows signature/publisher are checked before execution. The Microsoft license/setup UI remains interactive, with /norestart. Cancellation, failure, incomplete post-check or required restart stops before game edits. Unattended CLI mode reports unmet dependencies without unexpectedly opening system setup.
https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files
https://aka.ms/vc14/vc_redist.x86.exe

使用 / Usage
完整解压，运行DS2SeveredOneClick.exe，自动识别或选择游戏目录；两个平台均找到时选择对应版本。按需要选择组件，DLC继续使用已有Severed.marker。顺序仍为MarkerPatch→原始DLC→汉化。只装DLC保持英文/启动器语言，仅安装需要的最小运行组件。
Extract fully, select the intended game installation and components, and reuse Severed.marker. Order remains MarkerPatch, original DLC, then optional Chinese. DLC without Chinese keeps English/launcher language and minimal dependencies.

MD5与日志 / MD5 and reports
安装/更改安装自动计算安装前后完整MD5及SHA-256，包括大型DAT与已有原始备份；慢速/外接硬盘会耗时。报告在安装器旁DS2-InstallLogs内，完成或失败都会给出路径。ZIP包含installation.log、snapshot-before/after.json与.csv、verification.json、运行库前置记录、环境信息及失败详情。未开始游戏安装时会记录game_installation_started=false，不伪造前后摘要。报告含本机路径和设置，发出前可查看；不包含存档或DAT内容，不自动上传。
Install/Change records complete before/after MD5 and SHA-256. Large data/backups are read in full, so slow disks take longer. Reports are under DS2-InstallLogs next to the installer. Success/failure paths retain logs, digest CSV/JSON, independent expected comparisons, runtime status and exceptions. A prerequisite cancellation records that game installation never started. Reports contain paths/settings, no saves or DAT contents, and are not uploaded automatically.

同一原版数据、安装器、数据包和选择下，生成DAT与随包插件应跨机器一致。配置、自定义值、路径、日志及安装记录不使用统一MD5；管理器随安装器版本变化。现有SHA-256完整性检查继续保留，不把整包Severed.marker哈希固定在EXE中。
Identical inputs and choices should produce identical DATs/bundled modules across machines. Configurations, paths, logs and installation records may differ; manager binaries change with installer version. Existing SHA-256 checks remain; no fixed whole-package Severed.marker hash is embedded.

若游戏仍无法启动，复现一次后使用StartupDiagnostics/DS2StartupDiagnostics.exe收集启动报告，连同安装日志ZIP发回。不要用本工具改写存档。DLC存档仍需要DLC组件。
If startup still fails, reproduce once, then run StartupDiagnostics/DS2StartupDiagnostics.exe and send both generated reports. DLC saves still require the DLC/runtime components.

本地候选，未发布GitHub；未在本机实际重装VC运行库。缺失/取消/重启/失败分支采用受控模拟验证，真实Microsoft安装包已验证签名和身份。游戏内效果仍需验收。
Local candidates, not published on GitHub. No actual VC runtime reinstall was performed on this computer. Dependency decisions/exit paths were tested with controlled callbacks; the real Microsoft package identity/signature was checked. In-game acceptance remains pending.

回退：先用本候选卸载，再用目标旧版重新安装所需组件。安装记录schema 14，旧管理器可能无法管理此记录，勿直接用于卸载。
Rollback: uninstall with this candidate first, then reinstall the desired older version/components. Older managers may reject this installation schema.
