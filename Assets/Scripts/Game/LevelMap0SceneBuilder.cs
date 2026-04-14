#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Editor utility that builds the LevelMap0 scene (0 levels cleared).
    /// Shows the level map with all nodes locked and a GO button.
    /// Menu: Board of Education > Build LevelMap0 Scene
    /// </summary>
    public static class LevelMap0SceneBuilder
    {
        [MenuItem("Board of Education/Build LevelMap0 Scene")]
        public static void BuildScene()
        {
            LevelMapSceneBuilderHelper.BuildLevelMapScene(
                sceneName: "LevelMap0",
                mapSpritePath: "Assets/Textures/intro/level-map-0.png",
                nextScene: "TotalFractions2DemoWithBG"
            );
        }
    }
}
#endif
