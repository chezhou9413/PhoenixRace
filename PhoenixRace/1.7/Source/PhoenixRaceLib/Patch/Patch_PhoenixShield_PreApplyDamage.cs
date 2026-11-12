using HarmonyLib;
using PhoenixRaceLib.PhoxThingComp;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PhoenixRaceLib.Patch
{
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
                shield.curShield -= damage;
                shield.TriggerCombatCooldown();
                OnAbsorbed(shield, pawn, dinfo);
                absorbed = true;
                return false;
            }
            else
            {
                float remaining = damage - shield.curShield;
                shield.curShield = 0f;
                // 破盾也让表面闪一下
                shield.AddImpact(dinfo.Angle, 2.5f);
                shield.BreakShield(pawn);
                dinfo.SetAmount(remaining);
                return true;
            }
        }

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

            // ★ 触发护盾表面波纹
            float intensity = Mathf.Clamp(0.8f + dinfo.Amount / 30f, 0.8f, 2.5f);
            shield.AddImpact(dinfo.Angle, intensity);
        }
    }
}