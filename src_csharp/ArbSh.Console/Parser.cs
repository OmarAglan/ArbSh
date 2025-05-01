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
            bool inDoubleQuotes = false;
            bool inSingleQuotes = false; // Add tracking for single quotes
            int start = 0;

            for (int i = 0; i < inputLine.Length; i++)
            {
                char c = inputLine[i];

                // Handle comments: If # is encountered outside quotes, ignore the rest of the line
                if (c == '#' && !inDoubleQuotes && !inSingleQuotes)
                {
                    // If # is the first char of the potential statement, ignore the whole line segment
                    if (i == start || string.IsNullOrWhiteSpace(inputLine.Substring(start, i - start)))
                    {
                        start = inputLine.Length; // Effectively skip the rest
                    }
                    break; // Ignore rest of the current line segment being processed
                }

                // Handle escaping - skip next char if escaped (only applies outside single quotes)
                if (c == '\\' && !inSingleQuotes && i + 1 < inputLine.Length)
                {
                    i++;
                    continue; // Skip escape character logic for statement splitting
                }

                // Toggle quote states
                if (c == '"' && !inSingleQuotes)
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if (c == '\'' && !inDoubleQuotes) // Add single quote handling
                {
                    inSingleQuotes = !inSingleQuotes;
                }

                // Split statements only if outside *both* types of quotes
                if (c == ';' && !inDoubleQuotes && !inSingleQuotes)
                {
                    string statement = inputLine.Substring(start, i - start).Trim();
                    // Ignore empty statements or statements that were just comments
                    if (!string.IsNullOrWhiteSpace(statement) && !statement.TrimStart().StartsWith("#"))
                    {
                        allStatementsCommands.Add(ParseSingleStatement(statement));
                    }
                    start = i + 1; // Start next statement after the semicolon
                }
            }

            // Add the last statement (or the only statement if no semicolons)
            string lastStatement = inputLine.Substring(start).Trim();
            // Ignore empty statements or statements that were just comments
            if (!string.IsNullOrWhiteSpace(lastStatement) && !lastStatement.TrimStart().StartsWith("#"))
            {
                 // Also ignore if the remaining part starts with # after trimming whitespace
                 int commentIndex = lastStatement.IndexOf('#');
                 if (commentIndex == 0) { /* Ignore */ }
                 else if (commentIndex > 0) {
                     // If comment is present but not at start, parse the part before it
                     string beforeComment = lastStatement.Substring(0, commentIndex).Trim();
                     if (!string.IsNullOrWhiteSpace(beforeComment)) {
                         allStatementsCommands.Add(ParseSingleStatement(beforeComment));
                     }
                 } else {
                     // No comment found
                     allStatementsCommands.Add(ParseSingleStatement(lastStatement));
                 }
            }


            // TODO: Handle unterminated quotes at the statement level?
            if (inDoubleQuotes || inSingleQuotes) {
                 System.Console.WriteLine("WARN (Parser): Unterminated quote detected at end of input line.");
                 // Potentially throw error or try to recover?
            }


            System.Console.WriteLine($"DEBUG (Parser): Parsed into {allStatementsCommands.Count} statement(s).");
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
        /// Tokenizes a single pipeline stage input string, respecting quotes and handling escapes.
        /// </summary>
        private static List<string> TokenizeInput(string stageInput)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            bool inDoubleQuotes = false;
            bool inSingleQuotes = false;
            bool treatNextCharLiteral = false; // Flag for escape character '\'

            for (int i = 0; i < stageInput.Length; i++)
            {
                char c = stageInput[i];

                // --- Escape Character Handling ---
                // If the previous char was '\' (and we're not inside single quotes)
                if (treatNextCharLiteral)
                {
                    // Add this char literally, regardless of what it is
                    currentToken.Append(c);
                    treatNextCharLiteral = false;
                    continue;
                }

                // If current char is '\' (and not inside single quotes)
                if (c == '\\' && !inSingleQuotes && i + 1 < stageInput.Length)
                {
                    // Set flag to treat the *next* character literally
                    treatNextCharLiteral = true;
                    // Do not append the backslash itself *yet* - let the next iteration handle the escaped char
                    continue;
                }

                // --- Quoting ---
                if (c == '"' && !inSingleQuotes) // Toggle double quotes if not inside single quotes
                {
                    inDoubleQuotes = !inDoubleQuotes;
                    // Don't add the quote mark itself to the token
                    continue;
                }
                if (c == '\'' && !inDoubleQuotes) // Toggle single quotes if not inside double quotes
                {
                    inSingleQuotes = !inSingleQuotes;
                    // Don't add the quote mark itself to the token
                    continue;
                }

                // --- Token Splitting ---
                // Split on whitespace only if *outside both* single and double quotes
                if (char.IsWhiteSpace(c) && !inDoubleQuotes && !inSingleQuotes)
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                    // Skip the whitespace character itself
                    continue;
                }

                // --- Variable Expansion ---
                // Expand $variable only if *outside single quotes* and not escaped
                if (c == '$' && !inSingleQuotes && i + 1 < stageInput.Length)
                {
                    int varNameStart = i + 1;
                    int varNameEnd = varNameStart;
                    // Basic variable name: letters, numbers, underscore
                    while (varNameEnd < stageInput.Length && (char.IsLetterOrDigit(stageInput[varNameEnd]) || stageInput[varNameEnd] == '_'))
                    {
                        varNameEnd++;
                    }

                    if (varNameEnd > varNameStart) // Found a potential variable name
                    {
                        string varName = stageInput.Substring(varNameStart, varNameEnd - varNameStart);
                        string varValue = GetVariableValue(varName);
                        currentToken.Append(varValue); // Append the value
                        System.Console.WriteLine($"DEBUG (Tokenizer): Expanded variable '${varName}' to '{varValue}'");
                        i = varNameEnd - 1; // Move index past the variable name
                        continue; // Continue to next character after variable
                    }
                    // else: Fall through to treat '$' as a literal character if not followed by valid var name
                }

                // --- Character Appending ---
                // If none of the above conditions met, append the character to the current token
                currentToken.Append(c);
            }

            // Add the last token if any
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            // TODO: Handle unterminated quotes more robustly (e.g., throw a specific parsing error)
            if (inDoubleQuotes || inSingleQuotes)
            {
                 System.Console.WriteLine("WARN (Tokenizer): Unterminated quote detected in pipeline stage.");
                 // Decide how to handle this - error or treat as literal? For now, let it pass.
            }

            return tokens;
        }
    }
}
