#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Builds a minimal test scene with a full-screen green background.
    /// Used to diagnose screen-filling issues in level map scenes.
    /// Menu: Board of Education > Build LevelMap-Test Scene
    /// </summary>
    public static class LevelMapTestSceneBuilder
    {
        [MenuItem("Board of Education/Build LevelMap-Test Scene")]
        public static void BuildScene()
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
            cam.backgroundColor = Color.green;
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

            // ── MainCanvas (ScreenSpaceOverlay, same as LevelMap0) ──
            var canvasGo = new GameObject("MainCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Full-screen green panel ──
            var greenBg = new GameObject("GreenBackground");
            greenBg.transform.SetParent(canvasGo.transform, false);
            var rect = greenBg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = greenBg.AddComponent<Image>();
            img.color = Color.green;
            img.raycastTarget = false;

            // ── Red corner markers (50x50 each) ──
            CreateCornerMarker("TopLeft", canvasGo.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -50f), new Vector2(50f, 0f));

            CreateCornerMarker("TopRight", canvasGo.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-50f, -50f), new Vector2(0f, 0f));

            CreateCornerMarker("BottomLeft", canvasGo.transform,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(50f, 50f));

            CreateCornerMarker("BottomRight", canvasGo.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-50f, 0f), new Vector2(0f, 50f));

            // ── Save Scene ──
            string scenePath = "Assets/Scenes/LevelMap-Test.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings
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

            Debug.Log($"[LevelMapTestSceneBuilder] Scene 'LevelMap-Test' built and saved to {scenePath}");
        }
        private static void CreateCornerMarker(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("Corner_" + name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.color = Color.red;
            img.raycastTarget = false;
        }
    }
}
#endif
