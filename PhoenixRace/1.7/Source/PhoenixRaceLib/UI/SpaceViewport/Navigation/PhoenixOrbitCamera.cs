using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //将独立舰船的轨道位置映射到真实星球舷窗镜头，独占相机与离屏纹理。
    internal sealed class PhoenixOrbitCamera
    {
        private readonly GameObject root;
        private readonly Camera camera;
        private bool positioned;
        private RenderTexture target;
        internal RimUnity.Texture Texture { get; }
        internal RimUnity.Material Presentation { get; }

        //创建手动渲染相机，禁止参与地图的自动相机绘制。
        internal PhoenixOrbitCamera(RimUnity.Material presentationTemplate)
        {
            Presentation = new RimUnity.Material(presentationTemplate);
            root = new GameObject("PhoenixOrbitCamera");
            Object.DontDestroyOnLoad(root);
            camera = root.AddComponent<Camera>();
            camera.enabled = false; camera.cullingMask = WorldCameraManager.WorldLayerMask;
            camera.clearFlags = CameraClearFlags.Color; camera.backgroundColor = new Color(0.008f, 0.015f, 0.03f);
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 7500f;
            camera.fieldOfView = 44f; camera.renderingPath = RenderingPath.Forward;
            target = new RenderTexture(1440, 800, 24, RenderTextureFormat.ARGB32)
            { name = "PhoenixOrbitView", antiAliasing = 2, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            target.Create(); camera.targetTexture = target;
            Texture = RimUnity.Texture.FromNative(target.CoreTexture);
        }

        //首次打开立即对齐当前位置，后续帧沿径向和航行切线平滑更新视角。
        internal void Render(GameComponent_PhoenixNavigation state)
        {
            PlanetLayer surface = Find.WorldGrid.Surface;
            Vector3 radial = state.Initialized ? state.Position.ToHost() : Vector3.back;
            Vector3 heading = state.Initialized ? state.Heading.ToHost() : Vector3.right;
            Vector3 position = surface.Origin + radial * surface.Radius * 1.065f;
            //低轨道斜望星球边缘，弧形地平线位于画面上缘，大面积地表占据舷窗。
            Vector3 forward = (-radial + heading * 1.4f).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, heading) * Quaternion.Euler(0f, 0f, -state.BankDegrees);
            float blend = positioned ? 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f) : 1f;
            camera.transform.position = Vector3.Lerp(camera.transform.position, position, blend);
            camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, rotation, blend);
            positioned = true;
            //把实际镜头下的球心传给显示材质，使大气边缘随航行、转向和侧倾移动。
            Vector3 center = camera.transform.InverseTransformPoint(surface.Origin) / surface.Radius;
            float halfHeight = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            Presentation.SetVector("_PlanetCenter", new RimUnity.Vector4(center.x, center.y, center.z, 0f));
            Presentation.SetVector("_ViewPlane", new RimUnity.Vector4(halfHeight * camera.aspect, halfHeight, 0f, 0f));
            WorldRenderMode previousMode = Find.World.renderer.wantedMode;
            PlanetLayer previousLayer = PlanetLayer.Selected;
            float previousSunlight = Shader.GetGlobalFloat(ShaderPropertyIDs.PlanetSunLightEnabled);
            float previousBackground = Shader.GetGlobalFloat(ShaderPropertyIDs.BackgroundModeEnabled);
            Vector4 previousSunDirection = Shader.GetGlobalVector(ShaderPropertyIDs.PlanetSunLightDirection);
            bool previousScope = Patch_PhoenixOrbitBackground.Rendering;
            try
            {
                Patch_PhoenixOrbitBackground.Rendering = true;
                PlanetLayer.Selected = surface;
                Find.World.renderer.wantedMode = WorldRenderMode.Background;
                Find.World.renderer.DrawWorldLayers();
                //沿用原版背景模式的云层、辉光与日照，不把可交互地表网格画进舷窗。
                camera.Render();
            }
            finally
            {
                Find.World.renderer.wantedMode = previousMode;
                PlanetLayer.Selected = previousLayer;
                Patch_PhoenixOrbitBackground.Rendering = previousScope;
                Shader.SetGlobalFloat(ShaderPropertyIDs.PlanetSunLightEnabled, previousSunlight);
                Shader.SetGlobalFloat(ShaderPropertyIDs.BackgroundModeEnabled, previousBackground);
                Shader.SetGlobalVector(ShaderPropertyIDs.PlanetSunLightDirection, previousSunDirection);
            }
        }

        //再次打开时从航行后的实际位置显示，避免从旧位置穿越地球内部。
        internal void ResetView() { positioned = false; }

        //释放相机和纹理，不销毁原版共享的世界资源。
        internal void Dispose()
        {
            target.Release(); Object.Destroy(target); RimUnity.Object.Destroy(Texture); RimUnity.Object.Destroy(Presentation); Object.Destroy(root);
        }
    }
}
