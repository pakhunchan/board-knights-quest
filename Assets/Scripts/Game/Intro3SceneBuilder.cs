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

        [MenuItem("Knight's Quest: Math Adventures/Build Intro3 Scene")]
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
            knightRect.pivot = new Vector2(0.5f, 0f);
            knightRect.anchoredPosition = new Vector2(0, -400);
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
            subtitleTmp.textWrappingMode = TextWrappingModes.Normal;
            subtitleTmp.overflowMode = TextOverflowModes.Overflow;
            subtitleTmp.richText = true;
            subtitleTmp.raycastTarget = false;
            if (fredokaFont != null) subtitleTmp.font = fredokaFont;

            // ── Continue button (nature-framed, hidden until narration ends) ──
            var circleSprite = Navigation.NavigationHelper.EnsureCircleSprite();

            var contGo = CreateUIElement("ContinueSection", introScreenGo.transform);
            var contRect = contGo.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.63f, 0.35f);
            contRect.anchorMax = new Vector2(0.93f, 0.58f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;
            var contGroup = contGo.AddComponent<CanvasGroup>();

            // ButtonWrap — parent container
            var btnWrapGo = CreateUIElement("ButtonWrap", contGo.transform);
            var btnWrapRect = btnWrapGo.GetComponent<RectTransform>();
            btnWrapRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnWrapRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnWrapRect.pivot = new Vector2(0.5f, 0.5f);
            btnWrapRect.sizeDelta = new Vector2(540, 200);

            var btnPillSprite = CreatePillSprite(128, 64, 32);

            // WoodRing (rounded background behind button)
            var woodRingGo = new GameObject("WoodRing");
            woodRingGo.transform.SetParent(btnWrapGo.transform, false);
            var woodRingRect = woodRingGo.AddComponent<RectTransform>();
            woodRingRect.anchorMin = new Vector2(0.5f, 0.5f);
            woodRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            woodRingRect.pivot = new Vector2(0.5f, 0.5f);
            woodRingRect.sizeDelta = new Vector2(540, 200);
            var woodRingImg = woodRingGo.AddComponent<Image>();
            woodRingImg.sprite = btnPillSprite;
            woodRingImg.type = Image.Type.Sliced;
            woodRingImg.pixelsPerUnitMultiplier = 2f;
            woodRingImg.color = HexColor("#8B6B3E");
            woodRingImg.raycastTarget = false;

            // DarkBorder (rounded)
            var darkBorderGo = new GameObject("DarkBorder");
            darkBorderGo.transform.SetParent(btnWrapGo.transform, false);
            var darkBorderRect = darkBorderGo.AddComponent<RectTransform>();
            darkBorderRect.anchorMin = new Vector2(0.5f, 0.5f);
            darkBorderRect.anchorMax = new Vector2(0.5f, 0.5f);
            darkBorderRect.pivot = new Vector2(0.5f, 0.5f);
            darkBorderRect.sizeDelta = new Vector2(530, 188);
            var darkBorderImg = darkBorderGo.AddComponent<Image>();
            darkBorderImg.sprite = btnPillSprite;
            darkBorderImg.type = Image.Type.Sliced;
            darkBorderImg.pixelsPerUnitMultiplier = 2f;
            darkBorderImg.color = HexColor("#5A7A42");
            darkBorderImg.raycastTarget = false;

            // ContinueButton (rounded, lighter meadow green)
            var continueBtnGo = new GameObject("ContinueButton");
            continueBtnGo.transform.SetParent(btnWrapGo.transform, false);
            var btnRect = continueBtnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(510, 168);
            var btnImg = continueBtnGo.AddComponent<Image>();
            btnImg.sprite = btnPillSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.pixelsPerUnitMultiplier = 2f;
            btnImg.color = HexColor("#8FBC6B");
            var btn = continueBtnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            var colors = btn.colors;
            colors.normalColor = HexColor("#8FBC6B");
            colors.highlightedColor = HexColor("#A3D48C");
            colors.pressedColor = HexColor("#7DAF5A");
            colors.selectedColor = HexColor("#8FBC6B");
            btn.colors = colors;

            // ButtonText
            var btnTextGo = new GameObject("ButtonText");
            btnTextGo.transform.SetParent(continueBtnGo.transform, false);
            var btnTextRect = btnTextGo.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnTextTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTextTmp.text = "CONTINUE";
            btnTextTmp.fontSize = 46;
            btnTextTmp.alignment = TextAlignmentOptions.Center;
            btnTextTmp.color = HexColor("#FFF5D4");
            btnTextTmp.raycastTarget = false;
            if (fredokaFont != null) btnTextTmp.font = fredokaFont;

            // Corner leaves (TL, TR, BL, BR)
            BuildLeaf(btnWrapGo.transform, "Leaf_TL", circleSprite, new Vector2(-280, 102), -20f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_TR", circleSprite, new Vector2(280, 102), 70f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BL", circleSprite, new Vector2(-280, -102), -70f, new Vector2(36, 22), "#8FBC6B");
            BuildLeaf(btnWrapGo.transform, "Leaf_BR", circleSprite, new Vector2(280, -102), 20f, new Vector2(36, 22), "#8FBC6B");

            // Small leaves between corners
            BuildLeaf(btnWrapGo.transform, "LeafSm1", circleSprite, new Vector2(-235, 108), -40f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm2", circleSprite, new Vector2(235, 108), 40f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm3", circleSprite, new Vector2(-225, -108), 50f, new Vector2(24, 14), "#A3D48C");
            BuildLeaf(btnWrapGo.transform, "LeafSm4", circleSprite, new Vector2(225, -108), -50f, new Vector2(24, 14), "#A3D48C");

            // Grass clusters (L, C, R)
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_L", -220f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_C", 0f);
            BuildGrassCluster(btnWrapGo.transform, "GrassCluster_R", 220f);

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
            gameCoreGo.AddComponent<BoardOfEducation.Audio.GameAudioManager>();

            var manager = gameCoreGo.AddComponent<Intro3Manager>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var so = new SerializedObject(manager);
            SetRef(so, "introScreen", introGroup);
            SetRef(so, "mapScreen", mapGroup);
            SetRef(so, "subtitleText", subtitleTmp);
            SetRef(so, "continueButton", btn);
            SetRef(so, "continueButtonGroup", contGroup);
            SetRef(so, "continueButtonRect", btnRect);
            SetRef(so, "goButton", goBtnGo.GetComponent<Button>());
            SetRef(so, "knightRect", knightRect);
            SetRef(so, "knightImage", knightImg);
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Intro3.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);

            // Clear play-mode start scene so this scene plays directly when hitting Play
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = null;

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

        private static Sprite CreatePillSprite(int width, int height, int radius)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[width * height];
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= width - radius) dx = x - (width - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= height - radius) dy = y - (height - radius - 1);

                    bool inside = (dx * dx + dy * dy) <= (radius * radius);
                    if (dx == 0 || dy == 0) inside = true;

                    pixels[y * width + x] = inside ? white : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            sprite.name = "PillSprite";
            return sprite;
        }

        private static void BuildLeaf(Transform parent, string name, Sprite circleSprite,
            Vector2 position, float rotation, Vector2 size, string hex)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localEulerAngles = new Vector3(0, 0, rotation);
            var img = go.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = HexColor(hex);
            img.raycastTarget = false;
        }

        private static void BuildGrassCluster(Transform parent, string name, float xOffset)
        {
            var clusterGo = CreateUIElement(name, parent);
            var clusterRect = clusterGo.GetComponent<RectTransform>();
            clusterRect.anchorMin = new Vector2(0.5f, 0f);
            clusterRect.anchorMax = new Vector2(0.5f, 0f);
            clusterRect.pivot = new Vector2(0.5f, 0f);
            clusterRect.sizeDelta = new Vector2(30, 16);
            clusterRect.anchoredPosition = new Vector2(xOffset, -4);

            float[] heights = { 12f, 16f, 10f, 14f };
            float[] xPositions = { -8f, -2f, 4f, 10f };
            for (int i = 0; i < 4; i++)
            {
                var bladeGo = CreateUIElement($"Blade{i}", clusterGo.transform);
                var bladeRect = bladeGo.GetComponent<RectTransform>();
                bladeRect.anchorMin = new Vector2(0.5f, 0f);
                bladeRect.anchorMax = new Vector2(0.5f, 0f);
                bladeRect.pivot = new Vector2(0.5f, 0f);
                bladeRect.sizeDelta = new Vector2(3, heights[i]);
                bladeRect.anchoredPosition = new Vector2(xPositions[i], 0);
                bladeRect.localEulerAngles = new Vector3(0, 0, (i % 2 == 0 ? -8f : 8f));
                var bladeImg = bladeGo.AddComponent<Image>();
                bladeImg.color = HexColor("#5A7A42");
                bladeImg.raycastTarget = false;
            }
        }
    }
}
#endif
