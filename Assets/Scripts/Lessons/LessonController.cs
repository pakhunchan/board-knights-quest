using UnityEngine;
using System;
using System.Collections.Generic;
using BoardOfEducation.Core;
using BoardOfEducation.Input;

namespace BoardOfEducation.Lessons
{
    /// <summary>
    /// Manages the active lesson, presents problems according to the current
    /// scaffold level (E1-E4), and validates piece placements as answers.
    /// </summary>
    public class LessonController : MonoBehaviour
    {
        public static LessonController Instance { get; private set; }

        [Header("Lesson Settings")]
        [SerializeField] private float answerCheckDelay = 1.5f; // seconds after placement before checking

        private List<FractionProblem> problems;
        private FractionProblem currentProblem;
        private int currentProblemIndex;

        // Track placed pieces for current problem
        private List<PieceManager.PieceContact> placedPiecesForProblem = new List<PieceManager.PieceContact>();
        private float lastPlacementTime;
        private bool waitingToCheck;

        // Events for UI
        public event Action<FractionProblem, string> OnProblemPresented; // problem, instruction text
        public event Action<bool, string> OnAnswerChecked; // correct, feedback message
        public event Action<string> OnHintAvailable;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            problems = FractionProblem.CreateProblemBank();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScaffoldChanged += OnScaffoldChanged;
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            if (PieceManager.Instance != null)
            {
                PieceManager.Instance.OnPiecePlaced += OnPiecePlaced;
                PieceManager.Instance.OnPieceRemoved += OnPieceRemoved;
            }
        }

        private void Update()
        {
            // Auto-check answer after a delay when pieces are placed
            if (waitingToCheck && Time.time - lastPlacementTime >= answerCheckDelay)
            {
                waitingToCheck = false;
                CheckAnswer();
            }
        }

        private void OnGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.Playing)
            {
                currentProblemIndex = 0;
                PresentProblem();
            }
        }

        private void OnScaffoldChanged(GameManager.Scaffold scaffold)
        {
            // When scaffold changes, restart problem index within the bank
            // Higher scaffolds start from later problems (harder ones)
            currentProblemIndex = scaffold switch
            {
                GameManager.Scaffold.E1_FullInstruction => 0,
                GameManager.Scaffold.E2_GuidedPractice => 2,
                GameManager.Scaffold.E3_MinimalHints => 4,
                GameManager.Scaffold.E4_PurePractice => 6,
                _ => 0
            };
            PresentProblem();
        }

        public void PresentProblem()
        {
            if (currentProblemIndex >= problems.Count)
                currentProblemIndex = 0; // wrap around

            currentProblem = problems[currentProblemIndex];
            placedPiecesForProblem.Clear();
            waitingToCheck = false;

            string instruction = GetInstructionForCurrentScaffold();
            OnProblemPresented?.Invoke(currentProblem, instruction);

            // Log the event
            Logging.InteractionLogger.Instance?.LogGameEvent(
                "problem_presented",
                $"{currentProblem.Prompt} [{GameManager.Instance?.CurrentScaffold}]"
            );
        }

        private string GetInstructionForCurrentScaffold()
        {
            if (GameManager.Instance == null) return currentProblem.E1_Instruction;

            return GameManager.Instance.CurrentScaffold switch
            {
                GameManager.Scaffold.E1_FullInstruction => currentProblem.E1_Instruction,
                GameManager.Scaffold.E2_GuidedPractice => currentProblem.E2_Hint,
                GameManager.Scaffold.E3_MinimalHints => currentProblem.E3_Nudge,
                GameManager.Scaffold.E4_PurePractice => currentProblem.Prompt, // Just the problem, no help
                _ => currentProblem.Prompt
            };
        }

        private void OnPiecePlaced(PieceManager.PieceContact contact)
        {
            if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

            placedPiecesForProblem.Add(contact);
            lastPlacementTime = Time.time;

            // Start or reset the check timer
            if (placedPiecesForProblem.Count >= currentProblem.RequiredBlockCount)
            {
                waitingToCheck = true;
            }
        }

        private void OnPieceRemoved(PieceManager.PieceContact contact)
        {
            placedPiecesForProblem.RemoveAll(p => p.contactId == contact.contactId);
            waitingToCheck = false; // Reset check timer when pieces change
        }

        private void CheckAnswer()
        {
            if (currentProblem == null) return;

            bool correct = EvaluatePlacement();

            string feedback;
            if (correct)
            {
                feedback = GetCorrectFeedback();
                GameManager.Instance?.RecordAnswer(true, DetermineActivePlayer());
                Logging.InteractionLogger.Instance?.LogGameEvent("answer_correct", currentProblem.Prompt);
            }
            else
            {
                feedback = GetIncorrectFeedback();
                GameManager.Instance?.RecordAnswer(false, DetermineActivePlayer());
                Logging.InteractionLogger.Instance?.LogGameEvent("answer_incorrect", currentProblem.Prompt);
            }

            OnAnswerChecked?.Invoke(correct, feedback);

            if (correct)
            {
                // Move to next problem after a short delay
                Invoke(nameof(NextProblem), 2f);
            }
        }

        private bool EvaluatePlacement()
        {
            // Check 1: Do we have the right number of pieces?
            if (placedPiecesForProblem.Count < currentProblem.RequiredBlockCount)
                return false;

            // Check 2: Are the placed pieces from the accepted set?
            int validCount = 0;
            foreach (var piece in placedPiecesForProblem)
            {
                if (currentProblem.AcceptedGlyphIds.Contains(piece.glyphId))
                    validCount++;
            }

            // At least the required number of valid pieces must be placed
            return validCount >= currentProblem.RequiredBlockCount;
        }

        private string GetCorrectFeedback()
        {
            string[] responses = new[]
            {
                "Excellent! That's right!",
                "Perfect! Great teamwork!",
                "You got it! Well done!",
                "Correct! You're fraction masters!",
                "Awesome! Keep going!"
            };
            return responses[UnityEngine.Random.Range(0, responses.Length)];
        }

        private string GetIncorrectFeedback()
        {
            var scaffold = GameManager.Instance?.CurrentScaffold ?? GameManager.Scaffold.E1_FullInstruction;

            return scaffold switch
            {
                GameManager.Scaffold.E1_FullInstruction =>
                    $"Not quite. {currentProblem.E1_Instruction}\nTry again!",
                GameManager.Scaffold.E2_GuidedPractice =>
                    $"Close! {currentProblem.E2_Hint}",
                GameManager.Scaffold.E3_MinimalHints =>
                    "Try again!",
                GameManager.Scaffold.E4_PurePractice =>
                    "Not quite. Try different blocks.",
                _ => "Try again!"
            };
        }

        private int DetermineActivePlayer()
        {
            // In collaborative mode, alternate credit between players
            return (currentProblemIndex % 2 == 0) ? 1 : 2;
        }

        private void NextProblem()
        {
            currentProblemIndex++;
            placedPiecesForProblem.Clear();

            if (currentProblemIndex >= problems.Count)
            {
                GameManager.Instance?.SetState(GameManager.GameState.LessonComplete);
            }
            else
            {
                PresentProblem();
            }
        }

        public void RequestHint()
        {
            if (currentProblem == null) return;

            var scaffold = GameManager.Instance?.CurrentScaffold ?? GameManager.Scaffold.E1_FullInstruction;

            // Only provide hints in E2 and E3
            string hint = scaffold switch
            {
                GameManager.Scaffold.E2_GuidedPractice => currentProblem.E1_Instruction, // Escalate to full instruction
                GameManager.Scaffold.E3_MinimalHints => currentProblem.E2_Hint, // Escalate to guided
                _ => null
            };

            if (hint != null)
            {
                OnHintAvailable?.Invoke(hint);
                Logging.InteractionLogger.Instance?.LogGameEvent("hint_requested", currentProblem.Prompt);
            }
        }

        public FractionProblem GetCurrentProblem() => currentProblem;
    }
}
