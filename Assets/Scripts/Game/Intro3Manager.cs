using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
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
        [SerializeField] private Button goButton;

        private const float FADE_DURATION = 0.6f;
        private const float BASE_WORD_DURATION = 0.28f;
        private const float PER_CHAR_DURATION = 0.03f;
        private const float PAUSE_AFTER_COMMA = 0.15f;
        private const float PAUSE_AFTER_SENTENCE = 0.35f;
        private const string HIGHLIGHT_COLOR = "#E63946";

        private bool transitioning;

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

            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueClicked);
            goButton.onClick.AddListener(OnGoClicked);

            StartCoroutine(PlaySubtitles());
        }

        private IEnumerator PlaySubtitles()
        {
            string[] words = Script.Split(' ');
            subtitleText.text = "";

            // Brief pause before starting
            yield return new WaitForSeconds(0.8f);

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

            // Show continue button after narration finishes
            yield return new WaitForSeconds(0.4f);
            continueButton.gameObject.SetActive(true);
        }

        private void OnContinueClicked()
        {
            if (transitioning) return;
            StartCoroutine(CrossFade(introScreen, mapScreen));
        }

        private void OnGoClicked()
        {
            if (transitioning) return;
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
    }
}
