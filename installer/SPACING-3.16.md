# 3.16 字距修正 / Readable spacing

3.15 已正确分流标题 MStiffHei 和剧情 Swei Regular，但旧生成器把英文/数字步进设成紧轮廓宽度+1，且生成图集时减去了源字体的左侧留白。译文中的汉字间 ASCII 空格又占 5 个字库单位，形成中文偏松、英数偏挤的共同问题。

3.16 采用已给出的 B 方案，两种字体按同一策略处理：

- 三份 FFN 的 ASCII 空格步进从 5 改为 3。文本中的空格及断词线索保留。
- 英文字母和数字的步进恢复为源 TTF `hmtx.advance × 40 / unitsPerEm`，四舍五入到整数；左偏移恢复为同尺度的 `hmtx.lsb`，不再额外加 1。
- 中文本体、标点、图集、UV、字形边界、纵向度量、既有字偶数据及字体映射逐字节保留。只修改字符记录的 xAdvance/xOffset。
- 相对3.15，DAT0只改4564F3D7字体流；前端字体分流和存档图片不变，译文/DLC数据/插件不变。

The source font bearings and advance widths are restored for ASCII letters and digits. A 3-unit ASCII space tightens the Chinese separators. Spaces remain in the text so the existing line-break mechanism can keep using them. This affects both title and dialogue font resources; it does not change their identities or add a plugin.

For example, at the 40-unit build scale, Swei `I` changes from advance 4 / left 0 to advance 12 / left 4; `0` changes from 19 / 0 to 22 / 2. MStiffHei uses its own source metrics under the same policy. Screen-space distances depend on the selected text size and engine transforms.

Rebuild from the privately supplied 3.15 font stream with `tools/patch_readable_spacing.py`. The tool requires the exact Swei Regular and MStiffHei TTFs and verifies their hashes. Input/output files and private assets are excluded from the pure-source ZIP. The program retains the established component settings, ownership checks, original backups and transactional rollback.

Offline previews do not certify the engine's perspective, shaders, pair adjustment or line wrapping. Acceptance must include real menu text and opening CG mixed Chinese/Latin/digit subtitles. No claim is made that every chapter and screen combination has been played.

2026-11-04 acceptance: 249 checks passed against the final installer. Steam was updated to 3.16; sampled menu, chapter/difficulty, CEC loading text and opening CG subtitles were inspected at a 1280×720 client resolution. The RIG784283 / T-B-3 long subtitle renders fully in two lines; T-B-3 occupies the second line under the existing engine wrapping. After normal exit, both saves, configuration, original DAT0 backup and unchanged runtime/data files were rechecked. Evidence is recorded in the frozen candidate's live-acceptance.json. This is sampled acceptance, not a complete playthrough.
