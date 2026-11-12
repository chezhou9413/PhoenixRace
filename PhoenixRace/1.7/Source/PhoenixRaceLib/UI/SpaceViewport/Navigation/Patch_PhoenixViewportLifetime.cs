using HarmonyLib;
using PhoenixRaceLib.PhoxGameComp;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //游戏实例切换前释放旧座舱，避免返回主菜单后遗留常驻画布和显存。
    [HarmonyPatch(typeof(Current), nameof(Current.Game), MethodType.Setter)]
    internal static class Patch_PhoenixViewportLifetime
    {
        //在原游戏组件仍可访问时清理其视觉资源。
        private static void Prefix(Game value)
        {
            if (Current.Game != value) GameComponent_PhoenixSpaceViewport.Instance?.Cleanup();
        }
    }
}
