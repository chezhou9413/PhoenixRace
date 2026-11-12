using PhoenixRaceLib.PhoxGameComp;
using RimWorld;
using Verse;

namespace PhoenixRaceLib.PhoxAbility
{
    public class CompProperties_AbilityAirStrikeBombing : CompProperties_AbilityEffect
    {
        public float spawnHeight = 48f;
        public float spawnDistance = 60f;
        public float approachDistance = 36f;
        public float diveArcHeight = 4f;
        public float pullUpHeight = 44f;
        public float pullUpDistance = 108f;
        public int approachTicks = 36;
        public int diveTicks = 60;
        public int pullUpTicks = 48;
        public int barrelRollDelayTicks = 10;
        public int barrelRollDurationTicks = 72;
        public int damageAmount = 50;
        public float damageRadius = 2.9f;
        public DamageDef damageDef = null;
        public int explosionVisualLifeTicks = 5 * 60;

        public CompProperties_AbilityAirStrikeBombing()
        {
            compClass = typeof(CompAbilityEffect_AirStrikeBombing);
        }
    }

    public class CompAbilityEffect_AirStrikeBombing : CompAbilityEffect
    {
        public new CompProperties_AbilityAirStrikeBombing Props =>
            (CompProperties_AbilityAirStrikeBombing)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Map map = parent.pawn?.MapHeld;
            if (map == null || !target.IsValid || !target.Cell.IsValid || !target.Cell.InBounds(map))
            {
                return;
            }

            GameComponent_AirStrikeBombingManager.Instance?.QueueAirStrike(
                map,
                target.Cell,
                parent.pawn,
                Props);
        }
    }
}
