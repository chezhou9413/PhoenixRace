using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PhoenixRaceLib.PhoxVerb
{
    /// <summary>
    /// 干扰矩阵技能 - 发射追踪型干扰弹
    /// </summary>
    public class Verb_LaunchDisruptionMatrix : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (currentTarget == null || !currentTarget.IsValid)
            {
                return false;
            }

            Pawn casterPawn = CasterPawn;
            if (casterPawn == null)
            {
                return false;
            }

            Map map = casterPawn.Map;
            if (map == null)
            {
                return false;
            }

            // 获取抛射体定义
            ThingDef projectileDef = verbProps.defaultProjectile;
            if (projectileDef == null)
            {
                return false;
            }

            // 生成抛射体
            Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, casterPawn.Position, map);

            // 发射
            Vector3 origin = casterPawn.DrawPos;
            LocalTargetInfo target = currentTarget;

            projectile.Launch(
                launcher: casterPawn,
                origin: origin,
                usedTarget: target,
                intendedTarget: target,
                hitFlags: ProjectileHitFlags.IntendedTarget,
                preventFriendlyFire: true,
                equipment: null
            );

            // 音效（如果有的话）
            if (verbProps.soundCast != null)
            {
                verbProps.soundCast.PlayOneShot(new TargetInfo(casterPawn.Position, map));
            }

            // 枪口闪光
            if (verbProps.muzzleFlashScale > 0)
            {
                FleckMaker.Static(origin, map, FleckDefOf.ShotFlash, verbProps.muzzleFlashScale);
            }
            return true;
        }
    }
}