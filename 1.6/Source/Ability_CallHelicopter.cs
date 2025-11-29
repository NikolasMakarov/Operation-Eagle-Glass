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
            if (gizmo == null)
            {
                gizmo = (Command)Activator.CreateInstance(def.gizmoClass, this, pawn);
                gizmo.Order = def.uiOrder;
            }
            var helicopterComp = this.EffectComps.OfType<CompAbilityEffect_CallHelicopter>().FirstOrDefault();
            if (helicopterComp != null)
            {
                if (helicopterComp.Skyfaller != null && !helicopterComp.Skyfaller.Destroyed)
                {
                    gizmo.defaultLabel = "OEG_RecallHelicopter".Translate();
                    gizmo.defaultDesc = "OEG_RecallHelicopterDesc".Translate();
                    gizmo.icon = ContentFinder<Texture2D>.Get("UI/Commands/Recall", true);
                }
                else
                {
                    gizmo.defaultLabel = def.LabelCap;
                    gizmo.defaultDesc = def.GetTooltip(pawn);
                    gizmo.icon = def.uiIcon;
                }
            }

            if (!pawn.Drafted || def.showWhenDrafted)
            {
                yield return gizmo;
            }
            
            if (DebugSettings.ShowDevGizmos && inCooldown && CanCooldown)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset cooldown",
                    action = delegate
                    {
                        inCooldown = false;
                        charges = maxCharges;
                    }
                };
            }
        }
    }
}