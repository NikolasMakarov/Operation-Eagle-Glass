using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    public class Ability_CallHelicopter : Ability
    {
        public Ability_CallHelicopter() { }
        
        public Ability_CallHelicopter(Pawn pawn) : base(pawn) { }
        
        public Ability_CallHelicopter(Pawn pawn, Precept sourcePrecept) : base(pawn, sourcePrecept) { }
        
        public Ability_CallHelicopter(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        
        public Ability_CallHelicopter(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        public override IEnumerable<Command> GetGizmos()
        {
            var helicopterComp = this.EffectComps.OfType<CompAbilityEffect_CallHelicopter>().FirstOrDefault();

            foreach (var gizmo in base.GetGizmos())
            {
                if (helicopterComp.skyfaller != null && !helicopterComp.skyfaller.Destroyed)
                {
                    var recall = new Command_Action
                    {
                        defaultLabel = "OEG_RecallHelicopter".Translate(),
                        defaultDesc = "OEG_RecallHelicopterDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Recall", true),
                        action = delegate
                        {
                            helicopterComp.skyfaller.Depart();
                            helicopterComp.skyfaller = null;
                        }
                    };
                    yield return recall;
                }
                else
                {
                    yield return gizmo;
                }
            }
        }
    }
}
