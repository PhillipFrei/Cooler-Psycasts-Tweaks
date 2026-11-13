using Verse;

namespace CoolerPsycastsTweaks
{
    /// <summary>
    /// A non-blocking temporary light source for the three gravity shockwaves.
    /// CompGlower illuminates nearby terrain; after 15 ticks this ethereal
    /// thing despawns automatically so light never accumulates on the map.
    /// </summary>
    public sealed class Thing_GravityPulseFlash : ThingWithComps
    {
        private int ticksRemaining = 15;

        public override void Tick()
        {
            base.Tick();
            if (--ticksRemaining <= 0 && !Destroyed)
                Destroy(DestroyMode.Vanish);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksRemaining, "CPT_glowTicks", 15);
        }
    }
}
