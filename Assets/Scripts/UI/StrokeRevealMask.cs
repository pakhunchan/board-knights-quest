using UnityEngine;
using UnityEngine.UI;

namespace BoardOfEducation.UI
{
    /// <summary>
    /// Procedural UI Graphic that draws a thick bezier quad-strip tracing the
    /// stroke order of a "3". Used exclusively as a Mask shape — not rendered
    /// visibly — so its child TMP text is revealed progressively as Progress
    /// goes from 0 → 1.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class StrokeRevealMask : Graphic
    {
        [Range(0f, 1f)]
        public float Progress;

        [Tooltip("Smooth eases the stroke speed (slow start/end, fast middle). Unsmoothed is constant speed.")]
        public bool Smooth = true;

        [SerializeField] private float strokeWidth = 90f;
        [SerializeField] private int samplesPerSegment = 20;

        public override Texture mainTexture => Texture2D.whiteTexture;

        // Three cubic bezier segments tracing a "3" stroke:
        //   Segment 1: top-left → right across the top bar
        //   Segment 2: top-right → curves down-right to the middle waist
        //   Segment 3: middle → curves down-left to the bottom
        //
        // Coordinates are in normalized rect space (0,0)=bottom-left, (1,1)=top-right.
        // They get mapped to the RectTransform's pixel rect in OnPopulateMesh.

        private static readonly Vector2[] seg1 = {
            new Vector2(0.15f, 0.88f),  // P0: top-left start
            new Vector2(0.45f, 0.95f),  // P1: control — arches slightly up
            new Vector2(0.75f, 0.95f),  // P2: control — continues right
            new Vector2(0.80f, 0.78f),  // P3: end — starts curving down
        };

        private static readonly Vector2[] seg2 = {
            new Vector2(0.80f, 0.78f),  // P0: continues from seg1
            new Vector2(0.85f, 0.62f),  // P1: curves inward
            new Vector2(0.55f, 0.48f),  // P2: pulls to center
            new Vector2(0.50f, 0.50f),  // P3: middle waist of "3"
        };

        private static readonly Vector2[] seg3 = {
            new Vector2(0.50f, 0.50f),  // P0: middle waist
            new Vector2(0.85f, 0.45f),  // P1: pushes out right for belly
            new Vector2(0.85f, 0.15f),  // P2: curves down
            new Vector2(0.25f, 0.10f),  // P3: bottom-left end
        };

        private static readonly Vector2[][] segments = { seg1, seg2, seg3 };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (Progress <= 0f) return;

            Rect r = GetPixelAdjustedRect();
            float totalSamples = segments.Length * samplesPerSegment;
            float easedProgress = Smooth ? Mathf.SmoothStep(0f, 1f, Progress) : Progress;
            float samplesNeeded = easedProgress * totalSamples;

            Vector2 prevPoint = MapToRect(EvalCubic(segments[0], 0f), r);
            int vertIndex = 0;

            for (int seg = 0; seg < segments.Length; seg++)
            {
                int startSample = seg * samplesPerSegment;

                for (int i = 1; i <= samplesPerSegment; i++)
                {
                    float globalSample = startSample + i;
                    if (globalSample > samplesNeeded) break;

                    float t = (float)i / samplesPerSegment;
                    Vector2 point = MapToRect(EvalCubic(segments[seg], t), r);

                    // Direction perpendicular to the stroke
                    Vector2 dir = (point - prevPoint).normalized;
                    Vector2 perp = new Vector2(-dir.y, dir.x) * (strokeWidth * 0.5f);

                    // Two verts on each side of the stroke center
                    vh.AddVert(prevPoint + perp, color, Vector2.zero);
                    vh.AddVert(prevPoint - perp, color, Vector2.zero);
                    vh.AddVert(point + perp, color, Vector2.zero);
                    vh.AddVert(point - perp, color, Vector2.zero);

                    // Two triangles forming a quad
                    vh.AddTriangle(vertIndex, vertIndex + 1, vertIndex + 2);
                    vh.AddTriangle(vertIndex + 1, vertIndex + 3, vertIndex + 2);
                    vertIndex += 4;

                    prevPoint = point;
                }
            }
        }

        private static Vector2 EvalCubic(Vector2[] p, float t)
        {
            float u = 1f - t;
            return u * u * u * p[0]
                 + 3f * u * u * t * p[1]
                 + 3f * u * t * t * p[2]
                 + t * t * t * p[3];
        }

        private static Vector2 MapToRect(Vector2 normalized, Rect r)
        {
            return new Vector2(
                r.x + normalized.x * r.width,
                r.y + normalized.y * r.height
            );
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
            // Continuously rebuild mesh as Progress changes
            SetVerticesDirty();
        }
    }
}
