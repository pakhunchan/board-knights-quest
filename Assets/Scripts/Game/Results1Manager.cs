using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Results1 scene: animated reveal of level-up, title promotion,
    /// XP earned, and a CONTINUE button. Reads data from QuestResultsData.Pending.
    /// Enhanced version with sparkles, flower blooms, meadow glow, and bob animation.
    /// </summary>
    public class Results1Manager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup screenGroup;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private CanvasGroup levelSection;
        [SerializeField] private TextMeshProUGUI levelBeforeText;
        [SerializeField] private Image levelArrowImage;
        [SerializeField] private TextMeshProUGUI levelAfterText;
        [SerializeField] private CanvasGroup titleBanner;
        [SerializeField] private TextMeshProUGUI titleBannerText;
        [SerializeField] private TextMeshProUGUI titleBeforeText;
        [SerializeField] private Image titleArrowImage;
        [SerializeField] private TextMeshProUGUI titleAfterText;
        [SerializeField] private CanvasGroup xpSection;
        [SerializeField] private Image xpBarFill;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private CanvasGroup xpBonusGroup;
        [SerializeField] private TextMeshProUGUI xpBonusText;
        [SerializeField] private Button continueButton;
        [SerializeField] private CanvasGroup continueButtonGroup;

        // Enhanced fields
        [SerializeField] private Image meadowGlow;
        [SerializeField] private Image levelUpFlash;
        [SerializeField] private RectTransform[] flowerRoots;
        [SerializeField] private TextMeshProUGUI rankTitleText;
        [SerializeField] private RectTransform continueButtonRect;

        // Track which flowers have bloomed
        private bool[] flowerBloomed;

        private void Start()
        {
            var data = QuestResultsData.Pending;
            QuestResultsData.Pending = null;

            if (data == null)
            {
                Debug.Log("[Results1] No pending data — using demo fallback.");
                data = new QuestResultsData
                {
                    levelBefore = 1,
                    levelAfter = 2,
                    titleBefore = "Trainee",
                    titleAfter = "Explorer",
                    xpBefore = 0,
                    xpGained = 500,
                    xpToNextLevel = 200,
                    xpToNextLevelAfter = 600,
                    mapStageBefore = 1,
                    mapStageAfter = 2,
                    nextSceneName = "Outro1"
                };
            }

            // Initialize all groups hidden
            SetGroupHidden(screenGroup);
            SetGroupHidden(levelSection);
            SetGroupHidden(titleBanner);
            SetGroupHidden(xpSection);
            SetGroupHidden(xpBonusGroup);
            SetGroupHidden(continueButtonGroup);
            headerText.alpha = 0f;

            // Start XP bar empty
            SetXPBarFill(0f);

            // Hide arrow and after-level initially
            if (levelArrowImage != null)
            {
                var ac = levelArrowImage.color;
                levelArrowImage.color = new Color(ac.r, ac.g, ac.b, 0f);
            }
            levelAfterText.alpha = 0f;

            // Hide meadow glow and flash
            if (meadowGlow != null)
            {
                var c = meadowGlow.color;
                meadowGlow.color = new Color(c.r, c.g, c.b, 0f);
            }
            if (levelUpFlash != null)
            {
                var c = levelUpFlash.color;
                levelUpFlash.color = new Color(c.r, c.g, c.b, 0f);
            }

            // Initialize flower tracking
            if (flowerRoots != null)
            {
                flowerBloomed = new bool[flowerRoots.Length];
                for (int i = 0; i < flowerRoots.Length; i++)
                {
                    if (flowerRoots[i] != null)
                        flowerRoots[i].localScale = Vector3.zero;
                }
            }

            // Hide title sub-groups (promoted label + transition row)
            if (titleBanner != null)
            {
                // TitlePromotedLabel and TitleTransitionRow are children with their own CanvasGroups
                for (int i = 0; i < titleBanner.transform.childCount; i++)
                {
                    var childCg = titleBanner.transform.GetChild(i).GetComponent<CanvasGroup>();
                    if (childCg != null)
                    {
                        childCg.alpha = 0f;
                        childCg.blocksRaycasts = false;
                    }
                }
            }

            // Populate text fields
            levelBeforeText.text = $"Level {data.levelBefore}";
            levelAfterText.text = $"Level {data.levelAfter}";
            titleBannerText.text = "Title Promoted!";
            titleBeforeText.text = data.titleBefore;
            titleAfterText.text = data.titleAfter;
            xpBonusText.text = $"+{data.xpGained} XP";
            if (rankTitleText != null)
                rankTitleText.text = data.titleBefore;

            // Wire continue button
            string nextScene = string.IsNullOrEmpty(data.nextSceneName) ? "Outro1" : data.nextSceneName;
            continueButton.onClick.AddListener(() => NavigationHelper.LoadScene(nextScene));

            StartCoroutine(PlaySequence(data));
        }

        private IEnumerator PlaySequence(QuestResultsData data)
        {
            // 0.0s — Wait 1s
            yield return new WaitForSeconds(1f);

            // 1.0s — Fade in screenGroup (0.6s)
            yield return FadeCanvasGroup(screenGroup, 0f, 1f, 0.6f);

            yield return new WaitForSeconds(1.2f);

            // 2.8s — Fade+slideUp headerText (0.8s)
            yield return FadeAlphaSlideUp(headerText, 0.8f);

            // 3.6s — Fade in levelSection (0.6s) — arrow hidden, after hidden
            yield return new WaitForSeconds(0f);
            yield return FadeCanvasGroup(levelSection, 0f, 1f, 0.6f);

            // 4.0s — Fade in xpSection (0.6s) — slight overlap
            yield return new WaitForSeconds(0.0f);
            yield return FadeCanvasGroup(xpSection, 0f, 1f, 0.6f);

            // 4.6s — Show "+XP" bonus (scale punch + SpawnSparkles 12)
            yield return new WaitForSeconds(0f);
            xpBonusGroup.alpha = 1f;
            xpBonusGroup.blocksRaycasts = true;
            var bonusRect = xpBonusGroup.GetComponent<RectTransform>();
            SpawnSparkles(bonusRect, 12);
            if (bonusRect != null)
                yield return ScalePunch(bonusRect, 1.3f, 0.6f);
            else
                yield return new WaitForSeconds(0.6f);

            // 5.2s — Animate XP bar 0→max (2.2s, SmoothStep) — bloom flowers
            yield return new WaitForSeconds(0f);
            int endXP = Mathf.Min(data.xpBefore + data.xpGained, data.xpToNextLevel);
            float endFill = data.xpToNextLevel > 0 ? (float)endXP / data.xpToNextLevel : 1f;
            yield return AnimateXPBar(0f, endFill, 0, endXP, data.xpToNextLevel, 2.2f, true);

            // 6.8s — XP bar maxed

            // 7.2s — LEVEL UP sequence
            if (data.levelBefore != data.levelAfter)
            {
                yield return new WaitForSeconds(0.4f);

                // Flash levelUpFlash (alpha 0→0.35→0 over 0.6s)
                if (levelUpFlash != null)
                    StartCoroutine(FlashImage(levelUpFlash, 0.35f, 0.6f));

                // SpawnSparkles from levelSection center (24)
                SpawnSparkles(levelSection.GetComponent<RectTransform>(), 24);

                yield return new WaitForSeconds(0.6f);

                // 7.8s — Highlight levelBeforeText
                levelBeforeText.color = NavigationHelper.HexColor("#c88a20");
                yield return new WaitForSeconds(0.3f);

                // 8.1s — Fade in arrow
                if (levelArrowImage != null)
                    yield return FadeImage(levelArrowImage, 0f, 1f, 0.3f);

                // 8.4s — Fade in levelAfterText + highlight
                yield return FadeAlpha(levelAfterText, 0f, 1f, 0.4f);
                levelAfterText.color = NavigationHelper.HexColor("#c88a20");
                levelBeforeText.color = NavigationHelper.HexColor("#a08868");

                yield return new WaitForSeconds(0.6f);

                // 9.0s — Title promotion (if title changed)
                if (data.titleBefore != data.titleAfter)
                {
                    // Fade out rankTitleText as title banner appears
                    if (rankTitleText != null)
                        StartCoroutine(FadeAlpha(rankTitleText, 1f, 0f, 0.3f));

                    // Fade in titleBanner container
                    yield return FadeCanvasGroup(titleBanner, 0f, 1f, 0.3f);

                    // Fade in "Title Promoted!" label
                    var promotedLabel = FindChildCanvasGroup(titleBanner.transform, "TitlePromotedLabel");
                    if (promotedLabel != null)
                    {
                        yield return FadeCanvasGroup(promotedLabel, 0f, 1f, 0.5f);
                        // Start shimmer on titleBannerText
                        StartCoroutine(ShimmerText(titleBannerText,
                            NavigationHelper.HexColor("#c88a20"),
                            NavigationHelper.HexColor("#f0c060"),
                            1.5f));
                    }

                    SpawnSparkles(titleBanner.GetComponent<RectTransform>(), 16);

                    yield return new WaitForSeconds(0.7f);

                    // 9.7s — Show title transition row
                    var transitionRow = FindChildCanvasGroup(titleBanner.transform, "TitleTransitionRow");
                    if (transitionRow != null)
                        yield return FadeCanvasGroup(transitionRow, 0f, 1f, 0.5f);

                    SpawnSparkles(titleBanner.GetComponent<RectTransform>(), 12);

                    yield return new WaitForSeconds(1.0f);
                }

                // 10.7s — Reset XP bar to 0, close all flowers
                CloseAllFlowers();
                int newMax = data.xpToNextLevelAfter > 0 ? data.xpToNextLevelAfter : data.xpToNextLevel;
                SetXPBarFill(0f);
                xpText.text = $"0 / {newMax} XP";

                yield return new WaitForSeconds(0.6f);

                // 11.3s — Animate XP 0→overflow (1.6s) — bloom flowers proportionally
                int overflow = data.xpBefore + data.xpGained - data.xpToNextLevel;
                if (overflow > 0)
                {
                    float overflowFill = newMax > 0 ? (float)overflow / newMax : 0f;
                    yield return AnimateXPBar(0f, overflowFill, 0, overflow, newMax, 1.6f, true);
                }
            }

            // Show continue button (fade in)
            yield return new WaitForSeconds(0.8f);
            yield return FadeCanvasGroup(continueButtonGroup, 0f, 1f, 0.7f);
            continueButtonGroup.blocksRaycasts = true;
            continueButtonGroup.interactable = true;

            // 14.0s — SpawnSparkles around button + start BobAnimation
            if (continueButtonRect != null)
            {
                SpawnSparkles(continueButtonRect, 10);
                StartCoroutine(BobAnimation(continueButtonRect));
            }
        }

        // ── Sparkle System ────────────────────────────────────

        private void SpawnSparkles(RectTransform origin, int count)
        {
            if (origin == null || screenGroup == null) return;

            var parentTransform = screenGroup.transform;
            Vector2 center = origin.anchoredPosition;

            // Convert origin position relative to screenGroup
            if (origin.parent != parentTransform)
            {
                // Get world position and convert to screenGroup local space
                Vector3 worldPos = origin.TransformPoint(Vector3.zero);
                center = ((RectTransform)parentTransform).InverseTransformPoint(worldPos);
            }

            for (int i = 0; i < count; i++)
            {
                var sparkleGo = new GameObject($"Sparkle_{i}");
                sparkleGo.transform.SetParent(parentTransform, false);

                var rt = sparkleGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(6, 6);
                rt.anchoredPosition = center;

                var img = sparkleGo.AddComponent<Image>();
                img.sprite = NavigationHelper.EnsureCircleSprite();
                img.raycastTarget = false;

                // Random gold/warm color
                float hueShift = Random.Range(-0.05f, 0.05f);
                img.color = new Color(1f, 0.85f + hueShift, 0.3f + Random.Range(0f, 0.3f), 1f);

                // Random outward direction
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(40f, 120f);
                Vector2 target = center + new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);

                StartCoroutine(AnimateSparkle(rt, img, center, target, Random.Range(0.6f, 1.0f)));
            }
        }

        private IEnumerator AnimateSparkle(RectTransform rt, Image img, Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(from, to, t);
                rt.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, 1f - t);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        // ── Flower System ─────────────────────────────────────

        private void BloomFlower(int index)
        {
            if (flowerRoots == null || index < 0 || index >= flowerRoots.Length) return;
            if (flowerBloomed != null && flowerBloomed[index]) return;

            if (flowerBloomed != null) flowerBloomed[index] = true;
            if (flowerRoots[index] != null)
                StartCoroutine(BloomFlowerCoroutine(flowerRoots[index]));
        }

        private IEnumerator BloomFlowerCoroutine(RectTransform root)
        {
            float elapsed = 0f;
            float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Overshoot curve: goes to ~1.2 then settles to 1.0
                float scale = t < 0.7f
                    ? Mathf.Lerp(0f, 1.2f, t / 0.7f)
                    : Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);
                root.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            root.localScale = Vector3.one;
        }

        private void CloseAllFlowers()
        {
            if (flowerRoots == null) return;
            for (int i = 0; i < flowerRoots.Length; i++)
            {
                if (flowerBloomed != null) flowerBloomed[i] = false;
                if (flowerRoots[i] != null)
                    StartCoroutine(CloseFlowerCoroutine(flowerRoots[i]));
            }
        }

        private IEnumerator CloseFlowerCoroutine(RectTransform root)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 startScale = root.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                root.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            root.localScale = Vector3.zero;
        }

        // ── Bob Animation ─────────────────────────────────────

        private IEnumerator BobAnimation(RectTransform rt)
        {
            Vector2 originalPos = rt.anchoredPosition;
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * (2f * Mathf.PI / 3f)) * 5f;
                rt.anchoredPosition = new Vector2(originalPos.x, originalPos.y + offset);
                yield return null;
            }
        }

        // ── Shimmer Text ──────────────────────────────────────

        private IEnumerator ShimmerText(TextMeshProUGUI tmp, Color baseColor, Color brightColor, float period)
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed / period, 1f);
                tmp.color = Color.Lerp(baseColor, brightColor, t);
                yield return null;
            }
        }

        // ── Image Fade ────────────────────────────────────────

        private IEnumerator FadeImage(Image img, float fromA, float toA, float duration)
        {
            float elapsed = 0f;
            var c = img.color;
            img.color = new Color(c.r, c.g, c.b, fromA);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(fromA, toA, elapsed / duration);
                img.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            img.color = new Color(c.r, c.g, c.b, toA);
        }

        private IEnumerator FlashImage(Image img, float peakAlpha, float duration)
        {
            float half = duration * 0.5f;
            var c = img.color;
            // Ramp up
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, peakAlpha, elapsed / half));
                yield return null;
            }
            // Ramp down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(peakAlpha, 0f, elapsed / half));
                yield return null;
            }
            img.color = new Color(c.r, c.g, c.b, 0f);
        }

        // ── Helpers ──────────────────────────────────────────

        private void SetGroupHidden(CanvasGroup cg)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        private CanvasGroup FindChildCanvasGroup(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                return child.GetComponent<CanvasGroup>();
            return null;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            cg.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
            if (to > 0.5f)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

        private IEnumerator FadeAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
        {
            float elapsed = 0f;
            tmp.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                tmp.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            tmp.alpha = to;
        }

        private IEnumerator FadeAlphaSlideUp(TextMeshProUGUI tmp, float duration)
        {
            var rt = tmp.GetComponent<RectTransform>();
            Vector2 originalPos = rt.anchoredPosition;
            Vector2 startPos = originalPos + new Vector2(0, -20f);
            rt.anchoredPosition = startPos;
            tmp.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tmp.alpha = t;
                rt.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
                yield return null;
            }
            tmp.alpha = 1f;
            rt.anchoredPosition = originalPos;
        }

        private void SetXPBarFill(float fill)
        {
            var rt = xpBarFill.GetComponent<RectTransform>();
            rt.anchorMax = new Vector2(fill, 1f);
        }

        private IEnumerator AnimateXPBar(float fromFill, float toFill, int fromXP, int toXP, int maxXP, float duration, bool bloomFlowers)
        {
            float elapsed = 0f;
            int prevFlowerIdx = -1;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float currentFill = Mathf.Lerp(fromFill, toFill, t);
                SetXPBarFill(currentFill);
                int currentXP = Mathf.RoundToInt(Mathf.Lerp(fromXP, toXP, t));
                xpText.text = $"{currentXP} / {maxXP} XP";

                // Bloom flowers progressively
                if (bloomFlowers && flowerRoots != null && flowerRoots.Length > 0)
                {
                    int flowerIdx = Mathf.FloorToInt(currentFill * flowerRoots.Length) - 1;
                    flowerIdx = Mathf.Clamp(flowerIdx, -1, flowerRoots.Length - 1);
                    if (flowerIdx > prevFlowerIdx)
                    {
                        for (int i = prevFlowerIdx + 1; i <= flowerIdx; i++)
                            BloomFlower(i);
                        prevFlowerIdx = flowerIdx;
                    }
                }

                yield return null;
            }
            SetXPBarFill(toFill);
            xpText.text = $"{toXP} / {maxXP} XP";
        }

        private IEnumerator ScalePunch(RectTransform rt, float peakScale, float duration)
        {
            float half = duration * 0.5f;
            float elapsed = 0f;
            Vector3 original = rt.localScale;
            Vector3 peak = original * peakScale;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                rt.localScale = Vector3.Lerp(original, peak, elapsed / half);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                rt.localScale = Vector3.Lerp(peak, original, elapsed / half);
                yield return null;
            }
            rt.localScale = original;
        }
    }
}
