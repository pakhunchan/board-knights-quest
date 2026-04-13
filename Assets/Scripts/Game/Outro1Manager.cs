using UnityEngine;
using System.Collections;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Outro1 scene: knight farewell with karaoke-style subtitle.
    /// Auto-plays on Start — fades in, then highlights words one at a time.
    /// </summary>
    public class Outro1Manager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup screenGroup;

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
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            // Brief pause before starting narration
            yield return new WaitForSeconds(PRE_SUBTITLE_DELAY);

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
        }
    }
}
