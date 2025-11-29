using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OperationEagleGlass
{
    public class CompResourceLoader : ThingComp, IThingHolder
    {
        private ThingOwner<Thing> innerContainer;

        public CompProperties_ResourceLoader Props => (CompProperties_ResourceLoader)props;

        public int ResourceCount(ThingDef resourceDef)
        {
            return innerContainer.TotalStackCountOfDef(resourceDef);
        }

        public int GetAvailableSpace(ThingDef resourceDef)
        {
            int max = GetMaxResourceCountForType(resourceDef);
            int current = ResourceCount(resourceDef);
            return Mathf.Max(0, max - current);
        }

        public bool CanAcceptAmount(ThingDef resourceDef, int amount)
        {
            return GetAvailableSpace(resourceDef) >= amount;
        }

        public bool HasAnyAvailableSpace()
        {
            foreach (var resourceDef in Props.maxResources)
            {
                if (GetAvailableSpace(resourceDef.thingDef) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsLowResourceCountForType(ThingDef resourceDef, int threshold = 50)
        {
            int currentForType = ResourceCount(resourceDef);
            return currentForType < threshold;
        }

        public override void PostPostMake()
        {
            innerContainer = new ThingOwner<Thing>((IThingHolder)this, oneStackOnly: false);
            if (Props.startingResources != null)
            {
                foreach (var resource in Props.startingResources)
                {
                    Thing thing = ThingMaker.MakeThing(resource.thingDef);
                    thing.stackCount = resource.count;
                    innerContainer.TryAdd(thing);
                }
            }
        }

        public bool CanAcceptResource(ThingDef resourceDef)
        {
            return Props.maxResources.Exists(r => r.thingDef == resourceDef);
        }

        public bool TryLoadResource(Thing resource, int count = -1)
        {
            if (!CanAcceptResource(resource.def))
            {
                return false;
            }

            if (count == -1)
            {
                count = resource.stackCount;
            }
            int availableSpace = GetAvailableSpace(resource.def);
            if (availableSpace <= 0)
            {
                return false;
            }
            
            count = Mathf.Min(count, availableSpace);
            
            if (count <= 0)
            {
                return false;
            }

            Thing split = resource.SplitOff(count);
            bool added = innerContainer.TryAdd(split);
            if (!added)
            {
                split.Destroy();
            }
            return added;
        }

        public int GetMaxResourceCountForType(ThingDef resourceDef)
        {
            var resourceDefEntry = Props.maxResources?.FirstOrDefault(r => r.thingDef == resourceDef);
            return resourceDefEntry != null ? resourceDefEntry.count : 0;
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
            if (ResourceCount(resourceDef) < count)
            {
                return false;
            }

            int remaining = count;
            List<Thing> items = innerContainer.InnerListForReading.ToList();

            foreach (var item in items)
            {
                if (remaining <= 0) break;
                if (item.def != resourceDef) continue;

                int canTake = Mathf.Min(remaining, item.stackCount);
                remaining -= canTake;

                if (item.stackCount <= canTake)
                {
                    innerContainer.Remove(item);
                    item.Destroy();
                }
                else
                {
                    Thing split = item.SplitOff(canTake);
                    split.Destroy();
                }
            }

            return true;
        }


        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }
    }
}
