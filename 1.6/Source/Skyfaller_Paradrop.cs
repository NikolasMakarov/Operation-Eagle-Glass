using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Skyfaller_Paradrop : Skyfaller_LinearRun
    {
        private List<Pawn> pawnsToDrop = new List<Pawn>();
        private int dropIntervalTicks;
        private int ticksUntilNextDrop;

        public void Initialize(List<Pawn> pawns, int interval)
        {
            pawnsToDrop = pawns;
            dropIntervalTicks = interval;
            ticksUntilNextDrop = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawnsToDrop, "pawnsToDrop", LookMode.Deep);
            Scribe_Values.Look(ref dropIntervalTicks, "dropIntervalTicks");
            Scribe_Values.Look(ref ticksUntilNextDrop, "ticksUntilNextDrop");
        }
        
        public override void Tick()
        {
            base.Tick();
            
            if (this.Spawned)
            {
                HandleDroppingLogic();
            }
        }

        private void HandleDroppingLogic()
        {
            if (pawnsToDrop.NullOrEmpty()) return;

            Vector3 runVector = (end - start).ToVector3();
            Vector3 toCurrentPos = currentPos - start.ToVector3();
            float distanceAlongRun = Vector3.Dot(toCurrentPos, runVector.normalized);
            
            if (distanceAlongRun >= 0 && distanceAlongRun <= runVector.magnitude)
            {
                ticksUntilNextDrop--;
                if (ticksUntilNextDrop <= 0)
                {
                    DropSinglePawn();
                    ticksUntilNextDrop = dropIntervalTicks;
                }
            }
        }
        
        private void DropSinglePawn()
        {
            if (pawnsToDrop.NullOrEmpty()) return;

            Pawn pawnToDrop = pawnsToDrop[0];
            pawnsToDrop.RemoveAt(0);

            var parachuter = (Skyfaller_ParachutingPawn)SkyfallerMaker.MakeSkyfaller(DefsOf.OEG_ParachutingPawn);
            parachuter.ContainedPawn = pawnToDrop;
            GenSpawn.Spawn(parachuter, currentPos.ToIntVec3(), Map);
        }
        
        protected override void FireProjectile(Verb verb) { }
    }
}
