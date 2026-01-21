using RimWorld;
using Verse;

namespace OperationEagleGlass
{
    [HotSwappable]
    public class Projectile_NuclearBomb : Projectile_Explosive
    {
        public override void Explode()
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            float explosionRadius = def.projectile.explosionRadius;

            base.Explode();

            if (map != null)
            {
                if (ModsConfig.BiotechActive)
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, explosionRadius, useCenter: true))
                    {
                        if (cell.InBounds(map) && cell.CanPollute(map))
                        {
                            cell.Pollute(map);
                        }
                    }
                }
                GameCondition_ToxicFallout gameCondition_ToxicFallout = map.gameConditionManager.GetActiveCondition<GameCondition_ToxicFallout>();
                if (gameCondition_ToxicFallout == null)
                {
                    gameCondition_ToxicFallout = (GameCondition_ToxicFallout)GameConditionMaker.MakeCondition(GameConditionDefOf.ToxicFallout, 60000);
                    map.gameConditionManager.RegisterCondition(gameCondition_ToxicFallout);
                }
                else
                {
                    gameCondition_ToxicFallout.TicksLeft = 60000;
                }
            }
        }
    }
}
