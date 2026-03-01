using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PhoenixRaceLib.Tool
{
    [DefOf]
    public static class PhoenixShieldStatDefof
    {
        public static StatDef Phoenix_ShieldMultiplier;
        public static StatDef Phoenix_ShieldOffset;
        public static StatDef Phoenix_ShieldRegenMultiplier;
        public static StatDef Phoenix_ShieldRegenOffset;
        public static StatDef Phoenix_ShieldBreakRecoveryTime;
        public static StatDef Phoenix_ShieldBreakRecoveryTimeMultiplier;
    }

    public static class PhoenixShieldStatUtility
    {
        /// <summary>
        /// 获取 Pawn 的最终护盾上限。
        /// 计算公式：(baseMaxShield * 护盾倍率) + 护盾增加
        /// </summary>
        public static float GetFinalMaxShield(Pawn pawn, float baseMaxShield)
        {
            if (pawn == null) return baseMaxShield;

            float multiplier = pawn.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldMultiplier);
            float offset = pawn.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldOffset);

            return baseMaxShield * multiplier + offset;
        }

        /// <summary>
        /// 获取 Pawn 的最终护盾每次回复量（每60 tick）。
        /// 计算公式：(baseRegenRate * 护盾回复倍率) + 护盾回复增加
        /// </summary>
        public static float GetFinalRegenRate(Pawn pawn, float baseRegenRate)
        {
            if (pawn == null) return baseRegenRate;

            float multiplier = pawn.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldRegenMultiplier);
            float offset = pawn.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldRegenOffset);

            return baseRegenRate * multiplier + offset;
        }

        /// <summary>
        /// 获取 Pawn 的最终破盾后复原等待时间（秒，后续换算为 tick 使用）。
        /// 计算公式：(baseRecoveryTime + 破盾复原时间) * 破盾复原时间倍率
        /// </summary>
        public static float GetFinalBreakRecoveryTime(Pawn pawn, float baseRecoveryTime)
        {
            if (pawn == null) return baseRecoveryTime;
            float offset = pawn.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldBreakRecoveryTime);
            float multiplier = pawn.GetStatValue(PhoenixShieldStatDefof.Phoenix_ShieldBreakRecoveryTimeMultiplier);
            return (baseRecoveryTime + offset) * multiplier;
        }
    }
}
