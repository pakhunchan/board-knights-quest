using System;
using System.Collections.Generic;
using BoardOfEducation.Input;

namespace BoardOfEducation.Lessons
{
    /// <summary>
    /// Represents a single fraction equivalence problem.
    /// Players place Board Arcade pieces (robots/ships) that represent fraction values.
    /// Robot Yellow=1/2, Robot Purple=1/4, Robot Orange=1/3,
    /// Robot Pink=3/4, Ship Pink=1, Ship Yellow=2/4, Ship Purple=2/3
    /// </summary>
    [Serializable]
    public class FractionProblem
    {
        public string Prompt;
        public int Numerator;
        public int Denominator;
        public int EquivalentNumerator;
        public int EquivalentDenominator;

        // Which glyph IDs are valid answers
        public List<int> AcceptedGlyphIds;

        // How many pieces need to be placed to solve
        public int RequiredBlockCount;

        // Instruction text varies by scaffold level
        public string E1_Instruction;
        public string E2_Hint;
        public string E3_Nudge;

        public static List<FractionProblem> CreateProblemBank()
        {
            // Piece-to-fraction mapping:
            // Robot Yellow (0) = 1/2
            // Robot Purple (1) = 1/4
            // Robot Orange (2) = 1/3
            // Robot Pink   (3) = 3/4
            // Ship Pink    (4) = 1 (whole)
            // Ship Yellow  (5) = 2/4
            // Ship Purple  (6) = 2/3

            return new List<FractionProblem>
            {
                // Level 1: Identify 1/2
                new FractionProblem
                {
                    Prompt = "Show 1/2",
                    Numerator = 1, Denominator = 2,
                    EquivalentNumerator = 1, EquivalentDenominator = 2,
                    AcceptedGlyphIds = new List<int> { 0 }, // Robot Yellow = 1/2
                    RequiredBlockCount = 1,
                    E1_Instruction = "A half means 1 out of 2 equal parts.\nThe YELLOW ROBOT represents 1/2.\nPlace it on the board!",
                    E2_Hint = "Which robot represents one half?",
                    E3_Nudge = "Find the half."
                },

                // Level 2: Identify 1/4
                new FractionProblem
                {
                    Prompt = "Show 1/4",
                    Numerator = 1, Denominator = 4,
                    EquivalentNumerator = 1, EquivalentDenominator = 4,
                    AcceptedGlyphIds = new List<int> { 1 }, // Robot Purple = 1/4
                    RequiredBlockCount = 1,
                    E1_Instruction = "A quarter means 1 out of 4 equal parts.\nThe PURPLE ROBOT represents 1/4.\nPlace it on the board!",
                    E2_Hint = "Which robot is one quarter?",
                    E3_Nudge = "One quarter."
                },

                // Level 3: Identify 1/3
                new FractionProblem
                {
                    Prompt = "Show 1/3",
                    Numerator = 1, Denominator = 3,
                    EquivalentNumerator = 1, EquivalentDenominator = 3,
                    AcceptedGlyphIds = new List<int> { 2 }, // Robot Orange = 1/3
                    RequiredBlockCount = 1,
                    E1_Instruction = "A third means 1 out of 3 equal parts.\nThe ORANGE ROBOT represents 1/3.\nPlace it on the board!",
                    E2_Hint = "Which robot is one third?",
                    E3_Nudge = "One third."
                },

                // Level 4: Show a whole
                new FractionProblem
                {
                    Prompt = "Show 1 whole",
                    Numerator = 1, Denominator = 1,
                    EquivalentNumerator = 1, EquivalentDenominator = 1,
                    AcceptedGlyphIds = new List<int> { 4 }, // Ship Pink = 1
                    RequiredBlockCount = 1,
                    E1_Instruction = "One whole means ALL the parts together.\nThe PINK SHIP represents 1 whole.\nPlace it on the board!",
                    E2_Hint = "Which ship is the whole?",
                    E3_Nudge = "The whole."
                },

                // Level 5: Equivalence — 2/4 = 1/2
                new FractionProblem
                {
                    Prompt = "Show that 2/4 = 1/2",
                    Numerator = 2, Denominator = 4,
                    EquivalentNumerator = 1, EquivalentDenominator = 2,
                    AcceptedGlyphIds = new List<int> { 0, 5 }, // Robot Yellow(1/2) OR Ship Yellow(2/4)
                    RequiredBlockCount = 2,
                    E1_Instruction = "2/4 is the SAME as 1/2!\nPlace BOTH the Yellow Robot (1/2) AND\nthe Yellow Ship (2/4) to prove they're equal!",
                    E2_Hint = "Place the two yellow pieces — they show the same amount!",
                    E3_Nudge = "Two yellows prove it."
                },

                // Level 6: Show 3/4
                new FractionProblem
                {
                    Prompt = "Show 3/4",
                    Numerator = 3, Denominator = 4,
                    EquivalentNumerator = 3, EquivalentDenominator = 4,
                    AcceptedGlyphIds = new List<int> { 3 }, // Robot Pink = 3/4
                    RequiredBlockCount = 1,
                    E1_Instruction = "Three quarters means 3 out of 4 parts.\nThe PINK ROBOT represents 3/4.\nPlace it on the board!",
                    E2_Hint = "Which robot is three quarters?",
                    E3_Nudge = "Three quarters."
                },

                // Level 7: Show 2/3
                new FractionProblem
                {
                    Prompt = "Show 2/3",
                    Numerator = 2, Denominator = 3,
                    EquivalentNumerator = 2, EquivalentDenominator = 3,
                    AcceptedGlyphIds = new List<int> { 6 }, // Ship Purple = 2/3
                    RequiredBlockCount = 1,
                    E1_Instruction = "Two thirds means 2 out of 3 parts.\nThe PURPLE SHIP represents 2/3.\nPlace it on the board!",
                    E2_Hint = "Which ship is two thirds?",
                    E3_Nudge = "Two thirds."
                },

                // Level 8: Build 1/2 + 1/4 = 3/4
                new FractionProblem
                {
                    Prompt = "Show 1/2 + 1/4 = 3/4",
                    Numerator = 3, Denominator = 4,
                    EquivalentNumerator = 3, EquivalentDenominator = 4,
                    AcceptedGlyphIds = new List<int> { 0, 1, 3 }, // Yellow Robot + Purple Robot OR Pink Robot
                    RequiredBlockCount = 2,
                    E1_Instruction = "Can you ADD fractions?\n1/2 + 1/4 = 3/4!\nPlace the Yellow Robot (1/2) and Purple Robot (1/4)\ntogether to make 3/4!",
                    E2_Hint = "Add the half and the quarter — what do you get?",
                    E3_Nudge = "Half plus quarter."
                },

                // Level 9: Match equivalent — which equals 1/2?
                new FractionProblem
                {
                    Prompt = "Find the piece that equals 1/2",
                    Numerator = 1, Denominator = 2,
                    EquivalentNumerator = 2, EquivalentDenominator = 4,
                    AcceptedGlyphIds = new List<int> { 0, 5 }, // Robot Yellow(1/2) or Ship Yellow(2/4)
                    RequiredBlockCount = 1,
                    E1_Instruction = "Which piece equals 1/2?\nRemember: 2/4 is the same as 1/2!\nEither the Yellow Robot OR Yellow Ship works!",
                    E2_Hint = "Two different pieces can show the same fraction!",
                    E3_Nudge = "Equivalent fraction."
                },

                // Level 10: Build a whole from parts
                new FractionProblem
                {
                    Prompt = "Build 1 whole from 1/2 + 1/2",
                    Numerator = 1, Denominator = 1,
                    EquivalentNumerator = 2, EquivalentDenominator = 2,
                    AcceptedGlyphIds = new List<int> { 0, 4 }, // Two Yellow Robots OR Ship Pink
                    RequiredBlockCount = 2,
                    E1_Instruction = "Two halves make a whole!\nPlace TWO Yellow Robots (each is 1/2)\nto show that 1/2 + 1/2 = 1!",
                    E2_Hint = "How many halves make a whole?",
                    E3_Nudge = "Two halves."
                },
            };
        }
    }
}
