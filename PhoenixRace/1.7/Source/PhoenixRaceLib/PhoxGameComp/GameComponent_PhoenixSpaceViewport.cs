using ChezhouLib.ALLmap;
using PhoenixRaceLib.SpaceViewport;
using RimWorld;
using RimUnity;
using RimUnity.EventSystems;
using RimUnity.UI;
using Verse;

namespace PhoenixRaceLib.PhoxGameComp
{
    //协调舰船界面的开关和视觉资源生命周期，航行数据由独立组件持久保存。
    public sealed class GameComponent_PhoenixSpaceViewport : GameComponent
    {
        private GameObject prefab, instance;
        private Transform hologramWorld, sphere;
        private RectTransform content;
        private Camera hologramCamera;
        private RenderTexture hologramTexture;
        private PhoenixHologramPlanet planet;
        private PhoenixOrbitCamera orbit;
        private PhoenixNavigationView navigation;
        private bool visible;

        public static GameComponent_PhoenixSpaceViewport Instance => Current.Game?.GetComponent<GameComponent_PhoenixSpaceViewport>();
        public bool IsVisible => visible;

        //接收游戏自动创建组件时的实例。
        public GameComponent_PhoenixSpaceViewport(Game game) { }

        //统一处理原版选择状态和主面板，避免查看页自动重开导致界面闪退。
        public void ToggleViewport()
        {
            if (visible) { Hide(); return; }
            Find.Selector.ClearSelection(); Find.WorldSelector.ClearSelection();
            Find.MainTabsRoot.SetCurrentTab(null);
            EnsurePrefab();
            if (prefab == null)
            {
                const string message = "凤凰舰船界面资源未加载，请检查 ChezhouLib 和 RimUnity 资源清单。";
                Log.Error(message); Messages.Message(message, MessageTypeDefOf.RejectInput, false); return;
            }
            try
            {
                if (instance == null) Create();
                visible = true;
                instance.SetActive(true); hologramWorld.gameObject.SetActive(true);
                orbit.ResetView();
            }
            catch (System.Exception error)
            {
                Cleanup();
                Log.Error("[PhoenixRace] 舰船导航初始化失败：" + error);
                Messages.Message("舰船导航初始化失败：" + error.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        //隐藏视觉对象但保留独立航行状态和当前旅程。
        private void Hide()
        {
            visible = false; navigation?.Hide();
            if (instance != null) instance.SetActive(false);
            if (hologramWorld != null) hologramWorld.gameObject.SetActive(false);
        }

        //把返回键交给导航状态机，退出座舱也不终止航行。
        public void HandleEscape() { navigation?.Escape(); }

        //只在界面可见时处理交互并绘制两台独立镜头。
        public override void GameComponentUpdate()
        {
            if (!visible) return;
            if (Find.MainTabsRoot.OpenTab != null && Find.MainTabsRoot.OpenTab != MainButtonDefOf.Inspect)
            { Hide(); return; }
            if (Find.MainTabsRoot.OpenTab == MainButtonDefOf.Inspect)
            {
                Find.Selector.ClearSelection(); Find.MainTabsRoot.SetCurrentTab(null, false);
            }
            content.anchorMin = Vector2.zero;
            try
            {
                navigation.Update(); planet.UpdateLocation();
                orbit.Render(GameComponent_PhoenixNavigation.Instance);
                hologramCamera.Render();
            }
            catch (System.Exception error)
            {
                Hide();
                Log.Error("[PhoenixRace] 舰船导航渲染失败：" + error);
                Messages.Message("舰船导航渲染失败：" + error.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        //从 ChezhouLib 取得预制体，导航美术通过 RimUnity 明文资源交付。
        private void EnsurePrefab()
        {
            if (prefab != null || ChezhouLib.Assets.ClPrefab.Templates == null) return;
            if (!ChezhouLib.Assets.ClPrefab.Templates.TryGetValue("PhoenixSpaceViewportUI", out prefab))
                ChezhouLib.Assets.ClPrefab.Templates.TryGetValue("chezhou.Race.PhoenixRace:PhoenixSpaceViewportUI", out prefab);
        }

        //实例化完整导航预制体，建立与画布缩放无关的全息场景。
        private void Create()
        {
            EnsureEventSystem();
            instance = Object.Instantiate(prefab);
            instance.name = "PhoenixSpaceViewport_Runtime";
            content = (RectTransform)instance.transform.Find("ViewportContent");
            hologramWorld = ChezhouLib.Assets.ClPrefab.Instantiate("PhoenixHologramWorld").transform;
            sphere = hologramWorld.Find("PhoenixHologramSphere");
            hologramCamera = hologramWorld.Find("PhoenixHologramCamera").GetComponent<Camera>();
            hologramWorld.SetParent(null, false);
            hologramWorld.position = new Vector3(10000f, 10000f, 10000f);
            hologramCamera.enabled = false; hologramCamera.orthographic = true;
            hologramCamera.orthographicSize = 190f; hologramCamera.aspect = 1f;
            hologramCamera.nearClipPlane = 0.1f; hologramCamera.farClipPlane = 2000f;
            hologramCamera.clearFlags = CameraClearFlags.Color; hologramCamera.backgroundColor = Color.clear;
            hologramCamera.transform.position = hologramWorld.position + Vector3.back * 690f;
            hologramCamera.transform.rotation = Quaternion.identity;
            hologramTexture = new RenderTexture(1024, 1024, 24, (int)UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 4)
            { name = "PhoenixNavigationGlobe", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            hologramCamera.targetTexture = hologramTexture;
            RawImage globe = content.Find("RedPedestalUI/StarPlatform/Hologram_StarMap/PhoenixViewportOverlay").GetComponent<RawImage>();
            globe.texture = hologramTexture;
            RawImage orbitImage = content.Find("RedPedestalUI/PhoenixOrbitPlanetImage").GetComponent<RawImage>();
            orbit = new PhoenixOrbitCamera(orbitImage.material);
            orbitImage.texture = orbit.Texture;
            orbitImage.material = orbit.Presentation;
            planet = new PhoenixHologramPlanet(sphere);
            navigation = new PhoenixNavigationView(instance.transform, globe, sphere, hologramCamera, Hide);
            Canvas.ForceUpdateCanvases();
        }

        //确保标准 UGUI 按钮能够接收鼠标，复用已存在的事件系统。
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject system = new GameObject("PhoenixViewport_EventSystem");
            system.AddComponent<EventSystem>(); 
        }

        //读档时释放上一局的视觉对象，位置由独立航行组件恢复。
        public override void LoadedGame() { Cleanup(); }

        //新游戏不继承旧场景中的渲染对象。
        public override void StartedNewGame() { Cleanup(); }

        //保存时不序列化窗口开关和临时选区，避免读档期间自动创建 Unity 场景。
        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit) Cleanup();
        }

        //释放界面独占网格、相机与纹理，不触碰共享 AB 资源。
        internal void Cleanup()
        {
            visible = false;
            navigation?.Dispose(); navigation = null;
            planet?.Dispose(); planet = null;
            orbit?.Dispose(); orbit = null;
            if (hologramTexture != null) { Object.Destroy(hologramTexture); }
            if (hologramWorld != null) Object.Destroy(hologramWorld.gameObject);
            if (instance != null) Object.Destroy(instance);
            hologramTexture = null; hologramWorld = null; instance = null;
            hologramCamera = null; sphere = null; content = null;
        }
    }
}
