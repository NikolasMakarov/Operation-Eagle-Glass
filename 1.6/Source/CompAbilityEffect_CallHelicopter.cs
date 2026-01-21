using RimWorld;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public abstract class CompAbilityEffect_CallHelicopter : CompAbilityEffect_ResourceBase
    {
        public new CompProperties_AbilityCallHelicopter Props => (CompProperties_AbilityCallHelicopter)props;
        public Rot4? forcedRotation = null;
        public Skyfaller_Helicopter skyfaller = null;


        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var newSkyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef);
            PostHelicopterSpawn(newSkyfaller);
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

        protected abstract void PostHelicopterSpawn(Thing helicopter);
    }

    public class CompProperties_AbilityCallHelicopter : CompProperties_AbilityEffect_ResourceBase
    {
        public ThingDef skyfallerDef;
        public int durationTicks;
        public List<PawnGenOption> pawnsToDrop;
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
