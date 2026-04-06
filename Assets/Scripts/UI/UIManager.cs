using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BoardOfEducation.Core;
using BoardOfEducation.Lessons;
using BoardOfEducation.Input;

namespace BoardOfEducation.UI
{
    /// <summary>
    /// Manages the full-screen UI for the 2-player Board layout (1920x1080).
    /// Top half (540px) = Player 2 (rotated 180°), Bottom half = Player 1.
    /// Center strip = shared game area where blocks are placed.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Canvases")]
        [SerializeField] private Canvas mainCanvas;

        [Header("Menu")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Player 1 (Bottom)")]
        [SerializeField] private RectTransform player1Panel;
        [SerializeField] private TextMeshProUGUI p1InstructionText;
        [SerializeField] private TextMeshProUGUI p1FeedbackText;
        [SerializeField] private TextMeshProUGUI p1ScoreText;
        [SerializeField] private Button p1HintButton;

        [Header("Player 2 (Top, rotated 180°)")]
        [SerializeField] private RectTransform player2Panel;
        [SerializeField] private TextMeshProUGUI p2InstructionText;
        [SerializeField] private TextMeshProUGUI p2FeedbackText;
        [SerializeField] private TextMeshProUGUI p2ScoreText;
        [SerializeField] private Button p2HintButton;

        [Header("Shared Area (Center)")]
        [SerializeField] private RectTransform sharedArea;
        [SerializeField] private TextMeshProUGUI problemText;
        [SerializeField] private TextMeshProUGUI scaffoldLabel;
        [SerializeField] private Image progressBar;

        [Header("Completion")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TextMeshProUGUI completionText;
        [SerializeField] private Button replayButton;

        [Header("Visual Feedback")]
        [SerializeField] private Image feedbackFlash;
        [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color incorrectColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

        // Grid visualization for fraction problems
        [Header("Fraction Grid")]
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private GameObject gridCellPrefab;

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
            // Wire up buttons
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (replayButton != null)
                replayButton.onClick.AddListener(OnStartClicked);
            if (p1HintButton != null)
                p1HintButton.onClick.AddListener(OnHintRequested);
            if (p2HintButton != null)
                p2HintButton.onClick.AddListener(OnHintRequested);

            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
                GameManager.Instance.OnScaffoldChanged += OnScaffoldChanged;
            }

            if (LessonController.Instance != null)
            {
                LessonController.Instance.OnProblemPresented += OnProblemPresented;
                LessonController.Instance.OnAnswerChecked += OnAnswerChecked;
                LessonController.Instance.OnHintAvailable += OnHintReceived;
            }

            // Player 2's panel is rotated 180° so they can read from the other side
            if (player2Panel != null)
                player2Panel.localRotation = Quaternion.Euler(0, 0, 180);

            ShowMenu();
        }

        private void OnStartClicked()
        {
            GameManager.Instance?.StartGame();
        }

        private void OnHintRequested()
        {
            LessonController.Instance?.RequestHint();
        }

        private void OnGameStateChanged(GameManager.GameState state)
        {
            switch (state)
            {
                case GameManager.GameState.Menu:
                    ShowMenu();
                    break;
                case GameManager.GameState.Playing:
                    ShowGameplay();
                    break;
                case GameManager.GameState.LessonComplete:
                    ShowCompletion();
                    break;
            }
        }

        private void OnScaffoldChanged(GameManager.Scaffold scaffold)
        {
            UpdateScaffoldDisplay();
            UpdateHintButtonVisibility();
        }

        private void OnProblemPresented(FractionProblem problem, string instruction)
        {
            // Update both players' displays
            SetText(p1InstructionText, instruction);
            SetText(p2InstructionText, instruction);
            SetText(problemText, problem.Prompt);

            // Clear feedback
            SetText(p1FeedbackText, "");
            SetText(p2FeedbackText, "");

            // Update grid visualization
            UpdateFractionGrid(problem);
        }

        private void OnAnswerChecked(bool correct, string feedback)
        {
            SetText(p1FeedbackText, feedback);
            SetText(p2FeedbackText, feedback);

            UpdateScores();

            // Flash feedback
            StartCoroutine(FlashFeedback(correct));
        }

        private void OnHintReceived(string hint)
        {
            SetText(p1InstructionText, hint);
            SetText(p2InstructionText, hint);
        }

        private void ShowMenu()
        {
            SetActive(menuPanel, true);
            SetActive(player1Panel, false);
            SetActive(player2Panel, false);
            SetActive(sharedArea, false);
            SetActive(completionPanel, false);
        }

        private void ShowGameplay()
        {
            SetActive(menuPanel, false);
            SetActive(player1Panel, true);
            SetActive(player2Panel, true);
            SetActive(sharedArea, true);
            SetActive(completionPanel, false);

            UpdateScaffoldDisplay();
            UpdateScores();
            UpdateHintButtonVisibility();
        }

        private void ShowCompletion()
        {
            SetActive(completionPanel, true);
            SetActive(player1Panel, false);
            SetActive(player2Panel, false);

            var gm = GameManager.Instance;
            if (completionText != null && gm != null)
            {
                completionText.text = $"Lesson Complete!\n\n" +
                    $"Team Score: {gm.TeamScore}\n" +
                    $"Player 1: {gm.Player1Score}  |  Player 2: {gm.Player2Score}\n\n" +
                    $"You've mastered fraction equivalence!";
            }
        }

        private void UpdateScaffoldDisplay()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            SetText(scaffoldLabel, gm.GetScaffoldLabel());

            if (progressBar != null)
                progressBar.fillAmount = gm.GetScaffoldProgress();
        }

        private void UpdateScores()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            SetText(p1ScoreText, $"Score: {gm.Player1Score}");
            SetText(p2ScoreText, $"Score: {gm.Player2Score}");
        }

        private void UpdateHintButtonVisibility()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool showHint = gm.CurrentScaffold == GameManager.Scaffold.E2_GuidedPractice ||
                            gm.CurrentScaffold == GameManager.Scaffold.E3_MinimalHints;

            SetActive(p1HintButton, showHint);
            SetActive(p2HintButton, showHint);
        }

        private void UpdateFractionGrid(FractionProblem problem)
        {
            if (gridContainer == null || gridCellPrefab == null) return;

            // Clear existing cells
            foreach (Transform child in gridContainer)
                Destroy(child.gameObject);

            // Create grid cells based on denominator
            int cols = problem.Denominator <= 4 ? problem.Denominator : problem.Denominator / 2;
            int rows = problem.Denominator <= 4 ? 1 : 2;

            float cellWidth = gridContainer.rect.width / cols;
            float cellHeight = gridContainer.rect.height / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = Instantiate(gridCellPrefab, gridContainer);
                    var rt = cell.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2((float)c / cols, (float)r / rows);
                        rt.anchorMax = new Vector2((float)(c + 1) / cols, (float)(r + 1) / rows);
                        rt.offsetMin = Vector2.one * 2; // 2px padding
                        rt.offsetMax = -Vector2.one * 2;
                    }

                    // Color cells that should be filled (for E1 instruction)
                    int index = r * cols + c;
                    var image = cell.GetComponent<Image>();
                    if (image != null)
                    {
                        if (index < problem.Numerator &&
                            GameManager.Instance?.CurrentScaffold == GameManager.Scaffold.E1_FullInstruction)
                        {
                            // In E1, highlight target cells as a guide
                            image.color = new Color(0.6f, 0.8f, 1f, 0.5f);
                        }
                        else
                        {
                            image.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
                        }
                    }
                }
            }
        }

        private IEnumerator FlashFeedback(bool correct)
        {
            if (feedbackFlash == null) yield break;

            feedbackFlash.color = correct ? correctColor : incorrectColor;
            feedbackFlash.gameObject.SetActive(true);

            float elapsed = 0f;
            float duration = 0.8f;
            Color startColor = feedbackFlash.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                feedbackFlash.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            feedbackFlash.gameObject.SetActive(false);
        }

        // Safe null-checking helpers
        private void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null) text.text = value;
        }

        private void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
