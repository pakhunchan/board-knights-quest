#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the ChalkboardDemo scene.
    /// Menu: Board of Education > Build ChalkboardDemo Scene
    /// </summary>
    public static class ChalkboardDemoSceneBuilder
    {
        private static readonly string[] LayerPaths = new[]
        {
            "Assets/Textures/variants/chalkboard-layers/layer-0-background.png",
            "Assets/Textures/variants/chalkboard-layers/layer-1-frame.png",
            "Assets/Textures/variants/chalkboard-layers/layer-2-board.png",
            "Assets/Textures/variants/chalkboard-layers/layer-3-tray.png",
            "Assets/Textures/variants/chalkboard-layers/layer-4-border.png",
        };

        [MenuItem("Board of Education/Build ChalkboardDemo Scene")]
        public static void BuildChalkboardDemoScene()
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

            // ── Background (layer-0, always visible at full alpha) ──
            var bgGo = CreateUIElement("Background", canvasGo.transform);
            StretchFill(bgGo);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite(LayerPaths[0]);
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            // ── Overlay layers 1-4 (start at alpha 0) ──
            string[] layerNames = { "Frame", "Board", "Tray", "Border" };
            var overlayImages = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                var go = CreateUIElement(layerNames[i], canvasGo.transform);
                StretchFill(go);
                var img = go.AddComponent<Image>();
                img.sprite = LoadSprite(LayerPaths[i + 1]);
                img.preserveAspect = false;
                img.raycastTarget = false;

                var c = img.color;
                c.a = 0f;
                img.color = c;

                overlayImages[i] = img;
            }

            // ── GameCore ──
            var gameCoreGo = new GameObject("GameCore");
            var manager = gameCoreGo.AddComponent<ChalkboardDemoManager>();

            // ── Wire up serialized references ──
            var managerSO = new SerializedObject(manager);
            var layersProp = managerSO.FindProperty("layers");
            layersProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
                layersProp.GetArrayElementAtIndex(i).objectReferenceValue = overlayImages[i];
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/ChalkboardDemo.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[ChalkboardDemoSceneBuilder] Scene built and saved to {scenePath}");
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
                Debug.LogWarning($"[ChalkboardDemoSceneBuilder] Could not find field '{fieldName}' on {so.targetObject.GetType().Name}");
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
                Debug.LogWarning($"[ChalkboardDemoSceneBuilder] No texture importer found for {path}");
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
                Debug.Log($"[ChalkboardDemoSceneBuilder] Reimported {path} as Single Sprite");
            }
        }
    }
}
#endif
