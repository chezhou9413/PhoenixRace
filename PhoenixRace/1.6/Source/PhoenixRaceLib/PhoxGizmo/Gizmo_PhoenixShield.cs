
using PhoenixRaceLib.PhoxThingComp;
using UnityEngine;
using Verse;

namespace PhoenixRaceLib.PhoxGizmo
{
    [StaticConstructorOnStartup]
    public class Gizmo_PhoenixShield : Gizmo
    {
        public ThingComp_PhoenixShield comp;
        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.9f, 0.4f, 0f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        public override float GetWidth(float maxWidth) => 140f; // 标准 Gizmo 宽度

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);
            Rect innerRect = rect.ContractedBy(6f);
            Rect labelRect = innerRect;
            labelRect.height = innerRect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, "护盾能量");
            Rect barRect = innerRect;
            barRect.yMin = labelRect.yMax;
            float fillPercent = comp.curShield / comp.maxShield;
            Widgets.FillableBar(barRect, fillPercent, FullBarTex, EmptyBarTex, false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{comp.curShield:F0} / {comp.FinalMaxShield:F0}");
            Text.Anchor = TextAnchor.UpperLeft; 
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
