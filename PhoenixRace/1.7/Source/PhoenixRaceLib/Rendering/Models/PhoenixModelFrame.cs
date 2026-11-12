using RimUnity;
using CameraView = Waylith.Rendering.CameraView;
using HostTexture = UnityEngine.RenderTexture;

namespace PhoenixRaceLib.Rendering.Models;

//保存模型的采集方向、地图投影与最终纹理，炮塔使用固定俯视取景。
internal sealed class PhoenixModelFrame
{
    internal HostTexture Output { get; private set; }
    internal Texture Texture { get; private set; }
    internal Vector3 Center, Origin, Right, Up, Forward, DisplayCenter;
    internal Quaternion Rotation, DisplayRotation;
    internal float HalfSize;
    internal CameraView View;
    internal PhoenixTurretCapture Turret;
    private int resolution;

    //按实际屏幕面积选择精度，远景降低采样并用滞回避免缩放边界反复分配。
    internal bool Configure(Camera camera, Bounds bounds)
    {
        if (Turret != null) return Turret.Configure(camera, this);
        Center = bounds.center;
        HalfSize = Mathf.Max(bounds.extents.magnitude * 1.08f, 0.01f);
        Rotation = camera.transform.rotation;
        Right = camera.transform.right;
        Up = camera.transform.up;
        Forward = camera.transform.forward;
        DisplayCenter = Center;
        DisplayRotation = Rotation;
        Vector3 screen = camera.WorldToScreenPoint(Center);
        if (screen.z <= 0) return false;
        float width = (camera.WorldToScreenPoint(Center + Right * HalfSize) - screen).magnitude;
        float height = (camera.WorldToScreenPoint(Center + Up * HalfSize) - screen).magnitude;
        Rect viewport = camera.pixelRect;
        if (screen.x + width < viewport.xMin || screen.x - width > viewport.xMax
            || screen.y + height < viewport.yMin || screen.y - height > viewport.yMax) return false;
        float pixels = Mathf.Max(width, height) * 2f * 1.5f;
        int wanted = 128;
        while (wanted < 1024 && wanted < pixels) wanted *= 2;
        if (resolution == 0 || wanted > resolution || pixels < resolution * 0.35f)
        {
            if (wanted != resolution)
            {
                SetResolution(wanted);
            }
        }
        Origin = Center - Forward * (HalfSize * 3f);
        SetView(HalfSize * 0.1f, HalfSize * 6f);
        return true;
    }

    //为固定俯视和跟随镜头两种取景共用原生相机矩阵。
    internal void SetView(float near, float far)
    {
        Matrix4x4 view = Matrix4x4.Scale(new Vector3(1, 1, -1))
            * Matrix4x4.TRS(Origin, Rotation, Vector3.one).inverse;
        //沿用 RimUnity 相机的 Vulkan Y 方向与反向深度约定。
        Matrix4x4 projection = new()
        {
            m00 = 1f / HalfSize, m11 = -1f / HalfSize,
            m22 = 1f / (far - near), m23 = far / (far - near), m33 = 1
        };
        View = new CameraView(Matrix4x4.ToNumerics(view), Matrix4x4.ToNumerics(projection),
            Matrix4x4.ToNumerics(projection * view), -1);
    }

    internal int Resolution => resolution;
    internal int Supersampling => resolution < (Turret == null ? 1024 : 2048) ? 2 : 1;

    //仅在档位变化时重建实例输出，采集附件按实际尺寸共享。
    internal void SetResolution(int wanted)
    {
        if (wanted == resolution) return;
        ReleaseOutput();
        resolution = wanted;
    }

    //每个可见模型独占最终结果，共享缓冲被下一架飞机覆盖时不影响已排队画面。
    internal void EnsureOutput()
    {
        if (Output != null) return;
        Output = PhoenixModelBuffers.Create("PhoenixModelOutput", resolution, UnityEngine.RenderTextureFormat.ARGB32, 0);
        Texture = RimUnity.Texture.FromNative(Output.CoreTexture);
    }

    //离屏或进入对象池后释放纹理，下一次显示重新按屏幕面积取景。
    internal void ReleaseOutput()
    {
        if (Texture != null) Object.Destroy(Texture);
        if (Output != null) { Output.Release(); UnityEngine.Object.Destroy(Output); }
        Texture = null;
        Output = null;
        resolution = 0;
    }
}
