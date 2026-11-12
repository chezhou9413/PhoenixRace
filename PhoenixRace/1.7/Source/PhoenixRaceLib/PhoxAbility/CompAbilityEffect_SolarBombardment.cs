using RimWorld;
using Verse;
using PhoenixRaceLib.PhoxGameComp;

namespace PhoenixRaceLib.PhoxAbility
{
    public class CompProperties_AbilitySolarBombardment : CompProperties_AbilityEffect
    {
        public CompProperties_AbilitySolarBombardment()
        {
            compClass = typeof(CompAbilityEffect_SolarBombardment);
        }
    }

    public class CompAbilityEffect_SolarBombardment : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            GameComponent_SolarBombardmentManager.Instance?.QueueBombardment(
                parent.pawn?.MapHeld,
                target.Cell,
                parent.def?.EffectRadius ?? 0f,
                parent.pawn);
        }
    }
}
