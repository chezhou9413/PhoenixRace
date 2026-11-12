using System;
using RimUnity;
using HostTexture = UnityEngine.RenderTexture;
using Object = RimUnity.Object;

namespace PhoenixRaceLib.Rendering.Models;

//同一精度的模型共用颜色和几何附件，浮点几何保存世界法线与相机距离。
internal sealed class PhoenixModelBuffers : IDisposable
{
    internal HostTexture Color { get; }
    internal HostTexture Geometry { get; }
    internal Texture ColorTexture { get; }
    internal Texture GeometryTexture { get; }
    internal int LastFrame;

    //中远景以两倍宽高采集，几何采用点采样，避免背景与法线相互插值。
    internal PhoenixModelBuffers(int size)
    {
        try
        {
            Color = Create("PhoenixModelColor", size, UnityEngine.RenderTextureFormat.ARGB32, 24);
            Geometry = Create("PhoenixModelGeometry", size, UnityEngine.RenderTextureFormat.ARGBHalf, 24);
            Geometry.filterMode = UnityEngine.FilterMode.Point;
            ColorTexture = Texture.FromNative(Color.CoreTexture);
            GeometryTexture = Texture.FromNative(Geometry.CoreTexture);
        }
        catch { Dispose(); throw; }
    }

    //使用宿主真正支持的颜色格式，附件不生成 Mipmap 或依赖兼容层 MSAA。
    internal static HostTexture Create(string name, int size, UnityEngine.RenderTextureFormat format, int depth)
    {
        return new HostTexture(size, size, depth, format)
        {
            name = name, filterMode = UnityEngine.FilterMode.Bilinear,
            wrapMode = UnityEngine.TextureWrapMode.Clamp, useMipMap = false, antiAliasing = 1
        };
    }

    //纹理包装不拥有宿主附件，分别解除引用并释放实际 GPU 目标。
    public void Dispose()
    {
        if (ColorTexture != null) Object.Destroy(ColorTexture);
        if (GeometryTexture != null) Object.Destroy(GeometryTexture);
        if (Color != null) { Color.Release(); UnityEngine.Object.Destroy(Color); }
        if (Geometry != null) { Geometry.Release(); UnityEngine.Object.Destroy(Geometry); }
    }
}
