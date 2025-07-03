using System;
using System.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Handles RTL (Right-to-Left) display formatting for console output.
    /// Addresses Windows Console limitations with Arabic text positioning and character shaping.
    /// </summary>
    public static class ConsoleRTLDisplay
    {
        #region Console Properties
        
        /// <summary>
        /// Gets the current console width for RTL positioning calculations.
        /// </summary>
        private static int ConsoleWidth
        {
            get
            {
                try
                {
                    return System.Console.WindowWidth;
                }
                catch
                {
                    // Fallback if console width cannot be determined
                    return 80;
                }
            }
        }
        
        #endregion

        #region RTL Prompt Handling
        
        /// <summary>
        /// Formats and displays the shell prompt with proper RTL positioning.
        /// For Arabic context, we need to balance RTL appearance with console input behavior.
        /// </summary>
        /// <param name="promptText">The prompt text (e.g., "ArbSh>")</param>
        /// <param name="forceRTL">Force RTL positioning even for non-Arabic prompts</param>
        public static void DisplayRTLPrompt(string promptText, bool forceRTL = false)
        {
            if (string.IsNullOrEmpty(promptText))
            {
                return;
            }

            // Check if we should use RTL positioning
            bool useRTL = forceRTL || BiDiTextProcessor.ContainsArabicText(promptText);

            if (useRTL)
            {
                DisplayRTLAwarePrompt(promptText);
            }
            else
            {
                // Standard left-aligned prompt for LTR content
                System.Console.Write(promptText);
            }
        }

        /// <summary>
        /// Displays an RTL-aware prompt that works properly with console input behavior.
        /// For Arabic prompts, we want RTL display but cursor positioning that works with console input.
        /// </summary>
        /// <param name="promptText">The prompt text to display</param>
        private static void DisplayRTLAwarePrompt(string promptText)
        {
            try
            {
                // For Arabic prompts, we need a hybrid approach:
                // 1. Process the Arabic text for proper display
                // 2. Position it appropriately for RTL context
                // 3. Ensure cursor ends up in the right place for input

                if (BiDiTextProcessor.ContainsArabicText(promptText))
                {
                    // Process Arabic prompt for RTL display
                    string processedPrompt = ProcessTextForRTLDisplay(promptText);

                    // For Arabic prompts, add some right-alignment but not full width
                    // This creates a more natural RTL feel while keeping cursor behavior
                    int consoleWidth = ConsoleWidth;
                    int promptLength = GetDisplayLength(processedPrompt);

                    if (consoleWidth > 40 && promptLength < 20)
                    {
                        // Add moderate right padding for RTL feel
                        int padding = Math.Max(0, consoleWidth - promptLength - 20);
                        string paddedPrompt = new string(' ', padding) + processedPrompt;
                        System.Console.Write(paddedPrompt);
                    }
                    else
                    {
                        // Just display the processed prompt normally
                        System.Console.Write(processedPrompt);
                    }
                }
                else
                {
                    // Non-Arabic prompt, display normally
                    System.Console.Write(promptText);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG (RTL): RTL prompt display failed: {ex.Message}");
                // Fallback to normal prompt display
                System.Console.Write(promptText);
            }
        }
        
        #endregion

        #region Arabic Character Shaping
        
        /// <summary>
        /// Applies Arabic character shaping and connection rules for better display.
        /// This addresses the issue where Arabic characters appear disconnected.
        /// </summary>
        /// <param name="arabicText">Arabic text to shape</param>
        /// <returns>Text with improved Arabic character shaping</returns>
        public static string ShapeArabicText(string arabicText)
        {
            if (string.IsNullOrEmpty(arabicText))
            {
                return arabicText;
            }

            // Check if text contains Arabic characters
            if (!BiDiTextProcessor.ContainsArabicText(arabicText))
            {
                return arabicText;
            }

            try
            {
                // Apply basic Arabic character shaping
                // This is a simplified implementation - full shaping requires complex rules
                string shaped = ApplyBasicArabicShaping(arabicText);
                
                System.Console.WriteLine($"DEBUG (RTL): Arabic shaping: '{arabicText}' → '{shaped}'");
                
                return shaped;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"WARNING (RTL): Arabic shaping failed: {ex.Message}");
                return arabicText;
            }
        }

        /// <summary>
        /// Applies basic Arabic character shaping rules.
        /// This is a simplified implementation focusing on common connection issues.
        /// </summary>
        /// <param name="text">Text to shape</param>
        /// <returns>Shaped text</returns>
        private static string ApplyBasicArabicShaping(string text)
        {
            // For now, we'll focus on ensuring proper Unicode normalization
            // Full Arabic shaping requires complex contextual analysis
            
            // Normalize the text to ensure proper Unicode form
            string normalized = text.Normalize(NormalizationForm.FormC);
            
            // TODO: Implement full Arabic shaping engine
            // This would include:
            // - Contextual character form selection (isolated, initial, medial, final)
            // - Ligature formation
            // - Diacritic positioning
            
            return normalized;
        }
        
        #endregion

        #region RTL Text Display
        
        /// <summary>
        /// Displays text with proper RTL formatting and positioning.
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="rightAlign">Whether to right-align the text</param>
        public static void DisplayRTLText(string text, bool rightAlign = true)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Process text for display (BiDi + shaping)
            string processedText = ProcessTextForRTLDisplay(text);
            
            if (rightAlign && BiDiTextProcessor.ContainsArabicText(text))
            {
                DisplayRightAlignedText(processedText);
            }
            else
            {
                System.Console.WriteLine(processedText);
            }
        }

        /// <summary>
        /// Processes text for RTL display with BiDi and shaping.
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <returns>Text ready for RTL display</returns>
        public static string ProcessTextForRTLDisplay(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Step 1: Apply BiDi processing for proper text ordering
            string bidiProcessed = BiDiTextProcessor.ProcessOutputForDisplay(text);
            
            // Step 2: Apply Arabic character shaping
            string shaped = ShapeArabicText(bidiProcessed);
            
            return shaped;
        }

        /// <summary>
        /// Displays text with right alignment.
        /// </summary>
        /// <param name="text">Text to display</param>
        private static void DisplayRightAlignedText(string text)
        {
            try
            {
                string[] lines = text.Split('\n');
                
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        System.Console.WriteLine();
                        continue;
                    }

                    int lineLength = GetDisplayLength(line);
                    int consoleWidth = ConsoleWidth;
                    
                    if (lineLength < consoleWidth)
                    {
                        int padding = consoleWidth - lineLength;
                        string paddedLine = new string(' ', padding) + line;
                        System.Console.WriteLine(paddedLine);
                    }
                    else
                    {
                        System.Console.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG (RTL): Right alignment failed: {ex.Message}");
                System.Console.WriteLine(text);
            }
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Gets the display length of text, accounting for Unicode characters.
        /// </summary>
        /// <param name="text">Text to measure</param>
        /// <returns>Display length</returns>
        private static int GetDisplayLength(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            // Simple implementation - count characters
            // TODO: Improve to handle combining characters, wide characters, etc.
            return text.Length;
        }

        /// <summary>
        /// Determines if RTL display mode should be used based on text content.
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>True if RTL display should be used</returns>
        public static bool ShouldUseRTLDisplay(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            // Use RTL display if text contains Arabic characters
            return BiDiTextProcessor.ContainsArabicText(text);
        }
        
        #endregion
    }
}
