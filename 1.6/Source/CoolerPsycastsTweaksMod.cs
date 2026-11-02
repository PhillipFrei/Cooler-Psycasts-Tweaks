using HarmonyLib;
using Verse;

namespace CoolerPsycastsTweaks
{
    public sealed class CoolerPsycastsTweaksMod : Mod
    {
        public CoolerPsycastsTweaksMod(ModContentPack content) : base(content)
        {
            // This will patch only classes annotated with [HarmonyPatch] in OUR assembly.
            // The original Cooler Psycasts mod stays untouched.
            new Harmony("local.coolerpsycasts.tweaks").PatchAll(typeof(CoolerPsycastsTweaksMod).Assembly);
        }
    }
}
