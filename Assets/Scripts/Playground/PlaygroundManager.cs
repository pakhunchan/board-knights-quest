using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BoardOfEducation.Playground
{
    /// <summary>
    /// Playground runtime: manages Hub ↔ Features1 screen toggle and back navigation.
    /// </summary>
    public class PlaygroundManager : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject hubScreen;
        [SerializeField] private GameObject features1Screen;

        [Header("Hub")]
        [SerializeField] private Button backToLandingButton;
        [SerializeField] private Button features1CardButton;

        [Header("Features1")]
        [SerializeField] private Button backToHubButton;

        private void Start()
        {
            if (backToLandingButton != null)
                backToLandingButton.onClick.AddListener(OnBackToLanding);
            if (features1CardButton != null)
                features1CardButton.onClick.AddListener(OnFeatures1Clicked);
            if (backToHubButton != null)
                backToHubButton.onClick.AddListener(OnBackToHub);

            ShowHub();
        }

        private void ShowHub()
        {
            if (hubScreen != null) hubScreen.SetActive(true);
            if (features1Screen != null) features1Screen.SetActive(false);
        }

        private void ShowFeatures1()
        {
            if (hubScreen != null) hubScreen.SetActive(false);
            if (features1Screen != null) features1Screen.SetActive(true);
        }

        private void OnBackToLanding()
        {
            Navigation.NavigationHelper.LoadScene("LandingPage");
        }

        private void OnFeatures1Clicked()
        {
            ShowFeatures1();
        }

        private void OnBackToHub()
        {
            ShowHub();
        }
    }
}
