#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Circles game scene from scratch.
    /// Menu: Board of Education > Build Circles Scene
    /// </summary>
    public static class CirclesSceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build Circles Scene")]
        public static void BuildCirclesScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // ── Main Camera ──
            var cameraGo = new GameObject("Main Camera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HexColor("#0f0e2a");
            cameraGo.tag = "MainCamera";

            // ── EventSystem ──
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                eventSystemGo.AddComponent(inputModuleType);
            else
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            var boardInputModule = eventSystemGo.AddComponent<Board.Input.BoardUIInputModule>();
            var boardInputSO = new SerializedObject(boardInputModule);
            var maskProp = boardInputSO.FindProperty("m_InputMask.m_Bits");
            if (maskProp != null) { maskProp.longValue = 3; boardInputSO.ApplyModifiedPropertiesWithoutUndo(); }

            // ── GameCore ──
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();
            var gameManager = gameCoreGo.AddComponent<CirclesGameManager>();
            var board = gameCoreGo.AddComponent<CirclesBoard>();
            var ui = gameCoreGo.AddComponent<CirclesUI>();

            // ── MainCanvas ──
            var canvasGo = new GameObject("MainCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Background ──
            var bgGo = CreateUIElement("Background", canvasGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = HexColor("#0f0e2a");
            bgImg.raycastTarget = false;

            // ══════════════════════════════════════════════════
            // PIECE SELECT SCREEN
            // ══════════════════════════════════════════════════
            var pieceSelectGo = CreateUIElement("PieceSelectScreen", canvasGo.transform);
            StretchFill(pieceSelectGo);

            // Back button (top-left, returns to landing page)
            var backBtnGo = CreateButton(pieceSelectGo.transform, "BackButton",
                "< BACK", HexColor("#e74c3c"), HexColor("#555555"));
            SetAnchored(backBtnGo, new Vector2(0.02f, 0.88f), new Vector2(0.15f, 0.97f));

            // Title: "CIRCLES"
            var psTitle = CreateText(pieceSelectGo.transform, "Title", "CIRCLES",
                64, TextAlignmentOptions.Center, Color.white);
            SetAnchored(psTitle, new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.95f));

            // Subtitle
            var psSubtitle = CreateText(pieceSelectGo.transform, "Subtitle",
                "Place a piece on the board, then tap Confirm!",
                28, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.7f));
            SetAnchored(psSubtitle, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.75f));

            // Piece circle container (centered row)
            var pieceCircleContainer = CreateUIElement("PieceCircles", pieceSelectGo.transform);
            var pcRect = pieceCircleContainer.GetComponent<RectTransform>();
            pcRect.anchorMin = new Vector2(0.05f, 0.35f);
            pcRect.anchorMax = new Vector2(0.95f, 0.6f);
            pcRect.offsetMin = Vector2.zero;
            pcRect.offsetMax = Vector2.zero;

            // Piece status text ("Robot Yellow detected!")
            var psStatus = CreateText(pieceSelectGo.transform, "PieceStatus", "",
                24, TextAlignmentOptions.Center, new Color(0.5f, 1f, 0.5f));
            SetAnchored(psStatus, new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.35f));

            // Confirm button (greyed out until piece placed)
            var confirmBtnGo = CreateButton(pieceSelectGo.transform, "ConfirmButton",
                "CONFIRM", HexColor("#2ecc71"), new Color(0.3f, 0.3f, 0.3f));
            SetAnchored(confirmBtnGo, new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.22f));

            // ══════════════════════════════════════════════════
            // LEVEL SELECT SCREEN
            // ══════════════════════════════════════════════════
            var levelSelectGo = CreateUIElement("LevelSelectScreen", canvasGo.transform);
            StretchFill(levelSelectGo);
            levelSelectGo.SetActive(false);

            // Title
            var lsTitle = CreateText(levelSelectGo.transform, "Title", "SELECT LEVEL",
                48, TextAlignmentOptions.Center, Color.white);
            SetAnchored(lsTitle, new Vector2(0.1f, 0.8f), new Vector2(0.9f, 0.95f));

            // Level circle container
            var levelCircleContainer = CreateUIElement("LevelCircles", levelSelectGo.transform);
            var lcRect = levelCircleContainer.GetComponent<RectTransform>();
            lcRect.anchorMin = new Vector2(0.05f, 0.1f);
            lcRect.anchorMax = new Vector2(0.95f, 0.75f);
            lcRect.offsetMin = Vector2.zero;
            lcRect.offsetMax = Vector2.zero;

            // ══════════════════════════════════════════════════
            // GAMEPLAY SCREEN
            // ══════════════════════════════════════════════════
            var gameplayGo = CreateUIElement("GameplayScreen", canvasGo.transform);
            StretchFill(gameplayGo);
            gameplayGo.SetActive(false);

            // Level title (top left)
            var gpTitle = CreateText(gameplayGo.transform, "LevelTitle", "Level 1: Opposites",
                32, TextAlignmentOptions.TopLeft, Color.white);
            SetAnchored(gpTitle, new Vector2(0.03f, 0.9f), new Vector2(0.5f, 0.98f));

            // Move count (top right)
            var gpMoves = CreateText(gameplayGo.transform, "MoveCount", "Moves: 0",
                28, TextAlignmentOptions.TopRight, new Color(1, 1, 1, 0.8f));
            SetAnchored(gpMoves, new Vector2(0.5f, 0.9f), new Vector2(0.97f, 0.98f));

            // Instruction (center-top)
            var gpInstruction = CreateText(gameplayGo.transform, "Instruction",
                "Combine circles to reach zero!",
                24, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.6f));
            SetAnchored(gpInstruction, new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.9f));

            // Circle container (center 80%)
            var circleContainer = CreateUIElement("CircleContainer", gameplayGo.transform);
            var ccRect = circleContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.1f, 0.1f);
            ccRect.anchorMax = new Vector2(0.9f, 0.8f);
            ccRect.offsetMin = Vector2.zero;
            ccRect.offsetMax = Vector2.zero;

            // ══════════════════════════════════════════════════
            // LEVEL COMPLETE OVERLAY
            // ══════════════════════════════════════════════════
            var completeGo = CreateUIElement("LevelCompleteOverlay", canvasGo.transform);
            StretchFill(completeGo);
            var completeBg = completeGo.AddComponent<Image>();
            completeBg.color = new Color(0.05f, 0.04f, 0.15f, 0.9f);
            completeBg.raycastTarget = false;
            completeGo.SetActive(false);

            // "Level Complete!" title
            var compTitle = CreateText(completeGo.transform, "CompleteTitle", "Level Complete!",
                56, TextAlignmentOptions.Center, Color.white);
            SetAnchored(compTitle, new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.85f));

            // Stars
            var compStars = CreateText(completeGo.transform, "Stars", "\u2605\u2605\u2605",
                64, TextAlignmentOptions.Center, HexColor("#F1C40F"));
            SetAnchored(compStars, new Vector2(0.3f, 0.5f), new Vector2(0.7f, 0.65f));

            // Moves text
            var compMoves = CreateText(completeGo.transform, "MovesText", "Moves: 1 (par: 1)",
                28, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.8f));
            SetAnchored(compMoves, new Vector2(0.2f, 0.38f), new Vector2(0.8f, 0.5f));

            // Continue text
            var compContinue = CreateText(completeGo.transform, "ContinueText",
                "Tap Continue to proceed",
                24, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.5f));
            SetAnchored(compContinue, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.38f));

            // Continue button
            var continueBtnGo = CreateButton(completeGo.transform, "ContinueButton",
                "CONTINUE", HexColor("#3498db"), HexColor("#3498db"));
            SetAnchored(continueBtnGo, new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.25f));

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════

            // CirclesBoard
            var boardSO = new SerializedObject(board);
            SetRef(boardSO, "circleContainer", ccRect);
            SetRef(boardSO, "mainCanvas", canvas);
            boardSO.ApplyModifiedPropertiesWithoutUndo();

            // CirclesUI
            var uiSO = new SerializedObject(ui);
            SetRef(uiSO, "pieceSelectScreen", pieceSelectGo);
            SetRef(uiSO, "levelSelectScreen", levelSelectGo);
            SetRef(uiSO, "gameplayScreen", gameplayGo);
            SetRef(uiSO, "levelCompleteOverlay", completeGo);
            SetRef(uiSO, "backButton", backBtnGo.GetComponent<Button>());
            SetRef(uiSO, "pieceSelectTitle", psTitle.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "pieceSelectSubtitle", psSubtitle.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "pieceCircleContainer", pcRect);
            SetRef(uiSO, "confirmButton", confirmBtnGo.GetComponent<Button>());
            SetRef(uiSO, "pieceStatusText", psStatus.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "levelSelectTitle", lsTitle.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "levelCircleContainer", lcRect);
            SetRef(uiSO, "levelTitleText", gpTitle.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "moveCountText", gpMoves.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "instructionText", gpInstruction.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "completeTitle", compTitle.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "starsText", compStars.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "completeMoveText", compMoves.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "continueText", compContinue.GetComponent<TextMeshProUGUI>());
            SetRef(uiSO, "continueButton", continueBtnGo.GetComponent<Button>());
            uiSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Circles.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[CirclesSceneBuilder] Circles scene built and saved to {scenePath}");
        }

        // ── Helpers ──────────────────────────────────────────

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFill(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateText(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return go;
        }

        /// <summary>
        /// Creates a button with a background color and disabled color.
        /// Uses Image as targetGraphic. Text child sized 36.
        /// </summary>
        private static GameObject CreateButton(Transform parent, string name,
            string label, Color activeColor, Color disabledColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = activeColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button color block with disabled state
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            // Text label
            var textGo = CreateText(go.transform, "Text", label,
                36, TextAlignmentOptions.Center, Color.white);
            StretchFill(textGo);
            textGo.GetComponent<TextMeshProUGUI>().raycastTarget = false;

            return go;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[CirclesSceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif
