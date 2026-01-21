using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OperationEagleGlass
{
    public abstract class CompAbilityEffect_ResourceBase : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_ResourceBase Props => (CompProperties_AbilityEffect_ResourceBase)props;
        public List<ThingDefCountClass> CostList => Props.costList;
        public List<ThingDefCountClass> MaxResources => Props.maxResources;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var ability = parent as Ability_Resource;
            if (ability != null && CostList != null)
            {
                foreach (var cost in CostList)
                {
                    int available = ability.ResourceCount(cost.thingDef);
                    if (available < cost.count)
                    {
                        if (throwMessages)
                            Messages.Message("OEG_NotEnoughResources".Translate(cost.thingDef.LabelCap, cost.count, available),
                                MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
            }
            return base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var ability = parent as Ability_Resource;
            if (ability != null && CostList != null)
            {
                foreach (var cost in CostList)
                {
                    if (!ability.ConsumeResource(cost.thingDef, cost.count))
                    {
                        Messages.Message("OEG_FailedToConsumeResources".Translate(), MessageTypeDefOf.RejectInput);
                        return;
                    }
                }
            }
            base.Apply(target, dest);
        }
    }

    public class CompProperties_AbilityEffect_ResourceBase : CompProperties_AbilityEffect
    {
        public List<ThingDefCountClass> costList;
        public List<ThingDefCountClass> maxResources;
    }
}
