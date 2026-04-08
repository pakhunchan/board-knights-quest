using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BoardOfEducation.Input;
using BoardOfEducation.Navigation;
using BoardOfEducation.UI;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Drives the Equivalent Fractions demo: builds two pie-chart circles at runtime,
    /// animates them through 8 steps showing that 1/2 = 3/6 with synchronized subtitles.
    /// </summary>
    public class FractionsDemo2Manager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;

        // Runtime references — created in BuildVisuals
        private FractionCircle leftCircle;
        private FractionCircle rightCircle;
        private CanvasGroup leftCircleGroup;
        private CanvasGroup rightCircleGroup;
        private CanvasGroup leftLabelGroup;
        private CanvasGroup rightLabelGroup;

        // Step data
        private class EquationStep
        {
            public string subtitle;
            public float subtitleDuration;

            public EquationStep(string text)
            {
                subtitle = text;
                int wordCount = text.Split(' ').Length;
                subtitleDuration = Mathf.Max(2f, wordCount * 0.3f);
            }
        }

        private readonly EquationStep[] steps = new EquationStep[]
        {
            new EquationStep("This is a circle."),                                     // 0
            new EquationStep("If we draw a line down the middle and"),                 // 1
            new EquationStep("shade the left side"),                                   // 2
            new EquationStep("we get one-half"),                                       // 3
            new EquationStep("This is a circle."),                                     // 4
            new EquationStep("If we draw lines to split the circle into 6 pieces"),    // 5
            new EquationStep("and shade the left side"),                               // 6
            new EquationStep("we get three-sixths"),                                   // 7
        };

        // Layout constants
        private const float CircleSize = 350f;
        private const float CircleGap = 80f;
        private const float LabelFontSize = 64f;
        private const float LabelOffsetY = -40f;

        // Piece/finger dwell tracking for Play button
        private float dwellOnPlay = -1f;
        private const float DWELL_TIME = 1.0f;
        private Image playButtonImage;
        private Color playButtonBaseColor;

        private static readonly Color ShadeBlue = new Color(0.4f, 0.7f, 1f, 0.7f);
        private static readonly Color CircleStroke = new Color(0.9f, 0.9f, 0.9f, 1f);

        private void Start()
        {
            subtitleText.text = "";
            subtitleText.alpha = 0f;
            playButton.onClick.AddListener(OnPlayPressed);

            playButtonImage = playButtonGo.GetComponent<Image>();
            if (playButtonImage != null)
                playButtonBaseColor = playButtonImage.color;
        }

        private void Update()
        {
            if (!playButtonGo.activeSelf) return;
            if (PieceManager.Instance == null) return;

            Vector2 screenPos = Vector2.zero;
            bool hasContact = false;
            foreach (var kvp in PieceManager.Instance.ActivePieces)
            {
                screenPos = kvp.Value.screenPosition;
                hasContact = true;
                break;
            }

            if (!hasContact)
            {
                ResetDwell();
                return;
            }

            var playRect = playButtonGo.GetComponent<RectTransform>();
            if (playRect != null && NavigationHelper.IsOverRect(playRect, screenPos, 30f))
            {
                if (dwellOnPlay < 0f) dwellOnPlay = 0f;
                dwellOnPlay += Time.deltaTime;

                if (playButtonImage != null)
                    playButtonImage.color = Color.Lerp(playButtonImage.color, Color.white, Time.deltaTime * 3f);

                if (dwellOnPlay >= DWELL_TIME)
                {
                    dwellOnPlay = -1f;
                    OnPlayPressed();
                }
            }
            else
            {
                ResetDwell();
            }
        }

        private void ResetDwell()
        {
            dwellOnPlay = -1f;
            if (playButtonImage != null)
                playButtonImage.color = Color.Lerp(playButtonImage.color, playButtonBaseColor, Time.deltaTime * 5f);
        }

        private void OnPlayPressed()
        {
            playButtonGo.SetActive(false);
            StartCoroutine(CoPlaySequence());
        }

        // ── Visual Building ──────────────────────────────────────

        private void BuildVisuals()
        {
            // Clear any previous children
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);

            float totalWidth = CircleSize * 2 + CircleGap;
            float leftX = -totalWidth / 2f + CircleSize / 2f;
            float rightX = totalWidth / 2f - CircleSize / 2f;

            // ── Left Circle (1/2) ──
            leftCircle = CreateCircle("LeftCircle", 2,
                new bool[] { true, false }, // shade slice 0 (left half: 90°→270°)
                leftX);
            leftCircleGroup = leftCircle.GetComponent<CanvasGroup>();

            // ── Right Circle (3/6) ──
            rightCircle = CreateCircle("RightCircle", 6,
                new bool[] { false, true, true, true, false, false }, // shade slices 1,2,3 (left side)
                rightX);
            rightCircleGroup = rightCircle.GetComponent<CanvasGroup>();

            // ── Labels ──
            leftLabelGroup = CreateLabel("LeftLabel", "1/2", leftX);
            rightLabelGroup = CreateLabel("RightLabel", "3/6", rightX);
        }

        private FractionCircle CreateCircle(string name, int divisions, bool[] shaded, float xPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(contentArea, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CircleSize, CircleSize);
            rect.anchoredPosition = new Vector2(xPos, 20f);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var circle = go.AddComponent<FractionCircle>();
            circle.Divisions = divisions;
            circle.ShadedSlices = shaded;
            circle.ShadeColor = ShadeBlue;
            circle.StrokeColor = CircleStroke;
            circle.StrokeWidth = 6f;
            circle.OutlineProgress = 0f;
            circle.LineProgress = 0f;
            circle.ShadeProgress = 0f;
            circle.raycastTarget = false;

            return circle;
        }

        private CanvasGroup CreateLabel(string name, string text, float xPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(contentArea, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CircleSize, 80f);
            rect.anchoredPosition = new Vector2(xPos, -CircleSize / 2f + LabelOffsetY);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = LabelFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return cg;
        }

        // ── Sequence ─────────────────────────────────────────────

        private IEnumerator CoPlaySequence()
        {
            BuildVisuals();
            yield return new WaitForSeconds(0.3f);

            // Step 0: Show left circle outline
            yield return RunStep(0, onComplete => CoAnimateShowCircle(leftCircle, leftCircleGroup, onComplete));

            // Step 1: Draw vertical line on left circle
            yield return RunStep(1, onComplete => CoAnimateLines(leftCircle, onComplete));

            // Step 2: Shade left half
            yield return RunStep(2, onComplete => CoAnimateShading(leftCircle, onComplete));

            // Step 3: Show "1/2" label
            yield return RunStep(3, onComplete => CoAnimateLabel(leftLabelGroup, onComplete));

            // Step 4: Show right circle outline
            yield return RunStep(4, onComplete => CoAnimateShowCircle(rightCircle, rightCircleGroup, onComplete));

            // Step 5: Draw 3 lines on right circle
            yield return RunStep(5, onComplete => CoAnimateLines(rightCircle, onComplete));

            // Step 6: Shade left 3 slices
            yield return RunStep(6, onComplete => CoAnimateShading(rightCircle, onComplete));

            // Step 7: Show "3/6" label
            yield return RunStep(7, onComplete => CoAnimateLabel(rightLabelGroup, onComplete));

            // Done — show replay
            subtitleText.alpha = 0f;
            subtitleText.text = "";
            playButtonGo.SetActive(true);
        }

        private IEnumerator RunStep(int stepIndex, System.Func<System.Action, IEnumerator> animFactory)
        {
            bool animDone = false, subDone = false;

            if (animFactory != null)
                StartCoroutine(animFactory(() => animDone = true));
            else
                animDone = true;

            StartCoroutine(CoShowSubtitle(steps[stepIndex].subtitle, steps[stepIndex].subtitleDuration, () => subDone = true));
            yield return new WaitUntil(() => animDone && subDone);
            yield return new WaitForSeconds(0.5f);
        }

        // ── Step Animations ──────────────────────────────────────

        private IEnumerator CoAnimateShowCircle(FractionCircle circle, CanvasGroup cg, System.Action onComplete)
        {
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Clamp01(t * 3f); // fade in quickly in first third
                circle.OutlineProgress = t;
                yield return null;
            }

            cg.alpha = 1f;
            circle.OutlineProgress = 1f;
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateLines(FractionCircle circle, System.Action onComplete)
        {
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                circle.LineProgress = t;
                yield return null;
            }

            circle.LineProgress = 1f;
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateShading(FractionCircle circle, System.Action onComplete)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                circle.ShadeProgress = t;
                yield return null;
            }

            circle.ShadeProgress = 1f;
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateLabel(CanvasGroup labelGroup, System.Action onComplete)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Transform t = labelGroup.transform;
            t.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                labelGroup.alpha = p;
                float scale = EaseOutBack(p);
                t.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            labelGroup.alpha = 1f;
            t.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        // ── Subtitle ─────────────────────────────────────────────

        private IEnumerator CoShowSubtitle(string text, float duration, System.Action onComplete)
        {
            string[] words = text.Split(' ');
            float perWord = duration / words.Length;

            subtitleText.text = text;

            // Fade in
            float fadeTime = 0.25f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 1f;

            // Karaoke word highlighting
            for (int i = 0; i < words.Length; i++)
            {
                var sb = new System.Text.StringBuilder();
                for (int w = 0; w < words.Length; w++)
                {
                    if (w > 0) sb.Append(' ');
                    if (w == i)
                        sb.Append("<color=#FF3333>").Append(words[w]).Append("</color>");
                    else
                        sb.Append(words[w]);
                }
                subtitleText.text = sb.ToString();
                yield return new WaitForSeconds(perWord);
            }

            // Restore plain text briefly
            subtitleText.text = text;
            yield return new WaitForSeconds(0.15f);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 0f;

            onComplete?.Invoke();
        }

        // ── Helpers ──────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
