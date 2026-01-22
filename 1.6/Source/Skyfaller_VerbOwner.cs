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
        protected Dictionary<Verb, int> verbCooldowns = new Dictionary<Verb, int>();
        protected Dictionary<Verb, int> verbWarmups = new Dictionary<Verb, int>();
        protected Dictionary<Verb, LocalTargetInfo> verbTargets = new Dictionary<Verb, LocalTargetInfo>();
        private List<Verb> verbKeys;
        private List<int> intValues;
        private List<LocalTargetInfo> targetInfoValues;
        private const int TryStartShootSomethingIntervalTicks = 15;

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
            Scribe_Collections.Look(ref verbCooldowns, "verbCooldowns", LookMode.Reference, LookMode.Value, ref verbKeys, ref intValues);
            Scribe_Collections.Look(ref verbWarmups, "verbWarmups", LookMode.Reference, LookMode.Value, ref verbKeys, ref intValues);
            Scribe_Collections.Look(ref verbTargets, "verbTargets", LookMode.Reference, LookMode.TargetInfo, ref verbKeys, ref targetInfoValues);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (verbCooldowns == null) verbCooldowns = new Dictionary<Verb, int>();
                if (verbWarmups == null) verbWarmups = new Dictionary<Verb, int>();
                if (verbTargets == null) verbTargets = new Dictionary<Verb, LocalTargetInfo>();
                foreach (var verb in AllVerbs)
                {
                    verb.castCompleteCallback = () => OnBurstComplete(verb);
                    if (!verbCooldowns.ContainsKey(verb)) verbCooldowns[verb] = 0;
                    if (!verbWarmups.ContainsKey(verb)) verbWarmups[verb] = 0;
                    if (!verbTargets.ContainsKey(verb)) verbTargets[verb] = LocalTargetInfo.Invalid;
                }
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ammoComp = this.TryGetComp<CompAmmoUser>();
            foreach (var verb in AllVerbs)
            {
                verb.castCompleteCallback = () => OnBurstComplete(verb);
                verbCooldowns[verb] = 0;
                verbWarmups[verb] = 0;
                verbTargets[verb] = LocalTargetInfo.Invalid;
            }
        }

        protected void HandleShootingLogic()
        {
            foreach (var verb in AllVerbs)
            {
                if (verb.state == VerbState.Bursting) continue;

                if (verbWarmups.TryGetValue(verb, out int warmupLeft) && warmupLeft > 0)
                {
                    verbWarmups[verb]--;
                    if (verbWarmups[verb] <= 0) BeginBurst(verb);
                }
                else
                {
                    if (verbCooldowns.TryGetValue(verb, out int cooldownLeft) && cooldownLeft > 0)
                    {
                        verbCooldowns[verb]--;
                    }
                    if (verbCooldowns[verb] <= 0 && this.IsHashIntervalTick(TryStartShootSomethingIntervalTicks))
                    {
                        TryStartShootSomething(verb, true);
                    }
                }
            }
        }

        private void TryStartShootSomething(Verb verb, bool canBeginBurstImmediately)
        {
            if (!this.Spawned || !verb.Available())
            {
                ResetCurrentTarget(verb);
                return;
            }

            verbTargets[verb] = TryFindNewTarget(verb);

            if (verbTargets[verb].IsValid)
            {
                float warmupTime = verb.verbProps.warmupTime;
                if (warmupTime > 0f)
                {
                    verbWarmups[verb] = (int)(warmupTime * 60);
                }
                else if (canBeginBurstImmediately)
                {
                    BeginBurst(verb);
                }
                else
                {
                    verbWarmups[verb] = 1;
                }
            }
            else
            {
                ResetCurrentTarget(verb);
            }
        }

        private LocalTargetInfo TryFindNewTarget(Verb verb)
        {
            var nearbyPawns = this.Map.mapPawns.AllPawnsSpawned;
            Pawn bestPawn = null;
            float bestDistance = float.MaxValue;
            foreach (var pawn in nearbyPawns)
            {
                if (IsValidTarget(pawn, verb))
                {
                    float distance = pawn.Position.DistanceTo(this.Position);
                    if (distance < bestDistance && distance < verb.EffectiveRange)
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

        private bool IsValidTarget(Thing t, Verb verb)
        {
            if (t == null) return false;
            if (t.Destroyed || (t is Pawn pawn && (pawn.Dead || pawn.Destroyed))) return false;
            if (t is Pawn pawn2 && pawn2.Downed) return false;
            if (t.Faction != null && !t.Faction.HostileTo(Faction.OfPlayer)) return false;
            if (t.Position.DistanceTo(this.Position) > verb.EffectiveRange) return false;
            if (!verb.CanHitTarget(t)) return false;
            return true;
        }

        protected virtual void OnOutOfAmmo()
        {
            Destroy();
        }
        
        protected virtual void BeginBurst(Verb verb)
        {
            if (verbTargets[verb].IsValid)
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
                verb.TryStartCastOn(verbTargets[verb]);
            }
        }

        private void OnBurstComplete(Verb verb)
        {
            verbCooldowns[verb] = (int)(verb.verbProps.defaultCooldownTime * 60);
        }

        private void ResetCurrentTarget(Verb verb)
        {
            verbTargets[verb] = LocalTargetInfo.Invalid;
            verbWarmups[verb] = 0;
        }
    }
}
