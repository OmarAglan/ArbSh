using System;
using System.Text; // Required for Encoding
using ArbSh.Console.I18n; // For Arabic console input support

namespace ArbSh.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ensure console input is read as UTF-8 to handle Arabic characters correctly
            System.Console.InputEncoding = System.Text.Encoding.UTF8;
            // Optionally set output encoding too, though UTF-8 is often the default
            System.Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure output is also UTF-8

            System.Console.WriteLine("مرحباً بكم في أربش (النموذج الأولي)!");
            System.Console.WriteLine("اكتب 'exit' للخروج.");

            // Initialize Arabic-aware console input system
            ArabicConsoleInput.Initialize(ArabicConsoleInput.InputStrategy.Auto);

            // Display console configuration for debugging
            if (args.Length > 0 && args[0] == "--debug-console")
            {
                System.Console.WriteLine(ArabicConsoleInput.GetInputInfo());
                System.Console.WriteLine();
            }

            // TODO: Initialize core components (Parser, Executor, State)

            try
            {
                while (true)
                {
                    // Display Arabic RTL prompt for interactive mode
                    if (!System.Console.IsInputRedirected)
                    {
                        // Use Arabic prompt "أربش>" for proper RTL display
                        ConsoleRTLDisplay.DisplayRTLPrompt("أربش> ");
                    }

                    // Use Arabic-aware console input instead of StreamReader
                    string? inputLine = ArabicConsoleInput.ReadLine();

                    // Handle EOF (Ctrl+Z on Windows, Ctrl+D on Unix / end of redirected input)
                    if (inputLine == null)
                    {
                        // Avoid writing extra newline if input was redirected
                        if (!System.Console.IsInputRedirected)
                        {
                            System.Console.WriteLine(); // Newline after EOF in interactive mode
                        }
                        break;
                    }

                    // Trim whitespace
                    inputLine = inputLine.Trim();

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(inputLine))
                    {
                        continue;
                    }

                    // Basic exit command
                    if (inputLine.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    try
                    {
                        // --- Placeholder for Parsing and Execution ---
                        string debugMessage = $"DEBUG: Received command: {inputLine}";
                        ConsoleRTLDisplay.DisplayRTLText(debugMessage, rightAlign: false);

                        // 1. Parse the inputLine into commands/cmdlets and arguments
                        //    - TODO: Implement real parsing in Parser.cs
                        //    - TODO: Handle Arabic commands in Parser.cs
                        var commands = Parser.Parse(inputLine);

                        // 2. Execute the parsed commands through the pipeline engine
                        //    - TODO: Implement real execution in Executor.cs
                        //    - TODO: Instantiate real CmdletBase derived classes.
                        //    - TODO: Manage real pipeline object flow.
                        Executor.Execute(commands);

                        // --- End Placeholder ---
                    }
                    catch (NotImplementedException nie)
                    {
                         // Catch specific exceptions for features not yet implemented
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        string warningMessage = $"Feature not implemented: {nie.Message}";
                        string processedWarning = BiDiTextProcessor.ProcessOutputForDisplay(warningMessage);
                        System.Console.WriteLine(processedWarning);
                        System.Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        // Generic error handling
                        // TODO: Implement richer error records like PowerShell
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        string errorMessage = $"ERROR: {ex.Message}";
                        string processedError = BiDiTextProcessor.ProcessOutputForDisplay(errorMessage);
                        System.Console.WriteLine(processedError);
                        // System.Console.WriteLine(ex.StackTrace); // Optional: for debugging
                        System.Console.ResetColor();
                    }
                } // End main loop
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine($"FATAL ERROR: {ex.Message}");
                System.Console.ResetColor();
            }
            finally
            {
                // Clean up Arabic console input resources
                ArabicConsoleInput.Cleanup();

                // Only write exit message if not redirected (avoid cluttering captured output)
                if (!System.Console.IsInputRedirected)
                {
                    System.Console.WriteLine("Exiting ArbSh.");
                }
            }
        }
    }
}
