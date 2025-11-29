using RimWorld;
using Verse;

namespace OperationEagleGlass
{
    public class CompAbilityEffect_CallHelicopter_Support : CompAbilityEffect_CallHelicopter
    {
        protected override void PostHelicopterSpawn(Thing helicopter)
        {
            if (helicopter is Skyfaller_Hovering hoveringHeli)
            {
                hoveringHeli.abilityType = VehicleAbility.Support;
                hoveringHeli.duration = Props.durationTicks;
                hoveringHeli.hasRopeDeployment = false;
            }
        }
    }
    public class CompProperties_AbilityCallHelicopter_Support : CompProperties_AbilityCallHelicopter { public CompProperties_AbilityCallHelicopter_Support() { compClass = typeof(CompAbilityEffect_CallHelicopter_Support); } }
    public class CompAbilityEffect_CallHelicopter_Transport : CompAbilityEffect_CallHelicopter
    {
        protected override void PostHelicopterSpawn(Thing helicopter)
        {
            if (helicopter is Skyfaller_Hovering hoveringHeli)
            {
                hoveringHeli.abilityType = VehicleAbility.Transport;
                hoveringHeli.hasRopeDeployment = true;
                if (Props.pawnsToDrop != null)
                {
                    foreach (var pawnOption in Props.pawnsToDrop)
                    {
                        for (int i = 0; i < pawnOption.count; i++)
                        {
                            Pawn mech = PawnGenerator.GeneratePawn(pawnOption.kind, Faction.OfPlayer);
                            hoveringHeli.AddPawnToDeploy(mech);
                        }
                    }
                }
            }
        }
    }
    public class CompProperties_AbilityCallHelicopter_Transport : CompProperties_AbilityCallHelicopter { public CompProperties_AbilityCallHelicopter_Transport() { compClass = typeof(CompAbilityEffect_CallHelicopter_Transport); } }
    public class CompAbilityEffect_CallHelicopter_Reinforcement : CompAbilityEffect_CallHelicopter
    {
        protected override void PostHelicopterSpawn(Thing helicopter)
        {
            if (helicopter is Skyfaller_Hovering hoveringHeli)
            {
                hoveringHeli.abilityType = VehicleAbility.Reinforcement;
                hoveringHeli.duration = Props.durationTicks;
                hoveringHeli.hasRopeDeployment = true;
                if (Props.pawnsToDrop != null)
                {
                    foreach (var pawnOption in Props.pawnsToDrop)
                    {
                        for (int i = 0; i < pawnOption.count; i++)
                        {
                            Pawn mech = PawnGenerator.GeneratePawn(pawnOption.kind, Faction.OfPlayer);
                            hoveringHeli.AddPawnToDeploy(mech);
                        }
                    }
                }
            }
        }
    }
    public class CompProperties_AbilityCallHelicopter_Reinforcement : CompProperties_AbilityCallHelicopter { public CompProperties_AbilityCallHelicopter_Reinforcement() { compClass = typeof(CompAbilityEffect_CallHelicopter_Reinforcement); } }
}
