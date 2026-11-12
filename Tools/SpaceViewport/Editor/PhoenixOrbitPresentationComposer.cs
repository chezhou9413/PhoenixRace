using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PhoenixRace.Editor.SpaceViewport
{
    //配置座舱的不透明底层和窗外曝光，隔离游戏地图与半透明舷窗美术。
    internal static class PhoenixOrbitPresentationComposer
    {
        //将遮挡底色置于所有座舱图层之后，并保存星球显示材质供资源包引用。
        internal static void Compose(RectTransform content, RawImage planet)
        {
            GameObject backdrop = new GameObject("OpaqueCabinBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.layer = content.gameObject.layer;
            RectTransform rect = backdrop.GetComponent<RectTransform>();
            rect.SetParent(content, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.SetAsFirstSibling();
            Image image = backdrop.GetComponent<Image>();
            image.color = new Color(0.008f, 0.015f, 0.03f, 1f);
            image.raycastTarget = true;

            Shader shader = Shader.Find("PhoenixRace/OrbitPresentation");
            if (shader == null) throw new InvalidOperationException("缺少舷窗曝光着色器。");
            const string path = "Assets/Art/UI/PhoenixHologram/OrbitPresentation.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetFloat("_Exposure", 1.3f);
            material.SetFloat("_Midtone", 0.82f);
            material.SetFloat("_AtmosphereWidth", 0.018f);
            material.SetFloat("_AtmosphereStrength", 0.38f);
            material.SetFloat("_EdgeBlur", 2.4f);
            material.SetColor("_AtmosphereColor", new Color(0.24f, 0.58f, 1f, 1f));
            EditorUtility.SetDirty(material);
            planet.material = material;
        }
    }
}
