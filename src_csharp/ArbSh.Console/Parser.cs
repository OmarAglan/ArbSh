using System;
using System.Collections.Generic;
using System.Linq; // Needed for Skip
using System.Text; // Needed for StringBuilder

namespace ArbSh.Console
{
    /// <summary>
    /// Responsible for parsing user input strings into executable commands/structures.
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
            bool inDoubleQuotes = false; // Track quotes specifically for pipeline splitting
            bool inSingleQuotes = false; // Track single quotes for pipeline splitting

            for (int i = 0; i < statementInput.Length; i++)
            {
                char c = statementInput[i];

                // Handle escaping (only outside single quotes)
                if (c == '\\' && !inSingleQuotes && i + 1 < statementInput.Length)
                {
                    i++; // Skip escaped character for pipeline splitting logic
                    continue;
                }

                // Toggle quote states
                if (c == '"' && !inSingleQuotes)
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                 else if (c == '\'' && !inDoubleQuotes)
                {
                    inSingleQuotes = !inSingleQuotes;
                }

                // Split pipeline only if outside *both* types of quotes
                if (c == '|' && !inDoubleQuotes && !inSingleQuotes)
                {
                    string stageInput = statementInput.Substring(stageStart, i - stageStart).Trim();
                    if (!string.IsNullOrWhiteSpace(stageInput))
                    {
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
        /// Handles tokenization, redirection, arguments, parameters.
        /// </summary>
        private static ParsedCommand? ParseSinglePipelineStage(string stageInput)
        {
             // Tokenize the current pipeline stage input using the state machine
                List<string> tokens = TokenizeInput(stageInput); // Use the new tokenizer
                if (tokens.Count == 0) return null;

                string commandName = tokens[0];
                List<string> commandTokens = tokens.Skip(1).ToList();
                string? redirectPath = null;
                bool append = false;

                // --- Redirection Parsing (Basic) ---
                // TODO: Make redirection parsing more robust (e.g., handle 2>&1, allow anywhere)
                int redirectOperatorIndex = -1;
                for (int i = 0; i < commandTokens.Count; i++)
                {
                    // Check for redirection operators (simple check, assumes space separation)
                    if (commandTokens[i] == ">" || commandTokens[i] == ">>")
                    {
                        redirectOperatorIndex = i;
                        append = (commandTokens[i] == ">>");
                        if (i + 1 < commandTokens.Count)
                        {
                            redirectPath = commandTokens[i + 1];
                            commandTokens.RemoveRange(i, 2); // Remove operator and path
                        }
                        else
                        {
                            System.Console.WriteLine($"WARN (Parser): Redirection operator '{commandTokens[i]}' found without a target path.");
                            commandTokens.RemoveAt(i); // Remove just the operator
                        }
                        break; // Handle only the first redirection found for now
                    }
                }

                // --- Argument/Parameter Parsing ---
                List<string> arguments = new List<string>();
                Dictionary<string, string> parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < commandTokens.Count; i++)
                {
                    string currentToken = commandTokens[i];

                    // Check for parameter name (starts with '-' and has more chars)
                    if (currentToken.StartsWith("-") && currentToken.Length > 1)
                    {
                        string paramName = currentToken;
                        string? paramValue = null;

                        // Check if the next token exists and is not another parameter
                        if (i + 1 < commandTokens.Count && !commandTokens[i + 1].StartsWith("-"))
                        {
                            paramValue = commandTokens[i + 1]; // Assign the next token as value
                            i++; // Consume the value token
                        }
                        parameters[paramName] = paramValue ?? string.Empty; // Store even if value is null (for switch parameters)
                    }
                    else // It's an argument
                    {
                        arguments.Add(currentToken);
                    }
                }

                // Create the command object
                var parsedCommand = new ParsedCommand(commandName, arguments, parameters);
                if (redirectPath != null)
                {
                    parsedCommand.SetOutputRedirection(redirectPath, append);
                    System.Console.WriteLine($"DEBUG (Parser): Found redirection: {(append ? ">>" : ">")} '{redirectPath}'");
                }
                return parsedCommand;
        }


        // --- State Machine Tokenizer Implementation ---
        private enum TokenizerStateSM { Default, InDoubleQuotes, InSingleQuotes, EscapeNextDefault, EscapeNextDouble }

        /// <summary>
        /// Tokenizes a single pipeline stage input string using a state machine.
        /// Respects single quotes (literal content), double quotes (allows escapes and variable expansion),
        /// and escape character '\' (makes next character literal).
        /// </summary>
        private static List<string> TokenizeInput(string stageInput) {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            TokenizerStateSM state = TokenizerStateSM.Default;

            for (int i = 0; i < stageInput.Length; i++) {
                char c = stageInput[i];

                switch (state) {
                    case TokenizerStateSM.Default:
                        if (char.IsWhiteSpace(c)) {
                            // Finalize previous token if exists
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); }
                        } else if (c == '"') {
                            state = TokenizerStateSM.InDoubleQuotes; // Enter double quotes
                        } else if (c == '\'') {
                            state = TokenizerStateSM.InSingleQuotes; // Enter single quotes
                        } else if (c == '\\') {
                            state = TokenizerStateSM.EscapeNextDefault; // Expecting next char to be escaped
                        } else if (c == '$' && i + 1 < stageInput.Length) {
                            // Attempt variable expansion
                            int varNameStart = i + 1; int varNameEnd = varNameStart;
                            while (varNameEnd < stageInput.Length && (char.IsLetterOrDigit(stageInput[varNameEnd]) || stageInput[varNameEnd] == '_')) { varNameEnd++; }
                            if (varNameEnd > varNameStart) { // Found variable
                                string varName = stageInput.Substring(varNameStart, varNameEnd - varNameStart); string varValue = GetVariableValue(varName); currentToken.Append(varValue);
                                System.Console.WriteLine($"DEBUG (Tokenizer): Expanded variable '${varName}' to '{varValue}'"); i = varNameEnd - 1;
                            } else { currentToken.Append(c); } // Append '$' literally
                        } else {
                            currentToken.Append(c); // Append normal char
                        }
                        break;

                    case TokenizerStateSM.InDoubleQuotes:
                         if (c == '"') {
                            state = TokenizerStateSM.Default; // Exit double quotes
                        } else if (c == '\\') {
                            state = TokenizerStateSM.EscapeNextDouble; // Expecting next char to be escaped
                        } else if (c == '$' && i + 1 < stageInput.Length) {
                             // Attempt variable expansion inside double quotes
                            int varNameStart = i + 1; int varNameEnd = varNameStart;
                            while (varNameEnd < stageInput.Length && (char.IsLetterOrDigit(stageInput[varNameEnd]) || stageInput[varNameEnd] == '_')) { varNameEnd++; }
                            if (varNameEnd > varNameStart) { // Found variable
                                string varName = stageInput.Substring(varNameStart, varNameEnd - varNameStart); string varValue = GetVariableValue(varName); currentToken.Append(varValue);
                                System.Console.WriteLine($"DEBUG (Tokenizer): Expanded variable '${varName}' to '{varValue}' (in double quotes)"); i = varNameEnd - 1;
                            } else { currentToken.Append(c); } // Append '$' literally
                        } else {
                            currentToken.Append(c); // Append normal char
                        }
                        break;

                    case TokenizerStateSM.InSingleQuotes:
                        if (c == '\'') {
                            state = TokenizerStateSM.Default; // Exit single quotes
                        } else {
                            currentToken.Append(c); // Append literally (no escapes or variables)
                        }
                        break;

                    case TokenizerStateSM.EscapeNextDefault: // After '\' in Default state
                        // Append the escaped character literally.
                        currentToken.Append(c);
                        state = TokenizerStateSM.Default; // Return to Default state.
                        break;

                     case TokenizerStateSM.EscapeNextDouble: // After '\' in InDoubleQuotes state
                        // Append the escaped character literally. Handles \", \\, \$ etc.
                        currentToken.Append(c);
                        state = TokenizerStateSM.InDoubleQuotes; // Return to InDoubleQuotes state.
                        break;
                }
            }

            // Finalize last token if it exists
            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); }

            // Check for unterminated states
            if (state == TokenizerStateSM.EscapeNextDefault || state == TokenizerStateSM.EscapeNextDouble) {
                System.Console.WriteLine("WARN (Tokenizer): Input ended with an escape character.");
                // Append the trailing backslash? Or ignore? Let's ignore for now.
            } else if (state != TokenizerStateSM.Default) {
                System.Console.WriteLine("WARN (Tokenizer): Unterminated quote detected.");
                // Consider throwing FormatException("Unterminated quote in input.");
            }
            return tokens;
        }

    }
}
