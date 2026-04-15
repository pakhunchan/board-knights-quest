using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using BoardOfEducation.Audio;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Outro1 scene: knight farewell with karaoke-style subtitle.
    /// Auto-plays on Start — fades in, then highlights words one at a time.
    /// After narration, shows a continue button that navigates to LevelMap1.
    /// </summary>
    public class Outro1Manager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup screenGroup;
        [SerializeField] private Button continueButton;
        [SerializeField] private CanvasGroup continueButtonGroup;
        [SerializeField] private RectTransform knightRect;
        [SerializeField] private Image knightImage;

        public System.Action OnComplete;

        private TTSAudioProvider ttsProvider;

        private const float PRE_SUBTITLE_DELAY = 0.8f;
        private const float PER_WORD_DELAY = 0.25f;

        private const string FAREWELL_TEXT =
            "You did it, brave knight! Every math challenge you conquer makes you " +
            "sharper, stronger, and one step closer to becoming a true Math Knight\u2014" +
            "so return soon for your next adventure.";

        private void Start()
        {
            screenGroup.alpha = 1f;
            screenGroup.blocksRaycasts = true;
            subtitleText.text = "";

            if (knightImage != null)
                knightImage.color = new Color(1f, 1f, 1f, 0f);

            // Hide continue button until narration finishes
            if (continueButtonGroup != null)
            {
                continueButtonGroup.alpha = 0f;
                continueButtonGroup.blocksRaycasts = false;
                continueButtonGroup.gameObject.SetActive(false);
            }
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() =>
                {
                    if (OnComplete != null) { OnComplete(); return; }
                    NavigationHelper.LoadScene("LevelMap1");
                });
            }

            ttsProvider = gameObject.GetComponent<TTSAudioProvider>()
                ?? gameObject.AddComponent<TTSAudioProvider>();

            GameAudioManager.Instance?.PlayBGM();

            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            // Knight entrance animation (or brief pause if not wired)
            yield return KnightEntrance();

            // Fade in subtitle
            float fadeTime = 0.25f;
            float elapsed = 0f;
            subtitleText.text = FAREWELL_TEXT;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 1f;

            // Duck BGM during TTS narration
            GameAudioManager.Instance?.DuckBGM();

            // Play TTS audio in parallel with karaoke highlighting
            StartCoroutine(ttsProvider.SpeakCoroutine(FAREWELL_TEXT, null));

            // Karaoke: highlight one word at a time
            string[] words = FAREWELL_TEXT.Split(' ');
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
                yield return new WaitForSeconds(PER_WORD_DELAY);
            }

            // Restore plain text — leave visible as farewell
            subtitleText.text = FAREWELL_TEXT;

            // Restore BGM volume after narration
            GameAudioManager.Instance?.UnduckBGM();

            // Show continue button with fade-in
            if (continueButtonGroup != null)
            {
                continueButtonGroup.gameObject.SetActive(true);
                continueButtonGroup.alpha = 0f;
                float btnFade = 0.4f;
                float btnElapsed = 0f;
                while (btnElapsed < btnFade)
                {
                    btnElapsed += Time.deltaTime;
                    continueButtonGroup.alpha = Mathf.Clamp01(btnElapsed / btnFade);
                    yield return null;
                }
                continueButtonGroup.alpha = 1f;
                continueButtonGroup.blocksRaycasts = true;
            }
        }

        private IEnumerator KnightEntrance()
        {
            if (knightRect == null || knightImage == null)
            {
                Debug.LogWarning("[Outro1] knightRect or knightImage is null — skipping knight animations");
                yield return new WaitForSeconds(PRE_SUBTITLE_DELAY);
                yield break;
            }

            Debug.Log("[Outro1] Knight entrance animation starting");

            // Fade in + scale punch (1.0 → 1.08 → 1.0) over 0.7s
            knightImage.color = new Color(1f, 1f, 1f, 0f);
            Vector3 originalScale = knightRect.localScale;

            float elapsed = 0f;
            float duration = 0.7f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Fade in with SmoothStep
                knightImage.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, t));
                // Scale punch: ramp up first half, settle second half
                float scale = t < 0.5f
                    ? Mathf.Lerp(1f, 1.08f, t / 0.5f)
                    : Mathf.Lerp(1.08f, 1f, (t - 0.5f) / 0.5f);
                knightRect.localScale = originalScale * scale;
                yield return null;
            }
            knightImage.color = Color.white;
            knightRect.localScale = originalScale;

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
