using RimWorld;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class CompAbilityEffect_BombingRun : CompAbilityEffect_LinearRunBase
    {
        public new CompProperties_AbilityBombingRun Props => (CompProperties_AbilityBombingRun)props;

        protected override Skyfaller_LinearRun CreateSkyfaller()
        {
            var skyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef) as Skyfaller_BombingRun;
            skyfaller.height = Props.height;
            skyfaller.width = Props.width;
            return skyfaller;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Vector3 direction = (end - start).ToVector3().normalized;
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x).normalized;

            for (int i = 0; i < Props.planeCount; i++)
            {
                var skyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfallerDef) as Skyfaller_BombingRun;
                skyfaller.height = Props.height;
                skyfaller.width = Props.width;
                skyfaller.instigator = parent.pawn;

                float offset = (i - (Props.planeCount - 1) / 2f) * Props.planeSpacing;
                Vector3 offsetVector = perpendicular * offset;

                skyfaller.start = (start.ToVector3() + offsetVector).ToIntVec3();
                skyfaller.end = (end.ToVector3() + offsetVector).ToIntVec3();

                GenSpawn.Spawn(skyfaller, skyfaller.start, parent.pawn.Map);
            }
        }
    }

    public class CompProperties_AbilityBombingRun : CompProperties_AbilityLinearRunBase
    {
        public int planeCount = 1;
        public float planeSpacing = 5f;

        public CompProperties_AbilityBombingRun()
        {
            compClass = typeof(CompAbilityEffect_BombingRun);
        }
    }
}
