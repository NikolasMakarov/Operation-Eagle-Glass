using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class CompAbilityEffect_Paradrop : CompAbilityEffect_LinearRunBase
    {
        public new CompProperties_AbilityParadrop Props => (CompProperties_AbilityParadrop)props;

        protected override Skyfaller_LinearRun CreateSkyfaller()
        {
            var skyfaller = (Skyfaller_Paradrop)SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef);
            skyfaller.height = Props.height;
            skyfaller.width = Props.width;
            
            var pawns = new List<Pawn>();
            if (Props.pawnsToDrop != null)
            {
                foreach (var pawnOption in Props.pawnsToDrop)
                {
                    for (int i = 0; i < pawnOption.count; i++)
                    {
                        Pawn mech = PawnGenerator.GeneratePawn(pawnOption.kind, parent.pawn.Faction);
                        pawns.Add(mech);
                    }
                }
            }
            skyfaller.Initialize(pawns, Props.dropIntervalTicks);
            return skyfaller;
        }
    }

    public class CompProperties_AbilityParadrop : CompProperties_AbilityLinearRunBase
    {
        public List<PawnGenOption> pawnsToDrop;
        public int dropIntervalTicks = 30;

        public CompProperties_AbilityParadrop()
        {
            compClass = typeof(CompAbilityEffect_Paradrop);
        }
    }
}
