namespace BoardOfEducation.Lessons
{
    /// <summary>
    /// Converts integers to English words for dynamic subtitle narration.
    /// Covers range 1–64 (max denominator in the question set).
    /// </summary>
    public static class NumberWords
    {
        private static readonly string[] Ones =
        {
            "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
            "seventeen", "eighteen", "nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "twenty", "thirty", "forty", "fifty", "sixty"
        };

        /// <summary>
        /// Returns cardinal form: "one", "two", ..., "sixty-four".
        /// </summary>
        public static string ToCardinal(int n)
        {
            if (n < 1 || n > 64) return n.ToString();
            if (n < 20) return Ones[n];
            int t = n / 10;
            int o = n % 10;
            if (o == 0) return Tens[t];
            return Tens[t] + "-" + Ones[o];
        }

        /// <summary>
        /// Returns denominator plural form: "halves", "thirds", "sixths", "sixty-fourths", etc.
        /// </summary>
        public static string ToDenominatorPlural(int n)
        {
            if (n == 2) return "halves";
            string ordinal = ToOrdinal(n);
            // Pluralize: add 's' (ordinals ending in "th" → "ths", "fth" → "fths", etc.)
            return ordinal + "s";
        }

        private static string ToOrdinal(int n)
        {
            // Special cases for single-digit ordinals
            switch (n)
            {
                case 1: return "first";
                case 2: return "second";
                case 3: return "third";
                case 4: return "fourth";
                case 5: return "fifth";
                case 8: return "eighth";
                case 9: return "ninth";
                case 12: return "twelfth";
            }

            if (n < 20)
            {
                // Teens and remaining single digits: strip trailing 'e' or 't' where needed
                string cardinal = Ones[n];
                if (cardinal.EndsWith("e"))
                    return cardinal.Substring(0, cardinal.Length - 1) + "th";
                return cardinal + "th";
            }

            // Compound numbers (20–64)
            int t = n / 10;
            int o = n % 10;
            if (o == 0)
            {
                // "twenty" → "twentieth", "thirty" → "thirtieth", etc.
                string tensWord = Tens[t];
                return tensWord.Substring(0, tensWord.Length - 1) + "ieth";
            }
            else
            {
                // "twenty-one" → "twenty-first", "forty-five" → "forty-fifth"
                return Tens[t] + "-" + ToOrdinal(o);
            }
        }
    }
}
