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
    /// MVP1: Full fractions lesson (Phases 1-4 from TotalFractions2) followed by
    /// Phase 5 practice (10 interactive questions from FractionsDemo5) in one continuous session.
    /// </summary>
    public class MVP1Manager : MonoBehaviour
    {
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private RectTransform scoreArea;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject playButtonGo;
        [SerializeField] private LessonSequencer sequencer;

        // ── Phase 1 (circles) — set by BuildCircleVisuals(), nulled after transition ──
        private FractionCircle leftCircle, rightCircle;
        private CanvasGroup leftCircleGroup, rightCircleGroup;
        private CanvasGroup leftLabelGroup, rightLabelGroup;

        // ── Equation (shared Phases 2–5) — rebuilt each transition ──
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

        // ── Score tracker (Phase 5) ──
        private GameObject scoreBarGo;
        private CanvasGroup scoreBarGroup;
        private TextMeshProUGUI scoreCountText;
        private Image[] scorePills;
        private int currentQuestionIndex;
        private bool currentQuestionHadWrong;

        // ── Step Definitions (42 steps, Phases 1-4) ──────────────────────────────────

        private readonly LessonStep[] steps = new LessonStep[]
        {
            // Phase 1: Circles (steps 0–11)
            new LessonStep("This is a circle.",                                     "showCircle:left"),    // 0
            new LessonStep("If we draw a line down the middle and",                 "lines:left"),         // 1
            new LessonStep("shade the left side",                                   "shade:left"),         // 2
            new LessonStep("we get one-half",                                       "label:left"),         // 3
            new LessonStep("This is a circle.",                                     "showCircle:right"),   // 4
            new LessonStep("If we draw lines to split the circle into 6 pieces",    "lines:right"),        // 5
            new LessonStep("and shade the left side",                               "shade:right"),        // 6
            new LessonStep("we get three-sixths",                                   "label:right"),        // 7
            new LessonStep("You can see that these fractions are equal"),                                   // 8
            new LessonStep("But how do we go from one fraction to another fraction?"),                      // 9
            new LessonStep("We will learn that today"),                                                     // 10
            new LessonStep("Let's jump into an example"),                                                   // 11

            // Transition 1→2 (step 12)
            new LessonStep(" ",                                                     "transition1to2"),     // 12

            // Phase 2: Equation 1/2 = ?/6 (steps 13–21)
            new LessonStep("One half equals how many sixths?"),                                                                                                // 13
            new LessonStep("Let's shift this over to the right to make space",                                                              "slideApart"),     // 14
            new LessonStep("We need to multiply one half by something to turn it into something over six.",                                 "fadeInMultiply"), // 15
            new LessonStep("Focus first on the bottom numbers, the denominators.",                                                          "zoomInDenom"),    // 16
            new LessonStep("Two times three equals six.",                                                                                    "swapDenom3"),     // 17
            new LessonStep("Coming back to the full equation, the rule is whatever you multiply the bottom by, you have to multiply the top by the same value.", "zoomOut"), // 18
            new LessonStep("Since we multiplied the bottom by three, we have to multiply the top by three."),                                                   // 19
            new LessonStep("Multiplying the top is one times three, which is equal to three."),                                                                 // 20
            new LessonStep("So now we know that one-half is equal to three-sixths."),                                                                           // 21

            // Transition 2→3 (step 22)
            new LessonStep(" ",                                                     "transition2to3"),     // 22

            // Phase 3: Equation 2/3 = ?/9 (steps 23–35)
            new LessonStep("Let's try another example"),                                                                                    // 23
            new LessonStep("Two thirds is equal to how many ninths?",               "p3_showEquation"),    // 24
            new LessonStep("First we'll make some space",                           "p3_slideApart"),      // 25
            new LessonStep("We have to multiply two thirds by something to figure out how many ninths it's equal to", "p3_fadeInMultiply"), // 26
            new LessonStep("Three times what equals nine?"),                                                                                // 27
            new LessonStep("Three times three is equal to nine, so move your piece to the three"),                                          // 28
            new LessonStep("Whatever value we used for the bottom, we have to use for the top, so put a three on the top as well"),         // 29
            new LessonStep("Let's now multiply the top values"),                                                                            // 30
            new LessonStep("Two times three equals what?"),                                                                                 // 31
            new LessonStep("Two times three equals six"),                                                                                   // 32
            new LessonStep("Move your piece to the six"),                                                                                   // 33
            new LessonStep("Good job"),                                                                                                     // 34
            new LessonStep("So now we know that two thirds is equal to six ninths"),                                                        // 35

            // Transition 3→4 (step 36)
            new LessonStep(" ",                                                     "transition3to4"),     // 36

            // Phase 4: Equation 3/5 = ?/20 (steps 37–41)
            new LessonStep("Let's try another example",                             "p4_showEquation"),    // 37
            new LessonStep("Three fifths is equal to how many twentieths?"),                                                                // 38
            new LessonStep("First, figure out what you need to multiply five by to get to twenty"),                                         // 39
            new LessonStep("Multiply the bottom to get to 20"),                                                                             // 40
            new LessonStep("How many twentieths is three fifths?"),                                                                         // 41
        };

        // ── Phase 5 Practice Questions ────────────────────────────────

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
                ["transition1to2"]   = CoAnimateTransition1to2,
            };
        }

        private void BuildPhase2Registry()
        {
            animationRegistry["slideApart"]     = CoAnimateSlideApart;
            animationRegistry["fadeInMultiply"] = CoAnimateFadeInMultiply;
            animationRegistry["zoomInDenom"]    = CoAnimateZoomIntoDenominators;
            animationRegistry["swapDenom3"]     = CoAnimateSwapDenominator3;
            animationRegistry["zoomOut"]        = CoAnimateZoomOutToFull;
            animationRegistry["transition2to3"] = CoAnimateTransition2to3;
        }

        private void BuildPhase3Registry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["p3_showEquation"]   = CoAnimateShowEquation,
                ["p3_slideApart"]     = CoAnimateSlideApart,
                ["p3_fadeInMultiply"] = CoAnimateFadeInMultiply,
                ["transition3to4"]    = CoAnimateTransition3to4,
            };
        }

        private void BuildPhase4Registry()
        {
            animationRegistry = new Dictionary<string, Func<Action, IEnumerator>>
            {
                ["p4_showEquation"] = CoAnimateShowEquation,
            };
        }

        private Func<Action, IEnumerator> ResolveAnimation(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key)) return null;
            animationRegistry.TryGetValue(key, out var factory);
            return factory;
        }

        // ── Circle Layout Constants ──────────────────────────────────
        private const float P1CircleSize = 350f;
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

        // ── Answer Circle Constants ────────────────────────────────
        private const float AnswerCircleSize = 156f;
        private const float CircleSpacing = 320f;
        private const float CircleY = -240f;
        private const float DWELL_TIME = 1.0f;

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

            if (!hasContact) { ResetDwell(); return; }

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

        // ══════════════════════════════════════════════════════════════
        //  VISUAL BUILDING: Phase 1 (Circles)
        // ══════════════════════════════════════════════════════════════

        private void BuildCircleVisuals()
        {
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);

            float totalWidth = P1CircleSize * 2 + CircleGap;
            float leftX = -totalWidth / 2f + P1CircleSize / 2f;
            float rightX = totalWidth / 2f - P1CircleSize / 2f;

            leftCircle = CreateCircle("LeftCircle", 2, new bool[] { true, false }, leftX);
            leftCircleGroup = leftCircle.GetComponent<CanvasGroup>();

            rightCircle = CreateCircle("RightCircle", 6, new bool[] { false, true, true, true, false, false }, rightX);
            rightCircleGroup = rightCircle.GetComponent<CanvasGroup>();

            leftLabelGroup = CreateFractionLabel("LeftLabel", "1", "2", leftX);
            rightLabelGroup = CreateFractionLabel("RightLabel", "3", "6", rightX);
        }

        private FractionCircle CreateCircle(string name, int divisions, bool[] shaded, float xPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(contentArea, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(P1CircleSize, P1CircleSize);
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
            rect.anchoredPosition = new Vector2(xPos, -P1CircleSize / 2f + LabelOffsetY);

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

        // ══════════════════════════════════════════════════════════════
        //  EQUATION BUILDERS (Phases 2–4)
        // ══════════════════════════════════════════════════════════════

        private void BuildPhase2Equation()
        {
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

        private void BuildPhase3Equation()
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

        private void BuildPhase4Equation()
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

            fracLeft = CreateFraction(equationRow, "3", "5", out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", "20", out fracRightNum, out fracRightDen, out fracRightTopGroup);
            fracRight.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth / 2f, 0);
        }

        // ── Shared Equation Helpers ─────────────────────────────────

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

        private void NullEquationRefs()
        {
            equationRow = null;
            equationRowGroup = null;
            fracLeft = null; opEquals = null; fracRight = null;
            opMultiply = null; fracMiddle = null;
            fracLeftNum = null; fracLeftDen = null;
            fracRightNum = null; fracRightDen = null;
            fracMiddleNum = null; fracMiddleDen = null;
            fracMiddleGroup = null; opMultiplyGroup = null;
            fracLeftTopGroup = null; fracMiddleTopGroup = null; fracRightTopGroup = null;
        }

        // ── Phase 5 Equation Builder ──────────────────────────────────

        private void BuildPracticeEquation(PracticeQuestion q)
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

            fracLeft = CreateFraction(equationRow, q.leftNum, q.leftDen, out fracLeftNum, out fracLeftDen, out fracLeftTopGroup);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            fracRight = CreateFraction(equationRow, "?", q.rightDen, out fracRightNum, out fracRightDen, out fracRightTopGroup);
            fracRight.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth / 2f, 0);
        }

        // ══════════════════════════════════════════════════════════════
        //  MAIN SEQUENCE
        // ══════════════════════════════════════════════════════════════

        private IEnumerator CoPlaySequence()
        {
            // ── Phase 1: Circles ──
            BuildCircleVisuals();
            BuildPhase1Registry();
            sequencer.Begin();
            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i <= 11; i++)
                yield return sequencer.RunStep(steps[i], ResolveAnimation(steps[i].animationKey));

            // Step 12: transition1to2 (circles → equation 1/2=?/6)
            yield return sequencer.RunStep(steps[12], ResolveAnimation(steps[12].animationKey));

            // ── Phase 2: Equation 1/2 = ?/6 ──
            for (int i = 13; i <= 19; i++)
                yield return sequencer.RunStep(steps[i], ResolveAnimation(steps[i].animationKey));

            yield return RunP2KaraokeStep();   // step 20
            yield return RunP2SummaryStep();   // step 21

            // Step 22: transition2to3 (equation → new equation 2/3=?/9)
            yield return sequencer.RunStep(steps[22], ResolveAnimation(steps[22].animationKey));

            // ── Phase 3: Equation 2/3 = ?/9 ──
            for (int i = 23; i <= 26; i++)
                yield return sequencer.RunStep(steps[i], ResolveAnimation(steps[i].animationKey));

            yield return RunP3DenominatorInteraction();      // steps 27–28
            yield return RunP3NumeratorInteraction();         // step 29
            yield return RunP3FinalProductInteraction();      // steps 30–33
            yield return sequencer.RunStep(steps[34], null);  // "Good job"
            yield return RunP3SummaryStep();                  // step 35

            // Step 36: transition3to4 (equation → new equation 3/5=?/20)
            yield return sequencer.RunStep(steps[36], ResolveAnimation(steps[36].animationKey));

            // ── Phase 4: Equation 3/5 = ?/20 ──
            yield return sequencer.RunStep(steps[37], ResolveAnimation(steps[37].animationKey));
            yield return new WaitForSeconds(1f);
            yield return sequencer.RunStep(steps[38], null);
            yield return new WaitForSeconds(1f);
            yield return sequencer.RunStep(steps[39], null);
            yield return new WaitForSeconds(1f);
            yield return sequencer.RunStep(steps[40], null);
            yield return new WaitForSeconds(0.5f);
            yield return RunP4AnswerInteraction();  // step 41

            // ── Transition 4→5: crossfade to practice ──
            yield return CoAnimateTransition4to5();

            // ── Phase 5: Practice ──
            yield return CoShowIntroCard("Practice Problems");
            yield return new WaitForSeconds(0.5f);

            BuildScoreBar();
            yield return CoFadeScoreBar(0f, 1f, 0.3f);

            for (int i = 0; i < questions.Length; i++)
            {
                var q = questions[i];
                currentQuestionIndex = i;
                currentQuestionHadWrong = false;
                UpdateScoreCount(i + 1);

                BuildPracticeEquation(q);

                bool animDone = false;
                StartCoroutine(CoAnimateShowEquation(() => animDone = true));
                yield return new WaitUntil(() => animDone);

                yield return CoShowAnswerCircles(q.choices);

                bool subDone = false;
                StartCoroutine(sequencer.CoShowSubtitle(q.subtitle,
                    new LessonStep(q.subtitle).EstimatedDuration, () => subDone = true));

                yield return CoWaitForAnswer(q.correctIndex);
                UpdateScorePill(i, !currentQuestionHadWrong);

                DestroyAnswerCircles();
                bool writeDone = false;
                StartCoroutine(CoHandwriteDigit(
                    fracRightNum.rectTransform.parent as RectTransform,
                    fracRightNum, q.CorrectAnswer, 0.6f, () => writeDone = true));
                yield return new WaitUntil(() => writeDone);
                yield return new WaitForSeconds(0.8f);

                yield return CoAnimateFadeOutEquation();
                ClearContentArea();
                yield return new WaitForSeconds(0.3f);
            }

            // Outro
            yield return CoFadeScoreBar(1f, 0f, 0.3f);
            if (scoreBarGo != null) Destroy(scoreBarGo);
            yield return sequencer.RunStep(outroStep, null);

            sequencer.End();
            playButtonGo.SetActive(true);
        }

        // ══════════════════════════════════════════════════════════════
        //  TRANSITIONS
        // ══════════════════════════════════════════════════════════════

        private IEnumerator CoAnimateTransition1to2(Action onComplete)
        {
            var circleChildren = new List<CanvasGroup>();
            if (leftCircleGroup != null) circleChildren.Add(leftCircleGroup);
            if (rightCircleGroup != null) circleChildren.Add(rightCircleGroup);
            if (leftLabelGroup != null) circleChildren.Add(leftLabelGroup);
            if (rightLabelGroup != null) circleChildren.Add(rightLabelGroup);

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

            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);
            leftCircle = null; rightCircle = null;
            leftCircleGroup = null; rightCircleGroup = null;
            leftLabelGroup = null; rightLabelGroup = null;

            yield return new WaitForSeconds(0.3f);

            BuildPhase2Equation();
            BuildPhase2Registry();

            var eqGroup = equationRow.gameObject.AddComponent<CanvasGroup>();
            eqGroup.alpha = 0f;
            float fadeInDuration = 0.4f;
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                eqGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            eqGroup.alpha = 1f;
            Destroy(eqGroup);

            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateTransition2to3(Action onComplete)
        {
            var fadeGroup = equationRow.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;

            float fadeDuration = 0.6f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);
            NullEquationRefs();

            yield return new WaitForSeconds(0.3f);

            BuildPhase3Equation();
            BuildPhase3Registry();

            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateTransition3to4(Action onComplete)
        {
            var fadeGroup = equationRow.gameObject.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = equationRow.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;

            float fadeDuration = 0.6f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);
            NullEquationRefs();
            DestroyAnswerCircles();

            yield return new WaitForSeconds(0.3f);

            BuildPhase4Equation();
            BuildPhase4Registry();

            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateTransition4to5()
        {
            // Fade out Phase 4 equation
            var fadeGroup = equationRow.gameObject.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = equationRow.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;

            float fadeDuration = 0.6f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            // Destroy all content children
            for (int i = contentArea.childCount - 1; i >= 0; i--)
                Destroy(contentArea.GetChild(i).gameObject);
            NullEquationRefs();
            DestroyAnswerCircles();

            yield return new WaitForSeconds(0.3f);
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 1: Circle Animations
        // ══════════════════════════════════════════════════════════════

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
            Transform tr = labelGroup.transform;
            tr.localScale = Vector3.zero;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                labelGroup.alpha = p;
                float scale = EaseOutBack(p);
                tr.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            labelGroup.alpha = 1f;
            tr.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        // ══════════════════════════════════════════════════════════════
        //  EQUATION ANIMATIONS (shared by Phases 2–5)
        // ══════════════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════════════
        //  PHASE 2: Special Steps (Karaoke + Summary)
        // ══════════════════════════════════════════════════════════════

        private IEnumerator RunP2KaraokeStep()
        {
            var step = steps[20];
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

        private IEnumerator RunP2SummaryStep()
        {
            var step = steps[21];
            bool done = false;

            StartCoroutine(sequencer.CoShowKaraokeSubtitle(
                step.subtitle, 3f,
                (idx, word, duration) =>
                {
                    if (idx == 5)
                        StartCoroutine(CoPulseScale(fracLeft, 1.15f, duration));
                    else if (idx == 9)
                        StartCoroutine(CoPulseScale(fracRight, 1.15f, duration));
                },
                () => done = true));

            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.5f);
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 3: Interactive Sections (2/3 = ?/9)
        // ══════════════════════════════════════════════════════════════

        private IEnumerator RunP3DenominatorInteraction()
        {
            bool zoomDone = false;
            StartCoroutine(CoAnimateZoomIntoDenominators(() => zoomDone = true));
            yield return new WaitUntil(() => zoomDone);

            yield return CoShowAnswerCircles(new string[] { "2", "3", "4" });

            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[27].subtitle, steps[27].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(1f);

            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[28].subtitle, steps[28].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            yield return CoWaitForAnswer(1);

            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracMiddleDen.rectTransform.parent as RectTransform,
                fracMiddleDen, "3", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunP3NumeratorInteraction()
        {
            bool zoomDone = false;
            StartCoroutine(CoAnimateZoomOutToFull(() => zoomDone = true));
            yield return new WaitUntil(() => zoomDone);

            yield return CoShowAnswerCircles(new string[] { "2", "3", "4" });

            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[29].subtitle, steps[29].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            yield return CoWaitForAnswer(1);

            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracMiddleNum.rectTransform.parent as RectTransform,
                fracMiddleNum, "3", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunP3FinalProductInteraction()
        {
            yield return CoShowAnswerCircles(new string[] { "4", "5", "6" });

            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[30].subtitle, steps[30].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[31].subtitle, steps[31].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);
            yield return new WaitForSeconds(1f);

            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[32].subtitle, steps[32].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[33].subtitle, steps[33].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            yield return CoWaitForAnswer(2);

            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracRightNum.rectTransform.parent as RectTransform,
                fracRightNum, "6", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RunP3SummaryStep()
        {
            var step = steps[35];
            bool done = false;

            StartCoroutine(sequencer.CoShowKaraokeSubtitle(
                step.subtitle, 3f,
                (idx, word, duration) =>
                {
                    if (idx == 5)
                        StartCoroutine(CoPulseScale(fracLeft, 1.15f, duration));
                    else if (idx == 10)
                        StartCoroutine(CoPulseScale(fracRight, 1.15f, duration));
                },
                () => done = true));

            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.5f);
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 4: Interactive Section (3/5 = ?/20)
        // ══════════════════════════════════════════════════════════════

        private IEnumerator RunP4AnswerInteraction()
        {
            yield return CoShowAnswerCircles(new string[] { "10", "12", "14" });

            bool subDone = false;
            StartCoroutine(sequencer.CoShowSubtitle(steps[41].subtitle, steps[41].EstimatedDuration, () => subDone = true));
            yield return new WaitUntil(() => subDone);

            yield return CoWaitForAnswer(1);

            DestroyAnswerCircles();
            bool writeDone = false;
            StartCoroutine(CoHandwriteDigit(
                fracRightNum.rectTransform.parent as RectTransform,
                fracRightNum, "12", 0.6f, () => writeDone = true));
            yield return new WaitUntil(() => writeDone);
            yield return new WaitForSeconds(0.5f);
        }

        // ══════════════════════════════════════════════════════════════
        //  PHASE 5: Score Tracker
        // ══════════════════════════════════════════════════════════════

        private void BuildScoreBar()
        {
            scoreBarGo = new GameObject("ScoreBar");
            scoreBarGo.transform.SetParent(scoreArea, false);
            var barRect = scoreBarGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);

            var bgImg = scoreBarGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.14f, 0.25f, 0.85f);
            bgImg.raycastTarget = false;

            float textWidth = 100f;
            float dividerWidth = 2f;
            float dividerGap = 12f;
            float pillsWidth = questions.Length * PillWidth + (questions.Length - 1) * PillGap;
            float totalWidth = 20f + textWidth + dividerGap + dividerWidth + dividerGap + pillsWidth + 20f;
            float totalHeight = 56f;
            barRect.sizeDelta = new Vector2(totalWidth, totalHeight);

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

            float elapsed = 0f;
            float fadeDuration = 0.4f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                tmp.color = new Color(1, 1, 1, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            tmp.color = Color.white;

            yield return new WaitForSeconds(1.5f);

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

        // ══════════════════════════════════════════════════════════════
        //  ANSWER CIRCLE SYSTEM
        // ══════════════════════════════════════════════════════════════

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
                rect.sizeDelta = new Vector2(AnswerCircleSize, AnswerCircleSize);
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

        // ══════════════════════════════════════════════════════════════
        //  HANDWRITING EFFECTS
        // ══════════════════════════════════════════════════════════════

        // Overload for Phase 2 karaoke (hardcoded "3")
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

        // Overload for interactive handwriting (Phases 3–5)
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

        // ══════════════════════════════════════════════════════════════
        //  SHARED ANIMATION COROUTINES
        // ══════════════════════════════════════════════════════════════

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
