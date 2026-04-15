using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using BoardOfEducation.Audio;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Intro3 scene: Knight intro with TTS-style subtitle highlighting,
    /// then cross-fades to level map. Each word in the script turns red in sequence
    /// to mimic text-to-speech narration.
    /// </summary>
    public class Intro3Manager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup introScreen;
        [SerializeField] private CanvasGroup mapScreen;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button continueButton;
        [SerializeField] private CanvasGroup continueButtonGroup;
        [SerializeField] private RectTransform continueButtonRect;
        [SerializeField] private Button goButton;
        [SerializeField] private RectTransform knightRect;
        [SerializeField] private Image knightImage;

        public System.Action OnComplete;

        private const float FADE_DURATION = 0.6f;
        private const float BASE_WORD_DURATION = 0.28f;
        private const float PER_CHAR_DURATION = 0.03f;
        private const float PAUSE_AFTER_COMMA = 0.15f;
        private const float PAUSE_AFTER_SENTENCE = 0.35f;
        private const string HIGHLIGHT_COLOR = "#E63946";

        private bool transitioning;
        private TTSAudioProvider ttsProvider;

        private static readonly string Script =
            "Brave adventurer, you've already conquered tricky paths, " +
            "discovered treasure, and proven your courage. " +
            "Now it's time to face your greatest quest yet: " +
            "mastering the magic of math!";

        private void Start()
        {
            introScreen.alpha = 1f;
            introScreen.blocksRaycasts = true;
            mapScreen.alpha = 0f;
            mapScreen.blocksRaycasts = false;

            if (continueButtonGroup != null)
            {
                continueButtonGroup.alpha = 0f;
                continueButtonGroup.blocksRaycasts = false;
                continueButtonGroup.interactable = false;
            }
            if (knightImage != null)
                knightImage.color = new Color(1f, 1f, 1f, 0f);

            continueButton.onClick.AddListener(OnContinueClicked);
            goButton.onClick.AddListener(OnGoClicked);

            ttsProvider = gameObject.GetComponent<TTSAudioProvider>()
                ?? gameObject.AddComponent<TTSAudioProvider>();

            GameAudioManager.Instance?.PlayBGM();

            StartCoroutine(PlaySubtitles());
        }

        private IEnumerator PlaySubtitles()
        {
            string[] words = Script.Split(' ');
            subtitleText.text = "";

            // Knight entrance animation (or brief pause if not wired)
            yield return KnightEntrance();

            // Duck BGM during TTS narration
            GameAudioManager.Instance?.DuckBGM();

            // Play TTS audio in parallel with karaoke highlighting
            StartCoroutine(ttsProvider.SpeakCoroutine(Script, null));

            for (int i = 0; i < words.Length; i++)
            {
                // Build the displayed string with the current word highlighted
                var sb = new System.Text.StringBuilder();
                for (int j = 0; j < words.Length; j++)
                {
                    if (j > 0) sb.Append(' ');

                    if (j < i)
                    {
                        // Already spoken — default color
                        sb.Append(words[j]);
                    }
                    else if (j == i)
                    {
                        // Currently being spoken — red highlight
                        sb.Append($"<color={HIGHLIGHT_COLOR}>{words[j]}</color>");
                    }
                    else
                    {
                        // Not yet spoken — dim
                        sb.Append($"<alpha=#55>{words[j]}<alpha=#FF>");
                    }
                }

                subtitleText.text = sb.ToString();

                // Duration scales with word length for natural pacing
                string cleanWord = words[i].TrimEnd(',', '.', '!', ':', ';');
                float duration = BASE_WORD_DURATION + cleanWord.Length * PER_CHAR_DURATION;

                // Extra pause after punctuation
                char lastChar = words[i][words[i].Length - 1];
                if (lastChar == '.' || lastChar == '!' || lastChar == '?')
                    duration += PAUSE_AFTER_SENTENCE;
                else if (lastChar == ',' || lastChar == ':' || lastChar == ';')
                    duration += PAUSE_AFTER_COMMA;

                yield return new WaitForSeconds(duration);
            }

            // Final state: all words in default color
            subtitleText.text = Script;

            // Restore BGM volume after narration
            GameAudioManager.Instance?.UnduckBGM();

            // Fade in continue button after narration finishes
            yield return new WaitForSeconds(0.4f);
            yield return FadeCanvasGroup(continueButtonGroup, 0f, 1f, 0.7f);
            continueButtonGroup.blocksRaycasts = true;
            continueButtonGroup.interactable = true;

            if (continueButtonRect != null)
                StartCoroutine(BobAnimation(continueButtonRect));
        }

        private void OnContinueClicked()
        {
            if (transitioning) return;
            StartCoroutine(CrossFade(introScreen, mapScreen));
        }

        private void OnGoClicked()
        {
            if (transitioning) return;
            if (OnComplete != null) { OnComplete(); return; }
            NavigationHelper.LoadScene("TotalFractions2DemoWithBG");
        }

        private IEnumerator CrossFade(CanvasGroup from, CanvasGroup to)
        {
            transitioning = true;
            from.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < FADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FADE_DURATION);
                from.alpha = 1f - t;
                to.alpha = t;
                yield return null;
            }

            from.alpha = 0f;
            to.alpha = 1f;
            to.blocksRaycasts = true;
            transitioning = false;
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
        }

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

        private IEnumerator KnightEntrance()
        {
            if (knightRect == null || knightImage == null)
            {
                Debug.LogWarning("[Intro3] knightRect or knightImage is null — skipping knight animations");
                yield return new WaitForSeconds(0.8f);
                yield break;
            }

            Debug.Log("[Intro3] Knight entrance animation starting");

            // Fade in + slide up 50px over 0.8s
            Vector2 targetPos = knightRect.anchoredPosition;
            Vector2 startPos = targetPos + new Vector2(0, -50f);
            knightRect.anchoredPosition = startPos;
            knightImage.color = new Color(1f, 1f, 1f, 0f);

            float elapsed = 0f;
            float duration = 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                knightImage.color = new Color(1f, 1f, 1f, t);
                knightRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            knightImage.color = Color.white;
            knightRect.anchoredPosition = targetPos;

            StartCoroutine(KnightIdleAnimation());
        }

        private IEnumerator KnightIdleAnimation()
        {
            Vector2 originalPos = knightRect.anchoredPosition;
            Vector3 originalScale = knightRect.localScale;
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                // Breathing: scale 1.0 → 1.008 → 1.0, 5s period
                float breathScale = 1f + 0.008f * Mathf.Sin(elapsed * (2f * Mathf.PI / 5f));
                knightRect.localScale = originalScale * breathScale;
                // Bob: ±2px vertical, 4s period, phase-offset from breathing
                float bob = 2f * Mathf.Sin(elapsed * (2f * Mathf.PI / 4f) + Mathf.PI * 0.5f);
                knightRect.anchoredPosition = new Vector2(originalPos.x, originalPos.y + bob);
                yield return null;
            }
        }
    }
}
