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
        [SerializeField] private RectTransform circlesCard;
        [SerializeField] private RectTransform fractionsCard;
        [SerializeField] private RectTransform playgroundCard;
        [SerializeField] private Button circlesButton;
        [SerializeField] private Button fractionsButton;
        [SerializeField] private Button playgroundButton;
        [SerializeField] private TextMeshProUGUI subtitleText;

        // Piece dwell tracking
        private float dwellOnCircles = -1f;
        private float dwellOnFractions = -1f;
        private float dwellOnPlayground = -1f;
        private const float DWELL_TIME = 1.0f;

        // Visual feedback
        private Image circlesCardImage;
        private Image fractionsCardImage;
        private Image playgroundCardImage;
        private Color circlesBaseColor;
        private Color fractionsBaseColor;
        private Color playgroundBaseColor;

        private void Start()
        {
            if (circlesButton != null)
                circlesButton.onClick.AddListener(OnCirclesClicked);
            if (fractionsButton != null)
                fractionsButton.onClick.AddListener(OnFractionsClicked);
            if (playgroundButton != null)
                playgroundButton.onClick.AddListener(OnPlaygroundClicked);

            if (circlesCard != null)
                circlesCardImage = circlesCard.GetComponent<Image>();
            if (fractionsCard != null)
                fractionsCardImage = fractionsCard.GetComponent<Image>();
            if (playgroundCard != null)
                playgroundCardImage = playgroundCard.GetComponent<Image>();

            if (circlesCardImage != null)
                circlesBaseColor = circlesCardImage.color;
            if (fractionsCardImage != null)
                fractionsBaseColor = fractionsCardImage.color;
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

            // Check Circles card
            if (circlesCard != null && NavigationHelper.IsOverRect(circlesCard, screenPos, 30f))
            {
                dwellOnFractions = -1f;
                dwellOnPlayground = -1f;
                if (dwellOnCircles < 0f) dwellOnCircles = 0f;
                dwellOnCircles += Time.deltaTime;

                if (circlesCardImage != null)
                    circlesCardImage.color = Color.Lerp(circlesCardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Circles... ({Mathf.CeilToInt(DWELL_TIME - dwellOnCircles)}s)";

                if (dwellOnCircles >= DWELL_TIME)
                {
                    dwellOnCircles = -1f;
                    OnCirclesClicked();
                }
            }
            // Check Fractions card
            else if (fractionsCard != null && NavigationHelper.IsOverRect(fractionsCard, screenPos, 30f))
            {
                dwellOnCircles = -1f;
                dwellOnPlayground = -1f;
                if (dwellOnFractions < 0f) dwellOnFractions = 0f;
                dwellOnFractions += Time.deltaTime;

                if (fractionsCardImage != null)
                    fractionsCardImage.color = Color.Lerp(fractionsCardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Fractions... ({Mathf.CeilToInt(DWELL_TIME - dwellOnFractions)}s)";

                if (dwellOnFractions >= DWELL_TIME)
                {
                    dwellOnFractions = -1f;
                    OnFractionsClicked();
                }
            }
            // Check Playground card
            else if (playgroundCard != null && NavigationHelper.IsOverRect(playgroundCard, screenPos, 30f))
            {
                dwellOnCircles = -1f;
                dwellOnFractions = -1f;
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
            dwellOnCircles = -1f;
            dwellOnFractions = -1f;
            dwellOnPlayground = -1f;

            if (circlesCardImage != null)
                circlesCardImage.color = Color.Lerp(circlesCardImage.color, circlesBaseColor, Time.deltaTime * 5f);
            if (fractionsCardImage != null)
                fractionsCardImage.color = Color.Lerp(fractionsCardImage.color, fractionsBaseColor, Time.deltaTime * 5f);
            if (playgroundCardImage != null)
                playgroundCardImage.color = Color.Lerp(playgroundCardImage.color, playgroundBaseColor, Time.deltaTime * 5f);

            if (subtitleText != null)
                subtitleText.text = "Place a piece or tap to choose";
        }

        private void OnCirclesClicked()
        {
            NavigationHelper.LoadScene("Circles");
        }

        private void OnFractionsClicked()
        {
            NavigationHelper.LoadScene("FractionsDemo");
        }

        private void OnPlaygroundClicked()
        {
            NavigationHelper.LoadScene("Playground");
        }
    }
}
