#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Playground
{
    /// <summary>
    /// Editor utility that builds the Playground scene programmatically.
    /// Menu: Board of Education > Build Playground Scene
    /// </summary>
    public static class PlaygroundSceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build Playground Scene")]
        public static void BuildPlaygroundScene()
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
            var playgroundManager = gameCoreGo.AddComponent<PlaygroundManager>();
            var features1 = gameCoreGo.AddComponent<Features1Controller>();

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
            // HUB SCREEN
            // ══════════════════════════════════════════════════
            var hubGo = CreateUIElement("HubScreen", canvasGo.transform);
            StretchFill(hubGo);

            // Title
            var hubTitle = CreateText(hubGo.transform, "Title", "PAK'S PLAYGROUND",
                56, TextAlignmentOptions.Center, Color.white);
            SetAnchored(hubTitle, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.98f));
            hubTitle.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Back button (top-left)
            var hubBackBtn = CreateButton(hubGo.transform, "BackButton",
                "< BACK", HexColor("#e74c3c"), HexColor("#555555"));
            SetAnchored(hubBackBtn, new Vector2(0.02f, 0.9f), new Vector2(0.15f, 0.98f));

            // Features1 card
            var features1Card = CreateGameCard(hubGo.transform, "Features1Card",
                "FEATURES 1", "Board SDK demos:\nTracker, Spinner,\nPainter, Multi-piece",
                HexColor("#3498db"),
                new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.75f));

            // ══════════════════════════════════════════════════
            // FEATURES1 SCREEN
            // ══════════════════════════════════════════════════
            var f1Go = CreateUIElement("Features1Screen", canvasGo.transform);
            StretchFill(f1Go);
            f1Go.SetActive(false);

            // Back button (to Hub)
            var f1BackBtn = CreateButton(f1Go.transform, "BackButton",
                "< BACK", HexColor("#e74c3c"), HexColor("#555555"));
            SetAnchored(f1BackBtn, new Vector2(0.02f, 0.9f), new Vector2(0.15f, 0.98f));

            // Title
            var f1Title = CreateText(f1Go.transform, "Title", "FEATURES 1",
                36, TextAlignmentOptions.Center, Color.white);
            SetAnchored(f1Title, new Vector2(0.15f, 0.9f), new Vector2(0.85f, 0.98f));

            // Tab buttons
            var trackerTab = CreateTabButton(f1Go.transform, "TrackerTab", "Tracker",
                new Vector2(0.02f, 0.82f), new Vector2(0.26f, 0.9f));
            var spinnerTab = CreateTabButton(f1Go.transform, "SpinnerTab", "Spinner",
                new Vector2(0.27f, 0.82f), new Vector2(0.51f, 0.9f));
            var painterTab = CreateTabButton(f1Go.transform, "PainterTab", "Painter",
                new Vector2(0.52f, 0.82f), new Vector2(0.76f, 0.9f));
            var multiTab = CreateTabButton(f1Go.transform, "MultiPieceTab", "Multi-piece",
                new Vector2(0.77f, 0.82f), new Vector2(1.0f, 0.9f));

            // ── Tracker Panel ──
            var trackerPanel = CreateUIElement("TrackerPanel", f1Go.transform);
            SetAnchored(trackerPanel, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.8f));

            var trackerInfo = CreateText(trackerPanel.transform, "TrackerInfo",
                "No piece detected.\nPlace a piece on the board!",
                24, TextAlignmentOptions.MidlineLeft, Color.white);
            SetAnchored(trackerInfo, new Vector2(0.02f, 0.1f), new Vector2(0.6f, 0.9f));
            trackerInfo.GetComponent<TextMeshProUGUI>().richText = true;

            // Tracker circle indicator
            var trackerCircleGo = new GameObject("TrackerCircle");
            trackerCircleGo.transform.SetParent(trackerPanel.transform, false);
            var tcRect = trackerCircleGo.AddComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.7f, 0.3f);
            tcRect.anchorMax = new Vector2(0.7f, 0.3f);
            tcRect.pivot = new Vector2(0.5f, 0.5f);
            tcRect.sizeDelta = new Vector2(150, 150);
            tcRect.anchoredPosition = Vector2.zero;
            var tcImg = trackerCircleGo.AddComponent<Image>();
            tcImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // ── Spinner Panel ──
            var spinnerPanel = CreateUIElement("SpinnerPanel", f1Go.transform);
            SetAnchored(spinnerPanel, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.8f));
            spinnerPanel.SetActive(false);

            // Spinner circle background
            var spinnerBg = new GameObject("SpinnerBg");
            spinnerBg.transform.SetParent(spinnerPanel.transform, false);
            var sbRect = spinnerBg.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.5f, 0.55f);
            sbRect.anchorMax = new Vector2(0.5f, 0.55f);
            sbRect.pivot = new Vector2(0.5f, 0.5f);
            sbRect.sizeDelta = new Vector2(200, 200);
            var sbImg = spinnerBg.AddComponent<Image>();
            sbImg.color = new Color(0.2f, 0.2f, 0.35f, 1f);

            // Spinner arrow (child of background)
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(spinnerBg.transform, false);
            var arrowRect = arrowGo.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0f);
            arrowRect.sizeDelta = new Vector2(8, 90);
            arrowRect.anchoredPosition = Vector2.zero;
            var arrowImg = arrowGo.AddComponent<Image>();
            arrowImg.color = HexColor("#e74c3c");

            // Arrow tip (triangle-ish rectangle at top)
            var tipGo = new GameObject("Tip");
            tipGo.transform.SetParent(arrowGo.transform, false);
            var tipRect = tipGo.AddComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 1f);
            tipRect.anchorMax = new Vector2(0.5f, 1f);
            tipRect.pivot = new Vector2(0.5f, 0f);
            tipRect.sizeDelta = new Vector2(20, 20);
            tipRect.anchoredPosition = Vector2.zero;
            var tipImg = tipGo.AddComponent<Image>();
            tipImg.color = HexColor("#e74c3c");

            // Spinner degree text
            var spinnerDegree = CreateText(spinnerPanel.transform, "DegreeText", "--°",
                32, TextAlignmentOptions.Center, Color.white);
            SetAnchored(spinnerDegree, new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.2f));

            // Spinner hint text
            var spinnerHint = CreateText(spinnerPanel.transform, "HintText",
                "Place a piece and rotate it!",
                22, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.6f));
            SetAnchored(spinnerHint, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.3f));

            // ── Painter Panel ──
            var painterPanel = CreateUIElement("PainterPanel", f1Go.transform);
            SetAnchored(painterPanel, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.8f));
            painterPanel.SetActive(false);

            // Painter canvas area (for dots)
            var painterContainer = CreateUIElement("PainterContainer", painterPanel.transform);
            StretchFill(painterContainer);

            // Painter hint
            var painterHint = CreateText(painterPanel.transform, "HintText",
                "Move a piece to paint with color!",
                22, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.6f));
            SetAnchored(painterHint, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.1f));

            // Clear button
            var clearBtn = CreateButton(painterPanel.transform, "ClearButton",
                "CLEAR", HexColor("#e74c3c"), HexColor("#555555"));
            SetAnchored(clearBtn, new Vector2(0.8f, 0.02f), new Vector2(0.98f, 0.1f));

            // ── Multi-piece Panel ──
            var multiPanel = CreateUIElement("MultiPiecePanel", f1Go.transform);
            SetAnchored(multiPanel, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.8f));
            multiPanel.SetActive(false);

            // Multi-piece container (for labels)
            var multiContainer = CreateUIElement("MultiContainer", multiPanel.transform);
            StretchFill(multiContainer);

            // Count text
            var multiCount = CreateText(multiPanel.transform, "CountText",
                "0 pieces detected",
                28, TextAlignmentOptions.Center, Color.white);
            SetAnchored(multiCount, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.12f));

            // Multi-piece hint
            var multiHint = CreateText(multiPanel.transform, "HintText",
                "Place multiple pieces to track them all!",
                22, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.6f));
            SetAnchored(multiHint, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.2f));

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════

            // PlaygroundManager
            var pmSO = new SerializedObject(playgroundManager);
            SetRef(pmSO, "hubScreen", hubGo);
            SetRef(pmSO, "features1Screen", f1Go);
            SetRef(pmSO, "backToLandingButton", hubBackBtn.GetComponent<Button>());
            SetRef(pmSO, "features1CardButton", features1Card.GetComponent<Button>());
            SetRef(pmSO, "backToHubButton", f1BackBtn.GetComponent<Button>());
            pmSO.ApplyModifiedPropertiesWithoutUndo();

            // Features1Controller
            var f1SO = new SerializedObject(features1);
            SetRef(f1SO, "trackerTab", trackerTab.GetComponent<Button>());
            SetRef(f1SO, "spinnerTab", spinnerTab.GetComponent<Button>());
            SetRef(f1SO, "painterTab", painterTab.GetComponent<Button>());
            SetRef(f1SO, "multiPieceTab", multiTab.GetComponent<Button>());
            SetRef(f1SO, "trackerPanel", trackerPanel);
            SetRef(f1SO, "spinnerPanel", spinnerPanel);
            SetRef(f1SO, "painterPanel", painterPanel);
            SetRef(f1SO, "multiPiecePanel", multiPanel);
            SetRef(f1SO, "trackerInfoText", trackerInfo.GetComponent<TextMeshProUGUI>());
            SetRef(f1SO, "trackerCircle", tcImg);
            SetRef(f1SO, "spinnerArrow", arrowRect);
            SetRef(f1SO, "spinnerDegreeText", spinnerDegree.GetComponent<TextMeshProUGUI>());
            SetRef(f1SO, "spinnerHintText", spinnerHint.GetComponent<TextMeshProUGUI>());
            SetRef(f1SO, "painterContainer", painterContainer.GetComponent<RectTransform>());
            SetRef(f1SO, "clearButton", clearBtn.GetComponent<Button>());
            SetRef(f1SO, "mainCanvas", canvas);
            SetRef(f1SO, "multiPieceContainer", multiContainer.GetComponent<RectTransform>());
            SetRef(f1SO, "multiPieceCountText", multiCount.GetComponent<TextMeshProUGUI>());
            f1SO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Playground.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[PlaygroundSceneBuilder] Playground scene built and saved to {scenePath}");
        }

        // ── Helpers ──────────────────────────────────────────

        private static GameObject CreateGameCard(Transform parent, string name,
            string title, string description, Color themeColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var cardGo = new GameObject(name);
            cardGo.transform.SetParent(parent, false);
            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.anchorMin = anchorMin;
            cardRect.anchorMax = anchorMax;
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(themeColor.r * 0.3f, themeColor.g * 0.3f, themeColor.b * 0.3f, 0.9f);

            var btn = cardGo.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            // Icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.55f);
            iconRect.anchorMax = new Vector2(0.5f, 0.55f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(100, 100);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = themeColor;
            iconImg.raycastTarget = false;

            var titleGo = CreateText(cardGo.transform, "Title", title,
                32, TextAlignmentOptions.Center, Color.white);
            SetAnchored(titleGo, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.4f));
            titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var descGo = CreateText(cardGo.transform, "Description", description,
                18, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.7f));
            SetAnchored(descGo, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.22f));

            return cardGo;
        }

        private static GameObject CreateTabButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(2, 0);
            rect.offsetMax = new Vector2(-2, 0);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.25f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = CreateText(go.transform, "Text", label,
                22, TextAlignmentOptions.Center, Color.white);
            StretchFill(textGo);
            textGo.GetComponent<TextMeshProUGUI>().raycastTarget = false;

            return go;
        }

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

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            var textGo = CreateText(go.transform, "Text", label,
                24, TextAlignmentOptions.Center, Color.white);
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
                Debug.LogWarning($"[PlaygroundSceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif
