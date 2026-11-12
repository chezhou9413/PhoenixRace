using System;
using System.Collections.Generic;
using RimUnity;

namespace PhoenixRaceLib.Rendering.Models;

//缓存实体网格并关闭它们的自动绘制，采集时读取当前节点矩阵和材质。
internal sealed class PhoenixModelSubmission : IDisposable
{
    private readonly List<MeshRenderer> renderers = new();
    private readonly MaterialPropertyBlock properties = new();

    //仅接管启用的 MeshRenderer，不把粒子和拖尾写进实体轮廓。
    internal PhoenixModelSubmission(GameObject root)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.enabled) continue;
            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || mesh.subMeshCount == 0 || renderer.sharedMaterials.Length == 0)
                throw new InvalidOperationException("实体模型缺少网格或材质：" + renderer.gameObject.name);
            foreach (Material material in renderer.sharedMaterials)
                if (material == null || material.shader == null || !material.shader.isSupported)
                    throw new InvalidOperationException("实体模型材质不可用：" + renderer.gameObject.name);
            renderers.Add(renderer);
        }
        if (renderers.Count == 0) throw new InvalidOperationException("模型没有启用的实体网格：" + root.name);
        foreach (MeshRenderer renderer in renderers) renderer.enabled = false;
    }

    //包围盒随节点动画变化，颜色和几何两遍使用同一帧的模型姿态。
    internal bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
            if (found) bounds.Encapsulate(renderer.bounds);
            else { bounds = renderer.bounds; found = true; }
        }
        return found;
    }

    //颜色沿用源材质；几何通道保留相同贴图裁切，避免透明区域出现黑色方框。
    internal void Draw(Material geometry)
    {
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                int submesh = Math.Min(i, mesh.subMeshCount - 1);
                properties.Clear();
                if (geometry != null)
                {
                    properties.SetTexture("_MainTex", source.HasProperty("_MainTex") && source.mainTexture != null
                        ? source.mainTexture : Texture2D.whiteTexture);
                    properties.SetColor("_Color", source.HasProperty("_Color") ? source.color : Color.white);
                    Vector2 scale = source.HasProperty("_MainTex_ST") ? source.mainTextureScale : Vector2.one;
                    Vector2 offset = source.HasProperty("_MainTex_ST") ? source.mainTextureOffset : Vector2.zero;
                    properties.SetVector("_MainTex_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
                }
                Graphics.DrawMesh(mesh, renderer.transform.localToWorldMatrix, geometry ?? source,
                    renderer.gameObject.layer, submesh, properties);
            }
        }
    }

    //只恢复本组件接管的网格，不销毁预制体共享的材质与模型。
    public void Dispose()
    {
        foreach (MeshRenderer renderer in renderers)
            if (renderer != null) renderer.enabled = true;
        renderers.Clear();
    }
}
