using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;

namespace OperationEagleGlass
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
    
    [HotSwappable]
    public class Verb_HeliPlacement : Verb_CastAbility
    {
        private Rot4 rotation = Rot4.South;
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = false;
            return 0;
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            var comp = ability.comps.OfType<CompAbilityEffect_CallHelicopter>().FirstOrDefault();
            comp.forcedRotation = rotation;
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            if (!base.CanHitTarget(targ))
            {
                return false;
            }
            var lastAcceptanceReport = CheckCell(targ.Cell);
            return lastAcceptanceReport.Accepted;
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            HandleRotationInput();
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            var lastAcceptanceReport = CheckCell(target.Cell);
            Color ghostCol = lastAcceptanceReport.Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor;
            DrawGhost(ghostCol);
        }

        protected virtual void DrawGhost(Color ghostCol)
        {
            var compProps = ability.def.comps.OfType<CompProperties_AbilityCallHelicopter>().FirstOrDefault();
            GhostDrawer.DrawGhostThing(UI.MouseCell(), rotation, compProps.skyfallerDef, null, ghostCol, AltitudeLayer.Blueprint);
        }

        private void HandleRotationInput()
        {
            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
            {
                rotation.Rotate(RotationDirection.Clockwise);
                Event.current.Use();
            }
            if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
            {
                rotation.Rotate(RotationDirection.Counterclockwise);
                Event.current.Use();
            }
        }


        private AcceptanceReport CheckCell(IntVec3 center)
        {
            var compProps = ability.def.comps.OfType<CompProperties_AbilityCallHelicopter>().FirstOrDefault();

            var rect = GenAdj.OccupiedRect(center, rotation, compProps.skyfallerDef.size);
            foreach (var cell in rect)
            {
                var report = IsValidCell(cell, Caster.Map);
                if (!report.Accepted)
                {
                    return report;
                }
            }

            return true;
        }

        private static AcceptanceReport IsValidCell(IntVec3 cell, Map map)
        {
            return true;
        }
    }
}
