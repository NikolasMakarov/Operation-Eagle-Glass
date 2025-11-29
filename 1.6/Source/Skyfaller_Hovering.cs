using UnityEngine;
using Verse;
using Verse.AI;
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

    public class Skyfaller_Base : Skyfaller
    {
        private const float HoverOffset = 5f;
        public override Vector3 DrawPos
        {
            get
            {
                Vector3 pos = base.DrawPos;
                pos.z += HoverOffset;
                return pos;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10))
            {
                FleckMaker.ThrowDustPuffThick(this.Position.ToVector3Shifted(), this.Map, 2f, Color.white);
            }
        }
    }
    public class Skyfaller_Hovering : Skyfaller_Base, IVerbOwner, IAttackTargetSearcher
    {
        public int duration = -1;
        public VehicleAbility abilityType;
        private VerbTracker verbTracker;
        private CompAmmoUser ammoComp;
        private int burstCooldownTicksLeft;
        private int burstWarmupTicksLeft;
        private LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        private const int TryStartShootSomethingIntervalTicks = 15;
        private List<Pawn> pawnsToDeploy = new List<Pawn>();
        private int currentPawnIndex = 0;
        private bool isDeployingRope = false;
        private float ropeLength = 0f;
        private RopeState ropeState = RopeState.Extending;
        private Material ropeMaterial;
        public bool hasRopeDeployment = false;

        private const float EXTEND_SPEED = 0.3f;
        private const float RETRACT_SPEED = 0.5f;

        private enum RopeState { Extending, Retracting }

        public bool IsRopeDeploymentComplete => currentPawnIndex >= pawnsToDeploy.Count && !isDeployingRope && ropeLength <= 0f;

        private bool WarmingUp => burstWarmupTicksLeft > 0;

        public Verb AttackVerb => PrimaryVerb;

        private void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (!this.Spawned || !AttackVerb.Available())
            {
                ResetCurrentTarget();
                return;
            }

            currentTargetInt = TryFindNewTarget();

            if (currentTargetInt.IsValid)
            {
                float warmupTime = AttackVerb.verbProps.warmupTime;
                if (warmupTime > 0f)
                {
                    burstWarmupTicksLeft = (int)(warmupTime * 60);
                }
                else if (canBeginBurstImmediately)
                {
                    BeginBurst();
                }
                else
                {
                    burstWarmupTicksLeft = 1;
                }
            }
            else
            {
                ResetCurrentTarget();
            }
        }

        private bool IsValidTarget(Thing t)
        {
            if (t == null)
            {
                return false;
            }
            if (t.Destroyed || (t is Pawn pawn && (pawn.Dead || pawn.Destroyed)))
            {
                return false;
            }
            if (t is Pawn pawn2 && pawn2.Downed)
            {
                return false;
            }
            if (t.Faction != null && !t.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            if (t.Position.DistanceTo(this.Position) > PrimaryVerb.EffectiveRange)
            {
                return false;
            }
            if (!PrimaryVerb.CanHitTarget(t))
            {
                return false;
            }

            return true;
        }

        private LocalTargetInfo TryFindNewTarget()
        {
            var nearbyPawns = this.Map.mapPawns.AllPawnsSpawned;
            Pawn bestPawn = null;
            float bestDistance = float.MaxValue;
            foreach (var pawn in nearbyPawns)
            {
                if (IsValidTarget(pawn))
                {
                    float distance = pawn.Position.DistanceTo(this.Position);
                    if (distance < bestDistance && distance < PrimaryVerb.EffectiveRange)
                    {
                        bestDistance = distance;
                        bestPawn = pawn;
                    }
                }
            }

            if (bestPawn != null)
            {
                return bestPawn;
            }

            return LocalTargetInfo.Invalid;
        }

        private void BeginBurst()
        {
            if (currentTargetInt.IsValid)
            {
                if (ammoComp != null && ammoComp.HasAmmo(1))
                {
                    if (ammoComp.ConsumeAmmo(1))
                    {
                        AttackVerb.TryStartCastOn(currentTargetInt);
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

        private void OnBurstComplete()
        {
            burstCooldownTicksLeft = (int)(AttackVerb.verbProps.defaultCooldownTime * 60);
        }

        private void ResetCurrentTarget()
        {
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        private void HandleShootingLogic()
        {
            if (AttackVerb.state == VerbState.Bursting)
            {
                return;
            }

            if (WarmingUp)
            {
                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft <= 0)
                {
                    BeginBurst();
                }
            }
            else
            {
                if (burstCooldownTicksLeft > 0)
                {
                    burstCooldownTicksLeft--;
                }
                if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(TryStartShootSomethingIntervalTicks))
                {
                    TryStartShootSomething(canBeginBurstImmediately: true);
                }
            }
        }

        public VerbTracker VerbTracker
        {
            get
            {
                if (verbTracker == null)
                {
                    verbTracker = new VerbTracker(this);
                }
                return verbTracker;
            }
        }

        public List<Verb> AllVerbs => VerbTracker.AllVerbs;
        public Verb PrimaryVerb => VerbTracker.PrimaryVerb;
        public Thing ConstantCaster => this;
        public string UniqueVerbOwnerID() => "Skyfaller_Hovering_" + this.ThingID;
        public bool VerbsStillUsableBy(Pawn p) => true;
        public List<VerbProperties> VerbProperties => def.Verbs;
        public List<Tool> Tools => null;
        public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

        Thing IAttackTargetSearcher.Thing => this;
        Verb IAttackTargetSearcher.CurrentEffectiveVerb => PrimaryVerb;
        public LocalTargetInfo LastAttackedTarget { get; set; }
        public int LastAttackTargetTick { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ammoComp = this.TryGetComp<CompAmmoUser>();
            burstCooldownTicksLeft = 0;
            if (PrimaryVerb != null)
            {
                PrimaryVerb.castCompleteCallback = OnBurstComplete;
            }
            if (hasRopeDeployment)
            {
                Texture2D ropeTexture = ContentFinder<Texture2D>.Get("Effects/Rope", false);
                if (ropeTexture == null)
                {
                    ropeMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.1f, 0.05f));
                }
                else
                {
                    ropeMaterial = MaterialPool.MatFrom("Effects/Rope", ShaderDatabase.Transparent);
                }
            }
        }

        public override void Impact()
        {
            this.hasImpacted = true;
            if (hasRopeDeployment && pawnsToDeploy.Count > 0 && !isDeployingRope)
            {
                StartRopeDeployment();
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (!this.hasImpacted)
            {
                return;
            }

            if (this.Spawned)
            {
                VerbTracker.VerbsTick();
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

        private void TickSupport()
        {
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
                FleckMaker.ThrowDustPuffThick(this.Position.ToVector3Shifted(), this.Map, 3f,
                    new Color(0.7f, 0.7f, 0.7f, 2.5f));
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
            currentPawnIndex = 0;
            ropeLength = 0f;
            ropeState = RopeState.Extending;
        }

        private void TickRopeDeployment()
        {
            if (!isDeployingRope) return;
            float targetRopeLength = this.DrawPos.z - this.Position.z;

            if (ropeState == RopeState.Extending)
            {
                ropeLength += EXTEND_SPEED;

                if (this.IsHashIntervalTick(15))
                {
                    Vector3 dustPos = this.DrawPos;
                    dustPos.z -= ropeLength;
                    FleckMaker.ThrowDustPuffThick(dustPos, this.Map, 1.2f, Color.gray);
                }
                if (ropeLength >= targetRopeLength)
                {
                    ropeLength = targetRopeLength;
                    SpawnCurrentPawn();
                    ropeState = RopeState.Retracting;
                }
            }
            else if (ropeState == RopeState.Retracting)
            {
                ropeLength -= RETRACT_SPEED;

                if (ropeLength <= 0f)
                {
                    ropeLength = 0f;
                    currentPawnIndex++;

                    if (currentPawnIndex >= pawnsToDeploy.Count)
                    {
                        isDeployingRope = false;
                        pawnsToDeploy.Clear();
                    }
                    else
                    {
                        ropeState = RopeState.Extending;
                    }
                }
            }
        }

        private void SpawnCurrentPawn()
        {
            Pawn pawn = pawnsToDeploy[currentPawnIndex];
            GenSpawn.Spawn(pawn, this.Position, this.Map);
            FleckMaker.ThrowDustPuffThick(this.Position.ToVector3Shifted(), this.Map, 2f, Color.gray);
        }

        public override void DrawAt(Vector3 drawLoc, bool flipRot = false)
        {
            base.DrawAt(drawLoc, flipRot);

            if (!isDeployingRope || ropeLength <= 0f) return;
            Vector3 heliPos = drawLoc;
            heliPos.y = AltitudeLayer.Skyfaller.AltitudeFor();
            Vector3 ropeBottom = heliPos;
            ropeBottom.z -= ropeLength;
            
            DrawRope(heliPos, ropeBottom);
            if (ropeState == RopeState.Extending && currentPawnIndex < pawnsToDeploy.Count)
            {
                DrawPawnOnRope(pawnsToDeploy[currentPawnIndex], heliPos);
            }
        }

        private void DrawRope(Vector3 top, Vector3 bottom)
        {
            float length = top.z - bottom.z;
            Vector3 center = (top + bottom) / 2f;
            center.y = AltitudeLayer.Skyfaller.AltitudeFor() - 0.1f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.identity,
                new Vector3(0.2f, 1f, length)
            );

            Graphics.DrawMesh(MeshPool.plane10, matrix, ropeMaterial, 0);
        }

        private void DrawPawnOnRope(Pawn pawn, Vector3 heliPos)
        {
            Vector3 pawnPos = heliPos;
            pawnPos.z -= ropeLength;
            pawnPos.y = AltitudeLayer.Pawn.AltitudeFor();
            if (pawn != null && !pawn.Destroyed)
            {
                Rot4 rotation = Rot4.South;
                pawn.Drawer.renderer.DynamicDrawPhaseAt(DrawPhase.Draw, pawnPos, rotation);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", -1);
            Scribe_Values.Look(ref abilityType, "abilityType");
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_TargetInfo.Look(ref currentTargetInt, "currentTarget");
            Scribe_Values.Look(ref hasRopeDeployment, "hasRopeDeployment", false);
            Scribe_Collections.Look(ref pawnsToDeploy, "pawnsToDeploy", LookMode.Reference);
            Scribe_Values.Look(ref currentPawnIndex, "currentPawnIndex", 0);
            Scribe_Values.Look(ref isDeployingRope, "isDeployingRope", false);
            Scribe_Values.Look(ref ropeLength, "ropeLength", 0f);
            Scribe_Values.Look(ref ropeState, "ropeState", RopeState.Extending);
        }
    }
}
