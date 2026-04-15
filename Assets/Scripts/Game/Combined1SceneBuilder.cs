#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BoardOfEducation.Lessons;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Combined1 scene.
    /// Screen 1: Title screen (native sprites with hex play zone + robots).
    /// Screen 2: Level map with GO button.
    /// Screen 3: Chalkboard layer fade + TotalFractions2 lesson.
    /// Menu: Board of Education > Build Combined1 Scene
    /// </summary>
    public static class Combined1SceneBuilder
    {
        private static readonly string[] ChalkboardLayerPaths = new[]
        {
            "Assets/Textures/variants/chalkboard-layers/layer-0-background.png",
            "Assets/Textures/variants/chalkboard-layers/layer-1-frame.png",
            "Assets/Textures/variants/chalkboard-layers/layer-2-board.png",
            "Assets/Textures/variants/chalkboard-layers/layer-3-tray.png",
            "Assets/Textures/variants/chalkboard-layers/layer-4-border.png",
        };

        [MenuItem("Knight's Quest: Math Adventures/Build Combined1 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure all textures are imported as Sprite ──
            string[] introSprites = new[]
            {
                "Assets/Textures/title-bg.png",
                "Assets/Textures/shield-bg.png",
                "Assets/Textures/shield-bg-grey.png",
                "Assets/Textures/play-text.png",
                "Assets/Textures/robot-yellow.png",
                "Assets/Textures/robot-purple.png",
                "Assets/Textures/robot-orange.png",
                "Assets/Textures/robot-pink.png",
                "Assets/Textures/intro/level-map.png",
            };
            foreach (var path in introSprites)
                EnsureSpriteImport(path);
            foreach (var path in ChalkboardLayerPaths)
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
            var boardInputModule = eventSystemGo.AddComponent<Board.Input.BoardUIInputModule>();
            var boardInputSO = new SerializedObject(boardInputModule);
            var maskProp = boardInputSO.FindProperty("m_InputMask.m_Bits");
            if (maskProp != null) { maskProp.longValue = 3; boardInputSO.ApplyModifiedPropertiesWithoutUndo(); }

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

            // Load Cinzel font
            var cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Cinzel SDF.asset");

            // ══════════════════════════════════════════════════
            // SCREEN 1: TITLE SCREEN (native sprites)
            // ══════════════════════════════════════════════════

            var titleScreenGo = CreateUIElement("TitleScreen", canvasGo.transform);
            StretchFill(titleScreenGo);
            var titleGroup = titleScreenGo.AddComponent<CanvasGroup>();

            // Background
            var bgGo = CreateUIElement("Background", titleScreenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/title-bg.png");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = HexColor("#0f0e2a");
            }

            // PlayZone — hex, anchored bottom-center
            var playZoneGo = new GameObject("PlayZone");
            playZoneGo.transform.SetParent(titleScreenGo.transform, false);
            var playZoneRect = playZoneGo.AddComponent<RectTransform>();
            playZoneRect.anchorMin = new Vector2(0.5f, 0.08f);
            playZoneRect.anchorMax = new Vector2(0.5f, 0.08f);
            playZoneRect.pivot = new Vector2(0.5f, 0f);
            playZoneRect.sizeDelta = new Vector2(546, 546);

            // Grey hex base — also serves as PLAY button target graphic
            var playZoneImg = playZoneGo.AddComponent<Image>();
            var greyShieldSprite = LoadSprite("Assets/Textures/shield-bg-grey.png");
            if (greyShieldSprite != null)
            {
                playZoneImg.sprite = greyShieldSprite;
                playZoneImg.color = Color.white;
                playZoneImg.preserveAspect = true;
                playZoneImg.type = Image.Type.Simple;
            }

            // Button directly on PlayZone so the entire hex area is clickable
            var playBtn = playZoneGo.AddComponent<Button>();
            playBtn.targetGraphic = playZoneImg;
            var playBtnColors = playBtn.colors;
            playBtnColors.normalColor = Color.white;
            playBtnColors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            playBtnColors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            playBtn.colors = playBtnColors;

            // Green hex overlay
            var greenOverlayGo = CreateUIElement("GreenOverlay", playZoneGo.transform);
            StretchFill(greenOverlayGo);
            var greenOverlayImg = greenOverlayGo.AddComponent<Image>();
            var greenShieldSprite = LoadSprite("Assets/Textures/shield-bg.png");
            if (greenShieldSprite != null)
            {
                greenOverlayImg.sprite = greenShieldSprite;
                greenOverlayImg.color = new Color(1, 1, 1, 0f);
                greenOverlayImg.preserveAspect = true;
                greenOverlayImg.type = Image.Type.Simple;
                greenOverlayImg.raycastTarget = false;
            }

            // PLAY text PNG
            var playTitleGo = new GameObject("PlayTitle");
            playTitleGo.transform.SetParent(titleScreenGo.transform, false);
            var playTitleRect = playTitleGo.AddComponent<RectTransform>();
            playTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            playTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            playTitleRect.pivot = new Vector2(0.5f, 0.5f);
            playTitleRect.sizeDelta = new Vector2(875, 220);
            playTitleRect.anchoredPosition = new Vector2(0, -125);
            var playImg = playTitleGo.AddComponent<Image>();
            var playTextSprite = LoadSprite("Assets/Textures/play-text.png");
            if (playTextSprite != null)
            {
                playImg.sprite = playTextSprite;
                playImg.color = Color.white;
                playImg.preserveAspect = true;
                playImg.raycastTarget = false;
            }

            // Subtitle
            var subtitleGo = new GameObject("Subtitle");
            subtitleGo.transform.SetParent(titleScreenGo.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.pivot = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(500, 40);
            subtitleRect.anchoredPosition = new Vector2(0, -175);
            var subTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Place a robot on the board to begin!";
            subTmp.fontSize = 14;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.91f, 0.94f, 0.85f, 0.75f);
            subTmp.characterSpacing = 2f;
            subTmp.textWrappingMode = TextWrappingModes.NoWrap;
            subTmp.overflowMode = TextOverflowModes.Overflow;
            subTmp.raycastTarget = false;
            if (cinzelFont != null) subTmp.font = cinzelFont;

            // Robot images in arc arrangement
            var robotContainer = new GameObject("RobotContainer");
            robotContainer.transform.SetParent(playZoneGo.transform, false);
            var containerRect = robotContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.32f, 0.25f);
            containerRect.anchorMax = new Vector2(0.68f, 0.43f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            string[] robotPaths = new[]
            {
                "Assets/Textures/robot-yellow.png",
                "Assets/Textures/robot-purple.png",
                "Assets/Textures/robot-orange.png",
                "Assets/Textures/robot-pink.png",
            };
            string[] robotNames = { "RobotYellow", "RobotPurple", "RobotOrange", "RobotPink" };

            for (int i = 0; i < 4; i++)
            {
                var sprite = LoadSprite(robotPaths[i]);
                if (sprite == null) continue;

                var robotGo = new GameObject(robotNames[i]);
                robotGo.transform.SetParent(robotContainer.transform, false);
                var robotRect = robotGo.AddComponent<RectTransform>();

                float slotWidth = 1f / 4f;
                float centerX = slotWidth * i + slotWidth * 0.5f;
                robotRect.anchorMin = new Vector2(centerX - 0.12f, 0f);
                robotRect.anchorMax = new Vector2(centerX + 0.12f, 1f);
                robotRect.offsetMin = Vector2.zero;
                robotRect.offsetMax = Vector2.zero;

                float t = (i - 1.5f) / 1.5f;
                float arcOffset = (1f - t * t) * 12f;
                robotRect.anchoredPosition = new Vector2(0, arcOffset - 15f);

                float tilt = -t * 5f;
                robotGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);

                var robotImg = robotGo.AddComponent<Image>();
                robotImg.sprite = sprite;
                robotImg.preserveAspect = true;
                robotImg.raycastTarget = false;
            }

            // ══════════════════════════════════════════════════
            // SCREEN 2: LEVEL MAP
            // ══════════════════════════════════════════════════

            var mapScreenGo = CreateUIElement("MapScreen", canvasGo.transform);
            StretchFill(mapScreenGo);
            var mapGroup = mapScreenGo.AddComponent<CanvasGroup>();

            var mapBgGo = CreateUIElement("MapImage", mapScreenGo.transform);
            StretchFill(mapBgGo);
            var mapImg = mapBgGo.AddComponent<Image>();
            mapImg.sprite = LoadSprite("Assets/Textures/intro/level-map.png");
            mapImg.preserveAspect = false;
            mapImg.raycastTarget = false;

            var goBtnGo = CreateButton(mapScreenGo.transform, "GoButton",
                "GO >", HexColor("#c9a96e"));
            SetAnchored(goBtnGo, new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.12f));

            // ══════════════════════════════════════════════════
            // SCREEN 3: CHALKBOARD + LESSON
            // ══════════════════════════════════════════════════

            var lessonScreenGo = CreateUIElement("LessonScreen", canvasGo.transform);
            StretchFill(lessonScreenGo);
            var lessonGroup = lessonScreenGo.AddComponent<CanvasGroup>();

            // Chalkboard background (layer-0, always visible within this screen)
            var chalkBgGo = CreateUIElement("ChalkBackground", lessonScreenGo.transform);
            StretchFill(chalkBgGo);
            var chalkBgImg = chalkBgGo.AddComponent<Image>();
            chalkBgImg.sprite = LoadSprite(ChalkboardLayerPaths[0]);
            chalkBgImg.preserveAspect = false;
            chalkBgImg.raycastTarget = false;

            // Chalkboard overlay layers 1-4 (start at alpha 0)
            string[] layerNames = { "Frame", "Board", "Tray", "Border" };
            var overlayImages = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                var go = CreateUIElement(layerNames[i], lessonScreenGo.transform);
                StretchFill(go);
                var img = go.AddComponent<Image>();
                img.sprite = LoadSprite(ChalkboardLayerPaths[i + 1]);
                img.preserveAspect = false;
                img.raycastTarget = false;

                var c = img.color;
                c.a = 0f;
                img.color = c;

                overlayImages[i] = img;
            }

            // ContentArea with CanvasGroup (starts invisible)
            var contentAreaGo = CreateUIElement("ContentArea", lessonScreenGo.transform);
            var contentAreaRect = contentAreaGo.GetComponent<RectTransform>();
            contentAreaRect.anchorMin = new Vector2(0.05f, 0.18f);
            contentAreaRect.anchorMax = new Vector2(0.95f, 0.95f);
            contentAreaRect.offsetMin = Vector2.zero;
            contentAreaRect.offsetMax = Vector2.zero;

            var contentGroup = contentAreaGo.AddComponent<CanvasGroup>();
            contentGroup.alpha = 0f;

            // SubtitleText
            var lessonSubtitleGo = CreateText(lessonScreenGo.transform, "SubtitleText", "",
                36, TextAlignmentOptions.Center, new Color(1, 1, 1, 0.9f));
            SetAnchored(lessonSubtitleGo, new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.14f));

            // Lesson PlayButton
            var lessonPlayBtnGo = CreateButton(lessonScreenGo.transform, "LessonPlayButton",
                "\u25b6 PLAY", HexColor("#2ecc71"));
            SetAnchored(lessonPlayBtnGo, new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.33f));

            // ══════════════════════════════════════════════════
            // GAMECORE
            // ══════════════════════════════════════════════════
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();

            var chalkboardManager = gameCoreGo.AddComponent<ChalkboardDemoManager>();
            chalkboardManager.enabled = false; // enabled by Combined1Manager when screen 3 is visible

            var lessonManager = gameCoreGo.AddComponent<TotalFractions2Manager>();
            lessonManager.enabled = false;

            var sequencer = gameCoreGo.AddComponent<LessonSequencer>();

            var orchestrator = gameCoreGo.AddComponent<TotalFractions2DemoWithBGManager>();
            orchestrator.enabled = false; // enabled by Combined1Manager when screen 3 is visible

            var combined = gameCoreGo.AddComponent<Combined1Manager>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════

            // Combined1Manager
            var combinedSO = new SerializedObject(combined);
            SetRef(combinedSO, "titleScreen", titleGroup);
            SetRef(combinedSO, "mapScreen", mapGroup);
            SetRef(combinedSO, "lessonScreen", lessonGroup);
            SetRef(combinedSO, "playButton", playBtn);
            SetRef(combinedSO, "goButton", goBtnGo.GetComponent<Button>());
            SetRef(combinedSO, "chalkboardManager", chalkboardManager);
            SetRef(combinedSO, "orchestrator", orchestrator);
            combinedSO.ApplyModifiedPropertiesWithoutUndo();

            // ChalkboardDemoManager.layers
            var chalkboardSO = new SerializedObject(chalkboardManager);
            var layersProp = chalkboardSO.FindProperty("layers");
            layersProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
                layersProp.GetArrayElementAtIndex(i).objectReferenceValue = overlayImages[i];
            chalkboardSO.ApplyModifiedPropertiesWithoutUndo();

            // LessonSequencer
            var sequencerSO = new SerializedObject(sequencer);
            SetRef(sequencerSO, "subtitleText", lessonSubtitleGo.GetComponent<TextMeshProUGUI>());
            sequencerSO.ApplyModifiedPropertiesWithoutUndo();

            // TotalFractions2Manager
            var managerSO = new SerializedObject(lessonManager);
            SetRef(managerSO, "contentArea", contentAreaRect);
            SetRef(managerSO, "playButton", lessonPlayBtnGo.GetComponent<Button>());
            SetRef(managerSO, "playButtonGo", lessonPlayBtnGo);
            SetRef(managerSO, "sequencer", sequencer);
            managerSO.FindProperty("autoPlay").boolValue = true;
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // TotalFractions2DemoWithBGManager (orchestrator)
            var orchestratorSO = new SerializedObject(orchestrator);
            SetRef(orchestratorSO, "chalkboardManager", chalkboardManager);
            SetRef(orchestratorSO, "lessonManager", lessonManager);
            SetRef(orchestratorSO, "contentGroup", contentGroup);
            orchestratorSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Combined1.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            Debug.Log($"[Combined1SceneBuilder] Scene built and saved to {scenePath}");
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return go;
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
                Debug.LogWarning($"[Combined1SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
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
                Debug.LogWarning($"[Combined1SceneBuilder] No texture importer found for {path}");
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
                Debug.Log($"[Combined1SceneBuilder] Reimported {path} as Single Sprite");
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
            Debug.Log($"[Combined1SceneBuilder] Added {scenePath} to Build Settings");
        }
    }
}
#endif
