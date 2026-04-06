using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages all Nullify UI screens: piece select, level select, gameplay HUD, and level complete overlay.
    /// Piece select uses a confirm button (finger tap). Level select uses tappable buttons.
    /// </summary>
    public class NullifyUI : MonoBehaviour
    {
        [Header("Screen Panels")]
        [SerializeField] private GameObject pieceSelectScreen;
        [SerializeField] private GameObject levelSelectScreen;
        [SerializeField] private GameObject gameplayScreen;
        [SerializeField] private GameObject levelCompleteOverlay;

        [Header("Piece Select")]
        [SerializeField] private TextMeshProUGUI pieceSelectTitle;
        [SerializeField] private TextMeshProUGUI pieceSelectSubtitle;
        [SerializeField] private RectTransform pieceCircleContainer;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI pieceStatusText;
        [SerializeField] private Button backButton;

        [Header("Level Select")]
        [SerializeField] private TextMeshProUGUI levelSelectTitle;
        [SerializeField] private RectTransform levelCircleContainer;

        [Header("Gameplay")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI moveCountText;
        [SerializeField] private TextMeshProUGUI instructionText;

        [Header("Level Complete")]
        [SerializeField] private TextMeshProUGUI completeTitle;
        [SerializeField] private TextMeshProUGUI starsText;
        [SerializeField] private TextMeshProUGUI completeMoveText;
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private Button continueButton;

        // Piece select circle tracking
        private List<(RectTransform rect, Image img, int glyphId)> pieceCircles
            = new List<(RectTransform, Image, int)>();

        // Level select tracking
        private int selectedLevelIndex = -1;
        private List<(RectTransform rect, Image img, int levelIndex, bool unlocked)> levelCircleData
            = new List<(RectTransform, Image, int, bool)>();
        private Button playButton;
        private RectTransform playButtonRect;
        private TextMeshProUGUI levelInfoText;
        private Image playButtonImage;
        private const float PIECE_HOVER_RADIUS = 60f;

        // Piece colors
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

        private static readonly string[] PieceNames =
        {
            "Robot\nYellow", "Robot\nPurple", "Robot\nOrange", "Robot\nPink",
            "Ship\nPink", "Ship\nYellow", "Ship\nPurple"
        };

        private static readonly int[] GlyphIds =
        {
            PieceManager.ArcadeGlyphs.RobotYellow,
            PieceManager.ArcadeGlyphs.RobotPurple,
            PieceManager.ArcadeGlyphs.RobotOrange,
            PieceManager.ArcadeGlyphs.RobotPink,
            PieceManager.ArcadeGlyphs.ShipPink,
            PieceManager.ArcadeGlyphs.ShipYellow,
            PieceManager.ArcadeGlyphs.ShipPurple,
        };

        private static Sprite circleSprite;

        private void Start()
        {
            EnsureCircleSprite();

            var gm = NullifyGameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged += HandleStateChanged;
                gm.OnLevelSelected += HandleLevelSelected;
                gm.OnLevelCompleted += HandleLevelCompleted;
                gm.OnPieceSelected += HandlePieceSelected;
                gm.OnMoveCountChanged += HandleMoveCountChanged;
                gm.OnPieceDetected += HandlePieceDetected;
                gm.OnPieceLost += HandlePieceLost;
            }

            // Wire up back button
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            // Wire up confirm button
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            // Wire up continue button
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            // Initialize
            ShowScreen(NullifyGameManager.State.PieceSelect);
            BuildPieceSelectCircles();
            if (pieceStatusText != null) pieceStatusText.text = "";
        }

        private void OnDisable()
        {
            var gm = NullifyGameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged -= HandleStateChanged;
                gm.OnLevelSelected -= HandleLevelSelected;
                gm.OnLevelCompleted -= HandleLevelCompleted;
                gm.OnPieceSelected -= HandlePieceSelected;
                gm.OnMoveCountChanged -= HandleMoveCountChanged;
                gm.OnPieceDetected -= HandlePieceDetected;
                gm.OnPieceLost -= HandlePieceLost;
            }
        }

        // ── State Changes ────────────────────────────────────

        private void HandleStateChanged(NullifyGameManager.State state)
        {
            ShowScreen(state);
            pieceDwellOnPlay = -1f;
            pieceDwellOnContinue = -1f;

            if (state == NullifyGameManager.State.LevelSelect)
                BuildLevelSelectCircles();

            if (state == NullifyGameManager.State.LevelComplete)
                MakeOverlayTappable();
        }

        /// <summary>
        /// Makes the entire Level Complete overlay act as a tap target.
        /// Any tap anywhere on the screen will advance.
        /// </summary>
        private void MakeOverlayTappable()
        {
            if (levelCompleteOverlay == null) return;

            // Enable raycast on background so taps register
            var bgImg = levelCompleteOverlay.GetComponent<Image>();
            if (bgImg != null) bgImg.raycastTarget = true;

            // Add a button to the whole overlay if not already present
            var overlayBtn = levelCompleteOverlay.GetComponent<Button>();
            if (overlayBtn == null)
                overlayBtn = levelCompleteOverlay.AddComponent<Button>();

            overlayBtn.onClick.RemoveAllListeners();
            overlayBtn.onClick.AddListener(OnContinueClicked);
        }

        private void ShowScreen(NullifyGameManager.State state)
        {
            if (pieceSelectScreen != null)  pieceSelectScreen.SetActive(state == NullifyGameManager.State.PieceSelect);
            if (levelSelectScreen != null)  levelSelectScreen.SetActive(state == NullifyGameManager.State.LevelSelect);
            if (gameplayScreen != null)     gameplayScreen.SetActive(state == NullifyGameManager.State.Playing);
            if (levelCompleteOverlay != null) levelCompleteOverlay.SetActive(state == NullifyGameManager.State.LevelComplete);
        }

        // ── Piece Select ─────────────────────────────────────

        private void BuildPieceSelectCircles()
        {
            if (pieceCircleContainer == null) return;

            foreach (Transform child in pieceCircleContainer) Destroy(child.gameObject);
            pieceCircles.Clear();

            float spacing = 140f;
            float totalWidth = (GlyphIds.Length - 1) * spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < GlyphIds.Length; i++)
            {
                int glyphId = GlyphIds[i];
                Color col = PieceColors.ContainsKey(glyphId) ? PieceColors[glyphId] : Color.white;

                var go = new GameObject($"PieceCircle_{i}");
                go.transform.SetParent(pieceCircleContainer, false);

                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(120, 120);
                rect.anchoredPosition = new Vector2(startX + i * spacing, 0);

                var img = go.AddComponent<Image>();
                img.sprite = circleSprite;
                img.color = col;
                img.raycastTarget = false;

                // Label
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = PieceNames[i];
                label.fontSize = 16;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.enableWordWrapping = true;

                pieceCircles.Add((rect, img, glyphId));
            }
        }

        private Coroutine countdownCoroutine;

        private void HandlePieceDetected(int glyphId)
        {
            // Highlight the detected piece's circle
            foreach (var (rect, img, glyph) in pieceCircles)
            {
                if (rect == null) continue;
                rect.localScale = (glyph == glyphId) ? Vector3.one * 1.2f : Vector3.one;
            }

            // Enable confirm button (still available as fallback)
            if (confirmButton != null) confirmButton.interactable = true;

            // Show countdown status
            string name = PieceManager.GetPieceName(glyphId);
            if (pieceStatusText != null) pieceStatusText.text = $"{name} detected! Selecting...";

            // Start visual countdown
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(PieceCountdown(name));
        }

        private System.Collections.IEnumerator PieceCountdown(string pieceName)
        {
            float elapsed = 0f;
            float duration = 1.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (pieceStatusText != null)
                {
                    int dots = 1 + (int)(elapsed / 0.5f) % 3;
                    pieceStatusText.text = $"{pieceName} detected{new string('.', dots)}";
                }
                yield return null;
            }
            countdownCoroutine = null;
        }

        private void HandlePieceLost()
        {
            // Cancel countdown
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            // Reset circles
            foreach (var (rect, img, glyph) in pieceCircles)
            {
                if (rect != null) rect.localScale = Vector3.one;
            }

            // Disable confirm button
            if (confirmButton != null) confirmButton.interactable = false;
            if (pieceStatusText != null) pieceStatusText.text = "";
        }

        private void HandlePieceSelected(int glyphId)
        {
            // Visual feedback before transitioning
            foreach (var (rect, img, glyph) in pieceCircles)
            {
                if (glyph == glyphId && rect != null)
                    rect.localScale = Vector3.one * 1.3f;
            }
        }

        private void OnBackClicked()
        {
            SceneManager.LoadScene("LandingPage");
        }

        private void OnConfirmClicked()
        {
            var gm = NullifyGameManager.Instance;
            if (gm != null) gm.ConfirmPieceSelection();
        }

        // ── Level Select ─────────────────────────────────────

        private void BuildLevelSelectCircles()
        {
            if (levelCircleContainer == null) return;

            foreach (Transform child in levelCircleContainer) Destroy(child.gameObject);
            levelCircleData.Clear();
            selectedLevelIndex = -1;

            var gm = NullifyGameManager.Instance;
            if (gm == null) return;

            int totalLevels = NullifyLevel.AllLevels.Count;
            int cols = 7;
            float spacing = 180f;
            float rowSpacing = 180f;

            for (int i = 0; i < totalLevels; i++)
            {
                int row = i / cols;
                int col = i % cols;
                float rowWidth = (Mathf.Min(cols, totalLevels - row * cols) - 1) * spacing;
                float x = -rowWidth / 2f + col * spacing;
                float y = -row * rowSpacing + rowSpacing / 2f;

                bool unlocked = i <= gm.MaxUnlockedLevel;
                Color baseCol = unlocked ? gm.SelectedPieceColor : new Color(0.5f, 0.5f, 0.5f, 0.4f);

                var go = new GameObject($"LevelCircle_{i + 1}");
                go.transform.SetParent(levelCircleContainer, false);

                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(150, 150);
                rect.anchoredPosition = new Vector2(x, y);

                var img = go.AddComponent<Image>();
                img.sprite = circleSprite;
                img.color = baseCol;

                // Number label
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(go.transform, false);
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = (i + 1).ToString();
                label.fontSize = 42;
                label.alignment = TextAlignmentOptions.Center;
                label.color = unlocked ? Color.white : new Color(1, 1, 1, 0.3f);
                label.raycastTarget = false;

                // Make unlocked levels clickable (selects, not starts)
                if (unlocked)
                {
                    var btn = go.AddComponent<Button>();
                    btn.targetGraphic = img;

                    var colors = btn.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1, 1, 1, 0.85f);
                    colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                    btn.colors = colors;

                    int levelIndex = i;
                    btn.onClick.AddListener(() => OnLevelClicked(levelIndex));
                }
                else
                {
                    img.raycastTarget = false;
                }

                levelCircleData.Add((rect, img, i, unlocked));
            }

            // Build Play button and level info below the level grid
            BuildPlayButton();

            // Auto-select the first unlocked level
            int firstUnlocked = Mathf.Min(gm.MaxUnlockedLevel, totalLevels - 1);
            SelectLevelCircle(firstUnlocked);
        }

        private void BuildPlayButton()
        {
            if (levelSelectScreen == null) return;

            // Remove old play button if it exists
            if (playButton != null) Destroy(playButton.gameObject);
            if (levelInfoText != null) Destroy(levelInfoText.gameObject);

            var gm = NullifyGameManager.Instance;
            Color pieceCol = gm != null ? gm.SelectedPieceColor : Color.white;

            // Level info text (shows selected level name)
            var infoGo = new GameObject("LevelInfoText");
            infoGo.transform.SetParent(levelSelectScreen.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 0f);
            infoRect.anchorMax = new Vector2(0.5f, 0f);
            infoRect.pivot = new Vector2(0.5f, 0f);
            infoRect.anchoredPosition = new Vector2(0, 160);
            infoRect.sizeDelta = new Vector2(600, 50);
            levelInfoText = infoGo.AddComponent<TextMeshProUGUI>();
            levelInfoText.text = "";
            levelInfoText.fontSize = 28;
            levelInfoText.alignment = TextAlignmentOptions.Center;
            levelInfoText.color = Color.white;

            // Play button
            var btnGo = new GameObject("PlayButton");
            btnGo.transform.SetParent(levelSelectScreen.transform, false);
            playButtonRect = btnGo.AddComponent<RectTransform>();
            playButtonRect.anchorMin = new Vector2(0.5f, 0f);
            playButtonRect.anchorMax = new Vector2(0.5f, 0f);
            playButtonRect.pivot = new Vector2(0.5f, 0f);
            playButtonRect.anchoredPosition = new Vector2(0, 60);
            playButtonRect.sizeDelta = new Vector2(360, 120);

            playButtonImage = btnGo.AddComponent<Image>();
            playButtonImage.sprite = circleSprite;
            playButtonImage.color = pieceCol;
            playButtonImage.type = Image.Type.Sliced;

            playButton = btnGo.AddComponent<Button>();
            playButton.targetGraphic = playButtonImage;
            playButton.onClick.AddListener(OnPlayClicked);

            var btnColors = playButton.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(1, 1, 1, 0.85f);
            btnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            playButton.colors = btnColors;

            // Play label
            var playLabelGo = new GameObject("PlayLabel");
            playLabelGo.transform.SetParent(btnGo.transform, false);
            var playLabelRect = playLabelGo.AddComponent<RectTransform>();
            playLabelRect.anchorMin = Vector2.zero;
            playLabelRect.anchorMax = Vector2.one;
            playLabelRect.offsetMin = Vector2.zero;
            playLabelRect.offsetMax = Vector2.zero;
            var playLabel = playLabelGo.AddComponent<TextMeshProUGUI>();
            playLabel.text = "▶  PLAY";
            playLabel.fontSize = 48;
            playLabel.fontStyle = FontStyles.Bold;
            playLabel.alignment = TextAlignmentOptions.Center;
            playLabel.color = Color.white;
            playLabel.raycastTarget = false;
        }

        private void SelectLevelCircle(int levelIndex)
        {
            selectedLevelIndex = levelIndex;

            var gm = NullifyGameManager.Instance;
            Color pieceCol = gm != null ? gm.SelectedPieceColor : Color.white;
            Color selectedHighlight = Color.Lerp(pieceCol, Color.white, 0.4f);

            // Update circle visuals
            foreach (var (rect, img, idx, unlocked) in levelCircleData)
            {
                if (rect == null) continue;

                if (idx == selectedLevelIndex && unlocked)
                {
                    rect.localScale = Vector3.one * 1.25f;
                    img.color = selectedHighlight;
                }
                else
                {
                    rect.localScale = Vector3.one;
                    img.color = unlocked ? pieceCol : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                }
            }

            // Update level info text
            if (levelInfoText != null && levelIndex >= 0 && levelIndex < NullifyLevel.AllLevels.Count)
            {
                var level = NullifyLevel.AllLevels[levelIndex];
                levelInfoText.text = $"Level {level.LevelNumber}: {level.Title}";
            }
        }

        private void OnLevelClicked(int levelIndex)
        {
            SelectLevelCircle(levelIndex);
        }

        private void OnPlayClicked()
        {
            if (selectedLevelIndex < 0) return;
            var gm = NullifyGameManager.Instance;
            if (gm != null) gm.SelectLevel(selectedLevelIndex);
        }

        // ── Piece-based UI interaction (runs each frame) ──

        private float pieceDwellOnPlay = -1f;
        private float pieceDwellOnContinue = -1f;
        private const float PIECE_DWELL_TIME = 1.0f;

        private void Update()
        {
            var gm = NullifyGameManager.Instance;
            if (gm == null) return;
            if (gm.TrackedContactId < 0) return;
            if (PieceManager.Instance == null) return;
            if (!PieceManager.Instance.ActivePieces.TryGetValue(gm.TrackedContactId, out var piece)) return;

            Vector2 pieceScreen = piece.screenPosition;

            if (gm.CurrentState == NullifyGameManager.State.LevelSelect)
                UpdatePieceLevelSelect(pieceScreen, gm);
            else if (gm.CurrentState == NullifyGameManager.State.LevelComplete)
                UpdatePieceLevelComplete(pieceScreen);
        }

        private void UpdatePieceLevelSelect(Vector2 pieceScreen, NullifyGameManager gm)
        {
            // Check if piece is hovering over a level circle
            foreach (var (rect, img, idx, unlocked) in levelCircleData)
            {
                if (!unlocked || rect == null) continue;

                if (IsOverRect(rect, pieceScreen))
                {
                    if (selectedLevelIndex != idx)
                        SelectLevelCircle(idx);
                    break;
                }
            }

            // Check if piece is hovering over Play button
            if (playButtonRect != null && IsOverRect(playButtonRect, pieceScreen))
            {
                if (playButtonImage != null)
                    playButtonImage.color = Color.Lerp(playButtonImage.color, Color.green, Time.deltaTime * 4f);

                if (pieceDwellOnPlay < 0f) pieceDwellOnPlay = 0f;
                pieceDwellOnPlay += Time.deltaTime;
                if (pieceDwellOnPlay >= PIECE_DWELL_TIME)
                {
                    pieceDwellOnPlay = -1f;
                    OnPlayClicked();
                }
            }
            else
            {
                pieceDwellOnPlay = -1f;
                if (playButtonImage != null && gm != null)
                    playButtonImage.color = Color.Lerp(playButtonImage.color, gm.SelectedPieceColor, Time.deltaTime * 4f);
            }
        }

        private void UpdatePieceLevelComplete(Vector2 pieceScreen)
        {
            if (continueButton == null) return;
            var continueRect = continueButton.GetComponent<RectTransform>();
            if (continueRect == null) return;

            if (IsOverRect(continueRect, pieceScreen))
            {
                if (pieceDwellOnContinue < 0f) pieceDwellOnContinue = 0f;
                pieceDwellOnContinue += Time.deltaTime;
                if (pieceDwellOnContinue >= PIECE_DWELL_TIME)
                {
                    pieceDwellOnContinue = -1f;
                    OnContinueClicked();
                }
            }
            else
            {
                pieceDwellOnContinue = -1f;
            }
        }

        /// <summary>
        /// Checks if a screen point is within a RectTransform's bounds, with extra padding
        /// for more forgiving touch/piece detection.
        /// </summary>
        private static bool IsOverRect(RectTransform rect, Vector2 screenPoint, float padding = 20f)
        {
            // First try exact bounds
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null))
                return true;

            // If not, check with padding by testing nearby points
            if (padding <= 0f) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(padding, 0), null)
                || RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(-padding, 0), null)
                || RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(0, padding), null)
                || RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint + new Vector2(0, -padding), null);
        }

        // ── Gameplay ─────────────────────────────────────────

        private void HandleLevelSelected(int levelIndex)
        {
            var level = NullifyLevel.AllLevels[levelIndex];
            if (levelTitleText != null) levelTitleText.text = $"Level {level.LevelNumber}: {level.Title}";
            if (moveCountText != null) moveCountText.text = "Moves: 0";
            if (instructionText != null) instructionText.text = "Combine circles to reach zero!";
        }

        private void HandleMoveCountChanged()
        {
            var gm = NullifyGameManager.Instance;
            if (gm != null && moveCountText != null)
                moveCountText.text = $"Moves: {gm.MoveCount}";
        }

        // ── Level Complete ───────────────────────────────────

        private Coroutine autoContinueCoroutine;

        private void HandleLevelCompleted(int stars, int moveCount)
        {
            if (completeTitle != null) completeTitle.text = "Level Complete!";

            if (starsText != null)
            {
                string starStr = new string('\u2605', stars) + new string('\u2606', 3 - stars);
                starsText.text = starStr;
            }

            var gm = NullifyGameManager.Instance;
            if (completeMoveText != null && gm != null)
            {
                var level = gm.GetCurrentLevel();
                int par = level != null ? level.Par : 0;
                completeMoveText.text = $"Moves: {moveCount} (par: {par})";
            }

            if (continueText != null) continueText.text = "Tap anywhere to continue...";

            // Auto-continue after 5 seconds as fallback
            if (autoContinueCoroutine != null) StopCoroutine(autoContinueCoroutine);
            autoContinueCoroutine = StartCoroutine(AutoContinueRoutine());
        }

        private System.Collections.IEnumerator AutoContinueRoutine()
        {
            float remaining = 5f;
            while (remaining > 0f)
            {
                if (continueText != null)
                    continueText.text = $"Tap anywhere to continue ({Mathf.CeilToInt(remaining)}s)...";
                remaining -= Time.deltaTime;
                yield return null;
            }
            autoContinueCoroutine = null;
            OnContinueClicked();
        }

        private void OnContinueClicked()
        {
            if (autoContinueCoroutine != null)
            {
                StopCoroutine(autoContinueCoroutine);
                autoContinueCoroutine = null;
            }
            var gm = NullifyGameManager.Instance;
            if (gm != null) gm.ReturnToLevelSelect();
        }

        // ── Helpers ──────────────────────────────────────────

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
