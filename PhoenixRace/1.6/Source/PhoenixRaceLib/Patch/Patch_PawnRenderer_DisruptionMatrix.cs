using HarmonyLib;
using PhoenixRaceLib.Comp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Patch
{
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
    public static class Patch_PawnRenderer_DisruptionMatrix
    {
        [HarmonyPostfix]
        public static void Postfix(PawnRenderer __instance, Vector3 drawLoc, Pawn ___pawn)
        {
            // 基础检查
            if (___pawn == null || !___pawn.Spawned || ___pawn.Dead)
            {
                return;
            }

            // 检查Pawn是否有干扰矩阵Hediff
            Verse.Hediff hediff = ___pawn.health?.hediffSet?.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_DisruptionMatrix")
            );

            if (hediff == null)
            {
                return;
            }

            // 获取HediffComp并绘制覆盖层
            HediffComp_DisruptionMatrixOverlay overlayComp =
                hediff.TryGetComp<HediffComp_DisruptionMatrixOverlay>();

            if (overlayComp != null)
            {
                overlayComp.DrawOverlay(___pawn.DrawPos, ___pawn);
            }
        }
    }
}
