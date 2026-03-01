using HarmonyLib;
using Verse;

namespace PhoenixRaceLib
{
    [StaticConstructorOnStartup]
    public static class PhoenixMain
    {
        static Harmony harmony;

        static PhoenixMain()
        {
            harmony = new Harmony("chezhou.Race.Phoenix");
            harmony.PatchAll(typeof(PhoenixMain).Assembly);
        }
    }
}
