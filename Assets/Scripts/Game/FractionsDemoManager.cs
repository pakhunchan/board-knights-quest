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
    /// Drives the Fractions Demo: builds a fraction equation at runtime,
    /// animates it through 8 steps with synchronized subtitles.
    /// Includes a denominator-focus zoom effect.
    /// Step 8 uses karaoke-style word highlighting with handwritten digit animations.
    ///
    /// Step text and animation keys are co-located in a single LessonStep array.
    /// Playback is delegated to the shared LessonSequencer (barrier sync).
    /// </summary>
    public class FractionsDemoManager : MonoBehaviour
    {
        [SerializeField] private RectTransform equationRow;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;
        [SerializeField] private LessonSequencer sequencer;

        // Runtime references to equation elements
        private RectTransform fracLeft;      // 1/2
        private RectTransform opEquals;      // =
        private RectTransform fracRight;     // ?/6
        private RectTransform opMultiply;    // x
        private RectTransform fracMiddle;    // ?/?

        // Text references
        private TextMeshProUGUI fracLeftNum, fracLeftDen;
        private TextMeshProUGUI fracRightNum, fracRightDen;
        private TextMeshProUGUI fracMiddleNum, fracMiddleDen;

        // CanvasGroups for visibility control
        private CanvasGroup fracMiddleGroup;
        private CanvasGroup opMultiplyGroup;

        // Per-fraction top halves (numerator + bar) for focus effect
        private CanvasGroup fracLeftTopGroup;
        private CanvasGroup fracMiddleTopGroup;
        private CanvasGroup fracRightTopGroup;

        // ── Step Definitions (single source of truth) ──────────────

        private readonly LessonStep[] steps = new LessonStep[]
        {
            new LessonStep("One half equals how many sixths?"),                                                                                               // 0
            new LessonStep("Let's shift this over to the right to make space",                                                             "slideApart"),     // 1
            new LessonStep("We need to multiply one half by something to turn it into something over six.",                                "fadeInMultiply"), // 2
            new LessonStep("Focus first on the bottom numbers, the denominators.",                                                         "zoomInDenom"),   // 3
            new LessonStep("Two times three equals six.",                                                                                   "swapDenom3"),    // 4
            new LessonStep("Coming back to the full equation, the rule is whatever you multiply the bottom by, you have to multiply the top by the same value.", "zoomOut"), // 5
            new LessonStep("Since we multiplied the bottom by three, we have to multiply the top by three."),                                                  // 6
            new LessonStep("Multiplying the top is one times three, which is equal to three.",                                             "karaoke"),        // 7
        };

        // ── Animation Registry ─────────────────────────────────────

        private Dictionary<string, Func<Action, IEnumerator>> animationRegistry;

        private void BuildAnimationRegistry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["slideApart"]     = CoAnimateSlideApart,
                ["fadeInMultiply"] = CoAnimateFadeInMultiply,
                ["zoomInDenom"]    = CoAnimateZoomIntoDenominators,
                ["swapDenom3"]     = CoAnimateSwapDenominator3,
                ["zoomOut"]        = CoAnimateZoomOutToFull,
            };
        }

        private Func<Action, IEnumerator> ResolveAnimation(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            animationRegistry.TryGetValue(key, out var factory);
            return factory;
        }

        // Layout constants — 50% larger than original
        private const float FractionWidth = 180f;
        private const float OperatorWidth = 90f;
        private const float ElementGap = 30f;
        private const float SlideOffset = 150f;
        private const float BarHeight = 5f;
        private const float FractionHeight = 210f;
        private const float FontSize = 72f;

        // Zoom effect constants
        private const float ZoomScale = 1.5f;
        private const float ZoomDuration = 0.8f;

        // Handwritten digit color — matches the green highlight used in CoSwapText
        private static readonly Color HandwriteColor = new Color(0.18f, 0.8f, 0.44f);

        // Piece/finger dwell tracking for Play button
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

            // Find any active contact (piece or finger)
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

        // ── Equation Building ───────────────────────────────────

        private void BuildEquation()
        {
            for (int i = equationRow.childCount - 1; i >= 0; i--)
                Destroy(equationRow.GetChild(i).gameObject);

            // Center the initial equation: frac + gap + op + gap + frac
            float totalWidth = FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth;
            float startX = -totalWidth / 2f;

            // Left fraction: 1/2
            fracLeft = CreateFraction(equationRow, "1", "2", out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            // Equals sign
            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            // Right fraction: ?/6
            fracRight = CreateFraction(equationRow, "?", "6", out fracRightNum, out fracRightDen, out fracRightTopGroup);
            fracRight.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth / 2f, 0);

            // Middle fraction (hidden): ?/?
            fracMiddle = CreateFraction(equationRow, "?", "?", out fracMiddleNum, out fracMiddleDen, out fracMiddleTopGroup);
            fracMiddle.anchoredPosition = Vector2.zero;
            fracMiddleGroup = fracMiddle.gameObject.AddComponent<CanvasGroup>();
            fracMiddleGroup.alpha = 0f;

            // Multiply operator (hidden)
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

            // Top group (numerator + bar) — wrapped so we can fade together
            var topGo = new GameObject("Top");
            topGo.transform.SetParent(go.transform, false);
            var topRect = topGo.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.45f);
            topRect.anchorMax = Vector2.one;
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = Vector2.zero;
            topGroup = topGo.AddComponent<CanvasGroup>();

            // Numerator (inside top group)
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

            // Bar (inside top group)
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

            // Denominator (direct child, outside top group)
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

        // ── Sequence ────────────────────────────────────────────

        private IEnumerator CoPlaySequence()
        {
            BuildEquation();
            BuildAnimationRegistry();
            sequencer.Begin();
            yield return new WaitForSeconds(0.3f);

            // Steps 0–6: data-driven via animation registry
            for (int i = 0; i < steps.Length - 1; i++)
            {
                var step = steps[i];
                var anim = ResolveAnimation(step.animationKey);
                yield return sequencer.RunStep(step, anim);
            }

            // Step 7: karaoke with handwriting (special — uses word-boundary callbacks)
            yield return RunStep7Karaoke();

            // Done — show replay
            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ── Step 7: Karaoke with Handwriting ─────────────────────

        private IEnumerator RunStep7Karaoke()
        {
            var step = steps[7];
            bool karaokeDone = false;

            StartCoroutine(sequencer.CoShowKaraokeSubtitle(
                step.subtitle, 3f,
                (idx, word, duration) =>
                {
                    // idx 4 = "one" -> pulse the left numerator "1"
                    if (idx == 4)
                        StartCoroutine(CoPulseScale(fracLeftNum.transform, 1.3f, duration));
                    // idx 6 = "three," -> handwrite middle numerator
                    else if (idx == 6)
                        StartCoroutine(CoHandwriteDigit(
                            fracMiddleNum.rectTransform.parent as RectTransform,
                            fracMiddleNum, duration, null));
                    // idx 11 = "three." -> handwrite right numerator
                    else if (idx == 11)
                        StartCoroutine(CoHandwriteDigit(
                            fracRightNum.rectTransform.parent as RectTransform,
                            fracRightNum, duration, null));
                },
                () => karaokeDone = true));

            yield return new WaitUntil(() => karaokeDone);
            yield return new WaitForSeconds(0.5f);
        }

        // ── Step Animations ─────────────────────────────────────

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
            // Position x and ?/? in the gap between fracLeft and opEquals
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

        // Saved positions for zoom restore
        private Vector3 savedEquationScale;
        private float savedEquationY;
        private float savedOpEqualsY;
        private float savedOpMultiplyY;

        private IEnumerator CoAnimateZoomIntoDenominators(Action onComplete)
        {
            // Save current state for zoom-out
            savedEquationScale = equationRow.localScale;
            savedEquationY = equationRow.anchoredPosition.y;
            savedOpEqualsY = opEquals.anchoredPosition.y;
            savedOpMultiplyY = opMultiply.anchoredPosition.y;

            float targetY = savedEquationY + FractionHeight * 0.35f;
            Vector3 targetScale = savedEquationScale * ZoomScale;

            float opDropY = -FractionHeight * 0.275f;
            float opEqualsTargetY = savedOpEqualsY + opDropY;
            float opMultiplyTargetY = savedOpMultiplyY + opDropY;

            // Fade out all numerators + bars simultaneously
            bool fadeA = false, fadeB = false, fadeC = false;
            StartCoroutine(CoFadeOut(fracLeftTopGroup, ZoomDuration * 0.6f, () => fadeA = true));
            StartCoroutine(CoFadeOut(fracMiddleTopGroup, ZoomDuration * 0.6f, () => fadeB = true));
            StartCoroutine(CoFadeOut(fracRightTopGroup, ZoomDuration * 0.6f, () => fadeC = true));

            // Zoom, slide, and drop operators
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
            // Swap the middle fraction denominator ? -> 3
            yield return CoSwapText(fracMiddleDen, "3", 0.4f, null);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateZoomOutToFull(Action onComplete)
        {
            Vector3 currentScale = equationRow.localScale;
            float currentY = equationRow.anchoredPosition.y;
            float currentOpEqualsY = opEquals.anchoredPosition.y;
            float currentOpMultiplyY = opMultiply.anchoredPosition.y;

            // Zoom back, slide down, and restore operator positions
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

            // Fade numerators + bars back in
            bool fadeA = false, fadeB = false, fadeC = false;
            StartCoroutine(CoFadeIn(fracLeftTopGroup, ZoomDuration * 0.6f, () => fadeA = true));
            StartCoroutine(CoFadeIn(fracMiddleTopGroup, ZoomDuration * 0.6f, () => fadeB = true));
            StartCoroutine(CoFadeIn(fracRightTopGroup, ZoomDuration * 0.6f, () => fadeC = true));

            yield return new WaitUntil(() => fadeA && fadeB && fadeC);
            onComplete?.Invoke();
        }

        // ── Karaoke + Handwriting Coroutines ─────────────────────

        private IEnumerator CoPulseScale(Transform target, float scaleFactor, float duration)
        {
            Vector3 origScale = target.localScale;
            Vector3 peakScale = origScale * scaleFactor;

            float rampUp = duration * 0.3f;
            float hold = duration * 0.4f;
            float rampDown = duration * 0.3f;

            // Scale up
            float elapsed = 0f;
            while (elapsed < rampUp)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rampUp);
                target.localScale = Vector3.Lerp(origScale, peakScale, EaseOutBack(t));
                yield return null;
            }
            target.localScale = peakScale;

            // Hold
            yield return new WaitForSeconds(hold);

            // Scale back down
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
            // Set the final "3" text — the mask will reveal it stroke-by-stroke
            existingText.text = "3";
            existingText.color = HandwriteColor;
            existingText.ForceMeshUpdate();

            RectTransform numRect = existingText.rectTransform;

            // Save Num's original anchors within parent (the Top group)
            Vector2 savedAnchorMin = numRect.anchorMin;
            Vector2 savedAnchorMax = numRect.anchorMax;
            Vector2 savedOffsetMin = numRect.offsetMin;
            Vector2 savedOffsetMax = numRect.offsetMax;

            // Create a mask wrapper occupying the same area as Num
            var maskGo = new GameObject("RevealMask");
            maskGo.transform.SetParent(parent, false);
            var maskRect = maskGo.AddComponent<RectTransform>();
            maskRect.anchorMin = savedAnchorMin;
            maskRect.anchorMax = savedAnchorMax;
            maskRect.offsetMin = savedOffsetMin;
            maskRect.offsetMax = savedOffsetMax;

            // Add Mask + StrokeRevealMask — the bezier quad-strip defines the stencil shape
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var stroke = maskGo.AddComponent<StrokeRevealMask>();
            stroke.Progress = 0f;
            stroke.color = Color.white;

            // Reparent Num under the mask, filling it completely
            numRect.SetParent(maskRect, false);
            numRect.anchorMin = Vector2.zero;
            numRect.anchorMax = Vector2.one;
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;

            // Animate: Progress 0->1 traces the bezier stroke, revealing the TMP "3"
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stroke.Progress = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            stroke.Progress = 1f;

            // Restore: reparent Num back to original parent, restore anchors
            numRect.SetParent(parent, false);
            numRect.anchorMin = savedAnchorMin;
            numRect.anchorMax = savedAnchorMax;
            numRect.offsetMin = savedOffsetMin;
            numRect.offsetMax = savedOffsetMax;
            Destroy(maskGo);

            onComplete?.Invoke();
        }

        // ── Animation Coroutines ────────────────────────────────

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

            // Scale down
            float elapsed = 0f;
            Vector3 origScale = tmp.transform.localScale;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                tmp.transform.localScale = Vector3.Lerp(origScale, Vector3.zero, t);
                yield return null;
            }

            // Swap text and color
            tmp.text = newValue;
            tmp.color = new Color(0.18f, 0.8f, 0.44f); // green highlight

            // Scale up with EaseOutBack
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

        // ── Helpers ─────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
