namespace BoardOfEducation.Lessons
{
    [System.Serializable]
    public class PracticeQuestion
    {
        public string leftNum, leftDen;   // e.g. "1", "2"
        public string rightDen;           // e.g. "6"
        public string[] choices;          // e.g. {"3","4","5"}
        public int correctIndex;          // index into choices
        public string subtitle;           // karaoke text
        public string CorrectAnswer => choices[correctIndex];
    }
}
