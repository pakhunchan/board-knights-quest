using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private RectTransform fractions3Card;
        [SerializeField] private RectTransform fractions4Card;
        [SerializeField] private RectTransform fractions5Card;
        [SerializeField] private RectTransform totalFractions2Card;
        [SerializeField] private Button circlesButton;
        [SerializeField] private Button fractionsButton;
        [SerializeField] private Button playgroundButton;
        [SerializeField] private Button fractions3Button;
        [SerializeField] private Button fractions4Button;
        [SerializeField] private Button fractions5Button;
        [SerializeField] private Button totalFractions2Button;
        [SerializeField] private TextMeshProUGUI subtitleText;

        // Piece dwell tracking
        private float dwellOnCircles = -1f;
        private float dwellOnFractions = -1f;
        private float dwellOnPlayground = -1f;
        private float dwellOnFractions3 = -1f;
        private float dwellOnFractions4 = -1f;
        private float dwellOnFractions5 = -1f;
        private float dwellOnTotalFractions2 = -1f;
        private const float DWELL_TIME = 1.0f;

        // Visual feedback
        private Image circlesCardImage;
        private Image fractionsCardImage;
        private Image playgroundCardImage;
        private Image fractions3CardImage;
        private Image fractions4CardImage;
        private Image fractions5CardImage;
        private Image totalFractions2CardImage;
        private Color circlesBaseColor;
        private Color fractionsBaseColor;
        private Color playgroundBaseColor;
        private Color fractions3BaseColor;
        private Color fractions4BaseColor;
        private Color fractions5BaseColor;
        private Color totalFractions2BaseColor;

        private void Start()
        {
            if (circlesButton != null)
                circlesButton.onClick.AddListener(OnCirclesClicked);
            if (fractionsButton != null)
                fractionsButton.onClick.AddListener(OnFractionsClicked);
            if (playgroundButton != null)
                playgroundButton.onClick.AddListener(OnPlaygroundClicked);
            if (fractions3Button != null)
                fractions3Button.onClick.AddListener(OnFractions3Clicked);
            if (fractions4Button != null)
                fractions4Button.onClick.AddListener(OnFractions4Clicked);
            if (fractions5Button != null)
                fractions5Button.onClick.AddListener(OnFractions5Clicked);
            if (totalFractions2Button != null)
                totalFractions2Button.onClick.AddListener(OnTotalFractions2Clicked);

            if (circlesCard != null)
                circlesCardImage = circlesCard.GetComponent<Image>();
            if (fractionsCard != null)
                fractionsCardImage = fractionsCard.GetComponent<Image>();
            if (playgroundCard != null)
                playgroundCardImage = playgroundCard.GetComponent<Image>();
            if (fractions3Card != null)
                fractions3CardImage = fractions3Card.GetComponent<Image>();
            if (fractions4Card != null)
                fractions4CardImage = fractions4Card.GetComponent<Image>();
            if (fractions5Card != null)
                fractions5CardImage = fractions5Card.GetComponent<Image>();
            if (totalFractions2Card != null)
                totalFractions2CardImage = totalFractions2Card.GetComponent<Image>();

            if (circlesCardImage != null)
                circlesBaseColor = circlesCardImage.color;
            if (fractionsCardImage != null)
                fractionsBaseColor = fractionsCardImage.color;
            if (playgroundCardImage != null)
                playgroundBaseColor = playgroundCardImage.color;
            if (fractions3CardImage != null)
                fractions3BaseColor = fractions3CardImage.color;
            if (fractions4CardImage != null)
                fractions4BaseColor = fractions4CardImage.color;
            if (fractions5CardImage != null)
                fractions5BaseColor = fractions5CardImage.color;
            if (totalFractions2CardImage != null)
                totalFractions2BaseColor = totalFractions2CardImage.color;
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
                dwellOnFractions3 = -1f;
                dwellOnFractions4 = -1f;
                dwellOnFractions5 = -1f;
                dwellOnTotalFractions2 = -1f;
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
                dwellOnFractions3 = -1f;
                dwellOnFractions4 = -1f;
                dwellOnFractions5 = -1f;
                dwellOnTotalFractions2 = -1f;
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
                dwellOnFractions3 = -1f;
                dwellOnFractions4 = -1f;
                dwellOnFractions5 = -1f;
                dwellOnTotalFractions2 = -1f;
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
            // Check Fractions 3 card
            else if (fractions3Card != null && NavigationHelper.IsOverRect(fractions3Card, screenPos, 30f))
            {
                dwellOnCircles = -1f;
                dwellOnFractions = -1f;
                dwellOnPlayground = -1f;
                dwellOnFractions4 = -1f;
                dwellOnFractions5 = -1f;
                dwellOnTotalFractions2 = -1f;
                if (dwellOnFractions3 < 0f) dwellOnFractions3 = 0f;
                dwellOnFractions3 += Time.deltaTime;

                if (fractions3CardImage != null)
                    fractions3CardImage.color = Color.Lerp(fractions3CardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Interactive... ({Mathf.CeilToInt(DWELL_TIME - dwellOnFractions3)}s)";

                if (dwellOnFractions3 >= DWELL_TIME)
                {
                    dwellOnFractions3 = -1f;
                    OnFractions3Clicked();
                }
            }
            // Check Fractions 4 card
            else if (fractions4Card != null && NavigationHelper.IsOverRect(fractions4Card, screenPos, 30f))
            {
                dwellOnCircles = -1f;
                dwellOnFractions = -1f;
                dwellOnPlayground = -1f;
                dwellOnFractions3 = -1f;
                dwellOnFractions5 = -1f;
                dwellOnTotalFractions2 = -1f;
                if (dwellOnFractions4 < 0f) dwellOnFractions4 = 0f;
                dwellOnFractions4 += Time.deltaTime;

                if (fractions4CardImage != null)
                    fractions4CardImage.color = Color.Lerp(fractions4CardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Fractions 4... ({Mathf.CeilToInt(DWELL_TIME - dwellOnFractions4)}s)";

                if (dwellOnFractions4 >= DWELL_TIME)
                {
                    dwellOnFractions4 = -1f;
                    OnFractions4Clicked();
                }
            }
            // Check Practice card
            else if (fractions5Card != null && NavigationHelper.IsOverRect(fractions5Card, screenPos, 30f))
            {
                dwellOnCircles = -1f;
                dwellOnFractions = -1f;
                dwellOnPlayground = -1f;
                dwellOnFractions3 = -1f;
                dwellOnFractions4 = -1f;
                dwellOnTotalFractions2 = -1f;
                if (dwellOnFractions5 < 0f) dwellOnFractions5 = 0f;
                dwellOnFractions5 += Time.deltaTime;

                if (fractions5CardImage != null)
                    fractions5CardImage.color = Color.Lerp(fractions5CardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Practice... ({Mathf.CeilToInt(DWELL_TIME - dwellOnFractions5)}s)";

                if (dwellOnFractions5 >= DWELL_TIME)
                {
                    dwellOnFractions5 = -1f;
                    OnFractions5Clicked();
                }
            }
            // Check Total Fractions 2 card
            else if (totalFractions2Card != null && NavigationHelper.IsOverRect(totalFractions2Card, screenPos, 30f))
            {
                dwellOnCircles = -1f;
                dwellOnFractions = -1f;
                dwellOnPlayground = -1f;
                dwellOnFractions3 = -1f;
                dwellOnFractions4 = -1f;
                dwellOnFractions5 = -1f;
                if (dwellOnTotalFractions2 < 0f) dwellOnTotalFractions2 = 0f;
                dwellOnTotalFractions2 += Time.deltaTime;

                if (totalFractions2CardImage != null)
                    totalFractions2CardImage.color = Color.Lerp(totalFractions2CardImage.color, Color.white, Time.deltaTime * 3f);

                if (subtitleText != null)
                    subtitleText.text = $"Hold to select Full Lesson... ({Mathf.CeilToInt(DWELL_TIME - dwellOnTotalFractions2)}s)";

                if (dwellOnTotalFractions2 >= DWELL_TIME)
                {
                    dwellOnTotalFractions2 = -1f;
                    OnTotalFractions2Clicked();
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
            dwellOnFractions3 = -1f;
            dwellOnFractions4 = -1f;
            dwellOnFractions5 = -1f;
            dwellOnTotalFractions2 = -1f;

            if (circlesCardImage != null)
                circlesCardImage.color = Color.Lerp(circlesCardImage.color, circlesBaseColor, Time.deltaTime * 5f);
            if (fractionsCardImage != null)
                fractionsCardImage.color = Color.Lerp(fractionsCardImage.color, fractionsBaseColor, Time.deltaTime * 5f);
            if (playgroundCardImage != null)
                playgroundCardImage.color = Color.Lerp(playgroundCardImage.color, playgroundBaseColor, Time.deltaTime * 5f);
            if (fractions3CardImage != null)
                fractions3CardImage.color = Color.Lerp(fractions3CardImage.color, fractions3BaseColor, Time.deltaTime * 5f);
            if (fractions4CardImage != null)
                fractions4CardImage.color = Color.Lerp(fractions4CardImage.color, fractions4BaseColor, Time.deltaTime * 5f);
            if (fractions5CardImage != null)
                fractions5CardImage.color = Color.Lerp(fractions5CardImage.color, fractions5BaseColor, Time.deltaTime * 5f);
            if (totalFractions2CardImage != null)
                totalFractions2CardImage.color = Color.Lerp(totalFractions2CardImage.color, totalFractions2BaseColor, Time.deltaTime * 5f);

            if (subtitleText != null)
                subtitleText.text = "Place a piece or tap to choose";
        }

        private void OnCirclesClicked()
        {
            NavigationHelper.LoadScene("Circles");
        }

        private void OnFractionsClicked()
        {
            NavigationHelper.LoadScene("TotalFractionsDemo");
        }

        private void OnPlaygroundClicked()
        {
            NavigationHelper.LoadScene("Playground");
        }

        private void OnFractions3Clicked()
        {
            NavigationHelper.LoadScene("FractionsDemo3");
        }

        private void OnFractions4Clicked()
        {
            NavigationHelper.LoadScene("FractionsDemo4");
        }

        private void OnFractions5Clicked()
        {
            NavigationHelper.LoadScene("FractionsDemo5");
        }

        private void OnTotalFractions2Clicked()
        {
            NavigationHelper.LoadScene("TotalFractions2");
        }
    }
}
