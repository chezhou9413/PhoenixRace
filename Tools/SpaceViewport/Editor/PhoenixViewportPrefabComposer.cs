using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PhoenixRace.Editor.SpaceViewport
{
    //把完整红台座美术预制体和实时星球控件组合成可独立加载的舷窗资源。
    internal static class PhoenixViewportPrefabComposer
    {
        //在隔离的预制体编辑场景中替换画框并保存，不触碰用户场景。
        internal static void Compose()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PhoenixViewportBundleBuild.PrefabPath);
            try
            {
                //编辑期间停用布局回调，避免旧无效尺寸在重新挂接父节点时污染子节点。
                root.SetActive(false);
                RawImage planet = FindImage(root, "PhoenixOrbitPlanetImage");
                RawImage hologram = FindImage(root, "PhoenixViewportOverlay");
                //先保留实时渲染控件，使重复构建时可以安全替换完整美术容器。
                planet.transform.SetParent(root.transform, false);
                hologram.transform.SetParent(root.transform, false);
                RemoveChild(root, "PhoenixViewportBackground");
                RemoveChild(root, "PhoenixViewportFrame");
                RemoveChild(root, "RedPedestalUI");
                RemoveChild(root, "ViewportContent");

                //座舱独占全屏，导航覆盖层不继承台座美术的比例裁切。
                GameObject contentObject = new GameObject("ViewportContent", typeof(RectTransform), typeof(RectMask2D));
                contentObject.layer = root.layer;
                RectTransform content = contentObject.GetComponent<RectTransform>();
                content.SetParent(root.transform, false);
                Stretch(content);
                content.anchorMin = Vector2.zero;

                GameObject artAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PhoenixViewportBundleBuild.ArtPath + "/Prefabs/RedPedestalUI.prefab");
                if (artAsset == null)
                {
                    throw new InvalidOperationException("红台座美术预制体导入失败。");
                }
                GameObject art = (GameObject)PrefabUtility.InstantiatePrefab(artAsset, root.scene);
                art.SetActive(false);
                PrefabUtility.UnpackPrefabInstance(art, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                art.name = "RedPedestalUI";
                art.transform.SetParent(content, false);
                RectTransform composition = art.GetComponent<RectTransform>();
                Stretch(composition);
                AspectRatioFitter compositionAspect = art.AddComponent<AspectRatioFitter>();
                //先指定约束模式，避免编辑器在 None 模式下用零尺寸计算出无效比例。
                compositionAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                compositionAspect.aspectRatio = 1.8f;
                NormalizeArtRectangles(composition);

                planet.transform.SetParent(composition, false);
                Stretch(planet.rectTransform);
                planet.transform.SetAsFirstSibling();
                ConfigureLiveImage(planet);
                PhoenixOrbitPresentationComposer.Compose(content, planet);

                Transform slot = composition.Find("StarPlatform/Hologram_StarMap");
                slot.GetComponent<Image>().enabled = false;
                hologram.transform.SetParent(slot, false);
                Stretch(hologram.rectTransform);
                AspectRatioFitter square = hologram.GetComponent<AspectRatioFitter>();
                if (square == null)
                {
                    square = hologram.gameObject.AddComponent<AspectRatioFitter>();
                }
                square.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                square.aspectRatio = 1f;
                ConfigureLiveImage(hologram);

                PhoenixHologramPrefabComposer.Compose(root);

                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                {
                    camera.enabled = false;
                    camera.targetTexture = null;
                }
                RequireValidAspect(compositionAspect, 1.8f);
                RequireValidAspect(square, 1f);
                art.SetActive(true);
                root.SetActive(true);
                RequireValidRectangles(root);
                PrefabUtility.SaveAsPrefabAsset(root, PhoenixViewportBundleBuild.PrefabPath, out bool saved);
                if (!saved)
                {
                    throw new InvalidOperationException("舷窗预制体保存失败。");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //阻止把无效比例写入交付预制体，直接给出出错控件名称。
        private static void RequireValidAspect(AspectRatioFitter fitter, float expected)
        {
            if (float.IsNaN(fitter.aspectRatio) || float.IsInfinity(fitter.aspectRatio) ||
                !Mathf.Approximately(fitter.aspectRatio, expected))
            {
                throw new InvalidOperationException("舷窗控件比例无效：" + fitter.name);
            }
        }

        //阻止把受无效布局传播影响的位置和锚点保存到资源包。
        private static void RequireValidRectangles(GameObject root)
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                foreach (Vector2 value in new[] { rect.anchorMin, rect.anchorMax, rect.anchoredPosition, rect.sizeDelta })
                {
                    if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.x) || float.IsInfinity(value.y))
                    {
                        throw new InvalidOperationException("舷窗控件矩形无效：" + rect.name);
                    }
                }
            }
        }

        //将导出预制体的设计像素矩形转为比例锚点，使整套美术等比适配屏幕。
        private static void NormalizeArtRectangles(RectTransform root)
        {
            const float width = 1920f;
            const float height = 1066.6666667f;
            foreach (RectTransform group in root)
            {
                foreach (RectTransform part in group)
                {
                    Vector2 position = part.anchoredPosition;
                    Vector2 size = part.sizeDelta;
                    Stretch(part);
                    part.anchorMin = new Vector2(position.x / width, 1f + (position.y - size.y) / height);
                    part.anchorMax = new Vector2((position.x + size.x) / width, 1f + position.y / height);
                }
                Stretch(group);
            }
        }

        //移除预制体中指定的旧美术节点，保留模型和相机等其他资源。
        private static void RemoveChild(GameObject root, string name)
        {
            Transform child = root.transform.Find(name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        //按稳定名称取得实时图像控件，缺少控件时阻止生成不完整资源包。
        private static RawImage FindImage(GameObject root, string name)
        {
            foreach (RawImage image in root.GetComponentsInChildren<RawImage>(true))
            {
                if (image.name == name)
                {
                    return image;
                }
            }
            throw new InvalidOperationException("舷窗预制体缺少控件：" + name);
        }

        //清空渲染纹理占位引用，由模组绑定当前存档生成的实时纹理。
        private static void ConfigureLiveImage(RawImage image)
        {
            image.texture = null;
            image.color = Color.white;
            image.material = null;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            image.raycastTarget = false;
        }

        //复位矩形并拉满父容器，避免沿用旧画框的偏移和缩放。
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
