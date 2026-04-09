#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BoardOfEducation.Lessons;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Fractions Demo 3 (interactive) scene.
    /// Menu: Board of Education > Build Fractions Demo 3 Scene
    /// </summary>
    public static class FractionsDemo3SceneBuilder
    {
        [MenuItem("Board of Education/Build Fractions Demo 3 Scene")]
        public static void BuildFractionsDemo3Scene()
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

            // ── GameCore ──
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();
            var manager = gameCoreGo.AddComponent<FractionsDemo3Manager>();
            var sequencer = gameCoreGo.AddComponent<LessonSequencer>();

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
            var titleGo = CreateText(canvasGo.transform, "Title", "FRACTIONS",
                48, TextAlignmentOptions.Center, Color.white);
            SetAnchored(titleGo, new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.95f));

            // ── ContentArea ──
            var contentAreaGo = CreateUIElement("ContentArea", canvasGo.transform);
            var contentAreaRect = contentAreaGo.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0.05f, 0.18f);
            contentAreaRect.anchorMax = new Vector2(0.95f, 0.82f);
            contentAreaRect.offsetMin = Vector2.zero;
            contentAreaRect.offsetMax = Vector2.zero;

            // ── SubtitleText ──
            var subtitleGo = CreateText(canvasGo.transform, "SubtitleText", "",
                36, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.9f));
            SetAnchored(subtitleGo, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.14f));

            // ── PlayButton ──
            var playBtnGo = CreateButton(canvasGo.transform, "PlayButton",
                "\u25b6 PLAY", HexColor("#2ecc71"), HexColor("#555555"));
            SetAnchored(playBtnGo, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.33f));

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var sequencerSO = new SerializedObject(sequencer);
            SetRef(sequencerSO, "subtitleText", subtitleGo.GetComponent<TextMeshProUGUI>());
            sequencerSO.ApplyModifiedPropertiesWithoutUndo();

            var managerSO = new SerializedObject(manager);
            SetRef(managerSO, "contentArea", contentAreaRect);
            SetRef(managerSO, "playButton", playBtnGo.GetComponent<Button>());
            SetRef(managerSO, "playButtonGo", playBtnGo);
            SetRef(managerSO, "sequencer", sequencer);
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/FractionsDemo3.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[FractionsDemo3SceneBuilder] Fractions Demo 3 scene built and saved to {scenePath}");
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
            tmp.enableWordWrapping = true;
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
                Debug.LogWarning($"[FractionsDemo3SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif
