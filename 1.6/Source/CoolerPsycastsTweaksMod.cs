using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;

namespace CoolerPsycastsTweaks
{
    public sealed class CoolerPsycastsTweaksMod : Mod
    {
        public override string SettingsCategory() => "Cooler Psycasts Tweaks";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            TimeshuntVault vault = Current.Game?.GetComponent<TimeshuntVault>();
            int pending = vault?.PendingCount ?? 0;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f),
                "Timeshunt recovery: " + pending + " pawn(s) stored globally");
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + 40f, 330f, 35f),
                "Emergency: return stored Timeshunt pawns"))
            {
                int count = vault?.ReturnDuePawns(true) ?? 0;
                Messages.Message("Timeshunt: " + count + " pawn(s) returned. " +
                    (vault?.PendingCount ?? 0) + " still stored.", MessageTypeDefOf.PositiveEvent);
            }
        }

        public CoolerPsycastsTweaksMod(ModContentPack content) : base(content)
        {
            // This will patch only classes annotated with [HarmonyPatch] in OUR assembly.
            // The original Cooler Psycasts mod stays untouched.
            new Harmony("local.coolerpsycasts.tweaks").PatchAll(typeof(CoolerPsycastsTweaksMod).Assembly);
        }
    }
}
