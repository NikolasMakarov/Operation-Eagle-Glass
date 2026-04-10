using RimWorld;
using UnityEngine;
using Verse;
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
            var compProps = ability.def.comps.OfType<CompProperties_AbilityCallHelicopter>().FirstOrDefault();
            if (compProps != null && compProps.skyfallerDef != null && !compProps.skyfallerDef.verbs.NullOrEmpty())
            {
                return compProps.skyfallerDef.verbs[0].range;
            }
            return 0;
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            var comp = ability.comps.OfType<CompAbilityEffect_CallHelicopter>().FirstOrDefault();
            comp.forcedRotation = rotation;
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            var lastAcceptanceReport = CheckCell(target.Cell);
            if (!lastAcceptanceReport.Accepted)
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(ability.def.label) + ": " + lastAcceptanceReport.Reason, MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.ValidateTarget(target, showMessages: showMessages);
        }

        public override void OnGUI(LocalTargetInfo target)
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

        public override void DrawHighlight(LocalTargetInfo target)
        {
            var lastAcceptanceReport = CheckCell(target.Cell);
            Color ghostCol = lastAcceptanceReport.Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor;
            DrawGhost(ghostCol);
            base.DrawHighlight(target);
        }

        protected virtual void DrawGhost(Color ghostCol)
        {
            var compProps = ability.def.comps.OfType<CompProperties_AbilityCallHelicopter>().FirstOrDefault();
            GhostDrawer.DrawGhostThing(UI.MouseCell(), rotation, compProps.skyfallerDef, null, ghostCol, AltitudeLayer.Blueprint);
        }

        private static AcceptanceReport IsValidCell(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map)) return false;
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Skyfaller_Helicopter || things[i] is Skyfaller_Hovering)
                {
                    return false;
                }
            }
            var report = HasNearbyHelicopter(cell, map);
            if (!report.Accepted)
            {
                return report;
            }
            return true;
        }

        private static AcceptanceReport HasNearbyHelicopter(IntVec3 cell, Map map)
        {
            foreach (var thing in map.listerThings.AllThings)
            {
                if (thing is Skyfaller_Helicopter || thing is Skyfaller_Hovering)
                {
                    if (thing.Position.InHorDistOf(cell, 7f))
                    {
                        return "OEG_HelicopterTooClose".Translate();
                    }
                }
            }
            return true;
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

    }
}
