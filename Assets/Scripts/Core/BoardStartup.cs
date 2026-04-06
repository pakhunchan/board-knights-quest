using UnityEngine;
#if BOARD_SDK
using Board.Input;
#endif

namespace BoardOfEducation.Core
{
    /// <summary>
    /// Boot script that runs before anything else.
    /// Sets frame rate, configures Board SDK input, and validates setup.
    /// Attach to a GameObject in every scene or use RuntimeInitializeOnLoadMethod.
    /// </summary>
    public class BoardStartup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Board hardware runs at 60Hz — Unity defaults to 30 on Android
            Application.targetFrameRate = 60;

            // Prevent screen from sleeping during gameplay
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log("[BoardStartup] Target frame rate set to 60. Screen sleep disabled.");
        }

        private void Awake()
        {
            // Force landscape orientation (Board is always landscape)
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            #if BOARD_SDK
            // Enable debug overlay in development builds
            if (Debug.isDebugBuild)
            {
                BoardInput.enableDebugView = true;
                Debug.Log("[BoardStartup] Board SDK debug view enabled.");
            }
            #endif
        }

        private void Start()
        {
            ValidateSetup();
        }

        private void ValidateSetup()
        {
            if (Input.PieceManager.Instance == null)
            {
                Debug.LogError("[BoardStartup] PieceManager not found in scene!");
                return;
            }

            // These are optional — only present in the fraction game scene
            if (GameManager.Instance != null)
                Debug.Log("[BoardStartup] GameManager found.");
            if (Logging.InteractionLogger.Instance == null)
                Debug.Log("[BoardStartup] InteractionLogger not present (optional).");

            Debug.Log("[BoardStartup] Core systems initialized.");
        }
    }
}
