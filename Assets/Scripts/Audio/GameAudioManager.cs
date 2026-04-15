using UnityEngine;
using System.Collections;

namespace BoardOfEducation.Audio
{
    /// <summary>
    /// Per-scene audio manager for BGM and SFX.
    /// Two AudioSources: one looping BGM, one for one-shot SFX.
    /// Not DontDestroyOnLoad — each scene/phase manages its own instance.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private AudioClip correctClip;

        private const float BGM_VOLUME = 0.3f;
        private const float BGM_DUCKED_VOLUME = 0.08f;

        private void Awake()
        {
            Instance = this;
            SetupSources();
            LoadClips();
        }

        private void SetupSources()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = BGM_VOLUME;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        private void LoadClips()
        {
            correctClip = Resources.Load<AudioClip>("SFX/correct");
            if (correctClip == null)
                Debug.LogWarning("[GameAudioManager] Could not load SFX/correct from Resources");
        }

        /// <summary>
        /// Loads and plays the BGM track on loop.
        /// Safe to call multiple times — skips if already playing.
        /// </summary>
        public void PlayBGM()
        {
            if (bgmSource.isPlaying) return;

            var clip = Resources.Load<AudioClip>("Music/sunlit-glade");
            if (clip == null)
            {
                Debug.LogWarning("[GameAudioManager] Could not load Music/sunlit-glade from Resources");
                return;
            }

            bgmSource.clip = clip;
            bgmSource.volume = BGM_VOLUME;
            bgmSource.Play();
        }

        /// <summary>
        /// Pause BGM playback. Call PlayBGM() to resume.
        /// </summary>
        public void PauseBGM()
        {
            bgmSource.Pause();
        }

        /// <summary>
        /// Resume BGM if it was paused. Keeps playback position.
        /// </summary>
        public void ResumeBGM()
        {
            bgmSource.UnPause();
            bgmSource.volume = BGM_VOLUME;
        }

        /// <summary>
        /// Immediately duck BGM volume for TTS narration.
        /// </summary>
        public void DuckBGM()
        {
            bgmSource.volume = BGM_DUCKED_VOLUME;
        }

        /// <summary>
        /// Fade BGM volume back to normal after TTS narration ends.
        /// </summary>
        public void UnduckBGM()
        {
            StartCoroutine(FadeVolume(bgmSource, bgmSource.volume, BGM_VOLUME, 0.5f));
        }

        /// <summary>
        /// Play the correct-answer chime as a one-shot SFX.
        /// Static for convenience — callers don't need a local reference.
        /// </summary>
        public static void PlayCorrectSFX()
        {
            if (Instance == null || Instance.correctClip == null) return;
            Instance.sfxSource.PlayOneShot(Instance.correctClip);
        }

        private IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            source.volume = to;
        }
    }
}
