# 下游更新（2026-11-03）

2026.11.3.9 已将节点与存档显示扩展拆为独立 ASI，原版 MarkerPatch 保持不变；DLC 核心保留既有菜单处理。感谢作者在 [Issue #92](https://github.com/Wemino/MarkerPatch/issues/92#issuecomment-6590537755) 中提出独立插件的建议。下方是 2026-11-02 的历史核对记录，不代表重新核查了全部最新上游讨论。

---

# 上游节点核对（2026-11-02，香港时间）

结论：在所核对的最新公开源码、发布记录、全部公开 Issue/PR 正文及评论中，未找到直接匹配“Severed 首个商店能量节点不显示”的修复。MarkerPatch 已有黑客装备的同类章节／轮次绕过，不能说上游完全没有类似机制。

- PC 当前提交：[051fad3](https://github.com/Tubzbuster/DS2SeveredPC/commit/051fad379a7fdd9598a3e7fb96279f393ba91730)，发布 [v1.1.5（标题 v1.2.5）](https://github.com/Tubzbuster/DS2SeveredPC/releases/tag/v1.1.5)。原公开 ASI 下载 SHA256 仍为 9a4542eccdbf88ab3ca0c02c52aca34eff9d0ccf3d2fdf71fd1a6314cb561a46；当前 probe 源码与此前基线相同。核对 7 个 Issue、0 个 PR、9 条评论、131 份当前源码文本以及 archive/pre-1.0 的 110 份源码文本。
- MarkerPatch 当前提交：[d40f9ce](https://github.com/Wemino/MarkerPatch/commit/d40f9ce31175c8fa5d59cca9dc3b417a396076f8)，发布 [3.0.7](https://github.com/Wemino/MarkerPatch/releases/tag/3.0.7)。71 份功能源码／配置以 Git blob 身份校验；BonusUnlocks.cpp 与此前 3.0.7 一致。核对 88 个 Issue、1 个 PR、281 条评论；唯一 PR #26 只改 README。
- [BonusUnlocks.cpp](https://github.com/Wemino/MarkerPatch/blob/d40f9ce31175c8fa5d59cca9dc3b417a396076f8/src/Features/DLC/BonusUnlocks.cpp) 的 IsHackerItem 只匹配黑客套装与黑客接触光束。已有 [#43](https://github.com/Wemino/MarkerPatch/issues/43) 讨论 DLC 装备商店及章节进度，[#60](https://github.com/Wemino/MarkerPatch/issues/60) 讨论装备选择粒度／INI 复杂度；它们不是 Severed 节点修复。#2、#32 中虽提到 Power Node，但分别涉及场景崩溃和光效，不是商店节点购买。
- C1 在旧 Marker 构建中的 Steam 显示恢复来自用户反馈；C1 + 官方 3.0.7 在 EA 的 store/list hook failed 是我们下游集成日志。C2 仅通过隔离检查，游戏内仍待验证。不能把这些写成上游已确认缺陷或全部修复成功。

