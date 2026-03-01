using HarmonyLib;
using PhoenixRaceLib.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Patch
{
    //给被破甲飞弹的单位增加橙色材质
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "GetMaterial")]
    public static class Patch_PawnRenderNodeWorker_GetMaterial
    {
        public static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Material __result)
        {
            if (__result == null || parms.pawn == null || PhoxDefRef.Hediff_ArmorShred == null) return;
            if (!parms.pawn.health.hediffSet.HasHediff(PhoxDefRef.Hediff_ArmorShred)) return;
            __result = OrangeMaterialCache.GetOrangeVersion(__result, parms.pawn);
        }
    }
}
