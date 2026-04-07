using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.Navigation
{
    /// <summary>
    /// Shared utilities used across landing page, playground, and game scenes.
    /// Extracts duplicated code from CirclesUI/CirclesBoard into one place.
    /// </summary>
    public static class NavigationHelper
    {
        // ── Piece Colors (Board Arcade set) ──────────────────

        public static readonly Dictionary<int, Color> PieceColors = new Dictionary<int, Color>
        {
            { PieceManager.ArcadeGlyphs.RobotYellow, HexColor("#FFD700") },
            { PieceManager.ArcadeGlyphs.RobotPurple, HexColor("#9B59B6") },
            { PieceManager.ArcadeGlyphs.RobotOrange, HexColor("#FF6B35") },
            { PieceManager.ArcadeGlyphs.RobotPink,   HexColor("#FF69B4") },
            { PieceManager.ArcadeGlyphs.ShipPink,     HexColor("#FF1493") },
            { PieceManager.ArcadeGlyphs.ShipYellow,   HexColor("#F1C40F") },
            { PieceManager.ArcadeGlyphs.ShipPurple,   HexColor("#8E44AD") },
        };

        public static readonly string[] PieceNames =
        {
            "Robot Yellow", "Robot Purple", "Robot Orange", "Robot Pink",
            "Ship Pink", "Ship Yellow", "Ship Purple"
        };

        public static readonly int[] GlyphIds =
        {
            PieceManager.ArcadeGlyphs.RobotYellow,
            PieceManager.ArcadeGlyphs.RobotPurple,
            PieceManager.ArcadeGlyphs.RobotOrange,
            PieceManager.ArcadeGlyphs.RobotPink,
            PieceManager.ArcadeGlyphs.ShipPink,
            PieceManager.ArcadeGlyphs.ShipYellow,
            PieceManager.ArcadeGlyphs.ShipPurple,
        };

        // ── Hit Testing ──────────────────────────────────────

        /// <summary>
        /// Checks if a screen point is within a RectTransform's bounds, with padding.
        /// </summary>
        public static bool IsOverRect(RectTransform rect, Vector2 screenPoint, float padding = 20f)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null))
                return true;

            if (padding <= 0f) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(padding, 0), null)
                || RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(-padding, 0), null)
                || RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(0, padding), null)
                || RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(0, -padding), null);
        }

        // ── Coordinate Conversion ────────────────────────────

        /// <summary>
        /// Converts screen pixel position to canvas-local anchored position.
        /// </summary>
        public static Vector2 ScreenToCanvasPosition(Canvas canvas, Vector2 screenPos)
        {
            if (canvas == null) return screenPos;

            var canvasRt = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRt.sizeDelta;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            return new Vector2(
                (screenPos.x / screenSize.x - 0.5f) * canvasSize.x,
                (screenPos.y / screenSize.y - 0.5f) * canvasSize.y
            );
        }

        // ── Scene Loading ────────────────────────────────────

        public static void LoadScene(string sceneName)
        {
            Debug.Log($"[Navigation] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        // ── Procedural Circle Sprite ─────────────────────────

        private static Sprite circleSprite;

        public static Sprite EnsureCircleSprite()
        {
            if (circleSprite != null) return circleSprite;

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
            return circleSprite;
        }

        // ── Color Parsing ────────────────────────────────────

        public static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        /// <summary>
        /// Gets the color for a glyph ID, falling back to white.
        /// </summary>
        public static Color GetPieceColor(int glyphId)
        {
            return PieceColors.TryGetValue(glyphId, out var col) ? col : Color.white;
        }
    }
}
