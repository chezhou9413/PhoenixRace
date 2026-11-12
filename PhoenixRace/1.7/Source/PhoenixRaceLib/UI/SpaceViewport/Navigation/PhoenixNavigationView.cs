using System;
using RimUnity;
using RimUnity.UI;
using Verse;
using UiText = RimUnity.UI.Text;

namespace PhoenixRaceLib.SpaceViewport
{
    //管理座舱到球面导航的过渡、中文控件及独立观察视角，不改变游戏航行时间。
    internal sealed partial class PhoenixNavigationView
    {
        private readonly RectTransform root, slot, target;
        private readonly RawImage image;
        private readonly Transform sphere;
        private readonly Camera camera;
        private readonly Image dim;
        private readonly CanvasGroup controls;
        private readonly UiText info, prompt, travel;
        private readonly Button confirm, stop;
        private readonly PhoenixPlanetIndex index;
        private readonly PhoenixNavigationOverlay overlay;
        private readonly GameComponent_PhoenixNavigation state;
        private readonly Action close;
        private readonly Vector3[] corners = new Vector3[4];
        private readonly MaterialPropertyBlock highlight = new MaterialPropertyBlock();
        private readonly Renderer shell, core;
        private bool navigating;
        private float transition, yaw, pitch, zoom = 190f;
        private int selected = -1, hovered = -1, currentTile = -1;
        private float nextTextUpdate;
        private string hoverDescription, selectedDescription;
        internal bool Active => navigating || transition > 0f;

        //绑定资源包中的导航层级，使用游戏字体并初始化当前位置。
        internal PhoenixNavigationView(Transform viewport, RawImage image, Transform sphere, Camera camera, Action close)
        {
            this.image = image; this.sphere = sphere; this.camera = camera; this.close = close;
            root = (RectTransform)viewport.Find("ViewportContent/NavigationOverlay");
            slot = (RectTransform)image.transform.parent;
            target = (RectTransform)root.Find("GlobeTarget");
            dim = root.Find("CabinDim").GetComponent<Image>();
            controls = root.Find("Controls").GetComponent<CanvasGroup>();
            info = root.Find("Controls/InfoCard/Info").GetComponent<UiText>();
            prompt = root.Find("HoverPrompt").GetComponent<UiText>();
            travel = root.Find("TravelStatus").GetComponent<UiText>();
            foreach (UiText text in root.GetComponentsInChildren<UiText>(true)) text.font = new Font { GameFontIndex = 1 };
            state = GameComponent_PhoenixNavigation.Instance;
            state.InitializeFromMap();
            index = new PhoenixPlanetIndex();
            overlay = new PhoenixNavigationOverlay(sphere);
            shell = sphere.GetComponent<Renderer>();
            core = sphere.Find("HologramDetails/PlanetCore").GetComponent<Renderer>();
            confirm = Bind("Controls/InfoCard/Confirm", Confirm);
            stop = Bind("Controls/InfoCard/Stop", Stop);
            Bind("Controls/Current", FocusCurrent);
            Bind("Controls/Global", Global);
            Bind("Controls/Back", Back);
            Bind("CloseCabin", () => close());
            Bind("Pause", () => Find.TickManager.TogglePaused());
            Bind("NormalSpeed", () => Find.TickManager.CurTimeSpeed = TimeSpeed.Normal);
            Bind("FastSpeed", () => Find.TickManager.CurTimeSpeed = TimeSpeed.Fast);
            Bind("SuperSpeed", () => Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast);
            image.GetComponent<AspectRatioFitter>().enabled = false;
            image.transform.SetParent(root, false);
            image.transform.SetSiblingIndex(2);
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.zero;
            image.rectTransform.pivot = Vector2.zero;
        }

        //绑定一个具名按钮，委托随预制体销毁而释放。
        private Button Bind(string path, RimUnity.Events.UnityAction action)
        {
            Button button = root.Find(path).GetComponent<Button>();
            button.onClick.AddListener(action); return button;
        }

        //只在界面可见时更新交互和显示，镜头动画使用独立帧时间。
        internal void Update()
        {
            float goal = navigating ? 1f : 0f;
            transition = Mathf.MoveTowards(transition, goal, Time.unscaledDeltaTime / 0.6f);
            float eased = Mathf.SmoothStep(0f, 1f, transition);
            Rect source = Square(Bounds(slot)), destination = Square(Bounds(target));
            image.rectTransform.anchoredPosition = Vector2.Lerp(source.position, destination.position, eased);
            image.rectTransform.sizeDelta = Vector2.Lerp(source.size, destination.size, eased);
            Color tint = dim.color; tint.a = 0.38f * eased; dim.color = tint;
            controls.alpha = eased;
            controls.interactable = controls.blocksRaycasts = navigating && transition >= 1f;
            sphere.localRotation = Quaternion.Slerp(Quaternion.identity,
                Quaternion.AngleAxis(pitch, Vector3.right) * Quaternion.AngleAxis(yaw, Vector3.up), eased);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Lerp(190f, zoom, eased);
            camera.aspect = 1f;
            UpdateInput();
            overlay.Update(index, navigating ? hovered : -1, selected, state);
            highlight.SetColor("_RimColor", !navigating && onGlobe ? new Color(0.55f, 1.8f, 2f) : new Color(0.1f, 0.9f, 1f));
            shell.material.SetColor("_RimColor", !navigating && onGlobe ? new Color(0.55f, 1.8f, 2f) : new Color(0.1f, 0.9f, 1f));
            core.material.SetColor("_RimColor", !navigating && onGlobe ? new Color(0.55f, 1.8f, 2f) : new Color(0.1f, 0.9f, 1f));
            prompt.gameObject.SetActive(!Active && onGlobe);
            travel.gameObject.SetActive(!Active);
            if (Time.unscaledTime >= nextTextUpdate)
            {
                nextTextUpdate = Time.unscaledTime + 0.15f;
                UpdateText();
            }
        }

        //把任意矩形的屏幕角点转换到导航覆盖层坐标，兼容画布与游戏界面缩放。
        private Rect Bounds(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out Vector2 max);
            return new Rect(min - root.rect.min, max - min);
        }

        //在目标矩形内保留完整正圆，防止星球随屏幕宽高拉伸。
        private static Rect Square(Rect rect)
        {
            float size = Mathf.Min(rect.width, rect.height);
            return new Rect(rect.center - Vector2.one * size * 0.5f, Vector2.one * size);
        }

        //进入球体导航，保留独立观察角度和船舶当前位置。
        private void Enter() { navigating = true; hovered = -1; nextTextUpdate = 0f; }

        //返回座舱只改变视图，不取消正在进行的航行。
        private void Back() { navigating = false; dragging = false; hovered = -1; }

        //恢复完整北朝上的全球视图。
        private void Global() { yaw = pitch = 0f; zoom = 190f; }

        //把当前真实位置朝向镜头，之后继续允许自由观察。
        private void FocusCurrent()
        {
            if (!state.Initialized) return;
            Vector3 p = state.Position;
            yaw = Mathf.Atan2(p.x, -p.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Asin(Mathf.Clamp(p.y, -1f, 1f)) * Mathf.Rad2Deg;
            zoom = 190f;
        }

        //确认起始位置或发出航行命令，已经航行时不接受改道。
        private void Confirm()
        {
            if (selected < 0 || state.Travelling) return;
            if (!state.Initialized) state.SetInitialTile(selected);
            else if (!state.Depart(selected)) return;
            selected = -1; selectedDescription = null; nextTextUpdate = 0f;
        }

        //停止在当前球面位置，允许随后重新选择目的地。
        private void Stop() { state.Stop(); selected = -1; nextTextUpdate = 0f; }

        //依次取消目标、退出导航、关闭座舱，不取消在途航行。
        internal void Escape()
        {
            if (selected >= 0) { selected = -1; selectedDescription = null; }
            else if (Active) Back();
            else close();
        }

        //关闭界面时复位过渡和鼠标捕获，持久航行状态保持不变。
        internal void Hide()
        {
            navigating = false; transition = 0f; dragging = false; selected = hovered = -1; Global();
        }

        //组织当前位置、目的地和预计航行时间，避免每帧重建中文文本。
        private void UpdateText()
        {
            currentTile = state.Initialized ? index.Pick(state.Position) : -1;
            string location = state.Initialized ? (currentTile >= 0 ? index.Describe(currentTile) : "位于地表未生成区域上空") : "尚未定位：请选择起始区域";
            string status = state.Travelling ? (state.Turning ? "调整航向 · " : "航行中 · ") + (state.Progress * 100f).ToString("F0") + "%\n剩余 " + Seconds(state.RemainingTicks) : "轨道悬停";
            info.text = "当前位置\n" + location + "\n\n" + status + "\n\n";
            if (state.Travelling) info.text += "目的地\n" + index.Describe(state.DestinationTile);
            else if (selected >= 0)
            {
                info.text += "待确认目标\n" + selectedDescription;
                if (state.Initialized) info.text += "\n预计 " + Seconds(state.EstimateTicks(selected));
            }
            else info.text += "悬浮区域\n" + (hoverDescription ?? "将鼠标移到球面，点击选择区域");
            confirm.interactable = selected >= 0 && !state.Travelling && (!state.Initialized || Vector3.Angle(state.Position, GameComponent_PhoenixNavigation.Direction(selected)) >= 0.01f);
            confirm.GetComponentInChildren<UiText>().text = state.Initialized ? "确认航行" : "设为起始位置";
            stop.interactable = state.Travelling;
            travel.text = state.Travelling ? "凤凰舰船 · " + status.Replace('\n', ' ') : state.Initialized ? "凤凰舰船 · 轨道驻留" : "点击全息星球台设置初始位置";
            root.Find("Pause").GetComponentInChildren<UiText>().text = Find.TickManager.CurTimeSpeed == TimeSpeed.Paused ? "继续" : "暂停";
        }

        //按正常游戏速度显示剩余秒数，实际推进遵循暂停与倍速。
        private static string Seconds(int ticks) { return Mathf.CeilToInt(ticks / 60f) + " 秒（正常速度）"; }

        //释放运行时生成的导航网格。
        internal void Dispose() { overlay.Dispose(); }
    }
}
