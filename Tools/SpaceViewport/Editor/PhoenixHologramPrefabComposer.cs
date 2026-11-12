using System;
using UnityEditor;
using UnityEngine;

namespace PhoenixRace.Editor.SpaceViewport
{
    //构建全息球面材质、空间轨道和真实舰船模型，作为舷窗预制体的内嵌资源。
    internal static class PhoenixHologramPrefabComposer
    {
        private const string Folder = "Assets/Art/UI/PhoenixHologram";

        //重建具名全息节点，所有美术依赖通过预制体进入同一个资源包。
        internal static void Compose(GameObject root)
        {
            Transform world = root.transform.Find("PhoenixHologramWorld");
            Transform sphere = world.Find("PhoenixHologramSphere");
            sphere.localPosition = Vector3.zero;
            sphere.localRotation = Quaternion.identity;
            sphere.localScale = Vector3.one * 120f;
            Material surface = MaterialAsset("PlanetSurface", "PhoenixRace/HologramPlanet", Color.white);
            Material shipMaterial = MaterialAsset("ShipSurface", "PhoenixRace/HologramPlanet", new Color(1f, 0.68f, 0.22f));
            shipMaterial.SetFloat("_Grid", 0f);
            shipMaterial.SetColor("_RimColor", new Color(1f, 0.85f, 0.4f));
            Material lines = MaterialAsset("OrbitLines", "PhoenixRace/HologramLine", new Color(0.08f, 0.8f, 1f, 0.6f));
            Material navigationLines = MaterialAsset("NavigationLines", "PhoenixRace/HologramLine", Color.white);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = surface;
            Transform previous = sphere.Find("HologramDetails");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            Transform details = Child(sphere, "HologramDetails");
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "PlanetCore"; core.transform.SetParent(details, false);
            core.transform.localScale = Vector3.one * 1.99f;
            UnityEngine.Object.DestroyImmediate(core.GetComponent<Collider>());
            core.GetComponent<MeshRenderer>().sharedMaterial = MaterialAsset("PlanetCore", "PhoenixRace/HologramPlanet", new Color(0.015f, 0.07f, 0.09f));
            Ring(details, "EquatorialOrbit", 1.2f, Quaternion.Euler(18f, 0f, 0f), lines);
            Ring(details, "InclinedOrbit", 1.28f, Quaternion.Euler(64f, 0f, 28f), lines);

            Transform marker = Child(details, "ShipLocation");
            marker.localPosition = new Vector3(0f, 0f, -1.05f);
            Ring(marker, "PositionReticle", 0.09f, Quaternion.identity, lines);
            LineRenderer stem = Line(marker, "AltitudeLink", lines, 0.7f);
            stem.positionCount = 2;
            stem.SetPositions(new[] { Vector3.zero, Vector3.up * 0.19f });
            Transform headingRoot = Child(marker, "ShipHeading");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/MDL_Ship.fbx");
            if (model == null) throw new InvalidOperationException("缺少舰船模型 MDL_Ship.fbx。");
            GameObject ship = UnityEngine.Object.Instantiate(model);
            ship.name = "ShipModel";
            ship.transform.SetParent(headingRoot, false);
            ship.transform.localPosition = Vector3.zero;
            ship.transform.localRotation = Quaternion.identity;
            ship.transform.localScale = Vector3.one;
            Renderer[] renderers = ship.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("舰船模型没有可渲染网格。");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = shipMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Vector3 center = marker.InverseTransformPoint(bounds.center);
            Vector3 size = marker.InverseTransformVector(bounds.size);
            float extent = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            if (extent <= 0f) throw new InvalidOperationException("舰船模型尺寸无效。");
            float scale = 0.34f / extent;
            //统一导入模型的长轴与厚度轴，使舰艏沿局部正前方，偏航不会变成绕舰身自转。
            Vector3 absoluteSize = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            int forwardAxis = absoluteSize.x > absoluteSize.z ? 0 : 2;
            if (absoluteSize.y > absoluteSize[forwardAxis]) forwardAxis = 1;
            int upAxis = forwardAxis == 1 ? 2 : 1;
            for (int i = 0; i < 3; i++) if (i != forwardAxis && absoluteSize[i] < absoluteSize[upAxis]) upAxis = i;
            Vector3 forward = Vector3.zero, up = Vector3.zero;
            forward[forwardAxis] = 1f; up[upAxis] = 1f;
            //舰艏位于导入长轴的负向，只翻转舰体，导航箭头继续指向真实前进方向。
            Quaternion alignment = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Inverse(Quaternion.LookRotation(forward, up));
            ship.transform.localRotation = alignment;
            ship.transform.localScale = Vector3.one * scale;
            ship.transform.localPosition = Vector3.up * 0.23f - alignment * center * scale;
            Debug.Log("PHOENIX_SHIP_AXES: bounds=" + size + ", forward=" + forward + ", up=" + up);
            //舰艏箭头与模型共用航向节点，远景也能清楚辨认转向。
            LineRenderer nose = Line(headingRoot, "HeadingChevron", navigationLines, 1.1f);
            nose.startColor = nose.endColor = new Color(1f, 0.8f, 0.15f, 1f);
            nose.positionCount = 3;
            nose.SetPositions(new[] { new Vector3(-0.045f, 0.24f, 0.20f), new Vector3(0f, 0.24f, 0.26f), new Vector3(0.045f, 0.24f, 0.20f) });
            foreach (Camera camera in ship.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera);
            foreach (Light light in ship.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
            PhoenixNavigationPrefabComposer.Compose(root.transform.Find("ViewportContent"), sphere, navigationLines);
            foreach (Transform node in world.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 31;
            world.Find("PhoenixHologramCamera").GetComponent<Camera>().cullingMask = 1 << 31;
        }

        //保存可重复构建的材质资产，确保着色器通过直接引用打入资源包。
        private static Material MaterialAsset(string name, string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("缺少全息着色器：" + shaderName);
            string path = Folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        //创建层级职责清晰的局部坐标节点。
        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        //创建使用局部坐标的全息线条，避免依赖运行时组件生成美术。
        private static LineRenderer Line(Transform parent, string name, Material material, float width)
        {
            LineRenderer line = Child(parent, name).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        //构建连续轨道圆环，其背面由球面深度自然遮挡。
        private static void Ring(Transform parent, string name, float radius, Quaternion rotation, Material material)
        {
            LineRenderer line = Line(parent, name, material, 0.45f);
            line.loop = true;
            line.positionCount = 192;
            line.transform.localRotation = rotation;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
    }
}
