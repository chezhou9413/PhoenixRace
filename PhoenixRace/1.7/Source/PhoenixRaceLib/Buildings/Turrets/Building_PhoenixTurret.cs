using RimWorld;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.Buildings.Turrets;

//沿用原版索敌、供电、冷却和存档，模型显示交给刚性骨骼组件。
public sealed class Building_PhoenixTurret : Building_TurretGun
{
    public CompPhoenixTurretModel Model => GetComp<CompPhoenixTurretModel>();

    //玩家的自动炮塔也可以指定目标和停火。
    protected override bool CanSetForcedTarget => Faction == Faction.OfPlayer;

    //索敌和连射由原版推进，真实转向角度再写回炮塔顶。
    protected override void Tick()
    {
        base.Tick();
        Model.TickAim(CurrentTarget.IsValid && !CurrentTarget.ThingDestroyed ? Model.AngleTo(CurrentTarget) : Top.CurRotation);
        Top.CurRotation = Model.CurrentYaw;
    }

    //完成转向后才开始三连射，防止炮管仍朝背面时弹丸已经飞向目标。
    protected override void BeginBurst()
    {
        if (CurrentTarget.IsValid && !CurrentTarget.ThingDestroyed && !Model.IsAimed(CurrentTarget))
        {
            burstWarmupTicksLeft = 1;
            return;
        }
        base.BeginBurst();
    }

    //成品只绘制模型和组件覆盖层，避免叠加原版平面底座与炮塔顶。
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (!Spawned) Graphic.Draw(drawLoc, Rotation, this);
        Comps_DrawAt(drawLoc, flip);
        Comps_PostDraw();
    }
}
