# 凤凰三联装炮塔

目标为 RimWorld 1.7 / Waylith，使用凤凰现有离屏模型描边和 RimUnity 资源系统。

## 建造与射击

建筑定义为 `Phoenix_TripleTurret`，在「建筑规划 → 安全」中显示为「凤凰三联装炮塔」，没有研究前置。

- 占地 2×2，需要重型地基；造价为 250 钢铁、6 工业零部件，建造技能 6。
- 耗电 350 W；支持自动索敌、手动指定目标、停火与电源开关。
- 射程 3–34.9 格；预热 1.2 秒，三发间隔 8 Tick，连射后冷却 4 秒。
- 转台速度 180 度/秒，进入 2 度瞄准容差后才开始连射。
- 每根炮管独立后坐 0.14 格，在 18 Tick 内复进，射击顺序为左、中、右。
- 每发炮弹伤害 24、穿甲 0.3、速度 90 格/秒。数值集中在 `PhoenixRace/1.7/Defs/ThingDefs_Buildings/Phoenix_TripleTurret.xml`。

## 美术与刚性骨骼

原始模型来自用户提供的 `D:/QQ/炮塔_骨骼.fbx`，原件保留在 `ArtSource/Turrets/炮塔_骨骼.fbx`。Unity 资源位于 `E:/mygame/PhoenixRace/Assets/Models/PhoenixTripleTurret`。

原模型只有材质色，没有外部贴图。白、灰、黑、红烘焙到一张 256×256 贴图 `PhoenixTripleTurret_Albedo.png`，全炮塔共享一个材质。源模型每块网格的顶点均为单骨骼 100% 权重，导出时固化到对应骨骼的局部坐标；运行时移动刚性节点，不逐顶点计算蒙皮。

```text
PhoenixTripleTurret
└─ root
   ├─ Base
   └─ turret_yaw
      ├─ Housing
      ├─ barrel_0 → Barrel0、Muzzle
      ├─ barrel_1 → Barrel1、Muzzle
      └─ barrel_2 → Barrel2、Muzzle
```

三根炮管按网格连通部分拆分，保留完整炮口和炮管几何。转台、后坐和炮口绑定都来自 XML 节点路径。逻辑炮口读取共享模板坐标，屏幕外射击不需要创建模型实例。

## 视角与角度预览

炮塔采用 SSR `1.7test` 的固定 60° 正交俯视、纯黑外描边和 0.05 地图单位线宽。按屏幕面积在 256～4096 输出档位间选择，低于 2048 时以两倍宽高采集，最终纹理不生成 Mipmap。底座固定对齐建筑位置，转向与抬炮不改变取景尺度。

开启游戏开发者模式，选中炮塔后点击「调试：炮塔角度」。窗口可拖动，镜头可移动；滑条支持水平 0～360°、俯仰 −90～90°，正俯仰表示抬高炮口。「恢复原姿态」、关闭按钮和 Esc 都可恢复正常显示。

预览期间由原版窗口暂停游戏并禁止保存。角度只写入模型实例上的转台与 `barrel_pitch_preview` 刚性节点，不改逻辑朝向、目标、命中或弹道，也不写进存档。俯仰轴由炮管后端和炮口轴线确定，三个独立后坐节点位于其下；共享资源模板保持原有结构，无需重新导出模型或烘焙贴图。

## 战斗表现

- `PhoenixTurretMuzzle`：炮口亮核、短喷焰、火星与烟气，生成在当次实际使用的炮口。
- 炮口与炮弹起点使用同一六十度地图投影，避免更换视角后闪光和弹丸从炮口旁边出现。
- `PhoenixTurretImpact`：命中亮光、火星和尘烟。
- `PhoenixTurretShell`：十八边旋转体网格，包含弹底、金属弹身、束带和尖头；独立烘焙贴图提供金属色与环向明暗。
- 炮弹位置沿三维抛物线插值，弹头朝向沿曲线切线变化。双层尾迹采样过去 10 Tick 的曲线，不把弧形轨迹简化成一条长直线；命中后尾迹再收细消散。
- 碰撞、掩体、命中率和伤害继续使用原版水平弹道判定。视觉弧线不会赋予越墙射击能力。
- 转向、后坐和弹道按游戏 Tick 推进；装饰粒子使用 RimUnity 的帧时钟，并响应游戏暂停。
- 三种原创合成炮声与一段低频余响位于 `1.7/Sounds/Phoenix/Turret`，由 `PhoenixTurretSounds.xml` 绑定，不使用原版开火音效。

炮塔共享现有颜色、法线/深度和后处理采集；高速炮弹直接提交三维网格与线条，避免为每发弹丸分配离屏目标。切图、地图移除和对象销毁时分别清理模型、粒子与尾迹。

## 源码与导出

游戏代码位于 `PhoenixRace/1.7/Source/PhoenixRaceLib/Buildings/Turrets`：

| 文件 | 内容 |
| --- | --- |
| `Building_PhoenixTurret` | 原版炮塔逻辑、限速转向与瞄准门槛 |
| `CompPhoenixTurretModel` | 刚性模型、独立炮管动画、炮口顺序与存档 |
| `Window_PhoenixTurretAngles` | 暂停时的视觉旋转、俯仰与姿态恢复 |
| `Projectile_PhoenixTurretShell` | 实体弹丸、抛物线路径和命中回调 |
| `PhoenixShellVisual` | 三维炮弹姿态、双层轨迹与消散 |
| `PhoenixTurretEffect` | 一次性炮口与命中粒子的生命周期 |

运行 `Tools/Turrets/Export-Turret.ps1` 会使用凤凰 Unity 工程的批处理编辑器导入 FBX、生成刚性预制体、烘焙贴图、生成特效和炮弹网格、合成 WAV，并导出到开发目录的 `1.7/Data/RimUnity`。它不进入播放模式，也不启动 RimWorld。

导出脚本按模块保存在此目录，重新导出会保留 Unity 资源 GUID。菜单图写入 `1.7/Textures/Things/Building/Security/PhoenixTripleTurret.png`；炮口开火与炮弹素材预览写入仓库 `Build` 目录。源模型若不再满足刚性绑定或三根独立炮管条件，会报告具体错误并停止。

游戏部署需要同时更新 DLL、炮塔 ThingDef、资源挂载 Def、SoundDef、模型 ShaderDef/GLSL、RimUnity 清单与其资源文件、建筑图标和 WAV；不能只复制 DLL。
