using System;
using System.Collections.Generic;
using System.Linq; // Needed for Skip
using System.Text; // Needed for StringBuilder

namespace ArbSh.Console
{
    /// <summary>
    /// Responsible for parsing user input strings into executable commands/structures.
    /// (Placeholder implementation)
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Parses a line of input into a list of command representations.
        /// (Placeholder - needs actual parsing logic)
        /// </summary>
        /// <param name="inputLine">The raw input line from the user.</param>
        /// <returns>A list of ParsedCommand objects representing the commands in the input line.</returns>
        public static List<ParsedCommand> Parse(string inputLine)
        {
            // TODO: Implement actual parsing logic.
            // This should handle:
            // - Splitting commands (e.g., by ';')
            // - Identifying command names (verbs/nouns, potentially Arabic)
            // - Parsing parameters and their values (handling types)
            // - Handling redirection (>, >>, <)
            // - Variable expansion ($var)

            System.Console.WriteLine($"DEBUG (Parser): Parsing '{inputLine}'...");

            var parsedCommands = new List<ParsedCommand>();

            // Split the input line into pipeline stages based on '|'
            // TODO: This basic split doesn't handle quoted '|' characters.
            string[] pipelineStages = inputLine.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (string stageInput in pipelineStages)
            {
                List<string> tokens = TokenizeInput(stageInput);

                if (tokens.Count > 0)
                {
                    string commandName = tokens[0];
                    List<string> potentialArgsAndParams = tokens.Skip(1).ToList();

                    List<string> arguments = new List<string>();
                    Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // Basic parameter parsing loop (using tokenized input)
                    for (int i = 0; i < potentialArgsAndParams.Count; i++)
                    {
                        string currentToken = potentialArgsAndParams[i];

                        if (currentToken.StartsWith("-"))
                        {
                            string paramName = currentToken;
                            string? paramValue = null;

                            if (i + 1 < potentialArgsAndParams.Count && !potentialArgsAndParams[i + 1].StartsWith("-"))
                            {
                                paramValue = potentialArgsAndParams[i + 1];
                                i++;
                            }
                            parameters[paramName] = paramValue ?? string.Empty;
                        }
                        else
                        {
                            arguments.Add(currentToken);
                        }
                    }
                    parsedCommands.Add(new ParsedCommand(commandName, arguments, parameters));
                }
            } // End foreach pipeline stage

            System.Console.WriteLine($"DEBUG (Parser): Parsed into {parsedCommands.Count} command(s) / pipeline stage(s).");
            return parsedCommands;
        }

        /// <summary>
        /// Tokenizes the input line, respecting double quotes and handling escaped quotes.
        /// (Basic implementation)
        /// </summary>
        private static List<string> TokenizeInput(string inputLine)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < inputLine.Length; i++)
            {
                char c = inputLine[i];

                if (c == '\\' && i + 1 < inputLine.Length)
                {
                    // Handle potential escape sequence
                    char nextChar = inputLine[i + 1];
                    if (nextChar == '"' || nextChar == '\\')
                    {
                        // It's an escaped quote or backslash, add the escaped char
                        currentToken.Append(nextChar);
                        i++; // Skip the next character as it was part of the escape sequence
                        continue; // Continue to next character in loop
                    }
                    // If it's not an escaped quote or backslash, treat backslash literally
                    currentToken.Append(c); // Append the backslash itself
                }
                else if (c == '"')
                {
                    inQuotes = !inQuotes;
                    // Don't add the quote itself to the token
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    // End of a token if outside quotes
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else
                {
                    // Append character if it's part of a token or inside quotes
                    currentToken.Append(c);
                }
            }

            // Add the last token if any
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            // TODO: Handle unterminated quotes (e.g., throw an error)
            if (inQuotes)
            {
                 System.Console.WriteLine("WARN (Tokenizer): Unterminated quote detected.");
                 // Decide how to handle this - error or treat as literal?
            }

            return tokens;
        }
    }
}
