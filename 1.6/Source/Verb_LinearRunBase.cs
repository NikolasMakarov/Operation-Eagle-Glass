using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;
using System.Collections.Generic;

namespace OperationEagleGlass
{
    [HotSwappable]
    public abstract class Verb_LinearRunBase : Verb_CastAbility
    {
        protected abstract CompAbilityEffect_LinearRunBase GetComp();

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            var startPoint = castTarg;
            var targetingParameters = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = false,
            };

            Find.Targeter.BeginTargeting(
                targetingParameters,
                action: (LocalTargetInfo endPoint) =>
                {
                    var comp = GetComp();
                    comp.start = startPoint.Cell;
                    var direction = (endPoint.Cell - startPoint.Cell).ToVector3().normalized;
                    comp.end = startPoint.Cell + (direction * comp.Props.height).ToIntVec3();
                    base.TryStartCastOn(startPoint, endPoint, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
                },
                highlightAction: (LocalTargetInfo target) =>
                {
                    var comp = GetComp();
                    Vector3 startVec = startPoint.Cell.ToVector3Shifted();
                    Vector3 endVecMouse = target.Cell.ToVector3Shifted();
                    Vector3 direction = (endVecMouse - startVec).normalized;
                    Vector3 perpendicular = new Vector3(direction.z, 0, -direction.x);
                    Vector3 halfWidth = perpendicular * (comp.Props.width / 2f);
                    Vector3 endVec = startVec + direction * comp.Props.height;
                    List<IntVec3> cells = new List<IntVec3>();
                    int lengthSegments = Mathf.CeilToInt(comp.Props.height * 2);
                    int widthSegments = Mathf.CeilToInt(comp.Props.width * 2);
                    for (int i = 0; i <= lengthSegments; i++)
                    {
                        float lengthT = (float)i / lengthSegments;
                        Vector3 centerPos = Vector3.Lerp(startVec, endVec, lengthT);
                        for (int j = 0; j <= widthSegments; j++)
                        {
                            float widthT = (float)j / widthSegments;
                            Vector3 offset = Vector3.Lerp(-halfWidth, halfWidth, widthT);
                            IntVec3 cell = (centerPos + offset).ToIntVec3();
                            if (cell.InBounds(CasterPawn.Map))
                            {
                                cells.Add(cell);
                            }
                        }
                    }
                    cells = cells.Distinct().ToList();
                    GenDraw.DrawFieldEdges(cells, Color.white);
                },
                targetValidator: null,
                caster: CasterPawn
            );

            return false;
        }
    }
}
