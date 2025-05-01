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
        /// Parses a line of input into a list of command representations for a single pipeline.
        /// </summary>
        /// <param name="inputLine">The raw input line from the user.</param>
        /// <returns>A list of ParsedCommand objects representing the commands in the pipeline.</returns>
        public static List<ParsedCommand> Parse(string inputLine)
        {
            System.Console.WriteLine($"DEBUG (Parser): Parsing '{inputLine}'...");
            var parsedPipeline = new List<ParsedCommand>();

            // Split into potential statements separated by ';'
            // TODO: This basic split doesn't handle quoted ';' characters.
            // TODO: Executor needs updating to handle multiple statements.
            string[] statements = inputLine.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (statements.Length == 0)
            {
                return parsedPipeline; // Return empty list if input was empty or just semicolons
            }

            // --- Process only the FIRST statement for now ---
            string firstStatement = statements[0];
            System.Console.WriteLine($"DEBUG (Parser): Processing statement: '{firstStatement}'");
            if (statements.Length > 1)
            {
                 System.Console.WriteLine($"WARN (Parser): Multiple statements detected (';'), only processing the first one currently.");
            }

            // Split the first statement into pipeline stages based on '|'
            // TODO: This basic split doesn't handle quoted '|' characters.
            string[] pipelineStages = firstStatement.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (string stageInput in pipelineStages)
            {
                // Tokenize the current pipeline stage input
                List<string> tokens = TokenizeInput(stageInput);
                if (tokens.Count == 0) continue;

                string commandName = tokens[0];
                List<string> commandTokens = tokens.Skip(1).ToList(); // Tokens after command name
                string? redirectPath = null;
                bool append = false;

                // Find redirection operator and path, separating them from command tokens
                int redirectOperatorIndex = -1;
                for (int i = 0; i < commandTokens.Count; i++)
                {
                    // Check for redirection operators ONLY if not escaped
                    if ((commandTokens[i] == ">" || commandTokens[i] == ">>") && (i == 0 || commandTokens[i-1] != "\\")) // Basic check for preceding escape
                    {
                        redirectOperatorIndex = i;
                        append = (commandTokens[i] == ">>");
                        if (i + 1 < commandTokens.Count)
                        {
                            redirectPath = commandTokens[i + 1];
                            // Remove the operator and path from commandTokens
                            commandTokens.RemoveRange(i, 2);
                        }
                        else
                        {
                            System.Console.WriteLine($"WARN (Parser): Redirection operator '{commandTokens[i]}' found without a target path.");
                            // Remove just the operator
                            commandTokens.RemoveAt(i);
                        }
                        // TODO: Handle multiple redirection operators? For now, only first is processed.
                        break;
                    }
                }

                // Now parse the remaining commandTokens into arguments and parameters
                List<string> arguments = new List<string>();
                Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < commandTokens.Count; i++)
                {
                    string currentToken = commandTokens[i];
                    if (currentToken.StartsWith("-") && currentToken.Length > 1) // Ensure it's not just "-"
                    {
                        string paramName = currentToken;
                        string? paramValue = null;
                        if (i + 1 < commandTokens.Count && !commandTokens[i + 1].StartsWith("-"))
                        {
                            paramValue = commandTokens[i + 1];
                            i++;
                        }
                        parameters[paramName] = paramValue ?? string.Empty;
                    }
                    else
                    {
                        arguments.Add(currentToken);
                    }
                }

                // Create the command object and set redirection if found
                var parsedCommand = new ParsedCommand(commandName, arguments, parameters);
                if (redirectPath != null)
                {
                    parsedCommand.SetOutputRedirection(redirectPath, append);
                    System.Console.WriteLine($"DEBUG (Parser): Found redirection: {(append ? ">>" : ">")} '{redirectPath}'");
                }
                parsedPipeline.Add(parsedCommand);

            } // End foreach pipeline stage

            System.Console.WriteLine($"DEBUG (Parser): Parsed into {parsedPipeline.Count} pipeline stage(s).");
            return parsedPipeline;
        }

        /// <summary>
        /// Tokenizes the input line, respecting double quotes and handling escaped quotes/operators.
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
                    // Handle potential escape sequence for specific characters
                    char nextChar = inputLine[i + 1];
                    bool escaped = false;
                    switch (nextChar)
                    {
                        case '"':
                        case '\\':
                        case '|':
                        case ';':
                        case '>':
                        // Potentially add '<' later for input redirection
                            currentToken.Append(nextChar);
                            i++; // Consume the escaped character
                            escaped = true;
                            break;
                    }
                    if (escaped) continue;

                    // If not escaping a special character, treat backslash literally
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
