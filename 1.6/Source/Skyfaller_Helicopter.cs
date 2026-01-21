using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace OperationEagleGlass
{
    public enum VehicleAbility
    {
        Support,
        Transport,
        Reinforcement
    }

    [HotSwappable]
    public class Skyfaller_Helicopter : Skyfaller_VerbOwner
    {
        private const float HoverOffset = 5f;
        public int duration = -1;
        public VehicleAbility abilityType;
        private List<Pawn> pawnsToDeploy = new List<Pawn>();
        private bool isDeployingRope = false;
        private Rope leftRope;
        private Rope rightRope;
        public bool hasRopeDeployment = false;
        private Graphic gunGraphic;

        public bool IsRopeDeploymentComplete => leftRope != null && rightRope != null && leftRope.IsComplete && rightRope.IsComplete && pawnsToDeploy.Count == 0;

        public override Vector3 DrawPos
        {
            get
            {
                Vector3 pos = base.DrawPos;
                pos.z += HoverOffset;
                return pos;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            Log.Message("Destroying " + this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (hasRopeDeployment)
            {
                leftRope = new Rope(-1f);
                rightRope = new Rope(1f);
            }
            var ext = def.GetModExtension<HelicopterSkyfallerExtension>();
            if (ext != null && ext.gunGraphic != null)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    gunGraphic = ext.gunGraphic.Graphic;
                });
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Spawned)
            {
                VerbTracker.VerbsTick();
            }
            if (this.IsHashIntervalTick(10))
            {
                FleckMaker.ThrowDustPuffThick(this.Position.ToVector3Shifted(), this.Map, 5f, Color.white);
            }
            if (!this.hasImpacted)
            {
                return;
            }

            if (hasRopeDeployment && isDeployingRope)
            {
                TickRopeDeployment();
            }

            if (abilityType == VehicleAbility.Support || abilityType == VehicleAbility.Reinforcement)
            {
                HandleShootingLogic();
            }

            switch (abilityType)
            {
                case VehicleAbility.Support:
                    TickSupport();
                    break;
                case VehicleAbility.Transport:
                    TickTransport();
                    break;
                case VehicleAbility.Reinforcement:
                    TickReinforcement();
                    break;
            }
        }

        public override void Impact()
        {
            hasImpacted = true;
            if (hasRopeDeployment && pawnsToDeploy.Count > 0 && !isDeployingRope)
            {
                StartRopeDeployment();
            }
        }

        protected override void OnOutOfAmmo()
        {
            Depart();
        }

        protected override void BeginBurst(Verb verb)
        {
            if (verbTargets[verb].IsValid)
            {
                if (ammoComp != null && ammoComp.HasAmmo(1))
                {
                    if (ammoComp.ConsumeAmmo(1))
                    {
                        verb.TryStartCastOn(verbTargets[verb]);
                    }
                    else
                    {
                        Depart();
                    }
                }
                else
                {
                    Depart();
                }
            }
        }

        private void TickSupport()
        {
            HandleShootingLogic();
            if (duration > 0)
            {
                duration--;
                if (duration == 0)
                {
                    Depart();
                    return;
                }
            }
        }

        private void TickTransport()
        {
            if (hasRopeDeployment && IsRopeDeploymentComplete)
            {
                Depart();
            }
        }

        private void TickReinforcement()
        {
            if (hasRopeDeployment && !IsRopeDeploymentComplete)
            {
                return;
            }

            if (duration > 0)
            {
                duration--;
                if (duration == 0)
                {
                    Depart();
                    return;
                }
            }
        }

        public void Depart()
        {
            if (this.Destroyed) return;
            for (int i = 0; i < 3; i++)
            {
                FleckMaker.ThrowDustPuffThick(this.Position.ToVector3Shifted(), this.Map, 3f, new Color(0.7f, 0.7f, 0.7f, 2.5f));
            }

            var ext = def.GetModExtension<HelicopterSkyfallerExtension>();
            var skyfaller = SkyfallerMaker.MakeSkyfaller(ext.leavingSkyfaller);
            GenSpawn.Spawn(skyfaller, this.Position, this.Map, this.Rotation);
            this.Destroy(DestroyMode.Vanish);
        }

        public void AddPawnToDeploy(Pawn pawn)
        {
            pawnsToDeploy.Add(pawn);
        }

        public void StartRopeDeployment()
        {
            isDeployingRope = true;
        }

        private void TickRopeDeployment()
        {
            if (!isDeployingRope) return;

            Vector3 heliPos = this.DrawPos;
            heliPos.y = AltitudeLayer.Skyfaller.AltitudeFor();

            leftRope.Tick(heliPos, this.Position, this.Map);
            rightRope.Tick(heliPos, this.Position, this.Map);

            if (this.IsHashIntervalTick(15))
            {
                Vector3 dustPos = heliPos;
                dustPos.x -= 1f;
                dustPos.z -= leftRope.Length;
                FleckMaker.ThrowDustPuffThick(dustPos, this.Map, 1.2f, Color.gray);

                dustPos = heliPos;
                dustPos.x += 1f;
                dustPos.z -= rightRope.Length;
                FleckMaker.ThrowDustPuffThick(dustPos, this.Map, 1.2f, Color.gray);
            }

            AssignPawnsToRopes();

            if (pawnsToDeploy.Count == 0 && !leftRope.HasPawn && !rightRope.HasPawn)
            {
                leftRope.StartRetracting();
                rightRope.StartRetracting();
            }
        }

        private void AssignPawnsToRopes()
        {
            if (pawnsToDeploy.Count == 0) return;

            if (leftRope.IsReady)
            {
                leftRope.AssignPawn(pawnsToDeploy[0]);
                pawnsToDeploy.RemoveAt(0);
            }

            if (pawnsToDeploy.Count > 0 && rightRope.IsReady)
            {
                rightRope.AssignPawn(pawnsToDeploy[0]);
                pawnsToDeploy.RemoveAt(0);
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flipRot = false)
        {
            base.DrawAt(drawLoc, flipRot);

            if (gunGraphic != null)
            {
                gunGraphic.Draw(drawLoc, base.Rotation, this);
            }

            if (!isDeployingRope) return;

            Vector3 heliPos = drawLoc;
            heliPos.y = AltitudeLayer.Skyfaller.AltitudeFor();

            leftRope.Draw(heliPos);
            rightRope.Draw(heliPos);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", -1);
            Scribe_Values.Look(ref abilityType, "abilityType");
            Scribe_Values.Look(ref hasRopeDeployment, "hasRopeDeployment", false);
            Scribe_Collections.Look(ref pawnsToDeploy, "pawnsToDeploy", LookMode.Deep);
            Scribe_Values.Look(ref isDeployingRope, "isDeployingRope", false);
            Scribe_Deep.Look(ref leftRope, "leftRope");
            Scribe_Deep.Look(ref rightRope, "rightRope");
        }
    }
}
