using System.IO;
using RimUnity.Editor;
using UnityEditor;
using UnityEngine;

//制作炮口喷焰、火星、烟气、命中效果和轨迹共用的材质。
public static class PhoenixTurretBattleAssets
{
    //全部资源通过现有 RimUnity 导出器写入凤凰资源清单。
    public static void Export(string directory, string modRoot)
    {
        Texture2D soft = MakeSoftTexture(directory);
        Material glow = MakeMaterial(directory, "TurretGlow", "Legacy Shaders/Particles/Additive", soft);
        Material smoke = MakeMaterial(directory, "TurretSmoke", "Legacy Shaders/Particles/Alpha Blended", soft);
        GameObject muzzle = MakeEffect("PhoenixTurretMuzzle", false, glow, smoke);
        GameObject impact = MakeEffect("PhoenixTurretImpact", true, glow, smoke);
        GameObject shell = PhoenixTurretShellAsset.Create(directory, glow);
        try
        {
            var requests = new ExportRequest[3];
            GameObject[] roots = { muzzle, impact, shell };
            for (int i = 0; i < roots.Length; i++)
            {
                string path = directory + "/" + roots[i].name + ".prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(roots[i], path);
                requests[i] = new ExportRequest { prefab = prefab,
                    path = "Bundles/phoenixraceturrets/" + path, description = "凤凰三联装炮塔战斗资源：" + roots[i].name };
            }
            AssetDatabase.SaveAssets();
            PrefabExporter.Export(modRoot, requests);
            PhoenixTurretPreview.Write(shell, "E:/RimModDev/PhoenixRace/Build/turret-shell-preview.png", .34f);
        }
        finally
        {
            Object.DestroyImmediate(muzzle); Object.DestroyImmediate(impact); Object.DestroyImmediate(shell);
        }
    }

    //亮核负责瞬间闪光，喷焰和火星沿炮口方向运动，烟气缓慢扩散。
    private static GameObject MakeEffect(string name, bool impact, Material glow, Material smoke)
    {
        var root = new GameObject(name);
        AddParticles(root, "Flash", glow, 2, .07f, .11f, 0, 0, impact ? .5f : .65f, 0,
            new Color(3, 2.4f, 1.6f, 1), new Color(2, .6f, .06f, 0));
        AddParticles(root, "Flame", glow, impact ? 8 : 6, .07f, .16f, 3, 6, .2f, impact ? 65 : 12,
            new Color(1.8f, 1.2f, .4f, 1), new Color(.9f, .12f, .015f, 0));
        AddParticles(root, "Sparks", glow, impact ? 14 : 9, .1f, .28f, 4, 9, .035f, impact ? 70 : 25,
            new Color(1, .9f, .45f, 1), new Color(1, .3f, .04f, 0), true);
        AddParticles(root, "Smoke", smoke, impact ? 10 : 5, .35f, .7f, .5f, 1.8f, .22f, impact ? 70 : 28,
            new Color(.42f, .38f, .34f, .38f), new Color(.22f, .22f, .22f, 0));
        return root;
    }

    //仅启用当前效果需要的模块，停止和播放交给游戏中的粒子控制器。
    private static void AddParticles(GameObject root, string name, Material material, int count,
        float lifeMin, float lifeMax, float speedMin, float speedMax, float size, float cone,
        Color start, Color end, bool streak = false)
    {
        GameObject node = PhoenixTurretExport.Child(root.transform, name).gameObject;
        var system = node.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = false; main.playOnAwake = false; main.duration = .15f;
        main.startDelay = name == "Smoke" ? .06f : 0;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(size * .75f, size * 1.25f);
        main.startColor = Color.white;
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = name == "Smoke" ? -.12f : name == "Sparks" ? .15f : 0;
        var emission = system.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        var shape = system.shape;
        shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = cone; shape.radius = .025f;
        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new Gradient { colorKeys = new[] { new GradientColorKey(start, 0), new GradientColorKey(end, 1) },
            alphaKeys = new[] { new GradientAlphaKey(start.a, 0), new GradientAlphaKey(start.a * .7f, .2f), new GradientAlphaKey(0, 1) } };
        var growth = system.sizeOverLifetime;
        growth.enabled = true;
        growth.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, name == "Smoke" ? 2.8f : .15f));
        var renderer = node.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = streak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = 1.4f;
        renderer.velocityScale = .04f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    //白色软圆同时提供闪光、烟雾和轨迹横截面的透明衰减。
    private static Texture2D MakeSoftTexture(string directory)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) * 2 / size - 1, dy = (y + .5f) * 2 / size - 1;
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - dx * dx - dy * dy), 2.5f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        texture.SetPixels(pixels); texture.Apply();
        string path = directory + "/TurretSoftParticle.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    //使用已接通的原生粒子混合模式，避免额外 Shader 编译依赖。
    private static Material MakeMaterial(string directory, string name, string shader, Texture2D texture)
    {
        var material = new Material(Shader.Find(shader)) { name = name, mainTexture = texture };
        material.SetColor("_TintColor", name == "TurretGlow"
            ? new Color(.8f, .8f, .7f, .8f) : new Color(.5f, .5f, .5f, .5f));
        material.DisableKeyword("SOFTPARTICLES_ON");
        return PhoenixTurretExport.SaveAsset(material, directory + "/" + name + ".mat");
    }
}
