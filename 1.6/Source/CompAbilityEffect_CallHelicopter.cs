using RimWorld;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class CompAbilityEffect_CallHelicopter : CompAbilityEffect_ResourceBase
    {
        public new CompProperties_AbilityCallHelicopter Props => (CompProperties_AbilityCallHelicopter)props;
        public Rot4? forcedRotation = null;
        public Skyfaller_Helicopter skyfaller = null;


        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var newSkyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef);
            if (newSkyfaller is Skyfaller_Helicopter hoveringHeli)
            {
                hoveringHeli.abilityType = Props.abilityType;
                hoveringHeli.duration = Props.durationTicks;
                hoveringHeli.hasRopeDeployment = Props.hasRopeDeployment;
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
            Rot4 spawnRot = forcedRotation ?? Rot4.South;
            GenSpawn.Spawn(newSkyfaller, target.Cell, parent.pawn.Map, spawnRot);
            forcedRotation = null;
            skyfaller = newSkyfaller as Skyfaller_Helicopter;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref skyfaller, "skyfaller");
        }
    }

    public class CompProperties_AbilityCallHelicopter : CompProperties_AbilityEffect_ResourceBase
    {
        public CompProperties_AbilityCallHelicopter()
        {
            compClass = typeof(CompAbilityEffect_CallHelicopter);
        }

        public ThingDef skyfallerDef;
        public int durationTicks;
        public List<PawnGenOption> pawnsToDrop;
        public VehicleAbility abilityType;
        public bool hasRopeDeployment;
    }

    public class PawnGenOption
    {
        public PawnKindDef kind;

        public int count;

        public float Cost => kind.combatPower;

        public override string ToString()
        {
            return string.Format("({0} c={1} co={2})", (kind != null) ? kind.ToString() : "null", count, (kind != null) ? Cost.ToString("F2") : "null");
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "kind", xmlRoot.Name);
            count = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }
}
