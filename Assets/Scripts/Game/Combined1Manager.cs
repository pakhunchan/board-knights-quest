using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Combined1 scene: Title screen → Level map → Lesson.
    /// Cross-fades between 3 CanvasGroup screens, then hands off to the
    /// existing chalkboard + lesson orchestration.
    /// </summary>
    public class Combined1Manager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private CanvasGroup mapScreen;
        [SerializeField] private CanvasGroup lessonScreen;
        [SerializeField] private Button playButton;
        [SerializeField] private Button goButton;
        [SerializeField] private ChalkboardDemoManager chalkboardManager;
        [SerializeField] private TotalFractions2DemoWithBGManager orchestrator;

        private const float FADE_DURATION = 0.6f;
        private bool transitioning;

        private void Start()
        {
            // Title visible, others hidden
            SetScreen(titleScreen, true);
            SetScreen(mapScreen, false);
            SetScreen(lessonScreen, false);

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
            StartCoroutine(TransitionToLesson());
        }

        private IEnumerator TransitionToLesson()
        {
            yield return StartCoroutine(CrossFade(mapScreen, lessonScreen));

            // Now that the lesson screen is visible, kick off the chalkboard fade + lesson
            chalkboardManager.enabled = true;
            orchestrator.enabled = true;
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

        private static void SetScreen(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
        }
    }
}
