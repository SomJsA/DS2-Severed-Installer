# 2026.11.11.20 — subtitle windows / 字幕显示时长

- Extend 24 existing DLC subtitle windows to the maximum available time before the next authored event or conservative audio-stream tail.
- Preserve all subtitle starts, audio cue times, IDs and the 15 entries with no spare window; no timeline reordering.
- The localized stream changes 45 duration bytes only. The primary “不确定因素” line changes from 2.0 s to 3.6 s.
- The original DLC core, fonts, punctuation, 21 short-label fixes and independent runtime ASIs remain unchanged.

## 2026.11.6.19

- 修复“返回、确认、继续、取消、选择、启动”等短操作提示因内部空格而换行的问题，保留已确认的字体字形与度量。
- 按问题拆分为 `SeveredPowerNodeFix.asi`、`SeveredSaveDisplay.asi`、`SeveredMiningCampFix.asi`，保留原 DLC 核心与官方 MarkerPatch 3.0.7；升级时迁移旧合并 Extras，避免重复钩子。
- DLC 存档显示同时覆盖主菜单与局内全息保存页，匹配两章名称与图片；只改显示，不转换或改写存档。
- 矿区修正针对已复现的“操作面板后死亡读档，三个面板全锁”：只修正指定共用初始化对象，保留三个面板分别完成的要求。
- 合入此前字体角色、中文／英数字距与标点修正，以及 x86 运行库前置检查。已有设置、节点偏好、原始备份与存档保留。
- 安装记录升级为 schema 15，纳入独立 ASI 的校验、迁移、回滚与卸载。完整 MD5／SHA-256 审计报告仍保存在本机。

继续使用原 `Severed.marker`，本次不重发完整 DLC 数据包。程序包、纯源码和许可证单独提供；源码不含游戏资源、私有字体、图片、存档或第三方二进制。

472 项定向检查通过；Steam 已确认主菜单／局内保存页两章名称与图片、字体角色和多页短提示，四份存档与设置保持。详细范围见 installer/VALIDATION-2026.11.6.19.md。

PC 上游：[矿区 Draft PR #13](https://github.com/Tubzbuster/DS2SeveredPC/pull/13)、[存档显示 Draft PR #11](https://github.com/Tubzbuster/DS2SeveredPC/pull/11)、[节点 Draft PR #9](https://github.com/Tubzbuster/DS2SeveredPC/pull/9)。上游 PR 按其各自验证范围保留 Draft。

## English

This release removes internal spaces from short action prompts, retains the accepted fonts/spacing, and keeps each runtime issue in its own ASI. The original DLC core and official MarkerPatch loader remain separate. Known older combined Extras installations are migrated to prevent duplicate hooks.

The save-display plugin covers both the menu and the native in-game Save Game list, with chapter-specific DLC labels and pictures. Saved-game content and progress are unchanged. The mining fix targets the reproduced post-death/reload terminal lock by correcting only the exact shared initializer, while retaining all three independent puzzle steps.

The release also includes the prior font-role/spacing/punctuation improvements, x86 runtime prerequisite checks, and complete-file install auditing. Settings, Power Node preferences, saves and original backups are preserved. Ownership schema 15 covers migration, refusal, rollback and uninstall for the independent plugins.

Reuse the existing `Severed.marker`. Source excludes game data, private fonts/art, saved games and compiled dependencies. Targeted Steam display acceptance and 472 isolated checks passed. Current saves/settings are preserved; broader platform and full-playthrough combinations remain outside the targeted scope.

Thank you to the original localization contributors, Tubzbuster/DS2SeveredPC and Wemino/MarkerPatch. AI participated in analysis and implementation.


---

2026.11.4.18 — 剧情中点与标点微调候选7 / Complete middle dot candidate 7

按反馈恢复中英文句号的3.16间距；逗号左侧累计比3.16增加2字库单位。剧情人名中点修复UV裁切，显示完整圆点并适度放大；保留少量左侧留白。标题中点及其他3.17标点微调保留。中文、英数、空格3与字体分工不变，所有图集逐字节保持。详见installer/MIDDLE-DOT-3.18.md。

Periods restore 3.16 spacing, commas gain two left-side build units over 3.16, and the dialogue middle dot reveals its complete existing circle with a fuller quad. No atlas, Chinese/Latin/digit/space or font-role changes.

以下为历史说明 / Historical notes follow.

2026.11.4.17 — 标点左侧微调候选6 / Punctuation left padding candidate 6

人名中点·、逗号、句号、顿号、问号、叹号、冒号、分号、省略号及对应半角符号，左侧增加1个字库单位。步进同步增加1，右侧原有留白保持。保留3.16空格3与源字体英数步进、字形及字体分工。详情见installer/PUNCTUATION-3.17.md。

Selected name/separator/sentence punctuation gains one build unit on its left. Advance increases by the same amount, preserving its right-side whitespace. The 3.16 Chinese/Latin/digit/space metrics, glyph atlases and font roles remain exact.

以下为历史说明 / Historical notes follow.

2026.11.4.16 — 中英数字距修正候选5 / Readable spacing candidate 5

采用B方案：空格5→3；英文字母和数字恢复各自源TTF的步进与左侧留白。保留3.15的字形、标点及标题/剧情字体分工。详见 installer/SPACING-3.16.md。

Restore the source-font Latin/digit advance widths and side bearings, and tighten ASCII spaces from 5 to 3. The established MStiffHei title / Swei Regular dialogue split and all glyph atlases remain exact. No new ASI or option dependency.

以下为历史说明 / Historical notes follow.

2026.11.4.15 — 字体角色修正候选4 / Font role correction candidate 4

标题 MStiffHei、剧情 Swei Regular。52 个前端界面显式选择标题字体；中文额外字距为 0，保留英数/标点修正。详情见 installer/FONT-ROLES-3.15.md。实机样本字形分别匹配；此候选未发布 GitHub。

MStiffHei frontend titles and Swei Regular dialogue. A data-only DAT0 overlay separates the shared font resource and preserves existing save-preview art. No new ASI. See installer/FONT-ROLES-3.15.md for input requirements and verification scope.

以下为历史说明 / Historical notes follow.

## 2026.11.4.14 — 本地候选 / local candidate

2026.11.4.14 — 字距候选3 / font spacing candidate 3
中文字距恢复原始值；剧情字形使用 Swei Shear Sans CJK SC Regular，标题保持 MStiffHei。英文数字+1与符号留白沿用候选2。标题图集/UV沿用3.13，剧情图集按新字体重建；DAT0仅4564F3D7变化。
Native CJK tracking restored. Story text uses Swei Shear Sans CJK SC Regular and titles retain MStiffHei. Candidate 2 Latin/digit and punctuation spacing remains. The title atlas/UVs stay from 3.13; story atlases use the new face. Only DAT0 member 4564F3D7 changes.

- MarkerPatch/DLC前置x86运行库检查与确认后微软安装界面；安装前后MD5/SHA-256及持久报告。
- Runtime prerequisite with explicit consent; persistent MD5/SHA-256 installation reports.

## 2026.11.3.13 — 本地候选 / local candidate

2026.11.3.13 — 字距候选2 / font spacing candidate 2
剧情字形沿用MStiffHei，标题字形沿用狮尾。中文字距由上一候选+2减为+1像素，英文数字+1，空格与标点分类补留白。保留原字形、图集、UV、字形数量和行距。三份字体各1975个汉字宽度与对应源字体核对一致；DAT0的2188个成员中仅4564F3D7字体变化。对照图为离线模拟，实机换行/裁切待验收。
Keeps MStiffHei/Swei face assignments; CJK +1 instead of +2, Latin/digits +1, punctuation/word spacing adjusted. Glyph images, atlas, UVs/count and line height remain exact. In-game wrapping/clipping pending.

- MarkerPatch/DLC前置x86运行库检查与确认后微软安装界面；安装前后MD5/SHA-256及持久报告。
- Runtime prerequisite with explicit consent; persistent MD5/SHA-256 installation reports.

## 2026.11.3.11 — 本地诊断版 / local diagnostic build

- 自动保存安装过程和失败详情，完整记录安装前后MD5及SHA-256，已知数据与独立参考值核对；成功和失败均生成报告ZIP。
- Persistent operation/failure logs, before/after complete MD5 and SHA-256, known-output verification, report ZIP on success or failure.
- 记录x86运行库版本，数据/原生/选项沿用正式3.9；不包含3.10字体试验。不自动安装运行库，不发布GitHub。
- Records x86 runtime versions. Data/native modules/options stay on published3.9; no font experiment, automatic runtime installation or GitHub publication.

# 更新记录

## 2026.11.3.9

- 将能量节点与 DLC 存档显示修复拆为独立 ASI；MarkerPatch 的勾选、位置、默认值和依赖保持不变。汉化仍是数据补丁。
- 包含近期 DLC 菜单中文、存档章节名称和两章图片修复。未安装汉化时，DLC 与存档名称保持英文／启动器语言。
- 改善打开更新网页的兼容性；失败时提供可复制地址，检查失败时仍可手动查看发布页。不自动下载安装。
- 支持已知旧版直接升级并保留设置。异常时请先备份存档，再尝试全新安装。继续使用原 **Severed.marker**，本次不重发数据包。

矿区死亡／读档后的互动面板问题仍未修复。DLC 存档仍需要 DLC 和运行组件；本版不转换存档内容。

推荐下载 **program** 包；单 EXE、完整程序源码、独立 ASI、许可证和 SHA-256 校验文件也已提供。已有旧合并插件的用户建议使用安装器更改安装。


## 2026.11.3.1

- 移除安装前强制打开功能页和 3 秒等待，改为主页面一行说明；详细设置按需打开。
- 保留 18 项 MarkerPatch 默认预设、已有自定义值、DLC 最小依赖与原安装确认。
- 本次只更新安装器；复用原 Severed.marker。剩余 DLC 运行问题另行排查。

## 2026.11.3

- 修复了 MarkerPatch 功能内容老是有人不看的问题：每次打开程序后，对所选游戏首次点击安装会先打开功能页，阅读 3 秒后才可继续。可修改或取消全部可选功能；“取消并返回”不执行安装，再次安装时仍会提示。
- 新增默认勾选：高核 CPU 修复、移动输入历史修复、太阳能电池阵列电梯修复、菜单速度修复、爆炸伤害修复、视野外特效修复。MarkerPatch 预设共 18 项；已有自定义值保留，仅安装 DLC 仍只启用必需依赖。
- 功能页说明仅按随包 MarkerPatch 版本提供兼容性；支持按需选择，之后回到本程序更改安装或保存设置。
- 改善倒计时按钮文字的可读性，补全六项功能的中英文说明。
- 程序更新不附带 Severed.marker，继续使用原数据包。MarkerPatch 3.0.7、节点 C2、汉化数据不变；节点 C2 实机验证仍待完成。

## 2026.11.2.1

- 使用递增版本号，让已发出的 2026.11.2 能识别后续维护更新；确认后只打开网页，不自动下载安装。
- 发布页按钮、安装完成 DLC 入口提示及确认后退出保持；全部组件卸载与残留清理保持。
- 与已发出界面版使用相同节点 C2 二进制，游戏内验证仍待完成。
- 新旧 EXE 的更新解析检查共 14 项通过，中英文布局通过；此前维护、卸载和原生检查结果适用。


## 2026.11.2（已分发基线，节点候选 2）

- 右上交流群旁新增 GitHub 发布页按钮；安装成功后显示 DLC 的“新游戏 → Severed”入口提示，确认后关闭安装器主窗口。取消或安装失败时保留窗口。

- 默认全卸载并清理已确认日志、空目录和管理器版本文件；支持旧卸载残留清理。
- 旧版 DLC 没有节点选项记录时默认启用；保留明确保存的关闭状态。
- EA + MarkerPatch 3.0.7 的 C1 商店钩子失败已确认；候选 2 增加诊断与已校验回调回退，待实机验证。

- 勾选或重新勾选 DLC 时默认勾选能量节点修正，仍可手动取消；读取已有安装时保留已保存的选择。

- 新增 Severed 能量节点购买修正与提示优化。
- MarkerPatch 更新至 3.0.7，新增陀螺仪横向／纵向反转设置。
- 支持更改安装、保留自定义值的旧版升级、配置单独保存、根目录管理器与随包 MarkerPatch 启动控制。
- 显示安装依赖与外置版本提示，区分默认值与自动必需项。
- 检查更新并显示合并日志，确认后打开发布网页；不自更新。
- 安装完成提示 DLC 位于“新游戏”。

## 2026.11.1（历史版）

Playable：Severed 已可游玩。提供简体中文／English 安装器与汉化、DLC、MarkerPatch 独立选择。
