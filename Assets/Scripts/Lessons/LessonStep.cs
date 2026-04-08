using UnityEngine;

namespace BoardOfEducation.Lessons
{
    /// <summary>
    /// Single source of truth for one step in a lesson sequence.
    /// Co-locates the subtitle text and the animation key so they can't drift apart.
    /// The same subtitle string will be used for TTS when that system is added.
    /// </summary>
    [System.Serializable]
    public class LessonStep
    {
        [Tooltip("Text displayed as subtitle and spoken by TTS (single source of truth).")]
        public string subtitle;

        [Tooltip("Key that resolves to an animation coroutine via the demo's registry. Null/empty = subtitle only.")]
        public string animationKey;

        /// <summary>
        /// Fallback duration when TTS is unavailable. Computed from word count.
        /// </summary>
        public float EstimatedDuration { get; private set; }

        public LessonStep(string subtitle, string animationKey = null)
        {
            this.subtitle = subtitle;
            this.animationKey = animationKey;
            int wordCount = subtitle.Split(' ').Length;
            EstimatedDuration = Mathf.Max(2f, wordCount * 0.3f);
        }
    }
}
