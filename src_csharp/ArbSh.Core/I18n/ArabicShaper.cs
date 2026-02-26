using System;
using System.Collections.Generic;
using System.Text;

namespace ArbSh.Core.I18n
{
    /// <summary>
    /// يقوم بتشكيل الأحرف العربية (تحويل الأحرف من الصورة المنطقية إلى صور العرض المتصلة).
    /// Responsible for Arabic character shaping (converting logical characters to connected presentation forms).
    /// Implements a standalone shaping engine to avoid external dependencies.
    /// </summary>
    public static class ArabicShaper
    {
        // Define the 4 forms for each letter
        private class ArabicChar
        {
            public char Isolated;
            public char Final;
            public char Initial;
            public char Medial;
            public bool ConnectsToEnd; // Can connect to the next char (left in visual RTL)?

            public ArabicChar(char iso, char fin, char ini, char med, bool connectsToEnd)
            {
                Isolated = iso;
                Final = fin;
                Initial = ini;
                Medial = med;
                ConnectsToEnd = connectsToEnd;
            }
        }

        private static readonly Dictionary<char, ArabicChar> Map = new Dictionary<char, ArabicChar>();

        static ArabicShaper()
        {
            // Initialize the mapping table
            // Format: Logical Char -> Isolated, Final, Initial, Medial, ConnectsToEnd?
            
            // Hamza
            AddChar('\u0621', '\uFE80', '\uFE80', '\uFE80', '\uFE80', false);
            // Alef with Madda
            AddChar('\u0622', '\uFE81', '\uFE82', '\uFE81', '\uFE82', false);
            // Alef with Hamza Above
            AddChar('\u0623', '\uFE83', '\uFE84', '\uFE83', '\uFE84', false);
            // Waw with Hamza
            AddChar('\u0624', '\uFE85', '\uFE86', '\uFE85', '\uFE86', false);
            // Alef with Hamza Below
            AddChar('\u0625', '\uFE87', '\uFE88', '\uFE87', '\uFE88', false);
            // Yeh with Hamza
            AddChar('\u0626', '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C', true);
            // Alef
            AddChar('\u0627', '\uFE8D', '\uFE8E', '\uFE8D', '\uFE8E', false);
            // Beh
            AddChar('\u0628', '\uFE8F', '\uFE90', '\uFE91', '\uFE92', true);
            // Teh Marbuta
            AddChar('\u0629', '\uFE93', '\uFE94', '\uFE93', '\uFE94', false);
            // Teh
            AddChar('\u062A', '\uFE95', '\uFE96', '\uFE97', '\uFE98', true);
            // Theh
            AddChar('\u062B', '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C', true);
            // Jeem
            AddChar('\u062C', '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0', true);
            // Hah
            AddChar('\u062D', '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4', true);
            // Khah
            AddChar('\u062E', '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8', true);
            // Dal
            AddChar('\u062F', '\uFEA9', '\uFEAA', '\uFEA9', '\uFEAA', false);
            // Thal
            AddChar('\u0630', '\uFEAB', '\uFEAC', '\uFEAB', '\uFEAC', false);
            // Reh
            AddChar('\u0631', '\uFEAD', '\uFEAE', '\uFEAD', '\uFEAE', false);
            // Zain
            AddChar('\u0632', '\uFEAF', '\uFEB0', '\uFEAF', '\uFEB0', false);
            // Seen
            AddChar('\u0633', '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4', true);
            // Sheen
            AddChar('\u0634', '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8', true);
            // Sad
            AddChar('\u0635', '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC', true);
            // Dad
            AddChar('\u0636', '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0', true);
            // Tah
            AddChar('\u0637', '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4', true);
            // Zah
            AddChar('\u0638', '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8', true);
            // Ain
            AddChar('\u0639', '\uFEC9', '\uFECA', '\uFECB', '\uFECC', true);
            // Ghain
            AddChar('\u063A', '\uFECD', '\uFECE', '\uFECF', '\uFED0', true);
            // Fe
            AddChar('\u0641', '\uFED1', '\uFED2', '\uFED3', '\uFED4', true);
            // Qaf
            AddChar('\u0642', '\uFED5', '\uFED6', '\uFED7', '\uFED8', true);
            // Kaf
            AddChar('\u0643', '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC', true);
            // Lam
            AddChar('\u0644', '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0', true);
            // Meem
            AddChar('\u0645', '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4', true);
            // Noon
            AddChar('\u0646', '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8', true);
            // Heh
            AddChar('\u0647', '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC', true);
            // Waw
            AddChar('\u0648', '\uFEED', '\uFEEE', '\uFEED', '\uFEEE', false);
            // Alef Maksura
            AddChar('\u0649', '\uFEEF', '\uFEF0', '\uFEEF', '\uFEF0', false); // Like Alef/Dal, doesn't connect to end
            // Yeh
            AddChar('\u064A', '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4', true);
        }

        private static void AddChar(char c, char iso, char fin, char ini, char med, bool connects)
        {
            Map[c] = new ArabicChar(iso, fin, ini, med, connects);
        }

        /// <summary>
        /// يقوم بتشكيل النص العربي في السلسلة النصية المعطاة.
        /// Shapes the Arabic text in the given string, connecting letters based on context.
        /// </summary>
        public static string Shape(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            StringBuilder sb = new StringBuilder(text.Length);
            
            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];

                // Handle Lam-Alef Ligatures
                if (current == '\u0644' && i + 1 < text.Length)
                {
                    char next = text[i + 1];
                    char ligature = GetLamAlef(next);
                    if (ligature != '\0')
                    {
                        // Found a Lam-Alef combination
                        // Determine form based on previous char
                        bool prevConnects = CanConnectToPrevious(text, i);
                        
                        // Lam-Alef only has Isolated and Final forms (it ends with Alef)
                        // Example: LRM + LamAlef -> Isolated (U+FEFB)
                        //          Beh + LamAlef -> Final (U+FEFC)
                        
                        // We need the specific ligature char from the table
                        // But since we are simplifying, we can check the Unicode block
                        // Ligatures: 
                        // Lam-Alef Mad: FEF5 (Iso), FEF6 (Fin)
                        // Lam-Alef Hamza Above: FEF7 (Iso), FEF8 (Fin)
                        // Lam-Alef Hamza Below: FEF9 (Iso), FEFA (Fin)
                        // Lam-Alef: FEFB (Iso), FEFC (Fin)

                        char shapedLigature;
                        if (prevConnects)
                        {
                            // Final form
                            if (ligature == '\uFEFB') shapedLigature = '\uFEFC'; // Basic
                            else if (ligature == '\uFEF5') shapedLigature = '\uFEF6'; // Mad
                            else if (ligature == '\uFEF7') shapedLigature = '\uFEF8'; // Hamza Up
                            else if (ligature == '\uFEF9') shapedLigature = '\uFEFA'; // Hamza Down
                            else shapedLigature = ligature; // Fallback
                        }
                        else
                        {
                            // Isolated form (default returned by GetLamAlef)
                            shapedLigature = ligature;
                        }

                        sb.Append(shapedLigature);
                        i++; // Skip the Alef
                        continue;
                    }
                }

                if (Map.ContainsKey(current))
                {
                    bool prevConnects = CanConnectToPrevious(text, i);
                    bool nextConnects = CanConnectToNext(text, i);

                    ArabicChar info = Map[current];
                    char shapedChar;

                    if (!prevConnects && !nextConnects)
                    {
                        shapedChar = info.Isolated;
                    }
                    else if (prevConnects && !nextConnects)
                    {
                        shapedChar = info.Final;
                    }
                    else if (!prevConnects && nextConnects)
                    {
                        shapedChar = info.Initial;
                    }
                    else // prevConnects && nextConnects
                    {
                        shapedChar = info.Medial;
                    }

                    sb.Append(shapedChar);
                }
                else
                {
                    // Non-Arabic or not in our map (e.g. spaces, numbers, diacritics)
                    sb.Append(current);
                }
            }

            return sb.ToString();
        }

        private static bool CanConnectToPrevious(string text, int currentIndex)
        {
            if (currentIndex == 0) return false;
            char prev = text[currentIndex - 1];
            
            // Skip non-spacing marks (Harakat) if we want to support them (simplified: we don't skip yet)
            // Ideally we'd look back further if prev is a diacritic.

            if (Map.TryGetValue(prev, out ArabicChar? info))
            {
                // The previous char must be able to connect to its END (left)
                return info.ConnectsToEnd;
            }
            return false; // Prev char is not an Arabic letter (e.g. space) or doesn't connect
        }

        private static bool CanConnectToNext(string text, int currentIndex)
        {
            // First, does the CURRENT char support connecting to the end?
            char current = text[currentIndex];
            if (!Map.TryGetValue(current, out ArabicChar? currentInfo) || !currentInfo.ConnectsToEnd)
            {
                return false;
            }

            if (currentIndex >= text.Length - 1) return false;
            char next = text[currentIndex + 1];

            // Does the NEXT char exist in map? (All Arabic letters connect to their beginning/right)
            return Map.ContainsKey(next);
        }

        private static char GetLamAlef(char candidate)
        {
            // Return the ISOLATED form of the ligature
            switch (candidate)
            {
                case '\u0622': return '\uFEF5'; // Alef with Madda
                case '\u0623': return '\uFEF7'; // Alef with Hamza Above
                case '\u0625': return '\uFEF9'; // Alef with Hamza Below
                case '\u0627': return '\uFEFB'; // Alef
                default: return '\0';
            }
        }
    }
}
