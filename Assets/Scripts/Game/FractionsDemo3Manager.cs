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
    /// Interactive fractions lesson: 2/3 = ?/9
    /// Students answer by placing Board Arcade pieces onto answer circles.
    /// Follows the same step/sequencer architecture as TotalFractionsDemoManager
    /// but adds interactive input sections driven by piece dwell.
    /// </summary>
    public class FractionsDemo3Manager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;
        [SerializeField] private LessonSequencer sequencer;

        // ── Equation elements — set by BuildEquation() ──
        private RectTransform equationRow;
        private CanvasGroup equationRowGroup;
        private RectTransform fracLeft, opEquals, fracRight, opMultiply, fracMiddle;
        private TextMeshProUGUI fracLeftNum, fracLeftDen, fracRightNum, fracRightDen;
        private TextMeshProUGUI fracMiddleNum, fracMiddleDen;
        private CanvasGroup fracMiddleGroup, opMultiplyGroup;
        private CanvasGroup fracLeftTopGroup, fracMiddleTopGroup, fracRightTopGroup;
        private Vector3 savedEquationScale;
        private float savedEquationY, savedOpEqualsY, savedOpMultiplyY;

        // ── Answer circle tracking ──
        private GameObject[] answerCircles;
        private RectTransform[] answerCircleRects;
        private float[] answerDwellTimes;

        // ── Step Definitions ──────────────────────────────────────

        private readonly LessonStep[] steps = new LessonStep[]
        {
            new LessonStep("Let's try another example"),                                                                       // 0
            new LessonStep("Two thirds is equal to how many ninths?",                                                          // 1
                "showEquation"),
            new LessonStep("First we'll make some space",                                                                      // 2
                "slideApart"),
            new LessonStep("We have to multiply two thirds by something to figure out how many ninths it's equal to",          // 3
                "fadeInMultiply"),
            new LessonStep("Three times what equals nine?"),                                                                    // 4
            new LessonStep("Three times three is equal to nine, so move your piece to the three"),                              // 5
            new LessonStep("Whatever value we used for the bottom, we have to use for the top, so put a three on the top as well"), // 6
            new LessonStep("Let's now multiply the top values"),                                                                // 7
            new LessonStep("Two times three equals what?"),                                                                     // 8
            new LessonStep("Two times three equals six"),                                                                       // 9
            new LessonStep("Move your piece to the six"),                                                                       // 10
            new LessonStep("Good job"),                                                                                         // 11
            new LessonStep("So now we know that two thirds is equal to six ninths",                                             // 12
                "summary"),
        };

        // ── Animation Registry ────────────────────────────────────

        private Dictionary<string, Func<Action, IEnumerator>> animationRegistry;

        private void BuildAnimationRegistry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["showEquation"]   = CoAnimateShowEquation,
                ["slideApart"]     = CoAnimateSlideApart,
                ["fadeInMultiply"] = CoAnimateFadeInMultiply,
            };
        }

        private Func<Action, IEnumerator> ResolveAnimation(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key)) return null;
            animationRegistry.TryGetValue(key, out var factory);
            return factory;
        }

        // ── Equation Layout Constants ──────────────────────────────
        private const float FractionWidth = 180f;
        private const float OperatorWidth = 90f;
        private const float ElementGap = 30f;
        private const float SlideOffset = 150f;
        private const float BarHeight = 5f;
        private const float FractionHeight = 210f;
        private const float FontSize = 72f;
        private const float ZoomScale = 1.5f;
        private const float ZoomDuration = 0.8f;

        // ── Answer Circle Constants ────────────────────────────────
        private const float CircleSize = 156f;
        private const float CircleSpacing = 320f;
        private const float CircleY = -240f;
        private const float DWELL_TIME = 1.0f;

        private static readonly Color HandwriteColor = new Color(1f, 0.75f, 0.3f);
        private static readonly Color CircleDefaultColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color CircleCorrectColor = new Color(0.18f, 0.8f, 0.44f, 1f);
        private static readonly Color CircleWrongColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        // ── Play button dwell ──────────────────────────────────────
        private float dwellOnPlay = -1f;
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
                ResetPlayDwell();
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
                ResetPlayDwell();
            }
        }

        private void ResetPlayDwell()
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

        // ── Equation Building ─────────────────────────────────────

        private void BuildEquation()
        {
            // Clear any previous children
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);

            // Create equationRow as a child of contentArea for zoom support
            var rowGo = new GameObject("EquationRow");
            rowGo.transform.SetParent(contentArea, false);
            equationRow = rowGo.AddComponent<RectTransform>();
            equationRow.anchorMin = Vector2.zero;
            equationRow.anchorMax = Vector2.one;
            equationRow.offsetMin = Vector2.zero;
            equationRow.offsetMax = Vector2.zero;

            // Add CanvasGroup for initial fade-in
            equationRowGroup = rowGo.AddComponent<CanvasGroup>();
            equationRowGroup.alpha = 0f;

            float totalWidth = FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth;
            float startX = -totalWidth / 2f;

            fracLeft = CreateFraction(equationRow, "2", "3", out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", "9", out fracRightNum, out fracRightDen, out fracRightTopGroup);
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

        // ── Main Sequence ─────────────────────────────────────────

        private IEnumerator CoPlaySequence()
        {
            BuildEquation();
            BuildAnimationRegistry();
            sequencer.Begin();
            yield return new WaitForSeconds(0.3f);

            // Section 1: Standard steps 0–3
            for (int i = 0; i <= 3; i++)
            {
                var anim = ResolveAnimation(steps[i].animationKey);
                yield return sequencer.RunStep(steps[i], anim);
            }

            // Section 2: Denominator interaction (steps 4–5)
            yield return RunDenominatorInteraction();

            // Section 3: Numerator multiplier interaction (step 6)
            yield return RunNumeratorInteraction();

            // Section 4: Final product interaction (steps 7–10)
            yield return RunFinalProductInteraction();

            // Section 5: Conclusion
            yield return sequencer.RunStep(steps[11], null);  // "Good job"
            yield return RunSummaryStep();                     // step 12

            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ── Interactive Sections ──────────────────────────────────

        private IEnumerator RunDenominatorInteraction()
        {
            // Zoom into denominators
            bool zoomDone = false;
            StartCoroutine(CoAnimateZoomIntoDenominators(() => zoomDone = true));
            yield return new WaitUntil(() => zoomDone);

            // Show answer circles (2, 3, 4)
            yield return CoShowAnswerCircles(new string[] { "2", "3", "4" });

            // Step 4: "Three times what equals nine?"
            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[4].subtitle, steps[4].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(1f);

            // Step 5: "Three times three is equal to nine, so move your piece to the three"
            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[5].subtitle, steps[5].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Wait for correct answer: circle index 1 ("3")
            yield return CoWaitForAnswer(1);

            // Correct: destroy circles, handwrite "3" on fracMiddleDen
            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracMiddleDen.rectTransform.parent as RectTransform,
                fracMiddleDen, "3", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunNumeratorInteraction()
        {
            // Zoom back out to full equation
            bool zoomDone = false;
            StartCoroutine(CoAnimateZoomOutToFull(() => zoomDone = true));
            yield return new WaitUntil(() => zoomDone);

            // Show answer circles (2, 3, 4)
            yield return CoShowAnswerCircles(new string[] { "2", "3", "4" });

            // Step 6 subtitle
            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[6].subtitle, steps[6].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Wait for correct answer: circle index 1 ("3")
            yield return CoWaitForAnswer(1);

            // Correct: destroy circles, handwrite "3" on fracMiddleNum
            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracMiddleNum.rectTransform.parent as RectTransform,
                fracMiddleNum, "3", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunFinalProductInteraction()
        {
            // Show answer circles (4, 5, 6)
            yield return CoShowAnswerCircles(new string[] { "4", "5", "6" });

            // Step 7: "Let's now multiply the top values"
            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[7].subtitle, steps[7].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Step 8: "Two times three equals what?"
            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[8].subtitle, steps[8].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(1f);

            // Step 9: "Two times three equals six"
            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[9].subtitle, steps[9].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Step 10: "Move your piece to the six"
            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[10].subtitle, steps[10].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Wait for correct answer: circle index 2 ("6")
            yield return CoWaitForAnswer(2);

            // Correct: destroy circles, handwrite "6" on fracRightNum
            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracRightNum.rectTransform.parent as RectTransform,
                fracRightNum, "6", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        // "So now we know that two thirds is equal to six ninths"
        //  So(0) now(1) we(2) know(3) that(4) two(5) thirds(6) is(7) equal(8) to(9) six(10) ninths(11)
        private IEnumerator RunSummaryStep()
        {
            var step = steps[12];
            bool done = false;

            StartCoroutine(sequencer.CoShowKaraokeSubtitle(
                step.subtitle, 3f,
                (idx, word, duration) =>
                {
                    // "two thirds" → pulse fracLeft at word 5
                    if (idx == 5)
                        StartCoroutine(CoPulseScale(fracLeft, 1.15f, duration));
                    // "six ninths" → pulse fracRight at word 10
                    else if (idx == 10)
                        StartCoroutine(CoPulseScale(fracRight, 1.15f, duration));
                },
                () => done = true));

            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.5f);
        }

        // ── Answer Circle System ──────────────────────────────────

        private IEnumerator CoShowAnswerCircles(string[] values)
        {
            answerCircles = new GameObject[values.Length];
            answerCircleRects = new RectTransform[values.Length];
            answerDwellTimes = new float[values.Length];

            float totalWidth = (values.Length - 1) * CircleSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < values.Length; i++)
            {
                var go = new GameObject("AnswerCircle_" + values[i]);
                go.transform.SetParent(contentArea, false);
                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(CircleSize, CircleSize);
                rect.anchoredPosition = new Vector2(startX + i * CircleSpacing, CircleY);

                var img = go.AddComponent<Image>();
                img.sprite = NavigationHelper.EnsureCircleSprite();
                img.color = CircleDefaultColor;

                // Child text
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(go.transform, false);
                var textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = values[i];
                tmp.fontSize = 48f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.1f, 0.1f, 0.15f, 1f);
                tmp.raycastTarget = false;

                // Start at scale 0 for pop-in
                go.transform.localScale = Vector3.zero;

                answerCircles[i] = go;
                answerCircleRects[i] = rect;
                answerDwellTimes[i] = -1f;
            }

            // Staggered pop-in animation
            for (int i = 0; i < values.Length; i++)
            {
                StartCoroutine(CoPopIn(answerCircles[i].transform, 0.3f));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.3f); // wait for last one to finish
        }

        private IEnumerator CoPopIn(Transform target, float duration)
        {
            var cg = target.GetComponent<Image>();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = EaseOutBack(t);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private IEnumerator CoWaitForAnswer(int correctIndex)
        {
            bool answered = false;

            while (!answered)
            {
                if (PieceManager.Instance == null) { yield return null; continue; }

                // Get first active piece
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
                    // Reset all dwells
                    for (int i = 0; i < answerDwellTimes.Length; i++)
                    {
                        answerDwellTimes[i] = -1f;
                        ResetCircleColor(i);
                    }
                    yield return null;
                    continue;
                }

                int hoveredIndex = -1;
                for (int i = 0; i < answerCircleRects.Length; i++)
                {
                    if (answerCircleRects[i] != null &&
                        NavigationHelper.IsOverRect(answerCircleRects[i], screenPos, 30f))
                    {
                        hoveredIndex = i;
                        break;
                    }
                }

                if (hoveredIndex < 0)
                {
                    for (int i = 0; i < answerDwellTimes.Length; i++)
                    {
                        answerDwellTimes[i] = -1f;
                        ResetCircleColor(i);
                    }
                    yield return null;
                    continue;
                }

                // Reset non-hovered circles
                for (int i = 0; i < answerDwellTimes.Length; i++)
                {
                    if (i != hoveredIndex)
                    {
                        answerDwellTimes[i] = -1f;
                        ResetCircleColor(i);
                    }
                }

                // Track dwell on hovered circle
                if (answerDwellTimes[hoveredIndex] < 0f)
                    answerDwellTimes[hoveredIndex] = 0f;
                answerDwellTimes[hoveredIndex] += Time.deltaTime;

                // Visual feedback: lerp toward white
                var img = answerCircles[hoveredIndex].GetComponent<Image>();
                if (img != null)
                    img.color = Color.Lerp(img.color, Color.white, Time.deltaTime * 3f);

                if (answerDwellTimes[hoveredIndex] >= DWELL_TIME)
                {
                    if (hoveredIndex == correctIndex)
                    {
                        // Correct: play chime + green flash
                        BoardOfEducation.Audio.GameAudioManager.PlayCorrectSFX();
                        yield return CoFlashCircle(hoveredIndex, CircleCorrectColor, 0.3f);
                        answered = true;
                    }
                    else
                    {
                        // Wrong: red flash, reset dwell
                        yield return CoFlashCircle(hoveredIndex, CircleWrongColor, 0.5f);
                        answerDwellTimes[hoveredIndex] = -1f;
                        ResetCircleColor(hoveredIndex);
                    }
                }

                yield return null;
            }
        }

        private void ResetCircleColor(int index)
        {
            if (index < 0 || index >= answerCircles.Length) return;
            var img = answerCircles[index]?.GetComponent<Image>();
            if (img != null)
                img.color = Color.Lerp(img.color, CircleDefaultColor, Time.deltaTime * 5f);
        }

        private IEnumerator CoFlashCircle(int index, Color flashColor, float duration)
        {
            var img = answerCircles[index].GetComponent<Image>();
            if (img == null) yield break;

            img.color = flashColor;
            yield return new WaitForSeconds(duration);
        }

        private void DestroyAnswerCircles()
        {
            if (answerCircles == null) return;
            for (int i = 0; i < answerCircles.Length; i++)
            {
                if (answerCircles[i] != null)
                    Destroy(answerCircles[i]);
            }
            answerCircles = null;
            answerCircleRects = null;
            answerDwellTimes = null;
        }

        // ── Animations ────────────────────────────────────────────

        private IEnumerator CoAnimateShowEquation(Action onComplete)
        {
            float duration = 0.4f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                equationRowGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            equationRowGroup.alpha = 1f;
            onComplete?.Invoke();
        }

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

        // ── Handwriting Effect ────────────────────────────────────

        private IEnumerator CoHandwriteDigit(RectTransform parent, TextMeshProUGUI existingText,
            string digit, float duration, Action onComplete)
        {
            existingText.text = digit;
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

        // ── Shared Animation Coroutines ───────────────────────────

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

        // ── Helpers ───────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
