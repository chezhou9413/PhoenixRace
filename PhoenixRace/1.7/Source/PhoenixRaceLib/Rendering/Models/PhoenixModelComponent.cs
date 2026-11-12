using System;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.Rendering.Models;

//把实体模型交给离屏渲染，原有节点、动画、粒子和拖尾继续使用同一实例。
public sealed class PhoenixModelComponent : MonoBehaviour
{
    private static PhoenixModelRenderContext context;
    private static int owners;
    private PhoenixModelSubmission submission;
    private readonly PhoenixModelFrame frame = new();
    private Map map;
    private Thing owner;
    private bool registered;

    public float Opacity { get; set; } = 1f;

    //池中模型只初始化一次网格，复用时更新所属地图和透明度。
    public static PhoenixModelComponent Attach(GameObject root, Map ownerMap, Thing owner = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(ownerMap);
        var component = root.GetComponent<PhoenixModelComponent>() ?? root.AddComponent<PhoenixModelComponent>();
        if (component.submission == null)
        {
            component.submission = new PhoenixModelSubmission(root);
        }
        component.map = ownerMap;
        component.owner = owner;
        component.Opacity = 1f;
        component.enabled = true;
        component.OnEnable();
        return component;
    }

    //在默认刚性姿态下固定炮塔取景，后续旋转和俯仰只改变模型节点。
    internal void UseTurretView()
    {
        if (!submission.TryGetBounds(out Bounds bounds))
            throw new InvalidOperationException("炮塔缺少可采集的实体网格：" + gameObject.name);
        frame.Turret = new PhoenixTurretCapture(transform, bounds);
    }

    //池中模型重新显示时加入共享渲染生命周期，初次添加组件时尚未绑定网格。
    private void OnEnable()
    {
        if (submission == null || registered || !isActiveAndEnabled) return;
        registered = true;
        owners++;
    }

    //在 RimUnity 完成节点更新后采集，提交发生在宿主冻结本帧绘制队列之前。
    private void LateUpdate()
    {
        if (submission == null) return;
        if (map != Find.CurrentMap || Current.ProgramState != ProgramState.Playing || Opacity <= 0f
            || (owner != null && (!owner.Spawned || owner.Fogged())))
        {
            frame.ReleaseOutput();
            return;
        }
        try
        {
            Camera camera = Camera.main;
            if (camera == null || !submission.TryGetBounds(out Bounds bounds)
                || !frame.Configure(camera, bounds))
            {
                frame.ReleaseOutput();
                return;
            }
            context ??= new PhoenixModelRenderContext();
            context.Render(submission, frame, Mathf.Clamp01(Opacity));
        }
        catch (Exception error)
        {
            enabled = false;
            Log.Error("[PhoenixRace] 模型离屏渲染失败，已停用该实例：" + gameObject.name + "\n" + error);
        }
    }

    //进入对象池时释放实例输出，共享采集附件留给其他活动模型。
    private void OnDisable()
    {
        frame.ReleaseOutput();
        if (!registered) return;
        registered = false;
        if (--owners == 0)
        {
            context?.Dispose();
            context = null;
        }
    }

    //场景清理会销毁组件，最后一个模型同时释放共享采集资源。
    private void OnDestroy()
    {
        OnDisable();
        if (submission == null) return;
        submission.Dispose();
        submission = null;
    }
}
