using System;
using System.Collections.Generic;
using RimUnity;
using UnityStubs.Rendering;
using Waylith.Rendering;
using Graphics = RimUnity.Graphics;
using Material = RimUnity.Material;
using MaterialPropertyBlock = RimUnity.MaterialPropertyBlock;
using Mesh = RimUnity.Mesh;
using Shader = RimUnity.Shader;
using Object = RimUnity.Object;

namespace PhoenixRaceLib.Rendering.Models;

//共享离屏提交与后处理，将三维实体转换成带表面明暗和外描边的地图影像。
internal sealed class PhoenixModelRenderContext : IDisposable
{
    private readonly Dictionary<int, PhoenixModelBuffers> buffers = new();
    private readonly List<int> expired = new();
    private readonly List<MeshDrawCommand> commands = new();
    private readonly Material geometry, composite, display;
    private readonly Mesh fullscreen, plane;
    private readonly MaterialPropertyBlock properties = new();
    private int maintenanceFrame = -1;

    //Shader 由 PhoenixRace 自己的 Def 注册，不依赖 SSR 的程序集或资源包。
    internal PhoenixModelRenderContext()
    {
        try
        {
            geometry = RequireMaterial("Phoenix_ModelGeometry");
            composite = RequireMaterial("Phoenix_ModelComposite");
            display = RequireMaterial("Phoenix_ModelDisplay");
            fullscreen = new Mesh
            {
                name = "PhoenixModelFullscreen",
                vertices = new[] { new Vector3(-1, -1, 0), new Vector3(3, -1, 0), new Vector3(-1, 3, 0) },
                uv = new[] { new Vector2(0, 0), new Vector2(2, 0), new Vector2(0, 2) },
                triangles = new[] { 0, 1, 2 }
            };
            plane = new Mesh
            {
                name = "PhoenixModelMapPlane",
                vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0), new Vector3(.5f, -.5f, 0) },
                uv = new[] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
        }
        catch { Dispose(); throw; }
    }

    //先完成颜色与几何采集，再将明暗和描边写入该模型独占的最终纹理。
    internal void Render(PhoenixModelSubmission submission, PhoenixModelFrame frame, float opacity)
    {
        var renderer = CameraTextureRenderer.Current
            ?? throw new InvalidOperationException("Waylith 尚未提供离屏模型渲染服务。");
        ReleaseUnused();
        int captureSize = frame.Resolution * frame.Supersampling;
        if (!buffers.TryGetValue(captureSize, out PhoenixModelBuffers target))
            buffers.Add(captureSize, target = new PhoenixModelBuffers(captureSize));
        target.LastFrame = Time.frameCount;
        frame.EnsureOutput();

        commands.Clear();
        Graphics.Capture(commands, () => submission.Draw(null));
        renderer.Render(target.Color, commands, frame.View, new UnityEngine.Rect(0, 0, 1, 1), UnityEngine.Color.clear);

        geometry.SetVector("_CaptureOrigin", Direction(frame.Origin));
        geometry.SetVector("_CaptureForward", Direction(frame.Forward));
        commands.Clear();
        Graphics.Capture(commands, () => submission.Draw(geometry));
        renderer.Render(target.Geometry, commands, frame.View, new UnityEngine.Rect(0, 0, 1, 1), UnityEngine.Color.clear);

        composite.SetTexture("_MainTex", target.ColorTexture);
        composite.SetTexture("_GeometryTex", target.GeometryTexture);
        composite.SetVector("_CaptureRight", Direction(frame.Right));
        composite.SetVector("_CaptureUp", Direction(frame.Up));
        composite.SetVector("_CaptureForward", Direction(frame.Forward));
        composite.SetColor("_OutlineColor", frame.Turret == null ? new Color(0.025f, 0.018f, 0.016f) : Color.black);
        composite.SetVector("_Capture", new Vector4(frame.HalfSize, 0.05f / (frame.HalfSize * 2f),
            frame.Supersampling, 0));
        commands.Clear();
        Graphics.Capture(commands, () => Graphics.DrawMesh(fullscreen, Matrix4x4.identity, composite, 0));
        renderer.Render(frame.Output, commands, frame.View, new UnityEngine.Rect(0, 0, 1, 1), UnityEngine.Color.clear);

        //参数块在提交时复制，多个模型共享材质也不会互相覆盖纹理与淡出值。
        properties.Clear();
        properties.SetTexture("_MainTex", frame.Texture);
        properties.SetFloat("_Opacity", opacity);
        Graphics.DrawMesh(plane, Matrix4x4.TRS(frame.DisplayCenter, frame.DisplayRotation, Vector3.one * (frame.HalfSize * 2f)),
            display, 0, 0, properties);
    }

    //每帧只清理一次闲置精度，避免缩放后长期保留峰值附件。
    private void ReleaseUnused()
    {
        if (maintenanceFrame == Time.frameCount) return;
        maintenanceFrame = Time.frameCount;
        expired.Clear();
        foreach (var pair in buffers)
            if (Time.frameCount - pair.Value.LastFrame > 120) expired.Add(pair.Key);
        foreach (int key in expired) { buffers[key].Dispose(); buffers.Remove(key); }
    }

    //缺失或编译失败的程序直接报告，不使用外观不确定的替代材质。
    private static Material RequireMaterial(string name)
    {
        Shader shader = Shader.Find(name);
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("模型 Shader 不可用：" + name);
        return new Material(shader);
    }

    //方向参数以完整向量上传，所有材质成员保持十六字节对齐。
    private static Vector4 Direction(Vector3 vector) => new(vector.x, vector.y, vector.z, 0);

    //释放上下文自有资源，源模型、源贴图和模板材质不属于此上下文。
    public void Dispose()
    {
        foreach (PhoenixModelBuffers target in buffers.Values) target.Dispose();
        buffers.Clear();
        if (geometry != null) Object.Destroy(geometry);
        if (composite != null) Object.Destroy(composite);
        if (display != null) Object.Destroy(display);
        if (fullscreen != null) Object.Destroy(fullscreen);
        if (plane != null) Object.Destroy(plane);
        commands.Clear();
    }
}
