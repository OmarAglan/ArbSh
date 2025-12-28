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
        /// Captures keys manually to control display and cursor positioning.
        /// </summary>
        /// <returns>Input line with proper RTL processing</returns>
        public static string? ReadRTLLine()
        {
            // Reset cursor to known state if needed, though we rely on Redraw
            int startLeft = System.Console.CursorLeft;
            int startTop = System.Console.CursorTop;
            int consoleWidth = System.Console.WindowWidth;

            StringBuilder buffer = new StringBuilder();
            int logicalCursorPos = 0; // 0 means before the first char (logically)

            // We need to keep track of history/navigation eventually, but for now just basic input
            
            while (true)
            {
                // Redraw the line first
                RedrawLine(buffer.ToString(), logicalCursorPos, startTop, consoleWidth);

                ConsoleKeyInfo keyInfo = System.Console.ReadKey(true); // Intercept key

                // Handle Enter
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    System.Console.WriteLine(); // Move to next line
                    return buffer.ToString();
                }
                
                // Handle Backspace
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (logicalCursorPos > 0 && buffer.Length > 0)
                    {
                        buffer.Remove(logicalCursorPos - 1, 1);
                        logicalCursorPos--;
                    }
                    continue;
                }

                // Handle Delete
                if (keyInfo.Key == ConsoleKey.Delete)
                {
                    if (logicalCursorPos < buffer.Length)
                    {
                        buffer.Remove(logicalCursorPos, 1);
                    }
                    continue;
                }

                // Handle Arrows (RTL Logic Swapped!)
                // In RTL Visual:
                // Left Arrow -> Moves VISUALLY Left -> Logically Forward (Next char)
                // Right Arrow -> Moves VISUALLY Right -> Logically Backward (Prev char)
                
                if (keyInfo.Key == ConsoleKey.LeftArrow)
                {
                    if (logicalCursorPos < buffer.Length) logicalCursorPos++;
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.RightArrow)
                {
                    if (logicalCursorPos > 0) logicalCursorPos--;
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.Home)
                {
                    logicalCursorPos = 0; // Logical Start
                    continue;
                }

                if (keyInfo.Key == ConsoleKey.End)
                {
                    logicalCursorPos = buffer.Length; // Logical End
                    continue;
                }
                
                // Handle Normal Characters
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    buffer.Insert(logicalCursorPos, keyInfo.KeyChar);
                    logicalCursorPos++;
                }
            }
        }

        #endregion

        #region Private Methods

        private static void RedrawLine(string logicalText, int logicalCursorPos, int startTop, int consoleWidth)
        {
            // 1. Process text for Display (Shape -> Reorder)
            // This ensures the user sees connected letters while typing!
            string visualText = ConsoleRTLDisplay.ProcessTextForRTLDisplay(logicalText);
            
            // 2. Prepare Prompt
            string prompt = "أربش> "; 
            string processedPrompt = ConsoleRTLDisplay.ProcessTextForRTLDisplay(prompt); // >شبرأ
            
            // 3. Construct Full Line for Display
            // Layout: [Padding] [VisualText] [Prompt]
            // We want the prompt pinned to the right.
            
            int totalContentLength = visualText.Length + processedPrompt.Length;
            int padding = Math.Max(0, consoleWidth - totalContentLength - 1);

            // Clear current line
            System.Console.SetCursorPosition(0, startTop);
            System.Console.Write(new string(' ', consoleWidth - 1));
            System.Console.SetCursorPosition(0, startTop);

            // Write Padding + Text + Prompt
            // Note: VisualText is already reversed (RTL). 
            // So if logical is "ABC", Visual is "CBA".
            // Display: "       CBA >Prompt"
            
            string fullLine = new string(' ', padding) + visualText + processedPrompt;
            System.Console.Write(fullLine);

            // 4. Position Cursor
            // This is the tricky part. We need to map Logical Index -> Visual Index.
            // Simplified approach for pure RTL text:
            // Visual X = Padding + (Length - LogicalIndex)
            // Example: "ABC" (len 3). Cursor at 0 (before A).
            // Visual: "CBA". We want cursor at right of A. 
            // X = Padding + (3 - 0) = Padding + 3. (Right of C, B, A).
            
            // If Mixed text, this simple math fails. 
            // But for Phase 5 Arabic support, let's assume primary RTL flow.
            
            int cursorOffsetFromRight = processedPrompt.Length + (logicalText.Length - logicalCursorPos);
            int cursorLeft = consoleWidth - cursorOffsetFromRight - 1; // -1 for index

            // Clamp cursor
            if (cursorLeft < 0) cursorLeft = 0;
            if (cursorLeft >= consoleWidth) cursorLeft = consoleWidth - 1;

            System.Console.SetCursorPosition(cursorLeft, startTop);
        }
        
        #endregion
    }
}