using ChezhouLib.Assets;
using RimUnity;
using Verse;

namespace PhoenixRaceLib.Buildings.Turrets;

//真实网格炮弹配双层尾迹，轨迹采样直接来自弹丸的三维路径。
public sealed class PhoenixShellVisual : MonoBehaviour
{
    private Projectile_PhoenixTurretShell projectile;
    private Map map;
    private GameObject body;
    private LineRenderer[] trails;
    private float[] widths;
    private int finishedAt = -1;
    private const int TrailTicks = 10;
    private const int Samples = 14;

    //材质、网格和贴图共享，实例只持有自身的轨迹坐标。
    internal static PhoenixShellVisual Create(Projectile_PhoenixTurretShell projectile)
    {
        GameObject root = ClPrefab.Instantiate("Phoenix_TurretShell");
        var visual = root.AddComponent<PhoenixShellVisual>();
        visual.projectile = projectile;
        visual.map = projectile.Map;
        visual.body = root.transform.Find("Body").gameObject;
        visual.trails = root.GetComponentsInChildren<LineRenderer>(true);
        visual.widths = new float[visual.trails.Length];
        for (int i = 0; i < visual.trails.Length; i++)
        {
            visual.widths[i] = visual.trails[i].widthMultiplier;
            visual.trails[i].positionCount = Samples;
        }
        visual.UpdateFlight();
        return visual;
    }

    //与模型、线条绘制处于同一帧，暂停时位置与尾迹寿命保持不变。
    private void LateUpdate()
    {
        if (map != Find.CurrentMap || Current.ProgramState != ProgramState.Playing)
        {
            Destroy(gameObject);
            return;
        }
        if (projectile != null)
        {
            if (!projectile.Spawned) Finish();
            else UpdateFlight();
        }
        if (finishedAt < 0) return;
        float remaining = 1 - (Find.TickManager.TicksGame - finishedAt) / (float)TrailTicks;
        if (remaining <= 0) { Destroy(gameObject); return; }
        for (int i = 0; i < trails.Length; i++) trails[i].widthMultiplier = widths[i] * remaining;
    }

    //沿过去十个游戏 Tick 的抛物线采样，低帧率时也不会变成穿过弧线的长直线。
    private void UpdateFlight()
    {
        bool visible = !projectile.Fogged();
        body.SetActive(visible);
        foreach (var trail in trails) trail.enabled = visible;
        transform.position = projectile.ExactPosition.ToRim();
        UnityEngine.Quaternion angle = projectile.ExactRotation;
        body.transform.rotation = new Quaternion(angle.x, angle.y, angle.z, angle.w);
        float head = projectile.FlightFraction;
        float tail = Mathf.Max(0, head - TrailTicks / projectile.FlightTicks);
        for (int i = 0; i < Samples; i++)
        {
            Vector3 position = projectile.SamplePath(Mathf.Lerp(head, tail, i / (float)(Samples - 1))).ToRim();
            foreach (var trail in trails) trail.SetPosition(i, position);
        }
    }

    //保存命中前最后一段轨迹，关闭网格后逐渐收细尾迹。
    internal void Finish()
    {
        if (finishedAt >= 0) return;
        UpdateFlight();
        projectile = null;
        body.SetActive(false);
        finishedAt = Find.TickManager.TicksGame;
    }
}
