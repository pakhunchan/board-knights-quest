using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages the Intro2 scene: Native title screen → Level map screen.
    /// Screen 1 uses sprite assets (hex play zone + robots), Screen 2 is the level map.
    /// PLAY button updates subtitle to prompt robot placement; placing a robot proceeds.
    /// </summary>
    public class Intro2Manager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private CanvasGroup mapScreen;
        [SerializeField] private Button playButton;
        [SerializeField] private Button goButton;
        [SerializeField] private TextMeshProUGUI subtitleText;

        public System.Action OnComplete;

        private const float FADE_DURATION = 0.6f;
        private bool transitioning;
        private bool mapRevealed;
        private bool subscribedToPieces;

        private void Start()
        {
            // Title visible, map hidden
            titleScreen.alpha = 1f;
            titleScreen.blocksRaycasts = true;
            mapScreen.alpha = 0f;
            mapScreen.blocksRaycasts = false;

            playButton.onClick.AddListener(OnPlayClicked);
            goButton.onClick.AddListener(OnGoClicked);

            // Subscribe here instead of OnEnable — guarantees PieceManager.Instance
            // is set (all Awake calls complete before any Start call).
            SubscribeToPieces();
        }

        private void OnDisable()
        {
            UnsubscribeFromPieces();
        }

        private void SubscribeToPieces()
        {
            if (subscribedToPieces) return;
            if (Input.PieceManager.Instance != null)
            {
                Input.PieceManager.Instance.OnPiecePlaced += OnRobotPlaced;
                subscribedToPieces = true;
            }
        }

        private void UnsubscribeFromPieces()
        {
            if (!subscribedToPieces) return;
            if (Input.PieceManager.Instance != null)
                Input.PieceManager.Instance.OnPiecePlaced -= OnRobotPlaced;
            subscribedToPieces = false;
        }

        private void OnPlayClicked()
        {
            if (transitioning || mapRevealed) return;
            mapRevealed = true;
            StartCoroutine(CrossFade(titleScreen, mapScreen));
        }

        private void OnRobotPlaced(Input.PieceManager.PieceContact piece)
        {
            if (transitioning || mapRevealed) return;
            mapRevealed = true;
            StartCoroutine(CrossFade(titleScreen, mapScreen));
        }

        private void OnGoClicked()
        {
            if (transitioning) return;
            if (OnComplete != null) { OnComplete(); return; }
            NavigationHelper.LoadScene("Intro3");
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
