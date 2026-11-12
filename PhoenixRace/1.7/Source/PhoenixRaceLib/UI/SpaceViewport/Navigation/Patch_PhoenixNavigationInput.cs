using HarmonyLib;
using PhoenixRaceLib.PhoxGameComp;
using RimWorld;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.SpaceViewport
{
    //座舱开启期间隐藏原版面板，保留根入口的缩放及 RimUnity 的画布输入与绘制。
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    internal static class Patch_PhoenixNavigationInput
    {
        //只跳过原版方法体，RimUnity 在同一方法上的前后缀仍会处理画布。
        private static bool Prefix()
        {
            GameComponent_PhoenixSpaceViewport viewport = GameComponent_PhoenixSpaceViewport.Instance;
            if (viewport == null || !viewport.IsVisible) return true;
            Event input = Event.current;
            if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape)
            {
                viewport.HandleEscape(); input.Use(); return false;
            }
            if (input.type == EventType.KeyDown)
            {
                if (input.keyCode == KeyCode.Space) Find.TickManager.TogglePaused();
                else if (input.keyCode == KeyCode.Alpha1) Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                else if (input.keyCode == KeyCode.Alpha2) Find.TickManager.CurTimeSpeed = TimeSpeed.Fast;
                else if (input.keyCode == KeyCode.Alpha3) Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                input.Use();
            }
            return false;
        }
    }
}
