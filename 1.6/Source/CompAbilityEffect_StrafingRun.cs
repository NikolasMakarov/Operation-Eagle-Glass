using RimWorld;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class CompAbilityEffect_StrafingRun : CompAbilityEffect_ResourceBase
    {
        public new CompProperties_AbilityStrafingRun Props => (CompProperties_AbilityStrafingRun)props;
        public IntVec3 start;
        public IntVec3 end;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var newSkyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef) as Skyfaller_StrafingRun;
            newSkyfaller.start = start;
            newSkyfaller.end = end;
            newSkyfaller.instigator = parent.pawn;
            newSkyfaller.height = Props.height;
            newSkyfaller.width = Props.width;
            GenSpawn.Spawn(newSkyfaller, start, parent.pawn.Map);
        }
    }

    public class CompProperties_AbilityStrafingRun : CompProperties_AbilityEffect_ResourceBase
    {
        public ThingDef skyfallerDef;
        public float height;
        public float width;

        public CompProperties_AbilityStrafingRun()
        {
            compClass = typeof(CompAbilityEffect_StrafingRun);
        }
    }
}
