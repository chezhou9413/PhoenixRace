using System.IO;
using UnityEditor;
using UnityEngine;

//在编辑器离线渲染建筑菜单图，不进入游戏或播放模式。
public static class PhoenixTurretPreview
{
    //菜单与蓝图使用同一张透明底预览，游戏中的成品仍绘制真实三维模型。
    public static void Write(GameObject model, string path, float orthographicSize = 2f)
    {
        var cameraObject = new GameObject("TurretPreviewCamera");
        var lightObject = new GameObject("TurretPreviewLight");
        var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        var material = new Material(Shader.Find("Standard"));
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        Material original = renderers[0].sharedMaterial;
        RenderTexture previous = RenderTexture.active;
        var ambientMode = RenderSettings.ambientMode;
        Color ambient = RenderSettings.ambientLight;
        try
        {
            material.mainTexture = original.mainTexture;
            material.SetFloat("_Glossiness", .12f);
            foreach (var renderer in renderers) renderer.sharedMaterial = material;
            foreach (Transform node in model.GetComponentsInChildren<Transform>()) node.gameObject.layer = 31;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            camera.targetTexture = target;
            camera.transform.position = bounds.center + new Vector3(3, 6, 5);
            camera.transform.LookAt(bounds.center);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.5f, .5f, .5f);
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            foreach (var renderer in renderers) renderer.sharedMaterial = original;
            RenderTexture.active = previous;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambient;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }

    //导出素材预览只采样编辑器中的指定姿态，不启动播放模式。
    public static void WriteFiring(GameObject model, string assetDirectory, string output)
    {
        GameObject muzzle = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(assetDirectory + "/PhoenixTurretMuzzle.prefab"));
        Transform barrel = model.transform.Find("root/turret_yaw/barrel_0");
        muzzle.transform.SetParent(model.transform, false);
        muzzle.transform.position = barrel.Find("Muzzle").position;
        Vector3 rest = barrel.localPosition;
        try
        {
            barrel.localPosition -= Vector3.forward * .14f;
            foreach (var system in muzzle.GetComponentsInChildren<ParticleSystem>()) system.Simulate(.04f, false, true, true);
            Write(model, output);
        }
        finally
        {
            barrel.localPosition = rest;
            Object.DestroyImmediate(muzzle);
        }
    }
}
