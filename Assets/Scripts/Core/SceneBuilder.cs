#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace BoardOfEducation.Core
{
    /// <summary>
    /// Editor-only script that builds the full game scene with one click.
    /// Access via menu: Board of Education > Build Game Scene
    /// </summary>
    public static class SceneBuilder
    {
        [MenuItem("Board of Education/Build Game Scene")]
        public static void BuildScene()
        {
            // Clean up existing scene objects (except camera)
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name != "Main Camera" && go.transform.parent == null)
                    Object.DestroyImmediate(go);
            }

            // === CAMERA SETUP (2D) ===
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.4f; // half of 1080 / 100 pixels-per-unit
                cam.transform.position = new Vector3(9.6f, 5.4f, -10f); // center of 1920x1080
                cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            // === GAME MANAGER ===
            var gameManagerGO = new GameObject("GameManager");
            gameManagerGO.AddComponent<GameManager>();
            gameManagerGO.AddComponent<Input.PieceManager>();
            gameManagerGO.AddComponent<Logging.InteractionLogger>();
            gameManagerGO.AddComponent<Lessons.LessonController>();
            gameManagerGO.AddComponent<Progression.ProgressionManager>();
            gameManagerGO.AddComponent<BoardStartup>();

            // === EVENT SYSTEM ===
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            // Use New Input System UI module (required by Board SDK)
            eventSystem.AddComponent<InputSystemUIInputModule>();

            // === MAIN CANVAS (Screen Space - Overlay, 1920x1080) ===
            var canvasGO = new GameObject("MainCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // === BACKGROUND ===
            var bgGO = CreatePanel(canvasGO.transform, "Background", new Color(0.08f, 0.1f, 0.15f));
            SetFullStretch(bgGO);
            bgGO.GetComponent<Image>().raycastTarget = false;

            // === SHARED CENTER AREA ===
            var centerGO = CreatePanel(canvasGO.transform, "SharedArea", new Color(0.12f, 0.15f, 0.22f));
            var centerRT = centerGO.GetComponent<RectTransform>();
            centerRT.anchorMin = new Vector2(0, 0.25f);
            centerRT.anchorMax = new Vector2(1, 0.75f);
            centerRT.offsetMin = Vector2.zero;
            centerRT.offsetMax = Vector2.zero;

            // Problem text (center)
            var problemGO = CreateText(centerGO.transform, "ProblemText", "Press START to begin!",
                48, TextAlignmentOptions.Center, Color.white);
            var problemRT = problemGO.GetComponent<RectTransform>();
            problemRT.anchorMin = new Vector2(0.1f, 0.6f);
            problemRT.anchorMax = new Vector2(0.9f, 0.95f);
            problemRT.offsetMin = Vector2.zero;
            problemRT.offsetMax = Vector2.zero;

            // Scaffold label
            var scaffoldGO = CreateText(centerGO.transform, "ScaffoldLabel", "",
                28, TextAlignmentOptions.Center, new Color(0.5f, 0.8f, 1f));
            var scaffoldRT = scaffoldGO.GetComponent<RectTransform>();
            scaffoldRT.anchorMin = new Vector2(0.35f, 0.4f);
            scaffoldRT.anchorMax = new Vector2(0.65f, 0.55f);
            scaffoldRT.offsetMin = Vector2.zero;
            scaffoldRT.offsetMax = Vector2.zero;

            // Progress bar background
            var progressBgGO = CreatePanel(centerGO.transform, "ProgressBarBG", new Color(0.2f, 0.2f, 0.3f));
            var progressBgRT = progressBgGO.GetComponent<RectTransform>();
            progressBgRT.anchorMin = new Vector2(0.3f, 0.3f);
            progressBgRT.anchorMax = new Vector2(0.7f, 0.35f);
            progressBgRT.offsetMin = Vector2.zero;
            progressBgRT.offsetMax = Vector2.zero;

            var progressFillGO = CreatePanel(progressBgGO.transform, "ProgressBarFill", new Color(0.3f, 0.7f, 1f));
            var progressFillRT = progressFillGO.GetComponent<RectTransform>();
            progressFillRT.anchorMin = Vector2.zero;
            progressFillRT.anchorMax = new Vector2(0, 1); // starts empty
            progressFillRT.offsetMin = Vector2.zero;
            progressFillRT.offsetMax = Vector2.zero;
            var progressImage = progressFillGO.GetComponent<Image>();
            progressImage.type = Image.Type.Filled;
            progressImage.fillMethod = Image.FillMethod.Horizontal;

            // Grid container for fraction visualization
            var gridGO = CreatePanel(centerGO.transform, "GridContainer", new Color(0.15f, 0.18f, 0.25f, 0.5f));
            var gridRT = gridGO.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.3f, 0.05f);
            gridRT.anchorMax = new Vector2(0.7f, 0.25f);
            gridRT.offsetMin = Vector2.zero;
            gridRT.offsetMax = Vector2.zero;

            // === PLAYER 1 PANEL (BOTTOM) ===
            var p1GO = CreatePanel(canvasGO.transform, "Player1Panel", new Color(0.1f, 0.15f, 0.1f, 0.8f));
            var p1RT = p1GO.GetComponent<RectTransform>();
            p1RT.anchorMin = new Vector2(0, 0);
            p1RT.anchorMax = new Vector2(1, 0.25f);
            p1RT.offsetMin = Vector2.zero;
            p1RT.offsetMax = Vector2.zero;

            var p1Label = CreateText(p1GO.transform, "P1Label", "PLAYER 1",
                20, TextAlignmentOptions.TopLeft, new Color(0.5f, 1f, 0.5f));
            SetAnchored(p1Label, new Vector2(0.02f, 0.7f), new Vector2(0.2f, 0.95f));

            var p1Score = CreateText(p1GO.transform, "P1ScoreText", "Score: 0",
                24, TextAlignmentOptions.TopRight, Color.white);
            SetAnchored(p1Score, new Vector2(0.8f, 0.7f), new Vector2(0.98f, 0.95f));

            var p1Instruction = CreateText(p1GO.transform, "P1InstructionText", "",
                22, TextAlignmentOptions.TopLeft, Color.white);
            SetAnchored(p1Instruction, new Vector2(0.02f, 0.1f), new Vector2(0.7f, 0.7f));

            var p1Feedback = CreateText(p1GO.transform, "P1FeedbackText", "",
                26, TextAlignmentOptions.Center, Color.yellow);
            SetAnchored(p1Feedback, new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.7f));

            var p1HintBtn = CreateButton(p1GO.transform, "P1HintButton", "Hint",
                new Color(0.3f, 0.5f, 0.8f));
            SetAnchored(p1HintBtn, new Vector2(0.02f, 0.02f), new Vector2(0.15f, 0.15f));

            // === PLAYER 2 PANEL (TOP, rotated 180°) ===
            var p2GO = CreatePanel(canvasGO.transform, "Player2Panel", new Color(0.15f, 0.1f, 0.1f, 0.8f));
            var p2RT = p2GO.GetComponent<RectTransform>();
            p2RT.anchorMin = new Vector2(0, 0.75f);
            p2RT.anchorMax = new Vector2(1, 1);
            p2RT.offsetMin = Vector2.zero;
            p2RT.offsetMax = Vector2.zero;
            p2RT.localRotation = Quaternion.Euler(0, 0, 180); // Flipped for facing player

            var p2Label = CreateText(p2GO.transform, "P2Label", "PLAYER 2",
                20, TextAlignmentOptions.TopLeft, new Color(1f, 0.5f, 0.5f));
            SetAnchored(p2Label, new Vector2(0.02f, 0.7f), new Vector2(0.2f, 0.95f));

            var p2Score = CreateText(p2GO.transform, "P2ScoreText", "Score: 0",
                24, TextAlignmentOptions.TopRight, Color.white);
            SetAnchored(p2Score, new Vector2(0.8f, 0.7f), new Vector2(0.98f, 0.95f));

            var p2Instruction = CreateText(p2GO.transform, "P2InstructionText", "",
                22, TextAlignmentOptions.TopLeft, Color.white);
            SetAnchored(p2Instruction, new Vector2(0.02f, 0.1f), new Vector2(0.7f, 0.7f));

            var p2Feedback = CreateText(p2GO.transform, "P2FeedbackText", "",
                26, TextAlignmentOptions.Center, Color.yellow);
            SetAnchored(p2Feedback, new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.7f));

            var p2HintBtn = CreateButton(p2GO.transform, "P2HintButton", "Hint",
                new Color(0.8f, 0.3f, 0.3f));
            SetAnchored(p2HintBtn, new Vector2(0.02f, 0.02f), new Vector2(0.15f, 0.15f));

            // === MENU PANEL (overlay) ===
            var menuGO = CreatePanel(canvasGO.transform, "MenuPanel", new Color(0.05f, 0.07f, 0.12f, 0.95f));
            SetFullStretch(menuGO);

            var titleText = CreateText(menuGO.transform, "TitleText",
                "BOARD OF EDUCATION\nFraction Explorers",
                56, TextAlignmentOptions.Center, Color.white);
            SetAnchored(titleText, new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.8f));

            var subtitleText = CreateText(menuGO.transform, "SubtitleText",
                "A collaborative math adventure!\nPlace pieces on the board to solve fraction puzzles.\n\n2 Players - Work Together!",
                28, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.8f));
            SetAnchored(subtitleText, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.5f));

            var startBtn = CreateButton(menuGO.transform, "StartButton", "START GAME",
                new Color(0.2f, 0.6f, 0.3f));
            SetAnchored(startBtn, new Vector2(0.35f, 0.15f), new Vector2(0.65f, 0.28f));
            // Make the start button text bigger
            var startBtnText = startBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (startBtnText != null) startBtnText.fontSize = 36;

            // === COMPLETION PANEL (hidden by default) ===
            var completionGO = CreatePanel(canvasGO.transform, "CompletionPanel", new Color(0.05f, 0.07f, 0.12f, 0.95f));
            SetFullStretch(completionGO);
            completionGO.SetActive(false);

            var completionText = CreateText(completionGO.transform, "CompletionText",
                "Lesson Complete!",
                48, TextAlignmentOptions.Center, Color.white);
            SetAnchored(completionText, new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.8f));

            var replayBtn = CreateButton(completionGO.transform, "ReplayButton", "PLAY AGAIN",
                new Color(0.2f, 0.6f, 0.3f));
            SetAnchored(replayBtn, new Vector2(0.35f, 0.15f), new Vector2(0.65f, 0.28f));

            // === FEEDBACK FLASH (overlay for correct/incorrect) ===
            var flashGO = CreatePanel(canvasGO.transform, "FeedbackFlash", new Color(0, 0, 0, 0));
            SetFullStretch(flashGO);
            flashGO.GetComponent<Image>().raycastTarget = false;
            flashGO.SetActive(false);

            // === PIECE VISUALIZER ===
            var vizGO = new GameObject("PieceVisualizer");
            vizGO.transform.SetParent(canvasGO.transform, false);
            var viz = vizGO.AddComponent<UI.PieceVisualizer>();

            // === GRID CELL PREFAB ===
            var cellPrefab = new GameObject("GridCellPrefab");
            var cellRT2 = cellPrefab.AddComponent<RectTransform>();
            var cellImage = cellPrefab.AddComponent<Image>();
            cellImage.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            cellPrefab.SetActive(false);
            cellPrefab.transform.SetParent(canvasGO.transform, false);

            // === WIRE UP UI MANAGER ===
            var uiManagerGO = gameManagerGO;
            var uiManager = uiManagerGO.AddComponent<UI.UIManager>();

            // Use SerializedObject to assign references
            var so = new SerializedObject(uiManager);
            SetRef(so, "mainCanvas", canvas);
            SetRef(so, "menuPanel", menuGO);
            SetRef(so, "startButton", startBtn.GetComponent<Button>());
            SetRef(so, "titleText", titleText.GetComponent<TextMeshProUGUI>());
            SetRef(so, "player1Panel", p1RT);
            SetRef(so, "p1InstructionText", p1Instruction.GetComponent<TextMeshProUGUI>());
            SetRef(so, "p1FeedbackText", p1Feedback.GetComponent<TextMeshProUGUI>());
            SetRef(so, "p1ScoreText", p1Score.GetComponent<TextMeshProUGUI>());
            SetRef(so, "p1HintButton", p1HintBtn.GetComponent<Button>());
            SetRef(so, "player2Panel", p2RT);
            SetRef(so, "p2InstructionText", p2Instruction.GetComponent<TextMeshProUGUI>());
            SetRef(so, "p2FeedbackText", p2Feedback.GetComponent<TextMeshProUGUI>());
            SetRef(so, "p2ScoreText", p2Score.GetComponent<TextMeshProUGUI>());
            SetRef(so, "p2HintButton", p2HintBtn.GetComponent<Button>());
            SetRef(so, "sharedArea", centerRT);
            SetRef(so, "problemText", problemGO.GetComponent<TextMeshProUGUI>());
            SetRef(so, "scaffoldLabel", scaffoldGO.GetComponent<TextMeshProUGUI>());
            SetRef(so, "progressBar", progressImage);
            SetRef(so, "completionPanel", completionGO);
            SetRef(so, "completionText", completionText.GetComponent<TextMeshProUGUI>());
            SetRef(so, "replayButton", replayBtn.GetComponent<Button>());
            SetRef(so, "feedbackFlash", flashGO.GetComponent<Image>());
            SetRef(so, "gridContainer", gridRT);
            SetRef(so, "gridCellPrefab", cellPrefab);
            so.ApplyModifiedProperties();

            // Wire up PieceVisualizer
            var vizSO = new SerializedObject(viz);
            SetRef(vizSO, "worldCanvas", canvas);
            vizSO.ApplyModifiedProperties();

            // Set Game view to 1920x1080
            Debug.Log("[SceneBuilder] Game scene built successfully! Hit Play to test.");
            Debug.Log("[SceneBuilder] Open Board > Input > Simulator, enable simulation, then click a piece icon and click in the Game view to place it.");

            EditorUtility.SetDirty(gameManagerGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        // === HELPER METHODS ===

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGO = CreateText(go.transform, "Text", label,
                24, TextAlignmentOptions.Center, Color.white);
            SetFullStretch(textGO);

            return go;
        }

        private static void SetFullStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }
    }
}
#endif
