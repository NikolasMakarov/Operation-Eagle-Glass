using UnityEngine;
using Verse;
using Verse.Sound;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Skyfaller_BombingRun : Skyfaller_LinearRun
    {
        public bool hitCenter = false;
        private bool hasDroppedAtCenter = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hitCenter, "hitCenter", false);
            Scribe_Values.Look(ref hasDroppedAtCenter, "hasDroppedAtCenter", false);
        }

        protected override void FireProjectile(Verb verb)
        {
            if (hitCenter)
            {
                if (hasDroppedAtCenter)
                {
                    return;
                }

                Vector3 centerPoint = (start.ToVector3() + end.ToVector3()) / 2f;
                Vector3 runVector = (end - start).ToVector3();
                Vector3 toCurrentPos = currentPos - start.ToVector3();
                float distanceAlongRun = Vector3.Dot(toCurrentPos, runVector.normalized);
                float centerDistance = runVector.magnitude / 2f;

                if (distanceAlongRun >= centerDistance)
                {
                    if (verb.verbProps.soundCast != null)
                    {
                        verb.verbProps.soundCast.PlayOneShot(SoundInfo.InMap(new TargetInfo(centerPoint.ToIntVec3(), Map)));
                    }

                    Vector3 spawnPos = DrawPos;
                    var targetCell = centerPoint.ToIntVec3();

                    var projectile = (Projectile)GenSpawn.Spawn(verb.verbProps.defaultProjectile, centerPoint.ToIntVec3(), Map);
                    projectile.Launch(null, spawnPos, targetCell, targetCell, ProjectileHitFlags.IntendedTarget);

                    hasDroppedAtCenter = true;
                }
            }
            else
            {
                if (verb.verbProps.soundCast != null)
                {
                    verb.verbProps.soundCast.PlayOneShot(SoundInfo.InMap(new TargetInfo(currentPos.ToIntVec3(), Map)));
                }

                Vector3 spawnPos = DrawPos;
                var targetCell = currentPos.ToIntVec3();

                var projectile = (Projectile)GenSpawn.Spawn(verb.verbProps.defaultProjectile, currentPos.ToIntVec3(), Map);
                projectile.Launch(null, spawnPos, targetCell, targetCell, ProjectileHitFlags.IntendedTarget);
            }
        }
    }
}
