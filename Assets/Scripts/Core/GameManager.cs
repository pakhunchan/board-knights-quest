using UnityEngine;
using System;

namespace BoardOfEducation.Core
{
    /// <summary>
    /// Central game state manager. Owns lesson progression (E1-E4),
    /// player state, and coordinates all subsystems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int targetFrameRate = 60;

        public enum GameState { Menu, Playing, Paused, LessonComplete, GameOver }
        public enum Scaffold { E1_FullInstruction, E2_GuidedPractice, E3_MinimalHints, E4_PurePractice }

        public GameState CurrentState { get; private set; } = GameState.Menu;
        public Scaffold CurrentScaffold { get; private set; } = Scaffold.E1_FullInstruction;

        public event Action<GameState> OnStateChanged;
        public event Action<Scaffold> OnScaffoldChanged;

        // Track both players' progress
        public int Player1Score { get; private set; }
        public int Player2Score { get; private set; }
        public int TeamScore => Player1Score + Player2Score;
        public int CurrentProblemIndex { get; private set; }
        public int ProblemsPerScaffold => 5;

        private int correctInARow;
        private const int ADVANCE_THRESHOLD = 3; // correct in a row to advance scaffold

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = targetFrameRate;
        }

        private void Start()
        {
            SetState(GameState.Menu);
        }

        public void StartGame()
        {
            Player1Score = 0;
            Player2Score = 0;
            CurrentProblemIndex = 0;
            correctInARow = 0;
            CurrentScaffold = Scaffold.E1_FullInstruction;
            SetState(GameState.Playing);
            OnScaffoldChanged?.Invoke(CurrentScaffold);
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Called when players answer a problem. Both players collaborate,
        /// so a correct answer benefits the team.
        /// </summary>
        public void RecordAnswer(bool correct, int playerNumber)
        {
            if (correct)
            {
                if (playerNumber == 1) Player1Score++;
                else Player2Score++;

                correctInARow++;
                if (correctInARow >= ADVANCE_THRESHOLD)
                {
                    TryAdvanceScaffold();
                    correctInARow = 0;
                }
            }
            else
            {
                correctInARow = 0;
            }

            CurrentProblemIndex++;
        }

        public void AdvanceToNextProblem()
        {
            CurrentProblemIndex++;
        }

        private void TryAdvanceScaffold()
        {
            switch (CurrentScaffold)
            {
                case Scaffold.E1_FullInstruction:
                    SetScaffold(Scaffold.E2_GuidedPractice);
                    break;
                case Scaffold.E2_GuidedPractice:
                    SetScaffold(Scaffold.E3_MinimalHints);
                    break;
                case Scaffold.E3_MinimalHints:
                    SetScaffold(Scaffold.E4_PurePractice);
                    break;
                case Scaffold.E4_PurePractice:
                    SetState(GameState.LessonComplete);
                    break;
            }
        }

        public void SetScaffold(Scaffold scaffold)
        {
            CurrentScaffold = scaffold;
            CurrentProblemIndex = 0;
            OnScaffoldChanged?.Invoke(scaffold);
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
                SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
                SetState(GameState.Playing);
        }

        /// <summary>
        /// Returns the instruction level text for UI display.
        /// </summary>
        public string GetScaffoldLabel()
        {
            return CurrentScaffold switch
            {
                Scaffold.E1_FullInstruction => "Learn",
                Scaffold.E2_GuidedPractice => "Practice",
                Scaffold.E3_MinimalHints => "Challenge",
                Scaffold.E4_PurePractice => "Master",
                _ => ""
            };
        }

        public float GetScaffoldProgress()
        {
            return (int)CurrentScaffold / 3f;
        }
    }
}
