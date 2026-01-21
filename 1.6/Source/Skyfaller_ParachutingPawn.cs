using RimWorld;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    public class Skyfaller_ParachutingPawn : Skyfaller
    {
        private Pawn containedPawn;
        private ParachutingPawnExtension ext;
        public Pawn ContainedPawn { set => containedPawn = value; }
        private bool IsChuteOpen => this.ticksToImpact < 120;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ext = def.GetModExtension<ParachutingPawnExtension>();
        }

        public override void Tick()
        {
            base.Tick();
            if (!IsChuteOpen && this.IsHashIntervalTick(3))
            {
                FleckMaker.ThrowSmoke(this.DrawPos, this.Map, 1.5f);
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flipRot = false)
        {
            if (IsChuteOpen)
            {
                Graphic.Draw(drawLoc + new Vector3(0, 0, ext.parachuteVerticalOffset), Rot4.North, this);
                Vector3 pawnDrawPos = drawLoc;
                pawnDrawPos.y = AltitudeLayer.Pawn.AltitudeFor();
                containedPawn.Drawer.renderer.RenderPawnAt(pawnDrawPos);
            }
        }

        public override void Impact()
        {
            GenSpawn.Spawn(containedPawn, Position, Map, WipeMode.Vanish);
            base.Impact();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref containedPawn, "containedPawn");
        }
    }
}
