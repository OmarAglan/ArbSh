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
        // Placeholder for variable storage (replace with proper session state later)
        // TODO: Implement proper session state for variables
        private static Dictionary<string, string> _variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "testVar", "Value from $testVar!" },
            { "pathExample", "C:\\Users" },
            { "emptyVar", "" }
        };

        // Basic variable lookup (replace with proper session state access)
        private static string GetVariableValue(string variableName)
        {
            // Basic lookup, returns empty string if not found (like PowerShell)
            return _variables.TryGetValue(variableName, out var value) ? value : string.Empty;
        }


        /// <summary>
        /// Parses a line of input into a list of statements, where each statement is a list of commands for a pipeline.
        /// </summary>
        /// <param name="inputLine">The raw input line from the user.</param>
        /// <returns>A list where each element is a list of ParsedCommand objects representing a single statement's pipeline.</returns>
        public static List<List<ParsedCommand>> Parse(string inputLine)
        {
            System.Console.WriteLine($"DEBUG (Parser): Parsing '{inputLine}'...");
            var allStatementsCommands = new List<List<ParsedCommand>>();
            var statementBuilder = new StringBuilder();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < inputLine.Length; i++)
            {
                char c = inputLine[i];

                if (c == '\\' && i + 1 < inputLine.Length)
                {
                    // Skip the escaped character
                    i++;
                    continue; // Skip escape character logic for statement splitting
                }

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }

                if (c == ';' && !inQuotes)
                {
                    // Found a statement separator
                    string statement = inputLine.Substring(start, i - start).Trim();
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        allStatementsCommands.Add(ParseSingleStatement(statement));
                    }
                    start = i + 1; // Start next statement after the semicolon
                }
            }

            // Add the last statement (or the only statement if no semicolons)
            string lastStatement = inputLine.Substring(start).Trim();
            if (!string.IsNullOrWhiteSpace(lastStatement))
            {
                allStatementsCommands.Add(ParseSingleStatement(lastStatement));
            }

            // TODO: Handle unterminated quotes at the statement level?

            System.Console.WriteLine($"DEBUG (Parser): Parsed into {allStatementsCommands.Count} statement(s).");
            // TODO: Executor currently only processes the first statement list.
            return allStatementsCommands;
        }

        /// <summary>
        /// Parses a single statement (which might contain a pipeline) into a list of commands.
        /// </summary>
        private static List<ParsedCommand> ParseSingleStatement(string statementInput)
        {
            System.Console.WriteLine($"DEBUG (Parser): Processing statement: '{statementInput}'");
            var commandsInStatement = new List<ParsedCommand>();
            int stageStart = 0;
            bool inQuotes = false; // Track quotes specifically for pipeline splitting

            for (int i = 0; i < statementInput.Length; i++)
            {
                char c = statementInput[i];

                if (c == '\\' && i + 1 < statementInput.Length)
                {
                    // Skip escaped character
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }

                if (c == '|' && !inQuotes)
                {
                    // Found a pipeline separator
                    string stageInput = statementInput.Substring(stageStart, i - stageStart).Trim();
                    if (!string.IsNullOrWhiteSpace(stageInput))
                    {
                        // Process this stage
                        ParsedCommand? command = ParseSinglePipelineStage(stageInput);
                        if (command != null) commandsInStatement.Add(command);
                    }
                    stageStart = i + 1; // Start next stage after the pipe
                }
            }

            // Process the last stage
            string lastStageInput = statementInput.Substring(stageStart).Trim();
            if (!string.IsNullOrWhiteSpace(lastStageInput))
            {
                ParsedCommand? command = ParseSinglePipelineStage(lastStageInput);
                if (command != null) commandsInStatement.Add(command);
            }

            // TODO: Handle unterminated quotes within the statement?

            System.Console.WriteLine($"DEBUG (Parser): Statement parsed into {commandsInStatement.Count} pipeline stage(s).");
            return commandsInStatement;
        }

        /// <summary>
        /// Parses a single pipeline stage string into a ParsedCommand object.
        /// Handles tokenization, redirection, arguments, parameters, and variable expansion for the stage.
        /// </summary>
        /// <param name="stageInput">The string representing a single pipeline stage.</param>
        /// <returns>A ParsedCommand object, or null if the stage is empty or invalid.</returns>
        private static ParsedCommand? ParseSinglePipelineStage(string stageInput)
        {
             // Tokenize the current pipeline stage input (respects quotes/escapes)
                List<string> tokens = TokenizeInput(stageInput);
                if (tokens.Count == 0) return null; // If stage is empty after tokenization, return null

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
                    string expandedToken = currentToken; // Start with original

                    // Variable expansion is now handled during tokenization.
                    // The expandedToken is just the token received from the tokenizer.

                    if (currentToken.StartsWith("-") && currentToken.Length > 1) // Check for parameter name
                    {
                        string paramName = currentToken; // Parameter name itself is not expanded
                        string? paramValue = null;

                        if (i + 1 < commandTokens.Count && !commandTokens[i + 1].StartsWith("-"))
                        {
                            string potentialValueToken = commandTokens[i + 1];
                            // Variable expansion is now handled during tokenization.
                            string expandedValueToken = potentialValueToken;

                            paramValue = expandedValueToken; // Assign the token value
                            i++; // Consume the value token
                        }
                        // Use the *original* token (before expansion) as the parameter name key
                        parameters[paramName] = paramValue ?? string.Empty;
                    }
                    else // It's an argument
                    {
                        // Use the potentially expanded token
                        arguments.Add(expandedToken);
                    }
                }

                // Create the command object and set redirection if found
                var parsedCommand = new ParsedCommand(commandName, arguments, parameters);
                if (redirectPath != null)
                {
                    parsedCommand.SetOutputRedirection(redirectPath, append);
                    System.Console.WriteLine($"DEBUG (Parser): Found redirection: {(append ? ">>" : ">")} '{redirectPath}'");
                }
                return parsedCommand;
            // Return null if stage was empty after tokenization
            return null;
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
            bool treatNextCharLiteral = false; // Flag for escape character

            for (int i = 0; i < inputLine.Length; i++)
            {
                char c = inputLine[i];

                if (treatNextCharLiteral)
                {
                    // Previous char was '\', add this char literally
                    currentToken.Append(c);
                    treatNextCharLiteral = false;
                    continue;
                }

                // Allow escaping inside quotes as well
                if (c == '\\' && i + 1 < inputLine.Length)
                {
                    // Set flag to treat the *next* character literally
                    treatNextCharLiteral = true;
                    // Do not append the backslash itself
                    continue;
                }

                // Handle variable expansion ($) only outside quotes
                if (c == '$' && !inQuotes && i + 1 < inputLine.Length)
                {
                    int varNameStart = i + 1;
                    int varNameEnd = varNameStart;
                    // Basic variable name: letters, numbers, underscore (can be refined)
                    while (varNameEnd < inputLine.Length && (char.IsLetterOrDigit(inputLine[varNameEnd]) || inputLine[varNameEnd] == '_'))
                    {
                        varNameEnd++;
                    }

                    if (varNameEnd > varNameStart) // Found a potential variable name
                    {
                        string varName = inputLine.Substring(varNameStart, varNameEnd - varNameStart);
                        string varValue = GetVariableValue(varName);
                        currentToken.Append(varValue); // Append the value
                        System.Console.WriteLine($"DEBUG (Tokenizer): Expanded variable '${varName}' to '{varValue}'");
                        i = varNameEnd - 1; // Move index past the variable name
                        continue; // Continue to next character after variable
                    }
                    else
                    {
                        // Just a literal '$' not followed by a valid variable name start
                        currentToken.Append(c);
                    }
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
