#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardOfEducation.Game
{
    public static class Test1SceneBuilder
    {
        [MenuItem("Board of Education/Build Test1 Scene")]
        public static void BuildScene()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // Camera
            var cameraGo = new GameObject("Main Camera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.magenta; // obvious if visible
            cameraGo.tag = "MainCamera";

            // Canvas — exact same setup as Intro3
            var canvasGo = new GameObject("MainCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Single full-screen RawImage
            var imgGo = new GameObject("MapImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rect = imgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Fix NPOT texture padding for Android
            var texPath = "Assets/Textures/intro/level-map-0.png";
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null && importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            var rawImg = imgGo.AddComponent<RawImage>();
            rawImg.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            rawImg.raycastTarget = false;

            // Save
            string scenePath = "Assets/Scenes/Test1.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[Test1] Scene saved to " + scenePath);
        }
    }
}
#endif
