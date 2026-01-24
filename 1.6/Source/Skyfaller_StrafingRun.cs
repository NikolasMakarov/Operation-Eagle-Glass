using UnityEngine;
using Verse;
using Verse.Sound;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Skyfaller_StrafingRun : Skyfaller_LinearRun
    {
        protected override void FireProjectile(Verb verb)
        {
            if (verb.verbProps.soundCast != null)
            {
                verb.verbProps.soundCast.PlayOneShot(SoundInfo.InMap(new TargetInfo(currentPos.ToIntVec3(), Map)));
            }

            Vector3 spawnPos = DrawPos;

            var widthInt = (int)width;
            var forwardOffset = (direction * 10).ToIntVec3();
            var perpendicularDir = new Vector3(direction.z, 0, -direction.x);
            var perpendicularOffset = (perpendicularDir * Rand.Range(-widthInt, widthInt)).ToIntVec3();
            var targetCell = currentPos.ToIntVec3() + forwardOffset + perpendicularOffset;

            var projectile = (Projectile)GenSpawn.Spawn(verb.verbProps.defaultProjectile, currentPos.ToIntVec3(), Map);
            projectile.Launch(null, spawnPos, targetCell, targetCell, ProjectileHitFlags.All);
        }
    }
}
