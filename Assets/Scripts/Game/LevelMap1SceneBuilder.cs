#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the LevelMap1 scene (1 level cleared).
    /// Shows the level map with node 1 cleared and node 2 as current.
    /// Menu: Board of Education > Build LevelMap1 Scene
    /// </summary>
    public static class LevelMap1SceneBuilder
    {
        [MenuItem("Knight's Quest: Math Adventures/Build LevelMap1 Scene")]
        public static void BuildScene()
        {
            LevelMapSceneBuilderHelper.BuildLevelMapScene(
                sceneName: "LevelMap1",
                mapSpritePath: "Assets/Textures/intro/level-map-1.png",
                nextScene: "TotalFractions2DemoWithBG"
            );
        }
    }
}
#endif
