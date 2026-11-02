# MarkerPatch 提供的功能改善

此表与 2026.11.2 / MarkerPatch 3.0.7 的界面及配置定义一致。61 项功能中默认启用 12 项，其余默认不启用。
仅 DLC 的全新安装只开启 LoadASIPlugins、EnableSeveredDLC；已有配置保留。

| 功能 | 默认 | 配置项 | 说明 |
|---|---|---|---|
| DLC 内容 / Hazard 套装包 | 开启 | `EnableHazardPack` | 在商店开放 Hazard 套装包中的装备。 |
| DLC 内容 / Martial Law 套装包 | 开启 | `EnableMartialLawPack` | 在商店开放 Martial Law 套装包中的装备。 |
| DLC 内容 / Supernova 套装包 | 开启 | `EnableSupernovaPack` | 在商店开放 Supernova 套装包中的装备。 |
| DLC 内容 / Severed 巡逻装备 | 开启 | `EnableSeveredDLC` | 开放 Severed 巡逻服与配套步枪。此项只控制装备，剧情关卡由 DLC 安装项提供。 |
| DLC 内容 / 黑客服装与武器 | 开启 | `EnableHackerDLC` | 开放黑客服装和配套武器，部分装备仍受游戏进度限制。 |
| DLC 内容 / 狂热者服装与武器 | 开启 | `EnableZealotDLC` | 在商店开放 Zealot 狂热者套装及配套武器。 |
| DLC 内容 / 铆钉枪 | 开启 | `EnableRivetGunDLC` | 在商店开放额外的铆钉枪武器。 |
| DLC 内容 / Ignition 奖励房间 | 开启 | `EnableIgnitionRooms` | 解锁原本供 Dead Space Ignition 玩家进入的奖励房间。 |
| DLC 内容 / 初代等离子切割器 | 开启 | `EnableOriginalPlasmaCutter` | 开放原本由初代存档解锁的奖励等离子切割器。 |
| 显示与字幕 / 自动匹配屏幕分辨率 | 关闭 | `AutoResolution` | 首次启动时自动使用屏幕分辨率。关闭后保留游戏自身的分辨率设置。 |
| 显示与字幕 / 字幕缩放 | 开启 | `FontScaling` | 启用适配高分辨率的字幕缩放，并应用所设倍率。默认倍率为 1.0倍；关闭此项时倍率不生效。 |
| 输入与手柄 / 原始鼠标输入 | 关闭 | `RawMouseInput` | 使鼠标灵敏度不再随帧率变化。陀螺仪瞄准要求此项开启。 |
| 输入与手柄 / SDL 手柄输入 | 关闭 | `UseSDLControllerInput` | 用 SDL 接管 XInput 手柄处理。会改变手柄输入路径，现有手柄正常时可保持关闭。 |
| 输入与手柄 / 屏蔽 DirectInput 设备 | 关闭 | `BlockDirectInputDevices` | 除鼠标键盘外，屏蔽 DirectInput 设备；不会屏蔽 XInput 手柄。使用其他控制器时需留意兼容性。 |
| 输入与手柄 / 允许系统快捷键 | 关闭 | `DisableKeyboardHook` | 阻止游戏拦截 Windows 等系统按键，方便使用系统快捷键。 |
| 输入与手柄 / 额外鼠标键绑定 | 关闭 | `ExtraMouseButtonBinding` | 允许将鼠标第 4 至第 8 键绑定到原本仅接受键盘输入的动作。 |
| 输入与手柄 / 自动隐藏菜单鼠标 | 关闭 | `AutoHideMouseCursor` | 使用手柄时隐藏菜单鼠标指针，重新移动鼠标后再显示。 |
| 输入与手柄 / 陀螺仪瞄准 | 关闭 | `GyroEnabled` | 启用兼容手柄的体感瞄准，需要原始鼠标输入。漂移时可将手柄放稳数秒校准。 |
| 输入与手柄 / 保存陀螺仪校准 | 关闭 | `GyroCalibrationPersistence` | 为具有唯一序列号的手柄保存校准记录，并在重新连接后恢复。 |
| 输入与手柄 / 触摸板模拟鼠标 | 关闭 | `TouchpadEnabled` | 用兼容手柄的触摸板模拟鼠标移动。 |
| 输入与手柄 / 任天堂手柄按键交换 | 关闭 | `InvertABXYButtons` | 交换 A/B、X/Y 的对应关系，适配任天堂手柄布局。 |
| 帧率与物理 / 高帧率物理修复 | 关闭 | `HavokPhysicsFix` | 以 30 FPS 物理步长并插值显示尸体、断肢与物品，减轻高帧率下乱飞的问题。 |
| 帧率与物理 / 多核心处理器启动修复 | 关闭 | `HighCoreCPUFix` | 针对超过 10 个核心的处理器相关崩溃。无此问题时可保持关闭。 |
| 帧率与物理 / 解除线程核心限制 | 关闭 | `ThreadAffinityFix` | 允许 Windows 将主线程和渲染线程分配到不同核心，不再都固定于第一个核心。 |
| 帧率与物理 / 垂直同步刷新率修复 | 关闭 | `VSyncRefreshRateFix` | 让垂直同步使用所选刷新率，避免锁定在 30 FPS。 |
| 帧率与物理 / 帧率限制节奏修复 | 关闭 | `FixFrameLimiter` | 改善垂直同步限帧的帧间隔，并减少限帧等待时持续占用处理器。 |
| 修复 / 难度通关奖励修复 | 关闭 | `FixDifficultyRewards` | 修正存档的难度通关记录，使 Hardcore 与 Zealot 奖励正常解锁。 |
| 修复 / 服装编号冲突修复 | 关闭 | `FixSuitIDConflicts` | 修正装备编号冲突，减轻购买特定服装后其他服装消失的问题。 |
| 修复 / Hazard 服装图标修复 | 关闭 | `FixHazardSuitIcon` | 修正商店中 Hazard 工程服错误显示精英高级服头盔的问题。 |
| 修复 / 存档字符串处理修复 | 关闭 | `FixSaveStringHandling` | 修正存档字符串处理中的缓冲区越界。对游戏版本的函数匹配有要求，默认关闭。 |
| 帧率与物理 / 自动武器射速修复 | 关闭 | `FixAutomaticWeaponFireRate` | 减轻自动武器在不同帧率下射速不一致的问题。 |
| 修复 / 第七章电梯门修复 | 关闭 | `FixSolarArrayElevator` | 修正高于 30 FPS 时重载检查点导致太阳能阵列电梯门卡住的问题。 |
| 画面 / 高分辨率模糊效果修复 | 关闭 | `FixBlurResolution` | 修正高于 720p 时景深、泛光、发光与热浪效果的模糊强度。 |
| 画面 / 柔和阴影模糊修复 | 关闭 | `FixShadowBlur` | 修正高于 720p 时柔和阴影的模糊强度。 |
| 画面 / 顶点法线与着色修复 | 关闭 | `FixVertexNormals` | 修正部分着色器误读法线所导致的玻璃反射、手电、粒子照明与动态模糊异常。 |
| 画面 / 耀斑方块修复 / D3D9 | 开启 | `FixFlareArtifacts` | 修复 NVIDIA 显卡开启耀斑时出现的块状光影异常。该修复使用 Direct3D9 挂钩路径。 |
| 帧率与物理 / 头发与布料物理修复 | 关闭 | `FixClothPhysics` | 减轻高于 30 FPS 时头发与布料变轻、漂浮的现象。 |
| 修复 / 高帧率菜单速度修复 | 关闭 | `FixMenuSpeed` | 减轻高帧率下菜单滚动、菜单镜头跟随鼠标过快的问题。 |
| 帧率与物理 / 游戏时钟修复 | 关闭 | `FixGameClock` | 减轻不限帧时游戏时间快于真实时间，以及帧间隔不稳定的问题。 |
| 性能 / 主循环空转优化 | 关闭 | `FixMainLoopSpin` | 让主线程等待音频时休眠，减少持续空转占用处理器。 |
| 性能 / 高帧率场景流送优化 | 关闭 | `FixStreamingBudget` | 按真实帧时间处理下一地区的数据，减轻高帧率时的流送卡顿。 |
| 性能 / 音频载入卡顿修复 | 关闭 | `FixAudioSyncStall` | 缩短区域音频加载时产生的停顿。 |
| 性能 / 场景纹理预上传 | 关闭 | `PreloadStreamedTextures` | 在场景流送期间上传新纹理，减轻首次显示新纹理时的卡顿。 |
| 帧率与物理 / 移动转向停顿修复 | 关闭 | `FixInputHistory` | 减轻高帧率下边移动边转向时角色短暂停顿的问题。 |
| 帧率与物理 / 钉墙弹体修复 | 关闭 | `FixImpalingProjectiles` | 修正高帧率下钉刺弹体力度不足、敌人未能钉在墙上的表现。 |
| 帧率与物理 / 持续爆炸伤害修复 | 关闭 | `FixExplosionDamage` | 让持续爆炸按预期频率计算伤害与推力，避免高帧率时每帧重复命中。 |
| 画面 / 视野外特效时长修复 | 关闭 | `FixOffscreenEffects` | 修正高帧率下身后火焰、蒸汽、火花与爆炸过早停止的问题。 |
| 修复 / 换装动画时序修复 | 关闭 | `FixSuitChangeTiming` | 修正载入更快时换装柜门过早打开，与灯光扫过不同步的问题。 |
| 常规 / 游戏内成就提示 | 关闭 | `AchievementSupport` | 显示成就进度、解锁提示和成就列表。默认使用 HOME 键打开列表。 |
| 常规 / 关闭失效在线连接 | 关闭 | `DisableOnlineFeatures` | 阻止连接已经失效的在线服务，并隐藏相关断线提示。 |
| 常规 / 增加尸体与断肢保留 | 关闭 | `IncreasedEntityPersistence` | 让当前区域保留更多尸体与断肢；数量可在参数页调整，可能增加资源占用。 |
| 画面 / 增加贴花细节 | 关闭 | `IncreasedDecalPersistence` | 允许保留更详细的贴花效果，可能增加资源占用。 |
| 常规 / 跳过开场视频 | 关闭 | `SkipIntro` | 启动游戏时跳过开场视频。 |
| 性能 / 取消最短加载等待 | 关闭 | `SkipArtificialLoadingDelay` | 数据实际加载完成后立即结束加载画面，不再等待人为设定的最短时长。 |
| 扩展与模组 / 加载 ASI 插件 | 开启 | `LoadASIPlugins` | 加载游戏 plugins 文件夹内的 ASI 插件。安装 Severed DLC 时此项是必需依赖；已有 Ultimate ASI Loader 时由其负责加载。 |
| 常规 / 提示扩展游戏可用内存 | 关闭 | `CheckLAAPatch` | 需要时提示应用 LAA 补丁，让 32 位游戏可使用更大内存。确认后会修改游戏 EXE；此开关使用询问模式，默认关闭。 |
| 扩展与模组 / 导出已加载资源 | 关闭 | `DumpArchiveAssets` | 将游戏载入的资源写入 archive_dump，主要用于研究和模组开发，可能产生大量文件。 |
| 扩展与模组 / 加载散装模组文件 | 关闭 | `LoadModFiles` | 从 mods 子目录载入替换资源，目录结构需与资源导出结构一致。 |
| 扩展与模组 / 跳过已替换归档数据 | 关闭 | `SkipReplacedArchiveData` | 配合散装模组使用，跳过只包含已替换资源的归档区段；首次载入会建立资源映射记录。 |

字幕倍率默认 **1.0 倍**（范围 0.5–3.0），主页面可调；只有 FontScaling 开启才生效。

| 数值参数 | 默认 | 范围 | 配置项 | 说明 |
|---|---|---|---|---|
| 各向异性过滤 | 0 | 0–16 | `MaxAnisotropy` | 改善斜视角纹理清晰度；0 表示关闭。 |
| 动态阴影分辨率 | 1920 | 0–8192 | `DynamicShadowResolution` | 只在游戏阴影设为“非常高”时使用；高于 1920 才启用覆盖。 |
| 增强抗锯齿 | 0 | 0–3 | `ImprovedAntiAliasingMode` | FXAA、SMAA 需要游戏内抗锯齿开启；SSAA 更清晰，但性能开销较大。 |
| SSAA 分辨率倍率 | 2 | 1–4 | `SSAAScale` | 仅使用 SSAA 时生效，倍率越高，显卡负担越重。 |
| 视野倍率 | 1 | 0.5–2 | `FOVScale` | 以默认视野为基准，1.0 为原始视野。 |
| 陀螺仪灵敏度 | 1 | 0.01–10 | `GyroSensitivity` | 体感瞄准灵敏度倍率，需要先开启陀螺仪瞄准。 |
| 陀螺仪平滑 | 0.016 | 0–1 | `GyroSmoothing` | 数值越高越平滑，但响应也会变慢。 |
| 成就列表快捷键 | 36 | 1–255 | `AchievementOverlayKey` | Windows 虚拟键码；默认 36 为 HOME。 |
| 尸体保留数量 | 25 | 0–500 | `IncreasedEntityPersistenceBodies` | 需要先开启“增加尸体与断肢保留”。数量较大时可能增加资源占用。 |
| 断肢保留数量 | 96 | 0–1000 | `IncreasedEntityPersistenceLimbs` | 需要先开启“增加尸体与断肢保留”。 |
| 散装模组读取线程 | 4 | 0–16 | `LooseFileThreads` | 0 使用主线程，1–16 使用后台线程；只影响散装模组。 |
| 散装模组预读内存（MB） | 32 | 0–512 | `LooseFilePrefetchMB` | 读取线程大于 0 时可提前读取散装模组；0 关闭预读。 |
| 游戏归档预读（MB） | 0 | 0–64 | `StreamReadAheadMB` | 0 关闭此项；游戏原始值为 1；补丁支持最高 64 MB。 |

## MarkerPatch 3.0.7 新增

- GyroInvertX：陀螺仪横向反转，默认不启用，需要先启用陀螺仪。
- GyroInvertY：陀螺仪纵向反转，默认不启用，需要先启用陀螺仪。

界面状态统一为默认启用、默认不启用、随 DLC 自动启用；节点修正依赖额外标注，不代表 Ignition 房间会一起开启。
