using UnityEngine;
using System.Collections;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Orchestrates the chalkboard intro fade followed by the TotalFractions2 lesson.
    /// The chalkboard layers fade in first, then lesson content fades in and the lesson starts.
    /// </summary>
    public class TotalFractions2DemoWithBGManager : MonoBehaviour
    {
        [SerializeField] private ChalkboardDemoManager chalkboardManager;
        [SerializeField] private TotalFractions2Manager lessonManager;
        [SerializeField] private CanvasGroup contentGroup;

        private void Start()
        {
            chalkboardManager.OnFadeComplete += OnChalkboardFadeComplete;
        }

        private void OnChalkboardFadeComplete()
        {
            chalkboardManager.OnFadeComplete -= OnChalkboardFadeComplete;
            StartCoroutine(StartLesson());
        }

        private IEnumerator StartLesson()
        {
            // Fade in the lesson content area
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                contentGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            contentGroup.alpha = 1f;

            // Enable the lesson manager — Unity will call its Start() this frame
            lessonManager.enabled = true;
        }
    }
}
