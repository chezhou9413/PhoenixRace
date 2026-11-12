using System;
using System.IO;
using RimUnity.Editor;
using UnityEditor;
using UnityEngine;

//将凤凰的特效和舷窗资源导出到同一个 RimUnity 清单。
public static class PhoenixRimUnityExport
{
    private const string ModRoot = "E:/RimModDev/PhoenixRace/PhoenixRace";

    //两个资源工程分别执行此入口，逻辑路径保持独立。
    public static void Export()
    {
        EditorPrefs.SetString("RimUnity.ShaderTool", "E:/RimModDev/RimUnity/Tools/bin/ShaderTool/RimUnity.ShaderTool.exe");
        EditorPrefs.SetString("RimUnity.GameRoot", "E:/steam/steamapps/common/RimWorld");
        if (Directory.Exists("Assets/prefab/PhoenixRace"))
        {
            foreach (string name in new[] { "PhoenixShid", "taiyanghongzha", "hongzhaExplosion", "PHFEIJI1" })
                Write("Assets/prefab/PhoenixRace/" + name + ".prefab", "Bundles/phoenixraceobj/Assets/prefab/PhoenixRace/" + name + ".prefab");
            foreach (string name in new[] { "SingleImageGlitch", "ShieldDissolve", "ShieldEnergyFlow" })
                Write("Assets/Shader/" + name + ".shader", "Shaders/" + name);
            Write("Assets/image/Noise.png", "Textures/Noise");
        }
        else ExportViewport();
        Debug.Log("PHOENIX_RIMUNITY_EXPORT_OK");
    }

    //全息三维场景独立于 Canvas 导出，游戏打开界面时再组合。
    private static void ExportViewport()
    {
        Directory.CreateDirectory("Assets/PhoenixRimUnityExport");
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/PhoenixRace/PhoenixSpaceViewportUI.prefab");
        try
        {
            var world = root.transform.Find("PhoenixHologramWorld").gameObject;
            PrefabUtility.SaveAsPrefabAsset(world,"Assets/PhoenixRimUnityExport/PhoenixHologramWorld.prefab");
            UnityEngine.Object.DestroyImmediate(world);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/PhoenixRimUnityExport/PhoenixSpaceViewportUI.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Write("Assets/PhoenixRimUnityExport/PhoenixHologramWorld.prefab", "Bundles/phoenixraceviewportui/Assets/Prefabs/PhoenixRace/PhoenixHologramWorld.prefab");
        Write("Assets/PhoenixRimUnityExport/PhoenixSpaceViewportUI.prefab", "Bundles/phoenixraceviewportui/Assets/Prefabs/PhoenixRace/PhoenixSpaceViewportUI.prefab");
    }

    //缺失或不支持的资源中止导出，不用空预制体替代。
    private static void Write(string source, string logical)
    {
        var asset = AssetDatabase.LoadMainAssetAtPath(source);
        if (asset == null) throw new FileNotFoundException(source);
        //飞机未使用烘焙光照，将内置移动端无光照材质映射为可导出的同类 Shader。
        var changed = new System.Collections.Generic.Dictionary<Material, Shader>();
        var trails = new System.Collections.Generic.Dictionary<TrailRenderer, LineTextureMode>();
        var meshes = new System.Collections.Generic.Dictionary<MeshFilter, Mesh>();
        bool linesChanged = false;
        byte[] originalPrefab = asset is GameObject ? File.ReadAllBytes(source) : null;
        try
        {
            if (asset is GameObject prefab)
            {
                foreach (var line in prefab.GetComponentsInChildren<LineRenderer>(true))
                {
                    linesChanged = true;
                    if (line.loop && line.positionCount > 0)
                    {
                        Vector3 first = line.GetPosition(0);
                        line.positionCount++;
                        line.SetPosition(line.positionCount-1,first);
                        line.loop = false;
                    }
                    line.textureMode = LineTextureMode.Stretch;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                }
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && filter.sharedMesh.GetTopology(0) == MeshTopology.Lines)
                    {
                        meshes.Add(filter, filter.sharedMesh);
                        filter.sharedMesh = RibbonMesh(filter.sharedMesh);
                    }
                foreach (var trail in prefab.GetComponentsInChildren<TrailRenderer>(true))
                    {
                        trails.Add(trail, trail.textureMode);
                        trail.textureMode = LineTextureMode.Stretch;
                        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        trail.receiveShadows = false;
                        trail.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                        trail.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    }
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (material != null && material.shader.name == "Mobile/Unlit (Supports Lightmap)")
                        {
                            changed.Add(material, material.shader);
                            material.shader = Shader.Find("Unlit/Texture");
                        }
            }
            if (trails.Count > 0 || meshes.Count > 0 || linesChanged) PrefabUtility.SavePrefabAsset((GameObject)asset);
            PrefabExporter.Export(ModRoot, new ExportRequest { asset = asset, path = logical, description = "凤凰：" + source });
        }
        finally
        {
            foreach (var pair in changed) pair.Key.shader = pair.Value;
            foreach (var pair in trails) pair.Key.textureMode = pair.Value;
            foreach (var pair in meshes) pair.Key.sharedMesh = pair.Value;
            if (trails.Count > 0 || meshes.Count > 0 || linesChanged) { File.WriteAllBytes(source, originalPrefab); AssetDatabase.ImportAsset(source); }
        }
    }

    //将全息球的线段转换为窄三角带，保留原顶点轨迹；运行时仍使用真实世界表面网格。
    private static Mesh RibbonMesh(Mesh source)
    {
        Directory.CreateDirectory("Assets/PhoenixRimUnityExport");
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();
        var uv = new System.Collections.Generic.List<Vector2>();
        Vector3[] points = source.vertices;
        int[] lines = source.GetIndices(0);
        float width = source.bounds.size.magnitude * .0008f;
        for (int i = 0; i < lines.Length; i += 2)
        {
            Vector3 a = points[lines[i]], b = points[lines[i + 1]];
            Vector3 tangent = (b-a).normalized;
            Vector3 side = Vector3.Cross(tangent, (a+b).normalized).normalized * width;
            if (side.sqrMagnitude == 0) side = Vector3.Cross(tangent, Vector3.up).normalized * width;
            int n = vertices.Count;
            vertices.AddRange(new[] { a-side, a+side, b+side, b-side });
            uv.AddRange(new[] { new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0) });
            triangles.AddRange(new[] { n,n+1,n+2,n,n+2,n+3 });
        }
        var result = new Mesh { name = source.name + "_Triangles", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        result.SetVertices(vertices); result.SetUVs(0,uv); result.SetTriangles(triangles,0); result.RecalculateNormals(); result.RecalculateBounds();
        string path = "Assets/PhoenixRimUnityExport/" + result.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(result,path);
        else { EditorUtility.CopySerialized(result,existing); result=existing; }
        AssetDatabase.SaveAssets();
        return result;
    }
}
