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
    public class Verb_LaunchHomingMissile : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            Log.Message($"[HomingMissile] TryCastShot 触发!");

            if (currentTarget == null || !currentTarget.IsValid)
            {
                Log.Warning("[HomingMissile] 目标无效");
                return false;
            }

            Pawn casterPawn = CasterPawn;
            if (casterPawn == null)
            {
                Log.Warning("[HomingMissile] 施法者为空");
                return false;
            }

            Map map = casterPawn.Map;
            if (map == null)
            {
                Log.Warning("[HomingMissile] 地图为空");
                return false;
            }

            // 获取抛射体定义
            ThingDef projectileDef = verbProps.defaultProjectile;
            if (projectileDef == null)
            {
                Log.Error("[HomingMissile] defaultProjectile 未设置!");
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

            // 音效
            //if (verbProps.soundCast != null)
            //{
            //    verbProps.soundCast.PlayOneShot(new TargetInfo(casterPawn.Position, map));
            //}

            // 枪口闪光
            if (verbProps.muzzleFlashScale > 0)
            {
                FleckMaker.Static(origin, map, FleckDefOf.ShotFlash, verbProps.muzzleFlashScale);
            }

            Log.Message($"[HomingMissile] 发射成功! 抛射体: {projectile}, 目标: {target.Thing}");

            return true;
        }

    }
}
