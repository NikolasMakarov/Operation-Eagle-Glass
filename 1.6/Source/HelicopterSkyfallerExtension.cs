using Verse;
using UnityEngine;

namespace OperationEagleGlass
{
    public class HelicopterSkyfallerExtension : DefModExtension
    {
        public ThingDef leavingSkyfaller;
        public GraphicData gunGraphic;
        public Vector2 gunDrawOffset = new Vector2(0f, -4f);
        public float ropeExtendSpeed = 0.1f;
        public float ropeDescentTime = 1f;
        public Color ropeColor = new Color(0.15f, 0.1f, 0.05f);
        public float ropeWidth = 0.2f;
        public Vector2 leftRopeDrawOffset = new Vector2(-2f, 0f);
        public Vector2 rightRopeDrawOffset = new Vector2(2f, 0f);
    }
}