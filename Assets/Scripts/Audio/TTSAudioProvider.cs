using UnityEngine;
using System;
using System.Collections;

namespace BoardOfEducation.Audio
{
    public class TTSAudioProvider : MonoBehaviour
    {
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public IEnumerator SpeakCoroutine(string text, Action onComplete)
        {
            string hash = TTSHashUtil.Hash(text);
            Debug.Log($"[TTS] SpeakCoroutine called: hash={hash} text=\"{text.Substring(0, Mathf.Min(text.Length, 40))}...\"");
            var clip = Resources.Load<AudioClip>("TTS/" + hash);

            if (clip == null)
            {
                Debug.LogWarning($"[TTS] Cache miss for hash={hash} text=\"{text}\"");
                onComplete?.Invoke();
                yield break;
            }

            // Audio data may not be preloaded — request load and wait
            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading)
                yield return null;
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"[TTS] Failed to load audio data for hash={hash}");
                onComplete?.Invoke();
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitForSeconds(clip.length);
            onComplete?.Invoke();
        }
    }
}
