using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OperationEagleGlass
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    public class AbilityResourceGizmo : Command
    {
        private Pawn pawn;
        private const float BaseWidth = 200f;
        private const float BarMargin = 4f;

        private static readonly Texture2D BarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));
        private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
        private static readonly Texture2D DragBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

        private static Dictionary<Pawn, Dictionary<ThingDef, bool>> draggingBars = new Dictionary<Pawn, Dictionary<ThingDef, bool>>();
        private static Dictionary<Pawn, Dictionary<ThingDef, float>> targetValues = new Dictionary<Pawn, Dictionary<ThingDef, float>>();

        private static List<float> bandPercentages;

        private class ResourceTotals
        {
            public ThingDef Def { get; set; }
            public int Current { get; set; }
            public int Max { get; set; }
            public int TargetFill { get; set; }
        }

        public AbilityResourceGizmo(Pawn pawn)
        {
            this.pawn = pawn;
            if (bandPercentages == null)
            {
                bandPercentages = new List<float>();
                int num = 12;
                for (int i = 0; i <= num; i++)
                {
                    float item = 1f / (float)num * (float)i;
                    bandPercentages.Add(item);
                }
            }

            if (!draggingBars.ContainsKey(pawn))
            {
                draggingBars[pawn] = new Dictionary<ThingDef, bool>();
            }
            if (!targetValues.ContainsKey(pawn))
            {
                targetValues[pawn] = new Dictionary<ThingDef, float>();
            }
        }

        private List<Ability_Resource> GetAllResourceAbilities()
        {
            return pawn.abilities.AllAbilitiesForReading.OfType<Ability_Resource>().ToList();
        }

        private List<ResourceTotals> GetResourceTotals(List<Ability_Resource> abilities)
        {
            if (abilities.Count == 0) return new List<ResourceTotals>();

            var firstAbility = abilities.First();
            var resourceTypes = abilities
                .SelectMany(a => a.MaxResources)
                .Select(r => r.thingDef)
                .Distinct();

            return resourceTypes.Select(def => new ResourceTotals
            {
                Def = def,
                Current = firstAbility.ResourceCount(def),
                Max = abilities.Sum(a => a.GetMaxResourceCountForType(def)),
                TargetFill = abilities.Sum(a => a.GetMaxToFillForType(def))
            }).ToList();
        }

        public override float GetWidth(float maxWidth)
        {
            var allResourceAbilities = GetAllResourceAbilities();
            var resourceTotals = GetResourceTotals(allResourceAbilities);
            if (resourceTotals != null && resourceTotals.Count > 0)
            {
                return BaseWidth + (float)(resourceTotals.Count - 1) * BarMargin;
            }
            return BaseWidth;
        }

        public override bool GroupsWith(Gizmo other)
        {
            return other is AbilityResourceGizmo otherGizmo && otherGizmo.pawn == pawn;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(10f);
            Widgets.DrawWindowBackground(rect);
            Text.Font = GameFont.Small;
            TaggedString labelCap = "OEG_ResourceManagement".Translate();
            float height = Text.CalcHeight(labelCap, rect2.width);
            Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect3, labelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            var allResourceAbilities = GetAllResourceAbilities();
            var resourceTotals = GetResourceTotals(allResourceAbilities);

            if (resourceTotals != null && resourceTotals.Count > 0)
            {
                float totalMargin = (float)(resourceTotals.Count - 1) * BarMargin;
                float barWidth = (rect2.width - totalMargin) / (float)resourceTotals.Count;
                float xOffset = rect2.x;

                foreach (var totals in resourceTotals)
                {
                    float percentage = totals.Max > 0 ? (float)totals.Current / (float)totals.Max : 0f;
                    if (!targetValues[pawn].ContainsKey(totals.Def))
                    {
                        targetValues[pawn][totals.Def] = totals.Max > 0 ? (float)totals.TargetFill / (float)totals.Max : 0f;
                    }

                    if (!draggingBars[pawn].ContainsKey(totals.Def))
                    {
                        draggingBars[pawn][totals.Def] = false;
                    }
                    float targetValue = targetValues[pawn][totals.Def];
                    float lastTargetValue = targetValue;
                    bool dragging = draggingBars[pawn][totals.Def];

                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Rect labelRect = new Rect(xOffset, rect3.yMax - 7, barWidth, 20f);
                    Widgets.Label(labelRect, totals.Def.LabelCap);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;

                    Rect rect4 = new Rect(xOffset, labelRect.yMax, barWidth, rect2.height - rect3.height - labelRect.height + 7);
                    Widgets.DraggableBar(rect4, BarTex, BarHighlightTex, EmptyBarTex, DragBarTex, ref dragging, percentage, ref targetValue, bandPercentages, 24);

                    draggingBars[pawn][totals.Def] = dragging;

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect4, totals.Current + " / " + totals.Max);
                    Text.Anchor = TextAnchor.UpperLeft;
                    TooltipHandler.TipRegion(rect4, () => GetResourceBarTip(totals.Def, totals.Current, totals.Max), Gen.HashCombineInt(pawn.GetHashCode(), totals.Def.shortHash));
                    if (Mathf.Abs(lastTargetValue - targetValue) > 0.001f)
                    {
                        targetValues[pawn][totals.Def] = targetValue;

                        int newTarget = Mathf.RoundToInt(targetValue * (float)totals.Max);

                        var validAbilities = allResourceAbilities.Where(a => a.CanAcceptResource(totals.Def)).ToList();
                        if (validAbilities.Count > 0)
                        {
                            int perAbilityTarget = newTarget / validAbilities.Count;
                            foreach (var ab in validAbilities)
                            {
                                ab.SetMaxToFillForType(totals.Def, perAbilityTarget);
                            }
                        }
                    }

                    xOffset += barWidth + BarMargin;
                }
            }

            return new GizmoResult(GizmoState.Clear);
        }

        private string GetResourceBarTip(ThingDef resourceDef, int current, int max)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int targetFill = GetAllResourceAbilities().Sum(a => a.GetMaxToFillForType(resourceDef));
            stringBuilder.AppendLine(string.Concat("OEG_ResourceStorage".Translate() + " " + resourceDef.label + ": ", current.ToString(), " / ", max.ToString(), " (target: ", targetFill.ToString(), ")"));
            stringBuilder.AppendInNewLine("OEG_ClickToAdjustResources".Translate());
            return stringBuilder.ToString();
        }
    }
}
