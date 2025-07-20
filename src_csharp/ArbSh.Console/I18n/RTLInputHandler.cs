using System;
using System.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Handles RTL (Right-to-Left) text input for Arabic console applications.
    /// Addresses the issue where Arabic text should be typed from right-to-left
    /// but Windows Console input system works left-to-right.
    /// </summary>
    public static class RTLInputHandler
    {
        #region RTL Input Processing
        
        /// <summary>
        /// Processes console input to handle RTL text entry behavior.
        /// This is a workaround for Windows Console's LTR-only input system.
        /// </summary>
        /// <param name="rawInput">Raw input from console</param>
        /// <returns>Processed input with RTL handling</returns>
        public static string ProcessRTLInput(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput))
            {
                return rawInput;
            }

            // Check if input contains Arabic characters
            bool hasArabic = BiDiTextProcessor.ContainsArabicText(rawInput);
            
            if (!hasArabic)
            {
                // No Arabic content, return as-is
                return rawInput;
            }

            // For Arabic input, we need to handle the RTL nature
            // The console gives us characters in the order they were typed,
            // but for Arabic, this might not be the logical order we want
            
            return ProcessArabicInput(rawInput);
        }

        /// <summary>
        /// Provides RTL input guidance to the user.
        /// </summary>
        /// <returns>Help text for RTL input</returns>
        public static string GetRTLInputHelp()
        {
            return "💡 للنص العربي: اكتب من اليمين إلى اليسار. النظام سيعالج النص تلقائياً.";
        }

        /// <summary>
        /// Displays RTL input instructions if needed.
        /// </summary>
        /// <param name="showHelp">Whether to show RTL input help</param>
        public static void DisplayRTLInputInstructions(bool showHelp = false)
        {
            if (showHelp)
            {
                System.Console.WriteLine(GetRTLInputHelp());
                System.Console.WriteLine();
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Processes Arabic text input to handle RTL characteristics.
        /// </summary>
        /// <param name="arabicInput">Input containing Arabic text</param>
        /// <returns>Processed Arabic input</returns>
        private static string ProcessArabicInput(string arabicInput)
        {
            try
            {
                // For now, we'll keep the input as-is since the console
                // input system provides characters in typing order
                // 
                // The key insight is that for command parsing, we want
                // the logical order (which is what we get from typing),
                // not the visual order
                
                // Normalize the input
                string normalized = arabicInput.Normalize(NormalizationForm.FormC);
                
                // Log for debugging
                if (arabicInput != normalized)
                {
                    System.Console.WriteLine($"DEBUG (RTL Input): Normalized '{arabicInput}' → '{normalized}'");
                }
                
                return normalized;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"WARNING (RTL Input): Arabic input processing failed: {ex.Message}");
                return arabicInput;
            }
        }
        
        #endregion

        #region Console Cursor Management
        
        /// <summary>
        /// Attempts to position the cursor for RTL input.
        /// This is limited by Windows Console API capabilities.
        /// </summary>
        /// <param name="promptLength">Length of the prompt</param>
        public static void PositionCursorForRTL(int promptLength)
        {
            try
            {
                // Get current cursor position
                int currentLeft = System.Console.CursorLeft;
                int currentTop = System.Console.CursorTop;
                
                // For RTL, we might want to position the cursor differently
                // However, Windows Console input system will override this
                // This is more of a visual hint
                
                // For now, we'll leave the cursor where the console puts it
                // since fighting the console input system causes more problems
                
                System.Console.WriteLine($"DEBUG (RTL): Cursor at ({currentLeft}, {currentTop}) after prompt length {promptLength}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG (RTL): Cursor positioning failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Provides visual feedback for RTL input mode.
        /// </summary>
        public static void ShowRTLInputIndicator()
        {
            try
            {
                // Add a subtle RTL indicator
                // This helps users understand they're in RTL input mode
                System.Console.Write("◄"); // RTL arrow indicator
                
                // Move cursor back to overwrite the indicator when typing starts
                if (System.Console.CursorLeft > 0)
                {
                    System.Console.CursorLeft--;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"DEBUG (RTL): RTL indicator failed: {ex.Message}");
            }
        }
        
        #endregion

        #region Input Analysis
        
        /// <summary>
        /// Analyzes input to determine if RTL processing is needed.
        /// </summary>
        /// <param name="input">Input text to analyze</param>
        /// <returns>RTL input analysis results</returns>
        public static RTLInputAnalysis AnalyzeInput(string input)
        {
            var analysis = new RTLInputAnalysis
            {
                OriginalInput = input,
                HasArabicText = BiDiTextProcessor.ContainsArabicText(input),
                HasLatinText = ContainsLatinText(input),
                Length = input?.Length ?? 0
            };

            analysis.IsMixed = analysis.HasArabicText && analysis.HasLatinText;
            analysis.IsRTLPrimary = analysis.HasArabicText && !analysis.HasLatinText;
            analysis.NeedsRTLProcessing = analysis.HasArabicText;

            return analysis;
        }

        /// <summary>
        /// Checks if text contains Latin characters.
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if text contains Latin characters</returns>
        private static bool ContainsLatinText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char c in text)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    return true;
                }
            }

            return false;
        }
        
        #endregion

        #region Supporting Types
        
        /// <summary>
        /// Results of RTL input analysis.
        /// </summary>
        public class RTLInputAnalysis
        {
            public string OriginalInput { get; set; } = string.Empty;
            public bool HasArabicText { get; set; }
            public bool HasLatinText { get; set; }
            public bool IsMixed { get; set; }
            public bool IsRTLPrimary { get; set; }
            public bool NeedsRTLProcessing { get; set; }
            public int Length { get; set; }

            public override string ToString()
            {
                return $"RTL Analysis: Arabic={HasArabicText}, Latin={HasLatinText}, Mixed={IsMixed}, RTL Primary={IsRTLPrimary}";
            }
        }
        
        #endregion
    }
}
