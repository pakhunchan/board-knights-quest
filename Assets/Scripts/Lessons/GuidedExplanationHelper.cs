using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using BoardOfEducation.Input;
using BoardOfEducation.Navigation;
using BoardOfEducation.UI;

namespace BoardOfEducation.Lessons
{
    /// <summary>
    /// Parameterized guided explanation flow for fraction equivalence problems.
    /// Extracted from FractionsDemo3Manager so it can be reused from any scene.
    /// This is a plain C# class (not MonoBehaviour) — it uses a host for coroutines.
    /// </summary>
    public class GuidedExplanationHelper
    {
        private readonly MonoBehaviour host;
        private readonly RectTransform contentArea;
        private readonly LessonSequencer sequencer;

        // ── Equation elements ──
        private RectTransform equationRow;
        private CanvasGroup equationRowGroup;
        private RectTransform fracLeft, opEquals, fracRight, opMultiply, fracMiddle;
        private TextMeshProUGUI fracLeftNum, fracLeftDen, fracRightNum, fracRightDen;
        private TextMeshProUGUI fracMiddleNum, fracMiddleDen;
        private CanvasGroup fracMiddleGroup, opMultiplyGroup;
        private CanvasGroup fracLeftTopGroup, fracMiddleTopGroup, fracRightTopGroup;
        private Vector3 savedEquationScale;
        private float savedEquationY, savedOpEqualsY, savedOpMultiplyY;

        // ── Answer circles ──
        private GameObject[] answerCircles;
        private RectTransform[] answerCircleRects;
        private float[] answerDwellTimes;

        // ── Layout constants (matching Demo3) ──
        private const float FractionWidth = 180f;
        private const float OperatorWidth = 90f;
        private const float ElementGap = 30f;
        private const float SlideOffset = 150f;
        private const float BarHeight = 5f;
        private const float FractionHeight = 210f;
        private const float FontSize = 72f;
        private const float ZoomScale = 1.5f;
        private const float ZoomDuration = 0.8f;

        private const float CircleSize = 156f;
        private const float CircleSpacing = 320f;
        private const float CircleY = -240f;
        private const float DWELL_TIME = 1.0f;

        private static readonly Color HandwriteColor = new Color(1f, 0.75f, 0.3f);
        private static readonly Color CircleDefaultColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color CircleCorrectColor = new Color(0.18f, 0.8f, 0.44f, 1f);
        private static readonly Color CircleWrongColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        public GuidedExplanationHelper(MonoBehaviour host, RectTransform contentArea, LessonSequencer sequencer)
        {
            this.host = host;
            this.contentArea = contentArea;
            this.sequencer = sequencer;
        }

        // ── Public Entry Point ──────────────────────────────────────

        /// <summary>
        /// Runs the full guided explanation for a fraction equivalence problem.
        /// Mirrors FractionsDemo3Manager's interactive flow but parameterized.
        /// </summary>
        public IEnumerator CoRunExplanation(PracticeQuestion q)
        {
            int leftNum = int.Parse(q.leftNum);
            int leftDen = int.Parse(q.leftDen);
            int rightDen = int.Parse(q.rightDen);
            int multiplier = rightDen / leftDen;
            int finalAnswer = leftNum * multiplier;

            string leftNumWord = NumberWords.ToCardinal(leftNum);
            string leftDenPlural = NumberWords.ToDenominatorPlural(leftDen);
            string rightDenPlural = NumberWords.ToDenominatorPlural(rightDen);
            string multiplierWord = NumberWords.ToCardinal(multiplier);
            string finalAnswerWord = NumberWords.ToCardinal(finalAnswer);

            // Step 1: Build the expanded equation
            BuildEquation(q.leftNum, q.leftDen, q.rightDen);

            // Step 2: Fade in equation
            bool animDone = false;
            host.StartCoroutine(CoAnimateShowEquation(() => animDone = true));
            yield return new WaitUntil(() => animDone);

            // Step 3: Subtitle — "one half is equal to how many sixths?"
            string introSub = $"{leftNumWord} {leftDenPlural} is equal to how many {rightDenPlural}?";
            // Fix grammar: "one halves" → "one half", etc.
            introSub = FixSingularDenominator(introSub, leftNum, leftDen);
            bool subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(introSub,
                new LessonStep(introSub).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(0.5f);

            // Step 4: Slide apart
            animDone = false;
            host.StartCoroutine(CoAnimateSlideApart(() => animDone = true));
            yield return new WaitUntil(() => animDone);

            // Step 5: Subtitle about multiplying
            string multiplySub = $"We have to multiply {leftNumWord} {leftDenPlural} by something to figure out how many {rightDenPlural} it's equal to";
            multiplySub = FixSingularDenominator(multiplySub, leftNum, leftDen);
            subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(multiplySub,
                new LessonStep(multiplySub).EstimatedDuration, () => subDone = true));

            // Step 6: Fade in multiply operator + middle fraction (concurrent with subtitle)
            animDone = false;
            host.StartCoroutine(CoAnimateFadeInMultiply(() => animDone = true));
            yield return new WaitUntil(() => subDone && animDone);
            yield return new WaitForSeconds(0.5f);

            // ── Denominator interaction ──
            yield return RunDenominatorInteraction(leftDen, rightDen, multiplier, multiplierWord);

            // ── Numerator interaction ──
            yield return RunNumeratorInteraction(multiplier, multiplierWord);

            // ── Final product interaction ──
            yield return RunFinalProductInteraction(leftNum, multiplier, finalAnswer, leftNumWord, multiplierWord, finalAnswerWord);

            // Summary
            string summarySub = $"Good job! {leftNumWord} {leftDenPlural} is equal to {finalAnswerWord} {rightDenPlural}";
            summarySub = FixSingularDenominator(summarySub, leftNum, leftDen);
            subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(summarySub,
                new LessonStep(summarySub).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(0.5f);

            // Fade out and clean up
            yield return CoAnimateFadeOutEquation();
            ClearCreatedChildren();
        }

        // ── Interactive Sections ────────────────────────────────────

        private IEnumerator RunDenominatorInteraction(int leftDen, int rightDen, int multiplier, string multiplierWord)
        {
            // Zoom into denominators
            bool zoomDone = false;
            host.StartCoroutine(CoAnimateZoomIntoDenominators(() => zoomDone = true));
            yield return new WaitUntil(() => zoomDone);

            // Show answer circles
            string[] choices = GenerateChoices(multiplier);
            int correctIdx = FindCorrectIndex(choices, multiplier);
            yield return CoShowAnswerCircles(choices);

            // Subtitle: "three times what equals nine?"
            string denWord = NumberWords.ToCardinal(leftDen);
            string rightDenWord = NumberWords.ToCardinal(rightDen);
            string promptSub = $"{denWord} times what equals {rightDenWord}?";
            bool subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(promptSub,
                new LessonStep(promptSub).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(1f);

            // Subtitle: "three times three is equal to nine, so move your piece to the three"
            string hintSub = $"{denWord} times {multiplierWord} is equal to {rightDenWord}, so move your piece to the {multiplierWord}";
            subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(hintSub,
                new LessonStep(hintSub).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Wait for correct answer
            yield return CoWaitForAnswer(correctIdx);

            // Handwrite answer
            DestroyAnswerCircles();
            bool writeDone = false;
            host.StartCoroutine(CoHandwriteDigit(
                fracMiddleDen.rectTransform.parent as RectTransform,
                fracMiddleDen, multiplier.ToString(), 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunNumeratorInteraction(int multiplier, string multiplierWord)
        {
            // Zoom back out
            bool zoomDone = false;
            host.StartCoroutine(CoAnimateZoomOutToFull(() => zoomDone = true));
            yield return new WaitUntil(() => zoomDone);

            // Show answer circles
            string[] choices = GenerateChoices(multiplier);
            int correctIdx = FindCorrectIndex(choices, multiplier);
            yield return CoShowAnswerCircles(choices);

            // Subtitle
            string sub = $"Whatever value we used for the bottom, we have to use for the top, so put a {multiplierWord} on the top as well";
            bool subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(sub,
                new LessonStep(sub).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            yield return CoWaitForAnswer(correctIdx);

            DestroyAnswerCircles();
            bool writeDone = false;
            host.StartCoroutine(CoHandwriteDigit(
                fracMiddleNum.rectTransform.parent as RectTransform,
                fracMiddleNum, multiplier.ToString(), 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunFinalProductInteraction(int leftNum, int multiplier, int finalAnswer,
            string leftNumWord, string multiplierWord, string finalAnswerWord)
        {
            string[] choices = GenerateChoices(finalAnswer);
            int correctIdx = FindCorrectIndex(choices, finalAnswer);
            yield return CoShowAnswerCircles(choices);

            // "Let's now multiply the top values"
            string sub1 = "Let's now multiply the top values";
            bool subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(sub1,
                new LessonStep(sub1).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // "two times three equals what?"
            string sub2 = $"{leftNumWord} times {multiplierWord} equals what?";
            subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(sub2,
                new LessonStep(sub2).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(1f);

            // "two times three equals six"
            string sub3 = $"{leftNumWord} times {multiplierWord} equals {finalAnswerWord}";
            subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(sub3,
                new LessonStep(sub3).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // "Move your piece to the six"
            string sub4 = $"Move your piece to the {finalAnswerWord}";
            subDone = false;
            host.StartCoroutine(sequencer.CoShowSubtitle(sub4,
                new LessonStep(sub4).EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            yield return CoWaitForAnswer(correctIdx);

            DestroyAnswerCircles();
            bool writeDone = false;
            host.StartCoroutine(CoHandwriteDigit(
                fracRightNum.rectTransform.parent as RectTransform,
                fracRightNum, finalAnswer.ToString(), 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        // ── Choice Generation ───────────────────────────────────────

        private string[] GenerateChoices(int correctValue)
        {
            string[] choices = new string[3];
            int idx = UnityEngine.Random.Range(0, 3);
            choices[idx] = correctValue.ToString();

            // Distractors at ±1, ensuring > 0
            int d1 = correctValue - 1;
            int d2 = correctValue + 1;
            if (d1 < 1) { d1 = correctValue + 2; }

            int filled = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i == idx) continue;
                choices[i] = (filled == 0 ? d1 : d2).ToString();
                filled++;
            }

            return choices;
        }

        private int FindCorrectIndex(string[] choices, int correctValue)
        {
            string val = correctValue.ToString();
            for (int i = 0; i < choices.Length; i++)
                if (choices[i] == val) return i;
            return 0;
        }

        // ── Grammar Fix ─────────────────────────────────────────────

        /// <summary>
        /// When leftNum is 1, the denominator should be singular: "one half" not "one halves".
        /// </summary>
        private string FixSingularDenominator(string text, int leftNum, int leftDen)
        {
            if (leftNum != 1) return text;
            string plural = NumberWords.ToDenominatorPlural(leftDen);
            string singular = ToSingularDenominator(leftDen);
            return text.Replace(leftNum == 1 ? $"one {plural}" : "", $"one {singular}")
                       .Replace($"one {plural}", $"one {singular}");
        }

        private string ToSingularDenominator(int n)
        {
            if (n == 2) return "half";
            // Remove trailing 's' from plural
            string plural = NumberWords.ToDenominatorPlural(n);
            if (plural.EndsWith("s"))
                return plural.Substring(0, plural.Length - 1);
            return plural;
        }

        // ── Equation Building ───────────────────────────────────────

        private void BuildEquation(string leftNum, string leftDen, string rightDen)
        {
            var rowGo = new GameObject("EquationRow");
            rowGo.transform.SetParent(contentArea, false);
            equationRow = rowGo.AddComponent<RectTransform>();
            equationRow.anchorMin = Vector2.zero;
            equationRow.anchorMax = Vector2.one;
            equationRow.offsetMin = Vector2.zero;
            equationRow.offsetMax = Vector2.zero;

            equationRowGroup = rowGo.AddComponent<CanvasGroup>();
            equationRowGroup.alpha = 0f;

            float totalWidth = FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth;
            float startX = -totalWidth / 2f;

            fracLeft = CreateFraction(equationRow, leftNum, leftDen, out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", rightDen, out fracRightNum, out fracRightDen, out fracRightTopGroup);
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

        // ── Answer Circle System ────────────────────────────────────

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

                go.transform.localScale = Vector3.zero;

                answerCircles[i] = go;
                answerCircleRects[i] = rect;
                answerDwellTimes[i] = -1f;
            }

            for (int i = 0; i < values.Length; i++)
            {
                host.StartCoroutine(CoPopIn(answerCircles[i].transform, 0.3f));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator CoWaitForAnswer(int correctIndex)
        {
            bool answered = false;

            while (!answered)
            {
                if (PieceManager.Instance == null) { yield return null; continue; }

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

                for (int i = 0; i < answerDwellTimes.Length; i++)
                {
                    if (i != hoveredIndex)
                    {
                        answerDwellTimes[i] = -1f;
                        ResetCircleColor(i);
                    }
                }

                if (answerDwellTimes[hoveredIndex] < 0f)
                    answerDwellTimes[hoveredIndex] = 0f;
                answerDwellTimes[hoveredIndex] += Time.deltaTime;

                var img = answerCircles[hoveredIndex].GetComponent<Image>();
                if (img != null)
                    img.color = Color.Lerp(img.color, Color.white, Time.deltaTime * 3f);

                if (answerDwellTimes[hoveredIndex] >= DWELL_TIME)
                {
                    if (hoveredIndex == correctIndex)
                    {
                        yield return CoFlashCircle(hoveredIndex, CircleCorrectColor, 0.3f);
                        answered = true;
                    }
                    else
                    {
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
                    UnityEngine.Object.Destroy(answerCircles[i]);
            }
            answerCircles = null;
            answerCircleRects = null;
            answerDwellTimes = null;
        }

        // ── Animations ──────────────────────────────────────────────

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
            host.StartCoroutine(CoFadeIn(opMultiplyGroup, 0.4f, () => a = true));
            host.StartCoroutine(CoFadeIn(fracMiddleGroup, 0.4f, () => b = true));

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
            host.StartCoroutine(CoFadeOut(fracLeftTopGroup, ZoomDuration * 0.6f, () => fadeA = true));
            host.StartCoroutine(CoFadeOut(fracMiddleTopGroup, ZoomDuration * 0.6f, () => fadeB = true));
            host.StartCoroutine(CoFadeOut(fracRightTopGroup, ZoomDuration * 0.6f, () => fadeC = true));

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
            host.StartCoroutine(CoFadeIn(fracLeftTopGroup, ZoomDuration * 0.6f, () => fadeA = true));
            host.StartCoroutine(CoFadeIn(fracMiddleTopGroup, ZoomDuration * 0.6f, () => fadeB = true));
            host.StartCoroutine(CoFadeIn(fracRightTopGroup, ZoomDuration * 0.6f, () => fadeC = true));

            yield return new WaitUntil(() => fadeA && fadeB && fadeC);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateFadeOutEquation()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                equationRowGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            equationRowGroup.alpha = 0f;
        }

        // ── Handwriting Effect ──────────────────────────────────────

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
            UnityEngine.Object.Destroy(maskGo);

            onComplete?.Invoke();
        }

        // ── Utility Animations ──────────────────────────────────────

        private IEnumerator CoPopIn(Transform target, float duration)
        {
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

        private void ClearCreatedChildren()
        {
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(contentArea.GetChild(i).gameObject);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
