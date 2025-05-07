using System;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Provides functionality related to the Unicode Bidirectional Algorithm (UAX #9).
    /// Ported elements from the original C implementation.
    /// </summary>
    public static class BidiAlgorithm
    {
        // Corresponds to MAX_DEPTH in bidi.h
        private const int MaxDepth = 125; 

        /// <summary>
        /// Determines the bidirectional character type for a given Unicode codepoint.
        /// Based on logic from original C implementation's get_char_type.
        /// </summary>
        /// <param name="codepoint">The Unicode codepoint.</param>
        /// <returns>The BidiCharacterType.</returns>
        public static BidiCharacterType GetCharType(int codepoint)
        {
            // Arabic Letters (includes Presentation Forms)
            if ((codepoint >= 0x0600 && codepoint <= 0x06FF) || // Arabic
                (codepoint >= 0x0750 && codepoint <= 0x077F) || // Arabic Supplement
                (codepoint >= 0x08A0 && codepoint <= 0x08FF) || // Arabic Extended-A
                (codepoint >= 0xFB50 && codepoint <= 0xFDFF) || // Arabic Presentation Forms-A
                (codepoint >= 0xFE70 && codepoint <= 0xFEFF))   // Arabic Presentation Forms-B
            {
                 // Note: UAX#9 classifies Arabic letters as AL.
                 // The original C code used AL only for 0x0600-0x06FF, 0x0750-0x077F, 0x08A0-0x08FF.
                 // For simplicity in porting, we stick to the C code's ranges for now,
                 // but ideally, a full Unicode character database lookup would be better.
                 // Let's match the C code exactly for now.
                 if ((codepoint >= 0x0600 && codepoint <= 0x06FF) ||
                     (codepoint >= 0x0750 && codepoint <= 0x077F) ||
                     (codepoint >= 0x08A0 && codepoint <= 0x08FF))
                 {
                    return BidiCharacterType.AL;
                 }
            }

            // Hebrew Letters
            if (codepoint >= 0x0590 && codepoint <= 0x05FF)
                return BidiCharacterType.R; // Right-to-Left

            // European Numbers (ASCII Digits)
            if (codepoint >= 0x0030 && codepoint <= 0x0039)
                return BidiCharacterType.EN;

            // Arabic Numbers
            if ((codepoint >= 0x0660 && codepoint <= 0x0669) ||   // Arabic-Indic digits
                (codepoint >= 0x06F0 && codepoint <= 0x06F9))     // Extended Arabic-Indic digits
                return BidiCharacterType.AN;

            // Directional Formatting Characters (Explicit Codes)
            switch (codepoint)
            {
                case 0x200E: return BidiCharacterType.LRM; // LEFT-TO-RIGHT MARK
                case 0x200F: return BidiCharacterType.RLM; // RIGHT-TO-LEFT MARK
                case 0x202A: return BidiCharacterType.LRE; // LEFT-TO-RIGHT EMBEDDING
                case 0x202B: return BidiCharacterType.RLE; // RIGHT-TO-LEFT EMBEDDING
                case 0x202C: return BidiCharacterType.PDF; // POP DIRECTIONAL FORMATTING
                case 0x202D: return BidiCharacterType.LRO; // LEFT-TO-RIGHT OVERRIDE
                case 0x202E: return BidiCharacterType.RLO; // RIGHT-TO-LEFT OVERRIDE
                case 0x2066: return BidiCharacterType.LRI; // LEFT-TO-RIGHT ISOLATE
                case 0x2067: return BidiCharacterType.RLI; // RIGHT-TO-LEFT ISOLATE
                case 0x2068: return BidiCharacterType.FSI; // FIRST STRONG ISOLATE
                case 0x2069: return BidiCharacterType.PDI; // POP DIRECTIONAL ISOLATE
            }

            // Whitespace (Common cases)
            // Note: UAX#9 defines more WS characters. This matches the C code.
            if (codepoint == 0x0020 || codepoint == 0x0009 || codepoint == 0x00A0)
                return BidiCharacterType.WS;

            // Paragraph Separator (Common cases)
            // Note: UAX#9 defines more B characters. This matches the C code.
            if (codepoint == 0x000A || codepoint == 0x000D || codepoint == 0x2029)
                return BidiCharacterType.B;

            // Segment Separator (Matches C code)
            // Note: UAX#9 defines S as TAB (0x0009). The C code included 0x001F (Unit Separator).
            // Sticking to C code for porting.
            if (codepoint == 0x0009 || codepoint == 0x001F)
                return BidiCharacterType.S;

            // European Number Separator (+, -)
            if (codepoint == '+' || codepoint == '-')
                 return BidiCharacterType.ES;

            // European Number Terminator (Currency symbols, degree, percent etc.)
            // This is a simplification, UAX#9 is more complex. Matching C code's lack of specific ET handling.

            // Common Number Separator (., ,, :, / etc.)
            // This is a simplification, UAX#9 is more complex. Matching C code's lack of specific CS handling.
            if (codepoint == '.' || codepoint == ',' || codepoint == ':' || codepoint == '/')
                 return BidiCharacterType.CS;


            // Non-Spacing Mark (NSM) - Basic check for common combining marks range
            // This is a vast simplification. A proper implementation needs Unicode database lookup.
            if (codepoint >= 0x0300 && codepoint <= 0x036F) // Combining Diacritical Marks
                 return BidiCharacterType.NSM;


            // Default to LTR for basic Latin/ASCII range (excluding handled types above)
            // This needs refinement based on UAX#9 L type definition.
            // The C code defaulted to L for < 0x80 if not otherwise classified.
            if (codepoint < 0x0080) // Check if it's within ASCII range
            {
                 // Re-check if it was already classified above (e.g., EN, WS, B, S, ES, CS)
                 // This is inefficient, but mirrors the structure. A better way is needed.
                 if (GetCharTypeInternalSimplified(codepoint) == BidiCharacterType.ON) // If not classified yet by simplified checks
                 {
                     return BidiCharacterType.L;
                 }
            }

            // Default to Other Neutral (ON) for everything else not explicitly handled
            // This is a broad generalization.
            return BidiCharacterType.ON;
        }

        // Helper to avoid infinite recursion in the LTR default check
        private static BidiCharacterType GetCharTypeInternalSimplified(int codepoint)
        {
             // Only includes checks *before* the LTR default in the main function
            if ((codepoint >= 0x0600 && codepoint <= 0x06FF) ||
                (codepoint >= 0x0750 && codepoint <= 0x077F) ||
                (codepoint >= 0x08A0 && codepoint <= 0x08FF)) return BidiCharacterType.AL;
            if (codepoint >= 0x0590 && codepoint <= 0x05FF) return BidiCharacterType.R;
            if (codepoint >= 0x0030 && codepoint <= 0x0039) return BidiCharacterType.EN;
            if ((codepoint >= 0x0660 && codepoint <= 0x0669) || (codepoint >= 0x06F0 && codepoint <= 0x06F9)) return BidiCharacterType.AN;
            switch (codepoint) { 
                // Explicit codes LRM..PDI would be handled here if needed, but GetCharType handles them.
                // This helper is only for the LTR default check logic.
                default: break; // Add default break to satisfy CS1522
            } 
            if (codepoint == 0x0020 || codepoint == 0x0009 || codepoint == 0x00A0) return BidiCharacterType.WS;
            if (codepoint == 0x000A || codepoint == 0x000D || codepoint == 0x2029) return BidiCharacterType.B;
            // Note: 0x0009 is both WS and S in C code? Prioritize WS. Let's assume S is only 0x001F here.
            // Sticking to C code for porting.
            if (codepoint == 0x001F) return BidiCharacterType.S;
            if (codepoint == '+' || codepoint == '-') return BidiCharacterType.ES;
            if (codepoint == '.' || codepoint == ',' || codepoint == ':' || codepoint == '/') return BidiCharacterType.CS;
            if (codepoint >= 0x0300 && codepoint <= 0x036F) return BidiCharacterType.NSM;
            return BidiCharacterType.ON; // Default if not matched above
        }

        /// <summary>
        /// Represents a sequence of characters with the same resolved embedding level.
        /// </summary>
        public class BidiRun // Changed from internal to public
        {
            public int Start { get; }  // Start position in original text
            public int Length { get; } // Length of the run in original text
            public int Level { get; }  // Resolved embedding level

            public BidiRun(int start, int length, int level)
            {
                Start = start;
                Length = length;
                Level = level;
            }
        }

        /// <summary>
        /// Processes the input text to determine bidirectional runs based on UAX #9 explicit formatting codes and character types.
        /// Ported from the original C implementation's process_runs function.
        /// Note: This implementation mirrors the C code's logic, which might be a simplified version of the full UAX #9 algorithm.
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <param name="baseLevel">The base paragraph level (0 for LTR, 1 for RTL).</param>
        /// <returns>A list of BidiRun objects representing sequences of characters with the same resolved level.</returns>
        public static List<BidiRun> ProcessRuns(string text, int baseLevel)
        {
            var runs = new List<BidiRun>();
            if (string.IsNullOrEmpty(text))
            {
                return runs;
            }

            int length = text.Length;
            int currentLevel = baseLevel;
            int runStart = 0;
            
            // Stack for embedding levels
            var levelStack = new Stack<int>();
            levelStack.Push(baseLevel);

            // Directional status matching C implementation
            int overrideStatus = -1; // -1: neutral, 0: LTR, 1: RTL
            bool isolateStatus = false; // false: not in isolate, true: in isolate

            for (int i = 0; i < length; /* Index incremented inside loop */)
            {
                // Get Unicode codepoint, handling surrogate pairs
                int codepoint;
                int charLength;
                if (char.IsHighSurrogate(text[i]) && i + 1 < length && char.IsLowSurrogate(text[i + 1]))
                {
                    codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    charLength = 2;
                }
                else
                {
                    codepoint = text[i];
                    charLength = 1;
                }

                BidiCharacterType charType = GetCharType(codepoint);

                int newLevel = currentLevel; // Store potential new level for the *next* run
                bool createNewRun = false;

                // Handle directional formatting characters influencing levels and state
                switch (charType)
                {
                    case BidiCharacterType.LRE: // Left-to-Right Embedding
                        if (levelStack.Count < MaxDepth)
                        {
                            int nextOddLevel = (currentLevel % 2 == 0) ? currentLevel + 1 : currentLevel + 2; // Find next higher odd level
                            if (nextOddLevel > MaxDepth) nextOddLevel = MaxDepth; // Clamp
                            int resolvedLevel = (nextOddLevel + 1) & ~1; // Next even level >= nextOddLevel
                            if (resolvedLevel > MaxDepth) resolvedLevel = MaxDepth; // Clamp

                            levelStack.Push(resolvedLevel);
                            currentLevel = resolvedLevel;
                            overrideStatus = -1; // Embedding cancels override
                            createNewRun = true;
                        }
                        break;

                    case BidiCharacterType.RLE: // Right-to-Left Embedding
                         if (levelStack.Count < MaxDepth)
                        {
                            int nextOddLevel = (currentLevel % 2 == 0) ? currentLevel + 1 : currentLevel + 2; // Find next higher odd level
                            if (nextOddLevel > MaxDepth) nextOddLevel = MaxDepth; // Clamp
                            int resolvedLevel = nextOddLevel | 1; // Ensure odd level
                            if (resolvedLevel > MaxDepth) resolvedLevel = MaxDepth; // Clamp

                            levelStack.Push(resolvedLevel);
                            currentLevel = resolvedLevel;
                            overrideStatus = -1; // Embedding cancels override
                            createNewRun = true;
                        }
                        break;

                    case BidiCharacterType.LRO: // Left-to-Right Override
                        if (levelStack.Count < MaxDepth)
                        {
                            int nextOddLevel = (currentLevel % 2 == 0) ? currentLevel + 1 : currentLevel + 2;
                            if (nextOddLevel > MaxDepth) nextOddLevel = MaxDepth;
                            int resolvedLevel = (nextOddLevel + 1) & ~1; // Next even level
                             if (resolvedLevel > MaxDepth) resolvedLevel = MaxDepth;

                            levelStack.Push(resolvedLevel);
                            currentLevel = resolvedLevel;
                            overrideStatus = 0; // LTR override
                            createNewRun = true;
                        }
                        break;

                    case BidiCharacterType.RLO: // Right-to-Left Override
                        if (levelStack.Count < MaxDepth)
                        {
                            int nextOddLevel = (currentLevel % 2 == 0) ? currentLevel + 1 : currentLevel + 2;
                            if (nextOddLevel > MaxDepth) nextOddLevel = MaxDepth;
                            int resolvedLevel = nextOddLevel | 1; // Ensure odd level
                            if (resolvedLevel > MaxDepth) resolvedLevel = MaxDepth;

                            levelStack.Push(resolvedLevel);
                            currentLevel = resolvedLevel;
                            overrideStatus = 1; // RTL override
                            createNewRun = true;
                        }
                        break;

                    case BidiCharacterType.PDF: // Pop Directional Format
                        if (levelStack.Count > 1) // Can only pop if not the base level
                        {
                            // Check if the popped level was an override/isolate to reset status
                            // (Simplified: C code didn't track type of pushed level, just popped)
                            levelStack.Pop();
                            currentLevel = levelStack.Peek();
                            overrideStatus = -1; // Assume override/isolate ends on pop
                            isolateStatus = false;
                            createNewRun = true;
                        }
                        break;

                    // --- Isolates (Simplified handling based on C code) ---
                    case BidiCharacterType.LRI: // Left-to-Right Isolate
                    case BidiCharacterType.RLI: // Right-to-Left Isolate
                    case BidiCharacterType.FSI: // First Strong Isolate (Treat as RLI for simplicity like C code)
                         if (levelStack.Count < MaxDepth)
                        {
                            // Determine level based on type (LRI=even, RLI/FSI=odd)
                            int resolvedLevel;
                            if (charType == BidiCharacterType.LRI) {
                                int nextOddLevel = (currentLevel % 2 == 0) ? currentLevel + 1 : currentLevel + 2;
                                if (nextOddLevel > MaxDepth) nextOddLevel = MaxDepth;
                                resolvedLevel = (nextOddLevel + 1) & ~1; // Next even level
                                if (resolvedLevel > MaxDepth) resolvedLevel = MaxDepth;
                            } else { // RLI or FSI
                                int nextOddLevel = (currentLevel % 2 == 0) ? currentLevel + 1 : currentLevel + 2;
                                if (nextOddLevel > MaxDepth) nextOddLevel = MaxDepth;
                                resolvedLevel = nextOddLevel | 1; // Ensure odd level
                                if (resolvedLevel > MaxDepth) resolvedLevel = MaxDepth;
                            }

                            levelStack.Push(resolvedLevel);
                            currentLevel = resolvedLevel;
                            overrideStatus = -1; // Isolate cancels override
                            isolateStatus = true; // Mark as inside an isolate
                            createNewRun = true;
                        }
                        break;

                    case BidiCharacterType.PDI: // Pop Directional Isolate
                        // Find the matching isolate initiator on the stack (more complex than C code's simple pop)
                        // Simplified: Pop until isolateStatus is false or stack base is reached.
                        // This doesn't correctly handle nested isolates/embeddings.
                        // Matching the C code's apparent simple pop logic for now:
                        if (isolateStatus && levelStack.Count > 1)
                        {
                            levelStack.Pop();
                            currentLevel = levelStack.Peek();
                            isolateStatus = false; // Assume isolate ends
                            overrideStatus = -1; // Assume override ends too
                            createNewRun = true;
                        }
                        break;

                    // --- Non-Formatting Characters ---
                    default:
                        // Apply override if active
                        BidiCharacterType effectiveCharType = charType;
                        if (overrideStatus == 0) effectiveCharType = BidiCharacterType.L;
                        else if (overrideStatus == 1) effectiveCharType = BidiCharacterType.R; // Treat AL as R under override? Assume yes.

                        // Determine level based on effective character type
                        // This simplified logic only changes level for strong types L, R, AL
                        // It doesn't implement UAX#9 rules W1-W7 for neutrals or N0-N2 for numbers.
                        if (effectiveCharType == BidiCharacterType.L)
                        {
                            newLevel = (currentLevel % 2 == 0) ? currentLevel : currentLevel + 1; // Stay or increase to even
                            if (newLevel > MaxDepth) newLevel = MaxDepth;
                            newLevel &= ~1; // Ensure even
                        }
                        else if (effectiveCharType == BidiCharacterType.R || effectiveCharType == BidiCharacterType.AL)
                        {
                            newLevel = (currentLevel % 2 != 0) ? currentLevel : currentLevel + 1; // Stay or increase to odd
                            if (newLevel > MaxDepth) newLevel = MaxDepth;
                             newLevel |= 1; // Ensure odd
                        }
                        else
                        {
                            // Neutrals, numbers, etc., don't change the level in this simplified model
                            newLevel = currentLevel;
                        }

                        // If the character's natural level differs from the current run level, start new run
                        if (newLevel != currentLevel)
                        {
                             // Create run for previous segment before changing level
                             createNewRun = true;
                             // The *next* run will have 'newLevel', but the run ending *here*
                             // keeps the 'currentLevel'. The level change happens *after* this char.
                             // So, the level used when creating the run below should be 'currentLevel'.
                             // The 'newLevel' calculated here is only used to *trigger* the run break.
                             // We update currentLevel = newLevel *after* creating the run.
                        }
                        break;
                }

                // Create a new run for the preceding segment if needed
                if (createNewRun)
                {
                    if (i > runStart)
                    {
                        runs.Add(new BidiRun(runStart, i - runStart, currentLevel));
                    }
                    runStart = i;
                    // Update currentLevel based on the character that *caused* the break
                    // Re-calculate based on the formatting code, or the strong type level change
                    // This logic needs careful review against UAX #9 - the C code was simpler.
                    // Let's stick to the C code's apparent logic: level changes *after* the formatting code.
                    // For strong types, the level change also happens *after* the character.
                    // Re-evaluating the level based on the character type that triggered the break:
                     switch (charType) {
                         case BidiCharacterType.LRE: case BidiCharacterType.RLE:
                         case BidiCharacterType.LRO: case BidiCharacterType.RLO:
                         case BidiCharacterType.LRI: case BidiCharacterType.RLI: case BidiCharacterType.FSI:
                             currentLevel = levelStack.Peek(); // Level was already pushed
                             break;
                         case BidiCharacterType.PDF: case BidiCharacterType.PDI:
                             currentLevel = levelStack.Peek(); // Level was popped
                             break;
                         default:
                             // If triggered by L/R/AL, update currentLevel to the calculated newLevel
                             if (newLevel != currentLevel) { // Check if it was a strong type change
                                 currentLevel = newLevel;
                             }
                             // Otherwise (neutrals etc.), currentLevel remains unchanged for the next run start
                             break;
                     }
                }

                // Advance index
                i += charLength;
            }

            // Add the final run
            if (length > runStart)
            {
                runs.Add(new BidiRun(runStart, length - runStart, currentLevel));
            }

            return runs;
        }

        /// <summary>
        /// Reorders the text for display according to the calculated bidirectional runs and levels.
        /// Implements UAX #9 Rule L2.
        /// Ported from the original C implementation's reorder_runs function.
        /// </summary>
        /// <param name="originalText">The original input string.</param>
        /// <param name="runs">The list of BidiRun objects calculated by ProcessRuns.</param>
        /// <returns>The reordered string suitable for display.</returns>
        public static string ReorderRunsForDisplay(string originalText, List<BidiRun> runs)
        {
            if (string.IsNullOrEmpty(originalText) || runs == null || runs.Count == 0)
            {
                return originalText ?? string.Empty;
            }

            var outputBuilder = new System.Text.StringBuilder(originalText.Length);
            int maxLevel = 0;

            // Find maximum embedding level
            foreach (var run in runs)
            {
                if (run.Level > maxLevel)
                {
                    maxLevel = run.Level;
                }
            }

            // Process levels from highest to lowest (UAX#9 Rule L2)
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
                            int[] elementIndices = System.Globalization.StringInfo.ParseCombiningCharacters(runText);
                            for (int j = elementIndices.Length - 1; j >= 0; j--)
                            {
                                int startIndex = elementIndices[j];
                                int elementLength = (j == elementIndices.Length - 1) ? runText.Length - startIndex : elementIndices[j + 1] - startIndex;
                                string textElement = runText.Substring(startIndex, elementLength);

                                // Check the type of the first codepoint in the element (sufficient for filtering format codes)
                                int firstCodepoint = char.ConvertToUtf32(textElement, 0);
                                BidiCharacterType charType = GetCharType(firstCodepoint);

                                // Skip explicit directional formatting characters (LRE..PDI)
                                if (charType < BidiCharacterType.LRE || charType > BidiCharacterType.PDI)
                                {
                                    outputBuilder.Append(textElement);
                                }
                            }
                        }
                        else // Even level -> LTR run
                        {
                            // Iterate forwards by text element
                            int[] elementIndices = System.Globalization.StringInfo.ParseCombiningCharacters(runText);
                            for (int j = 0; j < elementIndices.Length; j++)
                            {
                                int startIndex = elementIndices[j];
                                int elementLength = (j == elementIndices.Length - 1) ? runText.Length - startIndex : elementIndices[j + 1] - startIndex;
                                string textElement = runText.Substring(startIndex, elementLength);

                                int firstCodepoint = char.ConvertToUtf32(textElement, 0);
                                BidiCharacterType charType = GetCharType(firstCodepoint);

                                // Skip explicit directional formatting characters (LRE..PDI)
                                if (charType < BidiCharacterType.LRE || charType > BidiCharacterType.PDI)
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

        /// <summary>
        /// Processes an input string using the simplified BiDi algorithm (level resolution and reordering)
        /// and returns the string ordered for display.
        /// </summary>
        /// <param name="text">The input string.</param>
        /// <param name="baseLevel">The base paragraph level (0 for LTR, 1 for RTL).</param>
        /// <returns>The processed string ready for display.</returns>
        public static string ProcessString(string text, int baseLevel)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            List<BidiRun> runs = ProcessRuns(text, baseLevel);
            if (runs.Count == 0)
            {
                // If no runs were generated (e.g., empty input after filtering), return empty or original?
                // Let's filter formatting codes even if no reordering happens.
                // ReorderRunsForDisplay handles filtering.
                 return ReorderRunsForDisplay(text, runs); // Will filter codes even with no runs
            }

            return ReorderRunsForDisplay(text, runs);
        }
    }
}
