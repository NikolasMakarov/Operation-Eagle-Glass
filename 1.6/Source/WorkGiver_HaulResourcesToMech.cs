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
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction)
                .Where(p => p.abilities?.AllAbilitiesForReading
                    .OfType<Ability_Resource>()
                    .Any(a => a.HasAnyAvailableSpace()) ?? false);
        }

        private class HaulableResourceInfo
        {
            public Thing Resource { get; set; }
            public int Count { get; set; }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn carrierPawn) || !carrierPawn.Spawned || carrierPawn.Downed)
                return false;

            if (!pawn.CanReserve(t, 1, -1, null, forced))
                return false;

            var abilities = carrierPawn.abilities?.AllAbilitiesForReading
                .OfType<Ability_Resource>()
                .ToList();

            if (abilities.NullOrEmpty())
                return false;

            return FindHaulableResource(pawn, abilities, forced) != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn carrierPawn))
                return null;

            var abilities = carrierPawn.abilities?.AllAbilitiesForReading
                .OfType<Ability_Resource>()
                .ToList();

            if (abilities.NullOrEmpty())
                return null;

            var resourceInfo = FindHaulableResource(pawn, abilities, forced);
            if (resourceInfo == null)
                return null;

            Job job = HaulAIUtility.HaulToContainerJob(pawn, resourceInfo.Resource, t);
            job.count = resourceInfo.Count;
            return job;
        }

        private HaulableResourceInfo FindHaulableResource(Pawn pawn, List<Ability_Resource> abilities, bool forced)
        {
            var allResourceTypes = abilities
                .SelectMany(a => a.maxResources)
                .Select(r => r.thingDef)
                .Distinct();

            foreach (var resourceDef in allResourceTypes)
            {
                int totalMaxToFill = abilities.Sum(a => a.GetMaxToFillForType(resourceDef));
                int totalCurrent = abilities.FirstOrDefault()?.ResourceCount(resourceDef) ?? 0;
                int availableSpace = totalMaxToFill - totalCurrent;

                if (availableSpace <= 0)
                    continue;

                Thing resource = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map,
                    pawn.Map.listerThings.ThingsOfDef(resourceDef),
                    PathEndMode.Touch,
                    TraverseParms.For(pawn),
                    9999f,
                    x => x.Spawned && !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced));

                if (resource != null)
                {
                    return new HaulableResourceInfo
                    {
                        Resource = resource,
                        Count = Mathf.Min(resource.stackCount, availableSpace)
                    };
                }
            }

            return null;
        }
    }
}
