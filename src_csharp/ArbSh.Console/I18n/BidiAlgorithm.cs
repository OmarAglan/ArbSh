using System;
using System.Collections.Generic; // For List<BidiRun> later
using System.Text; // For StringBuilder later
using ICU4N;
using ICU4N.Globalization;
using ICU4N.Text; // For UCharacter, UProperty, UCharacterDirection

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Provides functionality related to the Unicode Bidirectional Algorithm (UAX #9).
    /// </summary>
    public static class BidiAlgorithm
    {
        // Corresponds to MAX_DEPTH in UAX #9 (e.g., 125)
        private const int MaxEmbeddingDepth = 125;

        /// <summary>
        /// Determines the bidirectional character type (Bidi_Class) for a given Unicode codepoint using ICU.
        /// Aligns with UAX #9 Table 4 Bidirectional Character Types.
        /// </summary>
        /// <param name="codepoint">The Unicode codepoint.</param>
        /// <returns>The BidiCharacterType.</returns>
        public static BidiCharacterType GetCharType(int codepoint)
        {
            // --- UAX #9 Bidi_Class Override for LRM/RLM ---
            // LRM (U+200E) and RLM (U+200F) have a Bidi_Class of BN.
            // ICU's GetInt32PropertyValue(UProperty.Bidi_Class) for LRM returns the int for UCharacterDirection.LeftToRight,
            // and for RLM returns UCharacterDirection.RightToLeft.
            // To ensure our GetCharType returns BN for them (as per their UCD Bidi_Class), we pre-check.
            if (codepoint == 0x200E || codepoint == 0x200F)
            {
                return BidiCharacterType.BN;
            }

            int icuBidiClassValue = UChar.GetIntPropertyValue(codepoint, UProperty.Bidi_Class);

            // Cast the integer to ICU4N's UCharacterDirection enum for use in the switch.
            // This makes the switch statement more readable and type-safe.
            UCharacterDirection icuDirection = (UCharacterDirection)icuBidiClassValue;

            switch (icuDirection)
            {
                // Strong Types
                case UCharacterDirection.LeftToRight:
                    return BidiCharacterType.L;
                case UCharacterDirection.RightToLeft:
                    return BidiCharacterType.R;
                case UCharacterDirection.RightToLeftArabic: // AL in UAX #9
                    return BidiCharacterType.AL;

                // Weak Types
                case UCharacterDirection.EuropeanNumber:
                    return BidiCharacterType.EN;
                case UCharacterDirection.EuropeanNumberSeparator:
                    return BidiCharacterType.ES;
                case UCharacterDirection.EuropeanNumberTerminator:
                    return BidiCharacterType.ET;
                case UCharacterDirection.ArabicNumber:
                    return BidiCharacterType.AN;
                case UCharacterDirection.CommonNumberSeparator:
                    return BidiCharacterType.CS;
                case UCharacterDirection.DirNonSpacingMark: // ICU's name for NSM
                    return BidiCharacterType.NSM;

                // Neutral Types
                case UCharacterDirection.BlockSeparator: // B in UAX #9 (Paragraph Separator)
                    return BidiCharacterType.B;
                case UCharacterDirection.SegmentSeparator: // S in UAX #9
                    return BidiCharacterType.S;
                case UCharacterDirection.WhiteSpaceNeutral: // WS in UAX #9
                    return BidiCharacterType.WS;
                case UCharacterDirection.OtherNeutral: // ON in UAX #9
                    return BidiCharacterType.ON;

                // LRM and RLM are Boundary Neutrals.
                // Other control chars (like most C0 controls except TAB, LF, CR) are also BN.
                case UCharacterDirection.BoundaryNeutral:
                    // LRM/RLM were handled by the pre-check. This case handles other BNs.
                    return BidiCharacterType.BN;

                // Explicit Formatting Codes (UAX #9 X1-X8, X10)
                case UCharacterDirection.LeftToRightEmbedding:
                    return BidiCharacterType.LRE;
                case UCharacterDirection.RightToLeftEmbedding:
                    return BidiCharacterType.RLE;
                case UCharacterDirection.PopDirectionalFormat:
                    return BidiCharacterType.PDF;
                case UCharacterDirection.LeftToRightOverride:
                    return BidiCharacterType.LRO;
                case UCharacterDirection.RightToLeftOverride:
                    return BidiCharacterType.RLO;
                case UCharacterDirection.LeftToRightIsolate:
                    return BidiCharacterType.LRI;
                case UCharacterDirection.RightToLeftIsolate:
                    return BidiCharacterType.RLI;
                case UCharacterDirection.FirstStrongIsolate:
                    return BidiCharacterType.FSI;
                case UCharacterDirection.PopDirectionalIsolate:
                    return BidiCharacterType.PDI;

                // Note: LRM and RLM are not explicit members of UCharacterDirection.
                // UCharacter.GetInt32PropertyValue(0x200E, UProperty.Bidi_Class) returns the int for BoundaryNeutral.
                // UCharacter.GetInt32PropertyValue(0x200F, UProperty.Bidi_Class) returns the int for BoundaryNeutral.
                // So they are correctly handled by the BoundaryNeutral case above.

                default:
                    // This case should ideally not be hit if all UCharacterDirection values
                    // that can be returned by UProperty.Bidi_Class are mapped.
                    // Log a warning if an unhandled value is encountered.
                    System.Console.Error.WriteLine($"Warning: Unhandled ICU Bidi_Class value {icuBidiClassValue} (enum: {icuDirection}) for codepoint U+{codepoint:X4}. Defaulting to ON.");
                    return BidiCharacterType.ON; // Fallback to OtherNeutral for safety
            }
        }

        /// <summary>
        /// Represents a sequence of characters with the same resolved embedding level.
        /// </summary>
        public class BidiRun // Make public if tests are in a different assembly, or use InternalsVisibleTo
        {
            public int Start { get; }  // Start position in original text (char index)
            public int Length { get; } // Length of the run in original text (char count)
            public int Level { get; set; }  // Resolved embedding level (can be modified by algorithm steps)

            public BidiRun(int start, int length, int level)
            {
                Start = start;
                Length = length;
                Level = level;
            }

            public override string ToString() => $"Run(Start:{Start}, Len:{Length}, Lvl:{Level})";
        }


        // Placeholder for ProcessRuns - THIS WILL NEED SIGNIFICANT REWORK for UAX #9 X,W,N,I rules
        public static List<BidiRun> ProcessRuns(string text, int baseLevel)
        {
            var runs = new List<BidiRun>();
            if (string.IsNullOrEmpty(text))
            {
                return runs;
            }

            // TODO: THIS IS THE CRITICAL PART TO RE-IMPLEMENT based on UAX #9 X, W, N, I rules.
            // The current logic from the C port is highly simplified and incorrect for full BiDi.
            // For now, to make it compile and allow GetCharType testing, we can return a single run.
            // This is NOT a functional ProcessRuns for BiDi.

            System.Console.Error.WriteLine("WARNING: BidiAlgorithm.ProcessRuns is NOT YET UAX #9 COMPLIANT and uses placeholder logic.");

            // Placeholder: create a single run for the whole text with the baseLevel.
            // This is just to allow GetCharType to be tested in isolation without ProcessRuns fully working.
            if (text.Length > 0)
            {
                // Determine paragraph level P2, P3 if baseLevel is, say, -1 (auto)
                int paragraphLevel = baseLevel;
                if (paragraphLevel < 0 || paragraphLevel > 1) // Auto-detect if baseLevel is invalid/sentinel
                {
                    paragraphLevel = 0; // Default to LTR
                    for (int i = 0; i < text.Length;)
                    {
                        int codepoint = char.ConvertToUtf32(text, i); // Handles surrogates
                        BidiCharacterType charType = GetCharType(codepoint); // Use our new GetCharType
                        if (charType == BidiCharacterType.L) { paragraphLevel = 0; break; }
                        if (charType == BidiCharacterType.AL || charType == BidiCharacterType.R) { paragraphLevel = 1; break; }
                        i += char.IsSurrogatePair(text, i) ? 2 : 1;
                    }
                }
                runs.Add(new BidiRun(0, text.Length, paragraphLevel));
            }
            return runs;
        }

        // Placeholder for ReorderRunsForDisplay - THIS WILL BE CALLED WITH ACCURATE RUNS LATER
        public static string ReorderRunsForDisplay(string originalText, List<BidiRun> runs)
        {
            if (string.IsNullOrEmpty(originalText) || runs == null || runs.Count == 0)
            {
                return originalText ?? string.Empty;
            }

            System.Console.Error.WriteLine("WARNING: BidiAlgorithm.ReorderRunsForDisplay is using UAX #9 L2 logic but depends on ACCURATE runs from ProcessRuns.");

            var outputBuilder = new System.Text.StringBuilder(originalText.Length);
            int maxLevel = 0;
            foreach (var run in runs) { if (run.Level > maxLevel) maxLevel = run.Level; }

            for (int level = maxLevel; level >= 0; level--)
            {
                foreach (var run in runs)
                {
                    if (run.Level == level)
                    {
                        string runText = originalText.Substring(run.Start, run.Length);
                        if (level % 2 != 0) // Odd level -> RTL run
                        {
                            // Iterate backwards by text element (grapheme cluster)
                            // StringInfo.ParseCombiningCharacters helps get grapheme boundaries
                            var si = new System.Globalization.StringInfo(runText);
                            for (int j = si.LengthInTextElements - 1; j >= 0; j--)
                            {
                                string textElement = si.SubstringByTextElements(j, 1);
                                // Filter explicit formatting codes based on their Bidi_Class (now BN)
                                int firstCodepoint = char.ConvertToUtf32(textElement, 0);
                                BidiCharacterType charType = GetCharType(firstCodepoint);
                                if (charType != BidiCharacterType.LRE && charType != BidiCharacterType.RLE &&
                                    charType != BidiCharacterType.PDF && charType != BidiCharacterType.LRO &&
                                    charType != BidiCharacterType.RLO && charType != BidiCharacterType.LRI &&
                                    charType != BidiCharacterType.RLI && charType != BidiCharacterType.FSI &&
                                    charType != BidiCharacterType.PDI)
                                {
                                    outputBuilder.Append(textElement);
                                }
                            }
                        }
                        else // Even level -> LTR run
                        {
                            var si = new System.Globalization.StringInfo(runText);
                            for (int j = 0; j < si.LengthInTextElements; j++)
                            {
                                string textElement = si.SubstringByTextElements(j, 1);
                                int firstCodepoint = char.ConvertToUtf32(textElement, 0);
                                BidiCharacterType charType = GetCharType(firstCodepoint);
                                if (charType != BidiCharacterType.LRE && charType != BidiCharacterType.RLE &&
                                    charType != BidiCharacterType.PDF && charType != BidiCharacterType.LRO &&
                                    charType != BidiCharacterType.RLO && charType != BidiCharacterType.LRI &&
                                    charType != BidiCharacterType.RLI && charType != BidiCharacterType.FSI &&
                                    charType != BidiCharacterType.PDI)
                                {
                                    outputBuilder.Append(textElement);
                                }
                            }
                        }
                    }
                }
            }
            return outputBuilder.ToString();
        }

        // Placeholder for ProcessString
        public static string ProcessString(string text, int baseLevel)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            List<BidiRun> runs = ProcessRuns(text, baseLevel);
            return ReorderRunsForDisplay(text, runs);
        }
    }
}