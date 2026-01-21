using UnityEngine;
using Verse;
using Verse.Sound;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Skyfaller_StrafingRun : Skyfaller_VerbOwner
    {
        public IntVec3 start;
        public IntVec3 end;
        public Thing instigator;
        public float height;
        public float width;
        private Vector3 currentPos;
        private int ticksSinceSpawn;
        private Vector3 direction;
        public override Vector3 DrawPos => currentPos;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref end, "end");
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_Values.Look(ref height, "height");
            Scribe_Values.Look(ref width, "width");
            Scribe_Values.Look(ref currentPos, "currentPos");
            Scribe_Values.Look(ref ticksSinceSpawn, "ticksSinceSpawn");
            Scribe_Values.Look(ref direction, "direction");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                direction = (end.ToVector3() - start.ToVector3()).normalized;
                currentPos = start.ToVector3();
                while (currentPos.ToIntVec3().InBounds(Map))
                {
                    currentPos -= direction;
                }
                ticksToDiscard = 9999999;
                currentPos += direction;
                Position = currentPos.ToIntVec3();
            }
        }
        
        public override void Tick()
        {
            if (Spawned)
            {
                VerbTracker.VerbsTick();
            }
            base.Tick();
            currentPos += direction * def.skyfaller.speed;
            ticksSinceSpawn++;
            if (currentPos.ToIntVec3().InBounds(Map) is false)
            {
                Destroy();
                return;
            }
            Position = currentPos.ToIntVec3();

            Vector3 runVector = (end - start).ToVector3();
            Vector3 toCurrentPos = currentPos - start.ToVector3();

            float distanceAlongRun = Vector3.Dot(toCurrentPos, runVector.normalized);
            if (distanceAlongRun >= -10 && distanceAlongRun <= runVector.magnitude)
            {
                if (this.IsHashIntervalTick(PrimaryVerb.verbProps.ticksBetweenBurstShots))
                {
                    if (PrimaryVerb.verbProps.soundCast != null)
                    {
                        PrimaryVerb.verbProps.soundCast.PlayOneShot(new TargetInfo(currentPos.ToIntVec3(), Map));
                    }
                    var projectile = (Projectile)GenSpawn.Spawn(PrimaryVerb.verbProps.defaultProjectile, currentPos.ToIntVec3(), Map);
                    var widthInt = (int)width;
                    var randomOffset = new IntVec3(Rand.Range(-widthInt, widthInt), 0, Rand.Range(-widthInt, widthInt));
                    var targetCell = currentPos.ToIntVec3() + (direction * width).ToIntVec3() + randomOffset;
                    projectile.Launch(null, currentPos, targetCell, targetCell, ProjectileHitFlags.IntendedTarget);
                }
            }
        }
    }
}
