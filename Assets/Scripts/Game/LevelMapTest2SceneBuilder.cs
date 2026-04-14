#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Builds a test scene with the level-map-0 image as a full-screen background.
    /// Used to diagnose image-filling issues.
    /// Menu: Board of Education > Build LevelMap-Test2 Scene
    /// </summary>
    public static class LevelMapTest2SceneBuilder
    {
        [MenuItem("Board of Education/Build LevelMap-Test2 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string mapPath = "Assets/Textures/intro/level-map-0.png";

            // Ensure sprite import
            var importer = AssetImporter.GetAtPath(mapPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }
                if (importer.npotScale != TextureImporterNPOTScale.None)
                {
                    importer.npotScale = TextureImporterNPOTScale.None;
                    changed = true;
                }
                if (changed)
                    importer.SaveAndReimport();
            }
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
            cam.backgroundColor = Color.magenta;
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

            // ── Full-screen map image using Image + Sprite ──
            var mapBg = new GameObject("MapBackground");
            mapBg.transform.SetParent(canvasGo.transform, false);
            var rect = mapBg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = mapBg.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(mapPath);
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;

            if (img.sprite == null)
                Debug.LogWarning($"[LevelMapTest2] Could not load sprite at {mapPath}");
            else
                Debug.Log($"[LevelMapTest2] Loaded sprite: {img.sprite.texture.width}x{img.sprite.texture.height}");

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/LevelMap-Test2.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) { found = true; break; }
            }
            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            Debug.Log($"[LevelMapTest2] Scene built and saved to {scenePath}");
        }
    }
}
#endif
