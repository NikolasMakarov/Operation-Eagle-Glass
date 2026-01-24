using RimWorld;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    public class Skyfaller_Hovering : Skyfaller
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
                FleckMaker.ThrowDustPuffThick(DrawPos, this.Map, 5f, Color.white);
            }
        }
    }
}
