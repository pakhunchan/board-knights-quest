using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Plays a "Gentle Fade" animation that fades in chalkboard layers sequentially.
    /// Attach to a GameCore object; wire up the 4 overlay Images via the inspector
    /// (or let ChalkboardDemoSceneBuilder do it).
    /// </summary>
    public class ChalkboardDemoManager : MonoBehaviour
    {
        [SerializeField] private Image[] layers;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float delayBetween = 0.3f;

        private void Start()
        {
            // Ensure all layers start fully transparent
            foreach (var img in layers)
            {
                if (img == null) continue;
                var c = img.color;
                c.a = 0f;
                img.color = c;
            }

            StartCoroutine(PlayGentleFade());
        }

        public event System.Action OnFadeComplete;

        private IEnumerator PlayGentleFade()
        {
            // Small initial delay so the background is visible first
            yield return new WaitForSeconds(0.5f);

            foreach (var img in layers)
            {
                if (img == null) continue;
                yield return StartCoroutine(FadeIn(img, fadeDuration));
                yield return new WaitForSeconds(delayBetween);
            }

            OnFadeComplete?.Invoke();
        }

        private IEnumerator FadeIn(Image img, float duration)
        {
            float elapsed = 0f;
            Color startColor = img.color;
            Color endColor = startColor;
            endColor.a = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                img.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            img.color = endColor;
        }
    }
}
