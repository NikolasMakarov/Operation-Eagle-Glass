using RimWorld;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class CompAbilityEffect_StrafingRun : CompAbilityEffect_LinearRunBase
    {
        public new CompProperties_AbilityStrafingRun Props => (CompProperties_AbilityStrafingRun)props;

        protected override Skyfaller_LinearRun CreateSkyfaller()
        {
            var skyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef) as Skyfaller_StrafingRun;
            skyfaller.height = Props.height;
            skyfaller.width = Props.width;
            return skyfaller;
        }
    }

    public class CompProperties_AbilityStrafingRun : CompProperties_AbilityLinearRunBase
    {
        public CompProperties_AbilityStrafingRun()
        {
            compClass = typeof(CompAbilityEffect_StrafingRun);
        }
    }
}
