# 2026.11.4.15 字体修正 / Font role correction

用户指定：标题 MStiffHei、剧情 SweiShearSansCJKsc-Regular；中文额外 advance 为 0。保留 3.13 的英文数字、标点和空格策略。

3.14 只重建 Eurostile/Ishimura，CG 字幕仍从 RussellSquare 取得字形，因此此前的字体角色验收失败。3.15 把 Swei Regular 放入 RussellSquare，并把 52 个前端 APX 的 RussellSquare 声明定向到完整资源名 `eurostileltstdbold32`。该槽位含 MStiffHei。仅填写不带 `32` 的别名不能正确加载。Ishimura 同样保留 MStiffHei。

The frontend and native subtitles share RussellSquare. Candidate 3 rebuilt the wrong slot for the inspected dialogue. Candidate 4 assigns Swei Regular to that slot and explicitly routes the 52 frontend movies to the MStiffHei resource. This is a data patch, with no new ASI, executable hook, or option dependency.

界面流和 TOC 作为两个新成员加入 DAT0；游戏实测 DAT0 覆盖生效。界面流以现有已验证的存档图片版本为基础，保留两章图片、原有帧和非字体数据。DLC 的 DAT3、核心及 Extras 保持原字节。仅汉化安装也包含这一数据覆盖；完整卸载恢复原始 DAT0 备份。

The DAT0 overlay preserves the established save-preview artwork. Unknown frontend resources or a modified/foreign DAT3 stop localization instead of being silently masked. The existing installer transaction, ownership rules and rollback apply to the entire DAT0. Chinese-only installs require no DLC or MarkerPatch component.

生成顺序 / Rebuild order:

1. 私有 `font_build/data/font.ttf` 使用用户 Swei Regular，`mstiffhei.ttf` 使用用户标题字体；保留既有字符集和基准字体流。运行 `generate_font_roles.py`，再运行 `font_build/build_font_stream.py` 和 `patch_font_roles_metrics.py`。
2. 将校验后的字体流放为 `assets/chinese/4564F3D7.str`。标题界面使用 `build_title_ui_overlay.py`，输入旧版已验证的存档图标 DAT3，并把两项输出放入 `assets/chinese`。
3. `make_chinese_assets.py` 生成六成员私有资源包；构建时核对所有嵌入资源的 SHA-256 和完整 DAT0 的大小/SHA-256。重建新包需更新构建清单并使用新版本，不能覆盖冻结包。

Private fonts, translated/game resources, artwork, APX movies, and third-party binaries are excluded from the pure-source ZIP. The build scripts require those separately supplied inputs. Existing save files are never rewritten.

实机字形证据 / Measured evidence: menu “单人游戏” IoU MStiffHei 0.7791 vs Swei 0.4265; CG “你是说成功了” IoU Swei 0.6213 vs MStiffHei 0.4574. These are sampled outline comparisons, not a claim that every screen or chapter has been played.
