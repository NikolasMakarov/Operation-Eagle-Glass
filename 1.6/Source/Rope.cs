using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;

namespace OperationEagleGlass
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    public class Rope : IExposable
    {
        private enum RopeState { Extending, Extended, Retracting }
        
        private const float EXTEND_SPEED = 0.1f;
        private const float DESCENT_TIME = 1f;
        
        private static Material cachedRopeMaterial;
        
        private RopeState state;
        private float length;
        private float targetLength;
        private Pawn currentPawn;
        private float xOffset;
        private float descentProgress;
        
        public bool IsReady => state == RopeState.Extended && currentPawn == null;
        public bool IsRetracted => state == RopeState.Retracting && length <= 0f;
        public bool IsComplete => IsRetracted;
        public bool HasPawn => currentPawn != null;
        public float XOffset => xOffset;
        public float Length => length;
        public Pawn CurrentPawn => currentPawn;
        
        public Rope(float xOffset)
        {
            this.xOffset = xOffset;
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
            InitializeMaterial();
        }

        private static void InitializeMaterial()
        {
            if (cachedRopeMaterial == null)
            {
                cachedRopeMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.1f, 0.05f));
            }
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
            length += EXTEND_SPEED;
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
                descentProgress += 1f / (DESCENT_TIME * 60f);
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
            length -= EXTEND_SPEED;
            if (length <= 0f)
            {
                length = 0f;
            }
        }
        
        private void DeployPawn(IntVec3 groundPos, Map map)
        {
            IntVec3 spawnPos = groundPos;
            spawnPos.x += Mathf.RoundToInt(xOffset);
            GenSpawn.Spawn(currentPawn, spawnPos, map);
            FleckMaker.ThrowDustPuffThick(spawnPos.ToVector3Shifted(), map, 2f, Color.gray);
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
            ropeBottom.x += xOffset;
            ropeBottom.z -= length;
            
            DrawRope(heliPos, ropeBottom);
            
            if (currentPawn != null)
            {
                DrawPawnOnRope(heliPos);
            }
        }
        
        private void DrawRope(Vector3 top, Vector3 bottom)
        {
            if (cachedRopeMaterial == null)
            {
                InitializeMaterial();
            }
            
            float ropeLength = top.z - bottom.z;
            Vector3 center = (top + bottom) / 2f;
            center.y = AltitudeLayer.Skyfaller.AltitudeFor() - 0.1f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.identity,
                new Vector3(0.2f, 1f, ropeLength)
            );
            Graphics.DrawMesh(MeshPool.plane10, matrix, cachedRopeMaterial, 0);
        }

        private void DrawPawnOnRope(Vector3 heliPos)
        {
            if (currentPawn == null || state == RopeState.Extending)
            {
                return;
            }

            Vector3 top = heliPos;
            top.x += xOffset;

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
            Scribe_Values.Look(ref xOffset, "xOffset", 0f);
            Scribe_Values.Look(ref descentProgress, "descentProgress", 0f);
        }
    }
}
