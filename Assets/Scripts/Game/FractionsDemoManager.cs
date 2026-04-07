using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BoardOfEducation.Input;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Drives the Fractions Demo: builds a fraction equation at runtime,
    /// animates it through 4 steps with synchronized subtitles.
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

        private TextMeshProUGUI fracLeftNum, fracLeftDen;
        private TextMeshProUGUI fracRightNum, fracRightDen;
        private TextMeshProUGUI fracMiddleNum, fracMiddleDen;

        private CanvasGroup fracMiddleGroup;
        private CanvasGroup opMultiplyGroup;

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
            new EquationStep("Let's shift these to the right to make space"),
            new EquationStep("We need to multiply one-half by something to turn it into something over six"),
            new EquationStep("One half times three thirds equals three sixths"),
        };

        // Layout constants
        private const float FractionWidth = 120f;
        private const float OperatorWidth = 60f;
        private const float ElementGap = 20f;
        private const float SlideOffset = 100f;
        private const float BarHeight = 4f;
        private const float FractionHeight = 140f;
        private const float FontSize = 48f;

        // Piece/finger dwell tracking for Play button
        private float dwellOnPlay = -1f;
        private const float DWELL_TIME = 1.0f;
        private Image playButtonImage;
        private Color playButtonBaseColor;

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

                // Visual feedback — lerp toward brighter
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
            // Clear any previous children
            for (int i = equationRow.childCount - 1; i >= 0; i--)
                Destroy(equationRow.GetChild(i).gameObject);

            // Calculate positions: center the initial equation (frac = frac)
            // Total width: frac + gap + op + gap + frac
            float totalWidth = FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth;
            float startX = -totalWidth / 2f;

            // Left fraction: 1/2
            fracLeft = CreateFraction(equationRow, "1", "2", out fracLeftNum, out fracLeftDen);
            fracLeft.anchoredPosition = new Vector2(startX + FractionWidth / 2f, 0);

            // Equals sign
            opEquals = CreateOperator(equationRow, "=");
            opEquals.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth / 2f, 0);

            // Right fraction: ?/6
            fracRight = CreateFraction(equationRow, "?", "6", out fracRightNum, out fracRightDen);
            fracRight.anchoredPosition = new Vector2(startX + FractionWidth + ElementGap + OperatorWidth + ElementGap + FractionWidth / 2f, 0);

            // Middle fraction (hidden): ?/? — will appear between left and equals
            fracMiddle = CreateFraction(equationRow, "?", "?", out fracMiddleNum, out fracMiddleDen);
            fracMiddle.anchoredPosition = Vector2.zero; // positioned later
            fracMiddleGroup = fracMiddle.gameObject.AddComponent<CanvasGroup>();
            fracMiddleGroup.alpha = 0f;

            // Multiply operator (hidden)
            opMultiply = CreateOperator(equationRow, "\u00d7");
            opMultiply.anchoredPosition = Vector2.zero; // positioned later
            opMultiplyGroup = opMultiply.gameObject.AddComponent<CanvasGroup>();
            opMultiplyGroup.alpha = 0f;
        }

        private RectTransform CreateFraction(RectTransform parent, string num, string den,
            out TextMeshProUGUI numTmp, out TextMeshProUGUI denTmp)
        {
            var go = new GameObject("Fraction_" + num + "_" + den);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(FractionWidth, FractionHeight);

            // Numerator
            var numGo = new GameObject("Num");
            numGo.transform.SetParent(go.transform, false);
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0, 0.55f);
            numRect.anchorMax = new Vector2(1, 1f);
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;
            numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = num;
            numTmp.fontSize = FontSize;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;

            // Bar
            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(go.transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.1f, 0.5f);
            barRect.anchorMax = new Vector2(0.9f, 0.5f);
            barRect.offsetMin = new Vector2(0, -BarHeight / 2f);
            barRect.offsetMax = new Vector2(0, BarHeight / 2f);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = Color.white;
            barImg.raycastTarget = false;

            // Denominator
            var denGo = new GameObject("Den");
            denGo.transform.SetParent(go.transform, false);
            var denRect = denGo.AddComponent<RectTransform>();
            denRect.anchorMin = new Vector2(0, 0f);
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

            // Step 1: Show initial equation, display subtitle
            {
                bool animDone = false, subDone = false;
                animDone = true; // no animation for step 1, just display
                StartCoroutine(CoShowSubtitle(steps[0].subtitle, steps[0].subtitleDuration, () => subDone = true));
                yield return new WaitUntil(() => animDone && subDone);
                yield return new WaitForSeconds(0.5f);
            }

            // Step 2: Slide apart to make space
            {
                bool animDone = false, subDone = false;
                StartCoroutine(CoAnimateStep2(() => animDone = true));
                StartCoroutine(CoShowSubtitle(steps[1].subtitle, steps[1].subtitleDuration, () => subDone = true));
                yield return new WaitUntil(() => animDone && subDone);
                yield return new WaitForSeconds(0.5f);
            }

            // Step 3: Fade in multiply and middle fraction
            {
                bool animDone = false, subDone = false;
                StartCoroutine(CoAnimateStep3(() => animDone = true));
                StartCoroutine(CoShowSubtitle(steps[2].subtitle, steps[2].subtitleDuration, () => subDone = true));
                yield return new WaitUntil(() => animDone && subDone);
                yield return new WaitForSeconds(0.5f);
            }

            // Step 4: Swap ? values to 3
            {
                bool animDone = false, subDone = false;
                StartCoroutine(CoAnimateStep4(() => animDone = true));
                StartCoroutine(CoShowSubtitle(steps[3].subtitle, steps[3].subtitleDuration, () => subDone = true));
                yield return new WaitUntil(() => animDone && subDone);
                yield return new WaitForSeconds(1f);
            }

            // Fade out subtitle and show replay button
            subtitleText.alpha = 0f;
            subtitleText.text = "";
            playButtonGo.SetActive(true);
        }

        // ── Step Animations ─────────────────────────────────────

        private IEnumerator CoAnimateStep2(System.Action onComplete)
        {
            // Slide fracLeft left and (opEquals + fracRight) right to create gap
            Vector2 leftFrom = fracLeft.anchoredPosition;
            Vector2 leftTo = leftFrom + new Vector2(-SlideOffset, 0);

            Vector2 eqFrom = opEquals.anchoredPosition;
            Vector2 eqTo = eqFrom + new Vector2(SlideOffset, 0);

            Vector2 rightFrom = fracRight.anchoredPosition;
            Vector2 rightTo = rightFrom + new Vector2(SlideOffset, 0);

            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                fracLeft.anchoredPosition = Vector2.Lerp(leftFrom, leftTo, smooth);
                opEquals.anchoredPosition = Vector2.Lerp(eqFrom, eqTo, smooth);
                fracRight.anchoredPosition = Vector2.Lerp(rightFrom, rightTo, smooth);

                yield return null;
            }

            fracLeft.anchoredPosition = leftTo;
            opEquals.anchoredPosition = eqTo;
            fracRight.anchoredPosition = rightTo;

            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateStep3(System.Action onComplete)
        {
            // Position multiply and middle fraction in the gap
            float midX = (fracLeft.anchoredPosition.x + opEquals.anchoredPosition.x) / 2f;
            float mulX = fracLeft.anchoredPosition.x + (midX - fracLeft.anchoredPosition.x) / 2f + OperatorWidth / 4f;
            float fracMidX = midX + (opEquals.anchoredPosition.x - midX) / 2f - OperatorWidth / 4f;

            opMultiply.anchoredPosition = new Vector2(mulX, 0);
            fracMiddle.anchoredPosition = new Vector2(fracMidX, 0);

            // Fade in both
            bool mulDone = false, fracDone = false;
            StartCoroutine(CoFadeIn(opMultiplyGroup, 0.4f, () => mulDone = true));
            StartCoroutine(CoFadeIn(fracMiddleGroup, 0.4f, () => fracDone = true));

            yield return new WaitUntil(() => mulDone && fracDone);
            onComplete?.Invoke();
        }

        private IEnumerator CoAnimateStep4(System.Action onComplete)
        {
            // Swap all three ? values to 3 in parallel
            bool a = false, b = false, c = false;
            StartCoroutine(CoSwapText(fracMiddleNum, "3", 0.3f, () => a = true));
            StartCoroutine(CoSwapText(fracMiddleDen, "3", 0.3f, () => b = true));
            StartCoroutine(CoSwapText(fracRightNum, "3", 0.3f, () => c = true));

            yield return new WaitUntil(() => a && b && c);
            onComplete?.Invoke();
        }

        // ── Animation Coroutines ────────────────────────────────

        private IEnumerator CoSlide(RectTransform rt, Vector2 from, Vector2 to, float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            rt.anchoredPosition = to;
            onComplete?.Invoke();
        }

        private IEnumerator CoFadeIn(CanvasGroup cg, float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            cg.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = t;
                yield return null;
            }
            cg.alpha = 1f;
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

            // Swap
            tmp.text = newValue;
            tmp.color = new Color(0.18f, 0.8f, 0.44f); // green highlight

            // Scale up with EaseOutBack
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scale = EaseOutBack(t);
                tmp.transform.localScale = origScale * scale;
                yield return null;
            }
            tmp.transform.localScale = origScale;
            onComplete?.Invoke();
        }

        private IEnumerator CoShowSubtitle(string text, float duration, System.Action onComplete)
        {
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

            // Hold
            yield return new WaitForSeconds(duration);

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
