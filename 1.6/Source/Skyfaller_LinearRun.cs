using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OperationEagleGlass
{
    [HotSwappable]
    public abstract class Skyfaller_LinearRun : Skyfaller_VerbOwner
    {
        public IntVec3 start;
        public IntVec3 end;
        public Thing instigator;
        public float height;
        public float width;
        protected Vector3 currentPos;
        protected Vector3 direction;
        protected Dictionary<Verb, int> verbFireTicks = new Dictionary<Verb, int>();
        private List<Verb> verbKeys;
        private List<int> intValues;

        private const float FlightAltitudeOffset = 15f;

        public override Vector3 DrawPos
        {
            get
            {
                Vector3 pos = currentPos;
                pos.z += FlightAltitudeOffset;
                pos -= direction * FlightAltitudeOffset;
                return pos;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref end, "end");
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_Values.Look(ref height, "height");
            Scribe_Values.Look(ref width, "width");
            Scribe_Values.Look(ref currentPos, "currentPos");
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Collections.Look(ref verbFireTicks, "verbFireTicks", LookMode.Reference, LookMode.Value, ref verbKeys, ref intValues);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (verbFireTicks == null) verbFireTicks = new Dictionary<Verb, int>();
                foreach (var verb in AllVerbs)
                {
                    if (!verbFireTicks.ContainsKey(verb)) verbFireTicks[verb] = 0;
                }
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (verbFireTicks == null)
            {
                verbFireTicks = new Dictionary<Verb, int>();
            }
            foreach (var verb in AllVerbs)
            {
                if (!verbFireTicks.ContainsKey(verb))
                {
                    verbFireTicks[verb] = 0;
                }
            }

            if (!respawningAfterLoad)
            {
                InitializeRun();
            }
        }

        private void InitializeRun()
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

        public override void Tick()
        {
            if (Spawned)
            {
                VerbTracker.VerbsTick();
            }
            base.Tick();
            currentPos += direction * def.skyfaller.speed;

            if (!currentPos.ToIntVec3().InBounds(Map))
            {
                Destroy();
                return;
            }

            Position = currentPos.ToIntVec3();
            FireWeaponsIfInRange();
        }

        private void FireWeaponsIfInRange()
        {
            Vector3 runVector = (end - start).ToVector3();
            Vector3 toCurrentPos = currentPos - start.ToVector3();
            float distanceAlongRun = Vector3.Dot(toCurrentPos, runVector.normalized);

            if (distanceAlongRun >= -10 && distanceAlongRun <= runVector.magnitude)
            {
                foreach (var verb in AllVerbs)
                {
                    if (verbFireTicks[verb] > 0) verbFireTicks[verb]--;
                    if (verbFireTicks[verb] <= 0)
                    {
                        FireProjectile(verb);
                        verbFireTicks[verb] = verb.verbProps.ticksBetweenBurstShots;
                    }
                    ApplyAreaDamage(verb);
                }
            }
        }

        protected virtual void FireProjectile(Verb verb)
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

        protected virtual void ApplyAreaDamage(Verb verb)
        {
            
        }
    }
}
