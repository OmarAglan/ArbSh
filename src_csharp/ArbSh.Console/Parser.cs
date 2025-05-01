using System;
using System.Collections.Generic;
using System.Linq; // Needed for Skip

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
            // - Splitting pipeline stages (e.g., by '|')
            // - Identifying command names (verbs/nouns, potentially Arabic)
            // - Parsing parameters and their values (handling quotes, types)
            // - Handling redirection (>, >>, <)
            // - Variable expansion ($var)

            System.Console.WriteLine($"DEBUG (Parser): Parsing '{inputLine}'...");

            var parsedCommands = new List<ParsedCommand>();

            // TODO: Implement splitting by ';' for multiple commands on one line.

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
        /// Tokenizes the input line, respecting double quotes.
        /// (Basic implementation)
        /// </summary>
        private static List<string> TokenizeInput(string inputLine)
        {
            var tokens = new List<string>();
            var currentToken = new System.Text.StringBuilder();
            bool inQuotes = false;
            char? prevChar = null;

            foreach (char c in inputLine)
            {
                if (c == '"')
                {
                    // TODO: Handle escaped quotes (\")
                    inQuotes = !inQuotes;
                    // Don't add the quote itself to the token unless escaped
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    // End of a token
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else
                {
                    currentToken.Append(c);
                }
                prevChar = c;
            }

            // Add the last token if any
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            // TODO: Handle unterminated quotes

            return tokens;
        }
    }
}
