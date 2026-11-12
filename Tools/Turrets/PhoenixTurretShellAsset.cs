using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//用旋转截面制作实际三维炮弹，保留弹身、束带、尖头和底部曳光结构。
public static class PhoenixTurretShellAsset
{
    //炮弹网格只用一个材质，双层轨迹由游戏按真实弹道采样。
    public static GameObject Create(string directory, Material glow)
    {
        var root = new GameObject("PhoenixTurretShell");
        GameObject body = PhoenixTurretExport.Child(root.transform, "Body").gameObject;
        body.AddComponent<MeshFilter>().sharedMesh = MakeMesh(directory);
        var renderer = body.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = MakeMaterial(directory);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        MakeTrail(root, "TrailGlow", glow, .12f, new Color(1, .35f, .06f, .55f));
        MakeTrail(root, "TrailCore", glow, .035f, new Color(1, .88f, .5f, .95f));
        return root;
    }

    //十八边旋转体提供可辨认的金属弹头，不使用平面弹丸图片。
    private static Mesh MakeMesh(string directory)
    {
        float[] z = { -.24f, -.225f, -.20f, -.17f, -.15f, .07f, .15f, .24f };
        float[] radius = { 0, .045f, .045f, .055f, .045f, .045f, .032f, 0 };
        const int sides = 18;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int ring = 0; ring < z.Length; ring++)
            for (int side = 0; side <= sides; side++)
            {
                float angle = side * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius[ring], Mathf.Sin(angle) * radius[ring], z[ring]));
                uv.Add(new Vector2((float)side / sides, (z[ring] + .24f) / .48f));
                if (ring == 0 || side == sides) continue;
                int current = ring * (sides + 1) + side, previous = current - sides - 1;
                triangles.AddRange(new[] { previous, current + 1, current, previous, previous + 1, current + 1 });
            }
        var mesh = new Mesh { name = "PhoenixTurretShell" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return PhoenixTurretExport.SaveAsset(mesh, directory + "/PhoenixTurretShell.asset");
    }

    //把金属色和环向明暗烘焙进炮弹贴图，高速飞行时无需逐弹离屏采集。
    private static Material MakeMaterial(string directory)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float at = (y + .5f) / size;
                Color color = at < .09f ? new Color(1, .45f, .06f)
                    : at < .22f ? new Color(.75f, .48f, .18f)
                    : at < .66f ? new Color(.36f, .39f, .42f) : new Color(.85f, .7f, .39f);
                float light = .55f + .45f * Mathf.Max(0, Mathf.Cos((x + .5f) / size * Mathf.PI * 2 - .8f));
                pixels[y * size + x] = new Color(color.r * light, color.g * light, color.b * light, 1);
            }
        texture.SetPixels(pixels); texture.Apply();
        string path = directory + "/PhoenixTurretShell_Albedo.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var material = new Material(Shader.Find("Unlit/Texture"))
            { name = "PhoenixTurretShell", mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        return PhoenixTurretExport.SaveAsset(material, directory + "/PhoenixTurretShell.mat");
    }

    //线段由运行时写入三维轨迹，宽度和透明度从弹尾向后衰减。
    private static void MakeTrail(GameObject root, string name, Material material, float width, Color color)
    {
        var line = PhoenixTurretExport.Child(root.transform, name).gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = 0;
        line.widthCurve = AnimationCurve.Linear(0, 1, 1, .05f);
        line.widthMultiplier = width;
        line.colorGradient = new Gradient { colorKeys = new[] { new GradientColorKey(color, 0), new GradientColorKey(new Color(.7f, .12f, .02f), 1) },
            alphaKeys = new[] { new GradientAlphaKey(color.a, 0), new GradientAlphaKey(0, 1) } };
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        line.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }
}
