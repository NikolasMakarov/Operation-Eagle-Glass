using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Verb_StrafingRun : Verb_CastAbility
    {
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
                    var comp = ability.comps.OfType<CompAbilityEffect_StrafingRun>().First();
                    comp.start = startPoint.Cell;
                    var direction = (endPoint.Cell - startPoint.Cell).ToVector3().normalized;
                    comp.end = startPoint.Cell + (direction * comp.Props.height).ToIntVec3();
                    base.TryStartCastOn(startPoint, endPoint, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
                },
                highlightAction: (LocalTargetInfo target) =>
                {
                    var comp = ability.comps.OfType<CompAbilityEffect_StrafingRun>().First();
                    var graphic = comp.Props.skyfallerDef.graphic;
                    var shadowMaterial = MaterialPool.MatFrom((Texture2D)graphic.MatSingle.mainTexture, ShaderDatabase.Transparent, new Color(0f, 0f, 0f, 0.25f));
                    Vector3 startVec = startPoint.Cell.ToVector3Shifted();
                    Vector3 endVecMouse = target.Cell.ToVector3Shifted();
                    Vector3 direction = (endVecMouse - startVec).normalized;
                    float angle = direction.AngleFlat();
                    Vector3 pos = startVec + direction * (comp.Props.height / 2f);
                    pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                    Vector3 s = new Vector3(comp.Props.width, 1f, comp.Props.height);
                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0f, angle, 0f), s);
                    Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
                },
                targetValidator: null,
                caster: CasterPawn
            );

            return false;
        }
    }
}
