using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using BoardOfEducation.Input;
using BoardOfEducation.Navigation;
using BoardOfEducation.Lessons;
using BoardOfEducation.UI;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Drives the Equivalent Fractions demo: builds two pie-chart circles at runtime,
    /// animates them through steps showing that 1/2 = 3/6 with synchronized subtitles.
    ///
    /// Step text and animation keys are co-located in a single LessonStep array.
    /// Playback is delegated to the shared LessonSequencer (barrier sync).
    /// </summary>
    public class FractionsDemo2Manager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;
        [SerializeField] private LessonSequencer sequencer;

        // Runtime references — created in BuildVisuals
        private FractionCircle leftCircle;
        private FractionCircle rightCircle;
        private CanvasGroup leftCircleGroup;
        private CanvasGroup rightCircleGroup;
        private CanvasGroup leftLabelGroup;
        private CanvasGroup rightLabelGroup;

        // ── Step Definitions (single source of truth) ──────────────

        private readonly LessonStep[] steps = new LessonStep[]
        {
            new LessonStep("This is a circle.",                                     "showCircle:left"),   // 0
            new LessonStep("If we draw a line down the middle and",                 "lines:left"),        // 1
            new LessonStep("shade the left side",                                   "shade:left"),        // 2
            new LessonStep("we get one-half",                                       "label:left"),        // 3
            new LessonStep("This is a circle.",                                     "showCircle:right"),  // 4
            new LessonStep("If we draw lines to split the circle into 6 pieces",    "lines:right"),       // 5
            new LessonStep("and shade the left side",                               "shade:right"),       // 6
            new LessonStep("we get three-sixths",                                   "label:right"),       // 7
            new LessonStep("You can see that these fractions are equal"),                                  // 8
            new LessonStep("But how do we go from one fraction to another fraction?"),                     // 9
            new LessonStep("We will learn that today"),                                                    // 10
            new LessonStep("Let's jump into an example"),                                                  // 11
        };

        // ── Animation Registry ─────────────────────────────────────

        private Dictionary<string, Func<Action, IEnumerator>> animationRegistry;

        private void BuildAnimationRegistry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["showCircle:left"]  = cb => CoAnimateShowCircle(leftCircle, leftCircleGroup, cb),
                ["showCircle:right"] = cb => CoAnimateShowCircle(rightCircle, rightCircleGroup, cb),
                ["lines:left"]       = cb => CoAnimateLines(leftCircle, cb),
                ["lines:right"]      = cb => CoAnimateLines(rightCircle, cb),
                ["shade:left"]       = cb => CoAnimateShading(leftCircle, cb),
                ["shade:right"]      = cb => CoAnimateShading(rightCircle, cb),
                ["label:left"]       = cb => CoAnimateLabel(leftLabelGroup, cb),
                ["label:right"]      = cb => CoAnimateLabel(rightLabelGroup, cb),
            };
        }

        private Func<Action, IEnumerator> ResolveAnimation(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            animationRegistry.TryGetValue(key, out var factory);
            return factory;
        }

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
                new bool[] { true, false }, // shade slice 0 (left half: 90->270)
                leftX);
            leftCircleGroup = leftCircle.GetComponent<CanvasGroup>();

            // ── Right Circle (3/6) ──
            rightCircle = CreateCircle("RightCircle", 6,
                new bool[] { false, true, true, true, false, false }, // shade slices 1,2,3 (left side)
                rightX);
            rightCircleGroup = rightCircle.GetComponent<CanvasGroup>();

            // ── Labels (stacked fractions) ──
            leftLabelGroup = CreateFractionLabel("LeftLabel", "1", "2", leftX);
            rightLabelGroup = CreateFractionLabel("RightLabel", "3", "6", rightX);
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

        private CanvasGroup CreateFractionLabel(string name, string num, string den, float xPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(contentArea, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 140f);
            rect.anchoredPosition = new Vector2(xPos, -CircleSize / 2f + LabelOffsetY);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Numerator (top half)
            var numGo = new GameObject("Num");
            numGo.transform.SetParent(go.transform, false);
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0, 0.5f);
            numRect.anchorMax = Vector2.one;
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = num;
            numTmp.fontSize = LabelFontSize;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;
            numTmp.textWrappingMode = TextWrappingModes.NoWrap;

            // Horizontal bar
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(go.transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.2f, 0.48f);
            barRect.anchorMax = new Vector2(0.8f, 0.52f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            var barImg = barGo.AddComponent<Image>();
            barImg.color = Color.white;
            barImg.raycastTarget = false;

            // Denominator (bottom half)
            var denGo = new GameObject("Den");
            denGo.transform.SetParent(go.transform, false);
            var denRect = denGo.AddComponent<RectTransform>();
            denRect.anchorMin = Vector2.zero;
            denRect.anchorMax = new Vector2(1, 0.5f);
            denRect.offsetMin = Vector2.zero;
            denRect.offsetMax = Vector2.zero;
            var denTmp = denGo.AddComponent<TextMeshProUGUI>();
            denTmp.text = den;
            denTmp.fontSize = LabelFontSize;
            denTmp.alignment = TextAlignmentOptions.Center;
            denTmp.color = Color.white;
            denTmp.textWrappingMode = TextWrappingModes.NoWrap;

            return cg;
        }

        // ── Sequence ─────────────────────────────────────────────

        private IEnumerator CoPlaySequence()
        {
            BuildVisuals();
            BuildAnimationRegistry();
            sequencer.Begin();
            yield return new WaitForSeconds(0.3f);

            // Data-driven: iterate all steps, resolve animations by key
            foreach (var step in steps)
            {
                var anim = ResolveAnimation(step.animationKey);
                yield return sequencer.RunStep(step, anim);
            }

            // Done — show replay
            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ── Step Animations ──────────────────────────────────────

        private IEnumerator CoAnimateShowCircle(FractionCircle circle, CanvasGroup cg, Action onComplete)
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

        private IEnumerator CoAnimateLines(FractionCircle circle, Action onComplete)
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

        private IEnumerator CoAnimateShading(FractionCircle circle, Action onComplete)
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

        private IEnumerator CoAnimateLabel(CanvasGroup labelGroup, Action onComplete)
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

        // ── Helpers ──────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
