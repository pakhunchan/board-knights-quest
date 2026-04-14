using UnityEngine;
using UnityEngine.UI;
using BoardOfEducation.Navigation;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Manages a standalone level-map scene. Shows the map and a GO button
    /// that loads the next lesson scene.
    /// </summary>
    public class LevelMapManager : MonoBehaviour
    {
        [SerializeField] private Button goButton;
        [SerializeField] private string nextScene = "TotalFractions2DemoWithBG";

        private void Start()
        {
            if (goButton != null)
                goButton.onClick.AddListener(OnGoClicked);
        }

        private void OnGoClicked()
        {
            NavigationHelper.LoadScene(nextScene);
        }
    }
}
