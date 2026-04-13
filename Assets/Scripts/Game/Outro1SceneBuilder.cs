#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the Outro1 scene.
    /// Knight farewell screen with karaoke-style subtitle over landscape background.
    /// Menu: Board of Education > Build Outro1 Scene
    /// </summary>
    public static class Outro1SceneBuilder
    {
        [MenuItem("Board of Education/Build Outro1 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // ── Pre-step: ensure textures are imported as Sprite ──
            string[] spritePaths = new[]
            {
                "Assets/Textures/landscape_bg_soft.png",
                "Assets/Textures/knight-bronze.png",
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
            cam.backgroundColor = HexColor("#1a1a2e");
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

            // Load Cinzel font
            var cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Cinzel SDF.asset");
            if (cinzelFont == null)
                Debug.LogWarning("[Outro1SceneBuilder] Cinzel SDF font not found.");

            // ══════════════════════════════════════════════════
            // SCREEN GROUP
            // ══════════════════════════════════════════════════

            var screenGo = CreateUIElement("ScreenGroup", canvasGo.transform);
            StretchFill(screenGo);
            var screenGroup = screenGo.AddComponent<CanvasGroup>();

            // Background (landscape_bg_soft.png, stretch-fill)
            var bgGo = CreateUIElement("Background", screenGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            var bgSprite = LoadSprite("Assets/Textures/landscape_bg_soft.png");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = HexColor("#1a1a2e");
                Debug.LogWarning("[Outro1SceneBuilder] landscape_bg_soft.png not found, using fallback color.");
            }

            // Knight (knight-bronze.png, center, preserveAspect, 600x800)
            var knightGo = new GameObject("Knight");
            knightGo.transform.SetParent(screenGo.transform, false);
            var knightRect = knightGo.AddComponent<RectTransform>();
            knightRect.anchorMin = new Vector2(0.5f, 0.5f);
            knightRect.anchorMax = new Vector2(0.5f, 0.5f);
            knightRect.pivot = new Vector2(0.5f, 0.5f);
            knightRect.sizeDelta = new Vector2(600, 800);
            knightRect.anchoredPosition = Vector2.zero;
            var knightImg = knightGo.AddComponent<Image>();
            knightImg.raycastTarget = false;
            var knightSprite = LoadSprite("Assets/Textures/knight-bronze.png");
            if (knightSprite != null)
            {
                knightImg.sprite = knightSprite;
                knightImg.color = Color.white;
                knightImg.preserveAspect = true;
            }
            else
            {
                knightImg.color = new Color(1, 1, 1, 0);
                Debug.LogWarning("[Outro1SceneBuilder] knight-bronze.png not found.");
            }

            // Subtitle (TMP: Cinzel SDF, fontSize 32, white, center)
            var subtitleGo = new GameObject("Subtitle");
            subtitleGo.transform.SetParent(screenGo.transform, false);
            var subtitleRect = subtitleGo.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.1f, 0.05f);
            subtitleRect.anchorMax = new Vector2(0.9f, 0.18f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            var subTmp = subtitleGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "";
            subTmp.fontSize = 32;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = Color.white;
            subTmp.enableWordWrapping = true;
            subTmp.overflowMode = TextOverflowModes.Overflow;
            subTmp.raycastTarget = false;
            if (cinzelFont != null) subTmp.font = cinzelFont;

            // ══════════════════════════════════════════════════
            // GAMECORE
            // ══════════════════════════════════════════════════
            var gameCoreGo = new GameObject("GameCore");
            gameCoreGo.AddComponent<Core.BoardStartup>();
            gameCoreGo.AddComponent<Input.PieceManager>();

            var manager = gameCoreGo.AddComponent<Outro1Manager>();

            // ══════════════════════════════════════════════════
            // WIRE UP SERIALIZED REFERENCES
            // ══════════════════════════════════════════════════
            var so = new SerializedObject(manager);
            SetRef(so, "subtitleText", subTmp);
            SetRef(so, "screenGroup", screenGroup);
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/Outro1.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            AddToBuildSettings(scenePath);
            Debug.Log($"[Outro1SceneBuilder] Scene built and saved to {scenePath}");
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

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[Outro1SceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
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
                Debug.LogWarning($"[Outro1SceneBuilder] No texture importer found for {path}");
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
                Debug.Log($"[Outro1SceneBuilder] Reimported {path} as Single Sprite");
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
            Debug.Log($"[Outro1SceneBuilder] Added {scenePath} to Build Settings");
        }
    }
}
#endif
