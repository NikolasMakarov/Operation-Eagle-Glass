using HarmonyLib;
using Verse;

namespace OperationEagleGlass
{
    [HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.TryGetInnerInteractableThingOwner))]
    public static class ThingOwnerUtility_TryGetInnerInteractableThingOwner_Patch
    {
        public static void Postfix(Thing thing, ref ThingOwner __result)
        {
            if (thing is Pawn pawn && pawn.abilities != null)
            {
                foreach (var ability in pawn.abilities.AllAbilitiesForReading)
                {
                    if (ability is Ability_Resource resourceAbility && resourceAbility.HasAnyAvailableSpace())
                    {
                        __result = resourceAbility.GetDirectlyHeldThings();
                        return;
                    }
                }
            }
        }
    }
}
