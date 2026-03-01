using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PhoenixRaceLib.Patch
{
    /// </summary>
    [HarmonyPatch(typeof(Ability), "CanCast", MethodType.Getter)]
    public static class Patch_PreventAbilityCast
    {
        [HarmonyPostfix]
        public static void Postfix(Ability __instance, ref AcceptanceReport __result)
        {
            // 如果已经不可用，直接返回
            if (!__result.Accepted)
            {
                return;
            }

            // 检查是否受干扰矩阵影响
            if (__instance.pawn != null && HasDisruptionMatrix(__instance.pawn))
            {
                __result = new AcceptanceReport("受到干扰矩阵影响，无法使用技能");
            }
        }

        private static bool HasDisruptionMatrix(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;
            return pawn.health.hediffSet.HasHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_DisruptionMatrix")
            );
        }
    }

    /// <summary>
    /// 补丁：阻止受干扰矩阵影响的Pawn进行攻击
    /// </summary>
    [HarmonyPatch(typeof(Verb), "TryStartCastOn", new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
    public static class Patch_PreventAttack
    {
        [HarmonyPrefix]
        public static bool Prefix(Verb __instance)
        {
            // 检查施放者是否受干扰矩阵影响
            if (__instance.CasterPawn != null && HasDisruptionMatrix(__instance.CasterPawn))
            {
                // 显示提示信息
                Messages.Message(
                    "受到干扰矩阵影响，无法攻击",
                    __instance.CasterPawn,
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return false; // 阻止攻击
            }
            return true;
        }

        private static bool HasDisruptionMatrix(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;
            return pawn.health.hediffSet.HasHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_DisruptionMatrix")
            );
        }
    }

    /// <summary>
    /// 补丁：阻止AI选择攻击目标
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class Patch_PreventAIAttack
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (HasDisruptionMatrix(pawn))
            {
                __result = null;
                return false; // 阻止AI获得攻击Job
            }
            return true;
        }

        private static bool HasDisruptionMatrix(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return false;
            return pawn.health.hediffSet.HasHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_DisruptionMatrix")
            );
        }
    }
}
