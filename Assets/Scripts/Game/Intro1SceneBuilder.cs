#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Intro1 scene.
    /// Combines the Menu title screen and Level Map as two cross-fadeable screens.
    /// Menu: Board of Education > Build Intro1 Scene
    /// </summary>
    public static class Intro1SceneBuilder
    {
        private const string MenuImagePath = "Assets/Textures/intro/menu-title.png";
        private const string MapImagePath = "Assets/Textures/intro/level-map.png";

        [MenuItem("Knight's Quest: Math Adventures/Build Intro1 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure textures are imported as Sprite ──
            EnsureSpriteImport(MenuImagePath);
            EnsureSpriteImport(MapImagePath);
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

            // ══════════════════════════════════════════════════
            // SCREEN 1: MENU TITLE
            // ══════════════════════════════════════════════════

            var menuScreenGo = CreateUIElement("MenuScreen", canvasGo.transform);
            StretchFill(menuScreenGo);
            var menuGroup = menuScreenGo.AddComponent<CanvasGroup>();

            // Menu background image
            var menuBgGo = CreateUIElement("MenuImage", menuScreenGo.transform);
            StretchFill(menuBgGo);
            var menuImg = menuBgGo.AddComponent<Image>();
            menuImg.sprite = LoadSprite(MenuImagePath);
            menuImg.preserveAspect = false;
            menuImg.raycastTarget = false;

            // Menu "PLAY" button (centered, matching the shield position in the HTML)
            var menuBtnGo = CreateButton(menuScreenGo.transform, "MenuNextButton",
                "PLAY", HexColor("#4a7a3a"));
            SetAnchored(menuBtnGo, new Vector2(0.40f, 0.35f), new Vector2(0.60f, 0.48f));

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

            // Map "GO" button (bottom-center, below the path)
            var mapBtnGo = CreateButton(mapScreenGo.transform, "MapNextButton",
                "GO ▸", HexColor("#c9a96e"));
            SetAnchored(mapBtnGo, new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.12f));

            // ══════════════════════════════════════════════════
            // GAMECORE
            // ══════════════════════════════════════════════════
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();

            var manager = gameCoreGo.AddComponent<Intro1Manager>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var so = new SerializedObject(manager);
            SetRef(so, "menuScreen", menuGroup);
            SetRef(so, "mapScreen", mapGroup);
            SetRef(so, "menuNextButton", menuBtnGo.GetComponent<Button>());
            SetRef(so, "mapNextButton", mapBtnGo.GetComponent<Button>());
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Intro1.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            Debug.Log($"[Intro1SceneBuilder] Scene built and saved to {scenePath}");
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
                Debug.LogWarning($"[Intro1SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
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
                Debug.LogWarning($"[Intro1SceneBuilder] No texture importer found for {path}");
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
                Debug.Log($"[Intro1SceneBuilder] Reimported {path} as Single Sprite");
            }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (var s in scenes)
            {
                if (s.path == scenePath) return; // already present
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[Intro1SceneBuilder] Added {scenePath} to Build Settings");
        }
    }
}
#endif
