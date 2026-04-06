using UnityEngine;
using System;
using System.Collections.Generic;
using BoardOfEducation.Core;

namespace BoardOfEducation.Progression
{
    /// <summary>
    /// Long-term progression system: "Fraction Explorer" journey map.
    /// Players travel through themed math lands, unlocking new areas
    /// as they master fraction concepts. Designed to extend beyond
    /// the single-lesson scope of this prototype.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        [Serializable]
        public class MathLand
        {
            public string Name;
            public string Description;
            public string Theme; // visual theme identifier
            public int RequiredStars;
            public bool Unlocked;
            public int StarsEarned;
            public List<string> LessonTopics;
        }

        [Serializable]
        public class PlayerProfile
        {
            public string PlayerName;
            public int TotalStars;
            public int LessonsCompleted;
            public int CurrentLandIndex;
            public List<string> Achievements;
            public string LastPlayDate;
        }

        [Header("Progression Data")]
        [SerializeField] private List<MathLand> journeyMap;

        public PlayerProfile Profile { get; private set; }
        public IReadOnlyList<MathLand> JourneyMap => journeyMap;

        public event Action<int> OnStarsEarned;
        public event Action<MathLand> OnLandUnlocked;
        public event Action<string> OnAchievementUnlocked;

        private const string SAVE_KEY = "boe_progression";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeJourneyMap();
            LoadProgress();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
        }

        private void InitializeJourneyMap()
        {
            journeyMap = new List<MathLand>
            {
                new MathLand
                {
                    Name = "Halves Harbor",
                    Description = "Learn the basics of halves and wholes",
                    Theme = "ocean",
                    RequiredStars = 0,
                    Unlocked = true,
                    LessonTopics = new List<string> { "1/2", "2/2", "halves" }
                },
                new MathLand
                {
                    Name = "Quarter Canyon",
                    Description = "Explore quarters and their relationship to halves",
                    Theme = "desert",
                    RequiredStars = 3,
                    LessonTopics = new List<string> { "1/4", "2/4", "3/4", "4/4" }
                },
                new MathLand
                {
                    Name = "Third Falls",
                    Description = "Discover thirds and mixed fractions",
                    Theme = "waterfall",
                    RequiredStars = 8,
                    LessonTopics = new List<string> { "1/3", "2/3", "3/3" }
                },
                new MathLand
                {
                    Name = "Equivalence Peak",
                    Description = "Master fraction equivalence — the summit!",
                    Theme = "mountain",
                    RequiredStars = 15,
                    LessonTopics = new List<string> { "2/4=1/2", "3/6=1/2", "2/6=1/3" }
                },
                // Future lands (not implemented in prototype but shows extensibility)
                new MathLand
                {
                    Name = "Decimal Depths",
                    Description = "Connect fractions to decimals",
                    Theme = "underwater",
                    RequiredStars = 25,
                    LessonTopics = new List<string> { "0.5=1/2", "0.25=1/4", "0.1=1/10" }
                },
                new MathLand
                {
                    Name = "Operation Outpost",
                    Description = "Add and subtract fractions",
                    Theme = "space",
                    RequiredStars = 40,
                    LessonTopics = new List<string> { "1/4+1/4", "1/2-1/4", "mixed operations" }
                }
            };
        }

        private void OnGameStateChanged(GameManager.GameState state)
        {
            if (state == GameManager.GameState.LessonComplete)
            {
                CompleteLessonAndAwardStars();
            }
        }

        private void CompleteLessonAndAwardStars()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // Award stars based on performance
            int stars = CalculateStars(gm.TeamScore, (int)gm.CurrentScaffold);
            Profile.TotalStars += stars;
            Profile.LessonsCompleted++;
            Profile.LastPlayDate = System.DateTime.Now.ToString("yyyy-MM-dd");

            // Update current land
            if (Profile.CurrentLandIndex < journeyMap.Count)
            {
                journeyMap[Profile.CurrentLandIndex].StarsEarned += stars;
            }

            OnStarsEarned?.Invoke(stars);

            // Check for land unlocks
            CheckLandUnlocks();

            // Check achievements
            CheckAchievements();

            SaveProgress();
        }

        private int CalculateStars(int teamScore, int scaffoldReached)
        {
            // Base stars from score
            int stars = teamScore / 2;

            // Bonus for reaching higher scaffolds
            stars += scaffoldReached; // 0 for E1, 1 for E2, 2 for E3, 3 for E4

            // Bonus for completing E4 (mastery)
            if (scaffoldReached >= 3) stars += 2;

            return Mathf.Max(1, stars); // Always at least 1 star
        }

        private void CheckLandUnlocks()
        {
            for (int i = 0; i < journeyMap.Count; i++)
            {
                if (!journeyMap[i].Unlocked && Profile.TotalStars >= journeyMap[i].RequiredStars)
                {
                    journeyMap[i].Unlocked = true;
                    if (i > Profile.CurrentLandIndex)
                        Profile.CurrentLandIndex = i;

                    OnLandUnlocked?.Invoke(journeyMap[i]);
                }
            }
        }

        private void CheckAchievements()
        {
            if (Profile.Achievements == null)
                Profile.Achievements = new List<string>();

            void TryUnlock(string id, string name, bool condition)
            {
                if (condition && !Profile.Achievements.Contains(id))
                {
                    Profile.Achievements.Add(id);
                    OnAchievementUnlocked?.Invoke(name);
                }
            }

            TryUnlock("first_lesson", "First Steps", Profile.LessonsCompleted >= 1);
            TryUnlock("five_stars", "Star Collector", Profile.TotalStars >= 5);
            TryUnlock("ten_stars", "Rising Star", Profile.TotalStars >= 10);
            TryUnlock("quarter_canyon", "Canyon Explorer", journeyMap.Count > 1 && journeyMap[1].Unlocked);
            TryUnlock("three_lessons", "Dedicated Learner", Profile.LessonsCompleted >= 3);
            TryUnlock("equivalence", "Equivalence Master", journeyMap.Count > 3 && journeyMap[3].Unlocked);
        }

        private void SaveProgress()
        {
            string json = JsonUtility.ToJson(Profile);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                Profile = JsonUtility.FromJson<PlayerProfile>(json);
            }
            else
            {
                Profile = new PlayerProfile
                {
                    PlayerName = "Team",
                    TotalStars = 0,
                    LessonsCompleted = 0,
                    CurrentLandIndex = 0,
                    Achievements = new List<string>(),
                    LastPlayDate = ""
                };
            }
        }

        public MathLand GetCurrentLand()
        {
            if (Profile.CurrentLandIndex < journeyMap.Count)
                return journeyMap[Profile.CurrentLandIndex];
            return journeyMap[0];
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            LoadProgress();
            InitializeJourneyMap();
        }
    }
}
