using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public abstract class CompAbilityEffect_CallHelicopter : CompAbilityEffect
    {
        public new CompProperties_AbilityCallHelicopter Props => (CompProperties_AbilityCallHelicopter)props;
        public Rot4? forcedRotation = null;
        public Skyfaller_Hovering skyfaller = null;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var ability = parent as Ability_Resource;
            if (ability != null && Props.costList != null)
            {
                foreach (var cost in Props.costList)
                {
                    var allAbilities = ability.GetAllResourceAbilities();
                    foreach (var ab in allAbilities)
                    {
                        int steelInThis = ab.innerContainer.TotalStackCountOfDef(cost.thingDef);
                        Log.Message($"[OEG] Ability {ab.def.defName} has {steelInThis} {cost.thingDef.defName}");
                    }

                    int available = ability.ResourceCount(cost.thingDef);
                    Log.Message($"[OEG] Valid check: need {cost.count} {cost.thingDef.defName}, have {available}");

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
            if (ability != null && Props.costList != null)
            {
                foreach (var cost in Props.costList)
                {
                    if (!ability.ConsumeResource(cost.thingDef, cost.count))
                    {
                        Messages.Message("OEG_FailedToConsumeResources".Translate(), MessageTypeDefOf.RejectInput);
                        return;
                    }
                }
            }

            base.Apply(target, dest);
            var newSkyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef);
            PostHelicopterSpawn(newSkyfaller);
            Rot4 spawnRot = forcedRotation ?? Rot4.South;
            GenSpawn.Spawn(newSkyfaller, target.Cell, parent.pawn.Map, spawnRot);
            forcedRotation = null;
            skyfaller = newSkyfaller as Skyfaller_Hovering;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref skyfaller, "skyfaller");
        }

        protected abstract void PostHelicopterSpawn(Thing helicopter);
    }

    public class CompProperties_AbilityCallHelicopter : CompProperties_AbilityEffect
    {
        public ThingDef skyfallerDef;
        public int durationTicks;
        public List<PawnGenOption> pawnsToDrop;
        public List<ThingDefCountClass> costList;
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
