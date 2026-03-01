using HarmonyLib;
using PhoenixRaceLib.PhoxThingComp;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PhoenixRaceLib.Patch
{
    /// <summary>
    /// 拦截 Pawn_HealthTracker.PreApplyDamage，
    /// 在伤害作用于 Pawn 之前让凤凰护盾尝试吸收。
    /// Skip 列表中的伤害类型直接穿透，其余全部拦截。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
    public static class Patch_PhoenixShield_PreApplyDamage
    {
        public static bool Prefix(Pawn_HealthTracker __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null) return true;
            ThingComp_PhoenixShield shield = FindActiveShield(pawn);
            if (shield == null) return true;
            if (shield.Skip != null && shield.Skip.Contains(dinfo.Def)) return true;
            if (!shield.ShieldActive) return true;
            float damage = dinfo.Amount;

            if (shield.curShield >= damage)
            {
                //护盾足够，完全吸收
                shield.curShield -= damage;
                shield.TriggerCombatCooldown();
                OnAbsorbed(shield, pawn, dinfo);
                absorbed = true;
                return false; // 阻止原方法，伤害不作用于 Pawn
            }
            else
            {
                //护盾不够，先耗尽护盾，剩余伤害穿透
                float remaining = damage - shield.curShield;
                shield.curShield = 0f;
                shield.BreakShield(pawn);
                //修改 dinfo 让剩余伤害继续走原方法
                dinfo.SetAmount(remaining);
                return true;
            }
        }

        // ── 找到穿戴者身上第一个激活的凤凰护盾 ────────────────────
        private static ThingComp_PhoenixShield FindActiveShield(Pawn pawn)
        {
            if (pawn.apparel == null) return null;

            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                ThingComp_PhoenixShield comp = worn[i].GetComp<ThingComp_PhoenixShield>();
                if (comp != null && comp.ShieldActive)
                    return comp;
            }
            return null;
        }

        // ── 吸收成功的视觉/音效 ─────────────────────────────────────
        private static void OnAbsorbed(ThingComp_PhoenixShield shield, Pawn pawn, DamageInfo dinfo)
        {
            if (!pawn.Spawned) return;

            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            Vector3 impactVec = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            Vector3 loc = pawn.TrueCenter() + impactVec.RotatedBy(180f) * 0.5f;
            float flashSize = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
            FleckMaker.Static(loc, pawn.Map, FleckDefOf.ExplosionFlash, flashSize);
            int dustCount = (int)flashSize;
            for (int i = 0; i < dustCount; i++)
                FleckMaker.ThrowDustPuff(loc, pawn.Map, Rand.Range(0.8f, 1.2f));
        }
    }
}
