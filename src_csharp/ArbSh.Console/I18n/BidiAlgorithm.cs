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


        /// <summary>
        /// Represents an entry on the directional status stack used in UAX #9 X rules.
        /// Each entry tracks the embedding level, directional override status, and isolate status.
        /// </summary>
        private struct DirectionalStatusStackEntry
        {
            public int EmbeddingLevel { get; }
            public DirectionalOverrideStatus OverrideStatus { get; }
            public bool IsolateStatus { get; }

            public DirectionalStatusStackEntry(int embeddingLevel, DirectionalOverrideStatus overrideStatus, bool isolateStatus)
            {
                EmbeddingLevel = embeddingLevel;
                OverrideStatus = overrideStatus;
                IsolateStatus = isolateStatus;
            }

            public override string ToString() => $"StackEntry(Level:{EmbeddingLevel}, Override:{OverrideStatus}, Isolate:{IsolateStatus})";
        }

        /// <summary>
        /// Represents the directional override status as defined in UAX #9.
        /// </summary>
        private enum DirectionalOverrideStatus
        {
            Neutral,        // No override is currently active
            LeftToRight,    // Characters are to be reset to L
            RightToLeft     // Characters are to be reset to R
        }



        /// <summary>
        /// ProcessRuns - Implements UAX #9 rules for resolving embedding levels.
        /// This is the main entry point for the bidirectional algorithm's level resolution phase.
        ///
        /// The algorithm proceeds through these phases:
        /// 1. P rules: Determine paragraph embedding level
        /// 2. X rules: Process explicit formatting characters and determine explicit levels
        /// 3. W rules: Resolve weak character types
        /// 4. N rules: Resolve neutral character types
        /// 5. I rules: Resolve implicit embedding levels
        ///
        /// Currently implements: P2/P3 (paragraph level detection) and foundation for X rules
        /// TODO: Complete implementation of X, W, N, I rules for full UAX #9 compliance
        /// </summary>
        /// <param name="text">Input text to process</param>
        /// <param name="baseLevel">Base paragraph level (0=LTR, 1=RTL, -1=auto-detect)</param>
        /// <returns>List of BidiRun objects with resolved embedding levels</returns>
        public static List<BidiRun> ProcessRuns(string text, int baseLevel)
        {
            var runs = new List<BidiRun>();
            if (string.IsNullOrEmpty(text))
            {
                return runs;
            }

            // Phase 1: Determine paragraph embedding level (P2, P3)
            int paragraphLevel = DetermineParagraphLevel(text, baseLevel);

            // Phase 2: Apply X rules for explicit formatting characters
            // TODO: This is where the main X rules implementation will go
            var levels = new int[text.Length];
            var types = new BidiCharacterType[text.Length];

            // Initialize character types and levels
            InitializeCharacterTypesAndLevels(text, paragraphLevel, types, levels);

            // Apply X1-X8 rules for explicit formatting characters
            ApplyXRules(text, types, levels, paragraphLevel);

            // Phase 3-5: TODO - Apply W, N, I rules

            // Convert levels array to runs
            runs = ConvertLevelsToRuns(levels);

            return runs;
        }

        /// <summary>
        /// Determines the paragraph embedding level according to UAX #9 rules P2 and P3.
        /// </summary>
        private static int DetermineParagraphLevel(string text, int baseLevel)
        {
            // If explicit level provided and valid, use it
            if (baseLevel >= 0 && baseLevel <= 1)
            {
                return baseLevel;
            }

            // Auto-detect paragraph level (P2, P3)
            // P2: Find first strong character (L, AL, R)
            // P3: If first strong is L, paragraph level is 0; if AL or R, paragraph level is 1
            for (int i = 0; i < text.Length;)
            {
                int codepoint = char.ConvertToUtf32(text, i);
                BidiCharacterType charType = GetCharType(codepoint);

                if (charType == BidiCharacterType.L)
                {
                    return 0; // LTR paragraph
                }
                if (charType == BidiCharacterType.AL || charType == BidiCharacterType.R)
                {
                    return 1; // RTL paragraph
                }

                i += char.IsSurrogatePair(text, i) ? 2 : 1;
            }

            // No strong characters found, default to LTR
            return 0;
        }

        /// <summary>
        /// Initializes the character types and embedding levels arrays.
        /// </summary>
        private static void InitializeCharacterTypesAndLevels(string text, int paragraphLevel, BidiCharacterType[] types, int[] levels)
        {
            for (int i = 0; i < text.Length;)
            {
                int codepoint = char.ConvertToUtf32(text, i);
                BidiCharacterType charType = GetCharType(codepoint);

                // Set character type
                types[i] = charType;
                if (char.IsSurrogatePair(text, i))
                {
                    types[i + 1] = charType; // Surrogate pair shares the same type
                }

                // Set initial embedding level to paragraph level
                levels[i] = paragraphLevel;
                if (char.IsSurrogatePair(text, i))
                {
                    levels[i + 1] = paragraphLevel;
                }

                i += char.IsSurrogatePair(text, i) ? 2 : 1;
            }
        }

        /// <summary>
        /// Applies UAX #9 X rules (X1-X8) for processing explicit formatting characters.
        /// This implements the core logic for handling LRE, RLE, LRO, RLO, PDF and setting embedding levels.
        /// </summary>
        private static void ApplyXRules(string text, BidiCharacterType[] types, int[] levels, int paragraphLevel)
        {
            // Initialize directional status stack with paragraph level
            var stack = new Stack<DirectionalStatusStackEntry>();
            stack.Push(new DirectionalStatusStackEntry(paragraphLevel, DirectionalOverrideStatus.Neutral, false));

            // X1: Process each character in the text
            for (int i = 0; i < text.Length;)
            {
                BidiCharacterType charType = types[i];
                DirectionalStatusStackEntry currentEntry = stack.Peek();

                switch (charType)
                {
                    case BidiCharacterType.RLE:
                        // X2: Right-to-Left Embedding
                        ProcessRLE(stack, levels, i);
                        break;

                    case BidiCharacterType.LRE:
                        // X3: Left-to-Right Embedding
                        ProcessLRE(stack, levels, i);
                        break;

                    case BidiCharacterType.RLO:
                        // X4: Right-to-Left Override
                        ProcessRLO(stack, levels, i);
                        break;

                    case BidiCharacterType.LRO:
                        // X5: Left-to-Right Override
                        ProcessLRO(stack, levels, i);
                        break;

                    case BidiCharacterType.PDF:
                        // X7: Pop Directional Formatting
                        ProcessPDF(stack, levels, i);
                        break;

                    case BidiCharacterType.LRI:
                        // X5a: Left-to-Right Isolate
                        ProcessLRI(stack, levels, i);
                        break;

                    case BidiCharacterType.RLI:
                        // X5b: Right-to-Left Isolate
                        ProcessRLI(stack, levels, i);
                        break;

                    case BidiCharacterType.FSI:
                        // X5c: First Strong Isolate
                        ProcessFSI(text, types, stack, levels, i);
                        break;

                    case BidiCharacterType.PDI:
                        // X6a: Pop Directional Isolate
                        ProcessPDI(stack, levels, i);
                        break;

                    default:
                        // X6: For all other character types
                        ProcessOtherCharacter(currentEntry, types, levels, i);
                        break;
                }

                i += char.IsSurrogatePair(text, i) ? 2 : 1;
            }
        }

        /// <summary>
        /// X2: Process RLE (Right-to-Left Embedding) character.
        /// </summary>
        private static void ProcessRLE(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of RLE to current level
            levels[index] = current.EmbeddingLevel;

            // Calculate new embedding level (next odd level)
            int newLevel = (current.EmbeddingLevel + 1) | 1; // Force to odd

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.Neutral, false));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X3: Process LRE (Left-to-Right Embedding) character.
        /// </summary>
        private static void ProcessLRE(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of LRE to current level
            levels[index] = current.EmbeddingLevel;

            // Calculate new embedding level (next even level)
            int newLevel = (current.EmbeddingLevel + 2) & ~1; // Force to even

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.Neutral, false));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X4: Process RLO (Right-to-Left Override) character.
        /// </summary>
        private static void ProcessRLO(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of RLO to current level
            levels[index] = current.EmbeddingLevel;

            // Calculate new embedding level (next odd level)
            int newLevel = (current.EmbeddingLevel + 1) | 1; // Force to odd

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.RightToLeft, false));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X5: Process LRO (Left-to-Right Override) character.
        /// </summary>
        private static void ProcessLRO(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of LRO to current level
            levels[index] = current.EmbeddingLevel;

            // Calculate new embedding level (next even level)
            int newLevel = (current.EmbeddingLevel + 2) & ~1; // Force to even

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.LeftToRight, false));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X7: Process PDF (Pop Directional Formatting) character.
        /// </summary>
        private static void ProcessPDF(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of PDF to current level
            levels[index] = current.EmbeddingLevel;

            // Pop from stack if there's more than the initial entry (don't pop paragraph level)
            if (stack.Count > 1)
            {
                stack.Pop();
            }
            // If stack only has paragraph level entry, PDF has no effect (unmatched PDF)
        }

        /// <summary>
        /// X5a: Process LRI (Left-to-Right Isolate) character.
        /// </summary>
        private static void ProcessLRI(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of LRI to current level
            levels[index] = current.EmbeddingLevel;

            // Calculate new embedding level (next even level)
            int newLevel = (current.EmbeddingLevel + 2) & ~1; // Force to even

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.Neutral, true));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X5b: Process RLI (Right-to-Left Isolate) character.
        /// </summary>
        private static void ProcessRLI(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of RLI to current level
            levels[index] = current.EmbeddingLevel;

            // Calculate new embedding level (next odd level)
            int newLevel = (current.EmbeddingLevel + 1) | 1; // Force to odd

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.Neutral, true));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X5c: Process FSI (First Strong Isolate) character.
        /// Determines direction based on first strong character in the isolate sequence.
        /// </summary>
        private static void ProcessFSI(string text, BidiCharacterType[] types, Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of FSI to current level
            levels[index] = current.EmbeddingLevel;

            // Determine direction by scanning for first strong character
            bool isRTL = DetermineFirstStrongDirection(text, types, index + 1);

            // Calculate new embedding level based on determined direction
            int newLevel;
            if (isRTL)
            {
                newLevel = (current.EmbeddingLevel + 1) | 1; // Force to odd (RTL)
            }
            else
            {
                newLevel = (current.EmbeddingLevel + 2) & ~1; // Force to even (LTR)
            }

            // Check depth limit
            if (newLevel <= MaxEmbeddingDepth && stack.Count < MaxEmbeddingDepth)
            {
                stack.Push(new DirectionalStatusStackEntry(newLevel, DirectionalOverrideStatus.Neutral, true));
            }
            // If depth limit exceeded, don't push to stack (X9 overflow handling)
        }

        /// <summary>
        /// X6a: Process PDI (Pop Directional Isolate) character.
        /// </summary>
        private static void ProcessPDI(Stack<DirectionalStatusStackEntry> stack, int[] levels, int index)
        {
            DirectionalStatusStackEntry current = stack.Peek();

            // Set the embedding level of PDI to current level
            levels[index] = current.EmbeddingLevel;

            // Pop from stack if there's more than the initial entry and the current entry is an isolate
            if (stack.Count > 1 && current.IsolateStatus)
            {
                stack.Pop();
            }
            // If stack only has paragraph level entry or current entry is not an isolate, PDI has no effect
        }

        /// <summary>
        /// Determines the first strong direction for FSI processing.
        /// Scans forward from the given position to find the first strong character (L, AL, R).
        /// </summary>
        private static bool DetermineFirstStrongDirection(string text, BidiCharacterType[] types, int startIndex)
        {
            // Scan forward to find first strong character, respecting isolate boundaries
            int isolateDepth = 0;

            for (int i = startIndex; i < text.Length;)
            {
                BidiCharacterType charType = types[i];

                // Handle nested isolates
                if (charType == BidiCharacterType.LRI || charType == BidiCharacterType.RLI || charType == BidiCharacterType.FSI)
                {
                    isolateDepth++;
                }
                else if (charType == BidiCharacterType.PDI)
                {
                    if (isolateDepth == 0)
                    {
                        // This PDI matches our FSI, stop scanning
                        break;
                    }
                    isolateDepth--;
                }
                else if (isolateDepth == 0) // Only consider characters at our isolate level
                {
                    // Check for strong characters
                    if (charType == BidiCharacterType.L)
                    {
                        return false; // LTR
                    }
                    if (charType == BidiCharacterType.AL || charType == BidiCharacterType.R)
                    {
                        return true; // RTL
                    }
                }

                i += char.IsSurrogatePair(text, i) ? 2 : 1;
            }

            // No strong character found, default to LTR
            return false;
        }

        /// <summary>
        /// X6: Process all other character types (non-explicit formatting characters).
        /// </summary>
        private static void ProcessOtherCharacter(DirectionalStatusStackEntry currentEntry, BidiCharacterType[] types, int[] levels, int index)
        {
            // Set embedding level to current stack level
            levels[index] = currentEntry.EmbeddingLevel;

            // Apply directional override if active
            if (currentEntry.OverrideStatus == DirectionalOverrideStatus.LeftToRight)
            {
                types[index] = BidiCharacterType.L;
            }
            else if (currentEntry.OverrideStatus == DirectionalOverrideStatus.RightToLeft)
            {
                types[index] = BidiCharacterType.R;
            }
            // If override is Neutral, leave character type unchanged
        }

        /// <summary>
        /// Converts an array of embedding levels to a list of BidiRun objects.
        /// </summary>
        private static List<BidiRun> ConvertLevelsToRuns(int[] levels)
        {
            var runs = new List<BidiRun>();
            if (levels.Length == 0) return runs;

            int currentLevel = levels[0];
            int runStart = 0;

            for (int i = 1; i < levels.Length; i++)
            {
                if (levels[i] != currentLevel)
                {
                    // End current run and start new one
                    runs.Add(new BidiRun(runStart, i - runStart, currentLevel));
                    runStart = i;
                    currentLevel = levels[i];
                }
            }

            // Add final run
            runs.Add(new BidiRun(runStart, levels.Length - runStart, currentLevel));

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