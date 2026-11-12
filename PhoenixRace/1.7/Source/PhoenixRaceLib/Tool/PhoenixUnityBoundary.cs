using RimUnity;
using Verse;

namespace PhoenixRaceLib;

//在原版坐标、存档和 RimUnity 场景之间转换实际使用的数据。
internal static class PhoenixUnityBoundary
{
    internal static Vector3 ToRim(this UnityEngine.Vector3 value) => new(value.x, value.y, value.z);
    internal static Vector2 ToRim(this UnityEngine.Vector2 value) => new(value.x, value.y);
    internal static UnityEngine.Vector3 ToHost(this Vector3 value) => new(value.x, value.y, value.z);

    //仍以原版向量格式存档，不把场景类型交给 Scribe 的类型解析器。
    internal static void Look(ref Vector3 value, string label)
    {
        UnityEngine.Vector3 stored = value.ToHost();
        Scribe_Values.Look(ref stored, label);
        value = stored.ToRim();
    }

    //飞机投影沿用一个独立的 XY 平面，随后由业务设置旋转与比例。
    internal static GameObject CreateShadowQuad()
    {
        var obj = new GameObject("PhoenixAirStrikeShadow");
        var mesh = new Mesh
        {
            vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(.5f,.5f,0), new Vector3(-.5f,.5f,0) },
            uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) },
            triangles = new[] { 0,2,1,0,3,2 }
        };
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>();
        return obj;
    }
}
