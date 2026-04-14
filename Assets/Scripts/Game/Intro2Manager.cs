using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Intro2 scene: Native title screen → Level map screen.
    /// Screen 1 uses sprite assets (hex play zone + robots), Screen 2 is the level map.
    /// Button clicks cross-fade between screens; the map button loads the lesson scene.
    /// </summary>
    public class Intro2Manager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private CanvasGroup mapScreen;
        [SerializeField] private Button playButton;
        [SerializeField] private Button goButton;

        private const float FADE_DURATION = 0.6f;
        private bool transitioning;

        private void Start()
        {
            // Title visible, map hidden
            titleScreen.alpha = 1f;
            titleScreen.blocksRaycasts = true;
            mapScreen.alpha = 0f;
            mapScreen.blocksRaycasts = false;

            playButton.onClick.AddListener(OnPlayClicked);
            goButton.onClick.AddListener(OnGoClicked);
        }

        private void OnPlayClicked()
        {
            if (transitioning) return;
            StartCoroutine(CrossFade(titleScreen, mapScreen));
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
