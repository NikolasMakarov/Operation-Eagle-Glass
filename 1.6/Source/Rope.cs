using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace OperationEagleGlass
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    public class Rope : IExposable
    {
        private enum RopeState { Extending, Extended, Retracting }
        
        private static Dictionary<Color, Material> cachedRopeMaterials = new Dictionary<Color, Material>();
        
        private RopeState state;
        private float length;
        private float targetLength;
        private Pawn currentPawn;
        private Vector2 drawOffset;
        private float descentProgress;
        private float extendSpeed;
        private float descentTime;
        private Color ropeColor;
        private float ropeWidth;
        public bool IsReady => state == RopeState.Extended && currentPawn == null;
        public bool IsRetracted => state == RopeState.Retracting && length <= 0f;
        public bool IsComplete => IsRetracted;
        public bool HasPawn => currentPawn != null;
        public Pawn CurrentPawn => currentPawn;
        public Rope(Vector2 drawOffset, float extendSpeed, float descentTime, Color ropeColor, float ropeWidth)
        {
            this.drawOffset = drawOffset;
            this.extendSpeed = extendSpeed;
            this.descentTime = descentTime;
            this.ropeColor = ropeColor;
            this.ropeWidth = ropeWidth;
            state = RopeState.Extending;
            length = 0f;
            currentPawn = null;
            descentProgress = 0f;
        }
        public Rope()
        {
            
        }
        
        static Rope()
        {
        }

        private static Material GetRopeMaterial(Color color)
        {
            if (!cachedRopeMaterials.TryGetValue(color, out var material))
            {
                material = SolidColorMaterials.SimpleSolidColorMaterial(color);
                cachedRopeMaterials[color] = material;
            }
            return material;
        }
        
        public void Tick(Vector3 heliPos, IntVec3 groundPos, Map map)
        {
            targetLength = heliPos.z - groundPos.z;
            
            switch (state)
            {
                case RopeState.Extending:
                    TickExtending();
                    break;
                case RopeState.Extended:
                    TickExtended(heliPos, groundPos, map);
                    break;
                case RopeState.Retracting:
                    TickRetracting();
                    break;
            }
        }
        
        private void TickExtending()
        {
            length += extendSpeed;
            if (length >= targetLength)
            {
                length = targetLength;
                state = RopeState.Extended;
                descentProgress = 0f;
            }
        }
        
        private void TickExtended(Vector3 heliPos, IntVec3 groundPos, Map map)
        {
            if (currentPawn != null)
            {
                descentProgress += 1f / (descentTime * 60f);
                if (descentProgress >= 1f)
                {
                    descentProgress = 1f;
                    DeployPawn(groundPos, map);
                    currentPawn = null;
                }
            }
        }
        
        private void TickRetracting()
        {
            length -= extendSpeed;
            if (length <= 0f)
            {
                length = 0f;
            }
        }
        
        private void DeployPawn(IntVec3 groundPos, Map map)
        {
            IntVec3 spawnPos = groundPos;
            spawnPos.x += Mathf.RoundToInt(drawOffset.x);
            spawnPos.z += Mathf.RoundToInt(drawOffset.y);
            GenSpawn.Spawn(currentPawn, spawnPos, map);
        }
        
        public void AssignPawn(Pawn pawn)
        {
            currentPawn = pawn;
            descentProgress = 0f;
        }

        public void StartRetracting()
        {
            if (state == RopeState.Extended)
            {
                state = RopeState.Retracting;
            }
        }
        
        public void Draw(Vector3 heliPos)
        {
            if (length <= 0f) return;
            
            Vector3 ropeBottom = heliPos;
            ropeBottom.x += drawOffset.x;
            ropeBottom.z -= length;
            
            DrawRope(heliPos, ropeBottom);
            
            if (currentPawn != null)
            {
                DrawPawnOnRope(heliPos);
            }
        }
        
        private void DrawRope(Vector3 top, Vector3 bottom)
        {
            float ropeLength = top.z - bottom.z;
            Vector3 center = (top + bottom) / 2f;
            center.y = AltitudeLayer.Skyfaller.AltitudeFor() - 0.1f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.identity,
                new Vector3(ropeWidth, 1f, ropeLength)
            );
            Material ropeMaterial = GetRopeMaterial(ropeColor);
            Graphics.DrawMesh(MeshPool.plane10, matrix, ropeMaterial, 0);
        }

        private void DrawPawnOnRope(Vector3 heliPos)
        {
            if (currentPawn == null || state == RopeState.Extending)
            {
                return;
            }

            Vector3 top = heliPos;
            top.x += drawOffset.x;

            Vector3 bottom = heliPos;
            bottom.z -= length;

            Vector3 center = (top + bottom) / 2f;

            Vector3 pawnPos = new Vector3(center.x, AltitudeLayer.Pawn.AltitudeFor(), heliPos.z - length * descentProgress);

            Rot4 rotation = Rot4.South;
            currentPawn.Drawer.renderer.DynamicDrawPhaseAt(DrawPhase.Draw, pawnPos, rotation);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref state, "state", RopeState.Extending);
            Scribe_Values.Look(ref length, "length", 0f);
            Scribe_Values.Look(ref targetLength, "targetLength", 0f);
            Scribe_References.Look(ref currentPawn, "currentPawn");
            Scribe_Values.Look(ref drawOffset, "drawOffset", Vector2.zero);
            Scribe_Values.Look(ref descentProgress, "descentProgress", 0f);
            Scribe_Values.Look(ref extendSpeed, "extendSpeed", 0.1f);
            Scribe_Values.Look(ref descentTime, "descentTime", 1f);
            Scribe_Values.Look(ref ropeColor, "ropeColor", new Color(0.15f, 0.1f, 0.05f));
            Scribe_Values.Look(ref ropeWidth, "ropeWidth", 0.2f);
        }
    }
}
