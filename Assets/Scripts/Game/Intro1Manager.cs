using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Intro1 scene: Menu title screen → Level map screen.
    /// Each screen has a "Next" button that cross-fades to the next screen.
    /// The level map button advances to the main game scene.
    /// </summary>
    public class Intro1Manager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup menuScreen;
        [SerializeField] private CanvasGroup mapScreen;
        [SerializeField] private Button menuNextButton;
        [SerializeField] private Button mapNextButton;

        private const float FADE_DURATION = 0.6f;
        private bool transitioning;

        private void Start()
        {
            // Menu visible, map hidden
            menuScreen.alpha = 1f;
            menuScreen.blocksRaycasts = true;
            mapScreen.alpha = 0f;
            mapScreen.blocksRaycasts = false;

            menuNextButton.onClick.AddListener(OnMenuNext);
            mapNextButton.onClick.AddListener(OnMapNext);
        }

        private void OnMenuNext()
        {
            if (transitioning) return;
            StartCoroutine(CrossFade(menuScreen, mapScreen));
        }

        private void OnMapNext()
        {
            if (transitioning) return;
            // Advance to the main lesson scene
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
