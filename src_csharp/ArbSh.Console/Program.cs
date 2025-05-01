using System;

namespace ArbSh.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Welcome to ArbSh (C# Prototype)!");
            System.Console.WriteLine("Type 'exit' to quit.");

            // TODO: Initialize core components (Parser, Executor, State)

            while (true)
            {
                // TODO: Implement proper prompt handling (e.g., showing current path)
                // TODO: Integrate BiDi logic for prompt rendering if needed
                System.Console.Write("ArbSh> ");

                // TODO: Implement advanced input reading (history, completion, BiDi input)
                string? inputLine = System.Console.ReadLine();

                // Handle EOF (Ctrl+Z on Windows, Ctrl+D on Unix)
                if (inputLine == null)
                {
                    System.Console.WriteLine(); // Newline after EOF
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
                    System.Console.WriteLine($"DEBUG: Received command: {inputLine}");

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
                    System.Console.WriteLine($"Feature not implemented: {nie.Message}");
                    System.Console.ResetColor();
                }
                catch (Exception ex)
                {
                    // Generic error handling
                    // TODO: Implement richer error records like PowerShell
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"ERROR: {ex.Message}");
                    // System.Console.WriteLine(ex.StackTrace); // Optional: for debugging
                    System.Console.ResetColor();
                }
            }

            System.Console.WriteLine("Exiting ArbSh.");
            // TODO: Perform any necessary cleanup
        }
    }
}
