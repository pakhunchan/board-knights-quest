using UnityEngine;
using TMPro;
using System;
using System.Collections;

namespace BoardOfEducation.Lessons
{
    /// <summary>
    /// Shared step runner that barrier-syncs subtitle, animation, and (future) TTS.
    /// Each demo manager owns its animations; this component owns the subtitle display
    /// and the sync logic. Attach to any demo scene that needs sequenced narration.
    ///
    /// Sync guarantees:
    ///   - Step N+1 never starts until ALL systems finish step N
    ///   - If TTS is unavailable, subtitle uses estimated word-count timing
    ///   - A single currentStep index is owned here; no system can independently advance it
    /// </summary>
    public class LessonSequencer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI subtitleText;

        /// <summary>Current step index, read-only for external consumers.</summary>
        public int CurrentStep { get; private set; }

        // ── TTS Integration Point ──────────────────────────────────
        // When TTS is added, set this delegate. The sequencer will call it with the
        // step's subtitle text and wait for it to complete before advancing.
        // Word-boundary callbacks from TTS can drive karaoke highlighting via
        // OnTTSWordBoundary (future enhancement, Option 2 from the plan).

        /// <summary>
        /// Optional TTS provider. Return a coroutine that speaks the text and invokes
        /// onComplete when finished. If null, TTS is skipped and subtitle uses
        /// estimated duration as its timing source.
        /// </summary>
        public Func<string, Action, IEnumerator> TTSProvider { get; set; }

        // ── Public API ─────────────────────────────────────────────

        /// <summary>
        /// Initialize the subtitle display at the start of a sequence.
        /// </summary>
        public void Begin()
        {
            CurrentStep = 0;
            if (subtitleText != null)
            {
                subtitleText.text = "";
                subtitleText.alpha = 0f;
            }
        }

        /// <summary>
        /// Runs a single step with barrier sync across all active systems.
        /// Animation, subtitle, and TTS all run in parallel; the step doesn't
        /// advance until every system signals completion.
        /// </summary>
        /// <param name="step">The lesson step data (subtitle text + timing).</param>
        /// <param name="animation">
        /// Optional animation coroutine factory. Receives an Action to invoke on
        /// completion. Null = no animation for this step.
        /// </param>
        /// <param name="pauseAfter">Seconds to pause after all systems finish (default 0.5s).</param>
        public IEnumerator RunStep(LessonStep step, Func<Action, IEnumerator> animation, float pauseAfter = 0.5f)
        {
            bool animDone = false;
            bool subDone = false;
            bool ttsDone = false;

            // ── System 1: Animation ──
            if (animation != null)
                StartCoroutine(animation(() => animDone = true));
            else
                animDone = true;

            // ── System 2: Subtitle (karaoke word highlighting) ──
            float subtitleDuration = step.EstimatedDuration;
            // When TTS is active, TTS duration will override this via word-boundary
            // callbacks (Option 2, future). For now, estimated duration is the fallback.
            StartCoroutine(CoShowSubtitle(step.subtitle, subtitleDuration, () => subDone = true));

            // ── System 3: TTS (future — slot ready) ──
            if (TTSProvider != null)
                StartCoroutine(TTSProvider(step.subtitle, () => ttsDone = true));
            else
                ttsDone = true;

            // ── Barrier sync: wait for ALL systems ──
            yield return new WaitUntil(() => animDone && subDone && ttsDone);
            CurrentStep++;

            if (pauseAfter > 0f)
                yield return new WaitForSeconds(pauseAfter);
        }

        /// <summary>
        /// Clean up subtitle display at the end of a sequence.
        /// </summary>
        public void End()
        {
            if (subtitleText != null)
            {
                subtitleText.alpha = 0f;
                subtitleText.text = "";
            }
        }

        // ── Karaoke Subtitle ───────────────────────────────────────

        /// <summary>
        /// Displays subtitle text with karaoke-style word highlighting.
        /// Fades in, highlights each word in sequence, then fades out.
        /// </summary>
        public IEnumerator CoShowSubtitle(string text, float duration, Action onComplete)
        {
            string[] words = text.Split(' ');
            float perWord = duration / words.Length;

            subtitleText.text = text;

            // Fade in
            float fadeTime = 0.25f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 1f;

            // Karaoke: highlight one word at a time
            for (int i = 0; i < words.Length; i++)
            {
                var sb = new System.Text.StringBuilder();
                for (int w = 0; w < words.Length; w++)
                {
                    if (w > 0) sb.Append(' ');
                    if (w == i)
                        sb.Append("<color=#FF3333>").Append(words[w]).Append("</color>");
                    else
                        sb.Append(words[w]);
                }
                subtitleText.text = sb.ToString();
                yield return new WaitForSeconds(perWord);
            }

            // Restore plain text briefly
            subtitleText.text = text;
            yield return new WaitForSeconds(0.15f);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 0f;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Karaoke subtitle with per-word callbacks (for demos that trigger
        /// animations on specific words, like the handwriting effect).
        /// </summary>
        public IEnumerator CoShowKaraokeSubtitle(
            string text, float wordsPerSecond,
            Action<int, string, float> onWordStart,
            Action onComplete)
        {
            string[] words = text.Split(' ');

            // Compute per-word durations proportional to character length
            int totalChars = 0;
            foreach (var w in words) totalChars += w.Length;
            float totalDuration = words.Length / wordsPerSecond;

            float[] durations = new float[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                float proportion = (float)words[i].Length / totalChars;
                durations[i] = Mathf.Max(0.2f, proportion * totalDuration);
            }

            subtitleText.text = text;

            // Fade in
            float fadeTime = 0.25f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 1f;

            // Walk through words
            for (int i = 0; i < words.Length; i++)
            {
                var sb = new System.Text.StringBuilder();
                for (int w = 0; w < words.Length; w++)
                {
                    if (w > 0) sb.Append(' ');
                    if (w == i)
                        sb.Append("<color=#FF3333>").Append(words[w]).Append("</color>");
                    else
                        sb.Append(words[w]);
                }
                subtitleText.text = sb.ToString();

                onWordStart?.Invoke(i, words[i], durations[i]);
                yield return new WaitForSeconds(durations[i]);
            }

            // Restore and fade out
            subtitleText.text = text;
            yield return new WaitForSeconds(0.3f);

            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                subtitleText.alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            subtitleText.alpha = 0f;

            onComplete?.Invoke();
        }
    }
}
