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

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var ext = def.GetModExtension<HelicopterSkyfallerExtension>();
            if (ext != null && ext.gunGraphic != null)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    gunGraphic = ext.gunGraphic.Graphic;
                });
            }
            if (hasRopeDeployment)
            {
                float extendSpeed = ext?.ropeExtendSpeed ?? 0.1f;
                float descentTime = ext?.ropeDescentTime ?? 1f;
                Color ropeColor = ext?.ropeColor ?? new Color(0.15f, 0.1f, 0.05f);
                float ropeWidth = ext?.ropeWidth ?? 0.2f;
                Vector2 leftDrawOffset = ext?.leftRopeDrawOffset ?? new Vector2(-2f, 0f);
                Vector2 rightDrawOffset = ext?.rightRopeDrawOffset ?? new Vector2(2f, 0f);
                leftRope = new Rope(leftDrawOffset, extendSpeed, descentTime, ropeColor, ropeWidth);
                rightRope = new Rope(rightDrawOffset, extendSpeed, descentTime, ropeColor, ropeWidth);
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
                Vector3 dustPos = DrawPos;
                dustPos.y -= 50f;
                FleckMaker.ThrowDustPuffThick(dustPos, this.Map, 5f, Color.white);
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
            if (gunGraphic != null)
            {
                Vector3 gunDrawLoc = drawLoc;
                GetDrawPositionAndRotation(ref gunDrawLoc, out var extraRotation);
                var ext = def.GetModExtension<HelicopterSkyfallerExtension>();
                Vector2? gunOffset = Rotation.AsInt switch
                {
                    0 => ext?.gunDrawOffsetNorth,
                    1 => ext?.gunDrawOffsetEast,
                    2 => ext?.gunDrawOffsetSouth,
                    3 => ext?.gunDrawOffsetWest,
                    _ => null
                };
                Vector3 gunOffsetVec = gunOffset.HasValue ? new Vector3(gunOffset.Value.x, 0, gunOffset.Value.y) : Vector3.zero;
                gunOffsetVec = Quaternion.Euler(0, extraRotation, 0) * gunOffsetVec;
                Vector3 gunPos = gunDrawLoc + gunOffsetVec;
                gunPos.y = AltitudeLayer.Building.AltitudeFor();
                gunGraphic.Draw(gunPos, flipRot ? Rotation.Opposite : Rotation, this, extraRotation);
            }

            base.DrawAt(drawLoc, flipRot);

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
