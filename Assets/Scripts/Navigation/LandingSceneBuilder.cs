#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Navigation
{
    /// <summary>
    /// Editor utility that builds the LandingPage scene programmatically.
    /// Menu: Board of Education > Build Landing Scene
    /// </summary>
    public static class LandingSceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build Landing Scene")]
        public static void BuildLandingScene()
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
            var landingManager = gameCoreGo.AddComponent<LandingPageManager>();

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

            // ── Title ──
            var titleGo = CreateText(canvasGo.transform, "Title", "Knight's Quest,\nMath Adventures",
                56, TextAlignmentOptions.Center, Color.white);
            SetAnchored(titleGo, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.98f));
            titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // ── Subtitle ──
            var subtitleGo = CreateText(canvasGo.transform, "Subtitle",
                "Place a piece or tap to choose",
                28, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.7f));
            SetAnchored(subtitleGo, new Vector2(0.15f, 0.72f), new Vector2(0.85f, 0.82f));

            // ══════════════════════════════════════════════════
            // CIRCLES CARD
            // ══════════════════════════════════════════════════
            var circlesCardGo = CreateGameCard(canvasGo.transform, "CirclesCard",
                "CIRCLES", "Combine to zero!",
                HexColor("#e74c3c"), // red theme
                new Vector2(0.02f, 0.1f), new Vector2(0.135f, 0.68f));

            // ══════════════════════════════════════════════════
            // FRACTIONS CARD
            // ══════════════════════════════════════════════════
            var fractionsCardGo = CreateGameCard(canvasGo.transform, "FractionsCard",
                "FRACTIONS", "Visual equivalence",
                HexColor("#3498db"), // blue theme
                new Vector2(0.145f, 0.1f), new Vector2(0.255f, 0.68f));

            // ══════════════════════════════════════════════════
            // INTERACTIVE FRACTIONS CARD
            // ══════════════════════════════════════════════════
            var fractions3CardGo = CreateGameCard(canvasGo.transform, "Fractions3Card",
                "INTERACTIVE", "Answer with pieces",
                HexColor("#9b59b6"), // purple theme
                new Vector2(0.265f, 0.1f), new Vector2(0.375f, 0.68f));

            // ══════════════════════════════════════════════════
            // FRACTIONS 4 CARD
            // ══════════════════════════════════════════════════
            var fractions4CardGo = CreateGameCard(canvasGo.transform, "Fractions4Card",
                "FRACTIONS 4", "3/5 = ?/20",
                HexColor("#e67e22"), // orange theme
                new Vector2(0.385f, 0.1f), new Vector2(0.495f, 0.68f));

            // ══════════════════════════════════════════════════
            // PRACTICE CARD
            // ══════════════════════════════════════════════════
            var fractions5CardGo = CreateGameCard(canvasGo.transform, "Fractions5Card",
                "PRACTICE", "10 problems",
                HexColor("#1abc9c"), // teal theme
                new Vector2(0.505f, 0.1f), new Vector2(0.615f, 0.68f));

            // ══════════════════════════════════════════════════
            // FULL LESSON CARD
            // ══════════════════════════════════════════════════
            var totalFractions2CardGo = CreateGameCard(canvasGo.transform, "TotalFractions2Card",
                "FULL LESSON", "Complete lesson",
                HexColor("#f39c12"), // gold/amber theme
                new Vector2(0.625f, 0.1f), new Vector2(0.735f, 0.68f));

            // ══════════════════════════════════════════════════
            // MVP1 CARD
            // ══════════════════════════════════════════════════
            var mvp1CardGo = CreateGameCard(canvasGo.transform, "MVP1Card",
                "MVP1", "Lesson + Practice",
                HexColor("#e91e63"), // pink theme
                new Vector2(0.745f, 0.1f), new Vector2(0.855f, 0.68f));

            // ══════════════════════════════════════════════════
            // PLAYGROUND CARD
            // ══════════════════════════════════════════════════
            var playgroundCardGo = CreateGameCard(canvasGo.transform, "PlaygroundCard",
                "PAK'S\nPLAYGROUND", "Explore & experiment",
                HexColor("#2ecc71"), // green theme
                new Vector2(0.865f, 0.1f), new Vector2(0.98f, 0.68f));

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var managerSO = new SerializedObject(landingManager);
            SetRef(managerSO, "mainCanvas", canvas);
            SetRef(managerSO, "circlesCard", circlesCardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "fractionsCard", fractionsCardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "playgroundCard", playgroundCardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "fractions3Card", fractions3CardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "fractions4Card", fractions4CardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "fractions5Card", fractions5CardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "totalFractions2Card", totalFractions2CardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "mvp1Card", mvp1CardGo.GetComponent<RectTransform>());
            SetRef(managerSO, "circlesButton", circlesCardGo.GetComponent<Button>());
            SetRef(managerSO, "fractionsButton", fractionsCardGo.GetComponent<Button>());
            SetRef(managerSO, "playgroundButton", playgroundCardGo.GetComponent<Button>());
            SetRef(managerSO, "fractions3Button", fractions3CardGo.GetComponent<Button>());
            SetRef(managerSO, "fractions4Button", fractions4CardGo.GetComponent<Button>());
            SetRef(managerSO, "fractions5Button", fractions5CardGo.GetComponent<Button>());
            SetRef(managerSO, "totalFractions2Button", totalFractions2CardGo.GetComponent<Button>());
            SetRef(managerSO, "mvp1Button", mvp1CardGo.GetComponent<Button>());
            SetRef(managerSO, "subtitleText", subtitleGo.GetComponent<TextMeshProUGUI>());
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/LandingPage.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[LandingSceneBuilder] Landing scene built and saved to {scenePath}");
        }

        [MenuItem("Knight's Quest: Math Adventures/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LandingPage.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Circles.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Playground.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/TotalFractionsDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/FractionsDemo3.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/FractionsDemo4.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/FractionsDemo5.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/TotalFractions2.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MVP1.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[LandingSceneBuilder] Build settings configured: LandingPage(0), Circles(1), Playground(2), TotalFractionsDemo(3), FractionsDemo3(4), FractionsDemo4(5), FractionsDemo5(6), TotalFractions2(7), MVP1(8)");
        }

        // ── Game Card Helper ─────────────────────────────────

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

            // Card background
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(themeColor.r * 0.3f, themeColor.g * 0.3f, themeColor.b * 0.3f, 0.9f);

            // Make it a button
            var btn = cardGo.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            // Icon circle (centered upper portion)
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.45f);
            iconRect.anchorMax = new Vector2(0.5f, 0.45f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(120, 120);
            iconRect.anchoredPosition = new Vector2(0, 40);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = themeColor;
            iconImg.raycastTarget = false;
            // Will use procedural circle sprite at runtime

            // Title text
            var titleGo = CreateText(cardGo.transform, "Title", title,
                36, TextAlignmentOptions.Center, Color.white);
            SetAnchored(titleGo, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.4f));
            titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Description text
            var descGo = CreateText(cardGo.transform, "Description", description,
                22, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.7f));
            SetAnchored(descGo, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.18f));

            return cardGo;
        }

        // ── Helpers (same pattern as CirclesSceneBuilder) ────

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

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[LandingSceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif
