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
        /// Checks if a character is an Arabic letter (U+0621 to U+064A).
        /// </summary>
        private static bool IsArabicLetterChar(char c)
        {
            // Arabic letters range U+0621 to U+064A.
            // Consider adding U+0660-U+0669 for Eastern Arabic numerals if needed in identifiers.
            return (c >= '\u0621' && c <= '\u064A');
            // Note: The previous broader check (U+0600-U+06FF) included many non-letter characters.
        }

        /// <summary>
        /// Checks if a character is valid *within* an identifier (command name, parameter name, variable name).
        /// Allows Latin letters, digits, Arabic letters, underscore, and hyphen.
        /// </summary>
        private static bool IsValidIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || IsArabicLetterChar(c) || c == '_' || c == '-';
        }

        /// <summary>
        /// Checks if a character is valid *within* a general argument or path token.
        /// Allows identifier characters plus common path characters like '.', '\', '/'.
        /// </summary>
        private static bool IsValidArgumentChar(char c)
        {
            // Extend identifier chars with common path/argument chars
            return IsValidIdentifierChar(c) || c == '.' || c == '\\' || c == '/';
            // TODO: Consider adding other potentially valid argument chars if needed.
        }

        /// <summary>
        /// Checks if a character is valid to *start* an identifier.
        /// Allows Latin letters, Arabic letters, and underscore.
        /// (Typically identifiers don't start with digits or hyphens).
        /// </summary>
        private static bool IsValidIdentifierStartChar(char c)
        {
             return char.IsLetter(c) || IsArabicLetterChar(c) || c == '_';
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
                else if (commentIndex > 0)
                {
                    // If comment is present but not at start, parse the part before it
                    string beforeComment = lastStatement.Substring(0, commentIndex).Trim();
                    if (!string.IsNullOrWhiteSpace(beforeComment))
                    {
                        allStatementsCommands.Add(ParseSingleStatement(beforeComment));
                    }
                }
                else
                {
                    // No comment found
                    allStatementsCommands.Add(ParseSingleStatement(lastStatement));
                }
            }


            // TODO: Handle unterminated quotes at the statement level?
            if (inDoubleQuotes || inSingleQuotes)
            {
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
            List<string> remainingTokens = tokens.Skip(1).ToList(); // Start with tokens after command name

            // Create the command object early so we can add redirections
            var parsedCommand = new ParsedCommand(commandName, new List<object>(), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); // Changed List<string> to List<object>

            // --- Advanced Redirection Parsing ---
            // Loop through tokens to find and process *all* redirection operators
            // We need to iterate carefully as we remove items from the list
            for (int i = 0; i < remainingTokens.Count; /* No increment here */)
            {
                string currentToken = remainingTokens[i];
                int sourceStreamHandle = 1; // Default to stdout
                bool append = false;
                string redirectOperator = "";
                string targetToken = "";
                ParsedCommand.RedirectionTargetType targetType;

                // Identify the redirection operator and source stream
                if (currentToken == ">" || currentToken == ">>")
                {
                    redirectOperator = currentToken;
                    sourceStreamHandle = 1;
                }
                else if (currentToken.EndsWith(">>") && currentToken.Length > 2 && int.TryParse(currentToken.Substring(0, currentToken.Length - 2), out sourceStreamHandle)) // e.g., "2>>"
                {
                     redirectOperator = currentToken;
                }
                else if (currentToken.EndsWith(">") && currentToken.Length > 1 && int.TryParse(currentToken.Substring(0, currentToken.Length - 1), out sourceStreamHandle)) // e.g., "2>"
                {
                     redirectOperator = currentToken;
                }
                else if (currentToken.EndsWith(">&") && currentToken.Length > 2 && int.TryParse(currentToken.Substring(0, currentToken.Length - 2), out sourceStreamHandle)) // e.g., "2>&" (needs target handle)
                {
                    redirectOperator = currentToken;
                }
                 else if (currentToken.EndsWith(">>&") && currentToken.Length > 3 && int.TryParse(currentToken.Substring(0, currentToken.Length - 3), out sourceStreamHandle)) // e.g., "1>>&" (needs target handle)
                {
                    redirectOperator = currentToken;
                }
                else if (currentToken == ">&") // Default stdout >& (needs target handle)
                {
                    redirectOperator = currentToken;
                    sourceStreamHandle = 1;
                }
                 else if (currentToken == ">>&") // Default stdout >>& (needs target handle)
                {
                    redirectOperator = currentToken;
                    sourceStreamHandle = 1;
                }

                // If we identified a redirection operator
                if (!string.IsNullOrEmpty(redirectOperator))
                {
                    append = redirectOperator.Contains(">>");

                    // Check if a target token exists immediately after the operator
                    if (i + 1 < remainingTokens.Count)
                    {
                        targetToken = remainingTokens[i + 1];

                        // Determine target type (File or Stream Handle)
                        if (targetToken.StartsWith("&") && targetToken.Length > 1 && int.TryParse(targetToken.Substring(1), out _))
                        {
                            targetType = ParsedCommand.RedirectionTargetType.StreamHandle;
                            // Validate that stream handle redirection operators end with '&'
                            if (!redirectOperator.EndsWith("&")) {
                                System.Console.WriteLine($"WARN (Parser): Invalid redirection. Operator '{redirectOperator}' cannot target a stream handle '{targetToken}'.");
                                i++; // Move past operator, ignore target for now
                                continue;
                            }
                        }
                        else
                        {
                            targetType = ParsedCommand.RedirectionTargetType.FilePath;
                             // Validate that file path redirection operators do NOT end with '&'
                            if (redirectOperator.EndsWith("&")) {
                                System.Console.WriteLine($"WARN (Parser): Invalid redirection. Operator '{redirectOperator}' cannot target a file path '{targetToken}'.");
                                i++; // Move past operator, ignore target for now
                                continue;
                            }
                        }

                        // Create and add redirection info
                        var redirectionInfo = new ParsedCommand.RedirectionInfo(sourceStreamHandle, targetType, targetToken, append);
                        parsedCommand.AddRedirection(redirectionInfo);

                        // Remove the operator and the target token from the list
                        // Remove target first (higher index)
                        remainingTokens.RemoveAt(i + 1);
                        remainingTokens.RemoveAt(i);
                        // Do NOT increment i, as the list shifted and we need to check the new token at index i
                        continue; // Restart loop iteration from the current index
                    }
                    else
                    {
                        // Operator found, but no target token follows
                        System.Console.WriteLine($"WARN (Parser): Redirection operator '{redirectOperator}' found without a target.");
                        // Remove just the operator token
                        remainingTokens.RemoveAt(i);
                        // Do NOT increment i
                        continue; // Restart loop iteration
                    }
                }

                // If the current token was not a redirection operator, move to the next token
                i++;
            }


            // --- Argument/Parameter Parsing (using remaining tokens) ---
            List<object> arguments = parsedCommand.Arguments; // Changed to List<object>
            Dictionary<string, string> parameters = parsedCommand.Parameters; // Get the dictionary

            for (int i = 0; i < remainingTokens.Count; i++)
            {
                string currentToken = remainingTokens[i]; // Use remainingTokens here

                // Check for parameter name (starts with '-' and has more chars)
                if (currentToken.StartsWith("-") && currentToken.Length > 1)
                {
                    string paramName = currentToken;
                    string? paramValue = null;

                    // Check if the next token exists and is not another parameter
                    if (i + 1 < remainingTokens.Count && !remainingTokens[i + 1].StartsWith("-")) // Use remainingTokens here
                    {
                        paramValue = remainingTokens[i + 1]; // Use remainingTokens here
                        i++; // Consume the value token
                    }
                    parameters[paramName] = paramValue ?? string.Empty; // Store even if value is null (for switch parameters)
                }
                // --- SubExpression Parsing ---
                else if (currentToken == "$(") // Check for subexpression *before* treating as regular argument
                {
                    var subExpressionTokens = new List<string>();
                    int parenNestingLevel = 1; // Start at 1 due to the opening '$('
                    i++; // Move past the '$(' token

                    while (i < remainingTokens.Count)
                    {
                        string subToken = remainingTokens[i];
                        if (subToken == "(" || subToken == "$(") // Handle nested regular or sub-expressions
                        {
                            parenNestingLevel++;
                        }
                        else if (subToken == ")")
                        {
                            parenNestingLevel--;
                            if (parenNestingLevel == 0)
                            {
                                // Found the matching closing parenthesis
                                break; // Exit the inner loop
                            }
                        }
                        subExpressionTokens.Add(subToken);
                        i++; // Move to the next token within the subexpression
                    }

                    if (parenNestingLevel != 0)
                    {
                        System.Console.WriteLine("WARN (Parser): Unterminated subexpression '$()' found.");
                        // Decide how to handle - maybe add the tokens collected so far as arguments?
                        // For now, let's just add what we collected.
                        arguments.AddRange(subExpressionTokens.Cast<object>()); // Cast strings to object for AddRange
                    }
                    else
                    {
                        // Successfully parsed subexpression.
                        // Successfully parsed subexpression.
                        // Recursively parse the inner content.
                        string subExpressionInput = string.Join(" ", subExpressionTokens);
                        System.Console.WriteLine($"DEBUG (Parser): Recursively parsing subexpression content: '{subExpressionInput}'");
                        List<List<ParsedCommand>> subStatements = Parse(subExpressionInput);

                        // TODO: Handle multiple statements within subexpression? For now, assume one.
                        if (subStatements.Count > 0)
                        {
                            // Add the list of commands from the first statement as the argument.
                            arguments.Add(subStatements[0]); // Add List<ParsedCommand> to List<object>
                            System.Console.WriteLine($"DEBUG (Parser): Added parsed subexpression (statement 0) as argument.");
                        }
                        else
                        {
                            System.Console.WriteLine($"WARN (Parser): Subexpression '$({subExpressionInput})' parsed into zero statements.");
                            // Add an empty list or null? Or nothing? Add empty list for now.
                            arguments.Add(new List<ParsedCommand>());
                        }
                        // 'i' is already positioned after the closing ')' due to the loop structure
                    }
                }
                else // It's a regular argument (not a parameter or subexpression start)
                {
                     arguments.Add(currentToken);
                }
            }

            // Return the command object populated earlier
            return parsedCommand;
        }


        // --- State Machine Tokenizer Implementation ---
        private enum TokenizerStateSM { Default, InDoubleQuotes, InSingleQuotes, EscapeNextDefault, EscapeNextDouble }

        /// <summary>
        /// Tokenizes a single pipeline stage input string using a state machine.
        /// Respects single quotes (literal content), double quotes (allows escapes and variable expansion),
        /// and escape character '\' (makes next character literal).
        /// Handles Arabic letters in identifiers and parameter names like -ParamName or -اسم_معلمة.
        /// </summary>
        private static List<string> TokenizeInput(string stageInput)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            TokenizerStateSM state = TokenizerStateSM.Default;

            for (int i = 0; i < stageInput.Length; i++)
            {
                char c = stageInput[i];

                switch (state)
                {
                    case TokenizerStateSM.Default:
                        if (char.IsWhiteSpace(c))
                        {
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); }
                        }
                        else if (c == '"')
                        {
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); }
                            state = TokenizerStateSM.InDoubleQuotes;
                            currentToken.Append(c); // Keep quote start
                        }
                        else if (c == '\'')
                        {
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); }
                            state = TokenizerStateSM.InSingleQuotes;
                            currentToken.Append(c); // Keep quote start
                        }
                        else if (c == '\\')
                        {
                            state = TokenizerStateSM.EscapeNextDefault;
                        }
                        else if (c == '$' && i + 1 < stageInput.Length && IsValidIdentifierStartChar(stageInput[i + 1]))
                        {
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); }
                            int varNameStart = i + 1; int varNameEnd = varNameStart;
                            while (varNameEnd < stageInput.Length && (char.IsLetterOrDigit(stageInput[varNameEnd]) || IsArabicLetterChar(stageInput[varNameEnd]) || stageInput[varNameEnd] == '_')) { varNameEnd++; }
                            if (varNameEnd > varNameStart)
                            {
                                string varName = stageInput.Substring(varNameStart, varNameEnd - varNameStart);
                                string varValue = GetVariableValue(varName);
                                // Treat expanded value as a single token for now
                                tokens.Add(varValue);
                                System.Console.WriteLine($"DEBUG (Tokenizer): Expanded variable '${varName}' to '{varValue}' as separate token");
                                i = varNameEnd - 1;
                            }
                            else { currentToken.Append(c); } // Append '$' literally if not valid var name start
                        }
                        else if (IsValidIdentifierStartChar(c)) // Starts a new identifier (e.g., Get, احصل)
                        {
                            // If current token is not empty and doesn't look like an identifier start (e.g. operator), finalize it.
                            if (currentToken.Length > 0 && !IsValidIdentifierStartChar(currentToken[0]) && currentToken[0] != '-')
                            {
                                tokens.Add(currentToken.ToString()); currentToken.Clear();
                            }
                            currentToken.Append(c);
                        }
                        else if (currentToken.Length > 0 && IsValidArgumentChar(c)) // Continues an existing token (identifier, argument, path)
                        {
                            // Allow appending valid argument characters (including '.') to the current token
                            currentToken.Append(c);
                        }
                        // Note: Hyphen handling is implicitly covered by IsValidArgumentChar now if it's not the start char.
                        // We still need special handling if '-' *starts* a token.
                        else if (c == '-')
                        {
                             // If current token exists, finalize it before starting potential parameter
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); }

                            if (i + 1 < stageInput.Length && IsValidIdentifierStartChar(stageInput[i + 1]))
                            {
                                // Starts a parameter name (-Param, -اسم)
                                currentToken.Append(c);
                            }
                            else
                            {
                                // Standalone hyphen operator/argument
                                tokens.Add(c.ToString());
                            }
                        }
                        else // Other characters (operators, digits starting redirection, etc.)
                        {
                            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); currentToken.Clear(); } // Finalize previous token

                            // Check for multi-character operators first
                            if (c == '>' && i + 2 < stageInput.Length && stageInput[i + 1] == '>' && stageInput[i + 2] == '&' && i + 3 < stageInput.Length && char.IsDigit(stageInput[i + 3])) // >>&digit (e.g., >>&1)
                            {
                                tokens.Add(stageInput.Substring(i, 4)); i += 3;
                            }
                            else if (c == '>' && i + 1 < stageInput.Length && stageInput[i + 1] == '&' && i + 2 < stageInput.Length && char.IsDigit(stageInput[i + 2])) // >&digit (e.g., >&1)
                            {
                                tokens.Add(stageInput.Substring(i, 3)); i += 2;
                            }
                            else if (char.IsDigit(c) && i + 1 < stageInput.Length && stageInput[i + 1] == '>' && i + 2 < stageInput.Length && stageInput[i + 2] == '>') // digit>> (e.g., 2>>)
                            {
                                tokens.Add(stageInput.Substring(i, 3)); i += 2;
                            }
                             else if (c == '>' && i + 1 < stageInput.Length && stageInput[i + 1] == '>') // >>
                            {
                                tokens.Add(">>"); i++;
                            }
                            else if (char.IsDigit(c) && i + 1 < stageInput.Length && stageInput[i + 1] == '>') // digit> (e.g., 2>)
                            {
                                tokens.Add(stageInput.Substring(i, 2)); i += 1;
                            }
                            // Check for subexpression start '$(' before checking for single '>' or '('
                            else if (c == '$' && i + 1 < stageInput.Length && stageInput[i + 1] == '(')
                            {
                                tokens.Add("$("); i++; // Tokenize as '$('
                                // TODO: Need state change or recursive call for subexpression parsing
                            }
                            // Check for single-character operators / syntax elements
                            else if (c == '>' || c == '|' || c == ';' || c == '(' || c == ')' || c == '[' || c == ']' || c == '&') // Add other single chars?
                            {
                                tokens.Add(c.ToString());
                            }
                            // If none of the above, and not a valid argument character continuation (handled earlier), treat as unexpected.
                            else if (!IsValidArgumentChar(c)) // Check if it's truly invalid in any token context
                            {
                                // Maybe log a warning or throw an error? For now, add as single token.
                                System.Console.WriteLine($"WARN (Tokenizer): Encountered potentially unexpected character '{c}'. Treating as single token.");
                                tokens.Add(c.ToString());
                            }
                            // If it reaches here, it implies IsValidArgumentChar(c) is true, but it wasn't handled as a continuation.
                            // This might happen if a valid argument char starts a token immediately after an operator without space.
                            // Example: command>file. The '>' is handled above. Then 'f' is encountered.
                            // The IsValidIdentifierStartChar check earlier should handle 'f' correctly, starting a new token.
                            // So, this final implicit 'else' case where IsValidArgumentChar(c) is true should not be needed.
                        }
                        break;

                    case TokenizerStateSM.InDoubleQuotes:
                         currentToken.Append(c); // Append char within quotes
                         if (c == '"')
                        {
                            // We've reached the closing quote. Finalize the token.
                            tokens.Add(currentToken.ToString());
                            currentToken.Clear();
                            state = TokenizerStateSM.Default; // Exit double quotes
                        }
                        else if (c == '\\')
                        {
                            // Don't append the backslash yet, move to escape state
                            state = TokenizerStateSM.EscapeNextDouble;
                        }
                        else if (c == '$' && i + 1 < stageInput.Length && IsValidIdentifierStartChar(stageInput[i+1])) // Basic variable check
                        {
                            // Attempt variable expansion inside double quotes
                            int varNameStart = i + 1; int varNameEnd = varNameStart;
                             while (varNameEnd < stageInput.Length && (char.IsLetterOrDigit(stageInput[varNameEnd]) || IsArabicLetterChar(stageInput[varNameEnd]) || stageInput[varNameEnd] == '_'))
                            {
                                varNameEnd++;
                            }

                            if (varNameEnd > varNameStart)
                            { // Found variable
                                string varName = stageInput.Substring(varNameStart, varNameEnd - varNameStart);
                                string varValue = GetVariableValue(varName);
                                currentToken.Append(varValue); // Append expanded value
                                System.Console.WriteLine($"DEBUG (Tokenizer): Expanded variable '${varName}' to '{varValue}' (in double quotes)");
                                i = varNameEnd - 1; // Move index past variable name
                            }
                            else { currentToken.Append(c); } // Append '$' literally if not valid var name
                        }
                        // else // Normal character inside double quotes is handled by the initial append at the start of the state case
                        break;

                    case TokenizerStateSM.InSingleQuotes:
                        currentToken.Append(c); // Append literally (no escapes or variables)
                        if (c == '\'')
                        {
                            // We've reached the closing quote. Finalize the token.
                            tokens.Add(currentToken.ToString());
                            currentToken.Clear();
                            state = TokenizerStateSM.Default; // Exit single quotes
                        }
                        // else // Character was appended above
                        break;

                    case TokenizerStateSM.EscapeNextDefault: // After '\' in Default state
                        // Append the escaped character literally, regardless of what it is.
                        // This handles escaping operators like \| or \>
                        currentToken.Append(c);
                        state = TokenizerStateSM.Default; // Return to Default state.
                        break;

                    case TokenizerStateSM.EscapeNextDouble: // After '\' in InDoubleQuotes state
                        // Inside double quotes, \ only escapes $, ", and \ itself.
                        // Other characters preceded by \ are treated literally including the backslash.
                        if (c == '$' || c == '"' || c == '\\') // Removed backtick '`'
                        {
                            currentToken.Append(c); // Append the specifically escaped character
                        }
                        else
                        {
                            // For other characters, append the backslash *and* the character
                            currentToken.Append('\\');
                            currentToken.Append(c);
                        }
                        state = TokenizerStateSM.InDoubleQuotes; // Return to InDoubleQuotes state.
                        break;
                }
            }

            // Finalize last token if it exists
            if (currentToken.Length > 0) { tokens.Add(currentToken.ToString()); }

            // Check for unterminated states
            if (state == TokenizerStateSM.EscapeNextDefault || state == TokenizerStateSM.EscapeNextDouble)
            {
                System.Console.WriteLine("WARN (Tokenizer): Input ended with an escape character.");
                // Append the trailing backslash? Or ignore? Let's ignore for now.
            }
            else if (state != TokenizerStateSM.Default)
            {
                System.Console.WriteLine("WARN (Tokenizer): Unterminated quote detected.");
                // Consider throwing FormatException("Unterminated quote in input.");
            }
            return tokens;
        }

    }
}
