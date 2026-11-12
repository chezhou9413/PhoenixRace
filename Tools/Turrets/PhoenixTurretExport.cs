using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimUnity.Editor;
using UnityEditor;
using UnityEngine;

//导入炮塔源模型，生成凤凰可分发的 RimUnity 资源。
public static class PhoenixTurretExport
{
    private const string AssetRoot = "Assets/Models/PhoenixTripleTurret";
    private const string ModelPath = AssetRoot + "/炮塔_骨骼.fbx";
    private const string ModRoot = "E:/RimModDev/PhoenixRace/PhoenixRace";

    //颜色烘焙到同一张图，刚性网格直接跟随原模型对应的骨骼节点。
    public static void Export()
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.isReadable = true;
        importer.SaveAndReimport();
        var source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        var result = new GameObject("PhoenixTripleTurret");
        try
        {
            var skins = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length != 2) throw new InvalidDataException("炮塔源文件应包含底座和三联装舰炮两块网格。");
            var sourceYaw = source.transform.Find("TurretRig/root/turret_yaw");
            if (sourceYaw == null) throw new InvalidDataException("缺少 turret_yaw 骨骼。");
            Bounds sourceBounds = ActualBounds(skins);
            var baseSkin = skins.Single(skin => RigidBone(skin).name == "root");
            Bounds baseBounds = ActualBounds(new[] { baseSkin });
            float scale = 1.8f / Mathf.Max(baseBounds.size.x, baseBounds.size.z);
            Matrix4x4 normalize = Matrix4x4.TRS(new Vector3(0, -sourceBounds.min.y * scale, 0),
                Quaternion.Euler(0, 180, 0), Vector3.one * scale)
                * Matrix4x4.Translate(new Vector3(-sourceYaw.position.x, 0, -sourceYaw.position.z));
            Transform root = Child(result.transform, "root");
            Transform yaw = Child(root, "turret_yaw");
            yaw.position = normalize.MultiplyPoint3x4(sourceYaw.position);
            Material[] colors = skins.SelectMany(skin => skin.sharedMaterials).Distinct().ToArray();
            Material material = BakePalette(colors);
            foreach (var skin in skins)
            {
                Transform sourceBone = RigidBone(skin);
                Transform targetBone = sourceBone.name == "root" ? root : yaw;
                int boneIndex = Array.IndexOf(skin.bones, sourceBone);
                Matrix4x4 conversion = targetBone.worldToLocalMatrix * normalize
                    * sourceBone.localToWorldMatrix * skin.sharedMesh.bindposes[boneIndex];
                Mesh mesh = BakeMesh(skin, conversion, colors);
                if (sourceBone.name == "turret_yaw")
                {
                    PhoenixTurretRig.Split(yaw, mesh, material, AssetRoot);
                    continue;
                }
                Transform node = Child(targetBone, "Base");
                node.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
            string prefabPath = AssetRoot + "/PhoenixTripleTurret.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(result, prefabPath);
            AssetDatabase.SaveAssets();
            PrefabExporter.Export(ModRoot, new ExportRequest
            {
                prefab = prefab,
                path = "Bundles/phoenixraceturrets/Assets/Models/PhoenixTripleTurret/PhoenixTripleTurret.prefab",
                description = "凤凰三联装炮塔：单贴图、单材质、刚性转台与三根独立后坐炮管。"
            });
            PhoenixTurretBattleAssets.Export(AssetRoot, ModRoot);
            PhoenixTurretAudio.Write(ModRoot);
            PhoenixTurretPreview.Write(result, ModRoot + "/1.7/Textures/Things/Building/Security/PhoenixTripleTurret.png");
            PhoenixTurretPreview.WriteFiring(result, AssetRoot, "E:/RimModDev/PhoenixRace/Build/turret-fire-preview.png");
            Debug.Log($"PHOENIX_TURRET_EXPORT_OK scale={scale:F6} bounds={ActualRigidBounds(result)}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(result);
        }
    }

    //源模型是完整刚性绑定，权重变化时明确中止，避免把软蒙皮错误固化。
    private static Transform RigidBone(SkinnedMeshRenderer skin)
    {
        BoneWeight[] weights = skin.sharedMesh.boneWeights;
        if (weights.Length != skin.sharedMesh.vertexCount || weights.Length == 0)
            throw new InvalidDataException("模型缺少顶点骨骼权重：" + skin.name);
        int bone = weights[0].boneIndex0;
        if (weights.Any(w => w.boneIndex0 != bone || w.weight0 < .99999f || w.weight1 != 0 || w.weight2 != 0 || w.weight3 != 0))
            throw new InvalidDataException("网格不是单骨骼刚性绑定：" + skin.name);
        return skin.bones[bone];
    }

    //读取实际绑定姿态的顶点边界，不使用可能含动画扩张量的 Renderer 边界。
    private static Bounds ActualBounds(IEnumerable<SkinnedMeshRenderer> skins)
    {
        var points = skins.SelectMany(skin =>
        {
            Transform bone = RigidBone(skin);
            Matrix4x4 matrix = bone.localToWorldMatrix * skin.sharedMesh.bindposes[Array.IndexOf(skin.bones, bone)];
            return skin.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4);
        }).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        return bounds;
    }

    //将原材质的颜色烘焙到带安全边距的色块，所有面共用同一张贴图。
    private static Material BakePalette(Material[] colors)
    {
        if (colors.Any(material => material.mainTexture != null))
            throw new InvalidDataException("源模型带有外部贴图，需要按源 UV 烘焙，不能只采集材质颜色。");
        int columns = Mathf.CeilToInt(Mathf.Sqrt(colors.Length));
        const int tile = 128;
        var texture = new Texture2D(columns * tile, columns * tile, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texture.width * texture.height];
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                int index = y / tile * columns + x / tile;
                pixels[y * texture.width + x] = index < colors.Length ? colors[index].color : Color.black;
            }
        texture.SetPixels(pixels);
        texture.Apply();
        string path = AssetRoot + "/PhoenixTripleTurret_Albedo.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        var material = new Material(Shader.Find("Unlit/Texture"))
        {
            name = "PhoenixTripleTurret",
            mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path)
        };
        return SaveAsset(material, AssetRoot + "/PhoenixTripleTurret.mat");
    }

    //颜色分界处拆顶点，转换法线与绕序后合并为一个子网格。
    private static Mesh BakeMesh(SkinnedMeshRenderer skin, Matrix4x4 conversion, Material[] colors)
    {
        Mesh source = skin.sharedMesh;
        Vector3[] sourceVertices = source.vertices, sourceNormals = source.normals;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        Matrix4x4 normalMatrix = conversion.inverse.transpose;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(colors.Length));
        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            int colorIndex = Array.IndexOf(colors, skin.sharedMaterials[submesh]);
            var coordinate = new Vector2((colorIndex % columns + .5f) / columns, (colorIndex / columns + .5f) / columns);
            var remap = new Dictionary<int, int>();
            int[] indices = source.GetTriangles(submesh);
            if (conversion.determinant < 0)
                for (int i = 0; i < indices.Length; i += 3)
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
            foreach (int index in indices)
            {
                if (!remap.TryGetValue(index, out int target))
                {
                    target = vertices.Count;
                    remap.Add(index, target);
                    vertices.Add(conversion.MultiplyPoint3x4(sourceVertices[index]));
                    normals.Add(normalMatrix.MultiplyVector(sourceNormals[index]).normalized);
                    uv.Add(coordinate);
                }
                triangles.Add(target);
            }
        }
        var mesh = new Mesh { name = skin.name };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return SaveAsset(mesh, AssetRoot + "/" + (RigidBone(skin).name == "root" ? "Base" : "Cannon") + ".asset");
    }

    //资源保留 GUID，重复导出只替换内容。
    internal static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
        EditorUtility.CopySerialized(asset, existing);
        UnityEngine.Object.DestroyImmediate(asset);
        return existing;
    }

    //创建零旋转、单位比例的刚性骨骼或网格节点。
    internal static Transform Child(Transform parent, string name)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    //报告最终模型尺寸，供建筑占地和菜单图核对。
    private static Bounds ActualRigidBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    //检查源模型的节点、蒙皮权重和材质，保留作者的骨骼坐标。
    public static void Inspect()
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.isReadable = true;
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new FileNotFoundException(ModelPath);
        var report = new StringBuilder();
        foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            report.AppendLine($"NODE {AnimationUtility.CalculateTransformPath(node, model.transform)} pos={node.localPosition:F4} rot={node.localEulerAngles:F3} scale={node.localScale:F4} world={node.position:F4}");
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var skin = renderer as SkinnedMeshRenderer;
            Mesh mesh = skin != null ? skin.sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
            report.AppendLine($"MESH {renderer.name} type={renderer.GetType().Name} vertices={mesh.vertexCount} submeshes={mesh.subMeshCount} bounds={renderer.bounds} local={mesh.bounds}");
            if (skin != null)
            {
                report.AppendLine("BONES " + string.Join(" | ", skin.bones.Select((bone, index) => index + ":" + bone.name)));
                report.AppendLine("WEIGHTS " + string.Join(" | ", mesh.boneWeights.GroupBy(w => $"{w.boneIndex0}:{w.weight0:F4},{w.boneIndex1}:{w.weight1:F4},{w.boneIndex2}:{w.weight2:F4},{w.boneIndex3}:{w.weight3:F4}").Select(g => g.Key + " count=" + g.Count())));
            }
            foreach (Material material in renderer.sharedMaterials)
                report.AppendLine($"MATERIAL {material.name} shader={material.shader.name} color={material.color} texture={material.mainTexture?.name}");
        }
        Debug.Log(report.ToString());
        Debug.Log("PHOENIX_TURRET_INSPECT_OK");
    }
}
