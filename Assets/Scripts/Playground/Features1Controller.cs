using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BoardOfEducation.Input;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Playground
{
    /// <summary>
    /// Features1 screen: 4 interactive Board SDK demos.
    /// Tabs: Tracker, Spinner, Painter, Multi-piece
    /// </summary>
    public class Features1Controller : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button trackerTab;
        [SerializeField] private Button spinnerTab;
        [SerializeField] private Button painterTab;
        [SerializeField] private Button multiPieceTab;

        [Header("Demo Panels")]
        [SerializeField] private GameObject trackerPanel;
        [SerializeField] private GameObject spinnerPanel;
        [SerializeField] private GameObject painterPanel;
        [SerializeField] private GameObject multiPiecePanel;

        [Header("Tracker")]
        [SerializeField] private TextMeshProUGUI trackerInfoText;
        [SerializeField] private Image trackerCircle;

        [Header("Spinner")]
        [SerializeField] private RectTransform spinnerArrow;
        [SerializeField] private TextMeshProUGUI spinnerDegreeText;
        [SerializeField] private TextMeshProUGUI spinnerHintText;

        [Header("Painter")]
        [SerializeField] private RectTransform painterContainer;
        [SerializeField] private Button clearButton;
        [SerializeField] private Canvas mainCanvas;

        [Header("Multi-piece")]
        [SerializeField] private RectTransform multiPieceContainer;
        [SerializeField] private TextMeshProUGUI multiPieceCountText;

        // Painter state
        private List<GameObject> paintDots = new List<GameObject>();
        private const int MAX_DOTS = 300;
        private int paintFrameCounter;
        private Sprite circleSprite;

        // Multi-piece state
        private Dictionary<int, (GameObject go, TextMeshProUGUI label, Image circle)> multiPieceLabels
            = new Dictionary<int, (GameObject, TextMeshProUGUI, Image)>();

        // Tab state
        private int activeTab;
        private Button[] tabButtons;
        private GameObject[] panels;

        private void Start()
        {
            circleSprite = NavigationHelper.EnsureCircleSprite();

            tabButtons = new[] { trackerTab, spinnerTab, painterTab, multiPieceTab };
            panels = new[] { trackerPanel, spinnerPanel, painterPanel, multiPiecePanel };

            if (trackerTab != null) trackerTab.onClick.AddListener(() => ShowTab(0));
            if (spinnerTab != null) spinnerTab.onClick.AddListener(() => ShowTab(1));
            if (painterTab != null) painterTab.onClick.AddListener(() => ShowTab(2));
            if (multiPieceTab != null) multiPieceTab.onClick.AddListener(() => ShowTab(3));
            if (clearButton != null) clearButton.onClick.AddListener(ClearPaint);

            ShowTab(0);
        }

        private void ShowTab(int index)
        {
            activeTab = index;
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null) panels[i].SetActive(i == index);
            }

            // Highlight active tab
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == index) ? new Color(0.3f, 0.3f, 0.5f, 1f) : new Color(0.15f, 0.15f, 0.25f, 1f);
            }
        }

        private void Update()
        {
            switch (activeTab)
            {
                case 0: UpdateTracker(); break;
                case 1: UpdateSpinner(); break;
                case 2: UpdatePainter(); break;
                case 3: UpdateMultiPiece(); break;
            }
        }

        // ── Demo 1: Piece Tracker ────────────────────────────

        private void UpdateTracker()
        {
            if (PieceManager.Instance == null) return;

            PieceManager.PieceContact? first = null;
            foreach (var kvp in PieceManager.Instance.ActivePieces)
            {
                first = kvp.Value;
                break;
            }

            if (first == null)
            {
                if (trackerInfoText != null)
                    trackerInfoText.text = "No piece detected.\nPlace a piece on the board!";
                if (trackerCircle != null)
                    trackerCircle.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                return;
            }

            var p = first.Value;
            float degrees = p.orientation * Mathf.Rad2Deg;
            string name = PieceManager.GetPieceName(p.glyphId);

            if (trackerInfoText != null)
            {
                trackerInfoText.text =
                    $"<b>Piece:</b> {name}\n" +
                    $"<b>Position:</b> ({p.screenPosition.x:F0}, {p.screenPosition.y:F0})\n" +
                    $"<b>Orientation:</b> {degrees:F1}°\n" +
                    $"<b>Touched:</b> {p.isTouched}\n" +
                    $"<b>Contact ID:</b> {p.contactId}\n" +
                    $"<b>Glyph ID:</b> {p.glyphId}";
            }

            if (trackerCircle != null)
                trackerCircle.color = NavigationHelper.GetPieceColor(p.glyphId);
        }

        // ── Demo 2: Orientation Spinner ──────────────────────

        private void UpdateSpinner()
        {
            if (PieceManager.Instance == null) return;

            PieceManager.PieceContact? first = null;
            foreach (var kvp in PieceManager.Instance.ActivePieces)
            {
                first = kvp.Value;
                break;
            }

            if (first == null)
            {
                if (spinnerHintText != null) spinnerHintText.text = "Place a piece and rotate it!";
                if (spinnerDegreeText != null) spinnerDegreeText.text = "--°";
                if (spinnerArrow != null) spinnerArrow.gameObject.SetActive(false);
                return;
            }

            var p = first.Value;
            float degrees = p.orientation * Mathf.Rad2Deg;

            if (spinnerArrow != null)
            {
                spinnerArrow.gameObject.SetActive(true);
                spinnerArrow.localEulerAngles = new Vector3(0, 0, -degrees);
            }

            if (spinnerDegreeText != null)
                spinnerDegreeText.text = $"{degrees:F1}°";

            if (spinnerHintText != null)
                spinnerHintText.text = PieceManager.GetPieceName(p.glyphId);
        }

        // ── Demo 3: Color Painter ────────────────────────────

        private void UpdatePainter()
        {
            if (PieceManager.Instance == null || mainCanvas == null) return;

            PieceManager.PieceContact? first = null;
            foreach (var kvp in PieceManager.Instance.ActivePieces)
            {
                first = kvp.Value;
                break;
            }

            if (first == null) return;

            // Only spawn a dot every 2 frames
            paintFrameCounter++;
            if (paintFrameCounter % 2 != 0) return;

            var p = first.Value;
            Vector2 canvasPos = NavigationHelper.ScreenToCanvasPosition(mainCanvas, p.screenPosition);
            Color col = NavigationHelper.GetPieceColor(p.glyphId);

            // Create dot
            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(painterContainer, false);
            var rect = dotGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20, 20);
            rect.anchoredPosition = canvasPos;
            var img = dotGo.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = col;
            img.raycastTarget = false;

            paintDots.Add(dotGo);

            // Cap at MAX_DOTS
            while (paintDots.Count > MAX_DOTS)
            {
                if (paintDots[0] != null) Destroy(paintDots[0]);
                paintDots.RemoveAt(0);
            }
        }

        private void ClearPaint()
        {
            foreach (var dot in paintDots)
            {
                if (dot != null) Destroy(dot);
            }
            paintDots.Clear();
        }

        // ── Demo 4: Multi-piece Detection ────────────────────

        private void UpdateMultiPiece()
        {
            if (PieceManager.Instance == null || mainCanvas == null) return;

            var activePieces = PieceManager.Instance.ActivePieces;
            var seenIds = new HashSet<int>();

            foreach (var kvp in activePieces)
            {
                var p = kvp.Value;
                seenIds.Add(p.contactId);

                Vector2 canvasPos = NavigationHelper.ScreenToCanvasPosition(mainCanvas, p.screenPosition);
                Color col = NavigationHelper.GetPieceColor(p.glyphId);
                string name = PieceManager.GetPieceName(p.glyphId);

                if (multiPieceLabels.TryGetValue(p.contactId, out var existing))
                {
                    // Update position
                    var rt = existing.go.GetComponent<RectTransform>();
                    rt.anchoredPosition = canvasPos;
                    existing.label.text = $"{name}\nID: {p.contactId}";
                    existing.circle.color = col;
                }
                else
                {
                    // Create new label + circle
                    var go = new GameObject($"Piece_{p.contactId}");
                    go.transform.SetParent(multiPieceContainer, false);
                    var rect = go.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(160, 80);
                    rect.anchoredPosition = canvasPos;

                    // Circle indicator
                    var circleGo = new GameObject("Circle");
                    circleGo.transform.SetParent(go.transform, false);
                    var cRect = circleGo.AddComponent<RectTransform>();
                    cRect.anchorMin = new Vector2(0, 0.5f);
                    cRect.anchorMax = new Vector2(0, 0.5f);
                    cRect.pivot = new Vector2(0.5f, 0.5f);
                    cRect.sizeDelta = new Vector2(40, 40);
                    cRect.anchoredPosition = new Vector2(0, 0);
                    var cImg = circleGo.AddComponent<Image>();
                    cImg.sprite = circleSprite;
                    cImg.color = col;
                    cImg.raycastTarget = false;

                    // Label text
                    var labelGo = new GameObject("Label");
                    labelGo.transform.SetParent(go.transform, false);
                    var lRect = labelGo.AddComponent<RectTransform>();
                    lRect.anchorMin = new Vector2(0, 0);
                    lRect.anchorMax = new Vector2(1, 1);
                    lRect.offsetMin = new Vector2(30, 0);
                    lRect.offsetMax = Vector2.zero;
                    var label = labelGo.AddComponent<TextMeshProUGUI>();
                    label.text = $"{name}\nID: {p.contactId}";
                    label.fontSize = 14;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.color = Color.white;
                    label.textWrappingMode = TextWrappingModes.Normal;

                    multiPieceLabels[p.contactId] = (go, label, cImg);
                }
            }

            // Clean up removed pieces
            var toRemove = new List<int>();
            foreach (var kvp in multiPieceLabels)
            {
                if (!seenIds.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (var id in toRemove)
            {
                if (multiPieceLabels[id].go != null)
                    Destroy(multiPieceLabels[id].go);
                multiPieceLabels.Remove(id);
            }

            // Count display
            if (multiPieceCountText != null)
                multiPieceCountText.text = $"{activePieces.Count} piece{(activePieces.Count != 1 ? "s" : "")} detected";
        }

        private void OnDisable()
        {
            // Clean up multi-piece labels
            foreach (var kvp in multiPieceLabels)
            {
                if (kvp.Value.go != null) Destroy(kvp.Value.go);
            }
            multiPieceLabels.Clear();
        }
    }
}
