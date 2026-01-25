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

            var widthInt = (int)width;
            var forwardOffset = (direction * 10).ToIntVec3();
            var perpendicularDir = new Vector3(direction.z, 0, -direction.x);
            var perpendicularOffset = (perpendicularDir * Rand.Range(-widthInt, widthInt)).ToIntVec3();
            var targetCell = currentPos.ToIntVec3() + forwardOffset + perpendicularOffset;

            var projectile = (Projectile)GenSpawn.Spawn(verb.verbProps.defaultProjectile, currentPos.ToIntVec3(), Map);
            projectile.Launch(null, spawnPos, targetCell, targetCell, ProjectileHitFlags.All);
        }

        protected override void ApplyAreaDamage(Verb verb)
        {
            var projectileDef = verb.verbProps.defaultProjectile;
            if (projectileDef == null) return;

            var damageAmount = projectileDef.projectile.GetDamageAmount(null);
            var armorPenetration = projectileDef.projectile.GetArmorPenetration(null);

            var widthInt = (int)width;
            var forwardOffset = (direction * 10).ToIntVec3();
            var perpendicularDir = new Vector3(direction.z, 0, -direction.x);

            for (int x = -widthInt; x <= widthInt; x++)
            {
                var perpendicularOffset = (perpendicularDir * x).ToIntVec3();
                var cell = currentPos.ToIntVec3() + forwardOffset + perpendicularOffset;

                if (!cell.InBounds(Map)) continue;

                var things = Map.thingGrid.ThingsAt(cell).ToList();
                foreach (var thing in things)
                {
                    if (thing is Pawn pawn && !pawn.Dead && !pawn.Downed && pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
                    {
                        var dinfo = new DamageInfo(DamageDefOf.Bullet, damageAmount, armorPenetration, -1f, instigator);
                        pawn.TakeDamage(dinfo);
                    }
                }
            }
        }
    }
}
