#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BoardOfEducation.Lessons;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Fractions Demo 5 (practice session) scene.
    /// Menu: Board of Education > Build Fractions Demo 5 Scene
    /// </summary>
    public static class FractionsDemo5SceneBuilder
    {
        private static readonly string[] LayerPaths = new[]
        {
            "Assets/Textures/variants/chalkboard-layers/layer-0-background.png",
            "Assets/Textures/variants/chalkboard-layers/layer-1-frame.png",
            "Assets/Textures/variants/chalkboard-layers/layer-2-board.png",
            "Assets/Textures/variants/chalkboard-layers/layer-3-tray.png",
            "Assets/Textures/variants/chalkboard-layers/layer-4-border.png",
        };

        [MenuItem("Board of Education/Build Fractions Demo 5 Scene")]
        public static void BuildFractionsDemo5Scene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure all textures are imported as Sprite ──
            foreach (var path in LayerPaths)
                EnsureSpriteImport(path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

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

            // ══════════════════════════════════════════════════
            // CHALKBOARD LAYERS (bottom to top)
            // ══════════════════════════════════════════════════

            // ── Background (layer-0, always visible at full alpha) ──
            var bgGo = CreateUIElement("Background", canvasGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite(LayerPaths[0]);
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            // ── Overlay layers 1-4 (fully visible from the start) ──
            string[] layerNames = { "Frame", "Board", "Tray", "Border" };

            for (int i = 0; i < 4; i++)
            {
                var go = CreateUIElement(layerNames[i], canvasGo.transform);
                StretchFill(go);
                var img = go.AddComponent<Image>();
                img.sprite = LoadSprite(LayerPaths[i + 1]);
                img.preserveAspect = false;
                img.raycastTarget = false;
            }

            // ══════════════════════════════════════════════════
            // LESSON UI WRAPPER (fades in after chalkboard)
            // ══════════════════════════════════════════════════
            var lessonUIGo = CreateUIElement("LessonUI", canvasGo.transform);
            StretchFill(lessonUIGo);
            var contentGroup = lessonUIGo.AddComponent<CanvasGroup>();
            contentGroup.alpha = 1f;

            // ── ContentArea ──
            var contentAreaGo = CreateUIElement("ContentArea", lessonUIGo.transform);
            var contentAreaRect = contentAreaGo.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0.05f, 0.18f);
            contentAreaRect.anchorMax = new Vector2(0.95f, 0.82f);
            contentAreaRect.offsetMin = Vector2.zero;
            contentAreaRect.offsetMax = Vector2.zero;

            // ── SubtitleText ──
            var subtitleGo = CreateText(lessonUIGo.transform, "SubtitleText", "",
                36, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.9f));
            SetAnchored(subtitleGo, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.14f));

            // ── ScoreArea (top center, above title) ──
            var scoreAreaGo = CreateUIElement("ScoreArea", lessonUIGo.transform);
            var scoreAreaRect = scoreAreaGo.GetComponent<RectTransform>();
            scoreAreaRect.anchorMin = new Vector2(0.25f, 0.92f);
            scoreAreaRect.anchorMax = new Vector2(0.75f, 0.98f);
            scoreAreaRect.offsetMin = Vector2.zero;
            scoreAreaRect.offsetMax = Vector2.zero;

            // ── PlayButton (hidden at start) ──
            var playBtnGo = CreateButton(lessonUIGo.transform, "PlayButton",
                "\u25b6 PLAY", HexColor("#2ecc71"), HexColor("#555555"));
            SetAnchored(playBtnGo, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.33f));
            playBtnGo.SetActive(false);

            // ══════════════════════════════════════════════════
            // GAMECORE — all components on one object
            // ══════════════════════════════════════════════════
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();

            var manager = gameCoreGo.AddComponent<FractionsDemo5Manager>();
            var sequencer = gameCoreGo.AddComponent<LessonSequencer>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════

            // LessonSequencer
            var sequencerSO = new SerializedObject(sequencer);
            SetRef(sequencerSO, "subtitleText", subtitleGo.GetComponent<TextMeshProUGUI>());
            sequencerSO.ApplyModifiedPropertiesWithoutUndo();

            // FractionsDemo5Manager
            var managerSO = new SerializedObject(manager);
            SetRef(managerSO, "contentArea", contentAreaRect);
            SetRef(managerSO, "scoreArea", scoreAreaRect);
            SetRef(managerSO, "playButton", playBtnGo.GetComponent<Button>());
            SetRef(managerSO, "playButtonGo", playBtnGo);
            SetRef(managerSO, "sequencer", sequencer);
            SetRef(managerSO, "contentGroup", contentGroup);
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/FractionsDemo5.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[FractionsDemo5SceneBuilder] Fractions Demo 5 scene built and saved to {scenePath}");
        }

        // ── Helpers ──────────────────────────────────────────

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
                Debug.LogWarning($"[FractionsDemo5SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        private static void EnsureSpriteImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[FractionsDemo5SceneBuilder] No texture importer found for {path}");
                return;
            }
            bool needsReimport = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                needsReimport = true;
            }
            if (needsReimport)
            {
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
                Debug.Log($"[FractionsDemo5SceneBuilder] Reimported {path} as Single Sprite");
            }
        }
    }
}
#endif
