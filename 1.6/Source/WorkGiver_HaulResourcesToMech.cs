using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OperationEagleGlass
{
    public class WorkGiver_HaulResourcesToMech : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn carrierPawn = (Pawn)t;
            if (!carrierPawn.Spawned || carrierPawn.Downed)
            {
                return false;
            }

            CompResourceLoader comp = carrierPawn.TryGetComp<CompResourceLoader>();
            if (comp == null)
            {
                return false;
            }

            if (!comp.HasAnyAvailableSpace())
            {
                return false;
            }

            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            foreach (var resourceDef in comp.Props.maxResources)
            {
                if (comp.GetAvailableSpace(resourceDef.thingDef) <= 0)
                {
                    continue;
                }

                List<Thing> resources = pawn.Map.listerThings.ThingsOfDef(resourceDef.thingDef)
                    .Where(x => x.Spawned && !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced))
                    .ToList();

                if (!resources.NullOrEmpty())
                {
                    return true;
                }
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var compResourceLoader = t.TryGetComp<CompResourceLoader>();
            if (compResourceLoader == null)
            {
                return null;
            }

            if (!compResourceLoader.HasAnyAvailableSpace())
            {
                return null;
            }
            foreach (var resourceDef in compResourceLoader.Props.maxResources)
            {
                int availableSpace = compResourceLoader.GetAvailableSpace(resourceDef.thingDef);

                if (availableSpace <= 0)
                {
                    continue;
                }

                List<Thing> resources = pawn.Map.listerThings.ThingsOfDef(resourceDef.thingDef)
                    .Where(x => x.Spawned && !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced))
                    .ToList();

                if (!resources.NullOrEmpty())
                {
                    Thing resource = resources.First();
                    int count = Mathf.Min(resource.stackCount, availableSpace);

                    Job job = HaulAIUtility.HaulToContainerJob(pawn, resource, t);
                    job.count = count;
                    return job;
                }
            }

            return null;
        }
    }
}
