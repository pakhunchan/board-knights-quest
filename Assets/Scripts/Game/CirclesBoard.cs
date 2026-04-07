using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Core gameplay controller for Circles.
    /// The piece acts as an accumulator — touch a circle to absorb its value.
    /// A number label follows the piece showing its held value.
    /// </summary>
    public class CirclesBoard : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform circleContainer;
        [SerializeField] private Canvas mainCanvas;

        [Header("Settings")]
        [SerializeField] private float absorbRadius = 120f;
        [SerializeField] private float circleSize = 100f;

        // Active circles in the current level
        private List<CirclesCircle> circles = new List<CirclesCircle>();
        private CirclesCircle hoveredCircle;

        // Dwell-to-absorb: auto-absorb when piece hovers over a circle
        private float hoverDwellTime = -1f;
        private const float ABSORB_DWELL_SECONDS = 0.15f;

        // Piece cursor — a number label that follows the piece
        private GameObject pieceCursorGo;
        private RectTransform pieceCursorRect;
        private Image pieceCursorBg;
        private TextMeshProUGUI pieceCursorText;
        private bool pieceHasValue;
        private float pieceHeldValue;
        private CircleData pieceHeldData; // full data for combine logic

        // Procedurally generated circle sprite
        private static Sprite circleSprite;

        private void Start()
        {
            EnsureCircleSprite();
            CreatePieceCursor();

            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPieceTouched += HandlePieceTouched;
            }

            if (CirclesGameManager.Instance != null)
            {
                CirclesGameManager.Instance.OnLevelSelected += HandleLevelSelected;
                CirclesGameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPieceTouched -= HandlePieceTouched;
            }

            if (CirclesGameManager.Instance != null)
            {
                CirclesGameManager.Instance.OnLevelSelected -= HandleLevelSelected;
                CirclesGameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            var gm = CirclesGameManager.Instance;
            if (gm == null || gm.CurrentState != CirclesGameManager.State.Playing) return;
            if (gm.TrackedContactId < 0) return;
            if (PieceManager.Instance == null) return;
            if (!PieceManager.Instance.ActivePieces.TryGetValue(gm.TrackedContactId, out var piece)) return;

            TrackPiecePosition(piece);
        }

        // ── Piece Cursor ─────────────────────────────────────

        private void CreatePieceCursor()
        {
            if (mainCanvas == null) return;

            pieceCursorGo = new GameObject("PieceCursor");
            pieceCursorGo.transform.SetParent(mainCanvas.transform, false);

            pieceCursorRect = pieceCursorGo.AddComponent<RectTransform>();
            pieceCursorRect.sizeDelta = new Vector2(80, 80);

            // Background circle
            pieceCursorBg = pieceCursorGo.AddComponent<Image>();
            pieceCursorBg.sprite = circleSprite;
            pieceCursorBg.raycastTarget = false;

            // Value text
            var textGo = new GameObject("Value");
            textGo.transform.SetParent(pieceCursorGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            pieceCursorText = textGo.AddComponent<TextMeshProUGUI>();
            pieceCursorText.fontSize = 32;
            pieceCursorText.alignment = TextAlignmentOptions.Center;
            pieceCursorText.color = Color.white;
            pieceCursorText.enableWordWrapping = false;

            pieceCursorGo.SetActive(false);
        }

        private void UpdatePieceCursorPosition(Vector2 screenPos)
        {
            if (pieceCursorRect == null) return;
            // Offset slightly below the piece so it doesn't overlap the physical piece visual
            Vector2 canvasPos = ScreenToCanvasPosition(screenPos);
            pieceCursorRect.anchoredPosition = canvasPos + new Vector2(0, -60f);
        }

        private void ShowPieceCursor(float value, string display)
        {
            pieceHasValue = true;
            pieceHeldValue = value;
            pieceHeldData = new CircleData(value, CircleType.Number, display);

            if (pieceCursorGo != null) pieceCursorGo.SetActive(true);
            if (pieceCursorText != null) pieceCursorText.text = display;

            // Color: teal for positive, coral for negative
            Color col = value >= 0 ? HexColor("#2ecc71") : HexColor("#e74c3c");
            if (pieceCursorBg != null) pieceCursorBg.color = col;
        }

        private void HidePieceCursor()
        {
            pieceHasValue = false;
            pieceHeldValue = 0f;
            pieceHeldData = null;
            if (pieceCursorGo != null) pieceCursorGo.SetActive(false);
        }

        // ── Level Setup ──────────────────────────────────────

        private void HandleStateChanged(CirclesGameManager.State state)
        {
            if (state != CirclesGameManager.State.Playing)
            {
                ClearCircles();
                HidePieceCursor();
            }
        }

        private void HandleLevelSelected(int levelIndex)
        {
            var level = CirclesLevel.AllLevels[levelIndex];
            SpawnCircles(level);
        }

        public void SpawnCircles(CirclesLevel level)
        {
            ClearCircles();
            HidePieceCursor();

            var positions = CalculateCirclePositions(level.Circles.Count);

            for (int i = 0; i < level.Circles.Count; i++)
            {
                var circle = CreateCircleObject(level.Circles[i], positions[i]);
                circles.Add(circle);
                circle.AnimateSpawn();
            }
        }

        private void ClearCircles()
        {
            foreach (var c in circles)
            {
                if (c != null) Destroy(c.gameObject);
            }
            circles.Clear();
            hoveredCircle = null;
            hoverDwellTime = -1f;
        }

        // ── Circle Creation ──────────────────────────────────

        private CirclesCircle CreateCircleObject(CircleData data, Vector2 position)
        {
            var go = new GameObject($"Circle_{data.DisplayText}");
            go.transform.SetParent(circleContainer, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(circleSize, circleSize);
            rect.anchoredPosition = position;

            // Background circle
            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = circleSprite;
            bgImg.raycastTarget = false;

            // Glow ring (child, slightly larger)
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-circleSize * 0.1f, -circleSize * 0.1f);
            glowRect.offsetMax = new Vector2(circleSize * 0.1f, circleSize * 0.1f);
            var glowImg = glowGo.AddComponent<Image>();
            glowImg.sprite = circleSprite;
            glowImg.raycastTarget = false;
            glowGo.transform.SetAsFirstSibling();

            // Value text (child)
            var textGo = new GameObject("Value");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var circle = go.AddComponent<CirclesCircle>();
            circle.background = bgImg;
            circle.valueText = tmp;
            circle.glowRing = glowImg;
            circle.Initialize(data);

            return circle;
        }

        // ── Circle Layout ────────────────────────────────────

        private Vector2[] CalculateCirclePositions(int count)
        {
            var positions = new Vector2[count];
            float spacing = 200f;

            switch (count)
            {
                case 2:
                    positions[0] = new Vector2(-200, 0);
                    positions[1] = new Vector2(200, 0);
                    break;
                case 3:
                    positions[0] = new Vector2(0, 120);
                    positions[1] = new Vector2(-180, -80);
                    positions[2] = new Vector2(180, -80);
                    break;
                case 4:
                    float half = 150f;
                    positions[0] = new Vector2(-half, half);
                    positions[1] = new Vector2(half, half);
                    positions[2] = new Vector2(-half, -half);
                    positions[3] = new Vector2(half, -half);
                    break;
                default:
                    int topCount = (count + 1) / 2;
                    int botCount = count - topCount;
                    float topWidth = (topCount - 1) * spacing;
                    float botWidth = (botCount - 1) * spacing;
                    for (int i = 0; i < topCount; i++)
                        positions[i] = new Vector2(-topWidth / 2f + i * spacing, 80);
                    for (int i = 0; i < botCount; i++)
                        positions[topCount + i] = new Vector2(-botWidth / 2f + i * spacing, -80);
                    break;
            }
            return positions;
        }

        // ── Piece Interaction ────────────────────────────────

        private bool IsTrackedPiece(PieceManager.PieceContact piece)
        {
            var gm = CirclesGameManager.Instance;
            if (gm == null || gm.CurrentState != CirclesGameManager.State.Playing) return false;
            return piece.glyphId == gm.SelectedGlyphId;
        }

        private void TrackPiecePosition(PieceManager.PieceContact piece)
        {
            Vector2 canvasPos = ScreenToCanvasPosition(piece.screenPosition);

            // Update cursor position
            UpdatePieceCursorPosition(piece.screenPosition);

            // Highlight nearest circle
            var nearest = FindNearestCircle(canvasPos, absorbRadius);

            if (nearest != hoveredCircle)
            {
                if (hoveredCircle != null)
                    hoveredCircle.SetVisualState(CirclesCircle.VisualState.Idle);

                hoveredCircle = nearest;
                hoverDwellTime = nearest != null ? 0f : -1f;

                if (hoveredCircle != null)
                {
                    // Show preview of what would happen
                    if (pieceHasValue)
                    {
                        var result = CirclesLevel.EvaluateCombine(pieceHeldData, hoveredCircle.Data);
                        if (result.IsValid)
                        {
                            string preview = result.IsNullified ? "= 0!" : $"= {result.ResultDisplay}";
                            hoveredCircle.SetVisualState(CirclesCircle.VisualState.Preview, preview);
                        }
                        else
                        {
                            hoveredCircle.SetVisualState(CirclesCircle.VisualState.Highlighted);
                        }
                    }
                    else
                    {
                        hoveredCircle.SetVisualState(CirclesCircle.VisualState.Highlighted);
                    }
                }
            }
            else if (hoveredCircle != null)
            {
                // Same circle — accumulate dwell time
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= ABSORB_DWELL_SECONDS)
                {
                    hoverDwellTime = -1f;
                    PerformAutoAbsorb();
                }
            }
        }

        /// <summary>
        /// Auto-absorb or combine when piece dwells on a circle.
        /// Same logic as HandlePieceTouched but triggered by hover dwell.
        /// </summary>
        private void PerformAutoAbsorb()
        {
            if (hoveredCircle == null) return;

            var gm = CirclesGameManager.Instance;

            if (!pieceHasValue)
            {
                AbsorbCircle(hoveredCircle);
            }
            else
            {
                var result = CirclesLevel.EvaluateCombine(pieceHeldData, hoveredCircle.Data);
                if (result.IsValid)
                {
                    gm?.RecordMove();
                    PerformCombine(hoveredCircle, result);
                }
                else
                {
                    hoveredCircle.AnimateReject();
                    hoveredCircle.SetVisualState(CirclesCircle.VisualState.Idle);
                }
            }

            hoveredCircle = null;
        }

        private void HandlePieceTouched(PieceManager.PieceContact piece)
        {
            if (!IsTrackedPiece(piece)) return;
            if (hoveredCircle == null) return;

            var gm = CirclesGameManager.Instance;

            if (!pieceHasValue)
            {
                // First touch — absorb the circle's value
                AbsorbCircle(hoveredCircle);
            }
            else
            {
                // Piece already holds a value — combine with this circle
                var result = CirclesLevel.EvaluateCombine(pieceHeldData, hoveredCircle.Data);
                if (result.IsValid)
                {
                    gm?.RecordMove();
                    PerformCombine(hoveredCircle, result);
                }
                else
                {
                    // Can't combine (e.g. operation + operation)
                    hoveredCircle.AnimateReject();
                    hoveredCircle.SetVisualState(CirclesCircle.VisualState.Idle);
                }
            }

            hoveredCircle = null;
        }

        // ── Absorb & Combine ─────────────────────────────────

        private void AbsorbCircle(CirclesCircle circle)
        {
            var gm = CirclesGameManager.Instance;
            gm?.RecordMove();

            float val = circle.Data.Value;
            string display = circle.Data.DisplayText;
            CircleType type = circle.Data.Type;

            // Remove from active list immediately
            circles.Remove(circle);

            // Update piece cursor instantly — no waiting for animation
            ShowPieceCursor(val, display);
            pieceHeldData = new CircleData(val, type, display);

            // Fire-and-forget animation (plays in background)
            circle.AnimateDissolve(() =>
            {
                gm?.CompleteLevelCheck(circles.Count);
            });
        }

        private void PerformCombine(CirclesCircle target, CombineResult result)
        {
            var gm = CirclesGameManager.Instance;

            // Remove from active list immediately
            circles.Remove(target);
            int remaining = circles.Count;

            // Update piece cursor instantly
            if (result.IsNullified)
                HidePieceCursor();
            else
                ShowPieceCursor(result.ResultValue, result.ResultDisplay);

            // Fire-and-forget animation
            target.AnimateDissolve(() =>
            {
                gm?.CompleteLevelCheck(remaining);
            });
        }

        // ── Proximity Detection ──────────────────────────────

        private CirclesCircle FindNearestCircle(Vector2 canvasPos, float maxDist)
        {
            CirclesCircle nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var circle in circles)
            {
                if (circle == null) continue;

                var rect = circle.GetComponent<RectTransform>();
                if (rect == null) continue;

                float dist = Vector2.Distance(canvasPos, rect.anchoredPosition);
                if (dist < maxDist && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = circle;
                }
            }
            return nearest;
        }

        private Vector2 ScreenToCanvasPosition(Vector2 screenPos)
        {
            if (mainCanvas == null) return screenPos;

            var canvasRt = mainCanvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRt.sizeDelta;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            return new Vector2(
                (screenPos.x / screenSize.x - 0.5f) * canvasSize.x,
                (screenPos.y / screenSize.y - 0.5f) * canvasSize.y
            );
        }

        // ── Circle Sprite ────────────────────────────────────

        private static void EnsureCircleSprite()
        {
            if (circleSprite != null) return;

            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = center - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
