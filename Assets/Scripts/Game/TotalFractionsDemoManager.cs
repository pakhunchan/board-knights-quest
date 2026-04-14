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
    /// Unified fractions demo: plays circle visuals (1/2 = 3/6) followed by
    /// an equation-solving phase (1/2 = ?/6), with a smooth crossfade transition.
    /// Merges FractionsDemo2Manager (circles) and FractionsDemoManager (equation)
    /// into a single continuous lesson.
    /// </summary>
    public class TotalFractionsDemoManager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;
        [SerializeField] private LessonSequencer sequencer;

        // ── Phase 1 (circles) — set by BuildCircleVisuals(), nulled after transition ──
        private FractionCircle leftCircle, rightCircle;
        private CanvasGroup leftCircleGroup, rightCircleGroup;
        private CanvasGroup leftLabelGroup, rightLabelGroup;

        // ── Phase 2 (equation) — set by BuildEquation() during transition ──
        private RectTransform equationRow;
        private RectTransform fracLeft, opEquals, fracRight, opMultiply, fracMiddle;
        private TextMeshProUGUI fracLeftNum, fracLeftDen, fracRightNum, fracRightDen;
        private TextMeshProUGUI fracMiddleNum, fracMiddleDen;
        private CanvasGroup fracMiddleGroup, opMultiplyGroup;
        private CanvasGroup fracLeftTopGroup, fracMiddleTopGroup, fracRightTopGroup;
        private Vector3 savedEquationScale;
        private float savedEquationY, savedOpEqualsY, savedOpMultiplyY;

        // ── Step Definitions (single source of truth) ──────────────

        private readonly LessonStep[] steps = new LessonStep[]
        {
            // Phase 1: Circles (steps 0–11)
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

            // Transition (step 12) — no subtitle, visual crossfade
            new LessonStep(" ",                                                     "transition"),        // 12

            // Phase 2: Equation (steps 13–20)
            new LessonStep("One half equals how many sixths?"),                                                                                               // 13
            new LessonStep("Let's shift this over to the right to make space",                                                             "slideApart"),     // 14
            new LessonStep("We need to multiply one half by something to turn it into something over six.",                                "fadeInMultiply"), // 15
            new LessonStep("Focus first on the bottom numbers, the denominators.",                                                         "zoomInDenom"),   // 16
            new LessonStep("Two times three equals six.",                                                                                   "swapDenom3"),    // 17
            new LessonStep("Coming back to the full equation, the rule is whatever you multiply the bottom by, you have to multiply the top by the same value.", "zoomOut"), // 18
            new LessonStep("Since we multiplied the bottom by three, we have to multiply the top by three."),                                                  // 19
            new LessonStep("Multiplying the top is one times three, which is equal to three.",                                             "karaoke"),        // 20
            new LessonStep("So now we know that one-half is equal to three-sixths.",                                                       "summary"),       // 21
        };

        // ── Animation Registry ─────────────────────────────────────

        private Dictionary<string, Func<Action, IEnumerator>> animationRegistry;

        private void BuildPhase1Registry()
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
                ["transition"]       = CoAnimateTransition,
            };
        }

        private void BuildPhase2Registry()
        {
            animationRegistry["slideApart"]     = CoAnimateSlideApart;
            animationRegistry["fadeInMultiply"] = CoAnimateFadeInMultiply;
            animationRegistry["zoomInDenom"]    = CoAnimateZoomIntoDenominators;
            animationRegistry["swapDenom3"]     = CoAnimateSwapDenominator3;
            animationRegistry["zoomOut"]        = CoAnimateZoomOutToFull;
        }

        private Func<Action, IEnumerator> ResolveAnimation(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key)) return null;
            animationRegistry.TryGetValue(key, out var factory);
            return factory;
        }

        // ── Circle Layout Constants ──────────────────────────────────
        private const float CircleSize = 350f;
        private const float CircleGap = 80f;
        private const float LabelFontSize = 64f;
        private const float LabelOffsetY = -40f;

        private static readonly Color ShadeBlue = new Color(0.4f, 0.7f, 1f, 0.7f);
        private static readonly Color CircleStroke = new Color(0.9f, 0.9f, 0.9f, 1f);

        // ── Equation Layout Constants ────────────────────────────────
        private const float FractionWidth = 180f;
        private const float OperatorWidth = 90f;
        private const float ElementGap = 30f;
        private const float SlideOffset = 150f;
        private const float BarHeight = 5f;
        private const float FractionHeight = 210f;
        private const float FontSize = 72f;
        private const float ZoomScale = 1.5f;
        private const float ZoomDuration = 0.8f;

        private static readonly Color HandwriteColor = new Color(1f, 0.75f, 0.3f);

        // ── Piece/finger dwell tracking for Play button ──────────────
        private float dwellOnPlay = -1f;
        private const float DWELL_TIME = 1.0f;
        private Image playButtonImage;
        private Color playButtonBaseColor;

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

        // ── Visual Building: Phase 1 (Circles) ──────────────────────

        private void BuildCircleVisuals()
        {
            // Clear any previous children
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);

            float totalWidth = CircleSize * 2 + CircleGap;
            float leftX = -totalWidth / 2f + CircleSize / 2f;
            float rightX = totalWidth / 2f - CircleSize / 2f;

            leftCircle = CreateCircle("LeftCircle", 2,
                new bool[] { true, false },
                leftX);
            leftCircleGroup = leftCircle.GetComponent<CanvasGroup>();

            rightCircle = CreateCircle("RightCircle", 6,
                new bool[] { false, true, true, true, false, false },
                rightX);
            rightCircleGroup = rightCircle.GetComponent<CanvasGroup>();

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
            numTmp.enableWordWrapping = false;

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
            denTmp.enableWordWrapping = false;

            return cg;
        }

        // ── Visual Building: Phase 2 (Equation) ─────────────────────

        private void BuildEquation()
        {
            // Create equationRow as a child of contentArea for zoom support
            var rowGo = new GameObject("EquationRow");
            rowGo.transform.SetParent(contentArea, false);
            equationRow = rowGo.AddComponent<RectTransform>();
            equationRow.anchorMin = Vector2.zero;
            equationRow.anchorMax = Vector2.one;
            equationRow.offsetMin = Vector2.zero;
            equationRow.offsetMax = Vector2.zero;

            float totalWidth = FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth;
            float startX = -totalWidth / 2f;

            fracLeft = CreateFraction(equationRow, "1", "2", out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", "6", out fracRightNum, out fracRightDen, out fracRightTopGroup);
            fracRight.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth / 2f, 0);

            fracMiddle = CreateFraction(equationRow, "?", "?", out fracMiddleNum, out fracMiddleDen, out fracMiddleTopGroup);
            fracMiddle.anchoredPosition = Vector2.zero;
            fracMiddleGroup = fracMiddle.gameObject.AddComponent<CanvasGroup>();
            fracMiddleGroup.alpha = 0f;

            opMultiply = CreateOperator(equationRow, "\u00d7");
            opMultiply.anchoredPosition = Vector2.zero;
            opMultiplyGroup = opMultiply.gameObject.AddComponent<CanvasGroup>();
            opMultiplyGroup.alpha = 0f;
        }

        private RectTransform CreateFraction(RectTransform parent, string num, string den,
            out TextMeshProUGUI numTmp, out TextMeshProUGUI denTmp, out CanvasGroup topGroup)
        {
            var go = new GameObject("Fraction_" + num + "_" + den);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(FractionWidth, FractionHeight);

            var topGo = new GameObject("Top");
            topGo.transform.SetParent(go.transform, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.45f);
            topRect.anchorMax = Vector2.one;
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;
            topGroup = topGo.AddComponent<CanvasGroup>();

            var numGo = new GameObject("Num");
            numGo.transform.SetParent(topGo.transform, false);
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0, 0.15f);
            numRect.anchorMax = Vector2.one;
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;
            numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = num;
            numTmp.fontSize = FontSize;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;

            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(topGo.transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.05f, 0f);
            barRect.anchorMax = new Vector2(0.95f, 0f);
            barRect.offsetMin = new Vector2(0, -BarHeight / 2f);
            barRect.offsetMax = new Vector2(0, BarHeight / 2f);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = Color.white;
            barImg.raycastTarget = false;

            var denGo = new GameObject("Den");
            denGo.transform.SetParent(go.transform, false);
            var denRect = denGo.AddComponent<RectTransform>();
            denRect.anchorMin = Vector2.zero;
            denRect.anchorMax = new Vector2(1, 0.45f);
            denRect.offsetMin = Vector2.zero;
            denRect.offsetMax = Vector2.zero;
            denTmp = denGo.AddComponent<TextMeshProUGUI>();
            denTmp.text = den;
            denTmp.fontSize = FontSize;
            denTmp.alignment = TextAlignmentOptions.Center;
            denTmp.color = Color.white;

            return rect;
        }

        private RectTransform CreateOperator(RectTransform parent, string symbol)
        {
            var go = new GameObject("Op_" + symbol);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(OperatorWidth, FractionHeight);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = symbol;
            tmp.fontSize = FontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return rect;
        }

        // ── Sequence ─────────────────────────────────────────────

        private IEnumerator CoPlaySequence()
        {
            BuildCircleVisuals();
            BuildPhase1Registry();
            sequencer.Begin();
            yield return new WaitForSeconds(0.3f);

            // Steps 0–12: circles phase + transition
            // (transition step internally calls BuildEquation + BuildPhase2Registry)
            for (int i = 0; i <= 12; i++)
            {
                var anim = ResolveAnimation(steps[i].animationKey);
                yield return sequencer.RunStep(steps[i], anim);
            }

            // Steps 13–19: equation phase (standard steps)
            for (int i = 13; i < steps.Length - 2; i++)
            {
                var anim = ResolveAnimation(steps[i].animationKey);
                yield return sequencer.RunStep(steps[i], anim);
            }

            // Step 20: karaoke with handwriting (special)
            yield return RunKaraokeStep();

            // Step 21: summary with fraction pulses
            yield return RunSummaryStep();

            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ── Transition Animation ─────────────────────────────────

        private IEnumerator CoAnimateTransition(Action onComplete)
        {
            // 1. Gather all circle children for fade-out
            var circleChildren = new List<CanvasGroup>();
            if (leftCircleGroup != null) circleChildren.Add(leftCircleGroup);
            if (rightCircleGroup != null) circleChildren.Add(rightCircleGroup);
            if (leftLabelGroup != null) circleChildren.Add(leftLabelGroup);
            if (rightLabelGroup != null) circleChildren.Add(rightLabelGroup);

            // 2. Fade out circles over 0.6s
            float fadeDuration = 0.6f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                foreach (var cg in circleChildren)
                    if (cg != null) cg.alpha = 1f - t;
                yield return null;
            }

            // 3. Destroy circle children
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);
            leftCircle = null;
            rightCircle = null;
            leftCircleGroup = null;
            rightCircleGroup = null;
            leftLabelGroup = null;
            rightLabelGroup = null;

            // 4. Brief pause
            yield return new WaitForSeconds(0.3f);

            // 5. Build equation elements
            BuildEquation();
            BuildPhase2Registry();

            // 6. Fade in equation elements over 0.4s
            var eqGroup = equationRow.gameObject.AddComponent<CanvasGroup>();
            eqGroup.alpha = 0f;
            float fadeInDuration = 0.4f;
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                eqGroup.alpha = t;
                yield return null;
            }
            eqGroup.alpha = 1f;
            Destroy(eqGroup); // remove temporary group so it doesn't interfere with zoom

            onComplete?.Invoke();
        }

        // ── Phase 1: Circle Animations ───────────────────────────

        private IEnumerator CoAnimateShowCircle(FractionCircle circle, CanvasGroup cg, Action onComplete)
        {
            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Clamp01(t * 3f);
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

        // ── Phase 2: Equation Animations ─────────────────────────

        private IEnumerator CoAnimateSlideApart(Action onComplete)
        {
            Vector2 leftFrom = fracLeft.anchoredPosition;
            Vector2 leftTo = leftFrom + new Vector2(-SlideOffset, 0);

            Vector2 eqFrom = opEquals.anchoredPosition;
            Vector2 eqTo = eqFrom + new Vector2(SlideOffset, 0);

            Vector2 rightFrom = fracRight.anchoredPosition;
            Vector2 rightTo = rightFrom + new Vector2(SlideOffset, 0);

            float elapsed = 0f;
            float duration = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                fracLeft.anchoredPosition = Vector2.Lerp(leftFrom, leftTo, s);
                opEquals.anchoredPosition = Vector2.Lerp(eqFrom, eqTo, s);
                fracRight.anchoredPosition = Vector2.Lerp(rightFrom, rightTo, s);
                yield return null;
            }

            fracLeft.anchoredPosition = leftTo;
            opEquals.anchoredPosition = eqTo;
            fracRight.anchoredPosition = rightTo;
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateFadeInMultiply(Action onComplete)
        {
            float midX = (fracLeft.anchoredPosition.x + opEquals.anchoredPosition.x) / 2f;
            float mulX = fracLeft.anchoredPosition.x + (midX - fracLeft.anchoredPosition.x) / 2f + OperatorWidth / 4f;
            float fracMidX = midX + (opEquals.anchoredPosition.x - midX) / 2f - OperatorWidth / 4f;

            opMultiply.anchoredPosition = new Vector2(mulX, 0);
            fracMiddle.anchoredPosition = new Vector2(fracMidX, 0);

            bool a = false, b = false;
            StartCoroutine(CoFadeIn(opMultiplyGroup, 0.4f, () => a = true));
            StartCoroutine(CoFadeIn(fracMiddleGroup, 0.4f, () => b = true));

            yield return new WaitUntil(() => a && b);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateZoomIntoDenominators(Action onComplete)
        {
            savedEquationScale = equationRow.localScale;
            savedEquationY = equationRow.anchoredPosition.y;
            savedOpEqualsY = opEquals.anchoredPosition.y;
            savedOpMultiplyY = opMultiply.anchoredPosition.y;

            float targetY = savedEquationY + FractionHeight * 0.35f;
            Vector3 targetScale = savedEquationScale * ZoomScale;

            float opDropY = -FractionHeight * 0.275f;
            float opEqualsTargetY = savedOpEqualsY + opDropY;
            float opMultiplyTargetY = savedOpMultiplyY + opDropY;

            bool fadeA = false, fadeB = false, fadeC = false;
            StartCoroutine(CoFadeOut(fracLeftTopGroup, ZoomDuration * 0.6f, () => fadeA = true));
            StartCoroutine(CoFadeOut(fracMiddleTopGroup, ZoomDuration * 0.6f, () => fadeB = true));
            StartCoroutine(CoFadeOut(fracRightTopGroup, ZoomDuration * 0.6f, () => fadeC = true));

            float elapsed = 0f;
            while (elapsed < ZoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ZoomDuration));
                equationRow.localScale = Vector3.Lerp(savedEquationScale, targetScale, t);
                equationRow.anchoredPosition = new Vector2(
                    equationRow.anchoredPosition.x,
                    Mathf.Lerp(savedEquationY, targetY, t));

                opEquals.anchoredPosition = new Vector2(
                    opEquals.anchoredPosition.x,
                    Mathf.Lerp(savedOpEqualsY, opEqualsTargetY, t));
                opMultiply.anchoredPosition = new Vector2(
                    opMultiply.anchoredPosition.x,
                    Mathf.Lerp(savedOpMultiplyY, opMultiplyTargetY, t));

                yield return null;
            }

            equationRow.localScale = targetScale;
            equationRow.anchoredPosition = new Vector2(equationRow.anchoredPosition.x, targetY);
            opEquals.anchoredPosition = new Vector2(opEquals.anchoredPosition.x, opEqualsTargetY);
            opMultiply.anchoredPosition = new Vector2(opMultiply.anchoredPosition.x, opMultiplyTargetY);

            yield return new WaitUntil(() => fadeA && fadeB && fadeC);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateSwapDenominator3(Action onComplete)
        {
            yield return CoSwapText(fracMiddleDen, "3", 0.4f, null);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateZoomOutToFull(Action onComplete)
        {
            Vector3 currentScale = equationRow.localScale;
            float currentY = equationRow.anchoredPosition.y;
            float currentOpEqualsY = opEquals.anchoredPosition.y;
            float currentOpMultiplyY = opMultiply.anchoredPosition.y;

            float elapsed = 0f;
            while (elapsed < ZoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ZoomDuration));
                equationRow.localScale = Vector3.Lerp(currentScale, savedEquationScale, t);
                equationRow.anchoredPosition = new Vector2(
                    equationRow.anchoredPosition.x,
                    Mathf.Lerp(currentY, savedEquationY, t));

                opEquals.anchoredPosition = new Vector2(
                    opEquals.anchoredPosition.x,
                    Mathf.Lerp(currentOpEqualsY, savedOpEqualsY, t));
                opMultiply.anchoredPosition = new Vector2(
                    opMultiply.anchoredPosition.x,
                    Mathf.Lerp(currentOpMultiplyY, savedOpMultiplyY, t));

                yield return null;
            }

            equationRow.localScale = savedEquationScale;
            equationRow.anchoredPosition = new Vector2(equationRow.anchoredPosition.x, savedEquationY);
            opEquals.anchoredPosition = new Vector2(opEquals.anchoredPosition.x, savedOpEqualsY);
            opMultiply.anchoredPosition = new Vector2(opMultiply.anchoredPosition.x, savedOpMultiplyY);

            bool fadeA = false, fadeB = false, fadeC = false;
            StartCoroutine(CoFadeIn(fracLeftTopGroup, ZoomDuration * 0.6f, () => fadeA = true));
            StartCoroutine(CoFadeIn(fracMiddleTopGroup, ZoomDuration * 0.6f, () => fadeB = true));
            StartCoroutine(CoFadeIn(fracRightTopGroup, ZoomDuration * 0.6f, () => fadeC = true));

            yield return new WaitUntil(() => fadeA && fadeB && fadeC);
            onComplete?.Invoke();
        }

        // ── Karaoke Step ─────────────────────────────────────────

        private IEnumerator RunKaraokeStep()
        {
            var step = steps[steps.Length - 2]; // second-to-last step is karaoke
            bool karaokeDone = false;

            StartCoroutine(sequencer.CoShowKaraokeSubtitle(
                step.subtitle, 3f,
                (idx, word, duration) =>
                {
                    if (idx == 4)
                        StartCoroutine(CoPulseScale(fracLeftNum.transform, 1.3f, duration));
                    else if (idx == 6)
                        StartCoroutine(CoHandwriteDigit(
                            fracMiddleNum.rectTransform.parent as RectTransform,
                            fracMiddleNum, duration, null));
                    else if (idx == 11)
                        StartCoroutine(CoHandwriteDigit(
                            fracRightNum.rectTransform.parent as RectTransform,
                            fracRightNum, duration, null));
                },
                () => karaokeDone = true));

            yield return new WaitUntil(() => karaokeDone);
            yield return new WaitForSeconds(0.5f);
        }

        // "So now we know that one-half is equal to three-sixths."
        //   0   1   2    3    4      5     6   7     8      9
        private IEnumerator RunSummaryStep()
        {
            var step = steps[steps.Length - 1];
            bool done = false;

            StartCoroutine(sequencer.CoShowKaraokeSubtitle(
                step.subtitle, 3f,
                (idx, word, duration) =>
                {
                    // "one-half" → pulse the left fraction (1/2)
                    if (idx == 5)
                        StartCoroutine(CoPulseScale(fracLeft, 1.15f, duration));
                    // "three-sixths." → pulse the right fraction (3/6)
                    else if (idx == 9)
                        StartCoroutine(CoPulseScale(fracRight, 1.15f, duration));
                },
                () => done = true));

            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.5f);
        }

        // ── Karaoke + Handwriting Coroutines ─────────────────────

        private IEnumerator CoPulseScale(Transform target, float scaleFactor, float duration)
        {
            Vector3 origScale = target.localScale;
            Vector3 peakScale = origScale * scaleFactor;

            float rampUp = duration * 0.3f;
            float hold = duration * 0.4f;
            float rampDown = duration * 0.3f;

            float elapsed = 0f;
            while (elapsed < rampUp)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rampUp);
                target.localScale = Vector3.Lerp(origScale, peakScale, EaseOutBack(t));
                yield return null;
            }
            target.localScale = peakScale;

            yield return new WaitForSeconds(hold);

            elapsed = 0f;
            while (elapsed < rampDown)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rampDown);
                target.localScale = Vector3.Lerp(peakScale, origScale, t);
                yield return null;
            }
            target.localScale = origScale;
        }

        private IEnumerator CoHandwriteDigit(RectTransform parent, TextMeshProUGUI existingText,
            float duration, Action onComplete)
        {
            existingText.text = "3";
            existingText.color = HandwriteColor;
            existingText.ForceMeshUpdate();

            RectTransform numRect = existingText.rectTransform;

            Vector2 savedAnchorMin = numRect.anchorMin;
            Vector2 savedAnchorMax = numRect.anchorMax;
            Vector2 savedOffsetMin = numRect.offsetMin;
            Vector2 savedOffsetMax = numRect.offsetMax;

            var maskGo = new GameObject("RevealMask");
            maskGo.transform.SetParent(parent, false);
            var maskRect = maskGo.AddComponent<RectTransform>();
            maskRect.anchorMin = savedAnchorMin;
            maskRect.anchorMax = savedAnchorMax;
            maskRect.offsetMin = savedOffsetMin;
            maskRect.offsetMax = savedOffsetMax;

            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var stroke = maskGo.AddComponent<StrokeRevealMask>();
            stroke.Progress = 0f;
            stroke.color = Color.white;

            numRect.SetParent(maskRect, false);
            numRect.anchorMin = Vector2.zero;
            numRect.anchorMax = Vector2.one;
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stroke.Progress = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            stroke.Progress = 1f;

            numRect.SetParent(parent, false);
            numRect.anchorMin = savedAnchorMin;
            numRect.anchorMax = savedAnchorMax;
            numRect.offsetMin = savedOffsetMin;
            numRect.offsetMax = savedOffsetMax;
            Destroy(maskGo);

            onComplete?.Invoke();
        }

        // ── Shared Animation Coroutines ──────────────────────────

        private IEnumerator CoFadeIn(CanvasGroup cg, float duration, Action onComplete)
        {
            float elapsed = 0f;
            float startAlpha = cg.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }
            cg.alpha = 1f;
            onComplete?.Invoke();
        }

        private IEnumerator CoFadeOut(CanvasGroup cg, float duration, Action onComplete)
        {
            float elapsed = 0f;
            float startAlpha = cg.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            cg.alpha = 0f;
            onComplete?.Invoke();
        }

        private IEnumerator CoSwapText(TextMeshProUGUI tmp, string newValue, float duration, Action onComplete)
        {
            float half = duration / 2f;

            float elapsed = 0f;
            Vector3 origScale = tmp.transform.localScale;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                tmp.transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
                yield return null;
            }

            tmp.text = newValue;
            tmp.color = HandwriteColor;

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                tmp.transform.localScale = origScale * EaseOutBack(t);
                yield return null;
            }
            tmp.transform.localScale = origScale;
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
