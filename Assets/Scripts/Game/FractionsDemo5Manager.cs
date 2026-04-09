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
    /// 10-question fraction equivalence practice session.
    /// Loops through questions with an intro card and outro message.
    /// </summary>
    public class FractionsDemo5Manager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private RectTransform scoreArea;
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

        // ── Score tracker ──
        private GameObject scoreBarGo;
        private CanvasGroup scoreBarGroup;
        private TextMeshProUGUI scoreCountText;
        private Image[] scorePills;
        private int currentQuestionIndex;
        private bool currentQuestionHadWrong;

        // ── Question Data ────────────────────────────────────────

        private readonly PracticeQuestion[] questions = new PracticeQuestion[]
        {
            new PracticeQuestion { leftNum = "1", leftDen = "2", rightDen = "6",
                choices = new[] { "3", "4", "5" }, correctIndex = 0,
                subtitle = "One half is equal to how many sixths?" },
            new PracticeQuestion { leftNum = "3", leftDen = "5", rightDen = "10",
                choices = new[] { "4", "5", "6" }, correctIndex = 2,
                subtitle = "three fifths is equal to how many tenths?" },
            new PracticeQuestion { leftNum = "2", leftDen = "3", rightDen = "6",
                choices = new[] { "2", "3", "4" }, correctIndex = 2,
                subtitle = "two thirds is equal to how many sixths?" },
            new PracticeQuestion { leftNum = "4", leftDen = "5", rightDen = "10",
                choices = new[] { "6", "7", "8" }, correctIndex = 2,
                subtitle = "four fifths is equal to how many tenths?" },
            new PracticeQuestion { leftNum = "3", leftDen = "6", rightDen = "18",
                choices = new[] { "9", "12", "15" }, correctIndex = 0,
                subtitle = "three sixths is equal to how many eighteenths?" },
            new PracticeQuestion { leftNum = "4", leftDen = "7", rightDen = "35",
                choices = new[] { "16", "18", "20" }, correctIndex = 2,
                subtitle = "four sevenths is equal to how many thirty-fifths?" },
            new PracticeQuestion { leftNum = "2", leftDen = "9", rightDen = "45",
                choices = new[] { "10", "12", "14" }, correctIndex = 0,
                subtitle = "two ninths is equal to how many forty-fifths?" },
            new PracticeQuestion { leftNum = "5", leftDen = "8", rightDen = "64",
                choices = new[] { "30", "40", "50" }, correctIndex = 1,
                subtitle = "five eighths is equal to how many sixty fourths?" },
            new PracticeQuestion { leftNum = "3", leftDen = "7", rightDen = "49",
                choices = new[] { "18", "21", "24" }, correctIndex = 1,
                subtitle = "three sevenths is equal to how many forty ninths?" },
            new PracticeQuestion { leftNum = "3", leftDen = "8", rightDen = "48",
                choices = new[] { "18", "21", "24" }, correctIndex = 0,
                subtitle = "three eighths is equal to how many forty eighths?" },
        };

        // ── Outro Step ──
        private readonly LessonStep outroStep = new LessonStep("Amazing! Great work today. You answered all ten questions!");

        // ── Animation Registry ────────────────────────────────────

        private Dictionary<string, Func<Action, IEnumerator>> animationRegistry;

        private void BuildAnimationRegistry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["showEquation"] = CoAnimateShowEquation,
            };
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

        // ── Score Tracker Constants ──────────────────────────────
        private static readonly Color ScorePillDefault = new Color(0.75f, 0.75f, 0.78f, 0.6f);
        private static readonly Color ScorePillCorrect = new Color(0.18f, 0.78f, 0.35f, 1f);
        private static readonly Color ScorePillWrong = new Color(0.9f, 0.25f, 0.2f, 1f);
        private const float PillWidth = 22f;
        private const float PillHeight = 42f;
        private const float PillGap = 6f;

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

        // ── Equation Building (parameterized) ────────────────────

        private void BuildEquation(PracticeQuestion q)
        {
            // Create equationRow as a child of contentArea
            var rowGo = new GameObject("EquationRow");
            rowGo.transform.SetParent(contentArea, false);
            equationRow = rowGo.AddComponent<RectTransform>();
            equationRow.anchorMin = Vector2.zero;
            equationRow.anchorMax = Vector2.one;
            equationRow.offsetMin = Vector2.zero;
            equationRow.offsetMax = Vector2.zero;

            // Add CanvasGroup for fade-in/out
            equationRowGroup = rowGo.AddComponent<CanvasGroup>();
            equationRowGroup.alpha = 0f;

            float totalWidth = FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth;
            float startX = -totalWidth / 2f;

            fracLeft = CreateFraction(equationRow, q.leftNum, q.leftDen, out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", q.rightDen, out fracRightNum, out fracRightDen, out fracRightTopGroup);
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
            sequencer.Begin();
            yield return new WaitForSeconds(0.3f);

            // ── INTRO ──
            yield return CoShowIntroCard("Practice Problems");
            yield return new WaitForSeconds(0.5f);

            // ── BUILD SCORE BAR ──
            BuildScoreBar();
            yield return CoFadeScoreBar(0f, 1f, 0.3f);

            // ── QUESTIONS 1–10 ──
            for (int i = 0; i < questions.Length; i++)
            {
                var q = questions[i];
                currentQuestionIndex = i;
                currentQuestionHadWrong = false;
                UpdateScoreCount(i + 1);

                BuildEquation(q);
                BuildAnimationRegistry();

                // Fade in equation
                bool animDone = false;
                StartCoroutine(CoAnimateShowEquation(() => animDone = true));
                yield return new WaitUntil(() => animDone);

                // Show answer circles
                yield return CoShowAnswerCircles(q.choices);

                // Show karaoke subtitle (concurrent with waiting for answer)
                bool subDone = false;
                StartCoroutine(sequencer.CoShowSubtitle(q.subtitle,
                    new LessonStep(q.subtitle).EstimatedDuration, () => subDone = true));

                // Wait for correct answer
                yield return CoWaitForAnswer(q.correctIndex);

                // Update score pill (red if any wrong attempt, green if first-try correct)
                UpdateScorePill(i, !currentQuestionHadWrong);

                // Correct → destroy circles → stroke-reveal answer
                DestroyAnswerCircles();
                bool writeDone = false;
                StartCoroutine(CoHandwriteDigit(
                    fracRightNum.rectTransform.parent as RectTransform,
                    fracRightNum, q.CorrectAnswer, 0.6f, () => writeDone = true));
                yield return new WaitUntil(() => writeDone);
                yield return new WaitForSeconds(0.8f);

                // Fade out equation before next question
                yield return CoAnimateFadeOutEquation();
                ClearContentArea();
                yield return new WaitForSeconds(0.3f);
            }

            // ── OUTRO ──
            yield return CoFadeScoreBar(1f, 0f, 0.3f);
            if (scoreBarGo != null) Destroy(scoreBarGo);
            yield return sequencer.RunStep(outroStep, null);

            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ── Score Tracker ────────────────────────────────────────

        private void BuildScoreBar()
        {
            // Container parented to scoreArea (outside contentArea so ClearContentArea won't destroy it)
            scoreBarGo = new GameObject("ScoreBar");
            scoreBarGo.transform.SetParent(scoreArea, false);
            var barRect = scoreBarGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);

            // Background panel
            var bgImg = scoreBarGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.14f, 0.25f, 0.85f);
            bgImg.raycastTarget = false;

            // Layout: "X of 10" text | divider | 10 pills
            float textWidth = 100f;
            float dividerWidth = 2f;
            float dividerGap = 12f;
            float pillsWidth = questions.Length * PillWidth + (questions.Length - 1) * PillGap;
            float totalWidth = 20f + textWidth + dividerGap + dividerWidth + dividerGap + pillsWidth + 20f;
            float totalHeight = 56f;
            barRect.sizeDelta = new Vector2(totalWidth, totalHeight);

            // Counter text: "1 of 10"
            var textGo = new GameObject("CountText");
            textGo.transform.SetParent(scoreBarGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(0, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.anchoredPosition = new Vector2(20f + textWidth / 2f, 0);
            textRect.sizeDelta = new Vector2(textWidth, 0);
            scoreCountText = textGo.AddComponent<TextMeshProUGUI>();
            scoreCountText.text = "1 of 10";
            scoreCountText.fontSize = 24;
            scoreCountText.alignment = TextAlignmentOptions.Center;
            scoreCountText.color = new Color(1, 1, 1, 0.9f);
            scoreCountText.raycastTarget = false;

            // Divider line
            float dividerX = 20f + textWidth + dividerGap;
            var divGo = new GameObject("Divider");
            divGo.transform.SetParent(scoreBarGo.transform, false);
            var divRect = divGo.AddComponent<RectTransform>();
            divRect.anchorMin = new Vector2(0, 0.15f);
            divRect.anchorMax = new Vector2(0, 0.85f);
            divRect.offsetMin = Vector2.zero;
            divRect.offsetMax = Vector2.zero;
            divRect.anchoredPosition = new Vector2(dividerX + dividerWidth / 2f, 0);
            divRect.sizeDelta = new Vector2(dividerWidth, 0);
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(1, 1, 1, 0.3f);
            divImg.raycastTarget = false;

            // Pills
            float pillsStartX = dividerX + dividerWidth + dividerGap;
            scorePills = new Image[questions.Length];
            for (int i = 0; i < questions.Length; i++)
            {
                var pillGo = new GameObject("Pill_" + i);
                pillGo.transform.SetParent(scoreBarGo.transform, false);
                var pillRect = pillGo.AddComponent<RectTransform>();
                pillRect.anchorMin = new Vector2(0, 0.5f);
                pillRect.anchorMax = new Vector2(0, 0.5f);
                pillRect.pivot = new Vector2(0.5f, 0.5f);
                pillRect.sizeDelta = new Vector2(PillWidth, PillHeight);
                pillRect.anchoredPosition = new Vector2(
                    pillsStartX + i * (PillWidth + PillGap) + PillWidth / 2f, 0);

                var pillImg = pillGo.AddComponent<Image>();
                pillImg.sprite = NavigationHelper.EnsureCircleSprite();
                pillImg.color = ScorePillDefault;
                pillImg.raycastTarget = false;

                scorePills[i] = pillImg;
            }

            // Start hidden, fade in after intro
            scoreBarGroup = scoreBarGo.AddComponent<CanvasGroup>();
            scoreBarGroup.alpha = 0f;
        }

        private void UpdateScorePill(int index, bool correct)
        {
            if (index < 0 || index >= scorePills.Length) return;
            scorePills[index].color = correct ? ScorePillCorrect : ScorePillWrong;
        }

        private void UpdateScoreCount(int questionNum)
        {
            if (scoreCountText != null)
                scoreCountText.text = $"{questionNum} of {questions.Length}";
        }

        private IEnumerator CoFadeScoreBar(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                scoreBarGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            scoreBarGroup.alpha = to;
        }

        // ── Intro Card ──────────────────────────────────────────

        private IEnumerator CoShowIntroCard(string text)
        {
            var go = new GameObject("IntroCard");
            go.transform.SetParent(contentArea, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 96;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1, 1, 1, 0);

            // Fade in
            float elapsed = 0f;
            float fadeDuration = 0.4f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                tmp.color = new Color(1, 1, 1, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            tmp.color = Color.white;

            // Hold
            yield return new WaitForSeconds(1.5f);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                tmp.color = new Color(1, 1, 1, 1f - Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            Destroy(go);
        }

        // ── Clear Content Area ──────────────────────────────────

        private void ClearContentArea()
        {
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);
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
                        currentQuestionHadWrong = true;
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
