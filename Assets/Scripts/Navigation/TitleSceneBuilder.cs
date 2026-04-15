#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Navigation
{
    /// <summary>
    /// Editor utility that builds the TitleScreen scene programmatically.
    /// Menu: Board of Education > Build Title Scene
    /// </summary>
    public static class TitleSceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build Title Scene")]
        public static void BuildTitleScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure all textures are imported as Sprite ──
            EnsureSpriteImport("Assets/Textures/title-bg.png");
            EnsureSpriteImport("Assets/Textures/robot-yellow.png");
            EnsureSpriteImport("Assets/Textures/robot-purple.png");
            EnsureSpriteImport("Assets/Textures/robot-orange.png");
            EnsureSpriteImport("Assets/Textures/robot-pink.png");
            EnsureSpriteImport("Assets/Textures/shield-bg.png");
            EnsureSpriteImport("Assets/Textures/shield-bg-grey.png");
            EnsureSpriteImport("Assets/Textures/play-text.png");
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
            cameraGo.AddComponent<AudioListener>();

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
            gameCoreGo.AddComponent<Audio.GameAudioManager>();
            var titleManager = gameCoreGo.AddComponent<TitleScreenManager>();

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
            bgImg.raycastTarget = false;

            // Load the title background screenshot
            var bgSpritePath = "Assets/Textures/title-bg.png";
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);
            Debug.Log($"[TitleSceneBuilder] Background sprite loaded: {bgSprite != null}");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
                bgImg.preserveAspect = false; // stretch-fill to match canvas
            }
            else
            {
                bgImg.color = HexColor("#0f0e2a"); // fallback flat color
                Debug.LogWarning("[TitleSceneBuilder] Could not load title-bg.png, using flat color fallback.");
            }

            // ══════════════════════════════════════════════════
            // PLAY ZONE — regular hexagon, square aspect ratio
            // Using fixed size so hexagon stays square regardless of screen aspect
            // ══════════════════════════════════════════════════
            var playZoneGo = new GameObject("PlayZone");
            playZoneGo.transform.SetParent(canvasGo.transform, false);
            var playZoneRect = playZoneGo.AddComponent<RectTransform>();
            // Anchor to bottom-center area
            playZoneRect.anchorMin = new Vector2(0.5f, 0.08f);
            playZoneRect.anchorMax = new Vector2(0.5f, 0.08f);
            playZoneRect.pivot = new Vector2(0.5f, 0f);
            playZoneRect.sizeDelta = new Vector2(546, 546); // 420 * 1.3

            // Play zone background — grey hexagon (base, always visible)
            var playZoneImg = playZoneGo.AddComponent<Image>();
            var greyShieldSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/shield-bg-grey.png");
            if (greyShieldSprite != null)
            {
                playZoneImg.sprite = greyShieldSprite;
                playZoneImg.color = Color.white;
                playZoneImg.preserveAspect = true;
                playZoneImg.type = Image.Type.Simple;
            }

            // Green hexagon overlay (fades in when robot placed)
            var greenOverlayGo = new GameObject("GreenOverlay");
            greenOverlayGo.transform.SetParent(playZoneGo.transform, false);
            var greenOverlayRect = greenOverlayGo.AddComponent<RectTransform>();
            greenOverlayRect.anchorMin = Vector2.zero;
            greenOverlayRect.anchorMax = Vector2.one;
            greenOverlayRect.offsetMin = Vector2.zero;
            greenOverlayRect.offsetMax = Vector2.zero;
            var greenOverlayImg = greenOverlayGo.AddComponent<Image>();
            var greenShieldSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/shield-bg.png");
            if (greenShieldSprite != null)
            {
                greenOverlayImg.sprite = greenShieldSprite;
                greenOverlayImg.color = new Color(1, 1, 1, 0f); // start transparent
                greenOverlayImg.preserveAspect = true;
                greenOverlayImg.type = Image.Type.Simple;
                greenOverlayImg.raycastTarget = false;
            }

            // Load Cinzel font
            var cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Cinzel SDF.asset");
            if (cinzelFont == null)
                Debug.LogWarning("[TitleSceneBuilder] Cinzel SDF font not found. Run Tools > Setup Cinzel Font first.");

            // ── "PLAY" title — pre-rendered PNG from Cinzel 700 ──
            var playTitleGo = new GameObject("PlayTitle");
            playTitleGo.transform.SetParent(canvasGo.transform, false);
            var playTitleRect = playTitleGo.AddComponent<RectTransform>();
            playTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            playTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            playTitleRect.pivot = new Vector2(0.5f, 0.5f);
            playTitleRect.sizeDelta = new Vector2(875, 220);
            playTitleRect.anchoredPosition = new Vector2(0, -125);
            var playImg = playTitleGo.AddComponent<Image>();
            var playTextSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/play-text.png");
            if (playTextSprite != null)
            {
                playImg.sprite = playTextSprite;
                playImg.color = Color.white;
                playImg.preserveAspect = true;
                playImg.raycastTarget = false;
            }

            // ── Subtitle — Cinzel, smaller ──
            var subtitleGo = new GameObject("Subtitle");
            subtitleGo.transform.SetParent(canvasGo.transform, false);
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
            if (cinzelFont != null) subTmp.font = cinzelFont;

            // ── Robot images (lower-center, 70% smaller, tighter) ──
            var robotContainer = new GameObject("RobotContainer");
            robotContainer.transform.SetParent(playZoneGo.transform, false);
            var containerRect = robotContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.32f, 0.25f);
            containerRect.anchorMax = new Vector2(0.68f, 0.43f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Load robot sprites
            string[] robotPaths = new[]
            {
                "Assets/Textures/robot-yellow.png",
                "Assets/Textures/robot-purple.png",
                "Assets/Textures/robot-orange.png",
                "Assets/Textures/robot-pink.png",
            };
            string[] robotNames = { "RobotYellow", "RobotPurple", "RobotOrange", "RobotPink" };

            // 4 robots tightly packed inside the hexagon
            for (int i = 0; i < 4; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(robotPaths[i]);
                Debug.Log($"[TitleSceneBuilder] Robot sprite {robotNames[i]} loaded: {sprite != null}");
                if (sprite == null)
                {
                    Debug.LogWarning($"[TitleSceneBuilder] Could not load sprite at {robotPaths[i]}. Run SVG conversion first.");
                    continue;
                }

                var robotGo = new GameObject(robotNames[i]);
                robotGo.transform.SetParent(robotContainer.transform, false);
                var robotRect = robotGo.AddComponent<RectTransform>();

                // 4 tightly packed columns on a concave-down curve
                float slotWidth = 1f / 4f;
                float centerX = slotWidth * i + slotWidth * 0.5f;
                robotRect.anchorMin = new Vector2(centerX - 0.12f, 0f);
                robotRect.anchorMax = new Vector2(centerX + 0.12f, 1f);
                robotRect.offsetMin = Vector2.zero;
                robotRect.offsetMax = Vector2.zero;

                // Arc offset: center robots (1,2) sit higher, outer robots (0,3) sit lower
                // Normalized position: -1.5, -0.5, 0.5, 1.5 from center
                float t = (i - 1.5f) / 1.5f; // -1 to 1
                float arcOffset = (1f - t * t) * 12f; // parabola peak at center, 12px max
                robotRect.anchoredPosition = new Vector2(0, arcOffset - 15f);

                // Slight rotation: outer robots tilt outward
                float tilt = -t * 5f; // ±5 degrees
                robotGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);

                var robotImg = robotGo.AddComponent<Image>();
                robotImg.sprite = sprite;
                robotImg.preserveAspect = true;
                robotImg.raycastTarget = false;
            }

            // ── Version label (bottom-left) ──
            var versionGo = new GameObject("VersionLabel");
            versionGo.transform.SetParent(canvasGo.transform, false);
            var versionRect = versionGo.AddComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(0f, 0f);
            versionRect.anchorMax = new Vector2(0f, 0f);
            versionRect.pivot = new Vector2(0f, 0f);
            versionRect.sizeDelta = new Vector2(200, 40);
            versionRect.anchoredPosition = new Vector2(20, 10);
            var versionTmp = versionGo.AddComponent<TextMeshProUGUI>();
            versionTmp.text = "v0.0.16";
            versionTmp.fontSize = 18;
            versionTmp.alignment = TextAlignmentOptions.BottomLeft;
            versionTmp.color = Color.white;
            versionTmp.textWrappingMode = TextWrappingModes.NoWrap;
            versionTmp.overflowMode = TextOverflowModes.Overflow;

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var managerSO = new SerializedObject(titleManager);
            SetRef(managerSO, "playZone", playZoneRect);
            SetRef(managerSO, "subtitleText", subtitleGo.GetComponent<TextMeshProUGUI>());
            SetRef(managerSO, "greenOverlay", greenOverlayImg);
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/TitleScreen.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[TitleSceneBuilder] Title scene built and saved to {scenePath}");

            // ── Add to build settings ──
            AddToBuildSettings(scenePath);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var currentScenes = EditorBuildSettings.scenes;

            // Check if already in build settings
            foreach (var s in currentScenes)
            {
                if (s.path == scenePath) return;
            }

            // Add TitleScreen as the first scene (index 0) so it's the startup scene
            var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < currentScenes.Length; i++)
                newScenes[i + 1] = currentScenes[i];

            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[TitleSceneBuilder] Added TitleScreen.unity as build index 0 (startup scene)");
        }

        // ── Helpers (same pattern as LandingSceneBuilder) ────

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
                Debug.LogWarning($"[TitleSceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
        }

        private static void EnsureSpriteImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[TitleSceneBuilder] No texture importer found for {path}");
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
                Debug.Log($"[TitleSceneBuilder] Reimported {path} as Single Sprite");
            }
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
#endif
