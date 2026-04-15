using UnityEngine;
using System.Collections;
using BoardOfEducation.Audio;
using Board.Core;

namespace BoardOfEducation.Game
{
    /// <summary>
    /// Master orchestrator for the combined OfficialGame scene.
    /// Manages 7 phases using CanvasGroup crossfades instead of scene loading.
    /// Each phase's manager signals completion via its OnComplete callback.
    /// </summary>
    public class OfficialGameManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup phase1Group;
        [SerializeField] private CanvasGroup phase2Group;
        [SerializeField] private CanvasGroup phase3Group;
        [SerializeField] private CanvasGroup phase4Group;
        [SerializeField] private CanvasGroup phase5Group;
        [SerializeField] private CanvasGroup phase6Group;
        [SerializeField] private CanvasGroup phase7Group;

        [SerializeField] private Intro2Manager intro2Manager;
        [SerializeField] private Intro3Manager intro3Manager;
        [SerializeField] private ChalkboardDemoManager chalkboardManager;
        [SerializeField] private TotalFractions2DemoWithBGManager lessonOrchestrator;
        [SerializeField] private TotalFractions2Manager lessonManager;
        [SerializeField] private FractionsDemo5Manager practiceManager;
        [SerializeField] private Results1Manager results1Manager;
        [SerializeField] private Outro1Manager outro1Manager;

        private const float CROSSFADE_DURATION = 0.6f;
        private CanvasGroup[] phases;
        private int currentPhase;
        private bool transitioning;

        // Phase indices
        private const int PHASE_INTRO2 = 0;
        private const int PHASE_INTRO3 = 1;
        private const int PHASE_LESSON = 2;
        private const int PHASE_PRACTICE = 3;
        private const int PHASE_RESULTS = 4;
        private const int PHASE_OUTRO = 5;
        private const int PHASE_LEVELMAP = 6;

        private void Start()
        {
            phases = new[] { phase1Group, phase2Group, phase3Group, phase4Group, phase5Group, phase6Group, phase7Group };

            // Show phase 1, hide all others
            ShowPhase(0);
            for (int i = 1; i < phases.Length; i++)
                HidePhase(i);

            GameAudioManager.Instance?.PlayBGM();

            // Wire OnComplete callbacks
            intro2Manager.OnComplete = () => TransitionToPhase(PHASE_INTRO3);
            intro3Manager.OnComplete = () => TransitionToPhase(PHASE_LESSON);
            lessonManager.OnComplete = () => TransitionToPhase(PHASE_PRACTICE);
            practiceManager.OnComplete = () => TransitionToPhase(PHASE_RESULTS);
            results1Manager.OnComplete = () => TransitionToPhase(PHASE_OUTRO);
            outro1Manager.OnComplete = () => TransitionToPhase(PHASE_LEVELMAP);

            BoardApplication.pauseScreenActionReceived += OnPauseScreenAction;
        }

        private void OnDestroy()
        {
            BoardApplication.pauseScreenActionReceived -= OnPauseScreenAction;
        }

        private void OnPauseScreenAction(BoardPauseAction pauseAction, BoardPauseAudioTrack[] audioTracks)
        {
            if (pauseAction == BoardPauseAction.ExitGameSaved || pauseAction == BoardPauseAction.ExitGameUnsaved)
            {
                BoardApplication.Exit();
            }
        }

        private void TransitionToPhase(int nextPhase)
        {
            if (transitioning || nextPhase < 0 || nextPhase >= phases.Length) return;
            StartCoroutine(CoCrossfade(currentPhase, nextPhase));
        }

        private IEnumerator CoCrossfade(int fromIdx, int toIdx)
        {
            transitioning = true;
            var from = phases[fromIdx];
            var to = phases[toIdx];

            // Activate the target phase so Start() fires on its managers
            to.gameObject.SetActive(true);

            // Enable managers for the target phase
            EnablePhaseManagers(toIdx);

            // Pause BGM during lesson/practice phases, resume when leaving
            bool enteringQuiet = toIdx == PHASE_LESSON || toIdx == PHASE_PRACTICE;
            bool leavingQuiet = fromIdx == PHASE_LESSON || fromIdx == PHASE_PRACTICE;
            if (enteringQuiet && !leavingQuiet)
                GameAudioManager.Instance?.PauseBGM();
            else if (leavingQuiet && !enteringQuiet)
                GameAudioManager.Instance?.ResumeBGM();

            from.blocksRaycasts = false;
            to.alpha = 0f;
            to.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < CROSSFADE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / CROSSFADE_DURATION);
                from.alpha = 1f - t;
                to.alpha = t;
                yield return null;
            }

            from.alpha = 0f;
            to.alpha = 1f;
            to.blocksRaycasts = true;

            // Deactivate old phase to save memory
            from.gameObject.SetActive(false);

            currentPhase = toIdx;
            transitioning = false;
        }

        private void ShowPhase(int idx)
        {
            var cg = phases[idx];
            cg.gameObject.SetActive(true);
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }

        private void HidePhase(int idx)
        {
            var cg = phases[idx];
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
        }

        private void EnablePhaseManagers(int phaseIdx)
        {
            switch (phaseIdx)
            {
                case PHASE_INTRO3:
                    intro3Manager.enabled = true;
                    break;
                case PHASE_LESSON:
                    chalkboardManager.enabled = true;
                    lessonOrchestrator.enabled = true;
                    break;
                case PHASE_PRACTICE:
                    practiceManager.enabled = true;
                    break;
                case PHASE_RESULTS:
                    results1Manager.enabled = true;
                    break;
                case PHASE_OUTRO:
                    outro1Manager.enabled = true;
                    break;
            }
        }
    }
}
