using RimUnity;
using Verse;

namespace PhoenixRaceLib.Rendering.Models;

//沿用 SSR 的六十度正交取景、底座锚点和屏幕像素密度。
internal sealed class PhoenixTurretCapture
{
    internal static readonly Quaternion Direction = Quaternion.Euler(60, 0, 0);
    private static readonly Quaternion MapRotation = Quaternion.Euler(90, 0, 0);
    internal static readonly Vector3 ProjectionUp = Direction * Vector3.up;
    private readonly Transform root;
    private readonly Vector3 referenceScale, localCenter, localFloor;
    private readonly float referenceRadius;

    //以底座轴心记录固定包围球，炮管朝向和后坐不会改变画面尺度。
    internal PhoenixTurretCapture(Transform root, Bounds bounds)
    {
        this.root = root;
        referenceScale = root.lossyScale;
        Vector3 center = new(root.position.x, bounds.center.y, root.position.z);
        localCenter = root.InverseTransformPoint(center);
        localFloor = root.InverseTransformPoint(new Vector3(center.x, bounds.min.y, center.z));
        Vector3 offset = bounds.center - center;
        Vector3 reach = bounds.extents + new Vector3(Mathf.Abs(offset.x), Mathf.Abs(offset.y), Mathf.Abs(offset.z));
        referenceRadius = Mathf.Max(reach.magnitude, 0.01f);
    }

    //固定采集方向并把底座中心投回建筑位置，不随地图镜头改变炮塔侧壁厚度。
    internal bool Configure(Camera mapCamera, PhoenixModelFrame frame)
    {
        Vector3 scale = root.lossyScale;
        float ratio = Mathf.Max(Mathf.Abs(scale.x / referenceScale.x),
            Mathf.Max(Mathf.Abs(scale.y / referenceScale.y), Mathf.Abs(scale.z / referenceScale.z)));
        float radius = referenceRadius * ratio;
        frame.Center = root.TransformPoint(localCenter);
        frame.HalfSize = radius * 1.12f;
        frame.Rotation = Direction;
        frame.Right = Direction * Vector3.right;
        frame.Up = ProjectionUp;
        frame.Forward = Direction * Vector3.forward;
        frame.Origin = frame.Center - frame.Forward * (radius * 3f);
        Vector3 anchor = new(root.position.x, root.TransformPoint(localFloor).y, root.position.z);
        Vector3 relative = anchor - frame.Center;
        frame.DisplayCenter = new Vector3(anchor.x - Vector3.Dot(relative, frame.Right),
            AltitudeLayer.BuildingOnTop.AltitudeFor(), anchor.z - Vector3.Dot(relative, frame.Up));
        frame.DisplayRotation = MapRotation;
        if (!SelectResolution(mapCamera, frame)) return false;
        frame.SetView(Mathf.Max(0.001f, radius * 1.5f), radius * 4.5f);
        return true;
    }

    //按完整地图面片的屏幕边长选择 256～4096 档位，缩小时沿用 SSR 的滞回阈值。
    private static bool SelectResolution(Camera camera, PhoenixModelFrame frame)
    {
        Vector3 center = frame.DisplayCenter;
        float half = frame.HalfSize;
        Vector3 a = camera.WorldToScreenPoint(center + new Vector3(-half, 0, -half));
        Vector3 b = camera.WorldToScreenPoint(center + new Vector3(half, 0, -half));
        Vector3 c = camera.WorldToScreenPoint(center + new Vector3(half, 0, half));
        Vector3 d = camera.WorldToScreenPoint(center + new Vector3(-half, 0, half));
        Rect viewport = camera.pixelRect;
        if (a.z <= 0 || Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)) < viewport.xMin
            || Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)) > viewport.xMax
            || Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y)) < viewport.yMin
            || Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y)) > viewport.yMax) return false;
        float pixels = Mathf.Max(Mathf.Max(ScreenDistance(a, b), ScreenDistance(c, d)),
            Mathf.Max(ScreenDistance(a, d), ScreenDistance(b, c))) * 1.5f;
        int limit = Mathf.Min(4096, UnityEngine.SystemInfo.maxTextureSize);
        int wanted = 256;
        while (wanted < limit && wanted < pixels) wanted *= 2;
        wanted = Mathf.Min(wanted, limit);
        if (frame.Resolution > wanted && pixels > frame.Resolution * 0.375f) wanted = frame.Resolution;
        frame.SetResolution(wanted);
        return true;
    }

    //屏幕密度只使用二维距离，避免把相机深度算进清晰度。
    private static float ScreenDistance(Vector3 a, Vector3 b)
        => new Vector2(a.x - b.x, a.y - b.y).magnitude;
}
