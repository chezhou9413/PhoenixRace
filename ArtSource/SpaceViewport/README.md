# 凤凰族红台座舷窗 AssetBundle

美术来源为 `D:\QQ\红台座_UGUI`，采用移除白色底层后的半透明窗外版本。

## 资源和构建入口

- `RedPedestalUI/`：维护用的静态美术预制体、布局 JSON、16 个分层精灵和导入设置，6 个配色备选默认隐藏。
- Unity 工程：`E:\mygame\PhoenixRace`，Unity 版本 `2022.3.35f1c1`。
- 交付预制体：`Assets/Prefabs/PhoenixRace/PhoenixSpaceViewportUI.prefab`。
- 美术导入目录：`Assets/Art/UI/RedPedestalUI`。
- 编辑器构建源码：本项目 `Tools/SpaceViewport/Editor`，同步到 Unity 工程 `Assets/Editor/SpaceViewport`。
- 执行 `Tools/SpaceViewport/build_bundle.ps1` 同步资源、构建 Windows x64 AB，并覆盖本项目的 `PhoenixRace/1.6/AssetBundles/phoenixraceviewportui.ab` 和对应 `.manifest`。
- 构建日志与中间产物位于本项目 `Build/SpaceViewport`，不参与模组发布。

资源挂载继续使用 `PhoenixRaceViewportLord`，键名为 `PhoenixSpaceViewportUI`，包内路径保持 `assets/prefabs/phoenixrace/phoenixspaceviewportui.prefab`。独立 AB 包含预制体、分层图片、材质、全息球模型及相机，没有外部 AB 依赖。

## 游戏运行

界面美术与布局直接来自新 AB，代码不再加载松散 PNG 或创建美术层级。原背景和画框已经从交付预制体移除。

代码绑定 `ViewportContent/RedPedestalUI/PhoenixOrbitPlanetImage` 显示实时星球，绑定 `ViewportContent/RedPedestalUI/StarPlatform/Hologram_StarMap/PhoenixViewportOverlay` 显示当前世界的金色全息地块线框。静态星图 Image 在交付预制体中关闭，其源图仍保留供编辑。

点击屏幕底部原版主按钮栏中的“舰船”打开，再次点击关闭；G 不再作为界面入口。打开其他原版面板时关闭舰船界面。开发者镜头调试功能继续使用。

`ViewportContent` 按游戏 UI 缩放预留底部主按钮栏，并通过 RectMask2D 裁切美术。`RedPedestalUI` 按 9000:5000 比例等比覆盖内容区域；实时星球位于半透明窗外之后，保留 PNG 原始透明通道。台座、环境和悬浮面板在预制体中按职责命名。

AB 与重新编译的 `PhoenixRaceLib.dll` 需要配套使用。只执行资源构建和模组编译，不进入 Play Mode 或启动 RimWorld。
