using UnityEngine;
using UnityEngine.UI;

namespace PhoenixRace.Editor.SpaceViewport
{
    //把导航面板、镜头目标区域和中文控件写入资源包预制体。
    internal static class PhoenixNavigationPrefabComposer
    {
        //创建独立导航覆盖层，其矩形始终位于底部主按钮栏上方。
        internal static void Compose(Transform content, Transform sphere, Material lines)
        {
            Transform previous = sphere.Find("NavigationGeometry");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            Transform geometry = new GameObject("NavigationGeometry").transform;
            geometry.SetParent(sphere, false);
            foreach (string name in new[] { "HoverRegion", "TargetRegion", "Route" })
            {
                GameObject node = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                node.transform.SetParent(geometry, false);
                node.GetComponent<MeshRenderer>().sharedMaterial = lines;
                node.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            RectTransform root = Rect(content, "NavigationOverlay", Vector2.zero, Vector2.one);
            Image dim = Rect(root, "CabinDim", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            dim.color = new Color(0.005f, 0.02f, 0.04f, 0f); dim.raycastTarget = false;
            Rect(root, "GlobeTarget", new Vector2(0.04f, 0.14f), new Vector2(0.73f, 0.89f));
            RectTransform controls = Rect(root, "Controls", Vector2.zero, Vector2.one);
            Text title = Label(controls, "Title", "凤凰轨道导航", new Vector2(0.06f, 0.9f), new Vector2(0.7f, 0.97f));
            title.fontSize = 30;
            RectTransform card = Rect(controls, "InfoCard", new Vector2(0.75f, 0.13f), new Vector2(0.97f, 0.89f));
            card.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.055f, 0.08f, 0.95f);
            Label(card, "Info", "选择目标区域", new Vector2(0.07f, 0.28f), new Vector2(0.93f, 0.95f));
            Button(card, "Confirm", "确认航行", new Vector2(0.07f, 0.15f), new Vector2(0.93f, 0.24f));
            Button(card, "Stop", "停止航行", new Vector2(0.07f, 0.03f), new Vector2(0.93f, 0.12f));
            Button(controls, "Current", "当前位置", new Vector2(0.08f, 0.04f), new Vector2(0.25f, 0.10f));
            Button(controls, "Global", "全球视图", new Vector2(0.28f, 0.04f), new Vector2(0.45f, 0.10f));
            Button(controls, "Back", "返回座舱", new Vector2(0.48f, 0.04f), new Vector2(0.65f, 0.10f));
            Label(controls, "Help", "拖动旋转 · 滚轮缩放 · 点击选区", new Vector2(0.08f, 0.105f), new Vector2(0.70f, 0.14f));
            controls.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            Label(root, "HoverPrompt", "点击进入星球导航", new Vector2(0.34f, 0.04f), new Vector2(0.68f, 0.10f));
            Label(root, "TravelStatus", "", new Vector2(0.06f, 0.9f), new Vector2(0.72f, 0.97f));
            Button(root, "CloseCabin", "关闭舰船", new Vector2(0.82f, 0.92f), new Vector2(0.97f, 0.98f));
            Button(root, "Pause", "暂停", new Vector2(0.75f, 0.04f), new Vector2(0.815f, 0.10f));
            Button(root, "NormalSpeed", "1倍", new Vector2(0.825f, 0.04f), new Vector2(0.87f, 0.10f));
            Button(root, "FastSpeed", "快", new Vector2(0.88f, 0.04f), new Vector2(0.92f, 0.10f));
            Button(root, "SuperSpeed", "极快", new Vector2(0.93f, 0.04f), new Vector2(0.975f, 0.10f));
        }

        //按比例建立具名矩形，避免固定像素坐标破坏屏幕适配。
        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        //创建可换行中文标签，运行时绑定游戏的中文字库。
        private static Text Label(Transform parent, string name, string value, Vector2 min, Vector2 max)
        {
            Text text = Rect(parent, name, min, max).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = 20; text.color = new Color(0.55f, 0.94f, 1f);
            text.alignment = TextAnchor.UpperLeft; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = text.fontSize;
            return text;
        }

        //创建带目标图形的标准 UGUI 按钮，事件由运行时控制器绑定。
        private static void Button(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(parent, name, min, max);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.035f, 0.2f, 0.25f, 0.95f);
            rect.gameObject.AddComponent<Button>().targetGraphic = image;
            Label(rect, "Label", label, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f)).alignment = TextAnchor.MiddleCenter;
        }
    }
}
