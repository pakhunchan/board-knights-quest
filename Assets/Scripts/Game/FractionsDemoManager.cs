using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using BoardOfEducation.Input;
using BoardOfEducation.Navigation;
using BoardOfEducation.UI;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Drives the Fractions Demo: builds a fraction equation at runtime,
    /// animates it through 8 steps with synchronized subtitles.
    /// Includes a denominator-focus zoom effect.
    /// Step 8 uses karaoke-style word highlighting with handwritten digit animations.
    /// </summary>
    public class FractionsDemoManager : MonoBehaviour
    {
        [SerializeField] private RectTransform equationRow;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;

        // Runtime references to equation elements
        private RectTransform fracLeft;      // 1/2
        private RectTransform opEquals;      // =
        private RectTransform fracRight;     // ?/6
        private RectTransform opMultiply;    // ×
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
            new EquationStep("One half equals how many sixths?"),
            new EquationStep("Let's shift this over to the right to make space"),
            new EquationStep("We need to multiply one half by something to turn it into something over six."),
            new EquationStep("Focus first on the bottom numbers, the denominators."),
            new EquationStep("Two times three equals six."),
            new EquationStep("Coming back to the full equation, the rule is whatever you multiply the bottom by, you have to multiply the top by the same value."),
            new EquationStep("Since we multiplied the bottom by three, we have to multiply the top by three."),
            new EquationStep("Multiplying the top is one times three, which is equal to three."),
        };

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

        // ── Word Timing for Karaoke ──────────────────────────────

        private struct WordTiming
        {
            public string word;       // original word with punctuation
            public string cleanWord;  // lowercase, stripped of punctuation
            public float duration;    // seconds to display this word
            public int charStart;     // index in original string
            public int charEnd;       // end index (exclusive) in original string
        }

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
            yield return new WaitForSeconds(0.3f);

            // Step 1: 1/2 = ?/6 — display initial equation
            yield return RunStep(0, null);

            // Step 2: Slide apart to make space
            yield return RunStep(1, CoAnimateSlideApart);

            // Step 3: Fade in × ?/?
            yield return RunStep(2, CoAnimateFadeInMultiply);

            // Step 4: Focus on denominators (zoom in)
            yield return RunStep(3, CoAnimateZoomIntoDenominators);

            // Step 5: Swap denominator ? → 3
            yield return RunStep(4, CoAnimateSwapDenominator3);

            // Step 6: Zoom back out, show full equation
            yield return RunStep(5, CoAnimateZoomOutToFull);

            // Step 7: Subtitle only (middle num swap moved to step 8 karaoke)
            yield return RunStep(6, null);

            // Step 8: Karaoke subtitle with handwritten digits
            yield return RunStep8Karaoke();

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

        // ── Step 8: Karaoke with Handwriting ─────────────────────

        private IEnumerator RunStep8Karaoke()
        {
            string text = steps[7].subtitle;
            WordTiming[] timings = BuildWordTimings(text, 3f);

            bool karaokeDone = false;
            StartCoroutine(CoShowKaraokeSubtitle(timings, text, (idx, wt) =>
            {
                // idx 4 = "one" → pulse the left numerator "1"
                if (idx == 4)
                    StartCoroutine(CoPulseScale(fracLeftNum.transform, 1.3f, wt.duration));
                // idx 6 = "three," → handwrite middle numerator
                else if (idx == 6)
                    StartCoroutine(CoHandwriteDigit(
                        fracMiddleNum.rectTransform.parent as RectTransform,
                        fracMiddleNum, wt.duration, null));
                // idx 11 = "three." → handwrite right numerator
                else if (idx == 11)
                    StartCoroutine(CoHandwriteDigit(
                        fracRightNum.rectTransform.parent as RectTransform,
                        fracRightNum, wt.duration, null));
            }, () => karaokeDone = true));

            yield return new WaitUntil(() => karaokeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private WordTiming[] BuildWordTimings(string text, float wordsPerSecond)
        {
            string[] words = text.Split(' ');
            var timings = new WordTiming[words.Length];

            // Calculate total character count for proportional timing
            int totalChars = 0;
            foreach (var w in words)
                totalChars += w.Length;

            float totalDuration = words.Length / wordsPerSecond;
            int charPos = 0;

            // Trigger words get a minimum duration for their animations
            var triggerWords = new HashSet<string> { "one", "three" };

            for (int i = 0; i < words.Length; i++)
            {
                string clean = words[i].TrimEnd(',', '.', '!', '?', ';', ':').ToLower();
                float proportion = (float)words[i].Length / totalChars;
                float dur = Mathf.Max(0.2f, proportion * totalDuration);

                if (triggerWords.Contains(clean))
                    dur = Mathf.Max(0.6f, dur);

                timings[i] = new WordTiming
                {
                    word = words[i],
                    cleanWord = clean,
                    duration = dur,
                    charStart = charPos,
                    charEnd = charPos + words[i].Length
                };
                charPos += words[i].Length + 1; // +1 for space
            }

            return timings;
        }

        // ── Step Animations ─────────────────────────────────────

        private IEnumerator CoAnimateSlideApart(System.Action onComplete)
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

        private IEnumerator CoAnimateFadeInMultiply(System.Action onComplete)
        {
            // Position × and ?/? in the gap between fracLeft and opEquals
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

        private IEnumerator CoAnimateZoomIntoDenominators(System.Action onComplete)
        {
            // Save current state for zoom-out
            savedEquationScale = equationRow.localScale;
            savedEquationY = equationRow.anchoredPosition.y;
            savedOpEqualsY = opEquals.anchoredPosition.y;
            savedOpMultiplyY = opMultiply.anchoredPosition.y;

            float targetY = savedEquationY + FractionHeight * 0.35f;
            Vector3 targetScale = savedEquationScale * ZoomScale;

            // Operators are centered in FractionHeight; slide them down to denominator level
            // Denominators occupy the bottom 45% of the fraction rect, centered around 22.5%
            // Operators sit at 50% — so shift down by ~27.5% of FractionHeight
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

        private IEnumerator CoAnimateSwapDenominator3(System.Action onComplete)
        {
            // Swap the middle fraction denominator ? → 3
            yield return CoSwapText(fracMiddleDen, "3", 0.4f, null);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateZoomOutToFull(System.Action onComplete)
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

        private IEnumerator CoShowKaraokeSubtitle(WordTiming[] timings, string fullText,
            System.Action<int, WordTiming> onWordStart, System.Action onComplete)
        {
            subtitleText.text = fullText;

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

            // Walk through words one at a time
            string[] words = fullText.Split(' ');
            for (int i = 0; i < timings.Length; i++)
            {
                // Rebuild subtitle with current word highlighted
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

                onWordStart?.Invoke(i, timings[i]);
                yield return new WaitForSeconds(timings[i].duration);
            }

            // Restore plain text (no highlight) briefly
            subtitleText.text = fullText;
            yield return new WaitForSeconds(0.3f);

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
            float duration, System.Action onComplete)
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

            // Animate: Progress 0→1 traces the bezier stroke, revealing the TMP "3"
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

        private IEnumerator CoFadeIn(CanvasGroup cg, float duration, System.Action onComplete)
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

        private IEnumerator CoFadeOut(CanvasGroup cg, float duration, System.Action onComplete)
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

        private IEnumerator CoSwapText(TextMeshProUGUI tmp, string newValue, float duration, System.Action onComplete)
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

            // Highlight one word at a time in red
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

        // ── Helpers ─────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
