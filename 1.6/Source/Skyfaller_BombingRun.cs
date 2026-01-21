using UnityEngine;
using Verse;
using Verse.Sound;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Skyfaller_BombingRun : Skyfaller_LinearRun
    {
        protected override void FireProjectile(Verb verb)
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
