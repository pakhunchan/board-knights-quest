namespace BoardOfEducation.Game
{
    /// <summary>
    /// Data passed to the Results1 scene. Caller sets <see cref="Pending"/>
    /// before loading the scene; Results1Manager reads and nulls it on Start.
    /// </summary>
    [System.Serializable]
    public class QuestResultsData
    {
        public int levelBefore;
        public int levelAfter;
        public string titleBefore;
        public string titleAfter;
        public int xpBefore;
        public int xpGained;
        public int xpToNextLevel;
        public int xpToNextLevelAfter; // XP required for the level after level-up
        public int mapStageBefore;
        public int mapStageAfter;
        public string nextSceneName;

        /// <summary>
        /// Set by the calling scene before LoadScene("Results1").
        /// Results1Manager reads and clears this on Start.
        /// </summary>
        public static QuestResultsData Pending;
    }
}
