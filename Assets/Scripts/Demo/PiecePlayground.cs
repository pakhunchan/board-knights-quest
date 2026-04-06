using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.Demo
{
    /// <summary>
    /// Place any Board Arcade piece and get immediate colorful visual feedback.
    /// No game logic — pure visual sandbox to prove the SDK pipeline works.
    /// </summary>
    public class PiecePlayground : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text instructionText;
        [SerializeField] private Text pieceCountText;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private RectTransform bgCircleContainer;

        // Active cards keyed by contactId
        private Dictionary<int, GameObject> activeCards = new Dictionary<int, GameObject>();

        // Procedurally generated circle sprite (shared by all circles)
        private static Sprite circleSprite;

        // Background circle gentle float animation
        private List<(RectTransform rect, float speed, float amplitude, Vector2 origin)> floatingCircles
            = new List<(RectTransform, float, float, Vector2)>();

        // Distinct colors for each of the 7 Board Arcade pieces
        private static readonly Dictionary<int, Color> PieceColors = new Dictionary<int, Color>
        {
            { PieceManager.ArcadeGlyphs.RobotYellow, HexColor("#FFD700") },
            { PieceManager.ArcadeGlyphs.RobotPurple, HexColor("#9B59B6") },
            { PieceManager.ArcadeGlyphs.RobotOrange, HexColor("#FF6B35") },
            { PieceManager.ArcadeGlyphs.RobotPink,   HexColor("#FF69B4") },
            { PieceManager.ArcadeGlyphs.ShipPink,     HexColor("#FF1493") },
            { PieceManager.ArcadeGlyphs.ShipYellow,   HexColor("#F1C40F") },
            { PieceManager.ArcadeGlyphs.ShipPurple,   HexColor("#8E44AD") },
        };

        // Cards animating out (removal fade/shrink)
        private List<(GameObject card, float timer)> fadingCards = new List<(GameObject, float)>();
        private const float FadeDuration = 0.3f;

        // Cards animating in (placement scale-up)
        private List<(GameObject card, float timer)> growingCards = new List<(GameObject, float)>();
        private const float GrowDuration = 0.25f;

        private void OnEnable()
        {
            EnsureCircleSprite();
            SpawnBackgroundCircles();

            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPiecePlaced += HandlePiecePlaced;
                PieceManager.Instance.OnPieceRemoved += HandlePieceRemoved;
                PieceManager.Instance.OnPieceMoved += HandlePieceMoved;
            }
            else
            {
                Debug.LogError("[PiecePlayground] PieceManager not found! Make sure it's in the scene.");
            }

            UpdateCountText();
        }

        private void OnDisable()
        {
            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPiecePlaced -= HandlePiecePlaced;
                PieceManager.Instance.OnPieceRemoved -= HandlePieceRemoved;
                PieceManager.Instance.OnPieceMoved -= HandlePieceMoved;
            }
        }

        private void Update()
        {
            // Animate growing cards (scale up on placement)
            for (int i = growingCards.Count - 1; i >= 0; i--)
            {
                var (card, timer) = growingCards[i];
                if (card == null) { growingCards.RemoveAt(i); continue; }

                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / GrowDuration);
                // Overshoot ease for a satisfying pop
                float scale = EaseOutBack(t);
                card.transform.localScale = Vector3.one * scale;

                if (t >= 1f)
                    growingCards.RemoveAt(i);
                else
                    growingCards[i] = (card, timer);
            }

            // Animate fading cards (shrink + fade on removal)
            for (int i = fadingCards.Count - 1; i >= 0; i--)
            {
                var (card, timer) = fadingCards[i];
                if (card == null) { fadingCards.RemoveAt(i); continue; }

                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / FadeDuration);
                float scale = 1f - t;
                card.transform.localScale = Vector3.one * scale;

                // Fade the canvas group
                var cg = card.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f - t;

                if (t >= 1f)
                {
                    Destroy(card);
                    fadingCards.RemoveAt(i);
                }
                else
                {
                    fadingCards[i] = (card, timer);
                }
            }

            // Gently float background circles
            float time = Time.time;
            foreach (var (rect, speed, amplitude, origin) in floatingCircles)
            {
                if (rect == null) continue;
                float yOffset = Mathf.Sin(time * speed) * amplitude;
                float xOffset = Mathf.Cos(time * speed * 0.7f) * amplitude * 0.5f;
                rect.anchoredPosition = origin + new Vector2(xOffset, yOffset);
            }
        }

        private void HandlePiecePlaced(PieceManager.PieceContact piece)
        {
            if (activeCards.ContainsKey(piece.contactId)) return;

            var card = CreateCard(piece);
            activeCards[piece.contactId] = card;

            // Start scale-up animation from zero
            card.transform.localScale = Vector3.zero;
            growingCards.Add((card, 0f));

            UpdateCountText();
            Debug.Log($"[PiecePlayground] Placed: {PieceManager.GetPieceName(piece.glyphId)} (contact {piece.contactId})");
        }

        private void HandlePieceRemoved(PieceManager.PieceContact piece)
        {
            if (!activeCards.TryGetValue(piece.contactId, out var card)) return;

            activeCards.Remove(piece.contactId);

            // Add canvas group for fade if not present
            if (card.GetComponent<CanvasGroup>() == null)
                card.AddComponent<CanvasGroup>();

            fadingCards.Add((card, 0f));

            UpdateCountText();
            Debug.Log($"[PiecePlayground] Removed: {PieceManager.GetPieceName(piece.glyphId)} (contact {piece.contactId})");
        }

        private void HandlePieceMoved(PieceManager.PieceContact piece)
        {
            // Could add subtle position tracking later — for now just log
        }

        /// <summary>Creates a procedural circle texture + sprite (once, shared).</summary>
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
                    // Soft edge for anti-aliasing
                    float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Spawn soft glowing background circles for each piece color.</summary>
        private void SpawnBackgroundCircles()
        {
            if (bgCircleContainer == null) return;

            // Positions spread across the play area (normalized 0-1 within container)
            var positions = new Vector2[]
            {
                new Vector2(0.10f, 0.55f),
                new Vector2(0.25f, 0.35f),
                new Vector2(0.40f, 0.60f),
                new Vector2(0.55f, 0.30f),
                new Vector2(0.70f, 0.55f),
                new Vector2(0.85f, 0.40f),
                new Vector2(0.50f, 0.75f),
            };

            int[] glyphIds = {
                PieceManager.ArcadeGlyphs.RobotYellow,
                PieceManager.ArcadeGlyphs.RobotPurple,
                PieceManager.ArcadeGlyphs.RobotOrange,
                PieceManager.ArcadeGlyphs.RobotPink,
                PieceManager.ArcadeGlyphs.ShipPink,
                PieceManager.ArcadeGlyphs.ShipYellow,
                PieceManager.ArcadeGlyphs.ShipPurple,
            };

            for (int i = 0; i < 7; i++)
            {
                Color col = PieceColors.ContainsKey(glyphIds[i]) ? PieceColors[glyphIds[i]] : Color.white;

                var go = new GameObject($"BgCircle_{i}");
                go.transform.SetParent(bgCircleContainer, false);

                var rect = go.AddComponent<RectTransform>();
                float size = Random.Range(120f, 200f);
                rect.sizeDelta = new Vector2(size, size);

                // Position using anchors within the container
                rect.anchorMin = positions[i];
                rect.anchorMax = positions[i];
                rect.anchoredPosition = Vector2.zero;

                var img = go.AddComponent<Image>();
                img.sprite = circleSprite;
                img.color = new Color(col.r, col.g, col.b, 0.15f);
                img.raycastTarget = false;

                // Register for floating animation
                float speed = Random.Range(0.3f, 0.8f);
                float amplitude = Random.Range(5f, 15f);
                floatingCircles.Add((rect, speed, amplitude, Vector2.zero));
            }
        }

        private GameObject CreateCard(PieceManager.PieceContact piece)
        {
            Color pieceColor = PieceColors.ContainsKey(piece.glyphId)
                ? PieceColors[piece.glyphId]
                : Color.white;

            string pieceName = PieceManager.GetPieceName(piece.glyphId);

            // Card root
            var cardGo = new GameObject($"Card_{piece.contactId}");
            cardGo.transform.SetParent(cardContainer, false);

            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(220, 280);

            // Card background — rounded look via solid color
            var bgImage = cardGo.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.25f, 0.9f);

            // Vertical layout inside card
            var layout = cardGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 20, 20);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Colored circle
            var circleGo = new GameObject("Circle");
            circleGo.transform.SetParent(cardGo.transform, false);
            var circleRect = circleGo.AddComponent<RectTransform>();
            circleRect.sizeDelta = new Vector2(90, 90);
            var circleLayout = circleGo.AddComponent<LayoutElement>();
            circleLayout.preferredWidth = 90;
            circleLayout.preferredHeight = 90;
            var circleImg = circleGo.AddComponent<Image>();
            circleImg.sprite = circleSprite;
            circleImg.color = pieceColor;

            // Piece name text
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(cardGo.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = pieceName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 22;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleCenter;
            var nameLayout = nameGo.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 60;

            // Glyph ID text (small, for debugging)
            var idGo = new GameObject("GlyphId");
            idGo.transform.SetParent(cardGo.transform, false);
            var idText = idGo.AddComponent<Text>();
            idText.text = $"glyph {piece.glyphId}";
            idText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            idText.fontSize = 16;
            idText.color = new Color(1f, 1f, 1f, 0.5f);
            idText.alignment = TextAnchor.MiddleCenter;
            var idLayout = idGo.AddComponent<LayoutElement>();
            idLayout.preferredHeight = 24;

            return cardGo;
        }

        private void UpdateCountText()
        {
            if (pieceCountText != null)
            {
                int count = activeCards.Count;
                pieceCountText.text = $"{count} piece{(count != 1 ? "s" : "")} on board";
            }
        }

        /// <summary>Overshoot ease for a satisfying "pop" feel.</summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
