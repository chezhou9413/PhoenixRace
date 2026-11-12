using System;
using System.Collections.Generic;
using ChezhouLib.Assets;
using PhoenixRaceLib.Rendering.Models;
using RimUnity;
using RimWorld;
using Verse;
using HostVector = UnityEngine.Vector3;

namespace PhoenixRaceLib.Buildings.Turrets;

//让底座保持不动，转台刚性跟随原版炮塔角度；模型仅在当前地图可见时创建。
public sealed class CompPhoenixTurretModel : ThingComp
{
    private GameObject prefab, instance;
    private Transform yaw, pitch;
    private Transform[] barrels;
    private Quaternion restRotation;
    private Vector3[] restBarrels;
    private Vector3[] muzzleOffsets;
    private int nextMuzzle;
    private int[] shotTicks;
    private float currentYaw;
    private float previewYaw, previewPitch;
    private bool previewing;
    private bool visualFailed;

    private CompProperties_PhoenixTurretModel Props => (CompProperties_PhoenixTurretModel)props;
    internal float CurrentYaw => currentYaw;

    //新建炮塔使用建筑方向，读档保留上次的真实转台角度。
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!respawningAfterLoad) currentYaw = parent.Rotation.AsAngle;
    }

    //原版完成索敌后按游戏 Tick 限速转向，连射期间也持续跟踪目标。
    internal void TickAim(float desiredAngle)
    {
        float step = Props.rotationSpeed / 60f;
        currentYaw = Mathf.Repeat(currentYaw + Mathf.Clamp(Mathf.DeltaAngle(currentYaw, desiredAngle), -step, step), 360f);
        if (instance != null) UpdatePose(parent.DrawPos);
    }

    //逻辑瞄准和模型使用同一个角度，尚未转正时延长预热。
    internal bool IsAimed(LocalTargetInfo target) => target.IsValid && Mathf.Abs(Mathf.DeltaAngle(currentYaw, AngleTo(target))) <= 2f;

    //移动目标使用当前位置，格子目标使用格子中心。
    internal float AngleTo(LocalTargetInfo target)
        => ((target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted()) - parent.DrawPos).AngleFlat();

    //显示时才创建实例，远离视野的炮塔仍可直接使用模板中的炮口坐标开火。
    public override void DrawAt(HostVector drawLoc, bool flip = false)
    {
        if (!parent.Spawned || parent.Map != Find.CurrentMap || parent.Fogged() || visualFailed) return;
        try
        {
            if (instance == null)
            {
                LoadTemplate();
                instance = ClPrefab.Instantiate(prefab);
                yaw = FindNode(instance, Props.yawPath);
                restRotation = yaw.localRotation;
                barrels = new Transform[Props.barrelPaths.Count];
                restBarrels = new Vector3[barrels.Length];
                for (int i = 0; i < barrels.Length; i++)
                    barrels[i] = FindNode(instance, Props.barrelPaths[i]);
                CreatePitchPivot();
                for (int i = 0; i < barrels.Length; i++)
                {
                    barrels[i].SetParent(pitch, true);
                    restBarrels[i] = barrels[i].localPosition;
                }
                PhoenixModelComponent.Attach(instance, parent.Map, parent).UseTurretView();
            }
            UpdatePose(drawLoc);
        }
        catch (Exception error)
        {
            visualFailed = true;
            ReleaseModel();
            Log.Error("[PhoenixRace] 炮塔模型初始化失败：" + parent.def.defName + "\n" + error);
        }
    }

    //以模型炮口作为弹丸起点，三个炮口按实际射击次数轮流使用。
    internal HostVector FireFromMuzzle()
    {
        LoadTemplate();
        Vector3 offset = muzzleOffsets[nextMuzzle] - Vector3.forward * RecoilDistance(nextMuzzle);
        Quaternion rotation = Quaternion.AngleAxis(currentYaw, Vector3.up);
        offset = rotation * offset;
        //将炮口与六十度采集画面投影到同一位置，预览角度不参与发射计算。
        offset.z = Vector3.Dot(offset, PhoenixTurretCapture.ProjectionUp);
        Vector3 position = parent.DrawPos.ToRim() + offset;
        shotTicks[nextMuzzle] = Find.TickManager.TicksGame;
        nextMuzzle = (nextMuzzle + 1) % muzzleOffsets.Length;
        if (instance != null) UpdatePose(parent.DrawPos);
        if (parent.Map == Find.CurrentMap && !parent.Fogged())
            PhoenixTurretEffect.Spawn("Phoenix_TurretMuzzle", parent.Map, position,
                Quaternion.Euler(30, 0, 0) * rotation);
        return position.ToHost();
    }

    //同一模板提供节点与尺寸，逻辑坐标不依赖模型是否在屏幕内。
    private void LoadTemplate()
    {
        if (prefab != null) return;
        GameObject loaded = ClPrefab.Load(Props.prefabKey);
        FindNode(loaded, Props.yawPath);
        foreach (string path in Props.barrelPaths) FindNode(loaded, path);
        var offsets = new Vector3[Props.muzzlePaths.Count];
        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = loaded.transform.InverseTransformPoint(FindNode(loaded, Props.muzzlePaths[i]).position);
        muzzleOffsets = offsets;
        shotTicks = new int[offsets.Length];
        Array.Fill(shotTicks, -999999);
        prefab = loaded;
    }

    //模板节点缺失时保留具体路径，便于定位资源和 XML 的不一致。
    private static Transform FindNode(GameObject root, string path)
        => root.transform.Find(path) ?? throw new InvalidOperationException("炮塔预制体缺少节点：" + root.name + "/" + path);

    //由炮管后端和炮口轴线定位刚性俯仰轴，不改动共享模板中的逻辑炮口。
    private void CreatePitchPivot()
    {
        MeshFilter filter = barrels[0].GetComponentInChildren<MeshFilter>()
            ?? throw new InvalidOperationException("炮塔炮管缺少网格：" + Props.barrelPaths[0]);
        Bounds bounds = filter.sharedMesh.bounds;
        Vector3 rear = yaw.InverseTransformPoint(filter.transform.TransformPoint(
            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z)));
        Vector3 muzzle = yaw.InverseTransformPoint(FindNode(instance, Props.muzzlePaths[0]).position);
        pitch = new GameObject("barrel_pitch_preview").transform;
        pitch.SetParent(yaw, false);
        pitch.localPosition = new Vector3(0, muzzle.y, rear.z);
    }

    //调试只覆盖节点姿态，真实转角、瞄准判定和发射数据始终独立。
    private void UpdatePose(HostVector position)
    {
        instance.transform.position = position.ToRim();
        yaw.localRotation = Quaternion.AngleAxis(previewing ? previewYaw : currentYaw, Vector3.up) * restRotation;
        pitch.localRotation = Quaternion.AngleAxis(previewing ? -previewPitch : 0, Vector3.right);
        for (int i = 0; i < barrels.Length; i++)
            barrels[i].localPosition = restBarrels[i] - Vector3.forward * RecoilDistance(i);
    }

    //拖动滑条立即刷新模型，游戏暂停时也能检查刚性节点姿态。
    internal void SetPreviewAngles(float horizontal, float elevation)
    {
        previewing = true;
        previewYaw = horizontal;
        previewPitch = elevation;
        if (instance != null) UpdatePose(parent.DrawPos);
    }

    //关闭调试面板后继续显示当前逻辑转角，预览数据不写入存档。
    internal void EndPreview()
    {
        previewing = false;
        if (instance != null) UpdatePose(parent.DrawPos);
    }

    //开发者模式下为选中的炮塔提供角度预览入口。
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Prefs.DevMode || !parent.Spawned) yield break;
        yield return new Command_Action
        {
            defaultLabel = "调试：炮塔角度",
            defaultDesc = "暂停并预览水平旋转与炮管俯仰，只改变外观。关闭面板恢复正常姿态。",
            icon = parent.def.uiIcon,
            groupable = false,
            action = () => Find.WindowStack.Add(new Window_PhoenixTurretAngles(this))
        };
    }

    //快速后坐后平滑复进，三根炮管各自保留射击时间。
    private float RecoilDistance(int barrel)
    {
        float phase = Mathf.Clamp01((Find.TickManager.TicksGame - shotTicks[barrel]) / 18f);
        return .14f * (1 - phase * phase * (3 - 2 * phase));
    }

    //保存炮口顺序，模型实例和短暂后坐在读档后重新生成。
    public override void PostExposeData()
    {
        Scribe_Values.Look(ref nextMuzzle, "phoenixNextMuzzle", 0);
        Scribe_Values.Look(ref currentYaw, "phoenixTurretYaw", 0);
    }

    //拆除或搬运时销毁场景实例，同时释放该模型的离屏纹理。
    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish) => ReleaseModel();

    //建筑被摧毁时不留下独立于 Thing 的渲染对象。
    public override void PostDestroy(DestroyMode mode, Map previousMap) => ReleaseModel();

    //地图移除后释放仍在场景中的炮塔。
    public override void Notify_MapRemoved() => ReleaseModel();

    //地图迁移前解绑旧地图，目标地图首次显示时重新创建。
    public override void PreSwapMap() => ReleaseModel();

    //共享模板和贴图由资源系统管理，只释放本建筑的实例。
    private void ReleaseModel()
    {
        if (instance != null) RimUnity.Object.Destroy(instance);
        instance = null;
        yaw = null;
        pitch = null;
        barrels = null;
        restBarrels = null;
    }
}
