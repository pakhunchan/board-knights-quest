using UnityEngine;
using System;
using BoardOfEducation.Input;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Central state machine for Nullify.
    /// PieceSelect → LevelSelect → Playing → LevelComplete → LevelSelect …
    /// Tracks the player's chosen piece by glyphId (not contactId).
    /// </summary>
    public class NullifyGameManager : MonoBehaviour
    {
        public static NullifyGameManager Instance { get; private set; }

        public enum State { PieceSelect, LevelSelect, Playing, LevelComplete }

        // ── Public state ──
        public State CurrentState { get; private set; } = State.PieceSelect;
        public int SelectedGlyphId { get; private set; } = -1;
        public int TrackedContactId { get; private set; } = -1;
        public int CurrentLevelIndex { get; private set; }
        public int MoveCount { get; private set; }
        public int MaxUnlockedLevel
        {
            get => PlayerPrefs.GetInt("nullify_max_level", 0);
            private set { PlayerPrefs.SetInt("nullify_max_level", value); PlayerPrefs.Save(); }
        }

        /// <summary>GlyphId of piece currently on board during PieceSelect, or -1.</summary>
        public int DetectedGlyphId { get; private set; } = -1;

        /// <summary>Seconds to wait after detecting a piece before auto-confirming.</summary>
        private const float AUTO_CONFIRM_DELAY = 1.5f;
        private Coroutine autoConfirmCoroutine;

        // ── Events ──
        public event Action<State> OnStateChanged;
        public event Action<int> OnLevelSelected;     // level index
        public event Action<int, int> OnLevelCompleted; // stars, moveCount
        public event Action<int> OnPieceSelected;      // glyphId
        public event Action OnMoveCountChanged;
        public event Action<int> OnPieceDetected;      // glyphId placed during PieceSelect
        public event Action OnPieceLost;               // piece removed during PieceSelect

        // ── Piece color for UI ──
        public Color SelectedPieceColor { get; private set; } = Color.white;

        private static readonly System.Collections.Generic.Dictionary<int, Color> PieceColors
            = new System.Collections.Generic.Dictionary<int, Color>
        {
            { PieceManager.ArcadeGlyphs.RobotYellow, HexColor("#FFD700") },
            { PieceManager.ArcadeGlyphs.RobotPurple, HexColor("#9B59B6") },
            { PieceManager.ArcadeGlyphs.RobotOrange, HexColor("#FF6B35") },
            { PieceManager.ArcadeGlyphs.RobotPink,   HexColor("#FF69B4") },
            { PieceManager.ArcadeGlyphs.ShipPink,     HexColor("#FF1493") },
            { PieceManager.ArcadeGlyphs.ShipYellow,   HexColor("#F1C40F") },
            { PieceManager.ArcadeGlyphs.ShipPurple,   HexColor("#8E44AD") },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPiecePlaced += HandlePiecePlaced;
                PieceManager.Instance.OnPieceRemoved += HandlePieceRemoved;
                PieceManager.Instance.OnPieceMoved += HandlePieceMoved;
            }
            else
            {
                Debug.LogError("[Nullify] PieceManager not found! Piece detection won't work.");
            }
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

        // ── Piece tracking ──────────────────────────────────────

        private void HandlePiecePlaced(PieceManager.PieceContact piece)
        {
            if (CurrentState == State.PieceSelect)
            {
                DetectedGlyphId = piece.glyphId;
                TrackedContactId = piece.contactId;
                OnPieceDetected?.Invoke(piece.glyphId);
                StartAutoConfirm();
            }
            else if (SelectedGlyphId >= 0 && piece.glyphId == SelectedGlyphId)
            {
                TrackedContactId = piece.contactId;
            }
        }

        private void HandlePieceRemoved(PieceManager.PieceContact piece)
        {
            if (CurrentState == State.PieceSelect && piece.glyphId == DetectedGlyphId)
            {
                CancelAutoConfirm();
                DetectedGlyphId = -1;
                OnPieceLost?.Invoke();
            }

            if (piece.contactId == TrackedContactId)
            {
                TrackedContactId = -1;
            }
        }

        private void StartAutoConfirm()
        {
            CancelAutoConfirm();
            autoConfirmCoroutine = StartCoroutine(AutoConfirmRoutine());
        }

        private void CancelAutoConfirm()
        {
            if (autoConfirmCoroutine != null)
            {
                StopCoroutine(autoConfirmCoroutine);
                autoConfirmCoroutine = null;
            }
        }

        private System.Collections.IEnumerator AutoConfirmRoutine()
        {
            yield return new WaitForSeconds(AUTO_CONFIRM_DELAY);
            autoConfirmCoroutine = null;
            if (CurrentState == State.PieceSelect && DetectedGlyphId >= 0)
            {
                ConfirmPieceSelection();
            }
        }

        private void HandlePieceMoved(PieceManager.PieceContact piece)
        {
            if (SelectedGlyphId >= 0 && piece.glyphId == SelectedGlyphId)
            {
                TrackedContactId = piece.contactId;
            }
        }

        // ── State transitions ───────────────────────────────────

        /// <summary>Called by UI confirm button. Selects the currently detected piece.</summary>
        public void ConfirmPieceSelection()
        {
            if (DetectedGlyphId < 0) return;

            SelectedGlyphId = DetectedGlyphId;
            SelectedPieceColor = PieceColors.ContainsKey(SelectedGlyphId) ? PieceColors[SelectedGlyphId] : Color.white;
            OnPieceSelected?.Invoke(SelectedGlyphId);
            SetState(State.LevelSelect);
            Debug.Log($"[Nullify] Piece selected: {PieceManager.GetPieceName(SelectedGlyphId)}");
        }

        public void SelectLevel(int index)
        {
            if (index < 0 || index >= NullifyLevel.AllLevels.Count) return;
            if (index > MaxUnlockedLevel) return;

            CurrentLevelIndex = index;
            MoveCount = 0;
            OnMoveCountChanged?.Invoke();
            OnLevelSelected?.Invoke(index);
            SetState(State.Playing);
            Debug.Log($"[Nullify] Starting level {index + 1}: {NullifyLevel.AllLevels[index].Title}");
        }

        public void RecordMove()
        {
            MoveCount++;
            OnMoveCountChanged?.Invoke();
        }

        public void CompleteLevelCheck(int remainingCircles)
        {
            if (remainingCircles > 0) return;

            var level = NullifyLevel.AllLevels[CurrentLevelIndex];
            int stars = CalculateStars(MoveCount, level.Par);

            if (CurrentLevelIndex + 1 > MaxUnlockedLevel)
                MaxUnlockedLevel = CurrentLevelIndex + 1;

            OnLevelCompleted?.Invoke(stars, MoveCount);
            SetState(State.LevelComplete);
            Debug.Log($"[Nullify] Level {CurrentLevelIndex + 1} complete! {stars} stars, {MoveCount} moves (par {level.Par})");
        }

        public void ReturnToLevelSelect()
        {
            SetState(State.LevelSelect);
        }

        public void ReturnToPieceSelect()
        {
            SelectedGlyphId = -1;
            TrackedContactId = -1;
            DetectedGlyphId = -1;
            SetState(State.PieceSelect);
        }

        private void SetState(State newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public static int CalculateStars(int moves, int par)
        {
            if (moves <= par) return 3;
            if (moves <= par + 1) return 2;
            return 1;
        }

        public NullifyLevel GetCurrentLevel()
        {
            if (CurrentLevelIndex < 0 || CurrentLevelIndex >= NullifyLevel.AllLevels.Count) return null;
            return NullifyLevel.AllLevels[CurrentLevelIndex];
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
