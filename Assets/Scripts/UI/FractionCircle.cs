using UnityEngine;
using UnityEngine.UI;

namespace BoardOfEducation.UI
{
    /// <summary>
    /// Procedural UI Graphic that draws a pie-chart circle: outline, division lines,
    /// and shaded slices. All three layers are driven by 0→1 progress floats so the
    /// manager can animate them with coroutines.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class FractionCircle : Graphic
    {
        [Range(0f, 1f)] public float OutlineProgress;
        [Range(0f, 1f)] public float LineProgress;
        [Range(0f, 1f)] public float ShadeProgress;

        public int Divisions = 2;
        public bool[] ShadedSlices;
        public Color ShadeColor = new Color(0.4f, 0.7f, 1f, 0.6f);
        public Color StrokeColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        public float StrokeWidth = 8f;

        private const int CircleSegments = 64;

        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            float radius = Mathf.Min(r.width, r.height) * 0.45f;
            Vector2 center = r.center;

            // Layer 1: filled pie slices (behind everything)
            if (ShadeProgress > 0f && ShadedSlices != null)
            {
                float sliceAngle = 360f / Divisions;
                float startAngle = GetFirstSliceAngle();

                Color shadeCol = ShadeColor;
                shadeCol.a *= ShadeProgress;

                for (int i = 0; i < Divisions; i++)
                {
                    if (i >= ShadedSlices.Length || !ShadedSlices[i]) continue;

                    float fromAngle = startAngle + i * sliceAngle;
                    float toAngle = fromAngle + sliceAngle;
                    AddPieSlice(vh, center, radius - StrokeWidth * 0.5f, fromAngle, toAngle, shadeCol);
                }
            }

            // Layer 2: circle outline as thick quad-strip
            if (OutlineProgress > 0f)
            {
                AddCircleOutline(vh, center, radius, OutlineProgress);
            }

            // Layer 3: division lines from center outward
            if (LineProgress > 0f)
            {
                DrawDivisionLines(vh, center, radius);
            }
        }

        private float GetFirstSliceAngle()
        {
            // Divisions=2: slices start at 90° (top) → slice 0 is left half (90°→270°)
            // Divisions=6: slices start at 30° → boundaries at 30°,90°,150°,210°,270°,330°
            if (Divisions == 6) return 30f;
            return 90f;
        }

        private void AddPieSlice(VertexHelper vh, Vector2 center, float radius,
            float fromDeg, float toDeg, Color col)
        {
            int segments = Mathf.Max(8, Mathf.CeilToInt(CircleSegments * Mathf.Abs(toDeg - fromDeg) / 360f));
            int baseIdx = vh.currentVertCount;

            vh.AddVert(center, col, Vector2.zero);

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(fromDeg, toDeg, (float)i / segments) * Mathf.Deg2Rad;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(point, col, Vector2.zero);

                if (i > 0)
                    vh.AddTriangle(baseIdx, baseIdx + i, baseIdx + i + 1);
            }
        }

        private void AddCircleOutline(VertexHelper vh, Vector2 center, float radius, float progress)
        {
            int segmentsToDraw = Mathf.CeilToInt(CircleSegments * progress);
            if (segmentsToDraw < 1) return;

            float halfWidth = StrokeWidth * 0.5f;
            // Start from top (90°), draw clockwise
            float startRad = 90f * Mathf.Deg2Rad;

            for (int i = 0; i < segmentsToDraw; i++)
            {
                float t0 = (float)i / CircleSegments;
                float t1 = (float)(i + 1) / CircleSegments;

                // Clamp last segment to exact progress
                if (i == segmentsToDraw - 1 && progress < 1f)
                    t1 = progress;

                // Clockwise: subtract angle
                float a0 = startRad - t0 * Mathf.PI * 2f;
                float a1 = startRad - t1 * Mathf.PI * 2f;

                Vector2 p0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
                Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                Vector2 n0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector2 n1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));

                int baseIdx = vh.currentVertCount;
                vh.AddVert(p0 + n0 * halfWidth, StrokeColor, Vector2.zero);
                vh.AddVert(p0 - n0 * halfWidth, StrokeColor, Vector2.zero);
                vh.AddVert(p1 + n1 * halfWidth, StrokeColor, Vector2.zero);
                vh.AddVert(p1 - n1 * halfWidth, StrokeColor, Vector2.zero);

                vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                vh.AddTriangle(baseIdx + 1, baseIdx + 3, baseIdx + 2);
            }
        }

        private void DrawDivisionLines(VertexHelper vh, Vector2 center, float radius)
        {
            // Each diameter line goes from one edge through center to the opposite edge
            // Divisions=2: 1 vertical line at 90°–270°
            // Divisions=6: 3 lines at 90°–270°, 150°–330°, 30°–210° (via angles 90°, 150°, 30°)
            float[] lineAngles;
            if (Divisions == 2)
                lineAngles = new[] { 90f };
            else if (Divisions == 6)
                lineAngles = new[] { 90f, 150f, 30f };
            else
                return;

            float halfWidth = StrokeWidth * 0.5f;
            float lineRadius = radius - StrokeWidth * 0.5f;

            for (int i = 0; i < lineAngles.Length; i++)
            {
                float angleRad = lineAngles[i] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                Vector2 top = center + dir * lineRadius;
                Vector2 bottom = center - dir * lineRadius;

                if (Divisions == 2)
                {
                    // Animate: line draws from top to bottom
                    Vector2 animBottom = Vector2.Lerp(top, bottom, LineProgress);
                    AddLine(vh, top, animBottom, halfWidth);
                }
                else
                {
                    // Animate: lines grow from center outward in both directions
                    Vector2 animTop = Vector2.Lerp(center, top, LineProgress);
                    Vector2 animBottom = Vector2.Lerp(center, bottom, LineProgress);
                    AddLine(vh, animTop, animBottom, halfWidth);
                }
            }
        }

        private void AddLine(VertexHelper vh, Vector2 from, Vector2 to, float halfWidth)
        {
            Vector2 dir = (to - from);
            if (dir.sqrMagnitude < 0.001f) return;
            dir.Normalize();
            Vector2 perp = new Vector2(-dir.y, dir.x) * halfWidth;

            int baseIdx = vh.currentVertCount;
            vh.AddVert(from + perp, StrokeColor, Vector2.zero);
            vh.AddVert(from - perp, StrokeColor, Vector2.zero);
            vh.AddVert(to + perp, StrokeColor, Vector2.zero);
            vh.AddVert(to - perp, StrokeColor, Vector2.zero);

            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx + 1, baseIdx + 3, baseIdx + 2);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif

        private void Update()
        {
            SetVerticesDirty();
        }
    }
}
