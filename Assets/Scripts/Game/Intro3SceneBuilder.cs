#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Intro3 scene.
    /// Screen 1: Knight character over landscape background with TTS-style subtitles.
    /// Screen 2: Level map with GO button.
    /// Menu: Board of Education > Build Intro3 Scene
    /// </summary>
    public static class Intro3SceneBuilder
    {
        private const string BgImagePath = "Assets/Textures/landscape_bg_soft.png";
        private const string KnightImagePath = "Assets/Textures/intro/knight.png";
        private const string MapImagePath = "Assets/Textures/intro/level-map.png";

        [MenuItem("Board of Education/Build Intro3 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure all textures are imported as Sprite ──
            string[] spritePaths = new[]
            {
                BgImagePath,
                KnightImagePath,
                MapImagePath,
            };
            foreach (var path in spritePaths)
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

            // Load fonts
            var fredokaFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/Fredoka-VariableFont_wdth,wght SDF.asset");
            if (fredokaFont == null)
                Debug.LogWarning("[Intro3SceneBuilder] Fredoka SDF font not found.");

            // ══════════════════════════════════════════════════
            // SCREEN 1: INTRO (Knight + Subtitles)
            // ══════════════════════════════════════════════════

            var introScreenGo = CreateUIElement("IntroScreen", canvasGo.transform);
            StretchFill(introScreenGo);
            var introGroup = introScreenGo.AddComponent<CanvasGroup>();

            // ── Landscape background ──
            var bgGo = CreateUIElement("Background", introScreenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite(BgImagePath);
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = HexColor("#2a4a2a");
            }

            // ── Knight character sprite ──
            var knightGo = new GameObject("Knight");
            knightGo.transform.SetParent(introScreenGo.transform, false);
            var knightRect = knightGo.AddComponent<RectTransform>();
            // Centered vertically and horizontally
            knightRect.anchorMin = new Vector2(0.5f, 0.5f);
            knightRect.anchorMax = new Vector2(0.5f, 0.5f);
            knightRect.pivot = new Vector2(0.5f, 0.5f);
            knightRect.anchoredPosition = Vector2.zero;
            knightRect.sizeDelta = new Vector2(600, 800);
            var knightImg = knightGo.AddComponent<Image>();
            var knightSprite = LoadSprite(KnightImagePath);
            if (knightSprite != null)
            {
                knightImg.sprite = knightSprite;
                knightImg.preserveAspect = true;
                knightImg.raycastTarget = false;
            }

            // ── Subtitle text (TTS word highlighting) ──
            var subtitleGo = new GameObject("SubtitleText");
            subtitleGo.transform.SetParent(introScreenGo.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.1f, 0.02f);
            subtitleRect.anchorMax = new Vector2(0.9f, 0.15f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            var subtitleTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subtitleTmp.text = "";
            subtitleTmp.fontSize = 28;
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.color = Color.white;
            subtitleTmp.enableWordWrapping = true;
            subtitleTmp.overflowMode = TextOverflowModes.Overflow;
            subtitleTmp.richText = true;
            subtitleTmp.raycastTarget = false;
            if (fredokaFont != null) subtitleTmp.font = fredokaFont;

            // ── Continue button (hidden until narration ends) ──
            var continueBtnGo = CreateButton(introScreenGo.transform, "ContinueButton",
                "CONTINUE >", HexColor("#5A7A4A"));
            SetAnchored(continueBtnGo, new Vector2(0.35f, 0.16f), new Vector2(0.65f, 0.24f));

            // ══════════════════════════════════════════════════
            // SCREEN 2: LEVEL MAP
            // ══════════════════════════════════════════════════

            var mapScreenGo = CreateUIElement("MapScreen", canvasGo.transform);
            StretchFill(mapScreenGo);
            var mapGroup = mapScreenGo.AddComponent<CanvasGroup>();

            // Map background image
            var mapBgGo = CreateUIElement("MapImage", mapScreenGo.transform);
            StretchFill(mapBgGo);
            var mapImg = mapBgGo.AddComponent<Image>();
            mapImg.sprite = LoadSprite(MapImagePath);
            mapImg.preserveAspect = false;
            mapImg.raycastTarget = false;

            // Map "GO" button (bottom-center)
            var goBtnGo = CreateButton(mapScreenGo.transform, "GoButton",
                "GO >", HexColor("#c9a96e"));
            SetAnchored(goBtnGo, new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.12f));

            // ══════════════════════════════════════════════════
            // GAMECORE
            // ══════════════════════════════════════════════════
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();

            var manager = gameCoreGo.AddComponent<Intro3Manager>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var so = new SerializedObject(manager);
            SetRef(so, "introScreen", introGroup);
            SetRef(so, "mapScreen", mapGroup);
            SetRef(so, "subtitleText", subtitleTmp);
            SetRef(so, "continueButton", continueBtnGo.GetComponent<Button>());
            SetRef(so, "goButton", goBtnGo.GetComponent<Button>());
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Intro3.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            Debug.Log($"[Intro3SceneBuilder] Scene built and saved to {scenePath}");
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

        private static GameObject CreateButton(Transform parent, string name,
            string label, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            btn.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            textGo.AddComponent<RectTransform>();
            StretchFill(textGo);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 42;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return go;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[Intro3SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
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
                Debug.LogWarning($"[Intro3SceneBuilder] No texture importer found for {path}");
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
                Debug.Log($"[Intro3SceneBuilder] Reimported {path} as Single Sprite");
            }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[Intro3SceneBuilder] Added {scenePath} to Build Settings");
        }
    }
}
#endif
