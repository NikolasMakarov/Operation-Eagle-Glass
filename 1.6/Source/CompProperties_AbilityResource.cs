using System.Collections.Generic;
using RimWorld;
using Verse;

namespace OperationEagleGlass
{
    public class CompProperties_AbilityResource : CompProperties_AbilityEffect
    {
        public List<ThingDefCountClass> maxResources;

        public CompProperties_AbilityResource()
        {
            compClass = typeof(CompAbilityEffect_Resource);
        }
    }
}
