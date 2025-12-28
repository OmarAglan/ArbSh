using System;
using System.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Handles the separation between logical text processing (for parsing) and visual text rendering (for display).
    /// This is crucial for Arabic text where input should remain in logical order for command parsing,
    /// but output should be processed through the BiDi algorithm for proper RTL display.
    /// </summary>
    public static class BiDiTextProcessor
    {
        #region Text Processing Modes
        
        /// <summary>
        /// Defines how text should be processed for different purposes.
        /// </summary>
        public enum TextProcessingMode
        {
            /// <summary>Logical order - for parsing, command matching, and internal processing</summary>
            Logical,
            /// <summary>Visual order - for display output with proper BiDi rendering</summary>
            Visual,
            /// <summary>Auto-detect based on content and context</summary>
            Auto
        }
        
        #endregion

        #region Public Interface
        
        /// <summary>
        /// Processes input text for command parsing and internal logic.
        /// Keeps text in logical order for proper command matching and parsing.
        /// </summary>
        /// <param name="input">Raw input text from console</param>
        /// <returns>Text in logical order suitable for parsing</returns>
        public static string ProcessInputForParsing(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // For input processing, we want to keep the logical order
            // This ensures Arabic commands like "احصل-مساعدة" are parsed correctly
            
            // Normalize whitespace and trim
            string normalized = NormalizeWhitespace(input);
            
            return normalized;
        }

        /// <summary>
        /// Processes output text for display with proper BiDi rendering.
        /// Applies BiDi algorithm for correct RTL text presentation.
        /// </summary>
        /// <param name="output">Text to be displayed</param>
        /// <param name="forceRTL">Force RTL processing even for mixed content</param>
        /// <returns>Text processed for visual display</returns>
        public static string ProcessOutputForDisplay(string output, bool forceRTL = false)
        {
            if (string.IsNullOrEmpty(output))
            {
                return output;
            }

            // Check if the text contains Arabic content
            bool hasArabic = ContainsArabicText(output);
            
            if (!hasArabic && !forceRTL)
            {
                // No Arabic content, return as-is
                return output;
            }

            // Apply full BiDi algorithm processing using BidiAlgorithm.ProcessString
            string processed = ApplyFullBiDiProcessing(output);
            
            return processed;
        }

        /// <summary>
        /// Determines if text contains Arabic characters.
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>True if text contains Arabic characters</returns>
        public static bool ContainsArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char c in text)
            {
                // Check for Arabic Unicode ranges
                if ((c >= 0x0600 && c <= 0x06FF) ||  // Arabic
                    (c >= 0x0750 && c <= 0x077F) ||  // Arabic Supplement
                    (c >= 0x08A0 && c <= 0x08FF) ||  // Arabic Extended-A
                    (c >= 0xFB50 && c <= 0xFDFF) ||  // Arabic Presentation Forms-A
                    (c >= 0xFE70 && c <= 0xFEFF))    // Arabic Presentation Forms-B
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Analyzes text directionality and provides processing recommendations.
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Analysis results with processing recommendations</returns>
        public static TextAnalysis AnalyzeText(string text)
        {
            var analysis = new TextAnalysis
            {
                OriginalText = text ?? string.Empty,
                Length = text?.Length ?? 0,
                HasArabicText = ContainsArabicText(text ?? string.Empty),
                HasLatinText = ContainsLatinText(text ?? string.Empty),
                IsMixed = false,
                RecommendedMode = TextProcessingMode.Logical
            };

            if (analysis.HasArabicText && analysis.HasLatinText)
            {
                analysis.IsMixed = true;
                analysis.RecommendedMode = TextProcessingMode.Visual; // Mixed content needs BiDi processing
            }
            else if (analysis.HasArabicText)
            {
                analysis.RecommendedMode = TextProcessingMode.Visual; // Pure Arabic needs RTL processing
            }

            return analysis;
        }

        #endregion

        #region Private Methods
        
        /// <summary>
        /// Normalizes whitespace in text while preserving Arabic text structure.
        /// </summary>
        /// <param name="text">Text to normalize</param>
        /// <returns>Normalized text</returns>
        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Trim leading and trailing whitespace
            text = text.Trim();
            
            // Normalize internal whitespace (collapse multiple spaces to single space)
            var normalized = new StringBuilder();
            bool lastWasSpace = false;
            
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace)
                    {
                        normalized.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    normalized.Append(c);
                    lastWasSpace = false;
                }
            }
            
            return normalized.ToString();
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

        /// <summary>
        /// Applies full BiDi algorithm processing for RTL text display.
        /// Uses the complete UAX #9 implementation from BidiAlgorithm.cs.
        /// </summary>
        /// <param name="text">Text to process</param>
        /// <returns>Text processed for RTL display using full BiDi algorithm</returns>
        private static string ApplyFullBiDiProcessing(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            try
            {
                // Use auto-detection for base level (-1 means auto-detect)
                // The BidiAlgorithm will determine if the text should be LTR or RTL
                string processed = BidiAlgorithm.ProcessString(text, -1);

                return processed;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"WARNING (BiDi): BiDi processing failed: {ex.Message}");
                // Fallback to original text if BiDi processing fails
                return text;
            }
        }
        
        #endregion

        #region Supporting Types
        
        /// <summary>
        /// Results of text analysis for directionality and processing recommendations.
        /// </summary>
        public class TextAnalysis
        {
            public string OriginalText { get; set; } = string.Empty;
            public int Length { get; set; }
            public bool HasArabicText { get; set; }
            public bool HasLatinText { get; set; }
            public bool IsMixed { get; set; }
            public TextProcessingMode RecommendedMode { get; set; }

            public override string ToString()
            {
                return $"Text Analysis: Length={Length}, Arabic={HasArabicText}, Latin={HasLatinText}, Mixed={IsMixed}, Mode={RecommendedMode}";
            }
        }
        
        #endregion
    }
}
