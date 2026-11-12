using HarmonyLib;
using RimWorld.Planet;

namespace PhoenixRaceLib.SpaceViewport
{
    //只在舷窗手动渲染期间使用奥德赛背景模式，让原版云层可见并隐藏地块选择网格。
    [HarmonyPatch(typeof(WorldRendererUtility), nameof(WorldRendererUtility.CurrentWorldRenderMode), MethodType.Getter)]
    internal static class Patch_PhoenixOrbitBackground
    {
        internal static bool Rendering;

        //保留正常世界界面的渲染规则，舷窗渲染完毕后由调用方结束背景作用域。
        private static bool Prefix(ref WorldRenderMode __result)
        {
            if (!Rendering) return true;
            __result = WorldRenderMode.Background;
            return false;
        }
    }
}
