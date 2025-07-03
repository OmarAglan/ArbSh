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
        /// Represents an isolating run sequence for UAX #9 W and N rules processing.
        /// An isolating run sequence is a maximal sequence of level runs such that for all level runs
        /// except the last one in the sequence, the last character of the run is an isolate initiator
        /// whose matching PDI is the first character of the next level run in the sequence.
        /// </summary>
        public class IsolatingRunSequence
        {
            public List<BidiCharacterType> Types { get; set; }
            public List<int> Positions { get; set; }  // Original text positions
            public BidiCharacterType Sos { get; set; }  // Start-of-sequence type
            public BidiCharacterType Eos { get; set; }  // End-of-sequence type
            public int EmbeddingLevel { get; set; }

            public IsolatingRunSequence()
            {
                Types = new List<BidiCharacterType>();
                Positions = new List<int>();
            }

            public override string ToString() =>
                $"IRS(Level:{EmbeddingLevel}, Count:{Types.Count}, Sos:{Sos}, Eos:{Eos})";
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

            // Phase 3: Apply W rules for weak type resolution
            ApplyWRules(text, types, levels);

            // Phase 4: Apply N rules for neutral type resolution
            ApplyNRules(text, types, levels);

            // Phase 5: Apply I rules for final level assignment
            ApplyIRules(types, levels);

            // Convert levels array to runs
            runs = ConvertLevelsToRuns(levels);

            return runs;
        }

        /// <summary>
        /// Determines the paragraph embedding level according to UAX #9 rules P2 and P3.
        /// P2: Find the first character of type L, AL, or R while skipping over any characters
        ///     between an isolate initiator and its matching PDI or, if it has no matching PDI,
        ///     the end of the paragraph.
        /// P3: If a character is found in P2 and it is of type AL or R, then set the paragraph
        ///     embedding level to one; otherwise, set it to zero.
        /// </summary>
        private static int DetermineParagraphLevel(string text, int baseLevel)
        {
            // If explicit level provided and valid, use it
            if (baseLevel >= 0 && baseLevel <= 1)
            {
                return baseLevel;
            }

            // Auto-detect paragraph level (P2, P3)
            // P2: Find first strong character (L, AL, R) while properly skipping isolates
            for (int i = 0; i < text.Length;)
            {
                int codepoint = char.ConvertToUtf32(text, i);
                BidiCharacterType charType = GetCharType(codepoint);

                // P2: Skip over characters between isolate initiator and its matching PDI
                if (IsIsolateInitiator(charType))
                {
                    // Skip to matching PDI or end of paragraph
                    int matchingPDI = FindMatchingPDI(text, i);
                    if (matchingPDI != -1)
                    {
                        i = matchingPDI + (char.IsSurrogatePair(text, matchingPDI) ? 2 : 1);
                        continue;
                    }
                    else
                    {
                        // No matching PDI, skip to end of paragraph
                        break;
                    }
                }

                // P2: Ignore embedding initiators (but not characters within the embedding)
                if (IsEmbeddingInitiator(charType))
                {
                    i += char.IsSurrogatePair(text, i) ? 2 : 1;
                    continue;
                }

                // P2: Check for strong characters (L, AL, R)
                if (charType == BidiCharacterType.L)
                {
                    return 0; // P3: LTR paragraph
                }
                if (charType == BidiCharacterType.AL || charType == BidiCharacterType.R)
                {
                    return 1; // P3: RTL paragraph
                }

                i += char.IsSurrogatePair(text, i) ? 2 : 1;
            }

            // P3: No strong characters found, default to LTR
            return 0;
        }

        /// <summary>
        /// Helper method to check if a character type is an embedding initiator (LRE, RLE, LRO, RLO).
        /// </summary>
        private static bool IsEmbeddingInitiator(BidiCharacterType charType)
        {
            return charType == BidiCharacterType.LRE ||
                   charType == BidiCharacterType.RLE ||
                   charType == BidiCharacterType.LRO ||
                   charType == BidiCharacterType.RLO;
        }

        /// <summary>
        /// Find the matching PDI for an isolate initiator at the given position.
        /// Returns the position of the matching PDI, or -1 if no matching PDI is found.
        /// This version works directly with text and character positions for P2 rule implementation.
        /// </summary>
        private static int FindMatchingPDI(string text, int isolateInitiatorPos)
        {
            int isolateDepth = 1;
            int i = isolateInitiatorPos + (char.IsSurrogatePair(text, isolateInitiatorPos) ? 2 : 1);

            while (i < text.Length)
            {
                int codepoint = char.ConvertToUtf32(text, i);
                BidiCharacterType charType = GetCharType(codepoint);

                if (IsIsolateInitiator(charType))
                {
                    isolateDepth++;
                }
                else if (charType == BidiCharacterType.PDI)
                {
                    isolateDepth--;
                    if (isolateDepth == 0)
                    {
                        return i; // Found matching PDI
                    }
                }

                i += char.IsSurrogatePair(text, i) ? 2 : 1;
            }

            return -1; // No matching PDI found
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
        /// Applies UAX #9 W rules (W1-W7) for resolving weak character types.
        /// This processes each isolating run sequence to resolve weak types like EN, AN, ES, ET, CS, NSM.
        /// </summary>
        private static void ApplyWRules(string text, BidiCharacterType[] types, int[] levels)
        {
            // Build isolating run sequences from level runs
            var isolatingRunSequences = BuildIsolatingRunSequences(text, types, levels);

            // Apply W rules to each isolating run sequence
            foreach (var sequence in isolatingRunSequences)
            {
                ApplyWRulesToSequence(sequence);

                // Copy resolved types back to the main types array
                for (int i = 0; i < sequence.Types.Count; i++)
                {
                    int textPosition = sequence.Positions[i];
                    types[textPosition] = sequence.Types[i];
                }
            }
        }

        /// <summary>
        /// Builds isolating run sequences from the current embedding levels and character types.
        /// An isolating run sequence is a maximal sequence of level runs connected by isolate initiators and their matching PDIs.
        /// </summary>
        private static List<IsolatingRunSequence> BuildIsolatingRunSequences(string text, BidiCharacterType[] types, int[] levels)
        {
            var sequences = new List<IsolatingRunSequence>();
            var processed = new bool[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                if (processed[i]) continue;

                // Start a new isolating run sequence
                var sequence = new IsolatingRunSequence();
                sequence.EmbeddingLevel = levels[i];

                // Determine sos (start-of-sequence) type
                sequence.Sos = DetermineSosType(levels, i);

                // Build the sequence by following level runs and isolate connections
                BuildSequenceFromPosition(text, types, levels, processed, sequence, i);

                // Determine eos (end-of-sequence) type
                sequence.Eos = DetermineEosType(levels, sequence.Positions.Count > 0 ? sequence.Positions[sequence.Positions.Count - 1] : i);

                sequences.Add(sequence);
            }

            return sequences;
        }

        /// <summary>
        /// Builds an isolating run sequence starting from the given position.
        /// </summary>
        private static void BuildSequenceFromPosition(string text, BidiCharacterType[] types, int[] levels,
            bool[] processed, IsolatingRunSequence sequence, int startPos)
        {
            int currentLevel = levels[startPos];
            int pos = startPos;

            // Add all characters at the same level to the sequence
            while (pos < text.Length && levels[pos] == currentLevel && !processed[pos])
            {
                sequence.Types.Add(types[pos]);
                sequence.Positions.Add(pos);
                processed[pos] = true;

                // Check if this character is an isolate initiator with a matching PDI
                if (IsIsolateInitiator(types[pos]))
                {
                    int matchingPDI = FindMatchingPDI(text, types, pos);
                    if (matchingPDI != -1 && levels[matchingPDI] == currentLevel)
                    {
                        // Jump to the matching PDI and continue the sequence
                        pos = matchingPDI;
                        continue;
                    }
                }

                pos++;
            }
        }

        /// <summary>
        /// Determines the start-of-sequence (sos) type for an isolating run sequence.
        /// </summary>
        private static BidiCharacterType DetermineSosType(int[] levels, int startPos)
        {
            // Look at the level before this sequence
            int prevLevel = startPos > 0 ? levels[startPos - 1] : levels[startPos];
            int currentLevel = levels[startPos];

            // Use the higher of the two levels to determine direction
            int sosLevel = Math.Max(prevLevel, currentLevel);
            return (sosLevel % 2 == 0) ? BidiCharacterType.L : BidiCharacterType.R;
        }

        /// <summary>
        /// Determines the end-of-sequence (eos) type for an isolating run sequence.
        /// </summary>
        private static BidiCharacterType DetermineEosType(int[] levels, int endPos)
        {
            // Look at the level after this sequence
            int nextLevel = endPos < levels.Length - 1 ? levels[endPos + 1] : levels[endPos];
            int currentLevel = levels[endPos];

            // Use the higher of the two levels to determine direction
            int eosLevel = Math.Max(nextLevel, currentLevel);
            return (eosLevel % 2 == 0) ? BidiCharacterType.L : BidiCharacterType.R;
        }

        /// <summary>
        /// Finds the matching PDI for an isolate initiator at the given position.
        /// Returns -1 if no matching PDI is found.
        /// </summary>
        private static int FindMatchingPDI(string text, BidiCharacterType[] types, int isolatePos)
        {
            int depth = 1;

            for (int i = isolatePos + 1; i < text.Length; i++)
            {
                if (IsIsolateInitiator(types[i]))
                {
                    depth++;
                }
                else if (types[i] == BidiCharacterType.PDI)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1; // No matching PDI found
        }

        /// <summary>
        /// Checks if the given character type is an isolate initiator (LRI, RLI, FSI).
        /// </summary>
        private static bool IsIsolateInitiator(BidiCharacterType type)
        {
            return type == BidiCharacterType.LRI ||
                   type == BidiCharacterType.RLI ||
                   type == BidiCharacterType.FSI;
        }

        /// <summary>
        /// Checks if the given character type is an isolate initiator or PDI.
        /// </summary>
        private static bool IsIsolateInitiatorOrPDI(BidiCharacterType type)
        {
            return IsIsolateInitiator(type) || type == BidiCharacterType.PDI;
        }

        /// <summary>
        /// Applies all W rules (W1-W7) to a single isolating run sequence.
        /// </summary>
        private static void ApplyWRulesToSequence(IsolatingRunSequence sequence)
        {
            ApplyW1_NonspacingMarks(sequence);
            ApplyW2_EuropeanNumberContext(sequence);
            ApplyW3_ArabicLetterSimplification(sequence);
            ApplyW4_NumberSeparators(sequence);
            ApplyW5_EuropeanTerminators(sequence);
            ApplyW6_RemainingSeparators(sequence);
            ApplyW7_EuropeanNumberFinal(sequence);
        }

        /// <summary>
        /// W1: Examine each nonspacing mark (NSM) in the isolating run sequence, and change the type of the NSM
        /// to Other Neutral if the previous character is an isolate initiator or PDI, and to the type of the
        /// previous character otherwise. If the NSM is at the start of the isolating run sequence, it will get the type of sos.
        /// </summary>
        private static void ApplyW1_NonspacingMarks(IsolatingRunSequence sequence)
        {
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (sequence.Types[i] == BidiCharacterType.NSM)
                {
                    if (i == 0)
                    {
                        // NSM at start of sequence gets sos type
                        sequence.Types[i] = sequence.Sos;
                    }
                    else
                    {
                        var prevType = sequence.Types[i - 1];
                        if (IsIsolateInitiatorOrPDI(prevType))
                        {
                            // NSM after isolate initiator or PDI becomes ON
                            sequence.Types[i] = BidiCharacterType.ON;
                        }
                        else
                        {
                            // NSM takes the type of the previous character
                            sequence.Types[i] = prevType;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// W2: Search backward from each instance of a European number until the first strong type (R, L, AL, or sos) is found.
        /// If an AL is found, change the type of the European number to Arabic number.
        /// </summary>
        private static void ApplyW2_EuropeanNumberContext(IsolatingRunSequence sequence)
        {
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (sequence.Types[i] == BidiCharacterType.EN)
                {
                    var strongType = SearchBackwardForStrongType(sequence, i);
                    if (strongType == BidiCharacterType.AL)
                    {
                        sequence.Types[i] = BidiCharacterType.AN;
                    }
                }
            }
        }

        /// <summary>
        /// W3: Change all ALs to R.
        /// </summary>
        private static void ApplyW3_ArabicLetterSimplification(IsolatingRunSequence sequence)
        {
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (sequence.Types[i] == BidiCharacterType.AL)
                {
                    sequence.Types[i] = BidiCharacterType.R;
                }
            }
        }

        /// <summary>
        /// W4: A single European separator between two European numbers changes to a European number.
        /// A single common separator between two numbers of the same type changes to that type.
        /// </summary>
        private static void ApplyW4_NumberSeparators(IsolatingRunSequence sequence)
        {
            for (int i = 1; i < sequence.Types.Count - 1; i++)
            {
                var current = sequence.Types[i];
                var prev = sequence.Types[i - 1];
                var next = sequence.Types[i + 1];

                // Single ES between two EN
                if (current == BidiCharacterType.ES &&
                    prev == BidiCharacterType.EN &&
                    next == BidiCharacterType.EN)
                {
                    sequence.Types[i] = BidiCharacterType.EN;
                }
                // Single CS between two numbers of same type
                else if (current == BidiCharacterType.CS &&
                         IsNumberType(prev) && prev == next)
                {
                    sequence.Types[i] = prev;
                }
            }
        }

        /// <summary>
        /// W5: A sequence of European terminators adjacent to European numbers changes to all European numbers.
        /// </summary>
        private static void ApplyW5_EuropeanTerminators(IsolatingRunSequence sequence)
        {
            // Process ET sequences before EN
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (sequence.Types[i] == BidiCharacterType.ET)
                {
                    // Find the extent of the ET sequence
                    int start = i;
                    while (i < sequence.Types.Count && sequence.Types[i] == BidiCharacterType.ET)
                    {
                        i++;
                    }
                    int end = i - 1;

                    // Check if this ET sequence is adjacent to EN
                    bool adjacentToEN = false;

                    // Check before the sequence
                    if (start > 0 && sequence.Types[start - 1] == BidiCharacterType.EN)
                    {
                        adjacentToEN = true;
                    }
                    // Check after the sequence
                    if (end < sequence.Types.Count - 1 && sequence.Types[end + 1] == BidiCharacterType.EN)
                    {
                        adjacentToEN = true;
                    }

                    // If adjacent to EN, change all ET to EN
                    if (adjacentToEN)
                    {
                        for (int j = start; j <= end; j++)
                        {
                            sequence.Types[j] = BidiCharacterType.EN;
                        }
                    }

                    i = end; // Continue from end of sequence
                }
            }
        }

        /// <summary>
        /// W6: All remaining separators and terminators (after the application of W4 and W5) change to Other Neutral.
        /// </summary>
        private static void ApplyW6_RemainingSeparators(IsolatingRunSequence sequence)
        {
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                var type = sequence.Types[i];
                if (type == BidiCharacterType.ES ||
                    type == BidiCharacterType.ET ||
                    type == BidiCharacterType.CS)
                {
                    sequence.Types[i] = BidiCharacterType.ON;
                }
            }
        }

        /// <summary>
        /// W7: Search backward from each instance of a European number until the first strong type (R, L, or sos) is found.
        /// If an L is found, then change the type of the European number to L.
        /// </summary>
        private static void ApplyW7_EuropeanNumberFinal(IsolatingRunSequence sequence)
        {
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (sequence.Types[i] == BidiCharacterType.EN)
                {
                    var strongType = SearchBackwardForStrongTypeW7(sequence, i);
                    if (strongType == BidiCharacterType.L)
                    {
                        sequence.Types[i] = BidiCharacterType.L;
                    }
                }
            }
        }

        /// <summary>
        /// Searches backward from the given position for the first strong type (R, L, AL, or sos).
        /// Used by W2 rule.
        /// </summary>
        private static BidiCharacterType SearchBackwardForStrongType(IsolatingRunSequence sequence, int startIndex)
        {
            for (int i = startIndex - 1; i >= 0; i--)
            {
                var type = sequence.Types[i];
                if (IsStrongType(type))
                {
                    return type;
                }
            }
            return sequence.Sos;
        }

        /// <summary>
        /// Searches backward from the given position for the first strong type (R, L, or sos).
        /// Used by W7 rule (note: AL is not included as it was converted to R in W3).
        /// </summary>
        private static BidiCharacterType SearchBackwardForStrongTypeW7(IsolatingRunSequence sequence, int startIndex)
        {
            for (int i = startIndex - 1; i >= 0; i--)
            {
                var type = sequence.Types[i];
                if (type == BidiCharacterType.L || type == BidiCharacterType.R)
                {
                    return type;
                }
            }
            return sequence.Sos;
        }

        /// <summary>
        /// Checks if the given character type is a strong type (L, R, AL).
        /// </summary>
        private static bool IsStrongType(BidiCharacterType type)
        {
            return type == BidiCharacterType.L ||
                   type == BidiCharacterType.R ||
                   type == BidiCharacterType.AL;
        }

        /// <summary>
        /// Checks if the given character type is a number type (EN, AN).
        /// </summary>
        private static bool IsNumberType(BidiCharacterType type)
        {
            return type == BidiCharacterType.EN || type == BidiCharacterType.AN;
        }

        // --- N Rules (Neutral Type Resolution) ---

        /// <summary>
        /// Applies UAX #9 N rules (N0-N2) for resolving neutral character types.
        /// This processes each isolating run sequence to resolve neutral types like ON, WS, S, B.
        /// </summary>
        private static void ApplyNRules(string text, BidiCharacterType[] types, int[] levels)
        {
            // Build isolating run sequences from level runs (reuse W rules infrastructure)
            var isolatingRunSequences = BuildIsolatingRunSequences(text, types, levels);

            // Apply N rules to each isolating run sequence
            foreach (var sequence in isolatingRunSequences)
            {
                ApplyNRulesToSequence(text, sequence);

                // Copy resolved types back to the main types array
                for (int i = 0; i < sequence.Types.Count; i++)
                {
                    int textPosition = sequence.Positions[i];
                    types[textPosition] = sequence.Types[i];
                }
            }
        }

        /// <summary>
        /// Applies all N rules (N0-N2) to a single isolating run sequence.
        /// </summary>
        private static void ApplyNRulesToSequence(string text, IsolatingRunSequence sequence)
        {
            ApplyN0_BracketPairs(text, sequence);
            ApplyN1_SurroundingStrongTypes(sequence);
            ApplyN2_EmbeddingDirection(sequence);
        }

        /// <summary>
        /// N0: Process bracket pairs in an isolating run sequence sequentially in the logical order
        /// of the text positions of the opening paired brackets. Within this scope, bidirectional
        /// types EN and AN are treated as R.
        /// </summary>
        private static void ApplyN0_BracketPairs(string text, IsolatingRunSequence sequence)
        {
            var bracketPairs = IdentifyBracketPairs(text, sequence);

            foreach (var pair in bracketPairs)
            {
                ProcessBracketPair(sequence, pair);
            }
        }

        /// <summary>
        /// N1: Look for a sequence of neutrals (NI) between two characters of the same strong type.
        /// If found, change the neutrals to match that strong type. EN and AN are treated as R.
        /// </summary>
        private static void ApplyN1_SurroundingStrongTypes(IsolatingRunSequence sequence)
        {
            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (IsNeutralType(sequence.Types[i]))
                {
                    // Find the extent of the neutral sequence
                    int start = i;
                    while (i < sequence.Types.Count && IsNeutralType(sequence.Types[i]))
                    {
                        i++;
                    }
                    int end = i - 1;

                    // Get the strong types before and after the neutral sequence
                    var precedingType = GetPrecedingStrongType(sequence, start);
                    var followingType = GetFollowingStrongType(sequence, end);

                    // If both sides have the same strong type, change neutrals to that type
                    if (precedingType == followingType && IsStrongTypeForN1(precedingType))
                    {
                        for (int j = start; j <= end; j++)
                        {
                            sequence.Types[j] = precedingType;
                        }
                    }

                    i--; // Adjust for the outer loop increment
                }
            }
        }

        /// <summary>
        /// N2: Any remaining neutrals take the embedding direction.
        /// Even levels → L, Odd levels → R
        /// </summary>
        private static void ApplyN2_EmbeddingDirection(IsolatingRunSequence sequence)
        {
            var embeddingDirection = GetEmbeddingDirection(sequence.EmbeddingLevel);

            for (int i = 0; i < sequence.Types.Count; i++)
            {
                if (IsNeutralType(sequence.Types[i]))
                {
                    sequence.Types[i] = embeddingDirection;
                }
            }
        }

        // --- N Rules Helper Methods ---

        /// <summary>
        /// Bracket pair structure for N0 processing.
        /// </summary>
        private struct BracketPair
        {
            public int OpeningPosition;    // Position in isolating run sequence
            public int ClosingPosition;    // Position in isolating run sequence
            public char OpeningChar;       // Opening bracket character
            public char ClosingChar;       // Closing bracket character

            public BracketPair(int openPos, int closePos, char openChar, char closeChar)
            {
                OpeningPosition = openPos;
                ClosingPosition = closePos;
                OpeningChar = openChar;
                ClosingChar = closeChar;
            }
        }

        /// <summary>
        /// Stack entry for BD16 bracket pair identification algorithm.
        /// </summary>
        private struct BracketStackEntry
        {
            public char BracketChar;       // Bidi_Paired_Bracket property value
            public int TextPosition;       // Position in isolating run sequence

            public BracketStackEntry(char bracketChar, int position)
            {
                BracketChar = bracketChar;
                TextPosition = position;
            }
        }

        /// <summary>
        /// Hardcoded bracket pair mappings (fallback for missing ICU4N properties).
        /// </summary>
        private static readonly Dictionary<char, char> BracketPairs = new Dictionary<char, char>
        {
            // Basic brackets
            { '(', ')' }, { ')', '(' },
            { '[', ']' }, { ']', '[' },
            { '{', '}' }, { '}', '{' },

            // Angle brackets (canonical equivalence)
            { '⟨', '⟩' }, { '⟩', '⟨' },  // U+27E8, U+27E9
            { '〈', '〉' }, { '〉', '〈' },  // U+3008, U+3009 (canonical equivalent)

            // Additional Unicode brackets
            { '⟦', '⟧' }, { '⟧', '⟦' },  // U+27E6, U+27E7
            { '⟪', '⟫' }, { '⟫', '⟪' },  // U+27EA, U+27EB
            { '⦃', '⦄' }, { '⦄', '⦃' },  // U+2983, U+2984
            { '⦅', '⦆' }, { '⦆', '⦅' },  // U+2985, U+2986
        };

        /// <summary>
        /// Set of opening bracket characters.
        /// </summary>
        private static readonly HashSet<char> OpeningBrackets = new HashSet<char>
        {
            '(', '[', '{', '⟨', '〈', '⟦', '⟪', '⦃', '⦅'
        };

        /// <summary>
        /// Identifies bracket pairs in an isolating run sequence using BD16 algorithm.
        /// </summary>
        private static List<BracketPair> IdentifyBracketPairs(string text, IsolatingRunSequence sequence)
        {
            var stack = new BracketStackEntry[63]; // Fixed 63-element stack per BD16
            int stackTop = -1;
            var pairs = new List<BracketPair>();

            for (int i = 0; i < sequence.Types.Count; i++)
            {
                char currentChar = GetCharFromSequence(text, sequence, i);

                if (IsOpeningBracket(currentChar, sequence.Types[i]))
                {
                    // Push opening bracket if there's room
                    if (stackTop < 62) // 0-based, so 62 is the last valid index
                    {
                        stackTop++;
                        stack[stackTop] = new BracketStackEntry(GetPairedBracket(currentChar), i);
                    }
                    else
                    {
                        // Stack overflow - return empty list per BD16
                        return new List<BracketPair>();
                    }
                }
                else if (IsClosingBracket(currentChar, sequence.Types[i]))
                {
                    // Look for matching opening bracket in stack
                    char pairedChar = GetPairedBracket(currentChar);

                    for (int j = stackTop; j >= 0; j--)
                    {
                        if (stack[j].BracketChar == currentChar ||
                            AreCanonicalEquivalent(stack[j].BracketChar, currentChar))
                        {
                            // Found matching pair
                            char openingChar = GetCharFromSequence(text, sequence, stack[j].TextPosition);
                            pairs.Add(new BracketPair(stack[j].TextPosition, i, openingChar, currentChar));

                            // Pop stack through this element inclusively
                            stackTop = j - 1;
                            break;
                        }
                    }
                }
            }

            // Sort pairs by opening bracket position
            pairs.Sort((a, b) => a.OpeningPosition.CompareTo(b.OpeningPosition));
            return pairs;
        }

        /// <summary>
        /// Gets the paired bracket character for a given bracket.
        /// </summary>
        private static char GetPairedBracket(char c)
        {
            return BracketPairs.TryGetValue(c, out char paired) ? paired : c;
        }

        /// <summary>
        /// Checks if a character is an opening bracket with ON type (BD14).
        /// </summary>
        private static bool IsOpeningBracket(char c, BidiCharacterType currentType)
        {
            return currentType == BidiCharacterType.ON && OpeningBrackets.Contains(c);
        }

        /// <summary>
        /// Checks if a character is a closing bracket with ON type (BD15).
        /// </summary>
        private static bool IsClosingBracket(char c, BidiCharacterType currentType)
        {
            return currentType == BidiCharacterType.ON &&
                   BracketPairs.ContainsKey(c) &&
                   !OpeningBrackets.Contains(c);
        }

        /// <summary>
        /// Checks if two bracket characters are canonical equivalents.
        /// </summary>
        private static bool AreCanonicalEquivalent(char c1, char c2)
        {
            // Handle canonical equivalence for angle brackets
            return (c1 == '⟨' && c2 == '〈') || (c1 == '〈' && c2 == '⟨') ||
                   (c1 == '⟩' && c2 == '〉') || (c1 == '〉' && c2 == '⟩');
        }

        /// <summary>
        /// Gets the character at a specific position in the isolating run sequence.
        /// </summary>
        private static char GetCharFromSequence(string text, IsolatingRunSequence sequence, int position)
        {
            if (position >= 0 && position < sequence.Positions.Count)
            {
                int textPosition = sequence.Positions[position];
                if (textPosition >= 0 && textPosition < text.Length)
                {
                    return text[textPosition];
                }
            }
            return '?'; // Fallback for invalid positions
        }

        /// <summary>
        /// Processes a single bracket pair according to N0 rule logic.
        /// </summary>
        private static void ProcessBracketPair(IsolatingRunSequence sequence, BracketPair pair)
        {
            // Find strong type inside the bracket pair
            var strongTypeInside = FindStrongTypeInBrackets(sequence, pair.OpeningPosition + 1, pair.ClosingPosition - 1);
            var embeddingDirection = GetEmbeddingDirection(sequence.EmbeddingLevel);

            BidiCharacterType newType;

            if (strongTypeInside == embeddingDirection)
            {
                // Strong type matches embedding direction
                newType = embeddingDirection;
            }
            else if (strongTypeInside != null && IsStrongTypeForN1(strongTypeInside.Value))
            {
                // Strong type opposite to embedding direction - check context
                var precedingType = GetPrecedingStrongType(sequence, pair.OpeningPosition);
                if (precedingType == strongTypeInside)
                {
                    newType = strongTypeInside.Value;
                }
                else
                {
                    newType = embeddingDirection;
                }
            }
            else
            {
                // No strong type inside - leave unchanged for N1/N2
                return;
            }

            // Set both brackets to the determined type
            sequence.Types[pair.OpeningPosition] = newType;
            sequence.Types[pair.ClosingPosition] = newType;

            // Handle NSM characters following brackets that changed type
            HandleNSMAfterBrackets(sequence, pair.OpeningPosition, newType);
            HandleNSMAfterBrackets(sequence, pair.ClosingPosition, newType);
        }

        /// <summary>
        /// Finds the first strong type within a bracket pair (treating EN/AN as R).
        /// </summary>
        private static BidiCharacterType? FindStrongTypeInBrackets(IsolatingRunSequence sequence, int start, int end)
        {
            for (int i = start; i <= end && i < sequence.Types.Count; i++)
            {
                var type = sequence.Types[i];
                if (type == BidiCharacterType.L)
                    return BidiCharacterType.L;
                if (type == BidiCharacterType.R || type == BidiCharacterType.AL ||
                    type == BidiCharacterType.EN || type == BidiCharacterType.AN)
                    return BidiCharacterType.R;
            }
            return null;
        }

        /// <summary>
        /// Handles NSM characters following brackets that changed type in N0.
        /// </summary>
        private static void HandleNSMAfterBrackets(IsolatingRunSequence sequence, int bracketPosition, BidiCharacterType newType)
        {
            for (int i = bracketPosition + 1; i < sequence.Types.Count; i++)
            {
                if (sequence.Types[i] == BidiCharacterType.NSM)
                {
                    sequence.Types[i] = newType;
                }
                else
                {
                    break; // Stop at first non-NSM
                }
            }
        }

        /// <summary>
        /// Checks if a character type is neutral (NI) for N rules processing.
        /// </summary>
        private static bool IsNeutralType(BidiCharacterType type)
        {
            return type == BidiCharacterType.ON ||
                   type == BidiCharacterType.WS ||
                   type == BidiCharacterType.S ||
                   type == BidiCharacterType.B;
        }

        /// <summary>
        /// Checks if a character type is strong for N1 rule (treating EN/AN as R).
        /// </summary>
        private static bool IsStrongTypeForN1(BidiCharacterType type)
        {
            return type == BidiCharacterType.L || type == BidiCharacterType.R;
        }

        /// <summary>
        /// Gets the preceding strong type for N1 rule (treating EN/AN as R).
        /// </summary>
        private static BidiCharacterType GetPrecedingStrongType(IsolatingRunSequence sequence, int position)
        {
            for (int i = position - 1; i >= 0; i--)
            {
                var type = sequence.Types[i];
                if (type == BidiCharacterType.L)
                    return BidiCharacterType.L;
                if (type == BidiCharacterType.R || type == BidiCharacterType.AL ||
                    type == BidiCharacterType.EN || type == BidiCharacterType.AN)
                    return BidiCharacterType.R;
            }
            // Return sos if no strong type found
            return sequence.Sos == BidiCharacterType.L ? BidiCharacterType.L : BidiCharacterType.R;
        }

        /// <summary>
        /// Gets the following strong type for N1 rule (treating EN/AN as R).
        /// </summary>
        private static BidiCharacterType GetFollowingStrongType(IsolatingRunSequence sequence, int position)
        {
            for (int i = position + 1; i < sequence.Types.Count; i++)
            {
                var type = sequence.Types[i];
                if (type == BidiCharacterType.L)
                    return BidiCharacterType.L;
                if (type == BidiCharacterType.R || type == BidiCharacterType.AL ||
                    type == BidiCharacterType.EN || type == BidiCharacterType.AN)
                    return BidiCharacterType.R;
            }
            // Return eos if no strong type found
            return sequence.Eos == BidiCharacterType.L ? BidiCharacterType.L : BidiCharacterType.R;
        }

        /// <summary>
        /// Gets the embedding direction from the embedding level.
        /// Even levels → L, Odd levels → R
        /// </summary>
        private static BidiCharacterType GetEmbeddingDirection(int level)
        {
            return (level % 2 == 0) ? BidiCharacterType.L : BidiCharacterType.R;
        }

        /// <summary>
        /// Applies UAX #9 I rules (I1-I2) for final embedding level assignment.
        /// I1: For characters with even embedding levels:
        ///     - R characters: level + 1 (even → odd)
        ///     - AN/EN characters: level + 2 (even → even, higher)
        /// I2: For characters with odd embedding levels:
        ///     - L/EN/AN characters: level + 1 (odd → even)
        /// </summary>
        private static void ApplyIRules(BidiCharacterType[] types, int[] levels)
        {
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var currentLevel = levels[i];

                // Skip BN characters (Boundary Neutrals) per UAX #9 specification
                if (type == BidiCharacterType.BN)
                    continue;

                int newLevel = ResolveImplicitLevel(type, currentLevel);

                // Validate against maximum depth (max_depth + 1 = 126)
                if (newLevel <= MaxEmbeddingDepth + 1)
                {
                    levels[i] = newLevel;
                }
                // If exceeding max depth, keep current level (overflow handling)
            }
        }

        /// <summary>
        /// Resolves the implicit embedding level for a character based on its type and current level.
        /// Implements the core logic of UAX #9 I1 and I2 rules.
        /// </summary>
        private static int ResolveImplicitLevel(BidiCharacterType type, int currentLevel)
        {
            bool isEvenLevel = (currentLevel % 2 == 0);

            if (isEvenLevel)
            {
                // I1: Even level adjustments
                switch (type)
                {
                    case BidiCharacterType.R:
                        return currentLevel + 1; // Even → Odd
                    case BidiCharacterType.AN:
                    case BidiCharacterType.EN:
                        return currentLevel + 2; // Even → Even (higher)
                    default:
                        return currentLevel; // No change
                }
            }
            else
            {
                // I2: Odd level adjustments
                switch (type)
                {
                    case BidiCharacterType.L:
                    case BidiCharacterType.EN:
                    case BidiCharacterType.AN:
                        return currentLevel + 1; // Odd → Even
                    default:
                        return currentLevel; // No change
                }
            }
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

        /// <summary>
        /// Applies UAX #9 L rules (L1-L4) for final reordering and display.
        /// L1: Reset levels for separators and trailing whitespace
        /// L2: Reverse contiguous sequences by level
        /// L3: Handle combining marks (rendering-dependent)
        /// L4: Apply character mirroring
        /// </summary>
        public static string ReorderRunsForDisplay(string originalText, List<BidiRun> runs, int paragraphLevel = 0)
        {
            if (string.IsNullOrEmpty(originalText) || runs == null || runs.Count == 0)
            {
                return originalText ?? string.Empty;
            }

            // Create a working copy of runs for L1 modifications
            var workingRuns = new List<BidiRun>(runs);

            // L1: Reset levels for separators and trailing whitespace
            ApplyL1_ResetLevels(originalText, workingRuns, paragraphLevel);

            // L2: Reverse contiguous sequences by level
            string reorderedText = ApplyL2_ReverseByLevel(originalText, workingRuns);

            // L3: Handle combining marks (optional, rendering-dependent)
            // Currently not implemented as it's primarily a rendering concern

            // L4: Apply character mirroring
            reorderedText = ApplyL4_CharacterMirroring(reorderedText, workingRuns);

            return reorderedText;
        }

        /// <summary>
        /// Overload that maintains backward compatibility with existing calls.
        /// </summary>
        public static string ReorderRunsForDisplay(string originalText, List<BidiRun> runs)
        {
            return ReorderRunsForDisplay(originalText, runs, 0);
        }

        /// <summary>
        /// L1: Reset levels for separators and trailing whitespace to paragraph level.
        /// This modifies the runs in place to adjust levels for proper display.
        /// </summary>
        private static void ApplyL1_ResetLevels(string text, List<BidiRun> runs, int paragraphLevel)
        {
            if (runs.Count == 0) return;

            // L1.1: Reset segment separators and paragraph separators to paragraph level
            for (int i = 0; i < runs.Count; i++)
            {
                var run = runs[i];
                for (int pos = run.Start; pos < run.Start + run.Length; pos++)
                {
                    char ch = text[pos];
                    var charType = GetCharType(ch);

                    // Reset B (paragraph separators) and S (segment separators) to paragraph level
                    if (charType == BidiCharacterType.B || charType == BidiCharacterType.S)
                    {
                        // Split run if necessary and reset level
                        if (run.Length == 1)
                        {
                            // Single character run, just update level
                            runs[i] = new BidiRun(run.Start, run.Length, paragraphLevel);
                        }
                        else
                        {
                            // Need to split the run
                            SplitRunAtPosition(runs, i, pos, paragraphLevel);
                        }
                    }
                }
            }

            // L1.2: Reset trailing whitespace to paragraph level
            // Find trailing whitespace from the end of the text
            int trailingStart = text.Length;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                var charType = GetCharType(text[i]);
                if (charType == BidiCharacterType.WS || charType == BidiCharacterType.FSI ||
                    charType == BidiCharacterType.LRI || charType == BidiCharacterType.RLI ||
                    charType == BidiCharacterType.PDI)
                {
                    trailingStart = i;
                }
                else
                {
                    break;
                }
            }

            // Reset levels for trailing whitespace runs
            if (trailingStart < text.Length)
            {
                for (int i = 0; i < runs.Count; i++)
                {
                    var run = runs[i];
                    int runEnd = run.Start + run.Length;

                    // If run overlaps with trailing whitespace area
                    if (run.Start < text.Length && runEnd > trailingStart)
                    {
                        int overlapStart = Math.Max(run.Start, trailingStart);
                        int overlapEnd = Math.Min(runEnd, text.Length);

                        if (overlapStart < overlapEnd)
                        {
                            // Split and reset the overlapping portion
                            SplitRunForTrailingWhitespace(runs, i, overlapStart, overlapEnd, paragraphLevel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to split a run at a specific position and set the character at that position to a new level.
        /// </summary>
        private static void SplitRunAtPosition(List<BidiRun> runs, int runIndex, int position, int newLevel)
        {
            var originalRun = runs[runIndex];
            int relativePos = position - originalRun.Start;

            if (relativePos == 0 && originalRun.Length == 1)
            {
                // Single character at start, just update level
                runs[runIndex] = new BidiRun(originalRun.Start, 1, newLevel);
            }
            else if (relativePos == 0)
            {
                // Split at start: [new][rest]
                runs[runIndex] = new BidiRun(originalRun.Start, 1, newLevel);
                runs.Insert(runIndex + 1, new BidiRun(originalRun.Start + 1, originalRun.Length - 1, originalRun.Level));
            }
            else if (relativePos == originalRun.Length - 1)
            {
                // Split at end: [before][new]
                runs[runIndex] = new BidiRun(originalRun.Start, originalRun.Length - 1, originalRun.Level);
                runs.Insert(runIndex + 1, new BidiRun(position, 1, newLevel));
            }
            else
            {
                // Split in middle: [before][new][after]
                runs[runIndex] = new BidiRun(originalRun.Start, relativePos, originalRun.Level);
                runs.Insert(runIndex + 1, new BidiRun(position, 1, newLevel));
                runs.Insert(runIndex + 2, new BidiRun(position + 1, originalRun.Length - relativePos - 1, originalRun.Level));
            }
        }

        /// <summary>
        /// Helper method to split runs for trailing whitespace level reset.
        /// </summary>
        private static void SplitRunForTrailingWhitespace(List<BidiRun> runs, int runIndex, int overlapStart, int overlapEnd, int newLevel)
        {
            var originalRun = runs[runIndex];

            if (overlapStart == originalRun.Start && overlapEnd == originalRun.Start + originalRun.Length)
            {
                // Entire run is trailing whitespace
                runs[runIndex] = new BidiRun(originalRun.Start, originalRun.Length, newLevel);
            }
            else if (overlapStart == originalRun.Start)
            {
                // Trailing whitespace at start of run: [whitespace][rest]
                int whitespaceLength = overlapEnd - overlapStart;
                runs[runIndex] = new BidiRun(originalRun.Start, whitespaceLength, newLevel);
                runs.Insert(runIndex + 1, new BidiRun(overlapEnd, originalRun.Length - whitespaceLength, originalRun.Level));
            }
            else if (overlapEnd == originalRun.Start + originalRun.Length)
            {
                // Trailing whitespace at end of run: [before][whitespace]
                int beforeLength = overlapStart - originalRun.Start;
                int whitespaceLength = overlapEnd - overlapStart;
                runs[runIndex] = new BidiRun(originalRun.Start, beforeLength, originalRun.Level);
                runs.Insert(runIndex + 1, new BidiRun(overlapStart, whitespaceLength, newLevel));
            }
            else
            {
                // Trailing whitespace in middle: [before][whitespace][after]
                int beforeLength = overlapStart - originalRun.Start;
                int whitespaceLength = overlapEnd - overlapStart;
                int afterLength = (originalRun.Start + originalRun.Length) - overlapEnd;

                runs[runIndex] = new BidiRun(originalRun.Start, beforeLength, originalRun.Level);
                runs.Insert(runIndex + 1, new BidiRun(overlapStart, whitespaceLength, newLevel));
                runs.Insert(runIndex + 2, new BidiRun(overlapEnd, afterLength, originalRun.Level));
            }
        }

        /// <summary>
        /// L2: Reverse contiguous sequences by level from highest to lowest odd level.
        /// This implements the progressive reversal algorithm from UAX #9.
        /// </summary>
        private static string ApplyL2_ReverseByLevel(string originalText, List<BidiRun> runs)
        {
            if (runs.Count == 0) return originalText;

            // Find the maximum level
            int maxLevel = 0;
            foreach (var run in runs)
            {
                if (run.Level > maxLevel) maxLevel = run.Level;
            }

            // Create character array for reordering
            var chars = originalText.ToCharArray();
            var levels = new int[chars.Length];

            // Fill levels array from runs
            foreach (var run in runs)
            {
                for (int i = run.Start; i < run.Start + run.Length && i < levels.Length; i++)
                {
                    levels[i] = run.Level;
                }
            }

            // L2: Reverse from highest level down to 1 (only odd levels are reversed)
            for (int level = maxLevel; level >= 1; level--)
            {
                if (level % 2 == 1) // Only reverse odd levels
                {
                    ReverseSequencesAtLevel(chars, levels, level);
                }
            }

            return new string(chars);
        }

        /// <summary>
        /// Helper method to reverse all contiguous sequences at a specific level.
        /// </summary>
        private static void ReverseSequencesAtLevel(char[] chars, int[] levels, int targetLevel)
        {
            int start = -1;

            for (int i = 0; i <= levels.Length; i++)
            {
                bool atTargetLevel = i < levels.Length && levels[i] >= targetLevel;

                if (atTargetLevel && start == -1)
                {
                    // Start of sequence
                    start = i;
                }
                else if (!atTargetLevel && start != -1)
                {
                    // End of sequence, reverse it
                    ReverseCharacterSequence(chars, start, i - 1);
                    start = -1;
                }
            }
        }

        /// <summary>
        /// Helper method to reverse a character sequence, handling grapheme clusters properly.
        /// </summary>
        private static void ReverseCharacterSequence(char[] chars, int start, int end)
        {
            if (start >= end) return;

            // Simple character reversal - for full grapheme cluster support,
            // we would need to use StringInfo, but this handles most cases
            while (start < end)
            {
                char temp = chars[start];
                chars[start] = chars[end];
                chars[end] = temp;
                start++;
                end--;
            }
        }

        /// <summary>
        /// L4: Apply character mirroring for characters with Bidi_Mirrored=Yes in RTL context.
        /// This replaces mirrored characters with their mirrored forms when in RTL runs.
        /// </summary>
        private static string ApplyL4_CharacterMirroring(string text, List<BidiRun> runs)
        {
            if (runs.Count == 0) return text;

            var chars = text.ToCharArray();

            foreach (var run in runs)
            {
                // Only apply mirroring to RTL runs (odd levels)
                if (run.Level % 2 == 1)
                {
                    for (int i = run.Start; i < run.Start + run.Length && i < chars.Length; i++)
                    {
                        char original = chars[i];
                        char mirrored = GetMirroredCharacter(original);
                        if (mirrored != original)
                        {
                            chars[i] = mirrored;
                        }
                    }
                }
            }

            return new string(chars);
        }

        /// <summary>
        /// Get the mirrored form of a character if it has the Bidi_Mirrored property.
        /// This is a simplified implementation covering common mirrored characters.
        /// A full implementation would use the Unicode Bidi_Mirroring_Glyph property.
        /// </summary>
        private static char GetMirroredCharacter(char ch)
        {
            // Common mirrored character pairs
            switch (ch)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '{': return '}';
                case '}': return '{';
                case '<': return '>';
                case '>': return '<';
                case '«': return '»';
                case '»': return '«';
                case '\u2039': return '\u203A'; // ‹ → ›
                case '\u203A': return '\u2039'; // › → ‹
                case '\u201C': return '\u201D'; // " → "
                case '\u201D': return '\u201C'; // " → "
                case '\u2018': return '\u2019'; // ' → '
                case '\u2019': return '\u2018'; // ' → '
                case '\u27E8': return '\u27E9'; // ⟨ → ⟩
                case '\u27E9': return '\u27E8'; // ⟩ → ⟨
                case '\u27EA': return '\u27EB'; // ⟪ → ⟫
                case '\u27EB': return '\u27EA'; // ⟫ → ⟪
                case '\u27EC': return '\u27ED'; // ⟬ → ⟭
                case '\u27ED': return '\u27EC'; // ⟭ → ⟬
                case '\u27EE': return '\u27EF'; // ⟮ → ⟯
                case '\u27EF': return '\u27EE'; // ⟯ → ⟮
                // Add more mirrored pairs as needed
                default: return ch;
            }
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