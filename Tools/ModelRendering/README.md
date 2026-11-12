# 凤凰实体模型与后处理描边

目标为 RimWorld 1.7 / Waylith。当前接入空袭飞机 `PHFEIJI1` 和三联装炮塔 `Phoenix_TripleTurret`。飞机使用已有 RimUnity 资源；炮塔的导出、建造与战斗表现见 [炮塔说明](../Turrets/README.md)。

## 参考项目分析

核对的是本地 `CP的太阳系共和国/Solar-System-Republic-Framework` 的 `1.7test` 分支，提交 `3a1729d`；核心资源仓库对应 `1.7TEST`，提交 `48421bd`。两个分支的最新提交均为 2026-10-20。

该分支的核心已接入 RimUnity，但 `FS_SSR_Combat/FS_SSR_Combat.csproj` 仍使用 `net48` 和 1.6 输出路径。炮塔仍通过 Unity Camera、CommandBuffer 和 AssetBundle Shader 完成离屏绘制，不能把这一部分当作当前 Waylith 下可直接引用的渲染库。

炮塔的主要分工如下：

| 部分 | 工作 |
| --- | --- |
| `OffscreenTurretComp` | 保留逻辑模型和转轴、炮口、后坐节点，关闭实体的自动绘制 |
| `OffscreenTurretGraphic` | 接收原版可见性结果，同步地图位置并提交本帧炮塔 |
| `TurretMeshSubmission` | 从同一模型姿态提交颜色和表面几何，避免复制动画对象 |
| `OffscreenTurretRenderer` | 多座炮塔共享采集相机和按精度缓存的中间附件 |
| `TurretCaptureFrame` | 保存单座炮塔的最终纹理、取景和地面锚点 |
| 后处理 Shader | 根据颜色覆盖率生成外轮廓，根据表面信息计算明暗与接触遮蔽 |
| `MapProjectionPlane` | 用带透明度的结果面片显示模型，交给原版地图完成后续覆盖 |

参考分支采用约 60 度固定俯视、256–4096 输出档位，中远景两倍采集。几何缓冲保存观察空间位置和编码法线，默认不绘制内部结构黑线；模型逻辑位置和炮口不会被远处采集偏移改变。

凤凰按上述处理流程独立实现，使用宿主 `ICameraTextureRenderer` 真实采集，不引入 SSR DLL、资源包或旧 Unity 命令缓冲。

## 凤凰的模块

源码位于 `PhoenixRace/1.7/Source/PhoenixRaceLib/Rendering/Models`。

| 文件 | 工作 |
| --- | --- |
| `PhoenixModelComponent.cs` | 挂在模型根节点上，绑定地图、透明度及启停生命周期 |
| `PhoenixModelSubmission.cs` | 接管启用的 MeshRenderer，读取当前节点矩阵、子网格和材质 |
| `PhoenixModelFrame.cs` | 按屏幕面积取景，持有该实例独占的最终纹理 |
| `PhoenixTurretCapture.cs` | 炮塔固定六十度俯视、底座锚点与 SSR 清晰度档位 |
| `PhoenixModelBuffers.cs` | 创建共享颜色及浮点几何附件，集中释放 GPU 目标 |
| `PhoenixModelRenderContext.cs` | 执行两遍采集、后处理和最终地图面片提交 |

```text
同一个 RimUnity 模型实例
  ├─ 节点、位置、缩放、飞行姿态：原业务更新
  ├─ MeshRenderer：关闭自动绘制，交给模型组件
  │    ├─ 源材质 → 颜色附件
  │    └─ 几何材质 → 世界法线 + 相机距离附件
  │           ↓
  │    表面明暗 + 轻量接触遮蔽 + 外轮廓
  │           ↓
  │    实例独占纹理 → 预乘透明地图面片
  └─ 粒子、拖尾：继续由 RimUnity 原路径绘制
```

飞机使用地图相机的观察方向。采集和显示面片处于模型真实中心，保留俯冲、侧倾与翻滚的视觉方向。模型自身遮挡由采集深度处理，与地图其他对象的遮挡使用显示面片中心深度；当前没有逐像素重建地图深度。

飞机输出档位为 128、256、512、1024，低于 1024 时以两倍宽高采集。炮塔沿用 SSR `1.7test` 的 60° 正交俯视、1.12 取景余量、256～4096 输出档位与 0.375 缩小滞回阈值，低于 2048 时以两倍宽高采集并平均四个后处理样本。两者目标密度均为每个屏幕像素 1.5 个纹理像素，不使用 Mipmap。

炮塔按底座轴心固定包围球，并将底座投回建筑所在位置；水平旋转、炮管俯仰和后坐不会改变取景范围。最终面片位于 `BuildingOnTop` 地面层。炮塔外轮廓为纯黑，宽度与 SSR 一致为 0.05 地图单位，不绘制内部结构黑线。飞机继续使用原有描边颜色。几何缓冲使用 RGBAHalf 和点采样，避免插值出不存在的表面。

同尺寸采集附件可以供模型轮流使用，最终纹理逐实例保留。连续 120 个活动绘制帧未使用的精度附件会释放；最后一个活动模型进入池后立即释放共享上下文。离屏模型释放最终纹理，读档和退出游戏由 RimUnity 场景生命周期销毁实例。

目前接管 MeshRenderer 实体模型。当前飞机材质为 `RimUnity_Unlit`，后处理保留其贴图和颜色；几何裁切读取 `_MainTex`、`_MainTex_ST`、`_Color`。任意自定义顶点变形、多层透明材质、蒙皮模型需要对应的几何采集支持，不能仅挂组件就承诺一致效果。

## 接入方式

模型创建或从池中取出后绑定一次：

```csharp
PhoenixModelComponent model = PhoenixModelComponent.Attach(instance, map);
model.Opacity = 1f;
```

业务继续操作 `instance.transform` 和子节点。淡出更新 `model.Opacity`；`instance.SetActive(false)` 会停止采集并释放实例输出，销毁实例会释放组件。不要在接管后重新启用实体 MeshRenderer，否则会同时出现原模型和结果面片。

建筑使用 `PhoenixModelComponent.Attach(instance, map, parentThing)` 绑定实体所有者，迷雾或移除状态下停止采集。炮塔在默认姿态下调用 `UseTurretView()` 固定取景。五块刚性网格共享一个烘焙材质，三个炮管分别更新局部后坐位置；开发者角度预览通过实例上的刚性俯仰节点实现。

空袭入口位于 `GameComponent_AirStrikeBombingManager.EnsureMissionVisual`。当前地图之外的任务只推进飞行和投弹数据，切回后按当前进度重新创建视觉。投弹伤害、路线、翻滚、缩放与原阴影算法保持既有逻辑。

## 构建与交付

`compile_modSelf.bat` 编译 1.7 C#。本地 GLSL 使用现有编译工具检查，不启动游戏或图形设备：

```powershell
& 'E:\RimModDev\ChezhouLib\Tools\Build\Compile-WaylithShaders.ps1' `
  -ShaderRoot 'E:\RimModDev\PhoenixRace\PhoenixRace\1.7\Shaders\PhoenixModel'
```

部署本次功能必须同时包含：

- `1.7/Assemblies/PhoenixRaceLib.dll`
- `1.7/Defs/ShaderDefs/PhoenixModelShaders.xml`
- `1.7/Shaders/PhoenixModel/` 中的六个 GLSL 文件

模型继续使用已有 `1.7/Data/RimUnity` 资源。缺少后处理程序时会报告具体 Shader 名称并停用该实例，不会自动换成原版材质。

离线编译不等同于游戏画面验收。画面核对涵盖不同缩放下的轮廓、炮塔旋转和俯仰预览、飞机俯冲与翻滚、尾焰遮挡、末段淡出、多模型同屏和切换地图后的资源释放。
