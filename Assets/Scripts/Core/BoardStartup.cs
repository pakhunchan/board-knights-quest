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
            bool valid = true;

            if (GameManager.Instance == null)
            {
                Debug.LogError("[BoardStartup] GameManager not found in scene!");
                valid = false;
            }

            if (Input.PieceManager.Instance == null)
            {
                Debug.LogError("[BoardStartup] PieceManager not found in scene!");
                valid = false;
            }

            if (Logging.InteractionLogger.Instance == null)
            {
                Debug.LogWarning("[BoardStartup] InteractionLogger not found — interactions will not be logged.");
            }

            if (Lessons.LessonController.Instance == null)
            {
                Debug.LogError("[BoardStartup] LessonController not found in scene!");
                valid = false;
            }

            if (valid)
                Debug.Log("[BoardStartup] All core systems initialized successfully.");
        }
    }
}
