using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Visual component for a single Nullify circle.
    /// Manages its CircleData, visual state, and animations.
    /// </summary>
    public class NullifyCircle : MonoBehaviour
    {
        public enum VisualState { Idle, Highlighted, Grabbed, Preview }

        public CircleData Data { get; private set; }
        public VisualState State { get; private set; } = VisualState.Idle;

        // Child references (set by NullifyBoard when creating)
        [HideInInspector] public Image background;
        [HideInInspector] public TextMeshProUGUI valueText;
        [HideInInspector] public Image glowRing;

        private Vector3 baseScale = Vector3.one;
        private Color baseColor;

        // Colors
        private static readonly Color PositiveColor = HexColor("#2ecc71"); // teal
        private static readonly Color NegativeColor = HexColor("#e74c3c"); // coral
        private static readonly Color OperationColor = HexColor("#f39c12"); // amber

        public void Initialize(CircleData data)
        {
            Data = data;

            // Set color based on type
            if (data.Type == CircleType.Number)
                baseColor = data.Value >= 0 ? PositiveColor : NegativeColor;
            else
                baseColor = OperationColor;

            if (background != null)
                background.color = baseColor;

            if (valueText != null)
                valueText.text = data.DisplayText;

            if (glowRing != null)
            {
                var c = baseColor;
                c.a = 0f;
                glowRing.color = c;
            }
        }

        /// <summary>Update the circle's data after a combine (when it receives a new value).</summary>
        public void UpdateData(float newValue, string newDisplay)
        {
            Data = new CircleData(newValue, Data.Type, newDisplay);

            if (Data.Type == CircleType.Number)
                baseColor = newValue >= 0 ? PositiveColor : NegativeColor;

            if (background != null)
                background.color = baseColor;

            if (valueText != null)
                valueText.text = newDisplay;

            if (glowRing != null)
            {
                var c = baseColor;
                c.a = glowRing.color.a; // preserve current glow
                glowRing.color = c;
            }
        }

        // ── Visual State ─────────────────────────────────────

        public void SetVisualState(VisualState state, string previewText = null)
        {
            State = state;

            switch (state)
            {
                case VisualState.Idle:
                    SetGlowAlpha(0f);
                    transform.localScale = baseScale;
                    if (valueText != null) valueText.text = Data.DisplayText;
                    break;

                case VisualState.Highlighted:
                    SetGlowAlpha(0.4f);
                    transform.localScale = baseScale;
                    if (valueText != null) valueText.text = Data.DisplayText;
                    break;

                case VisualState.Grabbed:
                    SetGlowAlpha(0.7f);
                    transform.localScale = baseScale * 1.1f;
                    break;

                case VisualState.Preview:
                    SetGlowAlpha(0.4f);
                    transform.localScale = baseScale;
                    if (valueText != null && previewText != null)
                        valueText.text = previewText;
                    break;
            }
        }

        private void SetGlowAlpha(float alpha)
        {
            if (glowRing == null) return;
            var c = baseColor;
            c.a = alpha;
            glowRing.color = c;
        }

        // ── Animations ───────────────────────────────────────

        public void AnimateSpawn()
        {
            StartCoroutine(CoAnimateSpawn());
        }

        private IEnumerator CoAnimateSpawn()
        {
            float duration = 0.35f;
            float elapsed = 0f;
            transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = EaseOutBack(t);
                transform.localScale = baseScale * scale;
                yield return null;
            }
            transform.localScale = baseScale;
        }

        public void AnimateCombine(System.Action onComplete = null)
        {
            StartCoroutine(CoAnimateCombine(onComplete));
        }

        private IEnumerator CoAnimateCombine(System.Action onComplete)
        {
            // Flash white then pulse scale
            if (background != null)
            {
                background.color = Color.white;
                yield return new WaitForSeconds(0.08f);
                background.color = baseColor;
            }

            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = baseScale * s;
                yield return null;
            }
            transform.localScale = baseScale;
            onComplete?.Invoke();
        }

        public void AnimateNullify(System.Action onComplete = null)
        {
            StartCoroutine(CoAnimateNullify(onComplete));
        }

        private IEnumerator CoAnimateNullify(System.Action onComplete)
        {
            // Flash white
            if (background != null) background.color = Color.white;
            if (valueText != null) valueText.text = "0";
            yield return new WaitForSeconds(0.15f);

            // Shrink to zero
            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = baseScale * (1f - t);

                // Fade
                if (background != null)
                {
                    var c = background.color;
                    c.a = 1f - t;
                    background.color = c;
                }
                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        public void AnimateReject()
        {
            StartCoroutine(CoAnimateReject());
        }

        private IEnumerator CoAnimateReject()
        {
            // Horizontal shake
            var rect = GetComponent<RectTransform>();
            if (rect == null) yield break;

            Vector2 origin = rect.anchoredPosition;
            float duration = 0.3f;
            float elapsed = 0f;
            float amplitude = 12f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float offset = amplitude * Mathf.Sin(t * Mathf.PI * 4f) * (1f - t);
                rect.anchoredPosition = origin + new Vector2(offset, 0);
                yield return null;
            }
            rect.anchoredPosition = origin;
        }

        // ── Helpers ──────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
