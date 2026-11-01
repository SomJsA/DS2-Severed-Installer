using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
internal sealed class MarkerHelp { public string Key,Category,Title,Description; public MarkerHelp(string k,string c,string t,string d){Key=k;Category=c;Title=t;Description=d;} }
internal sealed class MarkerParameter
{
 public string Key,Title,Description,Choices; public decimal Min,Max,Step,Default; public int Decimals;
 public MarkerParameter(string k,string t,string d,decimal min,decimal max,decimal step,int places,decimal value,string choices){Key=k;Title=t;Description=d;Min=min;Max=max;Step=step;Decimals=places;Default=value;Choices=choices;}
}
internal static class MarkerCatalog
{
 public static readonly List<MarkerHelp> Features=new List<MarkerHelp>{
  new MarkerHelp("EnableHazardPack","DLC 内容","Hazard 套装包","在商店开放 Hazard 套装包中的装备。"),
  new MarkerHelp("EnableMartialLawPack","DLC 内容","Martial Law 套装包","在商店开放 Martial Law 套装包中的装备。"),
  new MarkerHelp("EnableSupernovaPack","DLC 内容","Supernova 套装包","在商店开放 Supernova 套装包中的装备。"),
  new MarkerHelp("EnableSeveredDLC","DLC 内容","Severed 巡逻装备","开放 Severed 巡逻服与配套步枪。此项只控制装备，剧情关卡由 DLC 安装项提供。"),
  new MarkerHelp("EnableHackerDLC","DLC 内容","黑客服装与武器","开放黑客服装和配套武器，部分装备仍受游戏进度限制。"),
  new MarkerHelp("EnableZealotDLC","DLC 内容","狂热者服装与武器","在商店开放 Zealot 狂热者套装及配套武器。"),
  new MarkerHelp("EnableRivetGunDLC","DLC 内容","铆钉枪","在商店开放额外的铆钉枪武器。"),
  new MarkerHelp("EnableIgnitionRooms","DLC 内容","Ignition 奖励房间","解锁原本供 Dead Space Ignition 玩家进入的奖励房间。"),
  new MarkerHelp("EnableOriginalPlasmaCutter","DLC 内容","初代等离子切割器","开放原本由初代存档解锁的奖励等离子切割器。"),
  new MarkerHelp("AutoResolution","显示与字幕","自动匹配屏幕分辨率","首次启动时自动使用屏幕分辨率。关闭后保留游戏自身的分辨率设置。"),
  new MarkerHelp("FontScaling","显示与字幕","字幕缩放","启用适配高分辨率的字幕缩放，并应用所设倍率。默认倍率为 1.2倍；关闭此项时倍率不生效。"),
  new MarkerHelp("RawMouseInput","输入与手柄","原始鼠标输入","使鼠标灵敏度不再随帧率变化。陀螺仪瞄准要求此项开启。"),
  new MarkerHelp("UseSDLControllerInput","输入与手柄","SDL 手柄输入","用 SDL 接管 XInput 手柄处理。会改变手柄输入路径，现有手柄正常时可保持关闭。"),
  new MarkerHelp("BlockDirectInputDevices","输入与手柄","屏蔽 DirectInput 设备","除鼠标键盘外，屏蔽 DirectInput 设备；不会屏蔽 XInput 手柄。使用其他控制器时需留意兼容性。"),
  new MarkerHelp("DisableKeyboardHook","输入与手柄","允许系统快捷键","阻止游戏拦截 Windows 等系统按键，方便使用系统快捷键。"),
  new MarkerHelp("ExtraMouseButtonBinding","输入与手柄","额外鼠标键绑定","允许将鼠标第 4 至第 8 键绑定到原本仅接受键盘输入的动作。"),
  new MarkerHelp("AutoHideMouseCursor","输入与手柄","自动隐藏菜单鼠标","使用手柄时隐藏菜单鼠标指针，重新移动鼠标后再显示。"),
  new MarkerHelp("GyroEnabled","输入与手柄","陀螺仪瞄准","启用兼容手柄的体感瞄准，需要原始鼠标输入。漂移时可将手柄放稳数秒校准。"),
  new MarkerHelp("GyroCalibrationPersistence","输入与手柄","保存陀螺仪校准","为具有唯一序列号的手柄保存校准记录，并在重新连接后恢复。"),
  new MarkerHelp("TouchpadEnabled","输入与手柄","触摸板模拟鼠标","用兼容手柄的触摸板模拟鼠标移动。"),
  new MarkerHelp("InvertABXYButtons","输入与手柄","任天堂手柄按键交换","交换 A/B、X/Y 的对应关系，适配任天堂手柄布局。"),
  new MarkerHelp("HavokPhysicsFix","帧率与物理","高帧率物理修复","以 30 FPS 物理步长并插值显示尸体、断肢与物品，减轻高帧率下乱飞的问题。"),
  new MarkerHelp("HighCoreCPUFix","帧率与物理","多核心处理器启动修复","针对超过 10 个核心的处理器相关崩溃。无此问题时可保持关闭。"),
  new MarkerHelp("ThreadAffinityFix","帧率与物理","解除线程核心限制","允许 Windows 将主线程和渲染线程分配到不同核心，不再都固定于第一个核心。"),
  new MarkerHelp("VSyncRefreshRateFix","帧率与物理","垂直同步刷新率修复","让垂直同步使用所选刷新率，避免锁定在 30 FPS。"),
  new MarkerHelp("FixFrameLimiter","帧率与物理","帧率限制节奏修复","改善垂直同步限帧的帧间隔，并减少限帧等待时持续占用处理器。"),
  new MarkerHelp("FixDifficultyRewards","修复","难度通关奖励修复","修正存档的难度通关记录，使 Hardcore 与 Zealot 奖励正常解锁。"),
  new MarkerHelp("FixSuitIDConflicts","修复","服装编号冲突修复","修正装备编号冲突，减轻购买特定服装后其他服装消失的问题。"),
  new MarkerHelp("FixHazardSuitIcon","修复","Hazard 服装图标修复","修正商店中 Hazard 工程服错误显示精英高级服头盔的问题。"),
  new MarkerHelp("FixSaveStringHandling","修复","存档字符串处理修复","修正存档字符串处理中的缓冲区越界。对游戏版本的函数匹配有要求，默认关闭。"),
  new MarkerHelp("FixAutomaticWeaponFireRate","帧率与物理","自动武器射速修复","减轻自动武器在不同帧率下射速不一致的问题。"),
  new MarkerHelp("FixSolarArrayElevator","修复","第七章电梯门修复","修正高于 30 FPS 时重载检查点导致太阳能阵列电梯门卡住的问题。"),
  new MarkerHelp("FixBlurResolution","画面","高分辨率模糊效果修复","修正高于 720p 时景深、泛光、发光与热浪效果的模糊强度。"),
  new MarkerHelp("FixShadowBlur","画面","柔和阴影模糊修复","修正高于 720p 时柔和阴影的模糊强度。"),
  new MarkerHelp("FixVertexNormals","画面","顶点法线与着色修复","修正部分着色器误读法线所导致的玻璃反射、手电、粒子照明与动态模糊异常。"),
  new MarkerHelp("FixFlareArtifacts","画面","耀斑方块修复 / D3D9","修复 NVIDIA 显卡开启耀斑时出现的块状光影异常。该修复使用 Direct3D9 挂钩路径。"),
  new MarkerHelp("FixClothPhysics","帧率与物理","头发与布料物理修复","减轻高于 30 FPS 时头发与布料变轻、漂浮的现象。"),
  new MarkerHelp("FixMenuSpeed","修复","高帧率菜单速度修复","减轻高帧率下菜单滚动、菜单镜头跟随鼠标过快的问题。"),
  new MarkerHelp("FixGameClock","帧率与物理","游戏时钟修复","减轻不限帧时游戏时间快于真实时间，以及帧间隔不稳定的问题。"),
  new MarkerHelp("FixMainLoopSpin","性能","主循环空转优化","让主线程等待音频时休眠，减少持续空转占用处理器。"),
  new MarkerHelp("FixStreamingBudget","性能","高帧率场景流送优化","按真实帧时间处理下一地区的数据，减轻高帧率时的流送卡顿。"),
  new MarkerHelp("FixAudioSyncStall","性能","音频载入卡顿修复","缩短区域音频加载时产生的停顿。"),
  new MarkerHelp("PreloadStreamedTextures","性能","场景纹理预上传","在场景流送期间上传新纹理，减轻首次显示新纹理时的卡顿。"),
  new MarkerHelp("FixInputHistory","帧率与物理","移动转向停顿修复","减轻高帧率下边移动边转向时角色短暂停顿的问题。"),
  new MarkerHelp("FixImpalingProjectiles","帧率与物理","钉墙弹体修复","修正高帧率下钉刺弹体力度不足、敌人未能钉在墙上的表现。"),
  new MarkerHelp("FixExplosionDamage","帧率与物理","持续爆炸伤害修复","让持续爆炸按预期频率计算伤害与推力，避免高帧率时每帧重复命中。"),
  new MarkerHelp("FixOffscreenEffects","画面","视野外特效时长修复","修正高帧率下身后火焰、蒸汽、火花与爆炸过早停止的问题。"),
  new MarkerHelp("FixSuitChangeTiming","修复","换装动画时序修复","修正载入更快时换装柜门过早打开，与灯光扫过不同步的问题。"),
  new MarkerHelp("AchievementSupport","常规","游戏内成就提示","显示成就进度、解锁提示和成就列表。默认使用 HOME 键打开列表。"),
  new MarkerHelp("DisableOnlineFeatures","常规","关闭失效在线连接","阻止连接已经失效的在线服务，并隐藏相关断线提示。"),
  new MarkerHelp("IncreasedEntityPersistence","常规","增加尸体与断肢保留","让当前区域保留更多尸体与断肢；数量可在参数页调整，可能增加资源占用。"),
  new MarkerHelp("IncreasedDecalPersistence","画面","增加贴花细节","允许保留更详细的贴花效果，可能增加资源占用。"),
  new MarkerHelp("SkipIntro","常规","跳过开场视频","启动游戏时跳过开场视频。"),
  new MarkerHelp("SkipArtificialLoadingDelay","性能","取消最短加载等待","数据实际加载完成后立即结束加载画面，不再等待人为设定的最短时长。"),
  new MarkerHelp("LoadASIPlugins","扩展与模组","加载 ASI 插件","加载游戏 plugins 文件夹内的 ASI 插件。安装 Severed DLC 时此项是必需依赖；已有 Ultimate ASI Loader 时由其负责加载。"),
  new MarkerHelp("CheckLAAPatch","常规","提示扩展游戏可用内存","需要时提示应用 LAA 补丁，让 32 位游戏可使用更大内存。确认后会修改游戏 EXE；此开关使用询问模式，默认关闭。"),
  new MarkerHelp("DumpArchiveAssets","扩展与模组","导出已加载资源","将游戏载入的资源写入 archive_dump，主要用于研究和模组开发，可能产生大量文件。"),
  new MarkerHelp("LoadModFiles","扩展与模组","加载散装模组文件","从 mods 子目录载入替换资源，目录结构需与资源导出结构一致。"),
  new MarkerHelp("SkipReplacedArchiveData","扩展与模组","跳过已替换归档数据","配合散装模组使用，跳过只包含已替换资源的归档区段；首次载入会建立资源映射记录。"),
 };
 public static readonly List<MarkerParameter> Parameters=new List<MarkerParameter>{
  new MarkerParameter("MaxAnisotropy","各向异性过滤","改善斜视角纹理清晰度；0 表示关闭。",0m,16m,2m,0,0m,"关闭|2×|4×|8×|16×"),
  new MarkerParameter("DynamicShadowResolution","动态阴影分辨率","只在游戏阴影设为“非常高”时使用；高于 1920 才启用覆盖。",0m,8192m,256m,0,1920m,""),
  new MarkerParameter("ImprovedAntiAliasingMode","增强抗锯齿","FXAA、SMAA 需要游戏内抗锯齿开启；SSAA 更清晰，但性能开销较大。",0m,3m,1m,0,0m,"关闭|FXAA|SMAA|SSAA"),
  new MarkerParameter("SSAAScale","SSAA 分辨率倍率","仅使用 SSAA 时生效，倍率越高，显卡负担越重。",1m,4m,0.25m,2,2m,""),
  new MarkerParameter("FOVScale","视野倍率","以默认视野为基准，1.0 为原始视野。",0.5m,2m,0.05m,2,1m,""),
  new MarkerParameter("GyroSensitivity","陀螺仪灵敏度","体感瞄准灵敏度倍率，需要先开启陀螺仪瞄准。",0.01m,10m,0.1m,2,1m,""),
  new MarkerParameter("GyroSmoothing","陀螺仪平滑","数值越高越平滑，但响应也会变慢。",0m,1m,0.001m,3,0.016m,""),
  new MarkerParameter("AchievementOverlayKey","成就列表快捷键","Windows 虚拟键码；默认 36 为 HOME。",1m,255m,1m,0,36m,""),
  new MarkerParameter("IncreasedEntityPersistenceBodies","尸体保留数量","需要先开启“增加尸体与断肢保留”。数量较大时可能增加资源占用。",0m,500m,1m,0,25m,""),
  new MarkerParameter("IncreasedEntityPersistenceLimbs","断肢保留数量","需要先开启“增加尸体与断肢保留”。",0m,1000m,1m,0,96m,""),
  new MarkerParameter("LooseFileThreads","散装模组读取线程","0 使用主线程，1–16 使用后台线程；只影响散装模组。",0m,16m,1m,0,4m,""),
  new MarkerParameter("LooseFilePrefetchMB","散装模组预读内存（MB）","读取线程大于 0 时可提前读取散装模组；0 关闭预读。",0m,512m,8m,0,32m,""),
  new MarkerParameter("StreamReadAheadMB","游戏归档预读（MB）","0 关闭此项；游戏原始值为 1；补丁支持最高 64 MB。",0m,64m,1m,0,0m,""),
 };
 public static MarkerHelp Find(string key){return Features.FirstOrDefault(x=>x.Key==key);}
 public static Dictionary<string,string> DefaultParameters(){return Parameters.ToDictionary(x=>x.Key,x=>x.Default.ToString(CultureInfo.InvariantCulture));}
 public static void Validate(Dictionary<string,string> values)
 {
  foreach(var pair in values)
  {
   var p=Parameters.FirstOrDefault(x=>x.Key==pair.Key);decimal value;
   if(p==null || !decimal.TryParse(pair.Value,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out value) || value<p.Min || value>p.Max)
    throw new System.IO.InvalidDataException("MarkerPatch 参数无效："+pair.Key);
   if(p.Decimals==0 && value!=decimal.Truncate(value))
    throw new System.IO.InvalidDataException("MarkerPatch 此参数必须是整数："+pair.Key);
   if(p.Key=="MaxAnisotropy" && value!=0 && value!=2 && value!=4 && value!=8 && value!=16)
    throw new System.IO.InvalidDataException("各向异性过滤仅支持 0、2、4、8、16。");
  }
 }
}
