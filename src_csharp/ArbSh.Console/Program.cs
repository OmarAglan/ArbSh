using System;
using System.Text; // Required for Encoding

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

            System.Console.WriteLine("Welcome to ArbSh (C# Prototype)!");
            System.Console.WriteLine("Type 'exit' to quit.");

            // TODO: Initialize core components (Parser, Executor, State)

            // Use StreamReader for potentially more reliable UTF-8 reading from redirected stdin
            // Note: System.Console methods/properties need full qualification here.
            using (var stdinReader = new StreamReader(System.Console.OpenStandardInput(), Encoding.UTF8))
            {
                while (true)
                {
                    // TODO: Implement proper prompt handling (e.g., showing current path)
                    // TODO: Integrate BiDi logic for prompt rendering if needed
                    // Only write prompt if input is not redirected (interactive mode)
                    if (!System.Console.IsInputRedirected)
                    {
                        System.Console.Write("ArbSh> ");
                    }

                    // TODO: Implement advanced input reading (history, completion, BiDi input)
                    // Read using the StreamReader
                    string? inputLine = stdinReader.ReadLine();

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
            } // End using stdinReader

            // Only write exit message if not redirected (avoid cluttering captured output)
            if (!System.Console.IsInputRedirected)
            {
                System.Console.WriteLine("Exiting ArbSh.");
            }
            // TODO: Perform any necessary cleanup
        }
    }
}}
