using System.Collections.Generic;

namespace BoardOfEducation.Game
{
    public enum CircleType
    {
        Number,
        Multiply,
        Divide,
        Add,
        Subtract
    }

    [System.Serializable]
    public class CircleData
    {
        public float Value;
        public CircleType Type;
        public string DisplayText;

        public CircleData(float value, CircleType type, string displayText)
        {
            Value = value;
            Type = type;
            DisplayText = displayText;
        }
    }

    /// <summary>
    /// Result of combining two circles.
    /// </summary>
    public struct CombineResult
    {
        public bool IsValid;
        public bool IsNullified;   // result == 0, both circles disappear
        public float ResultValue;
        public string ResultDisplay;
    }

    [System.Serializable]
    public class CirclesLevel
    {
        public int LevelNumber;
        public string Title;
        public List<CircleData> Circles;
        public int Par;

        public CirclesLevel(int num, string title, List<CircleData> circles, int par)
        {
            LevelNumber = num;
            Title = title;
            Circles = circles;
            Par = par;
        }

        /// <summary>
        /// Evaluate combining source onto target.
        /// Number + Number = add values (zero if 0).
        /// Number + Operation = apply operation to number.
        /// Operation + Operation = invalid.
        /// </summary>
        public static CombineResult EvaluateCombine(CircleData source, CircleData target)
        {
            bool sourceIsOp = source.Type != CircleType.Number;
            bool targetIsOp = target.Type != CircleType.Number;

            // Operation + Operation → invalid
            if (sourceIsOp && targetIsOp)
                return new CombineResult { IsValid = false };

            // Number + Number → add
            if (!sourceIsOp && !targetIsOp)
            {
                float result = source.Value + target.Value;
                bool nullified = UnityEngine.Mathf.Approximately(result, 0f);
                return new CombineResult
                {
                    IsValid = true,
                    IsNullified = nullified,
                    ResultValue = result,
                    ResultDisplay = FormatNumber(result)
                };
            }

            // One is number, one is operation — figure out which is which
            CircleData number = sourceIsOp ? target : source;
            CircleData op = sourceIsOp ? source : target;

            float val = number.Value;
            float opVal = op.Value;

            switch (op.Type)
            {
                case CircleType.Multiply:
                    val *= opVal;
                    break;
                case CircleType.Divide:
                    if (UnityEngine.Mathf.Approximately(opVal, 0f))
                        return new CombineResult { IsValid = false };
                    val /= opVal;
                    break;
                case CircleType.Add:
                    val += opVal;
                    break;
                case CircleType.Subtract:
                    val -= opVal;
                    break;
            }

            bool isNull = UnityEngine.Mathf.Approximately(val, 0f);
            return new CombineResult
            {
                IsValid = true,
                IsNullified = isNull,
                ResultValue = val,
                ResultDisplay = FormatNumber(val)
            };
        }

        public static string FormatNumber(float v)
        {
            if (UnityEngine.Mathf.Approximately(v, 0f)) return "0";
            if (UnityEngine.Mathf.Approximately(v, UnityEngine.Mathf.Round(v)))
                return ((int)UnityEngine.Mathf.Round(v)).ToString();
            return v.ToString("0.##");
        }

        // ── Level Bank ──────────────────────────────────────────

        private static List<CirclesLevel> _levels;

        public static List<CirclesLevel> AllLevels
        {
            get
            {
                if (_levels == null) _levels = BuildLevels();
                return _levels;
            }
        }

        private static CircleData Num(float v)
        {
            string sign = v >= 0 ? "+" : "";
            return new CircleData(v, CircleType.Number, sign + FormatNumber(v));
        }

        private static CircleData Mul(float v)
        {
            return new CircleData(v, CircleType.Multiply, "\u00d7" + FormatNumber(v));
        }

        private static CircleData Div(float v)
        {
            return new CircleData(v, CircleType.Divide, "\u00f7" + FormatNumber(v));
        }

        private static List<CirclesLevel> BuildLevels()
        {
            return new List<CirclesLevel>
            {
                // 1: Simple opposites
                new CirclesLevel(1, "Opposites", new List<CircleData>
                    { Num(3), Num(-3) }, 1),

                // 2: Chain addition
                new CirclesLevel(2, "Chain", new List<CircleData>
                    { Num(5), Num(-2), Num(-3) }, 2),

                // 3: Two pairs
                new CirclesLevel(3, "Two Pairs", new List<CircleData>
                    { Num(4), Num(-4), Num(2), Num(-2) }, 2),

                // 4: Split negative
                new CirclesLevel(4, "Split", new List<CircleData>
                    { Num(6), Num(-3), Num(-3) }, 2),

                // 5: Build then cancel
                new CirclesLevel(5, "Build Up", new List<CircleData>
                    { Num(1), Num(2), Num(-3) }, 2),

                // 6: Multiple paths
                new CirclesLevel(6, "Paths", new List<CircleData>
                    { Num(3), Num(-1), Num(2), Num(-4) }, 3),

                // 7: Intro multiply
                new CirclesLevel(7, "Multiply", new List<CircleData>
                    { Num(2), Mul(2), Num(-4) }, 2),

                // 8: Negate then cancel
                new CirclesLevel(8, "Negate", new List<CircleData>
                    { Num(-3), Mul(-1), Num(-3) }, 2),

                // 9: Intro divide
                new CirclesLevel(9, "Divide", new List<CircleData>
                    { Num(6), Div(2), Num(-3) }, 2),

                // 10: Mixed ops
                new CirclesLevel(10, "Mixed", new List<CircleData>
                    { Num(4), Div(2), Num(1), Num(-3) }, 3),

                // 11: Multiply then chain
                new CirclesLevel(11, "Multiply Chain", new List<CircleData>
                    { Num(2), Mul(3), Num(-3), Num(-3) }, 3),

                // 12: Divide then chain
                new CirclesLevel(12, "Divide Chain", new List<CircleData>
                    { Num(8), Div(4), Num(-1), Num(-1) }, 3),

                // 13: Negative multiply
                new CirclesLevel(13, "Negative Multiply", new List<CircleData>
                    { Num(-2), Mul(-2), Num(1), Num(-5) }, 3),

                // 14: Full chain
                new CirclesLevel(14, "Full Chain", new List<CircleData>
                    { Num(3), Mul(2), Div(3), Num(-2) }, 3),
            };
        }
    }
}
