using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public abstract class CompAbilityEffect_LinearRunBase : CompAbilityEffect_ResourceBase
    {
        public IntVec3 start;
        public IntVec3 end;

        protected abstract Skyfaller_LinearRun CreateSkyfaller();
        public new CompProperties_AbilityLinearRunBase Props => (CompProperties_AbilityLinearRunBase)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var newSkyfaller = CreateSkyfaller();
            newSkyfaller.start = start;
            newSkyfaller.end = end;
            newSkyfaller.instigator = parent.pawn;
            GenSpawn.Spawn(newSkyfaller, start, parent.pawn.Map);
        }
    }

    public abstract class CompProperties_AbilityLinearRunBase : CompProperties_AbilityEffect_ResourceBase
    {
        public ThingDef skyfallerDef;
        public float height;
        public float width;
    }
}
