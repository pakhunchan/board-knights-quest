using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.UI
{
    /// <summary>
    /// Renders visual representations of placed Strata blocks on the game board.
    /// Each physical piece gets a corresponding on-screen visual that moves with it.
    /// </summary>
    public class PieceVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private GameObject pieceVisualPrefab;
        [SerializeField] private Color[] blockColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f, 0.8f),  // Blue
            new Color(0.9f, 0.4f, 0.3f, 0.8f),  // Red
            new Color(0.3f, 0.8f, 0.4f, 0.8f),  // Green
            new Color(0.9f, 0.7f, 0.2f, 0.8f),  // Yellow
            new Color(0.7f, 0.3f, 0.9f, 0.8f),  // Purple
            new Color(1.0f, 0.5f, 0.0f, 0.8f),  // Orange
            new Color(0.0f, 0.8f, 0.8f, 0.8f),  // Cyan
            new Color(0.9f, 0.3f, 0.6f, 0.8f),  // Pink
            new Color(0.5f, 0.5f, 0.9f, 0.8f),  // Indigo
            new Color(0.6f, 0.9f, 0.3f, 0.8f),  // Lime
        };

        // Track visual representations of active pieces
        private Dictionary<int, GameObject> pieceVisuals = new Dictionary<int, GameObject>();
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;

            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPiecePlaced += OnPiecePlaced;
                PieceManager.Instance.OnPieceMoved += OnPieceMoved;
                PieceManager.Instance.OnPieceRemoved += OnPieceRemoved;
                PieceManager.Instance.OnPieceTouched += OnPieceTouched;
                PieceManager.Instance.OnPieceReleased += OnPieceReleased;
            }
        }

        private void OnPiecePlaced(PieceManager.PieceContact contact)
        {
            if (pieceVisualPrefab == null)
            {
                CreateDefaultVisual(contact);
                return;
            }

            var visual = Instantiate(pieceVisualPrefab, worldCanvas.transform);
            SetupVisual(visual, contact);
            pieceVisuals[contact.contactId] = visual;
        }

        private void OnPieceMoved(PieceManager.PieceContact contact)
        {
            if (pieceVisuals.TryGetValue(contact.contactId, out var visual))
            {
                UpdateVisualPosition(visual, contact);
            }
        }

        private void OnPieceRemoved(PieceManager.PieceContact contact)
        {
            if (pieceVisuals.TryGetValue(contact.contactId, out var visual))
            {
                // Fade out animation
                StartCoroutine(FadeOutAndDestroy(visual));
                pieceVisuals.Remove(contact.contactId);
            }
        }

        private void OnPieceTouched(PieceManager.PieceContact contact)
        {
            if (pieceVisuals.TryGetValue(contact.contactId, out var visual))
            {
                // Scale up slightly when touched
                visual.transform.localScale = Vector3.one * 1.1f;
            }
        }

        private void OnPieceReleased(PieceManager.PieceContact contact)
        {
            if (pieceVisuals.TryGetValue(contact.contactId, out var visual))
            {
                visual.transform.localScale = Vector3.one;
            }
        }

        private void CreateDefaultVisual(PieceManager.PieceContact contact)
        {
            // Create a simple visual when no prefab is assigned
            var go = new GameObject($"PieceVisual_{contact.contactId}");
            go.transform.SetParent(worldCanvas != null ? worldCanvas.transform : transform);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);

            var image = go.AddComponent<Image>();
            int colorIndex = contact.glyphId % blockColors.Length;
            image.color = blockColors[colorIndex];

            // Add label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = PieceManager.GetPieceName(contact.glyphId);
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            SetupVisual(go, contact);
            pieceVisuals[contact.contactId] = go;
        }

        private void SetupVisual(GameObject visual, PieceManager.PieceContact contact)
        {
            var rt = visual.GetComponent<RectTransform>();
            if (rt != null)
            {
                UpdateVisualPosition(visual, contact);
            }
        }

        private void UpdateVisualPosition(GameObject visual, PieceManager.PieceContact contact)
        {
            var rt = visual.GetComponent<RectTransform>();
            if (rt == null) return;

            // Convert screen position to canvas position
            // BoardContact.screenPosition is in screen pixel coordinates (1920x1080)
            rt.anchoredPosition = ScreenToCanvasPosition(contact.screenPosition);

            // Apply rotation (orientation is in radians, counter-clockwise from vertical)
            float degrees = contact.orientation * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, degrees);
        }

        private Vector2 ScreenToCanvasPosition(Vector2 screenPos)
        {
            if (worldCanvas == null) return screenPos;

            var canvasRt = worldCanvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRt.sizeDelta;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            // Map screen pixels to canvas coordinates
            return new Vector2(
                (screenPos.x / screenSize.x - 0.5f) * canvasSize.x,
                (screenPos.y / screenSize.y - 0.5f) * canvasSize.y
            );
        }

        private System.Collections.IEnumerator FadeOutAndDestroy(GameObject visual)
        {
            var image = visual.GetComponent<Image>();
            if (image != null)
            {
                float elapsed = 0f;
                Color startColor = image.color;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / 0.3f);
                    image.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }
            }
            Destroy(visual);
        }

        private void OnDestroy()
        {
            foreach (var kvp in pieceVisuals)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            pieceVisuals.Clear();
        }
    }
}
