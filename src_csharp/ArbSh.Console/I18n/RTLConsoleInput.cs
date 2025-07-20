using System;
using System.Text;

namespace ArbSh.Console.I18n
{
    /// <summary>
    /// Implements proper RTL console input handling for Arabic text.
    /// This addresses the fundamental issue that Windows Console input is LTR-only.
    /// </summary>
    public static class RTLConsoleInput
    {
        #region RTL Input Implementation
        
        /// <summary>
        /// Reads a line of input with proper RTL handling for Arabic text.
        /// </summary>
        /// <returns>Input line with proper RTL processing</returns>
        public static string? ReadRTLLine()
        {
            try
            {
                // Get console dimensions
                int consoleWidth = System.Console.WindowWidth;
                int startTop = System.Console.CursorTop;
                
                // Initialize input buffer
                var inputBuffer = new StringBuilder();
                int cursorPosition = 0; // Position within the input text
                
                while (true)
                {
                    // Read a key
                    ConsoleKeyInfo keyInfo = System.Console.ReadKey(true);
                    
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        // Enter pressed - finish input
                        System.Console.WriteLine(); // Move to next line
                        break;
                    }
                    else if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        // Handle backspace
                        if (inputBuffer.Length > 0 && cursorPosition > 0)
                        {
                            inputBuffer.Remove(cursorPosition - 1, 1);
                            cursorPosition--;
                            RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.Delete)
                    {
                        // Handle delete
                        if (cursorPosition < inputBuffer.Length)
                        {
                            inputBuffer.Remove(cursorPosition, 1);
                            RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.LeftArrow)
                    {
                        // Move cursor left (in RTL context, this is forward in text)
                        if (cursorPosition < inputBuffer.Length)
                        {
                            cursorPosition++;
                            RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.RightArrow)
                    {
                        // Move cursor right (in RTL context, this is backward in text)
                        if (cursorPosition > 0)
                        {
                            cursorPosition--;
                            RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.Home)
                    {
                        // Move to beginning of input (rightmost position in RTL)
                        cursorPosition = 0;
                        RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                    }
                    else if (keyInfo.Key == ConsoleKey.End)
                    {
                        // Move to end of input (leftmost position in RTL)
                        cursorPosition = inputBuffer.Length;
                        RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        // Regular character input
                        inputBuffer.Insert(cursorPosition, keyInfo.KeyChar);
                        cursorPosition++;
                        RedrawRTLInput(inputBuffer.ToString(), cursorPosition, consoleWidth, startTop);
                    }
                }
                
                return inputBuffer.ToString();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: RTL input failed: {ex.Message}");
                // Fallback to standard input
                return System.Console.ReadLine();
            }
        }

        /// <summary>
        /// Displays the RTL prompt and positions cursor correctly.
        /// </summary>
        public static void DisplayRTLPrompt()
        {
            try
            {
                int consoleWidth = System.Console.WindowWidth;
                string promptText = "< أربش";
                
                // Calculate right-side positioning
                int promptLength = promptText.Length;
                int padding = Math.Max(0, consoleWidth - promptLength);
                
                // Clear the line and display right-aligned prompt
                System.Console.Write(new string(' ', consoleWidth));
                System.Console.CursorLeft = 0;
                System.Console.Write(new string(' ', padding) + promptText);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: RTL prompt display failed: {ex.Message}");
                System.Console.Write("أربش> ");
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Redraws the RTL input text with proper positioning and cursor placement.
        /// </summary>
        /// <param name="text">Current input text</param>
        /// <param name="cursorPos">Cursor position within the text</param>
        /// <param name="consoleWidth">Console width</param>
        /// <param name="startTop">Starting line position</param>
        private static void RedrawRTLInput(string text, int cursorPos, int consoleWidth, int startTop)
        {
            try
            {
                // Save current cursor position
                int currentTop = System.Console.CursorTop;
                
                // Move to the input line
                System.Console.SetCursorPosition(0, startTop);
                
                // Clear the input area
                System.Console.Write(new string(' ', consoleWidth));
                System.Console.SetCursorPosition(0, startTop);
                
                // Display the prompt
                string promptText = "< أربش";
                int promptLength = promptText.Length;
                
                if (string.IsNullOrEmpty(text))
                {
                    // No input text, just show prompt
                    int padding = Math.Max(0, consoleWidth - promptLength);
                    System.Console.Write(new string(' ', padding) + promptText);
                    
                    // Position cursor at the end of prompt for input
                    System.Console.SetCursorPosition(consoleWidth - 1, startTop);
                }
                else
                {
                    // Display input text in RTL order
                    string displayText = ProcessTextForRTLDisplay(text);
                    int totalLength = promptLength + displayText.Length + 1; // +1 for space
                    
                    if (totalLength <= consoleWidth)
                    {
                        // Text fits on one line
                        int padding = Math.Max(0, consoleWidth - totalLength);
                        System.Console.Write(new string(' ', padding) + displayText + " " + promptText);
                        
                        // Position cursor based on RTL cursor position
                        int visualCursorPos = CalculateRTLCursorPosition(text, cursorPos, consoleWidth, totalLength);
                        System.Console.SetCursorPosition(visualCursorPos, startTop);
                    }
                    else
                    {
                        // Text is too long, handle wrapping or truncation
                        // For now, just display what fits
                        string truncatedText = displayText.Substring(0, Math.Min(displayText.Length, consoleWidth - promptLength - 1));
                        System.Console.Write(truncatedText + " " + promptText);
                        System.Console.SetCursorPosition(consoleWidth - promptLength - 1, startTop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: RTL redraw failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes text for RTL display.
        /// </summary>
        /// <param name="text">Input text</param>
        /// <returns>Text processed for RTL display</returns>
        private static string ProcessTextForRTLDisplay(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // For Arabic text, we may need to apply BiDi processing
            // For now, keep it simple and return as-is
            return text;
        }

        /// <summary>
        /// Calculates the visual cursor position for RTL text.
        /// </summary>
        /// <param name="text">Input text</param>
        /// <param name="logicalPos">Logical cursor position</param>
        /// <param name="consoleWidth">Console width</param>
        /// <param name="totalLength">Total display length</param>
        /// <returns>Visual cursor position</returns>
        private static int CalculateRTLCursorPosition(string text, int logicalPos, int consoleWidth, int totalLength)
        {
            try
            {
                // In RTL, logical position 0 is at the rightmost position
                // Visual position should be calculated from right to left
                
                int padding = Math.Max(0, consoleWidth - totalLength);
                int textStartPos = padding;
                int textLength = text.Length;
                
                // Convert logical position to visual position (RTL)
                int visualPos = textStartPos + (textLength - logicalPos);
                
                return Math.Max(textStartPos, Math.Min(visualPos, consoleWidth - 1));
            }
            catch
            {
                // Fallback to safe position
                return Math.Max(0, consoleWidth - 1);
            }
        }
        
        #endregion
    }
}
