using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using BoardOfEducation.Input;

namespace BoardOfEducation.Navigation
{
    /// <summary>
    /// Title screen: detects a robot piece placed on the play zone,
    /// transitions hexagon from grey to green, then loads LandingPage after 1s dwell.
    /// Ships and non-robot pieces are ignored.
    /// </summary>
    public class TitleScreenManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform playZone;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Image greenOverlay; // green hexagon on top, fades in

        private Image playZoneImage; // grey hexagon (base)
        private float dwellTimer = -1f;
        private bool isTransitioning;

        private const float DWELL_TIME = 1.0f;

        private void Start()
        {
            if (playZone != null)
                playZoneImage = playZone.GetComponent<Image>();

            // Start with green overlay fully transparent (grey showing)
            if (greenOverlay != null)
                greenOverlay.color = new Color(1, 1, 1, 0f);
        }

        private void Update()
        {
            if (isTransitioning) return;
            if (PieceManager.Instance == null) return;

            // Find first active piece
            PieceManager.PieceContact? firstPiece = null;
            foreach (var kvp in PieceManager.Instance.ActivePieces)
            {
                firstPiece = kvp.Value;
                break;
            }

            if (firstPiece == null)
            {
                ResetPlayZone();
                return;
            }

            var piece = firstPiece.Value;

            // Only react to robot pieces (glyph IDs 0-3)
            if (piece.glyphId < PieceManager.ArcadeGlyphs.RobotYellow ||
                piece.glyphId > PieceManager.ArcadeGlyphs.RobotPink)
            {
                ResetPlayZone();
                return;
            }

            if (playZone != null && NavigationHelper.IsOverRect(playZone, piece.screenPosition, 40f))
            {
                if (dwellTimer < 0f) dwellTimer = 0f;
                dwellTimer += Time.deltaTime;

                // Fade in green overlay over grey base
                float progress = Mathf.Clamp01(dwellTimer / DWELL_TIME);
                if (greenOverlay != null)
                    greenOverlay.color = new Color(1, 1, 1, progress);

                if (subtitleText != null)
                    subtitleText.text = "Hold to start...";

                if (dwellTimer >= DWELL_TIME)
                {
                    isTransitioning = true;
                    StartCoroutine(CoTransition());
                }
            }
            else
            {
                ResetPlayZone();
            }
        }

        private IEnumerator CoTransition()
        {
            // Brief bright flash
            if (greenOverlay != null)
                greenOverlay.color = Color.white;

            yield return new WaitForSeconds(0.15f);
            yield return new WaitForSeconds(0.3f);

            NavigationHelper.LoadScene("LandingPage");
        }

        private void ResetPlayZone()
        {
            dwellTimer = -1f;

            // Quickly fade green overlay back to transparent (revealing grey)
            if (greenOverlay != null)
            {
                float a = Mathf.Lerp(greenOverlay.color.a, 0f, Time.deltaTime * 8f);
                greenOverlay.color = new Color(1, 1, 1, a);
            }

            if (subtitleText != null)
                subtitleText.text = "Place a robot on the board to begin!";
        }
    }
}
