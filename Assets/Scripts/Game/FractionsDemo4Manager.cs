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
    /// Simple interactive fractions lesson: 3/5 = ?/20
    /// Single-interaction format: guided subtitles lead to one answer input.
    /// </summary>
    public class FractionsDemo4Manager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;
        [SerializeField] private LessonSequencer sequencer;

        // ── Equation elements — set by BuildEquation() ──
        private RectTransform equationRow;
        private CanvasGroup equationRowGroup;
        private RectTransform fracLeft, opEquals, fracRight;
        private TextMeshProUGUI fracLeftNum, fracLeftDen, fracRightNum, fracRightDen;
        private CanvasGroup fracLeftTopGroup, fracRightTopGroup;

        // ── Answer circle tracking ──
        private GameObject[] answerCircles;
        private RectTransform[] answerCircleRects;
        private float[] answerDwellTimes;

        // ── Step Definitions ──────────────────────────────────────

        private readonly LessonStep[] steps = new LessonStep[]
        {
            new LessonStep("Let's try another example",                                                    "showEquation"), // 0
            new LessonStep("Three fifths is equal to how many twentieths?"),                                                 // 1
            new LessonStep("First, figure out what you need to multiply five by to get to twenty"),                           // 2
            new LessonStep("Multiply the bottom to get to 20"),                                                              // 3
            new LessonStep("How many twentieths is three fifths?"),                                                          // 4
        };

        // ── Animation Registry ────────────────────────────────────

        private Dictionary<string, Func<Action, IEnumerator>> animationRegistry;

        private void BuildAnimationRegistry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["showEquation"] = CoAnimateShowEquation,
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
        private const float BarHeight = 5f;
        private const float FractionHeight = 210f;
        private const float FontSize = 72f;

        // ── Answer Circle Constants ────────────────────────────────
        private const float CircleSize = 156f;
        private const float CircleSpacing = 320f;
        private const float CircleY = -240f;
        private const float DWELL_TIME = 1.0f;

        private static readonly Color HandwriteColor = new Color(0.18f, 0.8f, 0.44f);
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

            // Create equationRow as a child of contentArea
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

            fracLeft = CreateFraction(equationRow, "3", "5", out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", "20", out fracRightNum, out fracRightDen, out fracRightTopGroup);
            fracRight.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth / 2f, 0);
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

            // Steps 0–3: Standard karaoke subtitles
            var anim0 = ResolveAnimation(steps[0].animationKey);
            yield return sequencer.RunStep(steps[0], anim0);  // "Let's try another example" + fade in equation
            yield return new WaitForSeconds(1f);

            yield return sequencer.RunStep(steps[1], null);   // "Three fifths is equal to how many twentieths?"
            yield return new WaitForSeconds(1f);

            yield return sequencer.RunStep(steps[2], null);   // "First, figure out what..."
            yield return new WaitForSeconds(1f);

            yield return sequencer.RunStep(steps[3], null);   // "Multiply the bottom to get to 20"
            yield return new WaitForSeconds(0.5f);

            // Interactive section
            yield return RunAnswerInteraction();               // step 4

            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ── Interactive Section ────────────────────────────────────

        private IEnumerator RunAnswerInteraction()
        {
            // Show answer circles (10, 12, 14)
            yield return CoShowAnswerCircles(new string[] { "10", "12", "14" });

            // Step 4: "How many twentieths is three fifths?"
            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[4].subtitle, steps[4].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            // Wait for correct answer: circle index 1 ("12")
            yield return CoWaitForAnswer(1);

            // Correct: destroy circles, handwrite "12" on fracRightNum
            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracRightNum.rectTransform.parent as RectTransform,
                fracRightNum, "12", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
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
            yield return new WaitForSeconds(0.3f);
        }

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
                        // Correct: green flash
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

        // ── Helpers ───────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
