# Severed 能量节点候选 2

SomJs 的节点修正基于 Tubzbuster/DS2SeveredPC 和 Wemino/MarkerPatch 的既有商店机制。候选 1 在旧 Marker 构建中由用户确认 Steam 节点恢复显示；随后 EA + 官方 MarkerPatch 3.0.7 的日志明确报告 store/list hook failed，不能把先前反馈当作该组合验收。

候选 2 保留节点 GUID、Severed 章节和商店清单范围限制，增加逐个挂钩错误记录。当原游戏入口链挂钩失败时，先回退已安装的部分钩子，再尝试已验证 E9 链指向的 MarkerPatch 回调。没有猜测加密代码入口；未知链仍拒绝启用。临时字段调用后恢复，不改价格或节点权限判断。

本地通过 71 项实际 x86 策略／挂钩检查及原插件构造器/AddEpisode ABI 检查，包括强制回退、原有钩子保留、节点恢复、价格和权限保留、本体及非商店范围不应用。候选 2 尚未实机验证，不能声称已解决当前 EA 问题。

Profile=1 仅节点；Profile=0 关闭节点修正；Profile=2 是此前保留的 20 商品实验，不属于已确认范围。安装器默认勾选节点修正，仍可手动取消。依赖 LoadASIPlugins、EnableSeveredDLC、EnableHackerDLC；最后一项会开放本体黑客装备，与 Ignition 房间开关不同。

Windows x86 MSVC 构建：运行 build.cmd。对应源码、SafetyHook/Zydis 源码及许可证均包含在此目录；不提交 build 输出、ASI、EXE 或游戏数据。冻结候选文件身份在 source-manifest.json。

来源：https://github.com/Tubzbuster/DS2SeveredPC 、https://github.com/Wemino/MarkerPatch 。保留各自许可证，修改部分按 GPL-2.0 提供。

AI 参与：候选调查、实现与说明有 AI 参与。隔离检查由工具实际执行；Steam 旧 C1 反馈与 EA C1 日志分别标注，C2 实机尚未完成。
