using System.Linq;
using RimWorld;
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

            var widthInt = Mathf.CeilToInt(width / 2f);
            var forwardOffset = (direction * 10).ToIntVec3();
            var perpendicularDir = new Vector3(direction.z, 0, -direction.x).normalized;
            var perpendicularOffset = (perpendicularDir * Rand.Range(-widthInt, widthInt)).ToIntVec3();
            var targetCell = currentPos.ToIntVec3() + forwardOffset + perpendicularOffset;

            var projectile = (Projectile)GenSpawn.Spawn(verb.verbProps.defaultProjectile, currentPos.ToIntVec3(), Map);
            projectile.Launch(instigator, spawnPos, targetCell, targetCell, ProjectileHitFlags.All);
        }

        protected override void ApplyAreaDamage(Verb verb)
        {
            var projectileDef = verb.verbProps.defaultProjectile;
            if (projectileDef == null) return;

            var damageAmount = projectileDef.projectile.GetDamageAmount(null);
            var armorPenetration = projectileDef.projectile.GetArmorPenetration(null);

            var widthInt = Mathf.CeilToInt(width / 2f);
            var forwardOffset = (direction * 10).ToIntVec3();
            var perpendicularDir = new Vector3(direction.z, 0, -direction.x).normalized;

            for (int x = -widthInt; x <= widthInt; x++)
            {
                var perpendicularOffset = (perpendicularDir * x).ToIntVec3();
                var cell = currentPos.ToIntVec3() + forwardOffset + perpendicularOffset;

                if (!cell.InBounds(Map)) continue;

                var things = cell.GetThingList(Map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (things[i] is Pawn pawn && !pawn.Dead)
                    {
                        var dinfo = new DamageInfo(projectileDef.projectile.damageDef ?? DamageDefOf.Bullet, damageAmount, armorPenetration, -1f, instigator);
                        pawn.TakeDamage(dinfo);
                    }
                }
            }
        }
    }
}
