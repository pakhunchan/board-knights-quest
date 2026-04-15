#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardOfEducation.Demo
{
    /// <summary>
    /// Editor utility that builds the PiecePlayground demo scene from scratch.
    /// Menu: Board of Education > Build Demo Scene
    /// </summary>
    public static class DemoSceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            // Confirm if the current scene has unsaved changes
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Create a fresh scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // ── Main Camera ──
            var cameraGo = new GameObject("Main Camera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = HexColor("#1a1a2e");
            cameraGo.tag = "MainCamera";

            // ── EventSystem ──
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Use the new Input System UI module if available, fall back to standalone
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

            // ── DemoCanvas ──
            var canvasGo = new GameObject("DemoCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Background (full-screen dark gradient feel via solid color)
            var bgGo = CreateUIElement("Background", canvasGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = HexColor("#0f0e2a");

            // Background circle container (sits behind cards, fills play area)
            var bgCirclesGo = CreateUIElement("BgCircleContainer", canvasGo.transform);
            var bgCirclesRect = bgCirclesGo.GetComponent<RectTransform>();
            bgCirclesRect.anchorMin = Vector2.zero;
            bgCirclesRect.anchorMax = Vector2.one;
            bgCirclesRect.offsetMin = Vector2.zero;
            bgCirclesRect.offsetMax = Vector2.zero;

            // Title text
            var titleGo = CreateUIElement("TitleText", canvasGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(800, 60);
            var titleText = titleGo.AddComponent<Text>();
            titleText.text = "Piece Playground";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 48;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Instruction text
            var instrGo = CreateUIElement("InstructionText", canvasGo.transform);
            var instrRect = instrGo.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0.5f, 1f);
            instrRect.anchorMax = new Vector2(0.5f, 1f);
            instrRect.pivot = new Vector2(0.5f, 1f);
            instrRect.anchoredPosition = new Vector2(0, -100);
            instrRect.sizeDelta = new Vector2(800, 40);
            var instrText = instrGo.AddComponent<Text>();
            instrText.text = "Place pieces on the board!";
            instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instrText.fontSize = 28;
            instrText.color = new Color(1f, 1f, 1f, 0.7f);
            instrText.alignment = TextAnchor.MiddleCenter;

            // Piece count text (top right)
            var countGo = CreateUIElement("PieceCountText", canvasGo.transform);
            var countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1f, 1f);
            countRect.anchorMax = new Vector2(1f, 1f);
            countRect.pivot = new Vector2(1f, 1f);
            countRect.anchoredPosition = new Vector2(-30, -30);
            countRect.sizeDelta = new Vector2(300, 40);
            var countText = countGo.AddComponent<Text>();
            countText.text = "0 pieces on board";
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 24;
            countText.color = new Color(1f, 1f, 1f, 0.6f);
            countText.alignment = TextAnchor.MiddleRight;

            // Card container (center area with horizontal layout)
            var containerGo = CreateUIElement("CardContainer", canvasGo.transform);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.1f);
            containerRect.anchorMax = new Vector2(0.9f, 0.75f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            var hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // PiecePlayground component on the canvas
            var playground = canvasGo.AddComponent<PiecePlayground>();

            // Wire up serialized references via SerializedObject
            var so = new SerializedObject(playground);
            so.FindProperty("instructionText").objectReferenceValue = instrText;
            so.FindProperty("pieceCountText").objectReferenceValue = countText;
            so.FindProperty("cardContainer").objectReferenceValue = containerRect;
            so.FindProperty("bgCircleContainer").objectReferenceValue = bgCirclesRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save the scene
            string scenePath = "Assets/Scenes/PiecePlayground.unity";
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[DemoSceneBuilder] Demo scene built and saved to {scenePath}");
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

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif
