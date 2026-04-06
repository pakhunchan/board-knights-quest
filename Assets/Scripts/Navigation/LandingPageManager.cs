using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.Navigation
{
    /// <summary>
    /// Landing page runtime: shows game cards, handles piece dwell + finger tap to choose a game.
    /// </summary>
    public class LandingPageManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private RectTransform nullifyCard;
        [SerializeField] private RectTransform playgroundCard;
        [SerializeField] private Button nullifyButton;
        [SerializeField] private Button playgroundButton;
        [SerializeField] private TextMeshProUGUI subtitleText;

        // Piece dwell tracking
        private float dwellOnNullify = -1f;
        private float dwellOnPlayground = -1f;
        private const float DWELL_TIME = 1.0f;

        // Visual feedback
        private Image nullifyCardImage;
        private Image playgroundCardImage;
        private Color nullifyBaseColor;
        private Color playgroundBaseColor;

        private void Start()
        {
            if (nullifyButton != null)
                nullifyButton.onClick.AddListener(OnNullifyClicked);
            if (playgroundButton != null)
                playgroundButton.onClick.AddListener(OnPlaygroundClicked);

            if (nullifyCard != null)
                nullifyCardImage = nullifyCard.GetComponent<Image>();
            if (playgroundCard != null)
                playgroundCardImage = playgroundCard.GetComponent<Image>();

            if (nullifyCardImage != null)
                nullifyBaseColor = nullifyCardImage.color;
            if (playgroundCardImage != null)
                playgroundBaseColor = playgroundCardImage.color;
        }

        private void Update()
        {
            if (PieceManager.Instance == null) return;

            // Check any active piece for dwell interaction
            PieceManager.PieceContact? firstPiece = null;
            foreach (var kvp in PieceManager.Instance.ActivePieces)
            {
                firstPiece = kvp.Value;
                break;
            }

            if (firstPiece == null)
            {
                ResetDwells();
                return;
            }

            Vector2 screenPos = firstPiece.Value.screenPosition;

            // Check Nullify card
            if (nullifyCard != null && NavigationHelper.IsOverRect(nullifyCard, screenPos, 30f))
            {
                dwellOnPlayground = -1f;
                if (dwellOnNullify < 0f) dwellOnNullify = 0f;
                dwellOnNullify += Time.deltaTime;

                // Visual feedback — lerp toward brighter
                if (nullifyCardImage != null)
                    nullifyCardImage.color = Color.Lerp(nullifyCardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Nullify... ({Mathf.CeilToInt(DWELL_TIME - dwellOnNullify)}s)";

                if (dwellOnNullify >= DWELL_TIME)
                {
                    dwellOnNullify = -1f;
                    OnNullifyClicked();
                }
            }
            // Check Playground card
            else if (playgroundCard != null && NavigationHelper.IsOverRect(playgroundCard, screenPos, 30f))
            {
                dwellOnNullify = -1f;
                if (dwellOnPlayground < 0f) dwellOnPlayground = 0f;
                dwellOnPlayground += Time.deltaTime;

                if (playgroundCardImage != null)
                    playgroundCardImage.color = Color.Lerp(playgroundCardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Pak's Playground... ({Mathf.CeilToInt(DWELL_TIME - dwellOnPlayground)}s)";

                if (dwellOnPlayground >= DWELL_TIME)
                {
                    dwellOnPlayground = -1f;
                    OnPlaygroundClicked();
                }
            }
            else
            {
                ResetDwells();
            }
        }

        private void ResetDwells()
        {
            dwellOnNullify = -1f;
            dwellOnPlayground = -1f;

            if (nullifyCardImage != null)
                nullifyCardImage.color = Color.Lerp(nullifyCardImage.color, nullifyBaseColor, Time.deltaTime * 5f);
            if (playgroundCardImage != null)
                playgroundCardImage.color = Color.Lerp(playgroundCardImage.color, playgroundBaseColor, Time.deltaTime * 5f);

            if (subtitleText != null)
                subtitleText.text = "Place a piece or tap to choose";
        }

        private void OnNullifyClicked()
        {
            NavigationHelper.LoadScene("Nullify");
        }

        private void OnPlaygroundClicked()
        {
            NavigationHelper.LoadScene("Playground");
        }
    }
}
