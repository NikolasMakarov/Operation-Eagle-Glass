using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace OperationEagleGlass
{
    public abstract class Skyfaller_VerbOwner : Skyfaller, IVerbOwner, IAttackTargetSearcher
    {
        private VerbTracker verbTracker;
        protected CompAmmoUser ammoComp;
        protected int burstCooldownTicksLeft;
        protected int burstWarmupTicksLeft;
        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        private const int TryStartShootSomethingIntervalTicks = 15;
        protected bool WarmingUp => burstWarmupTicksLeft > 0;
        public Verb AttackVerb => PrimaryVerb;

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
        public virtual string UniqueVerbOwnerID() => "Skyfaller_VerbOwner_" + ThingID;
        public bool VerbsStillUsableBy(Pawn p) => true;
        public List<VerbProperties> VerbProperties => def.Verbs;
        public List<Tool> Tools => null;
        public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

        Thing IAttackTargetSearcher.Thing => this;
        Verb IAttackTargetSearcher.CurrentEffectiveVerb => PrimaryVerb;
        public LocalTargetInfo LastAttackedTarget { get; set; }
        public int LastAttackTargetTick { get; set; }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_TargetInfo.Look(ref currentTargetInt, "currentTarget");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ammoComp = this.TryGetComp<CompAmmoUser>();
            if (PrimaryVerb != null)
            {
                PrimaryVerb.castCompleteCallback = OnBurstComplete;
            }
        }


        protected void HandleShootingLogic()
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

        private bool IsValidTarget(Thing t)
        {
            if (t == null) return false;
            if (t.Destroyed || (t is Pawn pawn && (pawn.Dead || pawn.Destroyed))) return false;
            if (t is Pawn pawn2 && pawn2.Downed) return false;
            if (t.Faction != null && !t.Faction.HostileTo(Faction.OfPlayer)) return false;
            if (t.Position.DistanceTo(this.Position) > PrimaryVerb.EffectiveRange) return false;
            if (!PrimaryVerb.CanHitTarget(t)) return false;
            return true;
        }

        protected virtual void OnOutOfAmmo()
        {
            Destroy();
        }
        
        protected virtual void BeginBurst()
        {
            if (currentTargetInt.IsValid)
            {
                if (ammoComp != null && !ammoComp.HasAmmo(1))
                {
                    OnOutOfAmmo();
                    return;
                }
        
                if (ammoComp != null)
                {
                    ammoComp.ConsumeAmmo(1);
                }
                AttackVerb.TryStartCastOn(currentTargetInt);
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
    }
}
