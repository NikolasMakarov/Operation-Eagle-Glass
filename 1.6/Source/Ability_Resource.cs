using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Ability_Resource : Ability, IThingHolder
    {
        public ThingOwner<Thing> innerContainer;

        private Dictionary<ThingDef, int> maxToFill = new Dictionary<ThingDef, int>();

        public IThingHolder ParentHolder => pawn;

        public List<ThingDefCountClass> MaxResources
        {
            get
            {
                return GetResourceProps().maxResources;
            }
        }

        public int ResourceCount(ThingDef resourceDef)
        {
            return GetAllResourceAbilities()
                .Sum(ability => ability.innerContainer.TotalStackCountOfDef(resourceDef));
        }

        public int GetTotalMaxResourceCountForType(ThingDef resourceDef)
        {
            return GetAllResourceAbilities()
                .Sum(ability => ability.GetMaxResourceCountForType(resourceDef));
        }

        public int GetAvailableSpace(ThingDef resourceDef)
        {
            return Mathf.Max(0, GetTotalMaxResourceCountForType(resourceDef) - ResourceCount(resourceDef));
        }

        public int GetLocalAvailableSpace(ThingDef resourceDef)
        {
            int max = GetMaxResourceCountForType(resourceDef);
            int current = innerContainer.TotalStackCountOfDef(resourceDef);
            return Mathf.Max(0, max - current);
        }

        public bool CanAcceptAmount(ThingDef resourceDef, int amount)
        {
            return GetAvailableSpace(resourceDef) >= amount;
        }

        public bool HasAnyAvailableSpace()
        {
            return MaxResources.Any(r => GetAvailableSpace(r.thingDef) > 0);
        }

        private CompAbilityEffect_ResourceBase GetResourceComp()
        {
            return comps.FirstOrDefault(c => c is CompAbilityEffect_ResourceBase) as CompAbilityEffect_ResourceBase;
        }

        private CompProperties_AbilityEffect_ResourceBase GetResourceProps()
        {
            return def.comps.FirstOrDefault(c => c is CompProperties_AbilityEffect_ResourceBase) as CompProperties_AbilityEffect_ResourceBase;
        }

        private List<Ability_Resource> cachedResourceAbilities;
        private int lastCacheTick = -1;

        public List<Ability_Resource> GetAllResourceAbilities()
        {
            if (cachedResourceAbilities == null || Find.TickManager.TicksGame - lastCacheTick > 60)
            {
                cachedResourceAbilities = pawn.abilities.AllAbilitiesForReading
                    .OfType<Ability_Resource>()
                    .ToList();
                lastCacheTick = Find.TickManager.TicksGame;
            }
            return cachedResourceAbilities;
        }

        private void DistributeToOtherAbilities(Thing resource)
        {
            var otherAbilities = GetAllResourceAbilities().Where(a => a != this);

            foreach (var ability in otherAbilities)
            {
                if (resource.stackCount <= 0) break;
                if (ability.CanAcceptResource(resource.def))
                {
                    ability.TryLoadResource(resource);
                }
            }

            if (resource.stackCount > 0)
                resource.Destroy();
        }

        public bool IsLowResourceCountForType(ThingDef resourceDef, int threshold = 50)
        {
            int currentForType = ResourceCount(resourceDef);
            return currentForType < threshold;
        }

        public Ability_Resource()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Ability_Resource(Pawn pawn) : base(pawn)
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Ability_Resource(Pawn pawn, Precept sourcePrecept) : base(pawn, sourcePrecept)
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Ability_Resource(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Ability_Resource(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def)
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public bool CanAcceptResource(ThingDef resourceDef)
        {
            return MaxResources.Exists(r => r.thingDef == resourceDef);
        }

        public bool TryLoadResource(Thing resource, int count = -1)
        {
            if (!CanAcceptResource(resource.def))
                return false;

            if (count == -1)
                count = resource.stackCount;

            int availableSpace = GetLocalAvailableSpace(resource.def);
            if (availableSpace <= 0)
                return false;

            int toLoad = Mathf.Min(count, availableSpace, resource.stackCount);
            if (toLoad <= 0)
                return false;

            Thing toAdd = resource.SplitOff(toLoad);
            bool added = innerContainer.TryAdd(toAdd);

            if (!added)
            {
                toAdd.Destroy();
                return false;
            }

            if (resource.stackCount > 0)
            {
                DistributeToOtherAbilities(resource);
            }

            return true;
        }

        public int GetMaxResourceCountForType(ThingDef resourceDef)
        {
            var resourceDefEntry = MaxResources.FirstOrDefault(r => r.thingDef == resourceDef);
            return resourceDefEntry != null ? resourceDefEntry.count : 0;
        }

        public int GetMaxToFillForType(ThingDef resourceDef)
        {
            if (maxToFill == null)
            {
                maxToFill = new Dictionary<ThingDef, int>();
            }

            if (maxToFill.ContainsKey(resourceDef))
            {
                return maxToFill[resourceDef];
            }
            return 0;
        }

        public void SetMaxToFillForType(ThingDef resourceDef, int value)
        {
            if (maxToFill == null)
            {
                maxToFill = new Dictionary<ThingDef, int>();
            }

            maxToFill[resourceDef] = value;
        }

        public bool WouldExceedTypeLimit(ThingDef resourceDef, int additionalCount = 1)
        {
            return !CanAcceptAmount(resourceDef, additionalCount);
        }

        public bool TryUnloadResource(out Thing resource)
        {
            List<Thing> things = innerContainer.InnerListForReading;
            if (things.Count > 0)
            {
                Thing thing = things[0];
                resource = innerContainer.Take(thing, thing.stackCount);
                return true;
            }
            resource = null;
            return false;
        }

        public bool ConsumeResource(ThingDef resourceDef, int count)
        {
            int remaining = count;
            var allResourceAbilities = GetAllResourceAbilities();
            foreach (var ability in allResourceAbilities)
            {
                if (remaining <= 0) break;

                int beforeConsume = remaining;
                int consumed = ConsumeFromContainer(ability.innerContainer, resourceDef, remaining);
                remaining -= consumed;
            }
            return remaining == 0;
        }

        private int ConsumeFromContainer(ThingOwner<Thing> container, ThingDef resourceDef, int count)
        {
            int consumed = 0;
            List<Thing> items = container.InnerListForReading.ToList();

            foreach (var item in items)
            {
                if (consumed >= count) break;
                if (item.def != resourceDef) continue;

                int toTake = Mathf.Min(count - consumed, item.stackCount);
                consumed += toTake;

                if (item.stackCount <= toTake)
                {
                    container.Remove(item);
                    item.Destroy();
                }
                else
                {
                    Thing split = item.SplitOff(toTake);
                    split.Destroy();
                }
            }

            return consumed;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Collections.Look(ref maxToFill, "maxToFill", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
                }
                if (maxToFill == null)
                {
                    maxToFill = new Dictionary<ThingDef, int>();
                }
            }
        }

        public override IEnumerable<Command> GetGizmos()
        {
            yield return new AbilityResourceGizmo(pawn);
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
    
        public override string Tooltip
        {
            get
            {
                string text = base.Tooltip;
                var costComp = GetResourceComp();
                StringBuilder costText = new StringBuilder();
                foreach (var cost in costComp.CostList)
                {
                    if (costText.Length > 0)
                    {
                        costText.AppendLine();
                    }
                    costText.Append(cost.thingDef.LabelCap + ": " + cost.count);
                }
                text = text + "\n\n" + "OEG_ResourceCost".Translate().Colorize(ColoredText.TipSectionTitleColor) + "\n" + costText.ToString();
                return text;
            }
        }
    }
}
