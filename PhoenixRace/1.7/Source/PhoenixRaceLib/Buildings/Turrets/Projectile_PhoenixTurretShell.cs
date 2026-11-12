using RimWorld;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Buildings.Turrets;

//保留原版命中与掩体判定，仅将三联装炮弹的起点对齐实际炮口。
public sealed class Projectile_PhoenixTurretShell : Bullet
{
    private PhoenixShellVisual visual;
    private bool visualFailed;
    internal float FlightFraction => DistanceCoveredFraction;
    internal float FlightTicks => StartingTicksToImpact;
    //原版 Verb 仍负责散布、命中率和目标选择。
    public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
        LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false,
        Thing equipment = null, ThingDef targetCoverDef = null)
    {
        if (launcher is Building_PhoenixTurret turret) origin = turret.Model.FireFromMuzzle();
        base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags,
            preventFriendlyFire, equipment, targetCoverDef);
    }

    //炮弹实体和拖尾共用同一条三维抛物线，碰撞沿用原版水平弹道。
    internal Vector3 SamplePath(float fraction)
    {
        Vector3 position = Vector3.Lerp(origin, destination, fraction);
        float height = Mathf.Min(def.projectile.arcHeightFactor, (destination - origin).MagnitudeHorizontal() * .04f);
        position.y = Mathf.Lerp(origin.y, def.Altitude, fraction) + 4 * height * fraction * (1 - fraction);
        return position;
    }

    public override Vector3 ExactPosition => SamplePath(FlightFraction);

    //弹头方向沿抛物线切线改变，包含起飞和下落时的俯仰。
    public override Quaternion ExactRotation => Quaternion.LookRotation(
        SamplePath(Mathf.Min(1, FlightFraction + .01f)) - SamplePath(Mathf.Max(0, FlightFraction - .01f)));

    //首次进入视野才创建实体，省去地图外弹丸的场景开销。
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (visual != null || visualFailed || Map != Find.CurrentMap || this.Fogged()) return;
        try { visual = PhoenixShellVisual.Create(this); }
        catch (System.Exception error)
        {
            visualFailed = true;
            Log.Error("[PhoenixRace] 炮弹模型创建失败：\n" + error);
        }
    }

    //命中粒子先读取有效地图与位置，原版随后处理伤害和弹丸销毁。
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (Map == Find.CurrentMap && !this.Fogged())
            PhoenixTurretEffect.Spawn("Phoenix_TurretImpact", Map, ExactPosition.ToRim(),
                RimUnity.Quaternion.LookRotation(RimUnity.Vector3.up));
        base.Impact(hitThing, blockedByShield);
    }

    //弹体消失后保留短暂尾迹，再由视觉组件自行释放。
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (visual != null) { visual.Finish(); visual = null; }
        base.DeSpawn(mode);
    }
}
